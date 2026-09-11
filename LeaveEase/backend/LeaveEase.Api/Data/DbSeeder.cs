using LeaveEase.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeaveEase.Api.Data;

public static class DbSeeder
{
    private const string DemoPassword = "Pass@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.EnsureCreatedAsync();

        foreach (var role in new[] { "Employee", "Manager", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var admin = await EnsureUserAsync(userManager, "admin@leaveease.local", "Aarav Mehta", "People Operations", "HR Administrator", null, "Admin");
        var manager = await EnsureUserAsync(userManager, "manager@leaveease.local", "Neha Sharma", "Engineering", "Engineering Manager", null, "Manager");
        var employee = await EnsureUserAsync(userManager, "employee@leaveease.local", "Rahul Verma", "Engineering", "Software Engineer", manager.Id, "Employee");
        var priya = await EnsureUserAsync(userManager, "priya@leaveease.local", "Priya Nair", "Engineering", "QA Engineer", manager.Id, "Employee");

        if (!await db.LeaveTypes.AnyAsync())
        {
            db.LeaveTypes.AddRange(
                new LeaveType { Name = "Annual Leave", Code = "AL", DefaultDays = 18, Color = "#4f46e5", IsPaid = true },
                new LeaveType { Name = "Sick Leave", Code = "SL", DefaultDays = 10, Color = "#0f766e", IsPaid = true },
                new LeaveType { Name = "Casual Leave", Code = "CL", DefaultDays = 8, Color = "#c2410c", IsPaid = true },
                new LeaveType { Name = "Unpaid Leave", Code = "UL", DefaultDays = 30, Color = "#64748b", IsPaid = false }
            );
            await db.SaveChangesAsync();
        }

        var users = new[] { admin, manager, employee, priya };
        var types = await db.LeaveTypes.ToListAsync();
        var year = DateTime.UtcNow.Year;

        foreach (var user in users)
        {
            foreach (var type in types)
            {
                if (!await db.LeaveBalances.AnyAsync(x => x.UserId == user.Id && x.LeaveTypeId == type.Id && x.Year == year))
                {
                    db.LeaveBalances.Add(new LeaveBalance
                    {
                        UserId = user.Id,
                        LeaveTypeId = type.Id,
                        Year = year,
                        Allocated = type.DefaultDays,
                        Used = 0,
                        Pending = 0
                    });
                }
            }
        }

        await db.SaveChangesAsync();

        if (!await db.LeaveRequests.AnyAsync())
        {
            var annual = types.First(x => x.Code == "AL");
            var sick = types.First(x => x.Code == "SL");
            var today = DateOnly.FromDateTime(DateTime.Today);
            var nextMonday = NextWeekday(today.AddDays(3));

            var pending = new LeaveRequest
            {
                EmployeeId = employee.Id,
                LeaveTypeId = annual.Id,
                StartDate = nextMonday,
                EndDate = nextMonday.AddDays(1),
                TotalDays = 2,
                Reason = "Family function out of town",
                Status = LeaveRequestStatus.Pending
            };
            db.LeaveRequests.Add(pending);

            var balance = await db.LeaveBalances.FirstAsync(x => x.UserId == employee.Id && x.LeaveTypeId == annual.Id && x.Year == year);
            balance.Pending += 2;

            var previousDay = PreviousWeekday(today.AddDays(-5));
            var approved = new LeaveRequest
            {
                EmployeeId = priya.Id,
                LeaveTypeId = sick.Id,
                StartDate = previousDay,
                EndDate = previousDay,
                TotalDays = 1,
                Reason = "Medical rest",
                Status = LeaveRequestStatus.Approved,
                ManagerComment = "Approved. Take care.",
                ReviewedById = manager.Id,
                ReviewedAtUtc = DateTime.UtcNow.AddDays(-5),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-6),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-5)
            };
            db.LeaveRequests.Add(approved);

            var priyaBalance = await db.LeaveBalances.FirstAsync(x => x.UserId == priya.Id && x.LeaveTypeId == sick.Id && x.Year == year);
            priyaBalance.Used += 1;

            await db.SaveChangesAsync();

            db.LeaveRequestHistories.AddRange(
                new LeaveRequestHistory
                {
                    LeaveRequestId = pending.Id,
                    ToStatus = LeaveRequestStatus.Pending,
                    ChangedById = employee.Id,
                    Comment = "Leave request submitted"
                },
                new LeaveRequestHistory
                {
                    LeaveRequestId = approved.Id,
                    ToStatus = LeaveRequestStatus.Pending,
                    ChangedById = priya.Id,
                    Comment = "Leave request submitted",
                    ChangedAtUtc = approved.CreatedAtUtc
                },
                new LeaveRequestHistory
                {
                    LeaveRequestId = approved.Id,
                    FromStatus = LeaveRequestStatus.Pending,
                    ToStatus = LeaveRequestStatus.Approved,
                    ChangedById = manager.Id,
                    Comment = approved.ManagerComment,
                    ChangedAtUtc = approved.UpdatedAtUtc
                }
            );
            await db.SaveChangesAsync();
        }
    }

    private static async Task<AppUser> EnsureUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string fullName,
        string department,
        string jobTitle,
        string? managerId,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Department = department,
                JobTitle = jobTitle,
                ManagerId = managerId,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to seed {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        else if (user.ManagerId != managerId && managerId is not null)
        {
            user.ManagerId = managerId;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        return user;
    }

    private static DateOnly NextWeekday(DateOnly date)
    {
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(1);
        return date;
    }

    private static DateOnly PreviousWeekday(DateOnly date)
    {
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(-1);
        return date;
    }
}
