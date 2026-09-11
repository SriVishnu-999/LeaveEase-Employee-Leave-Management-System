using LeaveEase.Api.Data;
using LeaveEase.Api.DTOs;
using LeaveEase.Api.Models;
using LeaveEase.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveEase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext db, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyCollection<AdminUserDto>>> Users()
    {
        var users = await db.Users.AsNoTracking().Include(x => x.Manager).OrderBy(x => x.FullName).ToListAsync();
        var result = new List<AdminUserDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new AdminUserDto(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                user.Department,
                user.JobTitle,
                user.Manager?.FullName,
                roles.ToList(),
                user.IsActive));
        }
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> Summary()
    {
        var year = DateTime.UtcNow.Year;
        return Ok(new
        {
            employees = await db.Users.CountAsync(x => x.IsActive),
            pendingRequests = await db.LeaveRequests.CountAsync(x => x.Status == LeaveRequestStatus.Pending),
            approvedThisYear = await db.LeaveRequests.CountAsync(x => x.Status == LeaveRequestStatus.Approved && x.StartDate.Year == year),
            leaveTypes = await db.LeaveTypes.CountAsync(x => x.IsActive)
        });
    }

    [HttpPost("leave-types")]
    public async Task<ActionResult<LeaveTypeDto>> CreateLeaveType(UpsertLeaveTypeDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.LeaveTypes.AnyAsync(x => x.Code == code))
            return Conflict(new { message = "A leave type with this code already exists." });

        var type = new LeaveType
        {
            Name = dto.Name.Trim(),
            Code = code,
            Color = dto.Color.Trim(),
            DefaultDays = dto.DefaultDays,
            IsPaid = dto.IsPaid,
            IsActive = dto.IsActive
        };
        db.LeaveTypes.Add(type);
        await db.SaveChangesAsync();

        var year = DateTime.UtcNow.Year;
        var activeUserIds = await db.Users.Where(x => x.IsActive).Select(x => x.Id).ToListAsync();
        db.LeaveBalances.AddRange(activeUserIds.Select(userId => new LeaveBalance
        {
            UserId = userId,
            LeaveTypeId = type.Id,
            Year = year,
            Allocated = type.DefaultDays
        }));
        await db.SaveChangesAsync();

        return Ok(type.ToDto());
    }

    [HttpPut("leave-types/{id:int}")]
    public async Task<ActionResult<LeaveTypeDto>> UpdateLeaveType(int id, UpsertLeaveTypeDto dto)
    {
        var type = await db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (type is null) return NotFound();

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await db.LeaveTypes.AnyAsync(x => x.Id != id && x.Code == code))
            return Conflict(new { message = "Another leave type already uses this code." });

        type.Name = dto.Name.Trim();
        type.Code = code;
        type.Color = dto.Color.Trim();
        type.DefaultDays = dto.DefaultDays;
        type.IsPaid = dto.IsPaid;
        type.IsActive = dto.IsActive;
        await db.SaveChangesAsync();
        return Ok(type.ToDto());
    }

    [HttpPost("allocate-balance")]
    public async Task<ActionResult<LeaveBalanceDto>> Allocate(AllocateBalanceDto dto)
    {
        if (!await db.Users.AnyAsync(x => x.Id == dto.UserId))
            return BadRequest(new { message = "Employee not found." });
        if (!await db.LeaveTypes.AnyAsync(x => x.Id == dto.LeaveTypeId))
            return BadRequest(new { message = "Leave type not found." });

        var balance = await db.LeaveBalances
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.UserId == dto.UserId && x.LeaveTypeId == dto.LeaveTypeId && x.Year == dto.Year);

        if (balance is null)
        {
            balance = new LeaveBalance
            {
                UserId = dto.UserId,
                LeaveTypeId = dto.LeaveTypeId,
                Year = dto.Year,
                Allocated = dto.Allocated
            };
            db.LeaveBalances.Add(balance);
            await db.SaveChangesAsync();
            await db.Entry(balance).Reference(x => x.LeaveType).LoadAsync();
        }
        else
        {
            if (dto.Allocated < balance.Used + balance.Pending)
                return BadRequest(new { message = "Allocation cannot be lower than days already used plus pending." });
            balance.Allocated = dto.Allocated;
            await db.SaveChangesAsync();
        }

        return Ok(balance.ToDto());
    }
}
