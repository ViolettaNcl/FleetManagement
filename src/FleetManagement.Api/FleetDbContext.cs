using FleetManagement.Core;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api;

public sealed class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Trip> Trips => Set<Trip>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var vehicles=model.Entity<Vehicle>();
        vehicles.Property(v=>v.Id).ValueGeneratedNever();
        vehicles.Property(v=>v.Plate).HasMaxLength(40);
        vehicles.Property(v=>v.Model).HasMaxLength(100);
        vehicles.HasIndex(v=>v.Plate).IsUnique();
        var drivers=model.Entity<Driver>();
        drivers.Property(d=>d.Id).ValueGeneratedNever();
        drivers.Property(d=>d.Name).HasMaxLength(100);
        drivers.Property(d=>d.License).HasMaxLength(50);
        drivers.HasIndex(d=>d.License).IsUnique();
        var trips=model.Entity<Trip>();
        trips.Property(t=>t.Id).ValueGeneratedNever();
        trips.Property(t=>t.Origin).HasMaxLength(100);
        trips.Property(t=>t.Destination).HasMaxLength(100);
        trips.HasOne<Vehicle>().WithMany().HasForeignKey(t=>t.VehicleId).OnDelete(DeleteBehavior.Restrict);
        trips.HasOne<Driver>().WithMany().HasForeignKey(t=>t.DriverId).OnDelete(DeleteBehavior.Restrict);
        trips.HasIndex(t=>new{t.VehicleId,t.Start,t.End});
        trips.HasIndex(t=>new{t.DriverId,t.Start,t.End});
    }
}
