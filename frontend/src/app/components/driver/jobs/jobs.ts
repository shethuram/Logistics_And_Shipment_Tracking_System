import { Component, ChangeDetectionStrategy, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ShipmentApiService } from '../../../services/shipment.service';
import { SignalrService } from '../../../services/signalr.service';
import { SessionService } from '../../../services/session.service';
import { AvailableShipmentDto, ShipmentResponse } from '../../../dtos/shipment.dto';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-driver-jobs',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './jobs.html',
  styleUrl: './jobs.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DriverJobsComponent implements OnInit, OnDestroy {
  private shipmentApi = inject(ShipmentApiService);
  private signalr = inject(SignalrService);
  private session = inject(SessionService);
  private router = inject(Router);

  isOnline = signal<boolean>(false);
  hasActiveVehicle = signal<boolean>(false);
  availableJobs = signal<AvailableShipmentDto[]>([]);
  activeShipment = signal<ShipmentResponse | null>(null);

  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  private signalrSub: Subscription | null = null;

  ngOnInit() {
    const profile = this.session.profile();
    if (profile && profile.driver) {
      this.isOnline.set(profile.driver.operationalStatus === 'ONLINE');
      this.hasActiveVehicle.set(!!profile.driver.activeVehicleId);
    }

    this.loadActiveShipment();

    this.signalr.startConnection().then(() => {
      this.signalrSub = this.signalr.newJobAlert$.subscribe((newJob: AvailableShipmentDto) => {
        this.availableJobs.update(jobs => {
          if (jobs.some(j => j.id === newJob.id)) return jobs;
          return [newJob, ...jobs];
        });
      });
    });
  }

  ngOnDestroy() {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadActiveShipment() {
    this.isLoading.set(true);
    this.shipmentApi.getActiveShipment().subscribe({
      next: (shipment) => {
        this.activeShipment.set(shipment);
        this.isLoading.set(false);
      },
      error: () => {
        this.activeShipment.set(null);
        this.isLoading.set(false);
        this.loadAvailableJobs();
      }
    });
  }

  loadAvailableJobs() {
    if (this.activeShipment() || !this.hasActiveVehicle() || !this.isOnline()) return;

    this.isLoading.set(true);
    this.shipmentApi.getAvailableShipments().subscribe({
      next: (list) => {
        this.availableJobs.set(list);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to load available jobs feed.');
        this.isLoading.set(false);
      }
    });
  }

  claimJob(id: string) {
    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.shipmentApi.claimShipment(id).subscribe({
      next: () => {
        this.router.navigate(['/driver/active']);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to claim job.');
        this.isLoading.set(false);
      }
    });
  }

  goToVehicles() {
    this.router.navigate(['/driver/vehicles']);
  }

  goToActive() {
    this.router.navigate(['/driver/active']);
  }
}
