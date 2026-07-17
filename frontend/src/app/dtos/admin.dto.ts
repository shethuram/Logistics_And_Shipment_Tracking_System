import { VehicleType, ShipmentStatus, PackageType, PreferredWindow, DriverApprovalStatus } from '../models/enums';

export interface AdminMetricsResponse {
  totalShipments: number;
  delivered: number;
  pending: number;
  cancelled: number;
  failed: number;
  avgDeliveryTimeMinutes: number;
  staleShipments: number;
  codPending: number;
  driversOnline: number;
  driversWithHighCancelCount: number;
  totalRevenue: number;
  totalDriverEarnings: number;
  totalPlatformFees: number;
  totalTaxCollected: number;
}

export interface PendingDriverVehicleDto {
  vehicleType: VehicleType;
  vehicleNumber: string;
}

export interface PendingDriverDto {
  id: string;
  fullName: string;
  phone: string;
  licenseNumber: string;
  vehicles: PendingDriverVehicleDto[];
  createdAt: string;
}

export interface ApproveDriverResponse {
  id: string;
  approvalStatus: DriverApprovalStatus;
  approvedAt?: string;
}

export interface RejectDriverRequest {
  reason: string;
}

export interface SuspendDriverRequest {
  reason: string;
}

export interface DriverApprovalResponse {
  id: string;
  approvalStatus: DriverApprovalStatus;
  approvalReason?: string;
}

export interface ReassignShipmentResponse {
  id: string;
  status: ShipmentStatus;
  driverId?: string;
}

export interface ResolveDisputeRequest {
  resolutionText: string;
}

export interface PagedResult<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AdminDriverDto {
  id: string;
  fullName: string;
  phone: string;
  licenseNumber: string;
  approvalStatus: DriverApprovalStatus;
  operationalStatus: string;
  vehicles: PendingDriverVehicleDto[];
  cancelCount: number;
  createdAt: string;
}
