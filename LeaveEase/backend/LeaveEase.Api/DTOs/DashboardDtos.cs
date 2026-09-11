namespace LeaveEase.Api.DTOs;

public record DashboardSummaryDto(
    string GreetingName,
    int CurrentYear,
    decimal TotalAvailableDays,
    decimal TotalUsedDays,
    decimal TotalPendingDays,
    int PendingRequests,
    int ApprovedRequests,
    int RejectedRequests,
    int TeamPendingApprovals,
    IReadOnlyCollection<LeaveBalanceDto> Balances,
    IReadOnlyCollection<LeaveRequestDto> RecentRequests);
