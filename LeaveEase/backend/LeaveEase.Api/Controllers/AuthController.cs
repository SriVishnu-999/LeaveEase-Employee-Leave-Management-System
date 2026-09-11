using LeaveEase.Api.Data;
using LeaveEase.Api.DTOs;
using LeaveEase.Api.Models;
using LeaveEase.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LeaveEase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<AppUser> userManager,
    AppDbContext db,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        var roles = await userManager.GetRolesAsync(user);
        var managerName = user.ManagerId is null
            ? null
            : await db.Users.Where(x => x.Id == user.ManagerId).Select(x => x.FullName).FirstOrDefaultAsync();
        var (token, expiresAt) = jwtTokenService.CreateToken(user, roles);

        return Ok(new LoginResponse(token, expiresAt, ToUserDto(user, roles, managerName)));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var user = await db.Users.Include(x => x.Manager).FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null)
            return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(ToUserDto(user, roles, user.Manager?.FullName));
    }

    private static UserDto ToUserDto(AppUser user, IEnumerable<string> roles, string? managerName) => new(
        user.Id,
        user.FullName,
        user.Email ?? string.Empty,
        user.Department,
        user.JobTitle,
        user.ManagerId,
        managerName,
        roles.ToList());
}
