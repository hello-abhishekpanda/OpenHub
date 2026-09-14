using Microsoft.EntityFrameworkCore;
using OpenHub.Domain.Entities;

namespace OpenHub.Infrastructure.Persistence;

public sealed class OpenHubDbContext(DbContextOptions<OpenHubDbContext> options) : DbContext(options)
{
    public DbSet<DeveloperProfile> DeveloperProfiles => Set<DeveloperProfile>();
    public DbSet<RepositoryMetadata> Repositories => Set<RepositoryMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var profile = modelBuilder.Entity<DeveloperProfile>();
        profile.HasKey(x => x.Id);
        profile.HasIndex(x => x.Username).IsUnique();
        profile.HasIndex(x => x.GitHubId).IsUnique();
        profile.Property(x => x.Username).HasMaxLength(100);
        profile.Property(x => x.DisplayName).HasMaxLength(200);
        profile.Property(x => x.LastSyncError).HasMaxLength(2000);
        profile.HasMany(x => x.Repositories).WithOne(x => x.DeveloperProfile)
            .HasForeignKey(x => x.DeveloperProfileId).OnDelete(DeleteBehavior.Cascade);

        var repository = modelBuilder.Entity<RepositoryMetadata>();
        repository.HasKey(x => x.Id);
        repository.HasIndex(x => x.GitHubId).IsUnique();
        repository.HasIndex(x => new { x.DeveloperProfileId, x.Name });
        repository.Property(x => x.Name).HasMaxLength(200);
        repository.Property(x => x.FullName).HasMaxLength(300);
        repository.Property(x => x.PrimaryLanguage).HasMaxLength(100);
        repository.Property(x => x.Topics).HasColumnType("text[]");
    }
}
