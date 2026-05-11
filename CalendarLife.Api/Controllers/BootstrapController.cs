using CalendarLife.Application.DTOs.Bootstrap;
using CalendarLife.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CalendarLife.Api.Controllers;

[ApiController]
[Route("users/{userId:guid}/bootstrap")]
[EnableRateLimiting("default")]
public sealed class BootstrapController : ControllerBase
{
    private readonly BootstrapService _bootstrap;

    public BootstrapController(BootstrapService bootstrap)
    {
        _bootstrap = bootstrap;
    }

    [HttpGet]
    public async Task<ActionResult<BootstrapStateResponse>> GetAsync(
        [FromRoute] Guid userId,
        CancellationToken ct)
    {
        var state = await _bootstrap.GetStateAsync(userId, ct);
        return Ok(state);
    }

    /// <summary>
    /// Imports a full "frontend state" payload into the backend for this user.
    /// This is meant for initial sync / restore.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BootstrapResponse>> ImportAsync(
        [FromRoute] Guid userId,
        [FromBody] BootstrapRequest request,
        CancellationToken ct)
    {
        var result = await _bootstrap.ImportAsync(userId, request, ct);
        return Ok(result);
    }
}
