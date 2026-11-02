import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Comment } from '../../models/comment.model';
import { CommentService } from '../../services/comment.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-comment-section',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './comment-section.html',
  styleUrl: './comment-section.css'
})
export class CommentSection implements OnInit {
  readonly postId = input.required<string>();

  private readonly commentService = inject(CommentService);
  readonly authService = inject(AuthService);

  readonly comments = signal<Comment[]>([]);
  readonly content = signal<string>('');
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly isSubmitting = signal<boolean>(false);

  readonly charCount = computed(() => {
    const text = this.content();
    return [...text].length;
  });

  ngOnInit(): void {
    this.loadComments();
  }

  loadComments(): void {
    const id = this.postId();
    if (!id) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.commentService.getComments(id).subscribe({
      next: (list) => {
        this.comments.set(list);
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        const serverError = err.error?.error || 'Không thể tải danh sách bình luận.';
        this.errorMessage.set(serverError);
      }
    });
  }

  onContentChange(value: string): void {
    this.content.set(value);
    // Clear validation error when user types
    if (this.errorMessage()) {
      this.errorMessage.set(null);
    }
  }

  submitComment(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    // AC7: Người dùng chưa đăng nhập
    if (!this.authService.isLoggedIn()) {
      this.errorMessage.set('Vui lòng đăng nhập để bình luận.');
      return;
    }

    const rawContent = this.content();

    // AC4: Nội dung trống hoặc chỉ có khoảng trắng
    if (!rawContent || rawContent.trim().length === 0) {
      this.errorMessage.set('Nội dung bình luận không được để trống hoặc chỉ chứa khoảng trắng.');
      return;
    }

    // AC3: Nội dung vượt quá 500 ký tự Unicode code points
    const codePointLength = [...rawContent].length;
    if (codePointLength > 500) {
      this.errorMessage.set('Nội dung bình luận không được vượt quá 500 ký tự.');
      return;
    }

    this.isSubmitting.set(true);

    this.commentService.addComment(this.postId(), rawContent).subscribe({
      next: (created) => {
        // AC1, AC2: Bình luận được đăng thành công và hiển thị ở cuối danh sách
        this.comments.update((prev) => [...prev, created]);
        this.content.set('');
        this.successMessage.set('Đăng bình luận thành công!');
        this.isSubmitting.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        // AC8: Bình luận trên bài đăng không tồn tại hoặc lỗi khác từ hệ thống
        const serverError = err.error?.error || 'Không thể gửi bình luận. Vui lòng thử lại sau.';
        this.errorMessage.set(serverError);
      }
    });
  }
}
