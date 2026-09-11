using System.ComponentModel.DataAnnotations;

namespace LeaveEase.Api.Models;

public class LeaveRequest
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public AppUser Employee { get; set; } = null!;
    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; }

    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    [MaxLength(1000)]
    public string? ManagerComment { get; set; }

    public string? ReviewedById { get; set; }
    public AppUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<LeaveRequestHistory> History { get; set; } = new List<LeaveRequestHistory>();
}
