using AIHelpdesk.Contracts.Employees;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class EmployeeServiceTests
{
    private static async Task<(EmployeeService Service, ApplicationDbContext Context)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new EmployeeService(context);
        return (service, context);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateEmployee()
    {
        var (service, context) = await CreateServiceAsync();
        var dept = TestDataFactory.CreateDepartment();
        var pos = TestDataFactory.CreatePosition(dept.Id);
        context.Departments.Add(dept);
        context.Positions.Add(pos);
        await context.SaveChangesAsync();

        var request = new CreateEmployeeRequest(
            "EMP-001", "John Doe", "john@example.com", "+62812345678",
            DateOnly.FromDateTime(DateTime.UtcNow), dept.Id, pos.Id, null, "Jakarta");

        var result = await service.CreateEmployeeAsync(request);

        result.EmployeeNo.Should().Be("EMP-001");
        result.FullName.Should().Be("John Doe");
        result.DepartmentName.Should().Be(dept.Name);
        result.PositionName.Should().Be(pos.Name);
    }

    [Fact]
    public async Task GetEmployeesAsync_ShouldReturnPaginatedResults()
    {
        var (service, context) = await CreateServiceAsync();
        for (int i = 0; i < 15; i++)
        {
            context.Employees.Add(TestDataFactory.CreateEmployee(
                employeeNo: $"EMP-{i:D3}", fullName: $"Employee {i}", email: $"e{i}@test.com"));
        }
        await context.SaveChangesAsync();

        var result = await service.GetEmployeesAsync(1, 5, null, null, null);

        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetEmployeesAsync_ShouldFilterBySearch()
    {
        var (service, context) = await CreateServiceAsync();
        context.Employees.Add(TestDataFactory.CreateEmployee(fullName: "Alice", email: "alice@test.com", employeeNo: "EMP-001"));
        context.Employees.Add(TestDataFactory.CreateEmployee(fullName: "Bob", email: "bob@test.com", employeeNo: "EMP-002"));
        await context.SaveChangesAsync();

        var result = await service.GetEmployeesAsync(1, 10, "Alice", null, null);

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetEmployeesAsync_ShouldFilterByDepartment()
    {
        var (service, context) = await CreateServiceAsync();
        var dept = TestDataFactory.CreateDepartment("HR");
        context.Departments.Add(dept);
        context.Employees.Add(TestDataFactory.CreateEmployee(fullName: "Alice", departmentId: dept.Id, employeeNo: "EMP-001"));
        context.Employees.Add(TestDataFactory.CreateEmployee(fullName: "Bob", employeeNo: "EMP-002"));
        await context.SaveChangesAsync();

        var result = await service.GetEmployeesAsync(1, 10, null, dept.Id, null);

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetEmployeesAsync_ShouldFilterByStatus()
    {
        var (service, context) = await CreateServiceAsync();
        context.Employees.Add(TestDataFactory.CreateEmployee(fullName: "Alice", employeeNo: "EMP-001", employmentStatus: AIHelpdesk.Domain.Common.EmploymentStatus.Active));
        context.Employees.Add(TestDataFactory.CreateEmployee(fullName: "Bob", employeeNo: "EMP-002", employmentStatus: AIHelpdesk.Domain.Common.EmploymentStatus.Resigned));
        await context.SaveChangesAsync();

        var result = await service.GetEmployeesAsync(1, 10, null, null, "Active");

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetEmployeeAsync_ShouldReturnEmployee()
    {
        var (service, context) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var result = await service.GetEmployeeAsync(emp.Id);

        result.FullName.Should().Be(emp.FullName);
        result.Email.Should().Be(emp.Email);
    }

    [Fact]
    public async Task GetEmployeeAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _) = await CreateServiceAsync();

        var act = () => service.GetEmployeeAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateEmployeeAsync_ShouldUpdateFields()
    {
        var (service, context) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee(fullName: "Old Name");
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var request = new UpdateEmployeeRequest("New Name", "+62899999999", null, null, null, "Remote");
        var result = await service.UpdateEmployeeAsync(emp.Id, request);

        result.FullName.Should().Be("New Name");
        result.Phone.Should().Be("+62899999999");
        result.WorkLocation.Should().Be("Remote");
    }

    [Fact]
    public async Task DeleteEmployeeAsync_ShouldSoftDelete()
    {
        var (service, context) = await CreateServiceAsync();
        var emp = TestDataFactory.CreateEmployee();
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        await service.DeleteEmployeeAsync(emp.Id);

        var deleted = await context.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == emp.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyProfileAsync_ShouldReturnByUserId()
    {
        var (service, context) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var emp = TestDataFactory.CreateEmployee(userId: userId, fullName: "Jane Doe");
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var result = await service.GetMyProfileAsync(userId);

        result.FullName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task ImportFromExcelAsync_ShouldImportValidRows()
    {
        var (service, context) = await CreateServiceAsync();
        var dept = TestDataFactory.CreateDepartment("Engineering");
        var pos = TestDataFactory.CreatePosition(dept.Id, "Engineer");
        context.Departments.Add(dept);
        context.Positions.Add(pos);
        await context.SaveChangesAsync();

        // Create a simple XLSX in memory
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Employees");
        ws.Cell(1, 1).Value = "EmployeeNo";
        ws.Cell(1, 2).Value = "FullName";
        ws.Cell(1, 3).Value = "Email";
        ws.Cell(1, 4).Value = "Phone";
        ws.Cell(1, 5).Value = "JoinDate";
        ws.Cell(1, 6).Value = "Department";
        ws.Cell(1, 7).Value = "Position";
        ws.Cell(1, 8).Value = "WorkLocation";
        ws.Cell(2, 1).Value = "EMP-010";
        ws.Cell(2, 2).Value = "Import Test";
        ws.Cell(2, 3).Value = "import@test.com";
        ws.Cell(2, 4).Value = "+62811111111";
        ws.Cell(2, 5).Value = "2024-01-15";
        ws.Cell(2, 6).Value = "Engineering";
        ws.Cell(2, 7).Value = "Engineer";
        ws.Cell(2, 8).Value = "Jakarta";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.ImportFromExcelAsync(stream);

        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.TotalRows.Should().Be(1);
    }

    [Fact]
    public async Task ImportFromExcelAsync_ShouldReportErrors_ForInvalidRows()
    {
        var (service, context) = await CreateServiceAsync();
        // Pre-seed an employee with the same number to trigger duplicate error
        context.Employees.Add(TestDataFactory.CreateEmployee(employeeNo: "EMP-999"));
        await context.SaveChangesAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Employees");
        ws.Cell(1, 1).Value = "EmployeeNo";
        ws.Cell(1, 2).Value = "FullName";
        ws.Cell(1, 3).Value = "Email";
        ws.Cell(2, 1).Value = "EMP-999";  // Duplicate employee number
        ws.Cell(2, 2).Value = "Test";
        ws.Cell(2, 3).Value = "test@test.com";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.ImportFromExcelAsync(stream);

        result.ErrorCount.Should().BeGreaterThan(0);
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task ExportToExcelAsync_ShouldReturnByteArray()
    {
        var (service, context) = await CreateServiceAsync();
        context.Employees.Add(TestDataFactory.CreateEmployee(employeeNo: "EMP-001"));
        await context.SaveChangesAsync();

        var result = await service.ExportToExcelAsync(null, null, null);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }
}
