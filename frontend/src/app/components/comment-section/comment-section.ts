import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Comment } from '../../models/comment.model';
import { CommentService } from '../../services/comment.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-comment-section',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './comment-section.html',
  styleUrl: './comment-section.css'
})
export class CommentSection {
  readonly postId = input.required<string>();
  readonly hasViewPermission = input<boolean>(true);

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

  constructor() {
    // Tự động tải lại bình luận khi postId thay đổi hoặc khi khởi tạo component
    effect(() => {
      const id = this.postId();
      if (id) {
        this.loadComments(id);
      }
    });
  }

  loadComments(id: string = this.postId()): void {
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
        const serverError = err.error?.error || (err.status === 403 ? 'Bạn không có quyền xem bài đăng này.' : 'Không thể tải danh sách bình luận.');
        this.errorMessage.set(serverError);
      }
    });
  }

  onContentChange(value: string): void {
    this.content.set(value);
    // Xóa thông báo lỗi validation khi người dùng tiếp tục nhập
    if (this.errorMessage()) {
      this.errorMessage.set(null);
    }
  }

  submitComment(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    // Kiểm tra trạng thái đăng nhập
    if (!this.authService.isLoggedIn()) {
      this.errorMessage.set('Vui lòng đăng nhập để bình luận.');
      return;
    }

    // AC2: Người dùng không có quyền xem bài đăng -> Không cho phép gửi
    if (!this.hasViewPermission()) {
      this.errorMessage.set('Bạn không có quyền xem bài đăng này.');
      return;
    }

    const rawContent = this.content();

    // AC5: Nội dung trống hoặc chỉ có khoảng trắng
    if (!rawContent || rawContent.trim().length === 0) {
      this.errorMessage.set('Nội dung bình luận không được để trống hoặc chỉ chứa khoảng trắng.');
      return;
    }

    // AC6: Nội dung vượt quá 500 ký tự Unicode code points
    const codePointLength = [...rawContent].length;
    if (codePointLength > 500) {
      this.errorMessage.set('Nội dung bình luận không được vượt quá 500 ký tự.');
      return;
    }

    this.isSubmitting.set(true);

    this.commentService.addComment(this.postId(), rawContent).subscribe({
      next: (created) => {
        // AC1, AC7: Bình luận được đăng thành công và hiển thị trên bài đăng
        this.comments.update((prev) => [...prev, created]);
        this.content.set('');
        this.successMessage.set('Đăng bình luận thành công!');
        this.isSubmitting.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        // AC2: Không có quyền xem (403), hoặc lỗi bài đăng không tồn tại (404), hoặc lỗi khác
        const serverError = err.error?.error || (err.status === 403 ? 'Bạn không có quyền xem bài đăng này.' : 'Không thể gửi bình luận. Vui lòng thử lại sau.');
        this.errorMessage.set(serverError);
      }
    });
  }
}
