using System;
using System.Collections.Generic;
using Logistics.Api.Services;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class OtpServiceTests
{
    private readonly OtpService _otpService;

    public OtpServiceTests()
    {
        _otpService = new OtpService();
    }

    [Fact]
    public void GenerateOtp_ReturnsFourDigitNumericString()
    {
        var otp = _otpService.GenerateOtp();

        Assert.NotNull(otp);
        Assert.Equal(4, otp.Length);
        Assert.True(int.TryParse(otp, out _));
    }

    [Fact]
    public void HashAndVerifyOtp_CorrectOtp_ReturnsTrue()
    {
        var otp = "1234";

        var hash = _otpService.HashOtp(otp);
        var result = _otpService.VerifyOtp(otp, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyOtp_IncorrectOtp_ReturnsFalse()
    {
        var otp = "1234";
        var wrongOtp = "5678";

        var hash = _otpService.HashOtp(otp);
        var result = _otpService.VerifyOtp(wrongOtp, hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyOtp_NullOrEmpty_ReturnsFalse()
    {
        var hash = _otpService.HashOtp("1234");

        Assert.False(_otpService.VerifyOtp("", hash));
        Assert.False(_otpService.VerifyOtp(null!, hash));
        Assert.False(_otpService.VerifyOtp("1234", ""));
        Assert.False(_otpService.VerifyOtp("1234", null!));
    }
}
