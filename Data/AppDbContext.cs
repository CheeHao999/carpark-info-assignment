using Microsoft.EntityFrameworkCore;
using carpark_info_assignment.Models;

namespace carpark_info_assignment.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Carpark> Carparks => Set<Carpark>();
    public DbSet<UserFavorite> UserFavorites => Set<UserFavorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Carpark>().HasKey(c => c.CarParkNo);
        modelBuilder.Entity<Carpark>().HasIndex(c => c.GantryHeight);
        modelBuilder.Entity<Carpark>().HasIndex(c => c.NightParking);

        modelBuilder.Entity<UserFavorite>()
            .HasOne(f => f.Carpark)
            .WithMany()
            .HasForeignKey(f => f.CarParkNo);

        modelBuilder.Entity<UserFavorite>()
            .HasIndex(f => new { f.UserId, f.CarParkNo })
            .IsUnique();
    }
}