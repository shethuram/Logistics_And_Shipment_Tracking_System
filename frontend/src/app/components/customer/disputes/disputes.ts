import { Component, OnInit, inject, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DisputeApiService } from '../../../services/dispute.service';
import { DisputeResponse } from '../../../dtos/dispute.dto';

@Component({
  selector: 'app-customer-disputes',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './disputes.html',
  styleUrl: './disputes.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CustomerDisputesComponent implements OnInit {
  private disputeApi = inject(DisputeApiService);

  disputes = signal<DisputeResponse[]>([]);
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);
  expandedDisputeId = signal<string | null>(null);

  ngOnInit() {
    this.loadDisputes();
  }

  loadDisputes() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.disputeApi.getMyDisputes().subscribe({
      next: (res) => {
        this.disputes.set(res);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to load disputes history.');
      }
    });
  }

  toggleExpand(disputeId: string) {
    if (this.expandedDisputeId() === disputeId) {
      this.expandedDisputeId.set(null);
    } else {
      this.expandedDisputeId.set(disputeId);
    }
  }
}
