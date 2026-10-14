using CivicLens.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CivicLens.Api.Data.Context;

public sealed class CivicLensDbContext(DbContextOptions<CivicLensDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueMedia> IssueMedia => Set<IssueMedia>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.CreatedByUserId);
            entity.HasIndex(x => x.Location).HasMethod("GIST");
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Location).HasColumnType("geography (point)");
            entity.HasOne(x => x.Category).WithMany(x => x.Issues).HasForeignKey(x => x.CategoryId);
            entity.HasOne(x => x.CreatedByUser).WithMany(x => x.CreatedIssues).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedToUser).WithMany(x => x.AssignedIssues).HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<IssueMedia>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.IssueId);
            entity.Property(x => x.MediaUrl).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Issue).WithMany(x => x.Media).HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.IssueId);
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(4000);
            entity.HasOne(x => x.Issue).WithMany(x => x.AuditLogs).HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.User).WithMany(x => x.AuditLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
