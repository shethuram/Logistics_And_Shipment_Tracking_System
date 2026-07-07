import { Component, OnInit, OnDestroy, inject, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { SessionService } from '../../../services/session.service';
import { ShipmentApiService } from '../../../services/shipment.service';
import { MetadataService } from '../../../services/metadata.service';
import { PaymentApiService } from '../../../services/payment.service';
import { ShipmentResponse } from '../../../dtos/shipment.dto';
import { MetadataOptionDto } from '../../../dtos/metadata.dto';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-customer-shipments',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './shipments.html',
  styleUrl: './shipments.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CustomerShipmentsComponent implements OnInit, OnDestroy {
  public session = inject(SessionService);
  private shipmentApi = inject(ShipmentApiService);
  private metadataService = inject(MetadataService);
  private paymentApi = inject(PaymentApiService);

  shipments = signal<ShipmentResponse[]>([]);
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);

  searchQuery = signal<string>('');
  selectedStatus = signal<string>('');
  dateFrom = signal<string>('');
  dateTo = signal<string>('');
  statusOptions = signal<MetadataOptionDto[]>([]);

  private searchSubject = new Subject<string>();
  private subs = new Subscription();
  private pollInterval: any;

  ngOnInit() {
    this.metadataService.getMetadata().subscribe({
      next: (metadata) => {
        this.statusOptions.set(metadata.shipmentStatuses || []);
      }
    });

    this.subs.add(
      this.searchSubject.pipe(
        debounceTime(300),
        distinctUntilChanged()
      ).subscribe(val => {
        this.searchQuery.set(val);
        this.loadShipments();
      })
    );

    this.loadShipments();
  }

  loadShipments() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const filters = {
      page: 1,
      pageSize: 50,
      search: this.searchQuery() || undefined,
      status: this.selectedStatus() || undefined,
      dateFrom: this.dateFrom() || undefined,
      dateTo: this.dateTo() || undefined
    };

    this.shipmentApi.getShipments(filters).subscribe({
      next: (res) => {
        this.shipments.set(res.data || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to load your shipments.');
      }
    });
  }

  onSearchChange(value: string) {
    this.searchSubject.next(value);
  }

  onStatusChange(value: string) {
    this.selectedStatus.set(value);
    this.loadShipments();
  }

  onDateFromChange(value: string) {
    this.dateFrom.set(value);
    this.loadShipments();
  }

  onDateToChange(value: string) {
    this.dateTo.set(value);
    this.loadShipments();
  }

  payNow(shipmentId: string) {
    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.paymentApi.initiatePayment({ shipmentId }).subscribe({
      next: (payRes) => {
        const options = {
          key: environment.razorpayKeyId,
          amount: payRes.amount * 100,
          currency: payRes.currency,
          name: 'CargoZ Logistics',
          description: 'Complete Pending Payment',
          order_id: payRes.razorpayOrderId,
          handler: (response: any) => {
            this.pollPaymentStatus(shipmentId);
          },
          modal: {
            ondismiss: () => {
              this.isLoading.set(false);
              this.errorMessage.set('Payment was cancelled or failed.');
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
        this.errorMessage.set(err.error?.message || 'Failed to initiate payment.');
      }
    });
  }

  private pollPaymentStatus(shipmentId: string) {
    let attempts = 0;
    this.pollInterval = setInterval(() => {
      attempts++;
      if (attempts > 15) {
        clearInterval(this.pollInterval);
        this.isLoading.set(false);
        this.errorMessage.set('Payment verification timed out. Refresh the list to check status.');
        return;
      }

      this.paymentApi.getPaymentStatus(shipmentId).subscribe({
        next: (statusRes) => {
          if (statusRes.paymentStatus === 'SUCCESS') {
            clearInterval(this.pollInterval);
            this.isLoading.set(false);
            this.loadShipments();
          }
        },
        error: () => {}
      });
    }, 2000);
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
    }
  }
}
