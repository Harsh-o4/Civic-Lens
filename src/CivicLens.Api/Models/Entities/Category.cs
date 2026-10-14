namespace CivicLens.Api.Models.Entities;

public sealed class Category : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
