namespace Logistics.Api.Interfaces.Services;

public interface IOtpService
{
    string GenerateOtp();
    string HashOtp(string otp);
    bool VerifyOtp(string otp, string hashedOtp);
    string GenerateDeterministicOtp(Guid shipmentId, string salt);
}
