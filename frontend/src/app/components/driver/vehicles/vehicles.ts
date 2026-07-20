import { Component, ChangeDetectionStrategy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DriverApiService } from '../../../services/driver.service';
import { SessionService } from '../../../services/session.service';
import { VehicleResponse } from '../../../dtos/vehicle.dto';
import { VehicleType } from '../../../models/enums';

@Component({
  selector: 'app-driver-vehicles',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './vehicles.html',
  styleUrl: './vehicles.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DriverVehiclesComponent implements OnInit {
  private driverApi = inject(DriverApiService);
  private session = inject(SessionService);
  private fb = inject(FormBuilder);

  isOnline = signal<boolean>(false);
  vehicles = signal<VehicleResponse[]>([]);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  allowedVehicleTypes = signal<string[]>([]);

  showNewVehicleModal = signal<boolean>(false);

  vehicleForm = this.fb.nonNullable.group({
    vehicleType: ['TWO_WHEELER' as VehicleType, [Validators.required]],
    vehicleNumber: ['', [Validators.required, Validators.pattern(/^[A-Z]{2}[ -]?[0-9]{1,2}[ -]?[A-Z]{1,2}[ -]?[0-9]{4}$/)]]
  });

  ngOnInit() {
    const profile = this.session.profile();
    if (profile && profile.driver) {
      this.isOnline.set(profile.driver.operationalStatus === 'ONLINE');
    }
    this.loadVehicles();
    this.loadAllowedVehicleTypes();
  }

  loadAllowedVehicleTypes() {
    this.driverApi.getAllowedVehicles().subscribe({
      next: (types) => {
        this.allowedVehicleTypes.set(types);
        if (types.length > 0) {
          this.vehicleForm.patchValue({
            vehicleType: types[0] as VehicleType
          });
        }
      },
      error: () => {
        this.allowedVehicleTypes.set(['TWO_WHEELER', 'THREE_WHEELER', 'FOUR_WHEELER', 'HEAVY_VEHICLE']);
      }
    });
  }

  isBusy(): boolean {
    return this.session.profile()?.driver?.operationalStatus === 'ON_DELIVERY';
  }

  loadVehicles() {
    this.isLoading.set(true);
    this.driverApi.getVehicles().subscribe({
      next: (list) => {
        this.vehicles.set(list);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to load vehicles list.');
        this.isLoading.set(false);
      }
    });
  }

  toggleOnlineStatus() {
    this.errorMessage.set(null);
    this.isLoading.set(true);

    if (this.isOnline()) {
      this.driverApi.goOffline().subscribe({
        next: () => {
          this.isOnline.set(false);
          this.session.updateDriverStatus('OFFLINE');
          this.isLoading.set(false);
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message || 'Failed to toggle offline.');
          this.isLoading.set(false);
        }
      });
    } else {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          this.driverApi.goOnline(pos.coords.latitude, pos.coords.longitude).subscribe({
            next: () => {
              this.isOnline.set(true);
              this.session.updateDriverStatus('ONLINE');
              this.isLoading.set(false);
            },
            error: (err) => {
              this.errorMessage.set(err.error?.message || 'Failed to toggle online.');
              this.isLoading.set(false);
            }
          });
        },
        (err) => {
          this.driverApi.goOnline(12.9716, 77.5946).subscribe({
            next: () => {
              this.isOnline.set(true);
              this.session.updateDriverStatus('ONLINE');
              this.isLoading.set(false);
            },
            error: (err) => {
              this.errorMessage.set(err.error?.message || 'Failed to toggle online.');
              this.isLoading.set(false);
            }
          });
        }
      );
    }
  }

  registerVehicleSubmit() {
    if (this.vehicleForm.invalid) return;

    this.errorMessage.set(null);
    this.isLoading.set(true);

    const values = this.vehicleForm.getRawValue();
    this.driverApi.registerVehicle(values).subscribe({
      next: (newVehicle) => {
        this.vehicles.update(list => [...list, newVehicle]);
        this.showNewVehicleModal.set(false);
        this.vehicleForm.reset();
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to register vehicle.');
        this.isLoading.set(false);
      }
    });
  }

  activateVehicle(id: string) {
    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.driverApi.activateVehicle(id).subscribe({
      next: () => {
        this.vehicles.update(list =>
          list.map(v => ({ ...v, isActive: v.id === id }))
        );
        this.session.updateDriverActiveVehicle(id);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to activate vehicle.');
        this.isLoading.set(false);
      }
    });
  }

  getActiveVehicle(): VehicleResponse | undefined {
    return this.vehicles().find(v => v.isActive);
  }
}
