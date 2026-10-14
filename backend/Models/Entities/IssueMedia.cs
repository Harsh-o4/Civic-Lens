namespace CivicLens.Api.Models.Entities;

public sealed class IssueMedia : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;
    public string MediaUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}
