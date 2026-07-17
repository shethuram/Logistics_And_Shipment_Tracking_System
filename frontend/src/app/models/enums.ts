export type VehicleType = 'TWO_WHEELER' | 'THREE_WHEELER' | 'FOUR_WHEELER' | 'HEAVY_VEHICLE';

export type ShipmentStatus = 
  | 'PENDING_PAYMENT'
  | 'OPEN'
  | 'ASSIGNED'
  | 'IN_TRANSIT'
  | 'DELIVERED'
  | 'CANCELLED'
  | 'PICKUP_FAILED'
  | 'STALE';

export type PackageType = 
  | 'DOCUMENT'
  | 'SMALL_PARCEL'
  | 'LARGE_PARCEL'
  | 'FRAGILE'
  | 'HOUSEHOLD';

export type PreferredWindow = 'MORNING' | 'AFTERNOON' | 'EVENING';

export type PaymentMethod = 'COD' | 'ONLINE';

export type DisputeStatus = 'OPEN' | 'RESOLVED' | 'ESCALATED';

export type DisputeLlmType = 'WRONG_ADDRESS' | 'LATE_DELIVERY' | 'DAMAGED_PACKAGE' | 'DRIVER_BEHAVIOUR';

export type DriverApprovalStatus = 'PENDING' | 'APPROVED' | 'REJECTED' | 'SUSPENDED';

export type OperationalStatus = 'ONLINE' | 'OFFLINE' | 'ON_DELIVERY';

