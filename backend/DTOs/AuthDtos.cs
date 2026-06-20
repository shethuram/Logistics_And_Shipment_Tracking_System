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
    [RegularExpression(@"^[A-Z]{2}-\d{2}-[A-Z]{1,2}-\d{4}$", ErrorMessage = "Invalid License Number. Format must be XX-XX-X-XXXX or XX-XX-XX-XXXX.")]
    public string LicenseNumber { get; init; } = string.Empty;
}

public record RegisterDriverResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; init; }
}
