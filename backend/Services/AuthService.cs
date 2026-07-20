using Logistics.Api.Data;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Mappings;
using Logistics.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace Logistics.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepo, 
        IDriverRepository driverRepo, 
        AppDbContext db,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        INotificationService notificationService,
        ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _driverRepo = driverRepo;
        _db = db;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<RegisterCustomerResponse> RegisterCustomerAsync(RegisterCustomerRequest request)
    {
        if (await _userRepo.ExistsByAuth0IdAsync(request.Auth0Id))
            throw new ConflictException("A user with this Auth0 ID already exists.");

        if (await _userRepo.ExistsByEmailAsync(request.Email))
            throw new ConflictException("A user with this email already exists.");

        if (await _userRepo.ExistsByPhoneAsync(request.Phone))
            throw new ConflictException("A user with this phone number already exists.");

        var user = new User
        {
            Auth0Id = request.Auth0Id,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Role = UserRole.CUSTOMER
        };

        await _userRepo.AddAsync(user);

        return user.ToRegisterCustomerResponse();
    }

    public async Task<RegisterDriverResponse> RegisterDriverAsync(RegisterDriverRequest request, IFormFile licenseFile)
    {
        if (await _userRepo.ExistsByAuth0IdAsync(request.Auth0Id))
            throw new ConflictException("A user with this Auth0 ID already exists.");

        if (await _userRepo.ExistsByEmailAsync(request.Email))
            throw new ConflictException("A user with this email already exists.");

        if (await _userRepo.ExistsByPhoneAsync(request.Phone))
            throw new ConflictException("A user with this phone number already exists.");

        var fileUrl = await SaveLicenseFileAsync(licenseFile);

        var user = new User
        {
            Auth0Id = request.Auth0Id,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Role = UserRole.DRIVER
        };

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            await _userRepo.AddAsync(user);

            var driver = new Driver
            {
                UserId = user.Id,
                LicenseNumber = request.LicenseNumber,
                ApprovalStatus = ApprovalStatus.PENDING,
                LicenseFileUrl = fileUrl,
                VerificationStatus = VerificationStatus.PENDING
            };

            await _driverRepo.AddAsync(driver);

            await transaction.CommitAsync();

            await _notificationService.CreateAdminNotificationAsync(null, "New Driver Registration", $"{user.FullName} has registered as a driver.");
            await _notificationService.BroadcastAdminAlertAsync("NEW_DRIVER_REGISTRATION", new { DriverId = driver.Id, FullName = user.FullName });

            // Trigger the AI Agent check in the background thread (fire-and-forget)
            _ = Task.Run(() => TriggerAgentVerificationAsync(driver.Id, driver.LicenseFileUrl));

            return user.ToRegisterDriverResponse(driver);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<string> SaveLicenseFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ValidationException("License file is required.");

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
}
