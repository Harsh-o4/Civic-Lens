using CivicLens.Api.Models.DTOs;

namespace CivicLens.Api.Services;

public interface IIssueService
{
    Task<IssueResponse> CreateAsync(CreateIssueRequest request, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<IssueResponse>> GetAsync(
        string? status, Guid? categoryId, double? minLatitude, double? maxLatitude,
        double? minLongitude, double? maxLongitude, CancellationToken cancellationToken);
    Task<IssueResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> UpdateStatusAsync(Guid id, UpdateIssueStatusRequest request, Guid userId, CancellationToken cancellationToken);
    Task<bool> AssignAsync(Guid id, Guid fieldTechnicianId, Guid adminUserId, CancellationToken cancellationToken);
}
