using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AIHelpdesk.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // ── Seed Roles ──
        var roles = new[]
        {
            ("Super Admin", "Full system access"),
            ("HRD", "HR department access"),
            ("Secretary", "Secretary module access"),
            ("Manager", "Manager-level access"),
            ("Employee", "Basic employee access"),
        };

        foreach (var (name, description) in roles)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = name,
                    Description = description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        // ── Seed Departments ──
        var departments = new[]
        {
            ("IT Department", "IT"),
            ("Human Resources", "HR"),
            ("Finance", "FIN"),
            ("Operations", "OPS"),
            ("Marketing", "MKT"),
        };

        foreach (var (name, code) in departments)
        {
            if (!await context.Departments.AnyAsync(d => d.Code == code))
            {
                context.Departments.Add(new Department { Name = name, Code = code, IsActive = true });
            }
        }
        await context.SaveChangesAsync();

        // ── Seed Positions ──
        var itDept   = await context.Departments.FirstAsync(d => d.Code == "IT");
        var hrDept   = await context.Departments.FirstAsync(d => d.Code == "HR");
        var finDept  = await context.Departments.FirstAsync(d => d.Code == "FIN");
        var opsDept  = await context.Departments.FirstAsync(d => d.Code == "OPS");
        var mktDept  = await context.Departments.FirstAsync(d => d.Code == "MKT");

        var positions = new (string Name, Department Dept)[]
        {
            ("Software Developer", itDept),
            ("System Administrator", itDept),
            ("IT Support", itDept),
            ("HR Specialist", hrDept),
            ("HR Manager", hrDept),
            ("Recruiter", hrDept),
            ("Accountant", finDept),
            ("Finance Manager", finDept),
            ("Auditor", finDept),
            ("Operations Staff", opsDept),
            ("Operations Manager", opsDept),
            ("Logistics Coordinator", opsDept),
            ("Marketing Specialist", mktDept),
            ("Marketing Manager", mktDept),
            ("Content Writer", mktDept),
        };

        foreach (var (name, dept) in positions)
        {
            if (!await context.Positions.AnyAsync(p => p.Name == name && p.DepartmentId == dept.Id))
            {
                context.Positions.Add(new Position { Name = name, DepartmentId = dept.Id, IsActive = true });
            }
        }
        await context.SaveChangesAsync();

        // ── Seed Users (10 per role = 50 total) ──
        var userDefinitions = new List<(string FullName, string Email, string Password, string Role)>();

        // Super Admin (10)
        userDefinitions.Add(("Super Admin",    "admin@aihelpdesk.com",      "Admin@123",      "Super Admin"));
        for (int i = 2; i <= 10; i++)
            userDefinitions.Add(($"Super Admin {i}", $"admin{i}@aihelpdesk.com", $"Admin@{i}123", "Super Admin"));

        // HRD (10)
        userDefinitions.Add(("HRD User",       "hrd@aihelpdesk.com",        "Hrd@12345",      "HRD"));
        for (int i = 2; i <= 10; i++)
            userDefinitions.Add(($"HRD User {i}",    $"hrd{i}@aihelpdesk.com",    $"Hrd@{i}12345",   "HRD"));

        // Secretary (10)
        userDefinitions.Add(("Secretary",      "secretary@aihelpdesk.com",  "Secretary@123",  "Secretary"));
        for (int i = 2; i <= 10; i++)
            userDefinitions.Add(($"Secretary {i}",   $"secretary{i}@aihelpdesk.com", $"Secretary@{i}123", "Secretary"));

        // Manager (10)
        userDefinitions.Add(("Manager",        "manager@aihelpdesk.com",    "Manager@123",    "Manager"));
        for (int i = 2; i <= 10; i++)
            userDefinitions.Add(($"Manager {i}",     $"manager{i}@aihelpdesk.com",   $"Manager@{i}123",  "Manager"));

        // Employee (10)
        userDefinitions.Add(("Employee",       "employee@aihelpdesk.com",   "Employee@123",   "Employee"));
        for (int i = 2; i <= 10; i++)
            userDefinitions.Add(($"Employee {i}",    $"employee{i}@aihelpdesk.com",  $"Employee@{i}123", "Employee"));

        foreach (var (fullName, email, password, role) in userDefinitions)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null) continue;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"Failed to create user {email}: {errors}");
            }
        }

        // ── Seed Employees (linked to users) ──
        var positionList = await context.Positions.ToListAsync();
        var deptList     = await context.Departments.ToListAsync();

        // Map role → typical department/position
        var (devPos, adminPos, supportPos)      = (positionList.First(p => p.Name == "Software Developer"),     positionList.First(p => p.Name == "System Administrator"), positionList.First(p => p.Name == "IT Support"));
        var (hrSpecPos, hrMgrPos, recruiterPos)  = (positionList.First(p => p.Name == "HR Specialist"),         positionList.First(p => p.Name == "HR Manager"),          positionList.First(p => p.Name == "Recruiter"));
        var (acctPos, finMgrPos, auditorPos)     = (positionList.First(p => p.Name == "Accountant"),            positionList.First(p => p.Name == "Finance Manager"),     positionList.First(p => p.Name == "Auditor"));
        var (opsStaffPos, opsMgrPos, logPos)     = (positionList.First(p => p.Name == "Operations Staff"),      positionList.First(p => p.Name == "Operations Manager"),  positionList.First(p => p.Name == "Logistics Coordinator"));
        var (mktSpecPos, mktMgrPos, writerPos)   = (positionList.First(p => p.Name == "Marketing Specialist"),  positionList.First(p => p.Name == "Marketing Manager"),   positionList.First(p => p.Name == "Content Writer"));

        async Task CreateEmployee(string email, string employeeNo, string role, Guid? managerEmployeeId = null)
        {
            if (await context.Employees.AnyAsync(e => e.Email == email)) return;

            var user = await userManager.FindByEmailAsync(email);
            if (user == null) return;

            var (dept, pos) = role switch
            {
                "Super Admin" => (itDept,   adminPos),
                "HRD"         => (hrDept,   hrMgrPos),
                "Secretary"   => (opsDept,  opsStaffPos),
                "Manager"     => (finDept,  finMgrPos),
                "Employee"    => (mktDept,  mktSpecPos),
                _             => (opsDept,  opsStaffPos),
            };

            context.Employees.Add(new Employee
            {
                EmployeeNo = employeeNo,
                FullName = user.FullName,
                Email = email,
                Phone = $"+62-{Random.Shared.Next(800, 900)}-{Random.Shared.Next(1000, 9999)}-{Random.Shared.Next(1000, 9999)}",
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 1500))),
                DepartmentId = dept.Id,
                PositionId = pos.Id,
                ManagerId = managerEmployeeId,
                EmploymentStatus = EmploymentStatus.Active,
                WorkLocation = "Head Office",
                UserId = user.Id,
            });
        }

        // Create employees in order so managers exist before subordinates
        // Super Admins (10) — emp 1-10
        for (int i = 1; i <= 10; i++)
        {
            var email = i == 1 ? "admin@aihelpdesk.com" : $"admin{i}@aihelpdesk.com";
            await CreateEmployee(email, $"EMP-{i:D3}", "Super Admin");
        }
        await context.SaveChangesAsync();

        // HRDs (10) — emp 11-20, report to admin #1
        var adminEmp = await context.Employees.FirstAsync(e => e.EmployeeNo == "EMP-001");
        for (int i = 1; i <= 10; i++)
        {
            var email = i == 1 ? "hrd@aihelpdesk.com" : $"hrd{i}@aihelpdesk.com";
            await CreateEmployee(email, $"EMP-{10 + i:D3}", "HRD", adminEmp.Id);
        }
        await context.SaveChangesAsync();

        // Secretaries (10) — emp 21-30, report to admin #1
        for (int i = 1; i <= 10; i++)
        {
            var email = i == 1 ? "secretary@aihelpdesk.com" : $"secretary{i}@aihelpdesk.com";
            await CreateEmployee(email, $"EMP-{20 + i:D3}", "Secretary", adminEmp.Id);
        }
        await context.SaveChangesAsync();

        // Managers (10) — emp 31-40, report to HRD #1
        var hrdEmp = await context.Employees.FirstAsync(e => e.EmployeeNo == "EMP-011");
        for (int i = 1; i <= 10; i++)
        {
            var email = i == 1 ? "manager@aihelpdesk.com" : $"manager{i}@aihelpdesk.com";
            await CreateEmployee(email, $"EMP-{30 + i:D3}", "Manager", hrdEmp.Id);
        }
        await context.SaveChangesAsync();

        // Employees (10) — emp 41-50, report to Managers
        for (int i = 1; i <= 10; i++)
        {
            var email = i == 1 ? "employee@aihelpdesk.com" : $"employee{i}@aihelpdesk.com";
            var mgrEmp = await context.Employees.FirstAsync(e => e.EmployeeNo == $"EMP-{30 + ((i-1) % 10 + 1):D3}");
            await CreateEmployee(email, $"EMP-{40 + i:D3}", "Employee", mgrEmp.Id);
        }
        await context.SaveChangesAsync();

        // ── Seed Leave Types ──
        var leaveTypes = new[]
        {
            ("Annual Leave", "AL", 12, true, 0, false, false),
            ("Sick Leave", "SL", 14, true, 0, true, true),
            ("Special Leave", "SPL", 5, true, 0, false, false),
            ("Maternity Leave", "ML", 90, true, 12, false, false),
            ("Paternity Leave", "PL", 5, true, 6, false, false),
            ("Lateness", "LT", 0, false, 0, false, false),
            ("Early Leave", "EL", 0, false, 0, false, false),
            ("Work From Home", "WFH", 0, true, 0, false, false),
        };

        foreach (var (name, code, daysPerYear, isPaid, minServiceMonths, requiresAttachment, skipManagerApproval) in leaveTypes)
        {
            if (!await context.LeaveTypes.AnyAsync(lt => lt.Code == code))
            {
                context.LeaveTypes.Add(new LeaveType
                {
                    Name = name,
                    Code = code,
                    DaysPerYear = daysPerYear,
                    IsPaid = isPaid,
                    MinServiceMonths = minServiceMonths,
                    RequiresAttachment = requiresAttachment,
                    SkipManagerApproval = skipManagerApproval,
                    IsActive = true
                });
            }
        }
        await context.SaveChangesAsync();

        // ── Seed Leave Balances (for all employees, all leave types with days > 0) ──
        var allEmployees    = await context.Employees.ToListAsync();
        var activeLeaveTypes = await context.LeaveTypes.Where(lt => lt.DaysPerYear > 0 && lt.IsActive).ToListAsync();
        var currentYear     = DateTime.UtcNow.Year;

        foreach (var emp in allEmployees)
        {
            foreach (var lt in activeLeaveTypes)
            {
                var exists = await context.LeaveBalances.AnyAsync(lb =>
                    lb.EmployeeId == emp.Id && lb.LeaveTypeId == lt.Id && lb.Year == currentYear);
                if (!exists)
                {
                    context.LeaveBalances.Add(new LeaveBalance
                    {
                        EmployeeId = emp.Id,
                        LeaveTypeId = lt.Id,
                        TotalDays = lt.DaysPerYear,
                        UsedDays = 0,
                        PendingDays = 0,
                        Year = currentYear,
                    });
                }
            }
        }
        await context.SaveChangesAsync();
    }
}
