import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.logo')?.textContent).toContain('Social Media');
  });

  it('SM-7: should have a fixed creation timestamp for the sample post by default', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app.postCreatedAt()).toBe('2026-11-06T08:00:00.000Z');
  });

  it('SM-7: should render relative time for the post', async () => {
    const fixture = TestBed.createComponent(App);
    // Thiết lập thời gian bài đăng cách đây 30 giây
    fixture.componentInstance.postCreatedAt.set(new Date(Date.now() - 30 * 1000).toISOString());
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const postTime = compiled.querySelector('.post-time');
    expect(postTime).toBeTruthy();
    expect(postTime?.textContent?.trim()).toBe('vừa xong');

    // Cập nhật thời gian bài đăng thành 1 giờ trước
    fixture.componentInstance.postCreatedAt.set(new Date(Date.now() - (3600 + 1) * 1000).toISOString());
    fixture.detectChanges();
    await fixture.whenStable();
    expect(postTime?.textContent?.trim()).toBe('1 giờ trước');
  });
});
