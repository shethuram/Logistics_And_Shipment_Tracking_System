import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AdminMetricsResponse,
  PendingDriverDto,
  ApproveDriverResponse,
  DriverApprovalResponse,
  ReassignShipmentResponse,
  PagedResult,
  AdminDriverDto
} from '../dtos/admin.dto';
import { ShipmentResponse } from '../dtos/shipment.dto';
import { DisputeResponse } from '../dtos/dispute.dto';
import { DriverLocationDto } from '../dtos/tracking.dto';
import { DisputeStatus, ShipmentStatus, DriverApprovalStatus } from '../models/enums';

@Injectable({
  providedIn: 'root'
})
export class AdminApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl;

  getMetrics(): Observable<AdminMetricsResponse> {
    return this.http.get<AdminMetricsResponse>(`${this.baseUrl}/api/admin/metrics`);
  }

  getPendingDrivers(page: number = 1, pageSize: number = 20): Observable<PagedResult<PendingDriverDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<PendingDriverDto>>(`${this.baseUrl}/api/admin/drivers/pending`, { params });
  }

  approveDriver(id: string): Observable<ApproveDriverResponse> {
    return this.http.post<ApproveDriverResponse>(`${this.baseUrl}/api/admin/drivers/${id}/approve`, {});
  }

  rejectDriver(id: string, reason: string): Observable<DriverApprovalResponse> {
    return this.http.post<DriverApprovalResponse>(`${this.baseUrl}/api/admin/drivers/${id}/reject`, { reason });
  }

  suspendDriver(id: string, reason: string): Observable<DriverApprovalResponse> {
    return this.http.post<DriverApprovalResponse>(`${this.baseUrl}/api/admin/drivers/${id}/suspend`, { reason });
  }

  getAdminShipments(filters: {
    search?: string;
    status?: ShipmentStatus;
    dateFrom?: string;
    dateTo?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<ShipmentResponse>> {
    let params = new HttpParams();
    if (filters.search) params = params.set('search', filters.search);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters.dateTo) params = params.set('dateTo', filters.dateTo);
    if (filters.page) params = params.set('page', filters.page.toString());
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http.get<PagedResult<ShipmentResponse>>(`${this.baseUrl}/api/admin/shipments`, { params });
  }

  reassignShipment(id: string): Observable<ReassignShipmentResponse> {
    return this.http.post<ReassignShipmentResponse>(`${this.baseUrl}/api/admin/shipments/${id}/reassign`, {});
  }

  exportShipmentsCsv(filters: {
    status?: ShipmentStatus;
    dateFrom?: string;
    dateTo?: string;
  }): Observable<Blob> {
    let params = new HttpParams();
    if (filters.status) params = params.set('status', filters.status);
    if (filters.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters.dateTo) params = params.set('dateTo', filters.dateTo);

    return this.http.get(`${this.baseUrl}/api/admin/export/shipments`, {
      params,
      responseType: 'blob'
    });
  }

  getDisputes(status?: DisputeStatus, page: number = 1, pageSize: number = 20): Observable<PagedResult<DisputeResponse>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (status) params = params.set('status', status);

    return this.http.get<PagedResult<DisputeResponse>>(`${this.baseUrl}/api/admin/disputes`, { params });
  }

  resolveDispute(id: string, resolutionText: string): Observable<DisputeResponse> {
    return this.http.post<DisputeResponse>(`${this.baseUrl}/api/admin/disputes/${id}/resolve`, { resolutionText });
  }

  getDrivers(status?: DriverApprovalStatus, page: number = 1, pageSize: number = 20): Observable<PagedResult<AdminDriverDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (status) params = params.set('status', status);

    return this.http.get<PagedResult<AdminDriverDto>>(`${this.baseUrl}/api/admin/drivers`, { params });
  }

  getDriverById(id: string): Observable<AdminDriverDto> {
    return this.http.get<AdminDriverDto>(`${this.baseUrl}/api/admin/drivers/${id}`);
  }

  getTrackingHistory(shipmentId: string): Observable<DriverLocationDto[]> {
    return this.http.get<DriverLocationDto[]>(`${this.baseUrl}/api/tracking/${shipmentId}/history`);
  }
}
