import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NotificationCenter } from './notification-center';
import { AuthService } from '../../services/auth.service';
import { Notification } from '../../models/comment.model';
import { environment } from '../../../environments/environment';

describe('NotificationCenter', () => {
  let fixture: ComponentFixture<NotificationCenter>;
  let component: NotificationCenter;
  let authService: AuthService;
  let httpTesting: HttpTestingController;

  const apiBase = environment.apiBaseUrl;

  const mockNotifications: Notification[] = [
    {
      id: 'notif-1',
      recipientUserId: 'author-system',
      actorUserId: 'user-1',
      actorName: 'Nguyễn Văn A',
      postId: '11111111-1111-1111-1111-111111111111',
      commentId: 'comment-1',
      message: 'Nguyễn Văn A đã bình luận về bài viết của bạn.',
      createdAt: '2026-11-02T10:00:00Z',
      isRead: false
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotificationCenter],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService
      ]
    }).compileComponents();

    httpTesting = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
    authService.login({ id: 'author-system', name: 'Hệ thống' });

    fixture = TestBed.createComponent(NotificationCenter);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should create and load notifications for authenticated post owner', async () => {
    fixture.detectChanges();

    const req = httpTesting.expectOne(`${apiBase}/notifications`);
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('X-User-Id')).toBe('author-system');
    req.flush(mockNotifications);

    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.notifications().length).toBe(1);
    expect(component.count()).toBe(1);

    const badge = fixture.nativeElement.querySelector('[data-testid="notif-badge"]');
    expect(badge).toBeTruthy();
    expect(badge.textContent.trim()).toBe('1');
  });

  it('should toggle notification dropdown and display notification message', async () => {
    fixture.detectChanges();
    const req = httpTesting.expectOne(`${apiBase}/notifications`);
    req.flush(mockNotifications);

    await fixture.whenStable();
    fixture.detectChanges();

    // Toggle dropdown open
    component.toggleOpen();
    fixture.detectChanges();

    expect(component.isOpen()).toBe(true);
    const dropdown = fixture.nativeElement.querySelector('.notification-dropdown');
    expect(dropdown).toBeTruthy();

    const notifItem = fixture.nativeElement.querySelector('[data-testid="notif-notif-1"]');
    expect(notifItem).toBeTruthy();
    expect(notifItem.textContent).toContain('Nguyễn Văn A đã bình luận về bài viết của bạn.');
  });

  it('should display empty message when user has no notifications', async () => {
    fixture.detectChanges();
    const req = httpTesting.expectOne(`${apiBase}/notifications`);
    req.flush([]);

    await fixture.whenStable();
    fixture.detectChanges();

    component.toggleOpen();
    fixture.detectChanges();

    const emptyMsg = fixture.nativeElement.querySelector('[data-testid="empty-notif"]');
    expect(emptyMsg).toBeTruthy();
    expect(emptyMsg.textContent).toContain('Bạn không có thông báo nào.');
  });

  it('should clear notifications and close dropdown on logout', async () => {
    fixture.detectChanges();
    const req = httpTesting.expectOne(`${apiBase}/notifications`);
    req.flush(mockNotifications);

    await fixture.whenStable();
    component.isOpen.set(true);
    fixture.detectChanges();

    authService.logout();
    fixture.detectChanges();

    expect(component.notifications().length).toBe(0);
    expect(component.isOpen()).toBe(false);
  });
});
