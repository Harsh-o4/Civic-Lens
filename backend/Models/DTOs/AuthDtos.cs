using System.ComponentModel.DataAnnotations;

namespace CivicLens.Api.Models.DTOs;

public sealed record RegisterRequest(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(200, MinimumLength = 8)] string Password,
    [Required, StringLength(200)] string FullName);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string Token,
    DateTime ExpiresAtUtc);
