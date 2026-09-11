using Microsoft.AspNetCore.Identity;

namespace LeaveEase.Api.Models;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public AppUser? Manager { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<AppUser> DirectReports { get; set; } = new List<AppUser>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
}
