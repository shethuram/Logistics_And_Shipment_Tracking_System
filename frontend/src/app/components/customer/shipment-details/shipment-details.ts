import { Component, OnInit, OnDestroy, AfterViewInit, inject, PLATFORM_ID, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { interval, Subscription, of } from 'rxjs';
import { startWith, switchMap, catchError } from 'rxjs/operators';
import { ShipmentApiService } from '../../../services/shipment.service';
import { TrackingApiService } from '../../../services/tracking.service';
import { DisputeApiService } from '../../../services/dispute.service';
import { ShipmentResponse, UpdateShipmentRequest } from '../../../dtos/shipment.dto';
import { PreferredWindow } from '../../../models/enums';

@Component({
  selector: 'app-shipment-details',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './shipment-details.html',
  styleUrl: './shipment-details.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CustomerShipmentDetailsComponent implements OnInit, OnDestroy, AfterViewInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);
  private shipmentApi = inject(ShipmentApiService);
  private trackingApi = inject(TrackingApiService);
  private disputeApi = inject(DisputeApiService);
  private fb = inject(FormBuilder);

  private isBrowser = isPlatformBrowser(this.platformId);
  private L: any = null;

  shipmentId!: string;
  shipment = signal<ShipmentResponse | null>(null);
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);

  private map: any = null;
  private pickupMarker: any = null;
  private dropMarker: any = null;
  private driverMarker: any = null;

  private subs = new Subscription();

  showDisputeModal = signal(false);
  disputeForm!: FormGroup;
  isDisputeSubmitting = signal(false);
  disputeError = signal<string | null>(null);
  disputeSuccess = signal(false);

  showEditModal = signal(false);
  editForm!: FormGroup;
  isSaving = signal(false);
  editError = signal<string | null>(null);

  ngOnInit() {
    this.shipmentId = this.route.snapshot.paramMap.get('id')!;
    this.disputeForm = this.fb.group({
      complaintText: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(1000)]]
    });
    this.editForm = this.fb.group({
      preferredWindow: ['', Validators.required],
      specialNotes: ['', [Validators.maxLength(500)]]
    });
    this.loadShipmentDetails();
  }

  ngAfterViewInit() {
    if (this.isBrowser) {
      this.loadLeaflet();
    }
  }

  async loadLeaflet() {
    this.L = await import('leaflet');
  }

  loadShipmentDetails() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.shipmentApi.getShipment(this.shipmentId).subscribe({
      next: (res) => {
        this.shipment.set(res);
        this.isLoading.set(false);
        this.setupTrackingAndMap();
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to load shipment details.');
      }
    });
  }

  setupTrackingAndMap() {
    const currentShipment = this.shipment();
    if (!currentShipment) return;

    if (this.isBrowser) {
      setTimeout(() => this.initMap(), 100);
    }

    if (currentShipment.status === 'IN_TRANSIT') {
      this.subs.add(
        interval(5000).pipe(
          startWith(0),
          switchMap(() => this.trackingApi.getLiveLocation(this.shipmentId).pipe(
            catchError(() => of(null))
          ))
        ).subscribe(res => {
          if (res && res.driverLocation) {
            this.updateDriverMarker(res.driverLocation.latitude, res.driverLocation.longitude);
          }
        })
      );
    }
  }

  initMap() {
    const currentShipment = this.shipment();
    if (!this.L || !currentShipment || this.map) return;

    const mapElement = document.getElementById('map');
    if (!mapElement) return;

    this.map = this.L.map('map').setView([currentShipment.pickupLat, currentShipment.pickupLng], 13);

    this.L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    this.pickupMarker = this.L.marker([currentShipment.pickupLat, currentShipment.pickupLng])
      .addTo(this.map)
      .bindPopup('Pickup Address')
      .openPopup();

    this.dropMarker = this.L.marker([currentShipment.dropLat, currentShipment.dropLng])
      .addTo(this.map)
      .bindPopup('Drop Address');

    const group = new this.L.featureGroup([this.pickupMarker, this.dropMarker]);
    this.map.fitBounds(group.getBounds().pad(0.1));
  }

  updateDriverMarker(lat: number, lng: number) {
    if (!this.L || !this.map) return;

    if (this.driverMarker) {
      this.driverMarker.setLatLng([lat, lng]);
    } else {
      this.driverMarker = this.L.circleMarker([lat, lng], {
        color: '#E8A845',
        fillColor: '#E8A845',
        fillOpacity: 0.8,
        radius: 8
      })
        .addTo(this.map)
        .bindPopup('Driver Live Position')
        .openPopup();
    }
  }

  cancelShipment() {
    if (!confirm('Are you sure you want to cancel this shipment?')) return;

    this.shipmentApi.cancelShipment(this.shipmentId).subscribe({
      next: (res) => {
        if (res && res.refundInitiated) {
          alert(`Shipment Cancelled. A full refund of ₹${this.shipment()?.totalAmount.toFixed(2)} has been initiated back to your original payment method.`);
        } else {
          alert('Shipment cancelled successfully.');
        }
        this.loadShipmentDetails();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to cancel shipment.');
      }
    });
  }

  openDispute() {
    this.showDisputeModal.set(true);
    this.disputeSuccess.set(false);
    this.disputeError.set(null);
    this.disputeForm.reset();
  }

  closeDispute() {
    this.showDisputeModal.set(false);
  }

  submitDispute() {
    if (this.disputeForm.invalid) return;

    this.isDisputeSubmitting.set(true);
    this.disputeError.set(null);

    const payload = {
      shipmentId: this.shipmentId,
      complaintText: this.disputeForm.value.complaintText
    };

    this.disputeApi.raiseDispute(payload).subscribe({
      next: () => {
        this.isDisputeSubmitting.set(false);
        this.disputeSuccess.set(true);
        setTimeout(() => this.closeDispute(), 2000);
      },
      error: (err) => {
        this.isDisputeSubmitting.set(false);
        this.disputeError.set(err.error?.message || 'Failed to submit dispute.');
      }
    });
  }
  openEditModal() {
    const current = this.shipment();
    if (!current) return;
    this.editForm.patchValue({
      preferredWindow: current.preferredWindow || 'MORNING',
      specialNotes: current.specialNotes || ''
    });
    this.showEditModal.set(true);
    this.editError.set(null);
  }

  closeEditModal() {
    this.showEditModal.set(false);
  }

  submitEdit() {
    if (this.editForm.invalid) return;
    this.isSaving.set(true);
    this.editError.set(null);

    const request: UpdateShipmentRequest = {
      preferredWindow: this.editForm.value.preferredWindow as PreferredWindow,
      specialNotes: this.editForm.value.specialNotes
    };

    this.shipmentApi.updateShipment(this.shipmentId, request).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.showEditModal.set(false);
        this.loadShipmentDetails();
      },
      error: (err) => {
        this.isSaving.set(false);
        this.editError.set(err.error?.message || 'Failed to update shipment.');
      }
    });
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
    if (this.map) {
      this.map.remove();
    }
  }
}
