import { Component, OnInit, inject, ChangeDetectionStrategy, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthApiService } from '../../../services/auth.service';
import { SessionService } from '../../../services/session.service';
import { CustomerRegistrationDto, DriverRegistrationDto } from '../../../dtos/auth.dto';

@Component({
  selector: 'app-complete-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './complete-profile.html',
  styleUrl: './complete-profile.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CompleteProfileComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auth = inject(AuthService);
  private authApi = inject(AuthApiService);
  private session = inject(SessionService);
  private fb = inject(FormBuilder);

  userType = signal<'CUSTOMER' | 'DRIVER'>('CUSTOMER');
  auth0Id = '';
  email = '';
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);

  profileForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    phone: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    licenseNumber: ['', [Validators.pattern(/^[A-Z]{2}\d{2} \d{11}$/)]]
  });

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const type = params['type'];
      const resolvedType = type === 'DRIVER' ? 'DRIVER' : 'CUSTOMER';
      this.userType.set(resolvedType);
      
      if (resolvedType === 'DRIVER') {
        this.profileForm.controls.licenseNumber.setValidators([
          Validators.required, 
          Validators.pattern(/^[A-Z]{2}\d{2} \d{11}$/)
        ]);
      } else {
        this.profileForm.controls.licenseNumber.clearValidators();
      }
      this.profileForm.controls.licenseNumber.updateValueAndValidity();
    });

    this.auth.user$.subscribe(user => {
      if (!user) return;
      this.auth0Id = user.sub || '';
      this.email = user.email || '';
      const isEmail = user.name && user.name.includes('@');
      this.profileForm.patchValue({
        fullName: isEmail ? '' : (user.name || '')
      });
    });
  }

  onSubmit() {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const formValues = this.profileForm.getRawValue();
    const type = this.userType();

    const request$ = type === 'DRIVER'
      ? this.authApi.registerDriver({
          auth0Id: this.auth0Id,
          fullName: formValues.fullName,
          email: this.email,
          phone: formValues.phone,
          licenseNumber: formValues.licenseNumber
        } as DriverRegistrationDto)
      : this.authApi.registerCustomer({
          auth0Id: this.auth0Id,
          fullName: formValues.fullName,
          email: this.email,
          phone: formValues.phone
        } as CustomerRegistrationDto);

    request$.subscribe({
      next: () => {
        this.session.clearSession();
        this.isLoading.set(false);
        this.navigateToDashboard(type);
      },
      error: (err) => {
        this.isLoading.set(false);
        const errorMsg = err.error?.message || '';
        if (err.status === 409 && errorMsg.includes('Auth0 ID')) {
          this.session.clearSession();
          this.navigateToDashboard(type);
        } else {
          this.errorMessage.set(errorMsg || 'Failed to save profile. Please try again.');
        }
      }
    });
  }

  private navigateToDashboard(role: string) {
    if (role === 'DRIVER') {
      this.router.navigate(['/driver/status']);
    } else {
      this.router.navigate(['/customer/book']);
    }
  }
}
