import { DriverApprovalStatus } from './enums';

export interface DriverModel {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  phone: string;
  licenseNumber: string;
  approvalStatus: DriverApprovalStatus;
  penaltyCount: number;
}
