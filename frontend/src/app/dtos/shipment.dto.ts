import { VehicleType, ShipmentStatus, PackageType, PreferredWindow, PaymentMethod } from '../models/enums';

export interface CreateShipmentRequest {
  pickupAddress: string;
  pickupLat: number;
  pickupLng: number;
  dropAddress: string;
  dropLat: number;
  dropLng: number;
  receiverName: string;
  receiverPhone: string;
  packageType: PackageType;
  weightKg: number;
  preferredWindow?: PreferredWindow;
  specialNotes?: string;
  paymentMethod: PaymentMethod;
}

export interface CreateShipmentResponse {
  id: string;
  orderId: string;
  status: ShipmentStatus;
  paymentUrl?: string;
}

export interface ShipmentDriverDto {
  id: string;
  fullName: string;
  vehicleType?: VehicleType;
  vehicleNumber: string;
}

export interface ShipmentResponse {
  id: string;
  orderId: string;
  status: ShipmentStatus;
  pickupAddress: string;
  pickupLat: number;
  pickupLng: number;
  dropAddress: string;
  dropLat: number;
  dropLng: number;
  receiverName: string;
  receiverPhone: string;
  packageType: PackageType;
  weightKg: number;
  preferredWindow?: PreferredWindow;
  specialNotes?: string;
  driverInstruction?: string;
  riskFlag: boolean;
  riskSeverity: 'HIGH' | 'LOW' | 'NONE';
  riskReason?: string;
  preferredDeliveryAfter?: string;
  driver?: ShipmentDriverDto;
  cashCollected: boolean;
  statusChangedBy?: string;
  statusUpdatedAt?: string;
  createdAt: string;
  updatedAt: string;

  deliveryCharge: number;
  platformFee: number;
  cgst: number;
  sgst: number;
  totalAmount: number;
  driverEarnings: number;
  paymentMethod: string;
  paymentStatus: string;
}

export interface AvailableShipmentDto {
  id: string;
  orderId: string;
  pickupAddress: string;
  dropAddress: string;
  packageType: PackageType;
  weightKg: number;
  preferredWindow?: PreferredWindow;
  senderPhone: string;
  receiverPhone: string;
  distanceToPickupKm: number;
  driverInstruction?: string;
  driverEarnings: number;
}

export interface PriceEstimationRequest {
  pickupLat: number;
  pickupLng: number;
  dropLat: number;
  dropLng: number;
  packageType: PackageType;
  weightKg: number;
}

export interface PriceEstimationResponse {
  deliveryCharge: number;
  platformFee: number;
  cgst: number;
  sgst: number;
  totalAmount: number;
}

export interface UpdateShipmentRequest {
  preferredWindow?: PreferredWindow;
  specialNotes?: string;
}
