import { Component, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AppNotification } from '../../models/comment.model';
import { NotificationService } from '../../services/notification.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './notification-center.html',
  styleUrl: './notification-center.css'
})
export class NotificationCenter {
  private readonly notificationService = inject(NotificationService);
  readonly authService = inject(AuthService);

  readonly isOpen = signal<boolean>(false);
  readonly notifications = signal<AppNotification[]>([]);
  readonly isLoading = signal<boolean>(false);

  readonly count = computed(() => this.notifications().length);

  constructor() {
    // Tự động tải thông báo khi người dùng đăng nhập
    effect(() => {
      const user = this.authService.currentUser();
      if (user) {
        this.loadNotifications();
      } else {
        this.notifications.set([]);
        this.isOpen.set(false);
      }
    });
  }

  toggleOpen(): void {
    this.isOpen.update((open) => !open);
  }

  refresh(): void {
    this.loadNotifications();
  }

  loadNotifications(): void {
    if (!this.authService.isLoggedIn()) {
      return;
    }

    this.isLoading.set(true);
    this.notificationService.getNotifications().subscribe({
      next: (list) => {
        this.notifications.set(list);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }
}
