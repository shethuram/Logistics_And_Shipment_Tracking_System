import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateShipmentRequest, CreateShipmentResponse, ShipmentResponse, AvailableShipmentDto } from '../dtos/shipment.dto';

@Injectable({
  providedIn: 'root'
})
export class ShipmentApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl;

  createShipment(request: CreateShipmentRequest): Observable<CreateShipmentResponse> {
    return this.http.post<CreateShipmentResponse>(`${this.baseUrl}/api/shipments`, request);
  }

  getShipment(id: string): Observable<ShipmentResponse> {
    return this.http.get<ShipmentResponse>(`${this.baseUrl}/api/shipments/${id}`);
  }

  getShipments(filters?: {
    search?: string;
    status?: string;
    dateFrom?: string;
    dateTo?: string;
    page?: number;
    pageSize?: number;
  }): Observable<{ data: ShipmentResponse[]; total: number; page: number; pageSize: number }> {
    let params = new HttpParams();
    if (filters) {
      if (filters.search) params = params.set('search', filters.search);
      if (filters.status) params = params.set('status', filters.status);
      if (filters.dateFrom) params = params.set('dateFrom', filters.dateFrom);
      if (filters.dateTo) params = params.set('dateTo', filters.dateTo);
      if (filters.page) params = params.set('page', filters.page.toString());
      if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());
    }
    return this.http.get<{ data: ShipmentResponse[]; total: number; page: number; pageSize: number }>(`${this.baseUrl}/api/shipments`, { params });
  }

  cancelShipment(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/api/shipments/${id}`);
  }

  updateShipment(id: string, request: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/api/shipments/${id}`, request);
  }

  getAvailableShipments(): Observable<AvailableShipmentDto[]> {
    return this.http.get<AvailableShipmentDto[]>(`${this.baseUrl}/api/shipments/available`);
  }

  getActiveShipment(): Observable<ShipmentResponse> {
    return this.http.get<ShipmentResponse>(`${this.baseUrl}/api/shipments/active`);
  }

  claimShipment(id: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/shipments/${id}/claim`, {});
  }

  cancelClaim(id: string, reason: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/shipments/${id}/cancel-claim`, { reason });
  }

  confirmPickup(id: string, otp: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/shipments/${id}/confirm-pickup`, { otp });
  }

  confirmDelivery(id: string, otp: string, driverLat: number, driverLng: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/shipments/${id}/confirm-delivery`, { otp, driverLat, driverLng });
  }

  confirmCashCollected(id: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/shipments/${id}/cash-collected`, {});
  }

  markPickupFailed(id: string, reason: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/shipments/${id}/pickup-failed`, { reason });
  }
}
