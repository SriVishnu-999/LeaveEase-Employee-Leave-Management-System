using System.ComponentModel.DataAnnotations;

namespace LeaveEase.Api.DTOs;

public class UpsertLeaveTypeDto
{
    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Color { get; set; } = "#4f46e5";

    [Range(0, 365)]
    public decimal DefaultDays { get; set; }

    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class AllocateBalanceDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int LeaveTypeId { get; set; }

    [Range(2000, 2200)]
    public int Year { get; set; }

    [Range(0, 365)]
    public decimal Allocated { get; set; }
}

public record AdminUserDto(
    string Id,
    string FullName,
    string Email,
    string Department,
    string JobTitle,
    string? ManagerName,
    IReadOnlyCollection<string> Roles,
    bool IsActive);
