import { Component, effect, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { CommentSection } from './components/comment-section/comment-section';
import { NotificationCenter } from './components/notification-center/notification-center';
import { PostCreate } from './components/post-create/post-create';
import { AuthService } from './services/auth.service';
import { RelativeTimePipe } from './pipes/relative-time.pipe';
import { PostService } from './services/post.service';
import { Post } from './models/comment.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommentSection, NotificationCenter, PostCreate, RelativeTimePipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly title = signal('Social Media');
  readonly authService = inject(AuthService);
  private readonly postService = inject(PostService);
  private readonly router = inject(Router);

  readonly defaultPostId = '11111111-1111-1111-1111-111111111111';
  readonly currentPostId = signal(this.defaultPostId);
  // Timestamp cố định cho bài đăng mẫu trên giao diện (thay vì giá trị động theo Date.now())
  readonly postCreatedAt = signal('2026-11-06T08:00:00.000Z');

  readonly posts = signal<Post[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isLoginRoute = signal<boolean>(false);

  constructor() {
    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        this.isLoginRoute.set(
          event.urlAfterRedirects.includes('/login') || event.url.includes('/login')
        );
      }
    });

    // Tự động tải lại danh sách bài đăng khi trạng thái đăng nhập thay đổi (AC3)
    effect(() => {
      // Đọc trạng thái đăng nhập để kích hoạt effect khi login/logout
      this.authService.isLoggedIn();
      this.loadPosts();
    });
  }

  loadPosts(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.postService.getPosts().subscribe({
      next: (data) => {
        this.posts.set(data);
        if (data.length > 0) {
          this.currentPostId.set(data[0].id);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        // Fallback default post nếu gặp lỗi kết nối API
        if (this.posts().length === 0) {
          this.posts.set([
            {
              id: this.defaultPostId,
              authorId: 'author-system',
              title: 'Chào mừng bạn đến với mạng xã hội',
              content: 'Đây là bài đăng mẫu. Mọi người hãy để lại bình luận để chia sẻ ý kiến nhé!',
              createdAt: new Date().toISOString(),
              likeCount: 0,
              isLiked: false
            }
          ]);
        }
      }
    });
  }

  onPostCreated(newPost: Post): void {
    this.posts.update(list => [newPost, ...list]);
  }

  toggleLike(post: Post): void {
    // AC4: Khi chưa đăng nhập, chuyển hướng sang trang đăng nhập (PO yêu cầu)
    // và không thay đổi trạng thái thích của bài đăng hay tổng số lượt thích.
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }

    // AC1, AC2: Người dùng đã đăng nhập bấm Thích / bỏ thích
    this.postService.toggleLike(post.id).subscribe({
      next: (res) => {
        this.posts.update(list => list.map(p => {
          if (p.id === post.id) {
            return {
              ...p,
              isLiked: res.isLiked,
              likeCount: res.likeCount
            };
          }
          return p;
        }));
      },
      error: (err) => {
        console.error('Không thể cập nhật lượt thích:', err);
      }
    });
  }

  toggleLogin(): void {
    if (this.authService.isLoggedIn()) {
      this.authService.logout();
    } else {
      this.authService.login({ id: 'user-1', name: 'Nguyễn Văn A' });
    }
  }
}
