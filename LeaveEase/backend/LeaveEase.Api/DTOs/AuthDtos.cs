using System.ComponentModel.DataAnnotations;

namespace LeaveEase.Api.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record UserDto(
    string Id,
    string FullName,
    string Email,
    string Department,
    string JobTitle,
    string? ManagerId,
    string? ManagerName,
    IReadOnlyCollection<string> Roles);

public record LoginResponse(string Token, DateTime ExpiresAtUtc, UserDto User);
