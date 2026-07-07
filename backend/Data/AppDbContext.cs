using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Tracking> Tracking => Set<Tracking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Dispute> Disputes => Set<Dispute>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Store all enums as VARCHAR with their exact string values.
        ConfigureEnumsAsStrings(b);

        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Auth0Id).IsRequired();
            e.Property(x => x.FullName).IsRequired();
            e.Property(x => x.Email).IsRequired();
            e.Property(x => x.Phone).IsRequired();
            e.Property(x => x.Role).IsRequired();

            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Phone).IsUnique();
            e.HasIndex(x => x.Auth0Id).IsUnique();
        });

        b.Entity<Driver>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.LicenseNumber).IsRequired();
            e.Property(x => x.CurrentLat).HasPrecision(9, 6);
            e.Property(x => x.CurrentLng).HasPrecision(9, 6);

            e.HasOne(x => x.User)
                .WithOne(u => u.Driver)
                .HasForeignKey<Driver>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // active_vehicle_id is a soft reference — no cascade to avoid cycles
            e.HasOne(x => x.ActiveVehicle)
                .WithMany()
                .HasForeignKey(x => x.ActiveVehicleId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.OperationalStatus);
        });

        b.Entity<Vehicle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.VehicleNumber).IsRequired();

            e.HasOne(x => x.Driver)
                .WithMany(d => d.Vehicles)
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.DriverId);
        });

        b.Entity<Shipment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderId).IsRequired();
            e.Property(x => x.PickupLat).HasPrecision(9, 6);
            e.Property(x => x.PickupLng).HasPrecision(9, 6);
            e.Property(x => x.DropLat).HasPrecision(9, 6);
            e.Property(x => x.DropLng).HasPrecision(9, 6);
            e.Property(x => x.WeightKg).HasPrecision(10, 2);

            e.HasOne(x => x.Customer)
                .WithMany(u => u.Shipments)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Driver)
                .WithMany(d => d.Shipments)
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Vehicle)
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.StatusChangedBy)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.OrderId).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.DriverId);
            e.HasIndex(x => x.Status);
        });

        b.Entity<Tracking>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);

            e.HasOne(x => x.Shipment)
                .WithMany(s => s.TrackingPings)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Driver)
                .WithMany(d => d.TrackingPings)
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.ShipmentId, x.RecordedAt }).IsDescending(false, true);
        });

        b.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(10, 2);

            e.HasOne(x => x.Shipment)
                .WithOne(s => s.Payment)
                .HasForeignKey<Payment>(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.ShipmentId).IsUnique();
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        b.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired();

            e.HasOne(x => x.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Shipment)
                .WithMany(s => s.Notifications)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.UserId, x.IsRead });
        });

        b.Entity<Dispute>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ComplaintText).IsRequired();

            e.HasOne(x => x.Shipment)
                .WithMany(s => s.Disputes)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.RaisedByUser)
                .WithMany(u => u.RaisedDisputes)
                .HasForeignKey(x => x.RaisedBy)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ResolvedByUser)
                .WithMany(u => u.ResolvedDisputes)
                .HasForeignKey(x => x.ResolvedBy)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.ShipmentId);
        });
    }

    private static void ConfigureEnumsAsStrings(ModelBuilder b)
    {
        b.Entity<User>().Property(x => x.Role).HasConversion<string>();
        b.Entity<Driver>().Property(x => x.ApprovalStatus).HasConversion<string>();
        b.Entity<Driver>().Property(x => x.OperationalStatus).HasConversion<string>();
        b.Entity<Vehicle>().Property(x => x.VehicleType).HasConversion<string>();
        b.Entity<Shipment>().Property(x => x.Status).HasConversion<string>();
        b.Entity<Shipment>().Property(x => x.PackageType).HasConversion<string>();
        b.Entity<Shipment>().Property(x => x.PreferredWindow).HasConversion<string>();
        b.Entity<Shipment>().Property(x => x.RiskSeverity).HasConversion<string>();
        b.Entity<Payment>().Property(x => x.Method).HasConversion<string>();
        b.Entity<Payment>().Property(x => x.Status).HasConversion<string>();
        b.Entity<Dispute>().Property(x => x.LlmType).HasConversion<string>();
        b.Entity<Dispute>().Property(x => x.Status).HasConversion<string>();
    }
}
