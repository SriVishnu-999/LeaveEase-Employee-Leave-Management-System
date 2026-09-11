using LeaveEase.Api.DTOs;
using LeaveEase.Api.Models;

namespace LeaveEase.Api.Services;

public static class DtoMapper
{
    public static LeaveTypeDto ToDto(this LeaveType type) => new(
        type.Id,
        type.Name,
        type.Code,
        type.Color,
        type.DefaultDays,
        type.IsPaid,
        type.IsActive);

    public static LeaveBalanceDto ToDto(this LeaveBalance balance) => new(
        balance.Id,
        balance.LeaveTypeId,
        balance.LeaveType.Name,
        balance.LeaveType.Code,
        balance.LeaveType.Color,
        balance.Year,
        balance.Allocated,
        balance.Used,
        balance.Pending,
        balance.Available);

    public static LeaveRequestDto ToDto(this LeaveRequest request, bool includeHistory = false) => new(
        request.Id,
        request.EmployeeId,
        request.Employee.FullName,
        request.Employee.Email ?? string.Empty,
        request.Employee.Department,
        request.LeaveTypeId,
        request.LeaveType.Name,
        request.LeaveType.Code,
        request.LeaveType.Color,
        request.StartDate,
        request.EndDate,
        request.TotalDays,
        request.Reason,
        request.Status.ToString(),
        request.ManagerComment,
        request.ReviewedBy?.FullName,
        request.ReviewedAtUtc,
        request.CreatedAtUtc,
        includeHistory
            ? request.History
                .OrderBy(x => x.ChangedAtUtc)
                .Select(x => new LeaveRequestHistoryDto(
                    x.Id,
                    x.FromStatus?.ToString(),
                    x.ToStatus.ToString(),
                    x.Comment,
                    x.ChangedBy.FullName,
                    x.ChangedAtUtc))
                .ToList()
            : null);
}
