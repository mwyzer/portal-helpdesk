using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AIHelpdesk.Tests.Services;

public class RecruitmentAIServiceTests
{
    // RecruitmentAIService now parses PDFs for real (PdfPig) instead of a permissive raw-bytes
    // scan, so a fixture that's just plain text with a ".pdf" extension no longer extracts any
    // text -- it correctly gets rejected as not-a-real-PDF. Use a real, minimal .docx instead,
    // which exercises the already-solid DocumentFormat.OpenXml extraction path.
    private static string CreateTestDocxFile(string text)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");
        using (var wordDoc = WordprocessingDocument.Create(tempFile, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
            mainPart.Document.Save();
        }
        return tempFile;
    }

    private static (RecruitmentAIService Service, ApplicationDbContext Context, Mock<IAIService> AiMock) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);
        var aiMock = new Mock<IAIService>();
        var service = new RecruitmentAIService(context, aiMock.Object, NullLogger<RecruitmentAIService>.Instance);
        return (service, context, aiMock);
    }

    private static async Task<(Guid CandidateId, Guid VacancyId)> SeedCandidateAsync(ApplicationDbContext context, string? aiSummaryJson = null)
    {
        var poster = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        context.Users.Add(poster);

        var vacancy = new JobVacancy { Title = "Engineer", Description = "d", Requirements = "5 years C#", PostedById = poster.Id };
        context.JobVacancies.Add(vacancy);

        var candidate = new Candidate
        {
            JobVacancyId = vacancy.Id,
            JobVacancy = vacancy,
            FullName = "Jane",
            Email = "j@test.com",
            AISummaryJson = aiSummaryJson
        };
        context.Candidates.Add(candidate);

        await context.SaveChangesAsync();
        return (candidate.Id, vacancy.Id);
    }

    [Fact]
    public async Task SummarizeCvAsync_ShouldThrow_WhenNoCvUploaded()
    {
        var (service, context, _) = CreateService();
        var (candidateId, _) = await SeedCandidateAsync(context);

        var act = () => service.SummarizeCvAsync(candidateId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SummarizeCvAsync_ShouldThrow_WhenCandidateNotFound()
    {
        var (service, _, _) = CreateService();

        var act = () => service.SummarizeCvAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SummarizeCvAsync_ShouldParseAIResponse_AndStoreOnCandidate()
    {
        var (service, context, aiMock) = CreateService();
        var (candidateId, _) = await SeedCandidateAsync(context);

        var tempFile = CreateTestDocxFile("Experienced backend engineer with skills in C# and SQL.");
        context.CandidateDocuments.Add(new CandidateDocument
        {
            CandidateId = candidateId,
            FileName = "resume.docx",
            FilePath = tempFile,
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            UploadedById = (await context.Users.FirstAsync()).Id
        });
        await context.SaveChangesAsync();

        aiMock.Setup(a => a.GenerateChatResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<(string Role, string Content)>>(), null))
            .ReturnsAsync("""{"skills":["C#","SQL"],"experienceSummary":"5 years backend","educationSummary":"BSc CS"}""");

        var result = await service.SummarizeCvAsync(candidateId);

        result.Skills.Should().Contain(["C#", "SQL"]);
        result.ExperienceSummary.Should().Be("5 years backend");

        var candidate = await context.Candidates.AsNoTracking().FirstAsync(c => c.Id == candidateId);
        candidate.AISummaryJson.Should().NotBeNullOrEmpty();

        File.Delete(tempFile);
    }

    [Fact]
    public async Task SummarizeCvAsync_ShouldReturnEmptyResult_WhenAIThrows()
    {
        var (service, context, aiMock) = CreateService();
        var (candidateId, _) = await SeedCandidateAsync(context);

        var tempFile = CreateTestDocxFile("Some resume content.");
        context.CandidateDocuments.Add(new CandidateDocument
        {
            CandidateId = candidateId,
            FileName = "resume.docx",
            FilePath = tempFile,
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            UploadedById = (await context.Users.FirstAsync()).Id
        });
        await context.SaveChangesAsync();

        aiMock.Setup(a => a.GenerateChatResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<(string Role, string Content)>>(), null))
            .ThrowsAsync(new HttpRequestException("network down"));

        var result = await service.SummarizeCvAsync(candidateId);

        result.Skills.Should().BeEmpty();
        result.RawSummary.Should().Be("AI summarization unavailable.");

        File.Delete(tempFile);
    }

    [Fact]
    public async Task MatchCandidatesAsync_ShouldReturnZeroScore_WhenNoCvSummary()
    {
        var (service, context, _) = CreateService();
        var (_, vacancyId) = await SeedCandidateAsync(context);

        var results = await service.MatchCandidatesAsync(vacancyId);

        results.Should().ContainSingle();
        results[0].MatchScore.Should().Be(0);
    }

    [Fact]
    public async Task MatchCandidatesAsync_ShouldParseAIResponse_WhenSummaryExists()
    {
        var (service, context, aiMock) = CreateService();
        var (_, vacancyId) = await SeedCandidateAsync(context, aiSummaryJson: """{"skills":["C#"]}""");

        aiMock.Setup(a => a.GenerateChatResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<(string Role, string Content)>>(), null))
            .ReturnsAsync("""{"score":0.85,"reason":"Strong technical match"}""");

        var results = await service.MatchCandidatesAsync(vacancyId);

        results.Should().ContainSingle();
        results[0].MatchScore.Should().Be(0.85);
        results[0].Reason.Should().Be("Strong technical match");
    }

    [Fact]
    public async Task GenerateInterviewQuestionsAsync_ShouldStoreQuestions()
    {
        var (service, context, aiMock) = CreateService();
        var (candidateId, _) = await SeedCandidateAsync(context, aiSummaryJson: """{"skills":["C#"]}""");
        var interviewer = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        context.Users.Add(interviewer);
        var interview = new Interview { CandidateId = candidateId, InterviewerId = interviewer.Id, ScheduledAt = DateTime.UtcNow.AddDays(1) };
        context.Interviews.Add(interview);
        await context.SaveChangesAsync();

        aiMock.Setup(a => a.GenerateChatResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<(string Role, string Content)>>(), null))
            .ReturnsAsync("""[{"question":"Explain SOLID principles","category":"Technical"},{"question":"Tell me about a conflict","category":"Behavioral"}]""");

        var result = await service.GenerateInterviewQuestionsAsync(interview.Id);

        result.Questions.Should().HaveCount(2);
        result.Questions.Should().Contain(q => q.Question == "Explain SOLID principles" && q.IsAIGenerated);
    }

    [Fact]
    public async Task GenerateInterviewQuestionsAsync_ShouldReturnEmpty_WhenAIThrows()
    {
        var (service, context, aiMock) = CreateService();
        var (candidateId, _) = await SeedCandidateAsync(context);
        var interviewer = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        context.Users.Add(interviewer);
        var interview = new Interview { CandidateId = candidateId, InterviewerId = interviewer.Id, ScheduledAt = DateTime.UtcNow.AddDays(1) };
        context.Interviews.Add(interview);
        await context.SaveChangesAsync();

        aiMock.Setup(a => a.GenerateChatResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<(string Role, string Content)>>(), null))
            .ThrowsAsync(new HttpRequestException("network down"));

        var result = await service.GenerateInterviewQuestionsAsync(interview.Id);

        result.Questions.Should().BeEmpty();
    }
}
