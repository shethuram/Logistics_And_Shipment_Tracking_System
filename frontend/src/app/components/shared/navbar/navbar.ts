import { Component, inject, ChangeDetectionStrategy, signal, OnInit, OnDestroy, effect, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { SessionService } from '../../../services/session.service';
import { NotificationApiService } from '../../../services/notification.service';
import { SignalrService } from '../../../services/signalr.service';
import { NotificationDto } from '../../../dtos/notification.dto';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavbarComponent implements OnInit, OnDestroy {
  public session = inject(SessionService);
  private notificationApi = inject(NotificationApiService);
  private signalrService = inject(SignalrService);
  private router = inject(Router);

  public isMobileMenuOpen = signal(false);
  public notifications = signal<NotificationDto[]>([]);
  public unreadCount = signal<number>(0);
  public showDropdown = signal(false);

  notificationReceived = toSignal(this.signalrService.notificationReceived$);

  constructor() {
    effect(() => {
      const notif = this.notificationReceived();
      if (notif) {
        this.notifications.update(list => [notif, ...list.slice(0, 4)]);
        this.unreadCount.update(c => c + 1);
      }
    });

    effect(() => {
      const profile = this.session.profile();
      if (profile && profile.isRegistered && profile.userId) {
        untracked(() => {
          this.loadInitialNotifications();
          this.initializeSignalR(profile.userId!);
        });
      }
    }, { allowSignalWrites: true });
  }

  ngOnInit() {}

  loadInitialNotifications() {
    this.notificationApi.getMyNotifications(1, 5).subscribe({
      next: (res) => {
        this.notifications.set(res.data);
        this.unreadCount.set(res.unreadCount);
      },
      error: (err) => {
        console.error('Failed to load initial notifications:', err);
      }
    });
  }

  initializeSignalR(userId: string) {
    this.signalrService.startConnection().then(() => {
      this.signalrService.joinUserGroup(userId);
      if (this.session.role() === 'ADMIN') {
        this.signalrService.joinAdminGroup();
      }
    });
  }

  toggleMobileMenu() {
    this.isMobileMenuOpen.update(v => !v);
  }

  closeMobileMenu() {
    this.isMobileMenuOpen.set(false);
  }

  toggleDropdown() {
    this.showDropdown.update(v => !v);
  }

  closeDropdown() {
    this.showDropdown.set(false);
  }

  onNotificationClick(notification: NotificationDto) {
    this.closeDropdown();
    this.closeMobileMenu();

    if (!notification.isRead) {
      this.notificationApi.markAsRead(notification.id).subscribe({
        next: () => {
          this.unreadCount.update(c => Math.max(0, c - 1));
          this.notifications.update(list =>
            list.map(n => n.id === notification.id ? { ...n, isRead: true } : n)
          );
        }
      });
    }

    if (notification.shipmentId) {
      const role = this.session.role();
      if (role === 'CUSTOMER') {
        this.router.navigate(['/customer/shipments', notification.shipmentId]);
      } else if (role === 'DRIVER') {
        this.router.navigate(['/driver/jobs']);
      }
    }
  }

  ngOnDestroy() {
    const profile = this.session.profile();
    if (profile && profile.userId) {
      this.signalrService.leaveUserGroup(profile.userId);
    }
  }
}
