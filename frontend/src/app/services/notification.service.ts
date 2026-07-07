import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { MyNotificationsResponse, MarkReadResponse } from '../dtos/notification.dto';

@Injectable({
  providedIn: 'root'
})
export class NotificationApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl;

  getMyNotifications(page = 1, pageSize = 20): Observable<MyNotificationsResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<MyNotificationsResponse>(`${this.baseUrl}/api/notifications/my`, { params });
  }

  markAsRead(id: string): Observable<MarkReadResponse> {
    return this.http.post<MarkReadResponse>(`${this.baseUrl}/api/notifications/${id}/read`, {});
  }
}
