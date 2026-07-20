import { Component, OnInit, OnDestroy, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AdminApiService } from '../../../services/admin';
import { DisputeResponse } from '../../../dtos/dispute.dto';
import { DisputeStatus } from '../../../models/enums';
import { SignalrService } from '../../../services/signalr.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-admin-disputes',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './disputes.html',
  styleUrl: './disputes.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDisputesComponent implements OnInit, OnDestroy {
  private adminApi = inject(AdminApiService);
  private fb = inject(FormBuilder);
  private signalrService = inject(SignalrService);

  private signalrSub!: Subscription;

  disputes = signal<DisputeResponse[]>([]);
  selectedStatus = signal<DisputeStatus | undefined>(undefined);
  totalCount = signal<number>(0);
  currentPage = signal<number>(1);
  pageSize = 10;

  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  selectedDisputeId = signal<string | null>(null);
  showResolveModal = signal<boolean>(false);
  resolveForm!: FormGroup;
  resolveError = signal<string | null>(null);
  isSubmitting = signal<boolean>(false);

  ngOnInit() {
    this.resolveForm = this.fb.group({
      resolutionText: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]]
    });
    this.loadDisputes();

    this.signalrSub = this.signalrService.adminAlert$.subscribe({
      next: (alert) => {
        const type = alert.type || alert.Type;
        if (type === 'NEW_DISPUTE') {
          this.loadDisputes();
        }
      }
    });
  }

  ngOnDestroy() {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadDisputes() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.adminApi.getDisputes(this.selectedStatus(), this.currentPage(), this.pageSize).subscribe({
      next: (res) => {
        this.disputes.set(res.data || []);
        this.totalCount.set(res.total || 0);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to load customer disputes.');
        this.isLoading.set(false);
      }
    });
  }

  filterByStatus(status: DisputeStatus | undefined) {
    this.selectedStatus.set(status);
    this.currentPage.set(1);
    this.loadDisputes();
  }

  openResolveModal(id: string) {
    this.selectedDisputeId.set(id);
    this.resolveForm.reset();
    this.resolveError.set(null);
    this.showResolveModal.set(true);
  }

  closeResolveModal() {
    this.showResolveModal.set(false);
    this.selectedDisputeId.set(null);
  }

  submitResolution() {
    if (this.resolveForm.invalid || !this.selectedDisputeId()) return;

    this.isSubmitting.set(true);
    this.resolveError.set(null);

    const disputeId = this.selectedDisputeId()!;
    const text = this.resolveForm.value.resolutionText;

    this.adminApi.resolveDispute(disputeId, text).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.closeResolveModal();
        this.loadDisputes();
      },
      error: (err) => {
        this.resolveError.set(err.error?.message || 'Failed to resolve customer dispute.');
        this.isSubmitting.set(false);
      }
    });
  }

  nextPage() {
    if (this.currentPage() * this.pageSize < this.totalCount()) {
      this.currentPage.update(p => p + 1);
      this.loadDisputes();
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadDisputes();
    }
  }
}
