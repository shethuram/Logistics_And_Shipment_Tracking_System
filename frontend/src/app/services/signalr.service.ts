import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { Subject, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '@auth0/auth0-angular';
import { NotificationDto } from '../dtos/notification.dto';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private auth = inject(AuthService);
  private connection: HubConnection | null = null;

  public notificationReceived$ = new Subject<NotificationDto>();
  public shipmentUpdated$ = new Subject<any>();
  public locationUpdated$ = new Subject<any>();
  public newJobAlert$ = new Subject<any>();

  async startConnection() {
    if (this.connection) return;

    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/tracking`, {
        accessTokenFactory: async () => {
          try {
            return await firstValueFrom(this.auth.getAccessTokenSilently());
          } catch (err) {
            console.error('Error fetching token for SignalR:', err);
            return '';
          }
        }
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('notificationReceived', (data: NotificationDto) => {
      this.notificationReceived$.next(data);
    });

    this.connection.on('shipmentUpdated', (data: any) => {
      this.shipmentUpdated$.next(data);
    });

    this.connection.on('locationUpdated', (data: any) => {
      this.locationUpdated$.next(data);
    });

    this.connection.on('newJobAlert', (data: any) => {
      this.newJobAlert$.next(data);
    });

    try {
      await this.connection.start();
    } catch (err) {
      console.error('Error starting SignalR connection:', err);
    }
  }

  joinShipmentGroup(shipmentId: string) {
    if (this.connection) {
      this.connection.invoke('JoinShipmentGroup', shipmentId)
        .catch(err => console.error('Error invoking JoinShipmentGroup:', err));
    }
  }

  leaveShipmentGroup(shipmentId: string) {
    if (this.connection) {
      this.connection.invoke('LeaveShipmentGroup', shipmentId)
        .catch(err => console.error('Error invoking LeaveShipmentGroup:', err));
    }
  }

  joinUserGroup(userId: string) {
    if (this.connection) {
      this.connection.invoke('JoinUserGroup', userId)
        .catch(err => console.error('Error invoking JoinUserGroup:', err));
    }
  }

  leaveUserGroup(userId: string) {
    if (this.connection) {
      this.connection.invoke('LeaveUserGroup', userId)
        .catch(err => console.error('Error invoking LeaveUserGroup:', err));
    }
  }

  joinVehicleGroup(vehicleType: string) {
    if (this.connection) {
      this.connection.invoke('JoinVehicleGroup', vehicleType)
        .catch(err => console.error('Error invoking JoinVehicleGroup:', err));
    }
  }

  leaveVehicleGroup(vehicleType: string) {
    if (this.connection) {
      this.connection.invoke('LeaveVehicleGroup', vehicleType)
        .catch(err => console.error('Error invoking LeaveVehicleGroup:', err));
    }
  }
}
