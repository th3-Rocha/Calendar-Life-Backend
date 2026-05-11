using CalendarLife.Application.DTOs.Users;
using CalendarLife.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CalendarLife.Api.Controllers;

[ApiController]
[Route("users/{userId:guid}")]
[EnableRateLimiting("default")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users)
    {
        _users = users;
    }

    /// <summary>
    /// Gets the user by id, creating it automatically if it doesn't exist.
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<UserResponse>> UpsertUserAsync(
        [FromRoute] Guid userId,
        [FromBody] UpsertUserRequest? request,
        CancellationToken ct)
    {
        var user = await _users.GetOrCreateUserAsync(userId, request, ct);
        return Ok(user);
    }

    [HttpGet]
    public async Task<ActionResult<UserResponse>> GetUserAsync(
        [FromRoute] Guid userId,
        CancellationToken ct)
    {
        var user = await _users.GetUserAsync(userId, ct);
        return Ok(user);
    }
}
