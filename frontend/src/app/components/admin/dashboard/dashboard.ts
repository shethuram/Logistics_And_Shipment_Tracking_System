import { Component, inject } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class AdminDashboardComponent {
  private auth = inject(AuthService);

  logout() {
    this.auth.logout({ logoutParams: { returnTo: window.location.origin } });
  }
}
