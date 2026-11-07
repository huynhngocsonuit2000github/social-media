import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { App } from './app';
import { AuthService } from './services/auth.service';
import { PostService } from './services/post.service';
import { CommentSection } from './components/comment-section/comment-section';
import { Post, LikeResponse } from './models/comment.model';
import { environment } from '../environments/environment';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let app: App;
  let authService: AuthService;
  let httpTesting: HttpTestingController;
  let routerMock: { events: { subscribe: ReturnType<typeof vi.fn> }; navigate: ReturnType<typeof vi.fn>; url: string };

  const baseUrl = environment.apiBaseUrl;

  const mockPosts: Post[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      authorId: 'author-system',
      title: 'Chào mừng bạn đến với mạng xã hội',
      content: 'Đây là bài đăng mẫu.',
      createdAt: '2026-11-06T10:00:00Z',
      likeCount: 5,
      isLiked: false
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      authorId: 'author-user-2',
      title: 'Bài viết số hai',
      content: 'Nội dung bài viết hai.',
      createdAt: '2026-11-06T11:00:00Z',
      likeCount: 10,
      isLiked: true
    }
  ];

  beforeEach(async () => {
    TestBed.resetTestingModule();

    routerMock = {
      events: {
        subscribe: vi.fn()
      },
      navigate: vi.fn(),
      url: '/'
    };

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService,
        PostService,
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    httpTesting = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);

    fixture = TestBed.createComponent(App);
    app = fixture.componentInstance;
  });

  afterEach(() => {
    // Flush any remaining child requests
    const remaining = httpTesting.match(() => true);
    for (const r of remaining) {
      r.flush([]);
    }
    httpTesting.verify();
  });

  function setupAndFlushPosts(posts: Post[]) {
    fixture.detectChanges();
    const req = httpTesting.expectOne(`${baseUrl}/posts`);
    req.flush(posts);

    // Flush any requests from child components (NotificationCenter, CommentSection)
    const childRequests = httpTesting.match(r => r.url.includes('/comments') || r.url.includes('/notifications'));
    for (const r of childRequests) {
      r.flush([]);
    }
  }

  it('should create the app', () => {
    expect(app).toBeTruthy();
  });

  it('should render title', async () => {
    setupAndFlushPosts([]);

    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.logo')?.textContent).toContain('Social Media');
  });

  it('SM-7: should render relative time for the post', async () => {
    // Thiết lập thời gian bài đăng cách đây 30 giây
    const initialTime = new Date(Date.now() - 30 * 1000).toISOString();
    app.postCreatedAt.set(initialTime);

    setupAndFlushPosts([
      {
        id: app.defaultPostId,
        authorId: 'author-system',
        title: 'Chào mừng bạn đến với mạng xã hội',
        content: 'Đây là bài đăng mẫu.',
        createdAt: app.postCreatedAt(),
        likeCount: 0,
        isLiked: false
      }
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const postTime = compiled.querySelector('.post-time');
    expect(postTime).toBeTruthy();
    expect(postTime?.textContent?.trim()).toBe('vừa xong');

    // Cập nhật thời gian bài đăng thành 1 giờ trước
    const oneHourAgo = new Date(Date.now() - (3600 + 1) * 1000).toISOString();
    app.postCreatedAt.set(oneHourAgo);
    app.posts.update(posts => [{ ...posts[0], createdAt: oneHourAgo }]);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(postTime?.textContent?.trim()).toBe('1 giờ trước');
  });

  it('AC3: should render list of posts with each post displaying like count and current user like status', async () => {
    // Arrange: Đăng nhập
    authService.login({ id: 'user-1', name: 'Nguyễn Văn A' });
    setupAndFlushPosts(mockPosts);

    await fixture.whenStable();
    fixture.detectChanges();

    // Verify 2 bài viết được hiển thị trong danh sách
    const postCards = fixture.nativeElement.querySelectorAll('.post-card');
    expect(postCards.length).toBe(2);

    // Bài viết 1: likeCount = 5, isLiked = false
    const likeBtn1 = fixture.nativeElement.querySelector('[data-testid="like-btn-11111111-1111-1111-1111-111111111111"]');
    expect(likeBtn1).toBeTruthy();
    expect(likeBtn1.classList.contains('liked')).toBe(false);
    expect(likeBtn1.textContent).toContain('Thích');
    expect(likeBtn1.textContent).toContain('(5)');

    // Bài viết 2: likeCount = 10, isLiked = true
    const likeBtn2 = fixture.nativeElement.querySelector('[data-testid="like-btn-22222222-2222-2222-2222-222222222222"]');
    expect(likeBtn2).toBeTruthy();
    expect(likeBtn2.classList.contains('liked')).toBe(true);
    expect(likeBtn2.textContent).toContain('Đã thích');
    expect(likeBtn2.textContent).toContain('(10)');
  });

  it('AC1: should mark post as liked and increase total likes by 1 when logged-in user clicks Thich on unliked post', async () => {
    authService.login({ id: 'user-1', name: 'Nguyễn Văn A' });
    setupAndFlushPosts([mockPosts[0]]); // likeCount: 5, isLiked: false

    await fixture.whenStable();
    fixture.detectChanges();

    const likeBtn = fixture.nativeElement.querySelector('[data-testid="like-btn-11111111-1111-1111-1111-111111111111"]');
    expect(likeBtn.textContent).toContain('(5)');
    expect(likeBtn.textContent).toContain('Thích');

    // Act: Bấm nút Thích
    likeBtn.click();

    // Mock API response
    const likeReq = httpTesting.expectOne(`${baseUrl}/posts/11111111-1111-1111-1111-111111111111/like`);
    expect(likeReq.request.method).toBe('POST');
    const mockRes: LikeResponse = {
      postId: '11111111-1111-1111-1111-111111111111',
      isLiked: true,
      likeCount: 6
    };
    likeReq.flush(mockRes);

    await fixture.whenStable();
    fixture.detectChanges();

    // Assert: Đã thích, tổng số lượt thích tăng thêm 1 (6)
    expect(likeBtn.classList.contains('liked')).toBe(true);
    expect(likeBtn.textContent).toContain('Đã thích');
    expect(likeBtn.textContent).toContain('(6)');
  });

  it('AC2: should unmark post as liked and decrease total likes by 1 when logged-in user clicks Thich on liked post', async () => {
    authService.login({ id: 'user-1', name: 'Nguyễn Văn A' });
    setupAndFlushPosts([mockPosts[1]]); // likeCount: 10, isLiked: true

    await fixture.whenStable();
    fixture.detectChanges();

    const likeBtn = fixture.nativeElement.querySelector('[data-testid="like-btn-22222222-2222-2222-2222-222222222222"]');
    expect(likeBtn.textContent).toContain('(10)');
    expect(likeBtn.textContent).toContain('Đã thích');

    // Act: Bấm nút Thích (để bỏ thích)
    likeBtn.click();

    // Mock API response
    const likeReq = httpTesting.expectOne(`${baseUrl}/posts/22222222-2222-2222-2222-222222222222/like`);
    expect(likeReq.request.method).toBe('POST');
    const mockRes: LikeResponse = {
      postId: '22222222-2222-2222-2222-222222222222',
      isLiked: false,
      likeCount: 9
    };
    likeReq.flush(mockRes);

    await fixture.whenStable();
    fixture.detectChanges();

    // Assert: Chưa thích, tổng số lượt thích giảm đi 1 (9)
    expect(likeBtn.classList.contains('liked')).toBe(false);
    expect(likeBtn.textContent).toContain('Thích');
    expect(likeBtn.textContent).toContain('(9)');
  });

  it('AC4: should redirect to login page and not change likes or state when unauthenticated user clicks Thich', async () => {
    // Chưa đăng nhập
    authService.logout();
    setupAndFlushPosts([mockPosts[0]]);

    await fixture.whenStable();
    fixture.detectChanges();

    const likeBtn = fixture.nativeElement.querySelector('[data-testid="like-btn-11111111-1111-1111-1111-111111111111"]');
    expect(likeBtn.textContent).toContain('(5)');

    // Act: Người dùng chưa đăng nhập bấm nút Thích
    likeBtn.click();

    // Assert: Điều hướng sang trang /login theo câu trả lời của PO
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);

    // Assert: Không gửi request toggle like lên server
    httpTesting.expectNone(`${baseUrl}/posts/11111111-1111-1111-1111-111111111111/like`);

    // Assert: Trạng thái và số lượt thích không thay đổi
    expect(app.posts()[0].likeCount).toBe(5);
    expect(app.posts()[0].isLiked).toBe(false);
  });

  it('AC1: should display "0 bình luận" on post UI when post has 0 comments', async () => {
    // Arrange: Bài đăng có 0 bình luận
    const postWith0Comments: Post = {
      id: 'p-0-comment',
      authorId: 'author-0',
      title: 'Bài viết chưa có bình luận',
      content: 'Nội dung bài viết 0 bình luận.',
      createdAt: '2026-11-06T10:00:00Z',
      likeCount: 0,
      isLiked: false,
      commentCount: 0
    };

    // Act
    setupAndFlushPosts([postWith0Comments]);
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert: Hiển thị "0 bình luận" trên giao diện bài đăng
    const commentCountElem = fixture.nativeElement.querySelector('[data-testid="comment-count-p-0-comment"]');
    expect(commentCountElem).toBeTruthy();
    expect(commentCountElem.textContent.trim()).toBe('0 bình luận');
  });

  it('AC2: should display "1 bình luận" on post UI when post has exactly 1 comment', async () => {
    // Arrange: Bài đăng có đúng 1 bình luận
    const postWith1Comment: Post = {
      id: 'p-1-comment',
      authorId: 'author-1',
      title: 'Bài viết có đúng một bình luận',
      content: 'Nội dung bài viết 1 bình luận.',
      createdAt: '2026-11-06T10:00:00Z',
      likeCount: 2,
      isLiked: false,
      commentCount: 1
    };

    // Act
    setupAndFlushPosts([postWith1Comment]);
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert: Hiển thị "1 bình luận" trên giao diện bài đăng
    const commentCountElem = fixture.nativeElement.querySelector('[data-testid="comment-count-p-1-comment"]');
    expect(commentCountElem).toBeTruthy();
    expect(commentCountElem.textContent.trim()).toBe('1 bình luận');
  });

  it('AC3: should display "N bình luận" (e.g. "5 bình luận") on post UI when post has N (>1) comments', async () => {
    // Arrange: Bài đăng có N=5 bình luận
    const postWith5Comments: Post = {
      id: 'p-5-comments',
      authorId: 'author-5',
      title: 'Bài viết có năm bình luận',
      content: 'Nội dung bài viết 5 bình luận.',
      createdAt: '2026-11-06T10:00:00Z',
      likeCount: 7,
      isLiked: false,
      commentCount: 5
    };

    // Act
    setupAndFlushPosts([postWith5Comments]);
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert: Hiển thị "5 bình luận" trên giao diện bài đăng
    const commentCountElem = fixture.nativeElement.querySelector('[data-testid="comment-count-p-5-comments"]');
    expect(commentCountElem).toBeTruthy();
    expect(commentCountElem.textContent.trim()).toBe('5 bình luận');
  });

  it('AC4: should update comment count to "M+1 bình luận" without reloading page or refetching post list when comment is successfully submitted', async () => {
    // Arrange: Người dùng đăng nhập và bài đăng ban đầu có M=3 bình luận
    authService.login({ id: 'user-1', name: 'Nguyễn Văn A' });
    const postId = 'p-m-comments';
    const postWithMComments: Post = {
      id: postId,
      authorId: 'author-m',
      title: 'Bài viết kiểm tra cập nhật bình luận',
      content: 'Nội dung bài viết ban đầu có 3 bình luận.',
      createdAt: '2026-11-06T10:00:00Z',
      likeCount: 3,
      isLiked: false,
      commentCount: 3
    };

    setupAndFlushPosts([postWithMComments]);
    await fixture.whenStable();
    fixture.detectChanges();

    // Flush initial GET comments request created by CommentSection for postWithMComments
    const getCommentsReq = httpTesting.expectOne(`${baseUrl}/posts/${postId}/comments`);
    expect(getCommentsReq.request.method).toBe('GET');
    getCommentsReq.flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    const commentCountElem = fixture.nativeElement.querySelector(`[data-testid="comment-count-${postId}"]`);
    expect(commentCountElem.textContent.trim()).toBe('3 bình luận');

    // Act: Nhập và gửi một bình luận mới thông qua CommentSection của bài đăng
    const commentSectionDebug = fixture.debugElement.query(By.directive(CommentSection));
    expect(commentSectionDebug).toBeTruthy();
    const commentSectionComp = commentSectionDebug.componentInstance as CommentSection;
    commentSectionComp.onContentChange('Bình luận mới thêm');
    fixture.detectChanges();
    await fixture.whenStable();

    commentSectionComp.submitComment();
    fixture.detectChanges();

    // Xử lý request POST comment lên backend
    const commentPostReq = httpTesting.expectOne(`${baseUrl}/posts/${postId}/comments`);
    expect(commentPostReq.request.method).toBe('POST');
    commentPostReq.flush({
      id: 'c-new-id',
      postId: postId,
      userId: 'user-1',
      authorName: 'Nguyễn Văn A',
      content: 'Bình luận mới thêm',
      createdAt: new Date().toISOString()
    });

    await fixture.whenStable();
    fixture.detectChanges();

    // Assert:
    // 1. Số bình luận hiển thị trên giao diện cập nhật thành "4 bình luận" (M+1 bình luận)
    expect(commentCountElem.textContent.trim()).toBe('4 bình luận');
    expect(app.posts()[0].commentCount).toBe(4);

    // 2. Không gọi lại API danh sách bài đăng (không có request nào tới /posts)
    httpTesting.expectNone(`${baseUrl}/posts`);
  });
});
