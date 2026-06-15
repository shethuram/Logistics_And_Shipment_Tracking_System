namespace Logistics.Api.Models;

public enum UserRole
{
    CUSTOMER,
    DRIVER,
    ADMIN
}

public enum ApprovalStatus
{
    PENDING,
    APPROVED,
    REJECTED,
    SUSPENDED
}

public enum OperationalStatus
{
    ONLINE,
    OFFLINE,
    ON_DELIVERY
}

public enum VehicleType
{
    TWO_WHEELER,
    THREE_WHEELER,
    FOUR_WHEELER,
    HEAVY_VEHICLE
}

public enum ShipmentStatus
{
    PENDING_PAYMENT,
    OPEN,
    ASSIGNED,
    IN_TRANSIT,
    DELIVERED,
    CANCELLED,
    PICKUP_FAILED,
    STALE
}

public enum PackageType
{
    DOCUMENT,
    SMALL_PARCEL,
    LARGE_PARCEL,
    FRAGILE,
    HOUSEHOLD
}

public enum PreferredWindow
{
    MORNING,
    AFTERNOON,
    EVENING
}

public enum RiskSeverity
{
    HIGH,
    LOW,
    NONE
}

public enum PaymentMethod
{
    COD,
    ONLINE
}

public enum PaymentStatus
{
    PENDING,
    SUCCESS,
    FAILED,
    REFUNDED
}

public enum DisputeLlmType
{
    WRONG_ADDRESS,
    LATE_DELIVERY,
    DAMAGED_PACKAGE,
    DRIVER_BEHAVIOUR
}

public enum DisputeStatus
{
    OPEN,
    RESOLVED,
    ESCALATED
}
