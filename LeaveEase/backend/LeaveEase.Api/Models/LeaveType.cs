using System.ComponentModel.DataAnnotations;

namespace LeaveEase.Api.Models;

public class LeaveType
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Color { get; set; } = "#4f46e5";

    public decimal DefaultDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
}
