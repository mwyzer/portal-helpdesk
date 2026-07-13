using AIHelpdesk.Domain.Entities;
using FluentAssertions;

namespace AIHelpdesk.Tests.Domain;

public class DepartmentTests
{
    [Fact]
    public void CreateDepartment_ShouldSetProperties()
    {
        var dept = TestDataFactory.CreateDepartment("Human Resources", "HR");

        dept.Name.Should().Be("Human Resources");
        dept.Code.Should().Be("HR");
        dept.IsActive.Should().BeTrue();
        dept.IsDeleted.Should().BeFalse();
        dept.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Position_ShouldBelongToDepartment()
    {
        var deptId = Guid.NewGuid();
        var position = TestDataFactory.CreatePosition(deptId, "Senior Engineer");

        position.DepartmentId.Should().Be(deptId);
        position.Name.Should().Be("Senior Engineer");
        position.IsActive.Should().BeTrue();
    }
}
