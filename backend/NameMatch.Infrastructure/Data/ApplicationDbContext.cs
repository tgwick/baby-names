using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NameMatch.Domain.Entities;
using NameMatch.Infrastructure.Identity;

namespace NameMatch.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Name> Names => Set<Name>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<NameCategory> NameCategories => Set<NameCategory>();
    public DbSet<NameCategoryMapping> NameCategoryMappings => Set<NameCategoryMapping>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Name>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NameText).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Origin).HasMaxLength(100);
            entity.Property(e => e.Meaning).HasMaxLength(500);
            entity.Property(e => e.EndingSound).HasMaxLength(10);
            entity.Property(e => e.SoundType).HasMaxLength(20);
            entity.HasIndex(e => e.NameText);
            entity.HasIndex(e => e.Gender);
        });

        builder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InitiatorId).IsRequired();
            entity.Property(e => e.JoinCode).IsRequired().HasMaxLength(6);
            entity.Property(e => e.PartnerLink).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.JoinCode).IsUnique();
            entity.HasIndex(e => e.PartnerLink).IsUnique();
            entity.HasIndex(e => e.InitiatorId);
            entity.HasIndex(e => e.PartnerId);
        });

        builder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();

            entity.HasOne(e => e.Name)
                .WithMany(n => n.Votes)
                .HasForeignKey(e => e.NameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Votes)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.NameId, e.SessionId }).IsUnique();
        });

        builder.Entity<NameCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CategoryType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.CategoryType);
        });

        builder.Entity<NameCategoryMapping>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Name)
                .WithMany(n => n.CategoryMappings)
                .HasForeignKey(e => e.NameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.NameMappings)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.NameId, e.CategoryId }).IsUnique();
        });

        builder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();

            entity.HasOne(e => e.Session)
                .WithMany(s => s.UserPreferences)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.UserPreferences)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.SessionId, e.CategoryId }).IsUnique();
        });
    }
}
