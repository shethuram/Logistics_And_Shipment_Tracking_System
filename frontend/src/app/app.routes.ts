import { Routes } from '@angular/router';
import { customerGuard, driverGuard, adminGuard, unregisteredGuard } from './guards/auth.guard';

export const routes: Routes = [
  { 
    path: '', 
    loadComponent: () => import('./components/landing-page/landing-page').then(m => m.LandingPageComponent) 
  },
  { 
    path: 'callback', 
    loadComponent: () => import('./components/auth/callback/callback').then(m => m.CallbackComponent) 
  },
  { 
    path: 'complete-profile', 
    loadComponent: () => import('./components/auth/complete-profile/complete-profile').then(m => m.CompleteProfileComponent), 
    canActivate: [unregisteredGuard] 
  },
  { 
    path: 'customer/book', 
    loadComponent: () => import('./components/customer/book-shipment/book-shipment').then(m => m.BookShipmentComponent), 
    canActivate: [customerGuard] 
  },
  { 
    path: 'customer/shipments', 
    loadComponent: () => import('./components/customer/shipments/shipments').then(m => m.CustomerShipmentsComponent), 
    canActivate: [customerGuard] 
  },
  { 
    path: 'customer/shipments/:id', 
    loadComponent: () => import('./components/customer/shipment-details/shipment-details').then(m => m.CustomerShipmentDetailsComponent), 
    canActivate: [customerGuard] 
  },
  { 
    path: 'customer/disputes', 
    loadComponent: () => import('./components/customer/disputes/disputes').then(m => m.CustomerDisputesComponent), 
    canActivate: [customerGuard] 
  },
  { 
    path: 'driver/jobs', 
    loadComponent: () => import('./components/driver/jobs/jobs').then(m => m.DriverJobsComponent), 
    canActivate: [driverGuard] 
  },
  { 
    path: 'driver/active', 
    loadComponent: () => import('./components/driver/active/active').then(m => m.DriverActiveComponent), 
    canActivate: [driverGuard] 
  },
  { 
    path: 'driver/vehicles', 
    loadComponent: () => import('./components/driver/vehicles/vehicles').then(m => m.DriverVehiclesComponent), 
    canActivate: [driverGuard] 
  },
  { 
    path: 'driver/rides', 
    loadComponent: () => import('./components/driver/rides/rides').then(m => m.DriverRidesComponent), 
    canActivate: [driverGuard] 
  },
  { 
    path: 'driver/status', 
    loadComponent: () => import('./components/driver/status/status').then(m => m.DriverStatusComponent), 
    canActivate: [driverGuard] 
  },
  { 
    path: 'admin/dashboard', 
    loadComponent: () => import('./components/admin/dashboard/dashboard').then(m => m.AdminDashboardComponent), 
    canActivate: [adminGuard] 
  },
  { 
    path: 'admin/drivers', 
    loadComponent: () => import('./components/admin/drivers/drivers').then(m => m.AdminDriversComponent), 
    canActivate: [adminGuard] 
  },
  { 
    path: 'admin/disputes', 
    loadComponent: () => import('./components/admin/disputes/disputes').then(m => m.AdminDisputesComponent), 
    canActivate: [adminGuard] 
  },
  { 
    path: 'admin/shipments', 
    loadComponent: () => import('./components/admin/shipments/shipments').then(m => m.AdminShipmentsComponent), 
    canActivate: [adminGuard] 
  },
  { 
    path: '**', 
    redirectTo: '' 
  }
];
