import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { PublicTrackingService } from '../../services/public-tracking.service';
import { PublicTrackingResponseDto } from '../../dtos/tracking.dto';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.css'
})
export class LandingPageComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private trackingService = inject(PublicTrackingService);
  private fb = inject(FormBuilder);

  trackForm = this.fb.nonNullable.group({
    orderId: ['', Validators.required],
    phone: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    date: ['', Validators.required]
  });

  trackingResult = signal<PublicTrackingResponseDto | null>(null);
  trackingError = signal<string | null>(null);
  isLoading = signal<boolean>(false);

  becomeCustomer() {
    this.auth.loginWithRedirect({
      appState: { target: '/callback', userType: 'CUSTOMER' },
      authorizationParams: {
        screen_hint: 'signup',
        prompt: 'login'
      }
    });
  }

  becomeDriver() {
    this.auth.loginWithRedirect({
      appState: { target: '/callback', userType: 'DRIVER' },
      authorizationParams: {
        screen_hint: 'signup',
        prompt: 'login'
      }
    });
  }

  login() {
    this.auth.loginWithRedirect({
      appState: { target: '/callback' },
      authorizationParams: {
        prompt: 'login'
      }
    });
  }

  adminLogin() {
    this.auth.loginWithRedirect({
      appState: { target: '/callback', userType: 'ADMIN' },
      authorizationParams: {
        prompt: 'login'
      }
    });
  }

  trackPackage() {
    if (this.trackForm.invalid) {
      this.trackForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.trackingResult.set(null);
    this.trackingError.set(null);

    const { orderId, phone, date } = this.trackForm.getRawValue();

    this.trackingService.trackShipment(orderId, phone, date).subscribe({
      next: (res) => {
        this.trackingResult.set(res);
        this.isLoading.set(false);
      },
      error: () => {
        this.trackingError.set('No shipment found. Please check your details.');
        this.isLoading.set(false);
      }
    });
  }
}
