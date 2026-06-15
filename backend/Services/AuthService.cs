using Logistics.Api.Data;
using Logistics.Api.DTOs;
using Logistics.Api.Exceptions;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Interfaces.Services;
using Logistics.Api.Mappings;
using Logistics.Api.Models;

namespace Logistics.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly AppDbContext _db;

    public AuthService(IUserRepository userRepo, IDriverRepository driverRepo, AppDbContext db)
    {
        _userRepo = userRepo;
        _driverRepo = driverRepo;
        _db = db;
    }

    public async Task<RegisterCustomerResponse> RegisterCustomerAsync(RegisterCustomerRequest request)
    {
        if (await _userRepo.ExistsByAuth0IdAsync(request.Auth0Id))
            throw new ConflictException("A user with this Auth0 ID already exists.");

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

    public async Task<RegisterDriverResponse> RegisterDriverAsync(RegisterDriverRequest request)
    {
        if (await _userRepo.ExistsByAuth0IdAsync(request.Auth0Id))
            throw new ConflictException("A user with this Auth0 ID already exists.");

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
                ApprovalStatus = ApprovalStatus.PENDING
            };

            await _driverRepo.AddAsync(driver);

            await transaction.CommitAsync();

            return user.ToRegisterDriverResponse(driver);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
