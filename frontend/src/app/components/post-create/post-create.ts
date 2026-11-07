import { Component, computed, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Post } from '../../models/comment.model';
import { PostService } from '../../services/post.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-post-create',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './post-create.html',
  styleUrl: './post-create.css'
})
export class PostCreate {
  private readonly postService = inject(PostService);
  readonly authService = inject(AuthService);

  readonly postCreated = output<Post>();

  readonly title = signal<string>('');
  readonly content = signal<string>('');
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly isSubmitting = signal<boolean>(false);

  readonly charCount = computed(() => {
    const text = this.content();
    return [...text].length;
  });

  readonly remainingChars = computed(() => {
    return 500 - this.charCount();
  });

  readonly isContentValid = computed(() => {
    const text = this.content();
    return text.trim().length > 0 && this.charCount() <= 500;
  });

  onContentChange(value: string): void {
    this.content.set(value);
    if (this.errorMessage()) {
      this.errorMessage.set(null);
    }
  }

  onTitleChange(value: string): void {
    this.title.set(value);
  }

  submitPost(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (!this.authService.isLoggedIn()) {
      this.errorMessage.set('Vui lòng đăng nhập để tạo bài đăng.');
      return;
    }

    const rawContent = this.content();

    // AC7, AC8: Kiểm tra rỗng và khoảng trắng
    if (!rawContent || rawContent.length === 0) {
      this.errorMessage.set('Nội dung bài đăng không được để trống.');
      return;
    }

    if (rawContent.trim().length === 0) {
      this.errorMessage.set('Nội dung bài đăng không hợp lệ hoặc không được để trống.');
      return;
    }

    // AC6: Vượt quá 500 ký tự
    const codePointLength = [...rawContent].length;
    if (codePointLength > 500) {
      this.errorMessage.set('Nội dung bài đăng không được vượt quá 500 ký tự.');
      return;
    }

    this.isSubmitting.set(true);

    this.postService.createPost(rawContent, this.title()).subscribe({
      next: (created) => {
        this.postCreated.emit(created);
        this.content.set('');
        this.title.set('');
        this.successMessage.set('Đăng bài thành công!');
        this.isSubmitting.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        const serverError = err.error?.error || 'Không thể tạo bài đăng. Vui lòng thử lại sau.';
        this.errorMessage.set(serverError);
      }
    });
  }
}
