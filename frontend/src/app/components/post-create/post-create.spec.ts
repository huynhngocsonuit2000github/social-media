import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PostCreate } from './post-create';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

describe('PostCreate', () => {
  let fixture: ComponentFixture<PostCreate>;
  let component: PostCreate;
  let authService: AuthService;
  let httpTesting: HttpTestingController;

  const apiBase = environment.apiBaseUrl;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostCreate],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService
      ]
    }).compileComponents();

    httpTesting = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    authService.login({ id: 'user-1', name: 'Nguyễn Văn A' });

    fixture = TestBed.createComponent(PostCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('AC1: should display correct remaining char count and enable submit button when content length is between 1 and 500', async () => {
    // Arrange: Nội dung 10 ký tự
    const content = '1234567890';
    component.onContentChange(content);
    fixture.detectChanges();
    await fixture.whenStable();

    // Assert: Số ký tự còn lại là 500 - 10 = 490
    expect(component.remainingChars()).toBe(490);
    const counterElem = fixture.nativeElement.querySelector('[data-testid="post-char-counter"]');
    expect(counterElem.textContent.trim()).toBe('490');

    // Nút 'Đăng' được kích hoạt (enabled)
    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('button.submit-button');
    expect(submitBtn.disabled).toBe(false);
  });

  it('AC1: should submit post successfully when content length is within 500 characters', async () => {
    component.onTitleChange('Tiêu đề');
    component.onContentChange('Nội dung bài đăng hợp lệ.');
    component.submitPost();

    const req = httpTesting.expectOne(`${apiBase}/posts`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ content: 'Nội dung bài đăng hợp lệ.', title: 'Tiêu đề' });

    req.flush({
      id: 'p-1',
      authorId: 'user-1',
      title: 'Tiêu đề',
      content: 'Nội dung bài đăng hợp lệ.',
      createdAt: new Date().toISOString()
    });

    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.content()).toBe('');
    expect(component.title()).toBe('');
    expect(component.successMessage()).toBe('Đăng bài thành công!');
    expect(component.errorMessage()).toBeNull();
  });

  it('AC2: should display negative remaining count (-1) and disable submit button when content is 501 characters', async () => {
    // Arrange: 501 ký tự
    const content501 = 'a'.repeat(501);
    component.onContentChange(content501);
    fixture.detectChanges();
    await fixture.whenStable();

    // Assert: Số ký tự còn lại là -1
    expect(component.remainingChars()).toBe(-1);
    const counterElem = fixture.nativeElement.querySelector('[data-testid="post-char-counter"]');
    expect(counterElem.textContent.trim()).toBe('-1');
    expect(counterElem.classList.contains('over-limit')).toBe(true);

    // Nút 'Đăng' bị vô hiệu hóa (disabled)
    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('button.submit-button');
    expect(submitBtn.disabled).toBe(true);
  });

  it('AC3: should display 500 remaining characters and disable submit button when content is empty string', async () => {
    // Arrange: Chuỗi rỗng
    component.onContentChange('');
    fixture.detectChanges();
    await fixture.whenStable();

    // Assert: Số ký tự còn lại là 500
    expect(component.remainingChars()).toBe(500);
    const counterElem = fixture.nativeElement.querySelector('[data-testid="post-char-counter"]');
    expect(counterElem.textContent.trim()).toBe('500');

    // Nút 'Đăng' bị vô hiệu hóa (disabled)
    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('button.submit-button');
    expect(submitBtn.disabled).toBe(true);
  });

  it('AC4: should display correct remaining characters and disable submit button when content is whitespace only', async () => {
    // Arrange: 5 khoảng trắng
    component.onContentChange('     ');
    fixture.detectChanges();
    await fixture.whenStable();

    // Assert: Số ký tự còn lại là 500 - 5 = 495
    expect(component.remainingChars()).toBe(495);
    const counterElem = fixture.nativeElement.querySelector('[data-testid="post-char-counter"]');
    expect(counterElem.textContent.trim()).toBe('495');

    // Nút 'Đăng' bị vô hiệu hóa (disabled)
    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('button.submit-button');
    expect(submitBtn.disabled).toBe(true);
  });

  it('should count Unicode code points (emojis) correctly for remaining characters', async () => {
    // 500 emojis (mỗi emoji là 2 UTF-16 code units nhưng là 1 code point)
    const emojis500 = '🔥'.repeat(500);
    component.onContentChange(emojis500);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.remainingChars()).toBe(0);
    const counterElem = fixture.nativeElement.querySelector('[data-testid="post-char-counter"]');
    expect(counterElem.textContent.trim()).toBe('0');

    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('button.submit-button');
    expect(submitBtn.disabled).toBe(false);
  });
});
