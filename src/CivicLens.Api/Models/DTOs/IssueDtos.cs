using System.ComponentModel.DataAnnotations;

namespace CivicLens.Api.Models.DTOs;

public sealed record CreateIssueRequest(
    [Required, StringLength(200)] string Title,
    [Required, StringLength(4000)] string Description,
    [Required] Guid CategoryId,
    [Range(-90, 90)] double Latitude,
    [Range(-180, 180)] double Longitude,
    IReadOnlyCollection<MediaRequest>? Media = null);

public sealed record MediaRequest(
    [Required, Url, StringLength(2048)] string MediaUrl,
    [Required, StringLength(100)] string ContentType);

public sealed record UpdateIssueStatusRequest(
    [Required] string Status,
    [StringLength(4000)] string? Details = null);

public sealed record AssignIssueRequest(Guid FieldTechnicianId);

public sealed record IssueResponse(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    Guid CategoryId,
    double Latitude,
    double Longitude,
    IReadOnlyCollection<MediaResponse> Media);

public sealed record MediaResponse(Guid Id, string MediaUrl, string ContentType);
