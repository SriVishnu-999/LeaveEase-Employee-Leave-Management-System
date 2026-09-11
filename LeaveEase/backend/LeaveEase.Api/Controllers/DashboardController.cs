using LeaveEase.Api.Data;
using LeaveEase.Api.DTOs;
using LeaveEase.Api.Models;
using LeaveEase.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LeaveEase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Summary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null)
            return Unauthorized();

        var year = DateTime.UtcNow.Year;
        var balances = await db.LeaveBalances
            .AsNoTracking()
            .Include(x => x.LeaveType)
            .Where(x => x.UserId == userId && x.Year == year && x.LeaveType.IsActive)
            .OrderBy(x => x.LeaveType.Name)
            .ToListAsync();

        var requests = db.LeaveRequests
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .Include(x => x.ReviewedBy)
            .Where(x => x.EmployeeId == userId);

        var pendingCount = await requests.CountAsync(x => x.Status == LeaveRequestStatus.Pending);
        var approvedCount = await requests.CountAsync(x => x.Status == LeaveRequestStatus.Approved);
        var rejectedCount = await requests.CountAsync(x => x.Status == LeaveRequestStatus.Rejected);
        var recent = await requests.OrderByDescending(x => x.CreatedAtUtc).Take(5).ToListAsync();

        var teamPending = 0;
        if (User.IsInRole("Admin"))
        {
            teamPending = await db.LeaveRequests.CountAsync(x => x.Status == LeaveRequestStatus.Pending);
        }
        else if (User.IsInRole("Manager"))
        {
            teamPending = await db.LeaveRequests
                .Where(x => x.Employee.ManagerId == userId && x.Status == LeaveRequestStatus.Pending)
                .CountAsync();
        }

        return Ok(new DashboardSummaryDto(
            user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? user.FullName,
            year,
            balances.Sum(x => x.Available),
            balances.Sum(x => x.Used),
            balances.Sum(x => x.Pending),
            pendingCount,
            approvedCount,
            rejectedCount,
            teamPending,
            balances.Select(x => x.ToDto()).ToList(),
            recent.Select(x => x.ToDto()).ToList()));
    }
}
