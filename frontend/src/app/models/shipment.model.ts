import { ShipmentStatus, PackageType, PreferredWindow } from './enums';

export interface ShipmentModel {
  id: string;
  orderId: string;
  status: ShipmentStatus;
  pickupAddress: string;
  dropAddress: string;
  packageType: PackageType;
  weightKg: number;
  preferredWindow?: PreferredWindow;
  createdAt: string;
}
