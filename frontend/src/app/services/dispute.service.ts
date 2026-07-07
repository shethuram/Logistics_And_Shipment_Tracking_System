import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { RaiseDisputeRequest, RaiseDisputeResponse, DisputeResponse } from '../dtos/dispute.dto';

@Injectable({
  providedIn: 'root'
})
export class DisputeApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl;

  raiseDispute(request: RaiseDisputeRequest): Observable<RaiseDisputeResponse> {
    return this.http.post<RaiseDisputeResponse>(`${this.baseUrl}/api/disputes`, request);
  }

  getMyDisputes(): Observable<DisputeResponse[]> {
    return this.http.get<DisputeResponse[]>(`${this.baseUrl}/api/disputes/my`);
  }
}
