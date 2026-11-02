import { Injectable, signal, computed } from '@angular/core';
import { User } from '../models/comment.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly _currentUser = signal<User | null>({
    id: 'user-1',
    name: 'Nguyễn Văn A'
  });

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
