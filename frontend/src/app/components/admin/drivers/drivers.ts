import { Component, OnInit, OnDestroy, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AdminApiService } from '../../../services/admin';
import { AdminDriverDto } from '../../../dtos/admin.dto';
import { DriverApprovalStatus } from '../../../models/enums';
import { SignalrService } from '../../../services/signalr.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-admin-drivers',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './drivers.html',
  styleUrl: './drivers.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDriversComponent implements OnInit, OnDestroy {
  private adminApi = inject(AdminApiService);
  private fb = inject(FormBuilder);
  private signalrService = inject(SignalrService);

  private signalrSub!: Subscription;

  drivers = signal<AdminDriverDto[]>([]);
  selectedStatus = signal<DriverApprovalStatus | undefined>(undefined);
  totalCount = signal<number>(0);
  currentPage = signal<number>(1);
  pageSize = 10;

  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  actionType = signal<'REJECT' | 'SUSPEND' | null>(null);
  selectedDriverId = signal<string | null>(null);
  showActionModal = signal<boolean>(false);
  actionForm!: FormGroup;
  actionError = signal<string | null>(null);
  isSubmitting = signal<boolean>(false);

  selectedDriverDetail = signal<AdminDriverDto | null>(null);
  showDetailModal = signal<boolean>(false);

  ngOnInit() {
    this.actionForm = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]]
    });
    this.loadDrivers();

    this.signalrSub = this.signalrService.adminAlert$.subscribe({
      next: (alert) => {
        const type = alert.type || alert.Type;
        if (type === 'DRIVER_VERIFICATION_UPDATE' || type === 'NEW_DRIVER_REGISTRATION') {
          this.loadDrivers();
          
          if (type === 'DRIVER_VERIFICATION_UPDATE') {
            const data = alert.data || alert.Data;
            const driverId = data?.driverId || data?.DriverId;
            const currentDetail = this.selectedDriverDetail();
            if (driverId && currentDetail && currentDetail.id === driverId) {
              this.adminApi.getDriverById(driverId).subscribe({
                next: (res) => {
                  this.selectedDriverDetail.set(res);
                }
              });
            }
          }
        }
      }
    });
  }

  ngOnDestroy() {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadDrivers() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.adminApi.getDrivers(this.selectedStatus(), this.currentPage(), this.pageSize).subscribe({
      next: (res) => {
        this.drivers.set(res.data || []);
        this.totalCount.set(res.total || 0);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to load driver profiles.');
        this.isLoading.set(false);
      }
    });
  }

  filterByStatus(status: DriverApprovalStatus | undefined) {
    this.selectedStatus.set(status);
    this.currentPage.set(1);
    this.loadDrivers();
  }

  approveDriver(id: string) {
    if (!confirm('Are you sure you want to approve this driver registration?')) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.adminApi.approveDriver(id).subscribe({
      next: () => {
        this.loadDrivers();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to approve driver profile.');
        this.isLoading.set(false);
      }
    });
  }

  openActionModal(id: string, type: 'REJECT' | 'SUSPEND') {
    this.selectedDriverId.set(id);
    this.actionType.set(type);
    this.actionForm.reset();
    this.actionError.set(null);
    this.showActionModal.set(true);
  }

  closeActionModal() {
    this.showActionModal.set(false);
    this.selectedDriverId.set(null);
    this.actionType.set(null);
  }

  submitAction() {
    if (this.actionForm.invalid || !this.selectedDriverId() || !this.actionType()) return;

    this.isSubmitting.set(true);
    this.actionError.set(null);

    const id = this.selectedDriverId()!;
    const reason = this.actionForm.value.reason;
    const obs = this.actionType() === 'REJECT' 
      ? this.adminApi.rejectDriver(id, reason)
      : this.adminApi.suspendDriver(id, reason);

    obs.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.closeActionModal();
        this.loadDrivers();
      },
      error: (err) => {
        this.actionError.set(err.error?.message || 'Failed to update driver status.');
        this.isSubmitting.set(false);
      }
    });
  }

  viewDriverDetail(id: string) {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    
    this.adminApi.getDriverById(id).subscribe({
      next: (res) => {
        this.selectedDriverDetail.set(res);
        this.showDetailModal.set(true);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to load driver profile details.');
        this.isLoading.set(false);
      }
    });
  }

  closeDetailModal() {
    this.showDetailModal.set(false);
    this.selectedDriverDetail.set(null);
  }

  nextPage() {
    if (this.currentPage() * this.pageSize < this.totalCount()) {
      this.currentPage.update(p => p + 1);
      this.loadDrivers();
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadDrivers();
    }
  }

  parseReport(reportJson: string | null | undefined): any {
    if (!reportJson) return null;
    try {
      return JSON.parse(reportJson);
    } catch {
      return null;
    }
  }

  getLicenseImageUrl(url: string | null | undefined): string {
    if (!url) return '';
    if (url.startsWith('http')) return url;
    return `http://localhost:5286${url}`;
  }
}
