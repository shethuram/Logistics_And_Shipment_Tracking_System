import { Routes } from '@angular/router';
import { LandingPageComponent } from './components/landing-page/landing-page';
import { CallbackComponent } from './components/auth/callback/callback';
import { CompleteProfileComponent } from './components/auth/complete-profile/complete-profile';
import { CustomerShipmentsComponent } from './components/customer/shipments/shipments';
import { DriverJobsComponent } from './components/driver/jobs/jobs';
import { DriverActiveComponent } from './components/driver/active/active';
import { DriverVehiclesComponent } from './components/driver/vehicles/vehicles';
import { DriverStatusComponent } from './components/driver/status/status';
import { DriverRidesComponent } from './components/driver/rides/rides';
import { AdminDashboardComponent } from './components/admin/dashboard/dashboard';
import { AdminDriversComponent } from './components/admin/drivers/drivers';
import { AdminDisputesComponent } from './components/admin/disputes/disputes';
import { AdminShipmentsComponent } from './components/admin/shipments/shipments';
import { BookShipmentComponent } from './components/customer/book-shipment/book-shipment';
import { CustomerShipmentDetailsComponent } from './components/customer/shipment-details/shipment-details';
import { CustomerDisputesComponent } from './components/customer/disputes/disputes';
import { customerGuard, driverGuard, adminGuard, unregisteredGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', component: LandingPageComponent },
  { path: 'callback', component: CallbackComponent },
  { path: 'complete-profile', component: CompleteProfileComponent, canActivate: [unregisteredGuard] },
  { path: 'customer/book', component: BookShipmentComponent, canActivate: [customerGuard] },
  { path: 'customer/shipments', component: CustomerShipmentsComponent, canActivate: [customerGuard] },
  { path: 'customer/shipments/:id', component: CustomerShipmentDetailsComponent, canActivate: [customerGuard] },
  { path: 'customer/disputes', component: CustomerDisputesComponent, canActivate: [customerGuard] },
  { path: 'driver/jobs', component: DriverJobsComponent, canActivate: [driverGuard] },
  { path: 'driver/active', component: DriverActiveComponent, canActivate: [driverGuard] },
  { path: 'driver/vehicles', component: DriverVehiclesComponent, canActivate: [driverGuard] },
  { path: 'driver/rides', component: DriverRidesComponent, canActivate: [driverGuard] },
  { path: 'driver/status', component: DriverStatusComponent, canActivate: [driverGuard] },
  { path: 'admin/dashboard', component: AdminDashboardComponent, canActivate: [adminGuard] },
  { path: 'admin/drivers', component: AdminDriversComponent, canActivate: [adminGuard] },
  { path: 'admin/disputes', component: AdminDisputesComponent, canActivate: [adminGuard] },
  { path: 'admin/shipments', component: AdminShipmentsComponent, canActivate: [adminGuard] }
];
