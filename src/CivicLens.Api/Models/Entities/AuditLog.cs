namespace CivicLens.Api.Models.Entities;

public sealed class AuditLog : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid? IssueId { get; set; }
    public Issue? Issue { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
}
