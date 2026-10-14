using CivicLens.Api.Data.Context;
using CivicLens.Api.Models.DTOs;
using CivicLens.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace CivicLens.Api.Services;

public sealed class IssueService(CivicLensDbContext dbContext) : IIssueService
{
    private static readonly Guid AnonymousUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly GeometryFactory GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(4326);
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Submitted"] = ["InReview"],
            ["InReview"] = ["Assigned", "Closed"],
            ["Assigned"] = ["InProgress"],
            ["InProgress"] = ["Resolved"],
            ["Resolved"] = ["Closed"]
        };

    public async Task<IssueResponse> CreateAsync(
        CreateIssueRequest request, Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            userId = await EnsureAnonymousUserAsync(cancellationToken);
        }
        if (!await dbContext.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken))
        {
            dbContext.Categories.Add(new Category
            {
                Id = request.CategoryId,
                Name = $"General ({request.CategoryId})",
                Description = "Temporary category for unauthenticated frontend testing."
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            CreatedByUserId = userId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Location = GeometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude))
        };
        if (request.Media is not null)
        {
            issue.Media = request.Media.Select(media => new IssueMedia
            {
                Id = Guid.NewGuid(),
                IssueId = issue.Id,
                MediaUrl = media.MediaUrl,
                ContentType = media.ContentType
            }).ToList();
        }

        dbContext.Issues.Add(issue);
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            IssueId = issue.Id,
            UserId = userId,
            Action = "IssueCreated",
            Details = "Issue submitted by citizen."
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(issue);
    }

    private async Task<Guid> EnsureAnonymousUserAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.Users.AnyAsync(user => user.Id == AnonymousUserId, cancellationToken))
        {
            dbContext.Users.Add(new User
            {
                Id = AnonymousUserId,
                Email = "anonymous-testing@civic-portal.local",
                PasswordHash = "AUTHENTICATION_DISABLED",
                FullName = "Anonymous Test User",
                Role = "Citizen"
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return AnonymousUserId;
    }

    public async Task<IReadOnlyCollection<IssueResponse>> GetAsync(
        string? status, Guid? categoryId, double? minLatitude, double? maxLatitude,
        double? minLongitude, double? maxLongitude, CancellationToken cancellationToken)
    {
        var query = dbContext.Issues.AsNoTracking().Include(issue => issue.Media).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(issue => issue.Status == status);
        }
        if (categoryId.HasValue)
        {
            query = query.Where(issue => issue.CategoryId == categoryId);
        }
        if (minLatitude.HasValue) query = query.Where(issue => issue.Latitude >= minLatitude);
        if (maxLatitude.HasValue) query = query.Where(issue => issue.Latitude <= maxLatitude);
        if (minLongitude.HasValue) query = query.Where(issue => issue.Longitude >= minLongitude);
        if (maxLongitude.HasValue) query = query.Where(issue => issue.Longitude <= maxLongitude);

        var issues = await query.OrderByDescending(issue => issue.CreatedAtUtc).ToListAsync(cancellationToken);
        return issues.Select(ToResponse).ToArray();
    }

    public async Task<IssueResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var issue = await dbContext.Issues
            .AsNoTracking()
            .Include(candidate => candidate.Media)
            .Where(issue => issue.Id == id)
            .SingleOrDefaultAsync(cancellationToken);
        return issue is null ? null : ToResponse(issue);
    }

    public async Task<bool> UpdateStatusAsync(
        Guid id, UpdateIssueStatusRequest request, Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) userId = await EnsureAnonymousUserAsync(cancellationToken);
        var issue = await dbContext.Issues.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (issue is null) return false;
        if (!AllowedTransitions.TryGetValue(issue.Status, out var transitions) ||
            !transitions.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Cannot transition an issue from {issue.Status} to {request.Status}.");
        }

        issue.Status = request.Status;
        issue.UpdatedAtUtc = DateTime.UtcNow;
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(), IssueId = id, UserId = userId,
            Action = "StatusChanged", Details = request.Details
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AssignAsync(
        Guid id, Guid fieldTechnicianId, Guid adminUserId, CancellationToken cancellationToken)
    {
        if (adminUserId == Guid.Empty) adminUserId = await EnsureAnonymousUserAsync(cancellationToken);
        var issue = await dbContext.Issues.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        var technician = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Id == fieldTechnicianId && user.Role == "FieldTech", cancellationToken);
        if (issue is null || technician is null) return false;
        if (issue.Status is not ("InReview" or "Submitted"))
        {
            throw new InvalidOperationException("Only submitted or in-review issues can be assigned.");
        }

        issue.AssignedToUserId = fieldTechnicianId;
        issue.Status = "Assigned";
        issue.UpdatedAtUtc = DateTime.UtcNow;
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(), IssueId = id, UserId = adminUserId,
            Action = "IssueAssigned", Details = $"Assigned to {technician.Email}."
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IssueResponse ToResponse(Issue issue) =>
        new(issue.Id, issue.Title, issue.Description, issue.Status, issue.Priority,
            issue.CategoryId, issue.Latitude, issue.Longitude,
            issue.Media.Select(media => new MediaResponse(media.Id, media.MediaUrl, media.ContentType)).ToArray());
}
