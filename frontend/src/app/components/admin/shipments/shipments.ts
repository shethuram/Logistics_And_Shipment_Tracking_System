import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../../services/admin';
import { ShipmentResponse } from '../../../dtos/shipment.dto';
import { DriverLocationDto } from '../../../dtos/tracking.dto';
import { ShipmentStatus } from '../../../models/enums';

@Component({
  selector: 'app-admin-shipments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './shipments.html',
  styleUrl: './shipments.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminShipmentsComponent implements OnInit {
  private adminApi = inject(AdminApiService);

  shipments = signal<ShipmentResponse[]>([]);
  totalCount = signal<number>(0);
  currentPage = signal<number>(1);
  pageSize = 10;

  searchQuery = signal<string>('');
  selectedStatus = signal<ShipmentStatus | undefined>(undefined);
  dateFrom = signal<string>('');
  dateTo = signal<string>('');

  isLoading = signal<boolean>(true);
  isExporting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  // Tracking history state
  expandedTrackingShipmentId = signal<string | null>(null);
  trackingHistory = signal<DriverLocationDto[]>([]);
  isTrackingLoading = signal<boolean>(false);

  ngOnInit() {
    this.loadShipments();
  }

  loadShipments() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.adminApi.getAdminShipments({
      search: this.searchQuery() || undefined,
      status: this.selectedStatus() || undefined,
      dateFrom: this.dateFrom() || undefined,
      dateTo: this.dateTo() || undefined,
      page: this.currentPage(),
      pageSize: this.pageSize
    }).subscribe({
      next: (res) => {
        this.shipments.set(res.data || []);
        this.totalCount.set(res.total || 0);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to load shipments registry.');
        this.isLoading.set(false);
      }
    });
  }

  applyFilters() {
    this.currentPage.set(1);
    this.loadShipments();
  }

  resetFilters() {
    this.searchQuery.set('');
    this.selectedStatus.set(undefined);
    this.dateFrom.set('');
    this.dateTo.set('');
    this.applyFilters();
  }

  reassignShipment(id: string) {
    if (!confirm('Are you sure you want to release this shipment back to the open pool? The current driver assignment will be removed.')) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.adminApi.reassignShipment(id).subscribe({
      next: () => {
        this.loadShipments();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to reassign shipment.');
        this.isLoading.set(false);
      }
    });
  }

  exportCsv() {
    this.isExporting.set(true);
    this.errorMessage.set(null);

    this.adminApi.exportShipmentsCsv({
      status: this.selectedStatus(),
      dateFrom: this.dateFrom() || undefined,
      dateTo: this.dateTo() || undefined
    }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `shipments_export_${new Date().toISOString().split('T')[0]}.csv`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.isExporting.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to download shipments CSV report.');
        this.isExporting.set(false);
      }
    });
  }

  toggleTrackingHistory(shipmentId: string) {
    if (this.expandedTrackingShipmentId() === shipmentId) {
      this.expandedTrackingShipmentId.set(null);
      this.trackingHistory.set([]);
      return;
    }

    this.expandedTrackingShipmentId.set(shipmentId);
    this.isTrackingLoading.set(true);
    this.trackingHistory.set([]);

    this.adminApi.getTrackingHistory(shipmentId).subscribe({
      next: (history) => {
        this.trackingHistory.set(history || []);
        this.isTrackingLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to load tracking location history.');
        this.isTrackingLoading.set(false);
      }
    });
  }

  nextPage() {
    if (this.currentPage() * this.pageSize < this.totalCount()) {
      this.currentPage.update(p => p + 1);
      this.loadShipments();
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadShipments();
    }
  }
}
