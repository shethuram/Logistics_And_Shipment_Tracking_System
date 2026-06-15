namespace Logistics.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Auth0Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Driver? Driver { get; set; }
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Dispute> RaisedDisputes { get; set; } = new List<Dispute>();
    public ICollection<Dispute> ResolvedDisputes { get; set; } = new List<Dispute>();
}
