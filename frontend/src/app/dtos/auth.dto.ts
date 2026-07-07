import { DriverApprovalStatus } from '../models/enums';

export interface CustomerRegistrationDto {
  auth0Id: string;
  fullName: string;
  email: string;
  phone: string;
}

export interface DriverRegistrationDto {
  auth0Id: string;
  fullName: string;
  email: string;
  phone: string;
  licenseNumber: string;
}

export interface DriverDetailsDto {
  approvalStatus: DriverApprovalStatus;
  approvalReason: string | null;
  operationalStatus?: 'ONLINE' | 'OFFLINE';
  activeVehicleId?: string | null;
}

export interface UserProfileDto {
  isAuthenticated: boolean;
  isRegistered: boolean;
  userId?: string;
  fullName?: string;
  email?: string;
  role?: 'CUSTOMER' | 'DRIVER' | 'ADMIN';
  driver?: DriverDetailsDto;
}
