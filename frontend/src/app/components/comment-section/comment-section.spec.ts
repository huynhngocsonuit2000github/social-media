import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CommentSection } from './comment-section';
import { AuthService } from '../../services/auth.service';
import { Comment } from '../../models/comment.model';

describe('CommentSection', () => {
  let fixture: ComponentFixture<CommentSection>;
  let component: CommentSection;
  let authService: AuthService;
  let httpTesting: HttpTestingController;

  const samplePostId = '11111111-1111-1111-1111-111111111111';

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentSection],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService
      ]
    }).compileComponents();

    httpTesting = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    // Ensure default test user is logged in
    authService.login({ id: 'user-1', name: 'Nguyễn Văn A' });

    fixture = TestBed.createComponent(CommentSection);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('postId', samplePostId);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should create component and load initial comments', async () => {
    fixture.detectChanges();

    const req = httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`);
    expect(req.request.method).toBe('GET');
    req.flush([]);

    await fixture.whenStable();
    expect(component).toBeTruthy();
    expect(component.comments().length).toBe(0);
  });

  it('AC1: should display valid comment at the end of the comment list', async () => {
    fixture.detectChanges();
    const initReq = httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`);
    initReq.flush([
      {
        id: 'c-1',
        postId: samplePostId,
        userId: 'u-1',
        authorName: 'User 1',
        content: 'Bình luận ban đầu',
        createdAt: '2026-11-02T10:00:00Z'
      }
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    // Type a new comment
    component.onContentChange('Bình luận mới thứ hai');
    component.submitComment();

    const postReq = httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`);
    expect(postReq.request.method).toBe('POST');
    expect(postReq.request.body).toEqual({ content: 'Bình luận mới thứ hai' });

    const newComment: Comment = {
      id: 'c-2',
      postId: samplePostId,
      userId: 'user-1',
      authorName: 'Nguyễn Văn A',
      content: 'Bình luận mới thứ hai',
      createdAt: '2026-11-02T10:05:00Z'
    };
    postReq.flush(newComment);
    await fixture.whenStable();
    fixture.detectChanges();

    const list = component.comments();
    expect(list.length).toBe(2);
    expect(list[list.length - 1].content).toBe('Bình luận mới thứ hai');
    expect(component.content()).toBe('');
    expect(component.errorMessage()).toBeNull();
  });

  it('AC2: should successfully post comment of exactly 500 characters', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    const content500 = 'a'.repeat(500);
    component.onContentChange(content500);
    component.submitComment();

    const postReq = httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`);
    expect(postReq.request.body.content.length).toBe(500);

    postReq.flush({
      id: 'c-500',
      postId: samplePostId,
      userId: 'user-1',
      authorName: 'Nguyễn Văn A',
      content: content500,
      createdAt: '2026-11-02T10:10:00Z'
    });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.comments().length).toBe(1);
    expect(component.errorMessage()).toBeNull();
  });

  it('AC3: should reject comment with 501 characters and display error message', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    const content501 = 'a'.repeat(501);
    component.onContentChange(content501);
    component.submitComment();

    // No HTTP request should be made
    httpTesting.expectNone(`http://localhost:5154/api/posts/${samplePostId}/comments`);

    expect(component.errorMessage()).toBe('Nội dung bình luận không được vượt quá 500 ký tự.');
  });

  it('AC4: should reject empty comment or whitespace-only comment and display error message', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    // Test with empty string
    component.onContentChange('');
    component.submitComment();
    httpTesting.expectNone(`http://localhost:5154/api/posts/${samplePostId}/comments`);
    expect(component.errorMessage()).toBe('Nội dung bình luận không được để trống hoặc chỉ chứa khoảng trắng.');

    // Test with whitespace only
    component.onContentChange('   \t \n  ');
    component.submitComment();
    httpTesting.expectNone(`http://localhost:5154/api/posts/${samplePostId}/comments`);
    expect(component.errorMessage()).toBe('Nội dung bình luận không được để trống hoặc chỉ chứa khoảng trắng.');
  });

  it('AC5: should display comments in chronological order (oldest first, newest at the end)', async () => {
    fixture.detectChanges();
    const req = httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`);
    req.flush([
      {
        id: 'c-1',
        postId: samplePostId,
        userId: 'u-1',
        authorName: 'User 1',
        content: 'Bình luận 1',
        createdAt: '2026-11-02T08:00:00Z'
      },
      {
        id: 'c-2',
        postId: samplePostId,
        userId: 'u-2',
        authorName: 'User 2',
        content: 'Bình luận 2',
        createdAt: '2026-11-02T09:00:00Z'
      },
      {
        id: 'c-3',
        postId: samplePostId,
        userId: 'u-3',
        authorName: 'User 3',
        content: 'Bình luận 3 (mới nhất)',
        createdAt: '2026-11-02T10:00:00Z'
      }
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    const commentItems = element.querySelectorAll('.comment-item');
    expect(commentItems.length).toBe(3);

    const firstContent = commentItems[0].querySelector('.comment-content')?.textContent?.trim();
    const lastContent = commentItems[2].querySelector('.comment-content')?.textContent?.trim();

    expect(firstContent).toBe('Bình luận 1');
    expect(lastContent).toBe('Bình luận 3 (mới nhất)');
  });

  it('AC7: should require login and display error when user is not logged in', async () => {
    authService.logout();
    fixture.detectChanges();
    httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    component.onContentChange('Bình luận khi chưa đăng nhập');
    component.submitComment();

    httpTesting.expectNone(`http://localhost:5154/api/posts/${samplePostId}/comments`);
    expect(component.errorMessage()).toBe('Vui lòng đăng nhập để bình luận.');
  });

  it('AC8: should display error message when trying to comment on non-existent post', async () => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    fixture.componentRef.setInput('postId', nonExistentId);
    fixture.detectChanges();

    httpTesting.expectOne(`http://localhost:5154/api/posts/${nonExistentId}/comments`).flush([]);
    await fixture.whenStable();

    component.onContentChange('Bình luận vào bài không tồn tại');
    component.submitComment();

    const postReq = httpTesting.expectOne(`http://localhost:5154/api/posts/${nonExistentId}/comments`);
    postReq.flush(
      { error: 'Bài đăng không tồn tại.' },
      { status: 404, statusText: 'Not Found' }
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.errorMessage()).toBe('Bài đăng không tồn tại.');
  });

  it('PO Decision: should display comment time formatted as local date/time', async () => {
    fixture.detectChanges();
    const req = httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`);
    req.flush([
      {
        id: 'c-date',
        postId: samplePostId,
        userId: 'u-1',
        authorName: 'User Date',
        content: 'Nội dung kiểm tra ngày giờ',
        createdAt: '2026-11-02T14:30:00Z'
      }
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    const timeElem = element.querySelector('.comment-time');
    expect(timeElem).toBeTruthy();
    expect(timeElem?.textContent?.trim()).toMatch(/\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}/);
  });

  it('PO Decision: should render text safely without executing HTML/script (XSS protection)', async () => {
    fixture.detectChanges();
    const req = httpTesting.expectOne(`http://localhost:5154/api/posts/${samplePostId}/comments`);
    req.flush([
      {
        id: 'c-xss',
        postId: samplePostId,
        userId: 'u-xss',
        authorName: 'Attacker',
        content: '<script>alert("xss")</script><b>Bold Text</b>',
        createdAt: '2026-11-02T10:00:00Z'
      }
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    const contentElem = element.querySelector('.comment-content');
    // In Angular interpolation {{ }}, raw HTML is rendered as plain text content, not DOM elements
    expect(contentElem?.innerHTML).not.toContain('<script>');
    expect(contentElem?.querySelector('script')).toBeNull();
    expect(contentElem?.querySelector('b')).toBeNull();
    expect(contentElem?.textContent).toBe('<script>alert("xss")</script><b>Bold Text</b>');
  });
});
