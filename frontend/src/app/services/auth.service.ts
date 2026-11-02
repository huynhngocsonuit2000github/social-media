import { Injectable, signal, computed } from '@angular/core';
import { User } from '../models/comment.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // Mặc định khởi tạo null (chưa đăng nhập) theo phản hồi review
  private readonly _currentUser = signal<User | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn = computed(() => this._currentUser() !== null);

  login(user: User = { id: 'user-1', name: 'Nguyễn Văn A' }): void {
    this._currentUser.set(user);
  }

  logout(): void {
    this._currentUser.set(null);
  }

  getAuthHeaders(): Record<string, string> {
    const user = this._currentUser();
    if (!user) {
      return {};
    }

    return {
      'Authorization': `Bearer ${user.id}`,
      'X-User-Id': user.id,
      'X-User-Name': user.name
    };
  }
}
