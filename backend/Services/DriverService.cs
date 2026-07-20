using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Data;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Mappings;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace Logistics.Api.Services;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _driverRepo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DriverService> _logger;

    public DriverService(
        IDriverRepository driverRepo, 
        IVehicleRepository vehicleRepo,
        IUserRepository userRepo,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<DriverService> logger)
    {
        _driverRepo = driverRepo;
        _vehicleRepo = vehicleRepo;
        _userRepo = userRepo;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<GoOnlineResponse> GoOnlineAsync(Guid driverId, GoOnlineRequest request)
    {
        var driver = await GetOrThrowAsync(driverId);

        if (driver.ApprovalStatus != ApprovalStatus.APPROVED)
            throw new BusinessRuleException("Driver must be approved before going online.");

        if (driver.ActiveVehicleId is null)
            throw new ValidationException("Driver has no active vehicle set.");

        var activeVehicle = await _vehicleRepo.GetByIdAsync(driver.ActiveVehicleId.Value);
        if (activeVehicle is null)
            throw new ValidationException("Driver has no active vehicle set.");

        driver.OperationalStatus = OperationalStatus.ONLINE;
        driver.CurrentLat = request.Latitude;
        driver.CurrentLng = request.Longitude;
        driver.LastPingAt = DateTime.UtcNow;

        await _driverRepo.UpdateAsync(driver);

        return driver.ToGoOnlineResponse(activeVehicle);
    }

    public async Task<GoOfflineResponse> GoOfflineAsync(Guid driverId)
    {
        var driver = await GetOrThrowAsync(driverId);

        driver.OperationalStatus = OperationalStatus.OFFLINE;

        await _driverRepo.UpdateAsync(driver);

        return driver.ToGoOfflineResponse();
    }

    public async Task UpdateDriverProfileAsync(Guid driverId, UpdateDriverProfileRequest request, IFormFile? licenseFile)
    {
        var driver = await _driverRepo.GetByIdWithUserAndVehiclesAsync(driverId);
        if (driver == null)
            throw new NotFoundException("Driver profile not found.");
        
        // Update user profile details
        if (driver.User != null)
        {
            driver.User.FullName = request.FullName;
            driver.User.Phone = request.Phone;
            await _userRepo.UpdateAsync(driver.User);
        }

        // Update driver details
        driver.LicenseNumber = request.LicenseNumber;

        // If a new license file was uploaded, save it and update the URL
        if (licenseFile != null && licenseFile.Length > 0)
        {
            var fileUrl = await SaveLicenseFileAsync(licenseFile);
            driver.LicenseFileUrl = fileUrl;
        }

        // Reset verification status to PENDING as license details changed
        driver.VerificationStatus = VerificationStatus.PENDING;
        driver.ApprovalStatus = ApprovalStatus.PENDING;
        driver.VerificationReport = null;
        driver.LicenseClasses = null;
        driver.AllowedVehicleTypes = null;

        await _driverRepo.UpdateAsync(driver);

        // Trigger agent check asynchronously in the background thread (fire-and-forget)
        _ = Task.Run(() => TriggerAgentVerificationAsync(driver.Id, driver.LicenseFileUrl!));
    }

    private async Task<string> SaveLicenseFileAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "licenses");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/licenses/{fileName}";
    }

    private async Task TriggerAgentVerificationAsync(Guid driverId, string licenseFileUrl)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var driver = await db.Drivers
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == driverId);

            if (driver == null) return;

            var agentUrl = _configuration["AgentSettings:AgentUrl"] ?? "http://localhost:8000/api/agent/verify";
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5286";
            var absoluteImageUrl = $"{baseUrl.TrimEnd('/')}{licenseFileUrl}";

            using var client = new HttpClient();
            var payload = new
            {
                driverId = driver.Id,
                imageUrl = absoluteImageUrl,
                fullName = driver.User?.FullName ?? string.Empty,
                licenseNumber = driver.LicenseNumber
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(agentUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to trigger AI agent for driver {DriverId}. Status code: {StatusCode}", driverId, response.StatusCode);
            }
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException sex && (sex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused || sex.NativeErrorCode == 10061))
        {
            _logger.LogWarning("AI Agent is offline. Falling back to manual comparison and approval for Driver {DriverId}.", driverId);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var adminDriverService = scope.ServiceProvider.GetRequiredService<IAdminDriverService>();
                await adminDriverService.UpdateDriverVerificationAsync(driverId, new UpdateDriverVerificationRequest
                {
                    VerificationStatus = VerificationStatus.AGENT_FAILED,
                    VerificationReport = "AI Agent is offline. Falling back to manual comparison and approval."
                });
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "CRITICAL ERROR: Failed to update driver verification status to AGENT_FAILED for offline agent context of driver {DriverId}", driverId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while triggering AI agent for driver {DriverId}", driverId);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var adminDriverService = scope.ServiceProvider.GetRequiredService<IAdminDriverService>();
                await adminDriverService.UpdateDriverVerificationAsync(driverId, new UpdateDriverVerificationRequest
                {
                    VerificationStatus = VerificationStatus.AGENT_FAILED,
                    VerificationReport = $"Agent connection failed: {ex.Message}"
                });
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "CRITICAL ERROR: Failed to update driver verification status to AGENT_FAILED for exception context of driver {DriverId}", driverId);
            }
        }
    }

    private async Task<Driver> GetOrThrowAsync(Guid id)
    {
        var driver = await _driverRepo.GetByIdAsync(id);
        if (driver is null)
            throw new NotFoundException("Driver not found.");
        return driver;
    }
}
