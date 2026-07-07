import { VehicleType } from './enums';

export interface VehicleModel {
  id: string;
  driverId: string;
  vehicleType: VehicleType;
  vehicleNumber: string;
  isActive: boolean;
}
