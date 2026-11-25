using FleetManagement.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Api;

public sealed class FleetDbContext(DbContextOptions<FleetDbContext> options, IHttpContextAccessor http) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<MaintenanceEntry> MaintenanceEntries => Set<MaintenanceEntry>();
    public DbSet<DriverShift> DriverShifts => Set<DriverShift>();
    public DbSet<VehicleReservation> VehicleReservations => Set<VehicleReservation>();
    public DbSet<BatchReceipt> BatchReceipts => Set<BatchReceipt>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var actor = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Vehicle or Driver or Trip or MaintenanceEntry or DriverShift or VehicleReservation)
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => new AuditEntry {
                ActorId = actor,
                EntityType = e.Metadata.ClrType.Name,
                EntityId = (int)e.Property("Id").CurrentValue!,
                Action = e.State.ToString(),
                OccurredUtc = DateTime.UtcNow
            }).ToList();
        AuditEntries.AddRange(entries);
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        var receipt=model.Entity<BatchReceipt>();
        receipt.HasKey(r=>new{r.ActorId,r.Key});
        receipt.Property(r=>r.ActorId).HasMaxLength(450);
        receipt.Property(r=>r.Key).ValueGeneratedNever();
        receipt.Property(r=>r.PayloadHash).HasMaxLength(64);
        receipt.Property(r=>r.ResponseJson).HasMaxLength(65536);
        receipt.HasIndex(r=>r.CreatedUtc);
        var audit = model.Entity<AuditEntry>();
        audit.Property(a => a.ActorId).HasMaxLength(450);
        audit.Property(a => a.EntityType).HasMaxLength(50);
        audit.Property(a => a.Action).HasMaxLength(30);
        audit.HasIndex(a => a.OccurredUtc);
        var work=model.Entity<MaintenanceEntry>();
        work.Property(e=>e.Id).ValueGeneratedNever();
        work.Property(e=>e.Kind).HasMaxLength(40);
        work.Property(e=>e.Notes).HasMaxLength(500);
        work.Property(e=>e.CostEur).HasPrecision(12,2);
        work.Property(e=>e.RecordedAt).HasConversion(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
        work.HasOne<Vehicle>().WithMany().HasForeignKey(e=>e.VehicleId).OnDelete(DeleteBehavior.Restrict);
        work.HasIndex(e=>new{e.VehicleId,e.RecordedAt});
        var shifts=model.Entity<DriverShift>();
        shifts.Property(e=>e.Id).ValueGeneratedNever();
        shifts.Property(e=>e.Start).HasConversion(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
        shifts.Property(e=>e.End).HasConversion(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
        shifts.HasOne<Driver>().WithMany().HasForeignKey(e=>e.DriverId).OnDelete(DeleteBehavior.Restrict);
        shifts.HasIndex(e=>new{e.DriverId,e.Start,e.End});
        var reservations=model.Entity<VehicleReservation>();
        reservations.Property(e=>e.Id).ValueGeneratedNever();
        reservations.Property(e=>e.Kind).HasMaxLength(40);
        reservations.Property(e=>e.Notes).HasMaxLength(500);
        reservations.Property(e=>e.Start).HasConversion(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
        reservations.Property(e=>e.End).HasConversion(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
        reservations.HasOne<Vehicle>().WithMany().HasForeignKey(e=>e.VehicleId).OnDelete(DeleteBehavior.Restrict);
        reservations.HasIndex(e=>new{e.VehicleId,e.Start,e.End});
        var vehicles=model.Entity<Vehicle>();
        vehicles.Property(v=>v.Id).ValueGeneratedNever();
        vehicles.Property(v=>v.Plate).HasMaxLength(40);
        vehicles.Property(v=>v.Model).HasMaxLength(100);
        vehicles.HasIndex(v=>v.Plate).IsUnique();
        vehicles.Property(v=>v.LastService).HasConversion(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
        var drivers=model.Entity<Driver>();
        drivers.Property(d=>d.Id).ValueGeneratedNever();
        drivers.Property(d=>d.Name).HasMaxLength(100);
        drivers.Property(d=>d.License).HasMaxLength(50);
        drivers.HasIndex(d=>d.License).IsUnique();
        var trips=model.Entity<Trip>();
        trips.Property(t=>t.Id).ValueGeneratedNever();
        trips.Property(t=>t.Origin).HasMaxLength(100);
        trips.Property(t=>t.Destination).HasMaxLength(100);
        trips.Property(t=>t.Start).HasConversion(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
        trips.Property(t=>t.End).HasConversion(v=>v,v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
        trips.HasOne<Vehicle>().WithMany().HasForeignKey(t=>t.VehicleId).OnDelete(DeleteBehavior.Restrict);
        trips.HasOne<Driver>().WithMany().HasForeignKey(t=>t.DriverId).OnDelete(DeleteBehavior.Restrict);
        trips.HasIndex(t=>new{t.VehicleId,t.Start,t.End});
        trips.HasIndex(t=>new{t.DriverId,t.Start,t.End});
    }
}
