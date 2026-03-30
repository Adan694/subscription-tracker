using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasMany(e => e.Subscriptions)
                  .WithOne(s => s.User)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Subscription configuration
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Frequency).HasMaxLength(20);
        });
    }
}