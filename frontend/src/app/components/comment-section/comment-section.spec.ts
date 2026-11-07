import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CommentSection } from './comment-section';
import { AuthService } from '../../services/auth.service';
import { Comment } from '../../models/comment.model';
import { environment } from '../../../environments/environment';

describe('CommentSection', () => {
  let fixture: ComponentFixture<CommentSection>;
  let component: CommentSection;
  let authService: AuthService;
  let httpTesting: HttpTestingController;

  const samplePostId = '11111111-1111-1111-1111-111111111111';
  const apiBase = environment.apiBaseUrl;

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
    // Khởi tạo trạng thái đã đăng nhập cho các test bình luận hợp lệ
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

    const req = httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`);
    expect(req.request.method).toBe('GET');
    req.flush([]);

    await fixture.whenStable();
    expect(component).toBeTruthy();
    expect(component.comments().length).toBe(0);
  });

  it('AC1: should display valid comment on post where user has permission', async () => {
    fixture.detectChanges();
    const initReq = httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`);
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

    // Nhập bình luận mới hợp lệ
    component.onContentChange('Bình luận mới thứ hai');
    component.submitComment();

    const postReq = httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`);
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

  it('AC2: should reject comment when user lacks permission to view post', async () => {
    fixture.componentRef.setInput('hasViewPermission', false);
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    component.onContentChange('Cố gắng bình luận khi không có quyền xem');
    component.submitComment();

    // Không gửi request POST khi không có quyền xem
    httpTesting.expectNone(`${apiBase}/posts/${samplePostId}/comments`);
    expect(component.errorMessage()).toBe('Bạn không có quyền xem bài đăng này.');
  });

  it('AC2: should handle 403 Forbidden error from backend when user lacks permission', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    component.onContentChange('Thử gửi bình luận');
    component.submitComment();

    const postReq = httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`);
    postReq.flush(
      { error: 'Bạn không có quyền xem bài đăng này.' },
      { status: 403, statusText: 'Forbidden' }
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.errorMessage()).toBe('Bạn không có quyền xem bài đăng này.');
  });

  it('AC2: should handle 403 Forbidden on loading comments', async () => {
    fixture.detectChanges();
    const req = httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`);
    req.flush(
      { error: 'Bạn không có quyền xem bài đăng này.' },
      { status: 403, statusText: 'Forbidden' }
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.errorMessage()).toBe('Bạn không có quyền xem bài đăng này.');
  });

  it('AC3: should display comments in chronological order (oldest first, newest last)', async () => {
    fixture.detectChanges();
    const req = httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`);

    const mockComments: Comment[] = [
      {
        id: 'c-old',
        postId: samplePostId,
        userId: 'u-1',
        authorName: 'User 1',
        content: 'Bình luận cũ nhất',
        createdAt: '2026-11-02T08:00:00Z'
      },
      {
        id: 'c-mid',
        postId: samplePostId,
        userId: 'u-2',
        authorName: 'User 2',
        content: 'Bình luận giữa',
        createdAt: '2026-11-02T09:00:00Z'
      },
      {
        id: 'c-new',
        postId: samplePostId,
        userId: 'u-3',
        authorName: 'User 3',
        content: 'Bình luận mới nhất',
        createdAt: '2026-11-02T10:00:00Z'
      }
    ];

    req.flush(mockComments);
    await fixture.whenStable();
    fixture.detectChanges();

    const commentElements = fixture.nativeElement.querySelectorAll('.comment-item');
    expect(commentElements.length).toBe(3);
    expect(commentElements[0].querySelector('.comment-content').textContent).toBe('Bình luận cũ nhất');
    expect(commentElements[1].querySelector('.comment-content').textContent).toBe('Bình luận giữa');
    expect(commentElements[2].querySelector('.comment-content').textContent).toBe('Bình luận mới nhất');
  });

  it('AC1: should display correct remaining char count and enable submit button when comment length is 1 to 500', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    component.onContentChange('Bình luận 15 ký tự!');
    fixture.detectChanges();
    await fixture.whenStable();

    // 500 - 18 = 482
    const expectedRemaining = 500 - [...'Bình luận 15 ký tự!'].length;
    expect(component.remainingChars()).toBe(expectedRemaining);
    const counterElem = fixture.nativeElement.querySelector('[data-testid="comment-char-counter"]');
    expect(counterElem.textContent.trim()).toBe(String(expectedRemaining));

    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('button.submit-button');
    expect(submitBtn.disabled).toBe(false);
  });

  it('AC2: should display negative remaining count (-1) and disable submit button when comment is 501 characters', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    const content501 = 'c'.repeat(501);
    component.onContentChange(content501);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.remainingChars()).toBe(-1);
    const counterElem = fixture.nativeElement.querySelector('[data-testid="comment-char-counter"]');
    expect(counterElem.textContent.trim()).toBe('-1');
    expect(counterElem.classList.contains('over-limit')).toBe(true);

    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('button.submit-button');
    expect(submitBtn.disabled).toBe(true);
  });

  it('AC3: should display 500 remaining characters and disable submit button when comment is empty string', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    component.onContentChange('');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.remainingChars()).toBe(500);
    const counterElem = fixture.nativeElement.querySelector('[data-testid="comment-char-counter"]');
    expect(counterElem.textContent.trim()).toBe('500');

    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('button.submit-button');
    expect(submitBtn.disabled).toBe(true);
  });

  it('AC4: should display correct remaining characters and disable submit button when comment is whitespace only', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    component.onContentChange('   ');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.remainingChars()).toBe(497);
    const counterElem = fixture.nativeElement.querySelector('[data-testid="comment-char-counter"]');
    expect(counterElem.textContent.trim()).toBe('497');

    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('button.submit-button');
    expect(submitBtn.disabled).toBe(true);
  });

  it('AC5: should reject empty comment or whitespace-only comment and display error message', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    // Kiểm tra nội dung rỗng
    component.onContentChange('');
    component.submitComment();
    httpTesting.expectNone(`${apiBase}/posts/${samplePostId}/comments`);
    expect(component.errorMessage()).toBe('Nội dung bình luận không được để trống hoặc chỉ chứa khoảng trắng.');

    // Kiểm tra chỉ có khoảng trắng
    component.onContentChange('   \t \n   ');
    component.submitComment();
    httpTesting.expectNone(`${apiBase}/posts/${samplePostId}/comments`);
    expect(component.errorMessage()).toBe('Nội dung bình luận không được để trống hoặc chỉ chứa khoảng trắng.');
  });

  it('AC6: should reject comment with 501 characters and display error message', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    const content501 = 'a'.repeat(501);
    component.onContentChange(content501);
    component.submitComment();

    // Không gửi HTTP request nào khi validation lỗi
    httpTesting.expectNone(`${apiBase}/posts/${samplePostId}/comments`);
    expect(component.errorMessage()).toBe('Nội dung bình luận không được vượt quá 500 ký tự.');
  });

  it('AC7: should successfully post comment of exactly 500 characters', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    const content500 = 'a'.repeat(500);
    component.onContentChange(content500);
    component.submitComment();

    const postReq = httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`);
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

  it('AC7: should count characters correctly by Unicode code points (emojis)', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    // 500 emojis (mỗi emoji gồm 2 code units trong JS string .length, nhưng là 1 code point)
    const emojis500 = '🎉'.repeat(500);
    expect(emojis500.length).toBe(1000); // 1000 UTF-16 code units

    component.onContentChange(emojis500);
    expect(component.charCount()).toBe(500); // 500 code points

    component.submitComment();
    const postReq = httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`);
    postReq.flush({
      id: 'c-emoji',
      postId: samplePostId,
      userId: 'user-1',
      authorName: 'Nguyễn Văn A',
      content: emojis500,
      createdAt: '2026-11-02T10:15:00Z'
    });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.errorMessage()).toBeNull();
    expect(component.comments().length).toBe(1);
  });

  it('should require login and display error when user is not logged in', async () => {
    authService.logout();
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    component.onContentChange('Bình luận khi chưa đăng nhập');
    component.submitComment();

    httpTesting.expectNone(`${apiBase}/posts/${samplePostId}/comments`);
    expect(component.errorMessage()).toBe('Vui lòng đăng nhập để bình luận.');
  });

  it('should display error message when trying to comment on non-existent post', async () => {
    const nonExistentId = '00000000-0000-0000-0000-000000000000';
    fixture.componentRef.setInput('postId', nonExistentId);
    fixture.detectChanges();

    const getReq = httpTesting.expectOne(`${apiBase}/posts/${nonExistentId}/comments`);
    getReq.flush(
      { error: 'Bài đăng không tồn tại.' },
      { status: 404, statusText: 'Not Found' }
    );
    await fixture.whenStable();
    fixture.detectChanges();
    expect(component.errorMessage()).toBe('Bài đăng không tồn tại.');

    component.onContentChange('Bình luận vào bài không tồn tại');
    component.submitComment();

    const postReq = httpTesting.expectOne(`${apiBase}/posts/${nonExistentId}/comments`);
    postReq.flush(
      { error: 'Bài đăng không tồn tại.' },
      { status: 404, statusText: 'Not Found' }
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.errorMessage()).toBe('Bài đăng không tồn tại.');
  });

  it('should reload comments when postId input changes', async () => {
    fixture.detectChanges();
    httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`).flush([]);
    await fixture.whenStable();

    const newPostId = '22222222-2222-2222-2222-222222222222';
    fixture.componentRef.setInput('postId', newPostId);
    fixture.detectChanges();

    const newReq = httpTesting.expectOne(`${apiBase}/posts/${newPostId}/comments`);
    newReq.flush([
      {
        id: 'c-new',
        postId: newPostId,
        userId: 'u-1',
        authorName: 'User',
        content: 'Bình luận bài mới',
        createdAt: '2026-11-02T12:00:00Z'
      }
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.comments().length).toBe(1);
    expect(component.comments()[0].content).toBe('Bình luận bài mới');
  });

  it('PO Decision: should display comment time formatted as local date/time', async () => {
    fixture.detectChanges();
    const req = httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`);
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
    const req = httpTesting.expectOne(`${apiBase}/posts/${samplePostId}/comments`);
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
    expect(contentElem?.innerHTML).not.toContain('<script>');
    expect(contentElem?.querySelector('script')).toBeNull();
    expect(contentElem?.querySelector('b')).toBeNull();
    expect(contentElem?.textContent).toBe('<script>alert("xss")</script><b>Bold Text</b>');
  });
});
