using LeaveEase.Api.Data;
using LeaveEase.Api.DTOs;
using LeaveEase.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveEase.Api.Controllers;

[ApiController]
[Route("api/leave-types")]
[Authorize]
public class LeaveTypesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<LeaveTypeDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = db.LeaveTypes.AsNoTracking();
        if (!includeInactive || !User.IsInRole("Admin"))
            query = query.Where(x => x.IsActive);

        var items = await query.OrderBy(x => x.Name).ToListAsync();
        return Ok(items.Select(x => x.ToDto()).ToList());
    }
}
