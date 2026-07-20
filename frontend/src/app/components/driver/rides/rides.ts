import { Component, ChangeDetectionStrategy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ShipmentApiService } from '../../../services/shipment.service';
import { ShipmentResponse } from '../../../dtos/shipment.dto';

@Component({
  selector: 'app-driver-rides',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './rides.html',
  styleUrl: './rides.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DriverRidesComponent implements OnInit {
  private shipmentApi = inject(ShipmentApiService);
  private router = inject(Router);

  rides = signal<ShipmentResponse[]>([]);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  totalDeliveries = signal<number>(0);
  totalEarnings = signal<number>(0);
  activeDeliveriesCount = signal<number>(0);
  cancelledCount = signal<number>(0);

  ngOnInit() {
    this.loadHistory();
  }

  loadHistory() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.shipmentApi.getDriverHistory().subscribe({
      next: (list) => {
        this.rides.set(list);
        this.calculateMetrics(list);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to load rides history.');
        this.isLoading.set(false);
      }
    });
  }

  calculateMetrics(list: ShipmentResponse[]) {
    let deliveries = 0;
    let earnings = 0;
    let active = 0;
    let cancelled = 0;

    for (const r of list) {
      if (r.status === 'DELIVERED') {
        deliveries++;
        earnings += r.driverEarnings || 0;
      } else if (r.status === 'ASSIGNED' || r.status === 'IN_TRANSIT') {
        active++;
      } else if (r.status === 'CANCELLED' || r.status === 'PICKUP_FAILED') {
        cancelled++;
      }
    }

    this.totalDeliveries.set(deliveries);
    this.totalEarnings.set(earnings);
    this.activeDeliveriesCount.set(active);
    this.cancelledCount.set(cancelled);
  }

  goToActive() {
    this.router.navigate(['/driver/active']);
  }

  browseJobs() {
    this.router.navigate(['/driver/jobs']);
  }
}
