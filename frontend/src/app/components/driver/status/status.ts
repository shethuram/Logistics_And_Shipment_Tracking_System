import { Component, inject, ChangeDetectionStrategy, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { SessionService } from '../../../services/session.service';
import { DriverApiService } from '../../../services/driver.service';
import { SignalrService } from '../../../services/signalr.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-driver-status',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './status.html',
  styleUrl: './status.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DriverStatusComponent implements OnInit, OnDestroy {
  public session = inject(SessionService);
  private driverApi = inject(DriverApiService);
  private fb = inject(FormBuilder);
  private signalrService = inject(SignalrService);

  private signalrSub!: Subscription;

  showCorrectionForm = signal(false);
  isLoading = signal(false);
  fileError = signal<string | null>(null);
  selectedFile: File | null = null;

  profileForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    phone: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    licenseNumber: ['', [Validators.required, Validators.pattern(/^[A-Z]{2}\d{2} \d{11}$/)]]
  });

  toggleCorrectionForm() {
    const user = this.session.profile();
    if (user) {
      this.profileForm.patchValue({
        fullName: user.fullName || '',
        phone: user.phone || '',
        licenseNumber: user.driver?.licenseNumber || ''
      });
    }
    this.showCorrectionForm.set(!this.showCorrectionForm());
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      if (file.size > 5 * 1024 * 1024) {
        this.fileError.set('File size must be less than 5MB.');
        this.selectedFile = null;
      } else {
        this.fileError.set(null);
        this.selectedFile = file;
      }
    }
  }

  onSubmit() {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const formValues = this.profileForm.getRawValue();
    const formData = new FormData();
    formData.append('fullName', formValues.fullName);
    formData.append('phone', formValues.phone);
    formData.append('licenseNumber', formValues.licenseNumber);
    if (this.selectedFile) {
      formData.append('licenseFile', this.selectedFile);
    }

    this.driverApi.updateProfile(formData).subscribe({
      next: () => {
        this.session.refreshProfile().subscribe(() => {
          this.isLoading.set(false);
          this.showCorrectionForm.set(false);
        });
      },
      error: (err) => {
        this.isLoading.set(false);
        alert(err.error?.message || 'Failed to update profile.');
      }
    });
  }

  ngOnInit() {
    this.signalrSub = this.signalrService.notificationReceived$.subscribe({
      next: (n) => {
        const titles = ['AI License Verification Update', 'Profile Approved', 'Profile Rejected', 'Profile Suspended'];
        if (titles.includes(n.title)) {
          this.session.refreshProfile().subscribe();
        }
      }
    });
  }

  ngOnDestroy() {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }
}
