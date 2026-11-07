import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Post, LikeResponse } from '../models/comment.model';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PostService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly baseUrl = environment.apiBaseUrl;

  getPosts(): Observable<Post[]> {
    const headers = new HttpHeaders(this.authService.getAuthHeaders());
    return this.http.get<Post[]>(`${this.baseUrl}/posts`, { headers });
  }

  getPost(id: string): Observable<Post> {
    const headers = new HttpHeaders(this.authService.getAuthHeaders());
    return this.http.get<Post>(`${this.baseUrl}/posts/${id}`, { headers });
  }

  createPost(content: string, title: string = ''): Observable<Post> {
    const headers = new HttpHeaders(this.authService.getAuthHeaders());
    return this.http.post<Post>(
      `${this.baseUrl}/posts`,
      { content, title },
      { headers }
    );
  }

  toggleLike(postId: string): Observable<LikeResponse> {
    const headers = new HttpHeaders(this.authService.getAuthHeaders());
    return this.http.post<LikeResponse>(`${this.baseUrl}/posts/${postId}/like`, {}, { headers });
  }
}
