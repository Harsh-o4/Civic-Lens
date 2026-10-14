using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CivicLens.Api.Data.Context;
using CivicLens.Api.Models.DTOs;
using CivicLens.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CivicLens.Api.Services;

public sealed class AuthService(
    CivicLensDbContext dbContext,
    IConfiguration configuration,
    IPasswordHasher<User> passwordHasher) : IAuthService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            Role = "Citizen"
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == normalizedEmail,
            cancellationToken);
        if (user is null)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        return verification == PasswordVerificationResult.Failed ? null : CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var key = configuration["Jwt:Key"];
        var issuer = configuration["Jwt:Issuer"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32 || string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("JWT configuration is missing or invalid.");
        }

        var expiresAtUtc = DateTime.UtcNow.Add(TokenLifetime);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            issuer,
            claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AuthResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc);
    }
}
