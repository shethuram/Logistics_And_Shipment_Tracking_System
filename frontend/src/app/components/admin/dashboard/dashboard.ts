import { Component, OnInit, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminApiService } from '../../../services/admin';
import { AdminMetricsResponse } from '../../../dtos/admin.dto';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDashboardComponent implements OnInit {
  private adminApi = inject(AdminApiService);

  metrics = signal<AdminMetricsResponse | null>(null);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  chartGradient = computed(() => {
    const m = this.metrics();
    if (!m) return 'conic-gradient(#262626 0% 100%)';

    const total = m.delivered + m.pending + m.cancelled + m.failed + m.staleShipments;
    if (total === 0) return 'conic-gradient(#262626 0% 100%)';

    const pDelivered = (m.delivered / total) * 100;
    const pPending = (m.pending / total) * 100;
    const pFailed = (m.failed / total) * 100;
    const pCancelled = (m.cancelled / total) * 100;
    const pStale = (m.staleShipments / total) * 100;

    let current = 0;
    const slices = [
      { color: '#22C55E', val: pDelivered },
      { color: '#E8A845', val: pPending },
      { color: '#EF4444', val: pFailed },
      { color: '#737373', val: pCancelled },
      { color: '#3B82F6', val: pStale }
    ];

    const gradientParts = slices
      .filter(s => s.val > 0)
      .map(s => {
        const start = current;
        current += s.val;
        return `${s.color} ${start.toFixed(1)}% ${current.toFixed(1)}%`;
      });

    if (gradientParts.length === 0) return 'conic-gradient(#262626 0% 100%)';
    return `conic-gradient(${gradientParts.join(', ')})`;
  });

  ngOnInit() {
    this.loadMetrics();
  }

  loadMetrics() {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.adminApi.getMetrics().subscribe({
      next: (res) => {
        this.metrics.set(res);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to load system metrics.');
        this.isLoading.set(false);
      }
    });
  }
}
