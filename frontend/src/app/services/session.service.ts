import { Injectable, inject, signal, computed } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { AuthApiService } from './auth.service';
import { UserProfileDto } from '../dtos/auth.dto';
import { Observable, of, tap } from 'rxjs';
import { take, switchMap, catchError } from 'rxjs/operators';
import { OperationalStatus } from '../models/enums';

@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private auth = inject(AuthService);
  private authApi = inject(AuthApiService);

  private profileSignal = signal<UserProfileDto | null>(null);

  public readonly profile = this.profileSignal.asReadonly();
  public readonly isAuthenticated = computed(() => this.profileSignal()?.isAuthenticated ?? false);
  public readonly isRegistered = computed(() => this.profileSignal()?.isRegistered ?? false);
  public readonly role = computed(() => this.profileSignal()?.role ?? null);

  loadProfile(): Observable<UserProfileDto> {
    const cached = this.profileSignal();
    if (cached) {
      return of(cached);
    }

    return this.authApi.getProfile().pipe(
      tap(profile => {
        this.profileSignal.set(profile);
      })
    );
  }

  resolveSession(): Observable<UserProfileDto | null> {
    const cached = this.profileSignal();
    if (cached) {
      return of(cached);
    }

    return this.auth.isAuthenticated$.pipe(
      take(1),
      switchMap(authd => {
        if (!authd) {
          this.clearSession();
          return of(null);
        }
        return this.loadProfile().pipe(
          catchError(() => {
            this.clearSession();
            return of(null);
          })
        );
      })
    );
  }

  clearSession() {
    this.profileSignal.set(null);
  }

  updateDriverStatus(status: OperationalStatus) {
    this.profileSignal.update(p => {
      if (p && p.driver) {
        return {
          ...p,
          driver: {
            ...p.driver,
            operationalStatus: status
          }
        };
      }
      return p;
    });
  }

  updateDriverActiveVehicle(vehicleId: string | null) {
    this.profileSignal.update(p => {
      if (p && p.driver) {
        return {
          ...p,
          driver: {
            ...p.driver,
            activeVehicleId: vehicleId
          }
        };
      }
      return p;
    });
  }

  logout() {
    this.clearSession();
    this.auth.logout({ logoutParams: { returnTo: window.location.origin } });
  }
}
