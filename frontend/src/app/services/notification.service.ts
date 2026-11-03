import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppNotification } from '../models/comment.model';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly baseUrl = environment.apiBaseUrl;

  getNotifications(): Observable<AppNotification[]> {
    const headers = new HttpHeaders(this.authService.getAuthHeaders());
    return this.http.get<AppNotification[]>(`${this.baseUrl}/notifications`, { headers });
  }
}
