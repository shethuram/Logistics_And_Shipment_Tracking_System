using Logistics.Api.Data;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Mappings;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logistics.Api.Services;

public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipmentRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IGeoService _geoService;
    private readonly IOtpService _otpService;
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _db;
    private readonly ILlmService _llmService;
    private readonly ILogger<ShipmentService> _logger;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;

    public ShipmentService(
        IShipmentRepository shipmentRepo,
        IDriverRepository driverRepo,
        IVehicleRepository vehicleRepo,
        IGeoService geoService,
        IOtpService otpService,
        INotificationService notificationService,
        AppDbContext db,
        ILlmService llmService,
        ILogger<ShipmentService> _logger,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService)
    {
        _shipmentRepo = shipmentRepo;
        _driverRepo = driverRepo;
        _vehicleRepo = vehicleRepo;
        _geoService = geoService;
        _otpService = otpService;
        _notificationService = notificationService;
        _db = db;
        _llmService = llmService;
        this._logger = _logger;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
    }

    public async Task<CreateShipmentResponse> CreateAsync(CreateShipmentRequest request, Guid customerId)
    {
        decimal deliveryCharge = CalculateDeliveryCharge(request.PackageType, request.WeightKg, request.PickupLat, request.PickupLng, request.DropLat, request.DropLng);
        decimal taxableAmount = deliveryCharge + 20m;
        decimal cgst = Math.Round(taxableAmount * 0.09m, 2);
        decimal sgst = Math.Round(taxableAmount * 0.09m, 2);
        decimal totalAmount = taxableAmount + cgst + sgst;

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            PickupAddress = request.PickupAddress,
            PickupLat = request.PickupLat,
            PickupLng = request.PickupLng,
            DropAddress = request.DropAddress,
            DropLat = request.DropLat,
            DropLng = request.DropLng,
            ReceiverName = request.ReceiverName,
            ReceiverPhone = request.ReceiverPhone,
            PackageType = request.PackageType,
            WeightKg = request.WeightKg,
            PreferredWindow = request.PreferredWindow,
            SpecialNotes = request.SpecialNotes,
            Status = request.PaymentMethod == PaymentMethod.COD ? ShipmentStatus.OPEN : ShipmentStatus.PENDING_PAYMENT,
            StatusChangedBy = customerId,
            StatusUpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            Method = request.PaymentMethod,
            Amount = totalAmount,
            DeliveryCharge = deliveryCharge,
            PlatformFee = 20m,
            DriverCommission = Math.Round(deliveryCharge * 0.05m, 2),
            DriverEarnings = Math.Round(deliveryCharge * 0.95m, 2),
            Cgst = cgst,
            Sgst = sgst,
            Status = PaymentStatus.PENDING,
            IdempotencyKey = $"pay_chk_{shipment.Id:N}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        shipment.Payment = payment;

        if (!string.IsNullOrWhiteSpace(request.SpecialNotes))
        {
            var parsed = await _llmService.ParseDeliveryNoteAsync(request.SpecialNotes);
            shipment.RiskFlag = parsed.RiskFlag;
            shipment.RiskSeverity = parsed.RiskSeverity;
            shipment.RiskReason = parsed.RiskReason;
            shipment.PreferredDeliveryAfter = parsed.PreferredDeliveryAfter;
            shipment.DriverInstruction = parsed.DriverInstruction;
        }

        const int maxRetries = 3;
        int attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
                var count = await _shipmentRepo.GetTodayCountAsync(todayStr);
                var sequence = count + attempt;
                shipment.OrderId = $"TRK-{todayStr}-{sequence:D5}";

                await _shipmentRepo.AddAsync(shipment);
                _logger.LogInformation("Shipment {ShipmentId} ({OrderId}) created by Customer {CustomerId} with value {Amount}.", shipment.Id, shipment.OrderId, customerId, totalAmount);
                break;
            }
            catch (DbUpdateException) when (attempt < maxRetries)
            {
            }
        }

        await _notificationService.CreateNotificationAsync(customerId, shipment.Id, "Shipment Created", $"Your shipment {shipment.OrderId} has been created successfully.");

        if (request.PaymentMethod == PaymentMethod.COD)
        {
            await BroadcastJobToEligibleDriversAsync(shipment);
        }

        var paymentUrl = request.PaymentMethod == PaymentMethod.ONLINE ? $"https://razorpay.com/pay/{shipment.Id}" : null;

        return shipment.ToCreateShipmentResponse(paymentUrl: paymentUrl);
    }

    public async Task<Shipment> GetRawByIdAsync(Guid id)
    {
        var shipment = await _shipmentRepo.GetByIdAsync(id);
        if (shipment == null)
            throw new NotFoundException("Shipment not found.");
        return shipment;
    }

    public async Task<ShipmentResponse> GetByIdAsync(Shipment shipment)
    {
        return shipment.ToShipmentResponse();
    }

    public async Task UpdateAsync(Shipment shipment, UpdateShipmentRequest request)
    {
        if (shipment.Status != ShipmentStatus.PENDING_PAYMENT && shipment.Status != ShipmentStatus.OPEN)
            throw new BusinessRuleException("Shipment can only be updated when status is PENDING_PAYMENT or OPEN.");

        if (request.PreferredWindow.HasValue)
        {
            shipment.PreferredWindow = request.PreferredWindow.Value;
        }

        if (request.SpecialNotes != null)
        {
            shipment.SpecialNotes = request.SpecialNotes;
            if (!string.IsNullOrWhiteSpace(request.SpecialNotes))
            {
                var parsed = await _llmService.ParseDeliveryNoteAsync(request.SpecialNotes);
                shipment.RiskFlag = parsed.RiskFlag;
                shipment.RiskSeverity = parsed.RiskSeverity;
                shipment.RiskReason = parsed.RiskReason;
                shipment.PreferredDeliveryAfter = parsed.PreferredDeliveryAfter;
                shipment.DriverInstruction = parsed.DriverInstruction;
            }
            else
            {
                shipment.RiskFlag = false;
                shipment.RiskSeverity = RiskSeverity.NONE;
                shipment.RiskReason = null;
                shipment.PreferredDeliveryAfter = null;
                shipment.DriverInstruction = null;
            }
        }

        shipment.UpdatedAt = DateTime.UtcNow;
        await _shipmentRepo.UpdateAsync(shipment);
    }

    public async Task<CancelShipmentResponse> CancelAsync(Shipment shipment, Guid customerId)
    {
        if (shipment.Status == ShipmentStatus.ASSIGNED ||
            shipment.Status == ShipmentStatus.IN_TRANSIT ||
            shipment.Status == ShipmentStatus.DELIVERED)
        {
            throw new BusinessRuleException("Shipment cannot be cancelled after driver is assigned.");
        }

        var refundInitiated = shipment.Status == ShipmentStatus.OPEN;
        if (refundInitiated && shipment.Payment != null)
        {
            shipment.Payment.Status = PaymentStatus.REFUNDED;
            shipment.Payment.UpdatedAt = DateTime.UtcNow;
        }
        shipment.Status = ShipmentStatus.CANCELLED;
        shipment.StatusUpdatedAt = DateTime.UtcNow;
        shipment.StatusChangedBy = customerId;
        shipment.UpdatedAt = DateTime.UtcNow;

        await _shipmentRepo.UpdateAsync(shipment);

        return new CancelShipmentResponse
        {
            Id = shipment.Id,
            Status = shipment.Status,
            RefundInitiated = refundInitiated
        };
    }

    public async Task<PagedResult<ShipmentResponse>> GetShipmentsAsync(
        Guid userId, string role, string? search, ShipmentStatus? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        Guid? customerIdFilter = null;
        if (role == "CUSTOMER")
        {
            customerIdFilter = userId;
        }

        var statusFilter = status;

        var (items, total) = await _shipmentRepo.GetShipmentsAsync(
            customerIdFilter, search, statusFilter, dateFrom, dateTo, page, pageSize);

        var data = items.Select(s => s.ToShipmentResponse()).ToList();

        return new PagedResult<ShipmentResponse>
        {
            Data = data,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<AvailableShipmentDto>> GetAvailableShipmentsAsync(Guid driverUserId)
    {
        var driver = await _driverRepo.GetByUserIdAsync(driverUserId);
        if (driver == null)
            throw new NotFoundException("Driver profile not found.");

        if (driver.ApprovalStatus != ApprovalStatus.APPROVED)
            throw new ForbiddenException("Driver is not approved.");

        if (driver.ActiveVehicleId == null)
            throw new ValidationException("Driver has no active vehicle set.");

        var activeVehicle = await _vehicleRepo.GetByIdAsync(driver.ActiveVehicleId.Value);
        if (activeVehicle == null)
            throw new ValidationException("Driver active vehicle not found.");

        var openShipments = await _shipmentRepo.GetOpenShipmentsAsync();

        var driverLat = driver.CurrentLat ?? 0;
        var driverLng = driver.CurrentLng ?? 0;

        var eligibleShipments = openShipments
            .Where(s => IsVehicleEligible(activeVehicle.VehicleType, s.PackageType, s.WeightKg))
            .Select(s =>
            {
                var distance = _geoService.CalculateDistance(driverLat, driverLng, s.PickupLat, s.PickupLng);
                return s.ToAvailableShipmentDto(distance);
            })
            .OrderBy(s => s.DistanceToPickupKm)
            .ToList();

        return eligibleShipments;
    }

    public async Task<ShipmentResponse?> GetActiveShipmentAsync(Guid driverUserId)
    {
        var driver = await _driverRepo.GetByUserIdAsync(driverUserId);
        if (driver == null)
            throw new NotFoundException("Driver profile not found.");

        var shipment = await _shipmentRepo.GetActiveShipmentForDriverAsync(driver.Id);
        return shipment?.ToShipmentResponse();
    }

    public async Task<IReadOnlyList<ShipmentResponse>> GetDriverHistoryAsync(Guid driverUserId)
    {
        var driver = await _driverRepo.GetByUserIdAsync(driverUserId);
        if (driver == null)
            throw new NotFoundException("Driver profile not found.");

        var shipments = await _db.Shipments
            .Include(s => s.Payment)
            .Where(s => s.DriverId == driver.Id)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return shipments.Select(s => s.ToShipmentResponse()).ToList();
    }

    public async Task<ClaimShipmentResponse> ClaimAsync(Guid id, Guid driverUserId)
    {
        var driver = await _driverRepo.GetByUserIdAsync(driverUserId);
        if (driver == null)
            throw new NotFoundException("Driver profile not found.");

        if (driver.ApprovalStatus != ApprovalStatus.APPROVED)
            throw new ForbiddenException("Driver is not approved.");

        if (driver.ActiveVehicleId == null)
            throw new ValidationException("Driver has no active vehicle set.");

        var activeVehicle = await _vehicleRepo.GetByIdAsync(driver.ActiveVehicleId.Value);
        if (activeVehicle == null)
            throw new ValidationException("Driver active vehicle not found.");

        var activeShipment = await _shipmentRepo.GetActiveShipmentForDriverAsync(driver.Id);
        if (activeShipment != null)
            throw new ValidationException("Driver already has an active shipment.");

        var existingShipment = await _shipmentRepo.GetByIdAsync(id);
        if (existingShipment == null)
            throw new NotFoundException("Shipment not found.");

        if (existingShipment.Status != ShipmentStatus.OPEN)
            throw new ConflictException("Shipment has already been claimed by another driver or is not open.");

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var shipment = await _shipmentRepo.GetByIdForUpdateAsync(id);
            if (shipment == null)
            {
                throw new ConflictException("Shipment has already been claimed by another driver or is not open.");
            }

            if (!IsVehicleEligible(activeVehicle.VehicleType, shipment.PackageType, shipment.WeightKg))
            {
                throw new ValidationException("Driver's active vehicle is not eligible for this shipment.");
            }

            shipment.DriverId = driver.Id;
            shipment.VehicleId = activeVehicle.Id;
            shipment.Status = ShipmentStatus.ASSIGNED;
            shipment.StatusUpdatedAt = DateTime.UtcNow;
            shipment.StatusChangedBy = driver.UserId;
            shipment.UpdatedAt = DateTime.UtcNow;

            driver.OperationalStatus = OperationalStatus.ON_DELIVERY;
            await _driverRepo.UpdateAsync(driver);

            var senderOtp = _otpService.GenerateOtp();
            var receiverOtp = _otpService.GenerateOtp();

            shipment.SenderOtpHash = _otpService.HashOtp(senderOtp);
            shipment.SenderOtpExpiresAt = DateTime.UtcNow.AddMinutes(30);
            shipment.SenderOtpAttempts = 0;

            shipment.ReceiverOtpHash = _otpService.HashOtp(receiverOtp);
            shipment.ReceiverOtpExpiresAt = DateTime.UtcNow.AddDays(1);
            shipment.ReceiverOtpAttempts = 0;

            await _shipmentRepo.UpdateAsync(shipment);
            await transaction.CommitAsync();
            _logger.LogInformation("Shipment {ShipmentId} ({OrderId}) claimed by Driver {DriverId}.", shipment.Id, shipment.OrderId, driver.Id);

            var customer = await _db.Users.FindAsync(shipment.CustomerId);
            var customerEmail = customer?.Email ?? "customer@example.com";
            var customerName = customer?.FullName ?? "Customer";

            var driverUser = await _db.Users.FindAsync(driver.UserId);
            var driverName = driverUser?.FullName ?? "Driver";
            var vehicleNumber = activeVehicle.VehicleNumber;
            var vehicleType = activeVehicle.VehicleType.ToString();

            var (emailSubject, emailBody) = _emailTemplateService.GenerateDriverAssignedNotification(
                shipment, customerName, driverName, vehicleNumber, vehicleType, senderOtp, receiverOtp);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(customerEmail, emailSubject, emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send driver assignment email to {Email}", customerEmail);
                }
            });

            await _notificationService.CreateNotificationAsync(shipment.CustomerId, shipment.Id, "Driver Assigned", $"A driver has been assigned to your shipment {shipment.OrderId}. Verification OTPs have been sent to your email.");
            await _notificationService.BroadcastShipmentUpdateAsync(shipment.Id, "ASSIGNED", new { shipment.DriverId, shipment.OrderId });

            return shipment.ToClaimShipmentResponse();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<CancelClaimResponse> CancelClaimAsync(Shipment shipment, Guid driverUserId, CancelClaimRequest request)
    {
        var driver = await _driverRepo.GetByUserIdAsync(driverUserId);
        if (driver == null)
            throw new NotFoundException("Driver profile not found.");

        if (shipment.Status != ShipmentStatus.ASSIGNED)
            throw new BusinessRuleException("You can only cancel claim when shipment is in ASSIGNED status.");

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            driver.CancelCount++;
            driver.OperationalStatus = OperationalStatus.ONLINE;
            await _driverRepo.UpdateAsync(driver);

            shipment.DriverId = null;
            shipment.VehicleId = null;
            shipment.Status = ShipmentStatus.OPEN;
            shipment.StatusUpdatedAt = DateTime.UtcNow;
            shipment.StatusChangedBy = driver.UserId;
            shipment.UpdatedAt = DateTime.UtcNow;

            await _shipmentRepo.UpdateAsync(shipment);
            await transaction.CommitAsync();
            _logger.LogWarning("Driver {DriverId} cancelled claim for Shipment {ShipmentId} ({OrderId}). Reason: {Reason}, Current Cancel Count: {CancelCount}.", driver.Id, shipment.Id, shipment.OrderId, request.Reason, driver.CancelCount);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        if (driver.CancelCount >= 3)
        {
            await _notificationService.BroadcastAdminAlertAsync("HIGH_CANCEL_COUNT", new { DriverId = driver.Id, DriverName = driver.User?.FullName, CancelCount = driver.CancelCount });
        }

        await _notificationService.CreateNotificationAsync(shipment.CustomerId, shipment.Id, "Driver Cancelled Claim", $"The driver assigned to your shipment {shipment.OrderId} has cancelled the claim. Reason: {request.Reason}");
        await _notificationService.BroadcastShipmentUpdateAsync(shipment.Id, "OPEN", new { shipment.OrderId });
        await BroadcastJobToEligibleDriversAsync(shipment);

        return shipment.ToCancelClaimResponse(driver.CancelCount);
    }

    public async Task<ConfirmPickupResponse> ConfirmPickupAsync(Shipment shipment, Guid driverUserId, ConfirmPickupRequest request)
    {
        var driver = await _driverRepo.GetByUserIdAsync(driverUserId);
        if (driver == null)
            throw new NotFoundException("Driver profile not found.");

        if (shipment.Status != ShipmentStatus.ASSIGNED)
            throw new BusinessRuleException("Shipment must be in ASSIGNED status to confirm pickup.");

        if (shipment.SenderOtpExpiresAt < DateTime.UtcNow)
        {
            throw new ValidationException("OTP has expired.");
        }

        if (shipment.SenderOtpAttempts >= 3)
        {
            throw new TooManyRequestsException("Maximum OTP verification attempts exceeded.");
        }

        if (!_otpService.VerifyOtp(request.Otp, shipment.SenderOtpHash ?? string.Empty))
        {
            shipment.SenderOtpAttempts++;
            await _shipmentRepo.UpdateAsync(shipment);

            if (shipment.SenderOtpAttempts >= 3)
            {
                await _notificationService.BroadcastAdminAlertAsync("OTP_BLOCKED", new { ShipmentId = shipment.Id, OrderId = shipment.OrderId, DriverId = driver.Id, Reason = "Sender OTP max attempts breached" });
                throw new TooManyRequestsException("Maximum OTP verification attempts exceeded.");
            }

            throw new ValidationException("Incorrect OTP.");
        }

        shipment.SenderOtpAttempts = 0;
        shipment.Status = ShipmentStatus.IN_TRANSIT;
        shipment.StatusChangedBy = driver.UserId;
        shipment.StatusUpdatedAt = DateTime.UtcNow;
        shipment.UpdatedAt = DateTime.UtcNow;

        await _shipmentRepo.UpdateAsync(shipment);
        _logger.LogInformation("Pickup confirmed for Shipment {ShipmentId} ({OrderId}) by Driver {DriverId}. Status set to IN_TRANSIT.", shipment.Id, shipment.OrderId, driver.Id);

        await _notificationService.CreateNotificationAsync(shipment.CustomerId, shipment.Id, "Package Picked Up", $"Your package for shipment {shipment.OrderId} has been picked up and is in transit.");
        await _notificationService.BroadcastShipmentUpdateAsync(shipment.Id, "IN_TRANSIT", new { shipment.OrderId });

        return shipment.ToConfirmPickupResponse();
    }

    public async Task<ConfirmDeliveryResponse> ConfirmDeliveryAsync(Shipment shipment, Guid driverUserId, ConfirmDeliveryRequest request)
    {
        var driver = await _driverRepo.GetByUserIdAsync(driverUserId);
        if (driver == null)
            throw new NotFoundException("Driver profile not found.");

        if (shipment.Status != ShipmentStatus.IN_TRANSIT)
            throw new BusinessRuleException("Shipment must be in IN_TRANSIT status to confirm delivery.");

        var distance = _geoService.CalculateDistance(request.DriverLat, request.DriverLng, shipment.DropLat, shipment.DropLng);
        if (distance > 0.2)
        {
            throw new ValidationException("Driver is not within 200m of the delivery location.");
        }

        if (shipment.ReceiverOtpExpiresAt < DateTime.UtcNow)
        {
            throw new ValidationException("OTP has expired.");
        }

        if (shipment.ReceiverOtpAttempts >= 3)
        {
            throw new TooManyRequestsException("Maximum OTP verification attempts exceeded.");
        }

        if (!_otpService.VerifyOtp(request.Otp, shipment.ReceiverOtpHash ?? string.Empty))
        {
            shipment.ReceiverOtpAttempts++;
            await _shipmentRepo.UpdateAsync(shipment);

            if (shipment.ReceiverOtpAttempts >= 3)
            {
                await _notificationService.BroadcastAdminAlertAsync("OTP_BLOCKED", new { ShipmentId = shipment.Id, OrderId = shipment.OrderId, DriverId = driver.Id, Reason = "Receiver OTP max attempts breached" });
                throw new TooManyRequestsException("Maximum OTP verification attempts exceeded.");
            }

            throw new ValidationException("Incorrect OTP.");
        }

        shipment.ReceiverOtpAttempts = 0;
        shipment.Status = ShipmentStatus.DELIVERED;
        shipment.StatusChangedBy = driver.UserId;
        shipment.StatusUpdatedAt = DateTime.UtcNow;
        shipment.UpdatedAt = DateTime.UtcNow;

        driver.OperationalStatus = OperationalStatus.ONLINE;
        await _driverRepo.UpdateAsync(driver);

        await _shipmentRepo.UpdateAsync(shipment);
        _logger.LogInformation("Delivery confirmed for Shipment {ShipmentId} ({OrderId}) by Driver {DriverId}. Status set to DELIVERED.", shipment.Id, shipment.OrderId, driver.Id);

        await _notificationService.CreateNotificationAsync(shipment.CustomerId, shipment.Id, "Package Delivered", $"Your package for shipment {shipment.OrderId} has been delivered successfully.");
        await _notificationService.BroadcastShipmentUpdateAsync(shipment.Id, "DELIVERED", new { shipment.OrderId });

        return shipment.ToConfirmDeliveryResponse();
    }

    public async Task<CashCollectedResponse> ConfirmCashCollectedAsync(Shipment shipment, Guid driverUserId)
    {
        var driver = await _driverRepo.GetByUserIdAsync(driverUserId);
        if (driver == null)
            throw new NotFoundException("Driver profile not found.");

        if (shipment.Status != ShipmentStatus.DELIVERED)
            throw new BusinessRuleException("Cash can only be marked as collected after delivery is confirmed.");

        shipment.CashCollected = true;
        if (shipment.Payment != null)
        {
            shipment.Payment.Status = PaymentStatus.SUCCESS;
            shipment.Payment.UpdatedAt = DateTime.UtcNow;
        }
        shipment.UpdatedAt = DateTime.UtcNow;

        await _shipmentRepo.UpdateAsync(shipment);
        _logger.LogInformation("Cash collection confirmed for Shipment {ShipmentId} ({OrderId}) by Driver {DriverId}.", shipment.Id, shipment.OrderId, driver.Id);

        return shipment.ToCashCollectedResponse();
    }

    public async Task<PickupFailedResponse> MarkPickupFailedAsync(Shipment shipment, Guid driverUserId, PickupFailedRequest request)
    {
        var driver = await _driverRepo.GetByUserIdAsync(driverUserId);
        if (driver == null)
            throw new NotFoundException("Driver profile not found.");

        if (shipment.Status != ShipmentStatus.ASSIGNED)
            throw new BusinessRuleException("Shipment must be in ASSIGNED status to mark pickup as failed.");

        shipment.Status = ShipmentStatus.PICKUP_FAILED;
        shipment.StatusChangedBy = driver.UserId;
        shipment.StatusUpdatedAt = DateTime.UtcNow;
        shipment.UpdatedAt = DateTime.UtcNow;

        driver.OperationalStatus = OperationalStatus.ONLINE;
        await _driverRepo.UpdateAsync(driver);

        await _shipmentRepo.UpdateAsync(shipment);
        _logger.LogWarning("Pickup failed for Shipment {ShipmentId} ({OrderId}) by Driver {DriverId}. Reason: {Reason}", shipment.Id, shipment.OrderId, driver.Id, request.Reason);

        await _notificationService.CreateNotificationAsync(shipment.CustomerId, shipment.Id, "Pickup Failed", $"Pickup failed for shipment {shipment.OrderId}. Reason: {request.Reason}");
        await _notificationService.BroadcastShipmentUpdateAsync(shipment.Id, "PICKUP_FAILED", new { shipment.OrderId, Reason = request.Reason });

        return shipment.ToPickupFailedResponse();
    }

    public async Task<PublicTrackingResponse> GetPublicTrackingAsync(string orderId, string phone, string date)
    {
        if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(date))
        {
            throw new NotFoundException("Shipment not found.");
        }

        if (!DateTime.TryParse(date, out var parsedDate))
        {
            throw new NotFoundException("Shipment not found.");
        }

        var shipment = await _shipmentRepo.GetByPublicTrackParamsAsync(orderId, phone);
        if (shipment == null)
        {
            throw new NotFoundException("Shipment not found.");
        }

        var timeZoneId = OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata";
        var istZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localCreatedAt = TimeZoneInfo.ConvertTimeFromUtc(shipment.CreatedAt, istZone);

        if (parsedDate.Date != localCreatedAt.Date)
        {
            throw new NotFoundException("Shipment not found.");
        }

        PublicTrackingDriverDto? driverDto = null;
        if (shipment.Driver != null)
        {
            driverDto = new PublicTrackingDriverDto
            {
                FullName = shipment.Driver.User?.FullName ?? string.Empty,
                VehicleType = shipment.Driver.ActiveVehicle?.VehicleType,
                VehicleNumber = shipment.Driver.ActiveVehicle?.VehicleNumber ?? string.Empty
            };
        }

        DriverLocationDto? driverLocation = null;
        if (shipment.Status == ShipmentStatus.IN_TRANSIT && shipment.TrackingPings != null && shipment.TrackingPings.Any())
        {
            var latestPing = shipment.TrackingPings.OrderByDescending(p => p.RecordedAt).First();
            driverLocation = new DriverLocationDto
            {
                Latitude = latestPing.Latitude,
                Longitude = latestPing.Longitude,
                RecordedAt = latestPing.RecordedAt
            };
        }

        var timeline = new List<TimelineEntryDto>();
        bool isCod = shipment.Payment == null || shipment.Payment.Method == PaymentMethod.COD;

        timeline.Add(new TimelineEntryDto
        {
            Status = "Order Created",
            Description = "Shipment booking created.",
            Timestamp = shipment.CreatedAt,
            IsCompleted = true
        });

        if (isCod)
        {
            timeline.Add(new TimelineEntryDto
            {
                Status = "Payment Status",
                Description = "Cash on delivery selected; payment will be collected at delivery.",
                Timestamp = shipment.CreatedAt,
                IsCompleted = true
            });

            timeline.Add(new TimelineEntryDto
            {
                Status = "Driver Assigned",
                Description = shipment.Status >= ShipmentStatus.ASSIGNED 
                    ? $"Driver {(shipment.Driver?.User?.FullName ?? "assigned")} has been assigned." 
                    : "Awaiting driver assignment.",
                Timestamp = shipment.Status >= ShipmentStatus.ASSIGNED 
                    ? (shipment.Status == ShipmentStatus.ASSIGNED ? shipment.StatusUpdatedAt : null) 
                    : null,
                IsCompleted = shipment.Status >= ShipmentStatus.ASSIGNED
            });

            timeline.Add(new TimelineEntryDto
            {
                Status = "In Transit",
                Description = shipment.Status >= ShipmentStatus.IN_TRANSIT 
                    ? "Package picked up and in transit." 
                    : "Awaiting package pickup.",
                Timestamp = shipment.Status >= ShipmentStatus.IN_TRANSIT 
                    ? (shipment.Status == ShipmentStatus.IN_TRANSIT ? shipment.StatusUpdatedAt : null) 
                    : null,
                IsCompleted = shipment.Status >= ShipmentStatus.IN_TRANSIT
            });

            timeline.Add(new TimelineEntryDto
            {
                Status = "Delivered",
                Description = shipment.Status == ShipmentStatus.DELIVERED 
                    ? "Package successfully delivered." 
                    : "Awaiting delivery.",
                Timestamp = shipment.Status == ShipmentStatus.DELIVERED 
                    ? shipment.StatusUpdatedAt 
                    : null,
                IsCompleted = shipment.Status == ShipmentStatus.DELIVERED
            });
        }
        else
        {
            bool isPaid = shipment.Status >= ShipmentStatus.OPEN;
            timeline.Add(new TimelineEntryDto
            {
                Status = "Payment Status",
                Description = isPaid 
                    ? "Payment successfully processed." 
                    : "Awaiting payment confirmation.",
                Timestamp = isPaid 
                    ? (shipment.Payment?.UpdatedAt ?? shipment.StatusUpdatedAt ?? shipment.CreatedAt) 
                    : null,
                IsCompleted = isPaid
            });

            timeline.Add(new TimelineEntryDto
            {
                Status = "Driver Assigned",
                Description = shipment.Status >= ShipmentStatus.ASSIGNED 
                    ? $"Driver {(shipment.Driver?.User?.FullName ?? "assigned")} has been assigned." 
                    : "Awaiting driver assignment.",
                Timestamp = shipment.Status >= ShipmentStatus.ASSIGNED 
                    ? (shipment.Status == ShipmentStatus.ASSIGNED ? shipment.StatusUpdatedAt : null) 
                    : null,
                IsCompleted = shipment.Status >= ShipmentStatus.ASSIGNED
            });

            timeline.Add(new TimelineEntryDto
            {
                Status = "In Transit",
                Description = shipment.Status >= ShipmentStatus.IN_TRANSIT 
                    ? "Package picked up and in transit." 
                    : "Awaiting package pickup.",
                Timestamp = shipment.Status >= ShipmentStatus.IN_TRANSIT 
                    ? (shipment.Status == ShipmentStatus.IN_TRANSIT ? shipment.StatusUpdatedAt : null) 
                    : null,
                IsCompleted = shipment.Status >= ShipmentStatus.IN_TRANSIT
            });

            timeline.Add(new TimelineEntryDto
            {
                Status = "Delivered",
                Description = shipment.Status == ShipmentStatus.DELIVERED 
                    ? "Package successfully delivered." 
                    : "Awaiting delivery.",
                Timestamp = shipment.Status == ShipmentStatus.DELIVERED 
                    ? shipment.StatusUpdatedAt 
                    : null,
                IsCompleted = shipment.Status == ShipmentStatus.DELIVERED
            });
        }

        if (shipment.Status == ShipmentStatus.CANCELLED)
        {
            timeline.Add(new TimelineEntryDto
            {
                Status = "Cancelled",
                Description = "Shipment was cancelled.",
                Timestamp = shipment.StatusUpdatedAt,
                IsCompleted = true
            });
        }
        else if (shipment.Status == ShipmentStatus.PICKUP_FAILED)
        {
            timeline.Add(new TimelineEntryDto
            {
                Status = "Pickup Failed",
                Description = "Pickup failed.",
                Timestamp = shipment.StatusUpdatedAt,
                IsCompleted = true
            });
        }
        else if (shipment.Status == ShipmentStatus.STALE)
        {
            timeline.Add(new TimelineEntryDto
            {
                Status = "Stale",
                Description = "Shipment marked as stale.",
                Timestamp = shipment.StatusUpdatedAt,
                IsCompleted = true
            });
        }

        return shipment.ToPublicTrackingResponse(driverDto, driverLocation, timeline);
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

    public PriceEstimationResponse EstimatePrice(PriceEstimationRequest request)
    {
        decimal deliveryCharge = CalculateDeliveryCharge(request.PackageType, request.WeightKg, request.PickupLat, request.PickupLng, request.DropLat, request.DropLng);
        decimal cgst = Math.Round(deliveryCharge * 0.09m, 2);
        decimal sgst = Math.Round(deliveryCharge * 0.09m, 2);
        decimal totalAmount = deliveryCharge + cgst + sgst;

        return new PriceEstimationResponse
        {
            DeliveryCharge = deliveryCharge,
            Cgst = cgst,
            Sgst = sgst,
            TotalAmount = totalAmount
        };
    }

    private decimal CalculateDeliveryCharge(PackageType packageType, decimal weightKg, decimal pickupLat, decimal pickupLng, decimal dropLat, decimal dropLng)
    {
        double distanceKm = _geoService.CalculateDistance(pickupLat, pickupLng, dropLat, dropLng);

        decimal baseFare = packageType switch
        {
            PackageType.DOCUMENT => 40.00m,
            PackageType.SMALL_PARCEL => 60.00m,
            PackageType.LARGE_PARCEL => 100.00m,
            PackageType.FRAGILE => 120.00m,
            PackageType.HOUSEHOLD => 250.00m,
            _ => 50.00m
        };

        decimal perKmRate = 12.00m;
        decimal distanceCharge = (decimal)distanceKm * perKmRate;

        decimal weightSurcharge = 0m;
        if (weightKg > 2.0m)
        {
            weightSurcharge = (weightKg - 2.0m) * 5.00m;
        }

        decimal deliveryCharge = baseFare + distanceCharge + weightSurcharge;
        return Math.Round(deliveryCharge, 2);
    }
}
