using System.ComponentModel.DataAnnotations;
using Logistics.Api.Models;

namespace Logistics.Api.DTOs;

public record RegisterCustomerRequest
{
    [Required]
    public string Auth0Id { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
    public string Phone { get; init; } = string.Empty;
}

public record RegisterCustomerResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
}

public record RegisterDriverRequest
{
    [Required]
    public string Auth0Id { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
    public string Phone { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^[A-Z]{2}[0-9]{2}[ ][0-9]{11}$", ErrorMessage = "Invalid License Number format. Must match SSRR YYYYNNNNNNN format (e.g. KA03 20150012345).")]
    public string LicenseNumber { get; init; } = string.Empty;
}

public record RegisterDriverResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; init; }
}
