import { Component, OnInit, OnDestroy, inject, ChangeDetectionStrategy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Subject, Subscription, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { GeocodingService, GeocodingResultDto } from '../../../services/geocoding.service';
import { ShipmentApiService } from '../../../services/shipment.service';
import { MetadataService } from '../../../services/metadata.service';
import { PaymentApiService } from '../../../services/payment.service';
import { SessionService } from '../../../services/session.service';
import { FormMetadataDto } from '../../../dtos/metadata.dto';
import { PackageType, PreferredWindow, PaymentMethod } from '../../../models/enums';
import { CreateShipmentRequest, CreateShipmentResponse } from '../../../dtos/shipment.dto';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-book-shipment',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './book-shipment.html',
  styleUrl: './book-shipment.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BookShipmentComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private geocoding = inject(GeocodingService);
  private shipmentApi = inject(ShipmentApiService);
  private metadataService = inject(MetadataService);
  private paymentApi = inject(PaymentApiService);
  private session = inject(SessionService);

  bookingForm!: FormGroup;
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  metadata = signal<FormMetadataDto | null>(null);
  bookingResult = signal<CreateShipmentResponse | null>(null);
  showSuccessOverlay = signal(false);
  showConfirmModal = signal(false);
  paymentMethodToConfirm = signal<PaymentMethod>('COD');

  pickupSuggestions = signal<GeocodingResultDto[]>([]);
  dropSuggestions = signal<GeocodingResultDto[]>([]);

  private pickupSearch$ = new Subject<string>();
  private dropSearch$ = new Subject<string>();

  private subs = new Subscription();
  private pollInterval: any;

  ngOnInit() {
    this.metadataService.getMetadata().subscribe({
      next: (data) => {
        this.metadata.set(data);
      }
    });

    this.bookingForm = this.fb.group({
      pickupAddress: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
      pickupLat: [null, [Validators.required]],
      pickupLng: [null, [Validators.required]],
      dropAddress: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
      dropLat: [null, [Validators.required]],
      dropLng: [null, [Validators.required]],
      receiverName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      receiverPhone: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
      packageType: ['SMALL_PARCEL', [Validators.required]],
      weightKg: [1.0, [Validators.required, Validators.min(0.01), Validators.max(10000.0)]],
      preferredWindow: ['MORNING'],
      specialNotes: ['', [Validators.maxLength(500)]],
      paymentMethod: ['COD', [Validators.required]]
    });

    this.subs.add(
      this.pickupSearch$.pipe(
        debounceTime(400),
        distinctUntilChanged(),
        switchMap(query => query ? this.geocoding.searchAddresses(query) : of([]))
      ).subscribe(results => {
        this.pickupSuggestions.set(results);
      })
    );

    this.subs.add(
      this.dropSearch$.pipe(
        debounceTime(400),
        distinctUntilChanged(),
        switchMap(query => query ? this.geocoding.searchAddresses(query) : of([]))
      ).subscribe(results => {
        this.dropSuggestions.set(results);
      })
    );
  }

  onPickupInput(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.bookingForm.patchValue({ pickupLat: null, pickupLng: null });
    this.pickupSuggestions.set([]);
    this.pickupSearch$.next(val);
  }

  onDropInput(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.bookingForm.patchValue({ dropLat: null, dropLng: null });
    this.dropSuggestions.set([]);
    this.dropSearch$.next(val);
  }

  selectPickup(result: GeocodingResultDto) {
    this.bookingForm.patchValue({
      pickupAddress: result.displayName,
      pickupLat: result.lat,
      pickupLng: result.lng
    });
    this.pickupSuggestions.set([]);
  }

  selectDrop(result: GeocodingResultDto) {
    this.bookingForm.patchValue({
      dropAddress: result.displayName,
      dropLat: result.lat,
      dropLng: result.lng
    });
    this.dropSuggestions.set([]);
  }

  onSubmit() {
    if (this.bookingForm.invalid) {
      this.bookingForm.markAllAsTouched();
      return;
    }
    this.paymentMethodToConfirm.set(this.bookingForm.value.paymentMethod as PaymentMethod);
    this.showConfirmModal.set(true);
  }

  confirmBooking() {
    this.showConfirmModal.set(false);
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const formValues = this.bookingForm.value;
    const payload: CreateShipmentRequest = {
      pickupAddress: formValues.pickupAddress,
      pickupLat: formValues.pickupLat,
      pickupLng: formValues.pickupLng,
      dropAddress: formValues.dropAddress,
      dropLat: formValues.dropLat,
      dropLng: formValues.dropLng,
      receiverName: formValues.receiverName,
      receiverPhone: formValues.receiverPhone,
      packageType: formValues.packageType as PackageType,
      weightKg: formValues.weightKg,
      preferredWindow: formValues.preferredWindow as PreferredWindow,
      specialNotes: formValues.specialNotes || undefined,
      paymentMethod: formValues.paymentMethod as PaymentMethod
    };

    this.shipmentApi.createShipment(payload).subscribe({
      next: (res) => {
        if (payload.paymentMethod === 'ONLINE') {
          this.initiateOnlineCheckout(res);
        } else {
          this.isLoading.set(false);
          this.bookingResult.set(res);
          this.showSuccessOverlay.set(true);
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to book shipment. Please try again.');
      }
    });
  }

  private initiateOnlineCheckout(shipmentRes: CreateShipmentResponse) {
    this.paymentApi.initiatePayment({ shipmentId: shipmentRes.id }).subscribe({
      next: (payRes) => {
        const options = {
          key: environment.razorpayKeyId,
          amount: payRes.amount * 100,
          currency: payRes.currency,
          name: 'CargoZ Logistics',
          description: 'Payment for shipment booking',
          order_id: payRes.razorpayOrderId,
          handler: (response: any) => {
            this.pollPaymentStatus(shipmentRes.id, shipmentRes);
          },
          modal: {
            ondismiss: () => {
              this.isLoading.set(false);
              this.errorMessage.set('Payment pending. You can complete the payment anytime from your Shipments page.');
              setTimeout(() => {
                this.router.navigate(['/customer/shipments']);
              }, 4000);
            }
          },
          prefill: {
            name: this.session.profile()?.fullName || '',
            email: this.session.profile()?.email || ''
          },
          theme: {
            color: '#E8A845'
          }
        };

        const rzp = new (window as any).Razorpay(options);
        rzp.open();
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to initiate online payment.');
      }
    });
  }

  private pollPaymentStatus(shipmentId: string, shipmentRes: CreateShipmentResponse) {
    let attempts = 0;
    this.pollInterval = setInterval(() => {
      attempts++;
      if (attempts > 15) {
        clearInterval(this.pollInterval);
        this.isLoading.set(false);
        this.errorMessage.set('Payment verification timed out. Check the status in your shipments list.');
        return;
      }

      this.paymentApi.getPaymentStatus(shipmentId).subscribe({
        next: (statusRes) => {
          if (statusRes.paymentStatus === 'SUCCESS') {
            clearInterval(this.pollInterval);
            this.isLoading.set(false);
            this.bookingResult.set(shipmentRes);
            this.showSuccessOverlay.set(true);
          }
        },
        error: () => {}
      });
    }, 2000);
  }

  goToShipments() {
    this.showSuccessOverlay.set(false);
    this.router.navigate(['/customer/shipments']);
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
    }
  }
}
