using NetTopologySuite.Geometries;

namespace CivicLens.Api.Models.Entities;

public sealed class Issue : AuditableEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Submitted";
    public string Priority { get; set; } = "Normal";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Point Location { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public ICollection<IssueMedia> Media { get; set; } = new List<IssueMedia>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
