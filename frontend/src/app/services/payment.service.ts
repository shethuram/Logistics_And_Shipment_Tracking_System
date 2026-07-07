import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { InitiatePaymentRequest, InitiatePaymentResponse, PaymentStatusResponse } from '../dtos/payment.dto';

@Injectable({
  providedIn: 'root'
})
export class PaymentApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl;

  initiatePayment(request: InitiatePaymentRequest): Observable<InitiatePaymentResponse> {
    return this.http.post<InitiatePaymentResponse>(`${this.baseUrl}/api/payments/initiate`, request);
  }

  getPaymentStatus(shipmentId: string): Observable<PaymentStatusResponse> {
    return this.http.get<PaymentStatusResponse>(`${this.baseUrl}/api/payments/${shipmentId}/status`);
  }
}
