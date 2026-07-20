using Logistics.Api.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Logistics.Api.Services;

public class OtpService : IOtpService
{

    public string GenerateOtp()
    {
        var num = Random.Shared.Next(0, 10000);
        return num.ToString("D4");
    }

    public string HashOtp(string otp)
    {
        return BCrypt.Net.BCrypt.HashPassword(otp);
    }

    public bool VerifyOtp(string otp, string hashedOtp)
    {
        if (string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(hashedOtp))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(otp, hashedOtp);
        }
        catch
        {
            return false;
        }
    }
}
