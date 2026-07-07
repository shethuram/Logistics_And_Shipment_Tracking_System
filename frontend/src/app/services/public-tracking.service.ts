import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PublicTrackingResponseDto } from '../dtos/tracking.dto';

@Injectable({
  providedIn: 'root'
})
export class PublicTrackingService {
  private http = inject(HttpClient);

  trackShipment(orderId: string, phone: string, date: string): Observable<PublicTrackingResponseDto> {
    return this.http.get<PublicTrackingResponseDto>(`${environment.apiBaseUrl}/api/public/track`, {
      params: { orderId, phone, date }
    });
  }
}
