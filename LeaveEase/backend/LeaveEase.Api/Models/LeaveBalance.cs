using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaveEase.Api.Models;

public class LeaveBalance
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public int Year { get; set; }
    public decimal Allocated { get; set; }
    public decimal Used { get; set; }
    public decimal Pending { get; set; }

    [NotMapped]
    public decimal Available => Allocated - Used - Pending;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
