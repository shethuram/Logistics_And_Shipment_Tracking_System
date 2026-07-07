import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SessionService } from '../../../services/session.service';

@Component({
  selector: 'app-driver-status',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './status.html',
  styleUrl: './status.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DriverStatusComponent {
  public session = inject(SessionService);
}
