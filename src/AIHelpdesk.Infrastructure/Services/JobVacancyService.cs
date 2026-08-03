using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class JobVacancyService : IJobVacancyService
{
    private readonly ApplicationDbContext _context;

    public JobVacancyService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static JobVacancyResponse MapToResponse(JobVacancy v) => new(
        v.Id, v.Title, v.Description, v.Requirements,
        v.DepartmentId, v.Department?.Name, v.PositionId, v.Position?.Name,
        v.OpeningsCount, v.Status.ToString(), v.PostedById, v.PostedBy.FullName,
        v.Candidates.Count, v.PublishedAt, v.ClosedAt, v.CreatedAt);

    public async Task<PagedResult<JobVacancyResponse>> GetAllAsync(int page, int pageSize, string? status, Guid? departmentId)
    {
        var query = _context.JobVacancies
            .Include(v => v.Department)
            .Include(v => v.Position)
            .Include(v => v.PostedBy)
            .Include(v => v.Candidates)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<VacancyStatus>(status, true, out var s))
            query = query.Where(v => v.Status == s);
        if (departmentId.HasValue)
            query = query.Where(v => v.DepartmentId == departmentId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<JobVacancyResponse>(items.Select(MapToResponse).ToList(), totalCount, page, pageSize);
    }

    public async Task<JobVacancyResponse> GetByIdAsync(Guid id)
    {
        var vacancy = await _context.JobVacancies
            .Include(v => v.Department)
            .Include(v => v.Position)
            .Include(v => v.PostedBy)
            .Include(v => v.Candidates)
            .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new KeyNotFoundException("Job vacancy not found");

        return MapToResponse(vacancy);
    }

    public async Task<JobVacancyResponse> CreateAsync(Guid userId, CreateJobVacancyRequest request)
    {
        var vacancy = new JobVacancy
        {
            Title = request.Title,
            Description = request.Description,
            Requirements = request.Requirements,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            OpeningsCount = request.OpeningsCount,
            Status = VacancyStatus.Draft,
            PostedById = userId
        };

        _context.JobVacancies.Add(vacancy);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(vacancy.Id);
    }

    public async Task<JobVacancyResponse> UpdateAsync(Guid id, UpdateJobVacancyRequest request)
    {
        var vacancy = await _context.JobVacancies.FindAsync(id)
            ?? throw new KeyNotFoundException("Job vacancy not found");

        if (vacancy.Status is VacancyStatus.Closed or VacancyStatus.Filled)
            throw new InvalidOperationException("Cannot edit a closed or filled vacancy");

        vacancy.Title = request.Title;
        vacancy.Description = request.Description;
        vacancy.Requirements = request.Requirements;
        vacancy.DepartmentId = request.DepartmentId;
        vacancy.PositionId = request.PositionId;
        vacancy.OpeningsCount = request.OpeningsCount;
        vacancy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<JobVacancyResponse> PublishAsync(Guid id)
    {
        var vacancy = await _context.JobVacancies.FindAsync(id)
            ?? throw new KeyNotFoundException("Job vacancy not found");

        if (vacancy.Status != VacancyStatus.Draft)
            throw new InvalidOperationException("Only draft vacancies can be published");

        vacancy.Status = VacancyStatus.Published;
        vacancy.PublishedAt = DateTime.UtcNow;
        vacancy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<JobVacancyResponse> CloseAsync(Guid id)
    {
        var vacancy = await _context.JobVacancies.FindAsync(id)
            ?? throw new KeyNotFoundException("Job vacancy not found");

        if (vacancy.Status != VacancyStatus.Published)
            throw new InvalidOperationException("Only published vacancies can be closed");

        var hiredCount = await _context.Candidates
            .CountAsync(c => c.JobVacancyId == id && c.Stage == CandidateStage.Hired);

        vacancy.Status = hiredCount >= vacancy.OpeningsCount ? VacancyStatus.Filled : VacancyStatus.Closed;
        vacancy.ClosedAt = DateTime.UtcNow;
        vacancy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }
}
