import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { CommonModule } from '@angular/common';
import { SessionService } from '../../../services/session.service';

@Component({
  selector: 'app-callback',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './callback.html',
  styleUrl: './callback.css'
})
export class CallbackComponent implements OnInit {
  private auth = inject(AuthService);
  private router = inject(Router);
  private session = inject(SessionService);

  ngOnInit() {
    this.auth.user$.subscribe(user => {
      if (!user) return;

      this.session.loadProfile().subscribe({
        next: (profile) => {
          if (profile.isRegistered && profile.role) {
            this.navigateToDashboard(profile);
          } else {
            this.auth.appState$.subscribe(state => {
              const userType = state?.['userType'] || 'CUSTOMER';
              this.router.navigate(['/complete-profile'], {
                queryParams: { type: userType }
              });
            });
          }
        },
        error: () => {
          this.router.navigate(['/']);
        }
      });
    });
  }

  private navigateToDashboard(profile: any) {
    if (profile.role === 'ADMIN') {
      this.router.navigate(['/admin/dashboard']);
    } else if (profile.role === 'DRIVER') {
      if (profile.driver?.approvalStatus === 'APPROVED') {
        this.router.navigate(['/driver/jobs']);
      } else {
        this.router.navigate(['/driver/status']);
      }
    } else {
      this.router.navigate(['/customer/book']);
    }
  }
}
