using System.ComponentModel.DataAnnotations;

namespace Logistics.Api.DTOs;

public record RegisterCustomerRequest
{
    [Required]
    public string Auth0Id { get; init; } = string.Empty;

    [Required]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, Phone]
    public string Phone { get; init; } = string.Empty;
}

public record RegisterCustomerResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public record RegisterDriverRequest
{
    [Required]
    public string Auth0Id { get; init; } = string.Empty;

    [Required]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, Phone]
    public string Phone { get; init; } = string.Empty;

    [Required]
    public string LicenseNumber { get; init; } = string.Empty;
}

public record RegisterDriverResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string ApprovalStatus { get; init; } = string.Empty;
}
