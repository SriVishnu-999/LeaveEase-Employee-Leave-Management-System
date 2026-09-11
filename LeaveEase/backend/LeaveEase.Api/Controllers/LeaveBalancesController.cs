using LeaveEase.Api.Data;
using LeaveEase.Api.DTOs;
using LeaveEase.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LeaveEase.Api.Controllers;

[ApiController]
[Route("api/leave-balances")]
[Authorize]
public class LeaveBalancesController(AppDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyCollection<LeaveBalanceDto>>> MyBalances([FromQuery] int? year = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var selectedYear = year ?? DateTime.UtcNow.Year;
        var balances = await db.LeaveBalances
            .AsNoTracking()
            .Include(x => x.LeaveType)
            .Where(x => x.UserId == userId && x.Year == selectedYear && x.LeaveType.IsActive)
            .OrderBy(x => x.LeaveType.Name)
            .ToListAsync();

        return Ok(balances.Select(x => x.ToDto()).ToList());
    }
}
