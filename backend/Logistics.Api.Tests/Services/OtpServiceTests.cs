using System;
using System.Collections.Generic;
using Logistics.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class OtpServiceTests
{
    private readonly OtpService _otpService;
    private const string Secret = "TEST_SECRET_KEY_FOR_DETERMINISTIC_OTP";

    public OtpServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "OtpSettings:Secret", Secret }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _otpService = new OtpService(config);
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

    [Fact]
    public void GenerateDeterministicOtp_SameInputs_ReturnsSameOtp()
    {

        var shipmentId = Guid.NewGuid();
        var salt = "receiver";

        var otp1 = _otpService.GenerateDeterministicOtp(shipmentId, salt);
        var otp2 = _otpService.GenerateDeterministicOtp(shipmentId, salt);

        Assert.Equal(otp1, otp2);
        Assert.Equal(4, otp1.Length);
        Assert.True(int.TryParse(otp1, out _));
    }

    [Fact]
    public void GenerateDeterministicOtp_DifferentInputs_ReturnsDifferentOtp()
    {

        var shipmentId1 = Guid.NewGuid();
        var shipmentId2 = Guid.NewGuid();
        var salt = "receiver";

        var otp1 = _otpService.GenerateDeterministicOtp(shipmentId1, salt);
        var otp2 = _otpService.GenerateDeterministicOtp(shipmentId2, salt);


        Assert.True(shipmentId1 == shipmentId2 || otp1 != otp2);
    }
}
