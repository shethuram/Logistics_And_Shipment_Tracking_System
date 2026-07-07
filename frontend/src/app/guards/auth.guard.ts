import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { SessionService } from '../services/session.service';
import { map } from 'rxjs/operators';

export const customerGuard: CanActivateFn = () => {
  const session = inject(SessionService);
  const router = inject(Router);

  return session.resolveSession().pipe(
    map(profile => {
      if (!profile) {
        router.navigate(['/']);
        return false;
      }
      if (!profile.isRegistered) {
        router.navigate(['/complete-profile'], { queryParams: { type: 'CUSTOMER' } });
        return false;
      }
      if (profile.role !== 'CUSTOMER') {
        if (profile.role === 'DRIVER') {
          router.navigate([profile.driver?.approvalStatus === 'APPROVED' ? '/driver/jobs' : '/driver/status']);
        } else if (profile.role === 'ADMIN') {
          router.navigate(['/admin/dashboard']);
        } else {
          router.navigate(['/']);
        }
        return false;
      }
      return true;
    })
  );
};

export const driverGuard: CanActivateFn = (route) => {
  const session = inject(SessionService);
  const router = inject(Router);

  return session.resolveSession().pipe(
    map(profile => {
      if (!profile) {
        router.navigate(['/']);
        return false;
      }
      if (!profile.isRegistered) {
        router.navigate(['/complete-profile'], { queryParams: { type: 'DRIVER' } });
        return false;
      }
      if (profile.role !== 'DRIVER') {
        if (profile.role === 'CUSTOMER') {
          router.navigate(['/customer/book']);
        } else if (profile.role === 'ADMIN') {
          router.navigate(['/admin/dashboard']);
        } else {
          router.navigate(['/']);
        }
        return false;
      }

      const status = profile.driver?.approvalStatus;
      const isStatusPage = route.url.some(segment => segment.path === 'status');

      if (status !== 'APPROVED') {
        if (!isStatusPage) {
          router.navigate(['/driver/status']);
          return false;
        }
      } else {
        if (isStatusPage) {
          router.navigate(['/driver/jobs']);
          return false;
        }
      }
      return true;
    })
  );
};

export const adminGuard: CanActivateFn = () => {
  const session = inject(SessionService);
  const router = inject(Router);

  return session.resolveSession().pipe(
    map(profile => {
      if (!profile) {
        router.navigate(['/']);
        return false;
      }
      if (!profile.isRegistered) {
        router.navigate(['/complete-profile'], { queryParams: { type: 'CUSTOMER' } });
        return false;
      }
      if (profile.role !== 'ADMIN') {
        if (profile.role === 'CUSTOMER') {
          router.navigate(['/customer/book']);
        } else if (profile.role === 'DRIVER') {
          router.navigate([profile.driver?.approvalStatus === 'APPROVED' ? '/driver/jobs' : '/driver/status']);
        } else {
          router.navigate(['/']);
        }
        return false;
      }
      return true;
    })
  );
};

export const unregisteredGuard: CanActivateFn = () => {
  const session = inject(SessionService);
  const router = inject(Router);

  return session.resolveSession().pipe(
    map(profile => {
      if (!profile) {
        router.navigate(['/']);
        return false;
      }
      if (profile.isRegistered) {
        if (profile.role === 'CUSTOMER') {
          router.navigate(['/customer/book']);
        } else if (profile.role === 'DRIVER') {
          router.navigate([profile.driver?.approvalStatus === 'APPROVED' ? '/driver/jobs' : '/driver/status']);
        } else if (profile.role === 'ADMIN') {
          router.navigate(['/admin/dashboard']);
        }
        return false;
      }
      return true;
    })
  );
};
