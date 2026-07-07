import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { LiveTrackingResponse } from '../dtos/tracking.dto';

@Injectable({
  providedIn: 'root'
})
export class TrackingApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl;

  getLiveLocation(shipmentId: string): Observable<LiveTrackingResponse> {
    return this.http.get<LiveTrackingResponse>(`${this.baseUrl}/api/tracking/${shipmentId}/live`);
  }

  recordLocation(request: { shipmentId: string; latitude: number; longitude: number }): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/tracking/location`, request);
  }
}
