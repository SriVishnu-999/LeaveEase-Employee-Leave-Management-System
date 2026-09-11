using System.ComponentModel.DataAnnotations;

namespace LeaveEase.Api.Models;

public class LeaveRequestHistory
{
    public int Id { get; set; }
    public int LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;
    public LeaveRequestStatus? FromStatus { get; set; }
    public LeaveRequestStatus ToStatus { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public string ChangedById { get; set; } = string.Empty;
    public AppUser ChangedBy { get; set; } = null!;
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
