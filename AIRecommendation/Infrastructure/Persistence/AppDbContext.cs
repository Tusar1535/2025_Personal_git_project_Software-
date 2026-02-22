using AIRecommendation.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIRecommendation.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentRecord>().ToTable("Documents");
        modelBuilder.Entity<DocumentRecord>()
            .HasIndex(d => new { d.Category, d.FileName });

        base.OnModelCreating(modelBuilder);
    }
}