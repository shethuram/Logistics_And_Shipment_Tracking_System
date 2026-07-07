import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CustomerRegistrationDto, DriverRegistrationDto, UserProfileDto } from '../dtos/auth.dto';

@Injectable({
  providedIn: 'root'
})
export class AuthApiService {
  private http = inject(HttpClient);

  getProfile(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${environment.apiBaseUrl}/api/auth/me`);
  }

  registerCustomer(payload: CustomerRegistrationDto): Observable<any> {
    return this.http.post<any>(`${environment.apiBaseUrl}/api/auth/register/customer`, payload);
  }

  registerDriver(payload: DriverRegistrationDto): Observable<any> {
    return this.http.post<any>(`${environment.apiBaseUrl}/api/auth/register/driver`, payload);
  }
}
