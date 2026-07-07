export interface TimelineEntryDto {
  status: string;
  description: string;
  timestamp: string | null;
  isCompleted: boolean;
}

export interface PublicTrackingDriverDto {
  fullName: string;
  vehicleNumber: string;
  phone: string;
}

export interface DriverLocationDto {
  latitude: number;
  longitude: number;
  recordedAt: string;
}

export interface PublicTrackingResponseDto {
  orderId: string;
  status: string;
  pickupAddress: string;
  dropAddress: string;
  timeline: TimelineEntryDto[];
  driver?: PublicTrackingDriverDto;
  liveLocation?: DriverLocationDto;
}

export interface LiveTrackingResponse {
  shipmentId: string;
  driverLocation: DriverLocationDto | null;
}
