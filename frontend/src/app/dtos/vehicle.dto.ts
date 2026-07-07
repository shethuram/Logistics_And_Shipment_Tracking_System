import { VehicleType } from '../models/enums';

export interface VehicleResponse {
  id: string;
  vehicleType: VehicleType;
  vehicleNumber: string;
  isActive: boolean;
  createdAt: string;
}

export interface RegisterVehicleRequest {
  vehicleType: VehicleType;
  vehicleNumber: string;
}

export interface SetActiveVehicleResponse {
  activeVehicleId: string;
  vehicleType: VehicleType;
}
