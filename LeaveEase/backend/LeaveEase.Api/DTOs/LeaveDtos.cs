using System.ComponentModel.DataAnnotations;
using LeaveEase.Api.Models;

namespace LeaveEase.Api.DTOs;

public record LeaveTypeDto(
    int Id,
    string Name,
    string Code,
    string Color,
    decimal DefaultDays,
    bool IsPaid,
    bool IsActive);

public record LeaveBalanceDto(
    int Id,
    int LeaveTypeId,
    string LeaveType,
    string Code,
    string Color,
    int Year,
    decimal Allocated,
    decimal Used,
    decimal Pending,
    decimal Available);

public class CreateLeaveRequestDto
{
    [Required]
    public int LeaveTypeId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required, StringLength(1000, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

public class DecisionRequestDto
{
    [Required]
    public LeaveRequestStatus Status { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

public record LeaveRequestHistoryDto(
    int Id,
    string? FromStatus,
    string ToStatus,
    string? Comment,
    string ChangedBy,
    DateTime ChangedAtUtc);

public record LeaveRequestDto(
    int Id,
    string EmployeeId,
    string EmployeeName,
    string EmployeeEmail,
    string Department,
    int LeaveTypeId,
    string LeaveType,
    string LeaveTypeCode,
    string LeaveTypeColor,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    string Reason,
    string Status,
    string? ManagerComment,
    string? ReviewedBy,
    DateTime? ReviewedAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<LeaveRequestHistoryDto>? History);
