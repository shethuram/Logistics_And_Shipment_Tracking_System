import { Component, ChangeDetectionStrategy, OnInit, OnDestroy, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ShipmentApiService } from '../../../services/shipment.service';
import { TrackingApiService } from '../../../services/tracking.service';
import { ShipmentResponse } from '../../../dtos/shipment.dto';

@Component({
  selector: 'app-driver-active',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './active.html',
  styleUrl: './active.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DriverActiveComponent implements OnInit, OnDestroy {
  private shipmentApi = inject(ShipmentApiService);
  private trackingApi = inject(TrackingApiService);
  private router = inject(Router);

  activeShipment = signal<ShipmentResponse | null>(null);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  showFailureReasonModal = signal<boolean>(false);

  otpCode = signal<string>('');
  failureReason = signal<string>('');

  private trackingInterval: any;
  private simStep = 0;
  private simMax = 10;

  constructor() {
    effect(() => {
      const activeJob = this.activeShipment();
      if (activeJob && activeJob.status === 'IN_TRANSIT') {
        this.startSimulation(activeJob);
      } else {
        this.stopSimulation();
      }
    });
  }

  ngOnInit() {
    this.loadActiveShipment();
  }

  ngOnDestroy() {
    this.stopSimulation();
  }

  loadActiveShipment() {
    this.isLoading.set(true);
    this.shipmentApi.getActiveShipment().subscribe({
      next: (shipment) => {
        this.activeShipment.set(shipment);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.activeShipment.set(null);
        this.isLoading.set(false);
      }
    });
  }

  releaseClaim() {
    const job = this.activeShipment();
    if (!job) return;

    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.shipmentApi.cancelClaim(job.id, 'Driver requested release').subscribe({
      next: () => {
        this.activeShipment.set(null);
        this.router.navigate(['/driver/jobs']);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to cancel claim.');
        this.isLoading.set(false);
      }
    });
  }

  pickupVerify() {
    const job = this.activeShipment();
    if (!job) return;

    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.shipmentApi.confirmPickup(job.id, this.otpCode()).subscribe({
      next: () => {
        this.otpCode.set('');
        this.loadActiveShipment();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Invalid Pickup OTP.');
        this.isLoading.set(false);
      }
    });
  }

  pickupFailedSubmit() {
    const job = this.activeShipment();
    if (!job || !this.failureReason()) return;

    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.shipmentApi.markPickupFailed(job.id, this.failureReason()).subscribe({
      next: () => {
        this.showFailureReasonModal.set(false);
        this.failureReason.set('');
        this.activeShipment.set(null);
        this.router.navigate(['/driver/jobs']);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to record failure.');
        this.isLoading.set(false);
      }
    });
  }

  deliveryVerify() {
    const job = this.activeShipment();
    if (!job) return;

    this.errorMessage.set(null);
    this.isLoading.set(true);

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        this.verifyDeliveryWithCoords(job.id, pos.coords.latitude, pos.coords.longitude);
      },
      (err) => {
        this.verifyDeliveryWithCoords(job.id, job.dropLat, job.dropLng);
      }
    );
  }

  private verifyDeliveryWithCoords(id: string, lat: number, lng: number) {
    this.shipmentApi.confirmDelivery(id, this.otpCode(), lat, lng).subscribe({
      next: () => {
        this.otpCode.set('');
        this.loadActiveShipment();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Invalid Delivery OTP or coordinates mismatch.');
        this.isLoading.set(false);
      }
    });
  }

  cashCollectedVerify() {
    const job = this.activeShipment();
    if (!job) return;

    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.shipmentApi.confirmCashCollected(job.id).subscribe({
      next: () => {
        this.activeShipment.set(null);
        this.router.navigate(['/driver/jobs']);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to confirm cash collected.');
        this.isLoading.set(false);
      }
    });
  }

  private startSimulation(shipment: ShipmentResponse) {
    this.stopSimulation();
    this.simStep = 0;

    this.trackingInterval = setInterval(() => {
      if (this.simStep >= this.simMax) {
        this.stopSimulation();
        return;
      }

      this.simStep++;
      const ratio = this.simStep / this.simMax;
      const currentLat = shipment.pickupLat + (shipment.dropLat - shipment.pickupLat) * ratio;
      const currentLng = shipment.pickupLng + (shipment.dropLng - shipment.pickupLng) * ratio;

      this.trackingApi.recordLocation({
        shipmentId: shipment.id,
        latitude: currentLat,
        longitude: currentLng
      }).subscribe({
        error: (err) => console.error('Simulated tracking failed:', err)
      });
    }, 5000);
  }

  private stopSimulation() {
    if (this.trackingInterval) {
      clearInterval(this.trackingInterval);
      this.trackingInterval = null;
    }
  }

  browseJobs() {
    this.router.navigate(['/driver/jobs']);
  }
}
