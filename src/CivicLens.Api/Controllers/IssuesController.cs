using System.Security.Claims;
using CivicLens.Api.Models.DTOs;
using CivicLens.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicLens.Api.Controllers;

[ApiController]
[Route("api/v1/issues")]
public sealed class IssuesController(IIssueService issueService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<IssueResponse>>> Get(
        [FromQuery] string? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] double? minLatitude,
        [FromQuery] double? maxLatitude,
        [FromQuery] double? minLongitude,
        [FromQuery] double? maxLongitude,
        CancellationToken cancellationToken) =>
        Ok(await issueService.GetAsync(status, categoryId, minLatitude, maxLatitude,
            minLongitude, maxLongitude, cancellationToken));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IssueResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await issueService.GetByIdAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    // [Authorize(Policy = "CitizenOnly")] // Temporarily disabled for frontend testing.
    [AllowAnonymous]
    public async Task<ActionResult<IssueResponse>> Create(
        CreateIssueRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrEmpty();
        var response = await issueService.CreateAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPatch("{id:guid}/status")]
    // [Authorize(Roles = "Admin,FieldTech")] // Temporarily disabled for frontend testing.
    [AllowAnonymous]
    public async Task<IActionResult> UpdateStatus(
        Guid id, UpdateIssueStatusRequest request, CancellationToken cancellationToken)
    {
        var updated = await issueService.UpdateStatusAsync(id, request, GetUserIdOrEmpty(), cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/assign")]
    // [Authorize(Policy = "AdminOnly")] // Temporarily disabled for frontend testing.
    [AllowAnonymous]
    public async Task<IActionResult> Assign(
        Guid id, AssignIssueRequest request, CancellationToken cancellationToken)
    {
        var assigned = await issueService.AssignAsync(id, request.FieldTechnicianId, GetUserIdOrEmpty(), cancellationToken);
        return assigned ? NoContent() : NotFound();
    }

    // private Guid GetUserId() =>
    //     Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
    //         ?? User.FindFirstValue("sub")
    //         ?? throw new InvalidOperationException("Authenticated user ID is missing."));

    private Guid GetUserIdOrEmpty() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub"), out var userId) ? userId : Guid.Empty;
}
