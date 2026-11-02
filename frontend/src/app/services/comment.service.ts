import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Comment, Post } from '../models/comment.model';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class CommentService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly baseUrl = 'http://localhost:5154/api';

  countCodePoints(text: string): number {
    if (!text) {
      return 0;
    }
    return [...text].length;
  }

  getPosts(): Observable<Post[]> {
    return this.http.get<Post[]>(`${this.baseUrl}/posts`);
  }

  getPost(postId: string): Observable<Post> {
    return this.http.get<Post>(`${this.baseUrl}/posts/${postId}`);
  }

  getComments(postId: string): Observable<Comment[]> {
    return this.http.get<Comment[]>(`${this.baseUrl}/posts/${postId}/comments`);
  }

  addComment(postId: string, content: string): Observable<Comment> {
    const headers = new HttpHeaders(this.authService.getAuthHeaders());
    return this.http.post<Comment>(
      `${this.baseUrl}/posts/${postId}/comments`,
      { content },
      { headers }
    );
  }
}
