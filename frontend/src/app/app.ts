import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommentSection } from './components/comment-section/comment-section';
import { NotificationCenter } from './components/notification-center/notification-center';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommentSection, NotificationCenter],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly title = signal('Social Media');
  readonly authService = inject(AuthService);

  readonly defaultPostId = '11111111-1111-1111-1111-111111111111';
  readonly currentPostId = signal(this.defaultPostId);
  readonly hasViewPermission = signal<boolean>(true);

  toggleLogin(): void {
    if (this.authService.isLoggedIn()) {
      this.authService.logout();
    } else {
      this.authService.login({ id: 'user-1', name: 'Nguyễn Văn A' });
    }
  }

  loginAsPostAuthor(): void {
    this.authService.login({ id: 'author-system', name: 'Chủ bài đăng (Hệ thống)' });
  }

  toggleViewPermission(): void {
    this.hasViewPermission.update((v) => !v);
  }
}
