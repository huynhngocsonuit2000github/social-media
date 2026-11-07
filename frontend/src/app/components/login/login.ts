import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  onLogin(): void {
    this.authService.login({ id: 'user-1', name: 'Nguyễn Văn A' });
    this.router.navigate(['/']);
  }

  goHome(): void {
    this.router.navigate(['/']);
  }
}
