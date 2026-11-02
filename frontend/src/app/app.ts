import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { CommentSection } from './components/comment-section/comment-section';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, CommentSection],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly title = signal('Social Media');
  readonly authService = inject(AuthService);

  readonly defaultPostId = '11111111-1111-1111-1111-111111111111';
  readonly nonExistentPostId = '00000000-0000-0000-0000-000000000000';

  readonly currentPostId = signal(this.defaultPostId);
  readonly isTestingNonExistent = signal(false);

  toggleTestingNonExistent(): void {
    if (this.isTestingNonExistent()) {
      this.currentPostId.set(this.defaultPostId);
      this.isTestingNonExistent.set(false);
    } else {
      this.currentPostId.set(this.nonExistentPostId);
      this.isTestingNonExistent.set(true);
    }
  }

  toggleLogin(): void {
    if (this.authService.isLoggedIn()) {
      this.authService.logout();
    } else {
      this.authService.login({ id: 'user-1', name: 'Nguyễn Văn A' });
    }
  }
}
