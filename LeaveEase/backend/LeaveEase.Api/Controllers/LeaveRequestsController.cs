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
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController(
    AppDbContext db,
    ILeavePolicyService leavePolicy,
    IEmailService emailService) : ControllerBase
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyCollection<LeaveRequestDto>>> Mine([FromQuery] int? limit = null)
    {
        if (UserId is null) return Unauthorized();

        var query = BaseQuery().Where(x => x.EmployeeId == UserId).OrderByDescending(x => x.CreatedAtUtc);
        var items = limit is > 0 ? await query.Take(limit.Value).ToListAsync() : await query.ToListAsync();
        return Ok(items.Select(x => x.ToDto()).ToList());
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<IReadOnlyCollection<LeaveRequestDto>>> Pending()
    {
        if (UserId is null) return Unauthorized();

        var query = BaseQuery().Where(x => x.Status == LeaveRequestStatus.Pending);
        if (!User.IsInRole("Admin"))
            query = query.Where(x => x.Employee.ManagerId == UserId);

        var items = await query.OrderBy(x => x.StartDate).ThenBy(x => x.CreatedAtUtc).ToListAsync();
        return Ok(items.Select(x => x.ToDto()).ToList());
    }

    [HttpGet("team")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<IReadOnlyCollection<LeaveRequestDto>>> Team([FromQuery] string? status = null)
    {
        if (UserId is null) return Unauthorized();

        var query = BaseQuery();
        if (!User.IsInRole("Admin"))
            query = query.Where(x => x.Employee.ManagerId == UserId);

        if (Enum.TryParse<LeaveRequestStatus>(status, true, out var parsed))
            query = query.Where(x => x.Status == parsed);

        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync();
        return Ok(items.Select(x => x.ToDto()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveRequestDto>> GetById(int id)
    {
        if (UserId is null) return Unauthorized();

        var request = await db.LeaveRequests
            .AsNoTracking()
            .Include(x => x.Employee).ThenInclude(x => x.Manager)
            .Include(x => x.LeaveType)
            .Include(x => x.ReviewedBy)
            .Include(x => x.History).ThenInclude(x => x.ChangedBy)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null) return NotFound();
        if (!CanView(request)) return Forbid();

        return Ok(request.ToDto(includeHistory: true));
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Create(CreateLeaveRequestDto dto, CancellationToken cancellationToken)
    {
        if (UserId is null) return Unauthorized();
        if (dto.EndDate < dto.StartDate)
            return BadRequest(new { message = "End date cannot be before start date." });
        if (dto.StartDate < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new { message = "Leave cannot start in the past." });
        if (dto.StartDate.Year != dto.EndDate.Year)
            return BadRequest(new { message = "A leave request cannot span multiple calendar years in this version." });

        var type = await db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == dto.LeaveTypeId && x.IsActive, cancellationToken);
        if (type is null)
            return BadRequest(new { message = "Selected leave type is unavailable." });

        var totalDays = leavePolicy.CalculateWorkingDays(dto.StartDate, dto.EndDate);
        if (totalDays <= 0)
            return BadRequest(new { message = "The selected range contains no working days." });

        var overlap = await db.LeaveRequests.AnyAsync(x =>
            x.EmployeeId == UserId &&
            (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.Approved) &&
            x.StartDate <= dto.EndDate && x.EndDate >= dto.StartDate,
            cancellationToken);
        if (overlap)
            return Conflict(new { message = "You already have a pending or approved leave request overlapping these dates." });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var balance = await db.LeaveBalances
                .Include(x => x.LeaveType)
                .FirstOrDefaultAsync(x => x.UserId == UserId && x.LeaveTypeId == dto.LeaveTypeId && x.Year == dto.StartDate.Year, cancellationToken);

            if (balance is null)
                return BadRequest(new { message = "No leave balance is allocated for this leave type and year." });
            if (balance.Available < totalDays)
                return BadRequest(new { message = $"Insufficient balance. Available: {balance.Available:0.##} days; requested: {totalDays:0.##} days." });

            var request = new LeaveRequest
            {
                EmployeeId = UserId,
                LeaveTypeId = dto.LeaveTypeId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TotalDays = totalDays,
                Reason = dto.Reason.Trim(),
                Status = LeaveRequestStatus.Pending
            };

            db.LeaveRequests.Add(request);
            balance.Pending += totalDays;
            await db.SaveChangesAsync(cancellationToken);

            db.LeaveRequestHistories.Add(new LeaveRequestHistory
            {
                LeaveRequestId = request.Id,
                ToStatus = LeaveRequestStatus.Pending,
                Comment = "Leave request submitted",
                ChangedById = UserId
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var hydrated = await BaseQuery().Include(x => x.Employee.Manager).FirstAsync(x => x.Id == request.Id, cancellationToken);
            if (hydrated.Employee.Manager is not null)
                await emailService.SendLeaveSubmittedAsync(hydrated, hydrated.Employee.Manager, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = request.Id }, hydrated.ToDto());
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Your leave balance changed while the request was being submitted. Refresh and try again." });
        }
    }

    [HttpPut("{id:int}/decision")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<LeaveRequestDto>> Decide(int id, DecisionRequestDto dto, CancellationToken cancellationToken)
    {
        if (UserId is null) return Unauthorized();
        if (dto.Status is not (LeaveRequestStatus.Approved or LeaveRequestStatus.Rejected))
            return BadRequest(new { message = "Decision status must be Approved or Rejected." });
        if (dto.Status == LeaveRequestStatus.Rejected && string.IsNullOrWhiteSpace(dto.Comment))
            return BadRequest(new { message = "A comment is required when rejecting leave." });

        var request = await db.LeaveRequests
            .Include(x => x.Employee).ThenInclude(x => x.Manager)
            .Include(x => x.LeaveType)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null) return NotFound();
        if (request.Status != LeaveRequestStatus.Pending)
            return Conflict(new { message = "Only pending requests can be reviewed." });
        if (!User.IsInRole("Admin") && request.Employee.ManagerId != UserId)
            return Forbid();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var balance = await db.LeaveBalances.FirstOrDefaultAsync(x =>
                x.UserId == request.EmployeeId &&
                x.LeaveTypeId == request.LeaveTypeId &&
                x.Year == request.StartDate.Year,
                cancellationToken);
            if (balance is null)
                return Conflict(new { message = "The employee's balance record is missing." });

            var previous = request.Status;
            balance.Pending = Math.Max(0, balance.Pending - request.TotalDays);
            if (dto.Status == LeaveRequestStatus.Approved)
                balance.Used += request.TotalDays;

            request.Status = dto.Status;
            request.ManagerComment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim();
            request.ReviewedById = UserId;
            request.ReviewedAtUtc = DateTime.UtcNow;
            request.UpdatedAtUtc = DateTime.UtcNow;

            db.LeaveRequestHistories.Add(new LeaveRequestHistory
            {
                LeaveRequestId = request.Id,
                FromStatus = previous,
                ToStatus = dto.Status,
                Comment = request.ManagerComment,
                ChangedById = UserId
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            request.ReviewedBy = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == UserId, cancellationToken);
            await emailService.SendLeaveStatusChangedAsync(request, cancellationToken);
            return Ok(request.ToDto());
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "This request or balance was modified by another user. Refresh and try again." });
        }
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<LeaveRequestDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        if (UserId is null) return Unauthorized();

        var request = await db.LeaveRequests
            .Include(x => x.Employee)
            .Include(x => x.LeaveType)
            .Include(x => x.ReviewedBy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (request is null) return NotFound();
        if (request.EmployeeId != UserId) return Forbid();
        if (request.Status != LeaveRequestStatus.Pending)
            return Conflict(new { message = "Only pending requests can be cancelled." });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var balance = await db.LeaveBalances.FirstOrDefaultAsync(x =>
                x.UserId == UserId && x.LeaveTypeId == request.LeaveTypeId && x.Year == request.StartDate.Year,
                cancellationToken);
            if (balance is not null)
                balance.Pending = Math.Max(0, balance.Pending - request.TotalDays);

            request.Status = LeaveRequestStatus.Cancelled;
            request.UpdatedAtUtc = DateTime.UtcNow;
            db.LeaveRequestHistories.Add(new LeaveRequestHistory
            {
                LeaveRequestId = request.Id,
                FromStatus = LeaveRequestStatus.Pending,
                ToStatus = LeaveRequestStatus.Cancelled,
                Comment = "Cancelled by employee",
                ChangedById = UserId
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(request.ToDto());
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Your balance changed while cancelling. Refresh and try again." });
        }
    }

    private IQueryable<LeaveRequest> BaseQuery() => db.LeaveRequests
        .AsNoTracking()
        .Include(x => x.Employee)
        .Include(x => x.LeaveType)
        .Include(x => x.ReviewedBy);

    private bool CanView(LeaveRequest request)
    {
        if (UserId == request.EmployeeId || User.IsInRole("Admin")) return true;
        return User.IsInRole("Manager") && request.Employee.ManagerId == UserId;
    }
}
