import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { VehicleResponse, RegisterVehicleRequest, SetActiveVehicleResponse } from '../dtos/vehicle.dto';

@Injectable({
  providedIn: 'root'
})
export class DriverApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl;

  goOnline(latitude: number, longitude: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/drivers/go-online`, { latitude, longitude });
  }

  goOffline(): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/drivers/go-offline`, {});
  }

  getVehicles(): Observable<VehicleResponse[]> {
    return this.http.get<VehicleResponse[]>(`${this.baseUrl}/api/drivers/vehicles`);
  }

  registerVehicle(request: RegisterVehicleRequest): Observable<VehicleResponse> {
    return this.http.post<VehicleResponse>(`${this.baseUrl}/api/drivers/vehicles`, request);
  }

  activateVehicle(id: string): Observable<SetActiveVehicleResponse> {
    return this.http.post<SetActiveVehicleResponse>(`${this.baseUrl}/api/drivers/vehicles/${id}/set-active`, {});
  }
}
