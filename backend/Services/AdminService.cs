using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logistics.Api.Data;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace Logistics.Api.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IShipmentRepository _shipmentRepo;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        AppDbContext db,
        IShipmentRepository shipmentRepo,
        INotificationService notificationService,
        ILogger<AdminService> logger)
    {
        _db = db;
        _shipmentRepo = shipmentRepo;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ReassignShipmentResponse> ReassignShipmentAsync(Guid shipmentId)
    {
        var shipment = await _shipmentRepo.GetByIdAsync(shipmentId);
        if (shipment == null)
        {
            throw new NotFoundException("Shipment not found.");
        }

        Guid? oldDriverUserId = null;
        if (shipment.DriverId.HasValue)
        {
            var oldDriver = await _db.Drivers.FindAsync(shipment.DriverId.Value);
            if (oldDriver != null)
            {
                oldDriverUserId = oldDriver.UserId;
            }
        }

        shipment.DriverId = null;
        shipment.VehicleId = null;
        shipment.Status = ShipmentStatus.OPEN;
        shipment.StatusUpdatedAt = DateTime.UtcNow;
        shipment.UpdatedAt = DateTime.UtcNow;

        await _shipmentRepo.UpdateAsync(shipment);
        _logger.LogInformation("Shipment {ShipmentId} ({OrderId}) force-released back to open pool by Administrator.", shipment.Id, shipment.OrderId);

        if (oldDriverUserId.HasValue)
        {
            await _notificationService.CreateNotificationAsync(
                oldDriverUserId.Value,
                shipment.Id,
                "Shipment Unassigned",
                $"You have been unassigned from shipment {shipment.OrderId} by the administrator."
            );
        }

        await _notificationService.BroadcastShipmentUpdateAsync(shipment.Id, "OPEN", new { shipment.OrderId });
        await BroadcastJobToEligibleDriversAsync(shipment);

        return new ReassignShipmentResponse
        {
            Id = shipment.Id,
            Status = ShipmentStatus.OPEN,
            DriverId = null
        };
    }

    public async Task<AdminMetricsResponse> GetMetricsAsync()
    {
        var totalShipments = await _db.Shipments.CountAsync();
        var delivered = await _db.Shipments.CountAsync(s => s.Status == ShipmentStatus.DELIVERED);
        var cancelled = await _db.Shipments.CountAsync(s => s.Status == ShipmentStatus.CANCELLED);
        var failed = await _db.Shipments.CountAsync(s => s.Status == ShipmentStatus.PICKUP_FAILED);
        var staleShipments = await _db.Shipments.CountAsync(s => s.Status == ShipmentStatus.STALE);

        var pending = await _db.Shipments.CountAsync(s => 
            s.Status != ShipmentStatus.DELIVERED && 
            s.Status != ShipmentStatus.CANCELLED && 
            s.Status != ShipmentStatus.PICKUP_FAILED);

        var deliveredShipments = await _db.Shipments
            .Where(s => s.Status == ShipmentStatus.DELIVERED && s.StatusUpdatedAt.HasValue)
            .Select(s => new { s.CreatedAt, s.StatusUpdatedAt })
            .ToListAsync();

        int avgDeliveryTimeMinutes = 0;
        if (deliveredShipments.Count > 0)
        {
            var totalDiffMinutes = deliveredShipments.Sum(s => (s.StatusUpdatedAt.GetValueOrDefault() - s.CreatedAt).TotalMinutes);
            avgDeliveryTimeMinutes = (int)Math.Round(totalDiffMinutes / deliveredShipments.Count);
        }

        var codPending = await _db.Shipments
            .Include(s => s.Payment)
            .CountAsync(s => s.Status == ShipmentStatus.DELIVERED && !s.CashCollected && (s.Payment == null || s.Payment.Method == PaymentMethod.COD));

        var driversOnline = await _db.Drivers
            .CountAsync(d => d.OperationalStatus == OperationalStatus.ONLINE || d.OperationalStatus == OperationalStatus.ON_DELIVERY);

        var driversWithHighCancelCount = await _db.Drivers
            .CountAsync(d => d.CancelCount >= 3);

        var successfulPayments = await _db.Payments
            .Where(p => p.Status == PaymentStatus.SUCCESS)
            .Select(p => new { p.Amount, p.DriverEarnings, p.PlatformFee, p.Cgst, p.Sgst })
            .ToListAsync();

        var totalRevenue = successfulPayments.Sum(p => p.Amount);
        var totalDriverEarnings = successfulPayments.Sum(p => p.DriverEarnings);
        var totalPlatformFees = successfulPayments.Sum(p => p.PlatformFee);
        var totalTaxCollected = successfulPayments.Sum(p => p.Cgst + p.Sgst);

        return new AdminMetricsResponse
        {
            TotalShipments = totalShipments,
            Delivered = delivered,
            Pending = pending,
            Cancelled = cancelled,
            Failed = failed,
            AvgDeliveryTimeMinutes = avgDeliveryTimeMinutes,
            StaleShipments = staleShipments,
            CodPending = codPending,
            DriversOnline = driversOnline,
            DriversWithHighCancelCount = driversWithHighCancelCount,
            TotalRevenue = totalRevenue,
            TotalDriverEarnings = totalDriverEarnings,
            TotalPlatformFees = totalPlatformFees,
            TotalTaxCollected = totalTaxCollected
        };
    }

    public async Task<byte[]> ExportShipmentsCsvAsync(ShipmentStatus? status, DateTime? dateFrom, DateTime? dateTo)
    {
        var query = _db.Shipments
            .Include(s => s.Customer)
            .Include(s => s.Driver)
                .ThenInclude(d => d!.User)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (dateFrom.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(dateFrom.Value, DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt >= fromUtc);
        }

        if (dateTo.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(dateTo.Value, DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt <= toUtc);
        }

        var shipments = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("OrderId,CustomerName,CustomerEmail,CustomerPhone,PickupAddress,DropAddress,ReceiverName,ReceiverPhone,PackageType,WeightKg,Status,CashCollected,RiskFlag,RiskSeverity,CreatedAt");

        foreach (var s in shipments)
        {
            csv.AppendLine(string.Join(",",
                EscapeCsv(s.OrderId),
                EscapeCsv(s.Customer?.FullName),
                EscapeCsv(s.Customer?.Email),
                EscapeCsv(s.Customer?.Phone),
                EscapeCsv(s.PickupAddress),
                EscapeCsv(s.DropAddress),
                EscapeCsv(s.ReceiverName),
                EscapeCsv(s.ReceiverPhone),
                EscapeCsv(s.PackageType.ToString()),
                s.WeightKg.ToString("F2"),
                EscapeCsv(s.Status.ToString()),
                s.CashCollected.ToString().ToLower(),
                s.RiskFlag.ToString().ToLower(),
                EscapeCsv(s.RiskSeverity.ToString()),
                s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            ));
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private string EscapeCsv(string? value)
    {
        if (value == null) return string.Empty;
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private async Task BroadcastJobToEligibleDriversAsync(Shipment shipment)
    {
        var eligibleVehicles = new List<VehicleType> { VehicleType.TWO_WHEELER, VehicleType.THREE_WHEELER, VehicleType.FOUR_WHEELER, VehicleType.HEAVY_VEHICLE }
            .Where(v => IsVehicleEligible(v, shipment.PackageType, shipment.WeightKg));

        foreach (var vehicle in eligibleVehicles)
        {
            await _notificationService.BroadcastNewJobAlertAsync(vehicle.ToString(), new
            {
                shipment.Id,
                shipment.OrderId,
                shipment.PickupAddress,
                shipment.DropAddress,
                PackageType = shipment.PackageType.ToString(),
                shipment.WeightKg,
                PreferredWindow = shipment.PreferredWindow.ToString(),
                DriverInstruction = shipment.DriverInstruction,
                DriverEarnings = shipment.Payment != null ? shipment.Payment.DriverEarnings : 0m,
                RiskFlag = shipment.RiskFlag,
                RiskSeverity = shipment.RiskSeverity.ToString(),
                RiskReason = shipment.RiskReason
            });
        }
    }

    private static bool IsVehicleEligible(VehicleType vehicleType, PackageType packageType, decimal weight)
    {
        if (packageType == PackageType.DOCUMENT)
        {
            return vehicleType == VehicleType.TWO_WHEELER;
        }
        if (packageType == PackageType.FRAGILE || packageType == PackageType.HOUSEHOLD)
        {
            return vehicleType == VehicleType.FOUR_WHEELER || vehicleType == VehicleType.HEAVY_VEHICLE;
        }

        if (weight >= 0 && weight <= 5)
        {
            return vehicleType == VehicleType.TWO_WHEELER || vehicleType == VehicleType.THREE_WHEELER;
        }
        if (weight > 5 && weight <= 20)
        {
            return vehicleType == VehicleType.THREE_WHEELER || vehicleType == VehicleType.FOUR_WHEELER;
        }
        if (weight > 20 && weight <= 200)
        {
            return vehicleType == VehicleType.FOUR_WHEELER;
        }
        if (weight > 200)
        {
            return vehicleType == VehicleType.HEAVY_VEHICLE;
        }

        return false;
    }
}
