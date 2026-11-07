import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RelativeTimePipe, formatRelativeTime, formatFullDate } from './relative-time.pipe';

@Component({
  standalone: true,
  imports: [RelativeTimePipe],
  template: `<span data-testid="time-display">{{ testDate | relativeTime: testRef }}</span>`
})
class TestHostComponent {
  testDate: Date | string | null = null;
  testRef: Date | undefined = undefined;
}

describe('RelativeTimePipe & formatRelativeTime', () => {
  const pipe = new RelativeTimePipe();
  // Giả định thời điểm chuẩn T: 2026-11-07T12:00:00Z
  const T = new Date('2026-11-07T12:00:00.000Z');

  describe('Acceptance Criteria', () => {
    // AC 1
    it('AC1: should display "vừa xong" when created at T - 30 seconds', () => {
      const created = new Date(T.getTime() - 30 * 1000);
      expect(pipe.transform(created, T)).toBe('vừa xong');
    });

    // AC 2
    it('AC2: should display "vừa xong" when created at T - 59 seconds', () => {
      const created = new Date(T.getTime() - 59 * 1000);
      expect(pipe.transform(created, T)).toBe('vừa xong');
    });

    // AC 3
    it('AC3: should display "1 phút trước" when created at T - 1 minute and 1 second', () => {
      const created = new Date(T.getTime() - (60 + 1) * 1000);
      expect(pipe.transform(created, T)).toBe('1 phút trước');
    });

    // Boundary check: T - 60 seconds exactly
    it('should display "1 phút trước" when created exactly at T - 60 seconds', () => {
      const created = new Date(T.getTime() - 60 * 1000);
      expect(pipe.transform(created, T)).toBe('1 phút trước');
    });

    // PO Decision: Floor rounding check (4 minutes 30 seconds -> 4 phút trước)
    it('PO Decision: should round down (floor) 4 minutes 30 seconds to "4 phút trước"', () => {
      const created = new Date(T.getTime() - (4 * 60 + 30) * 1000);
      expect(pipe.transform(created, T)).toBe('4 phút trước');
    });

    // AC 4
    it('AC4: should display "5 phút trước" when created at T - 5 minutes', () => {
      const created = new Date(T.getTime() - 5 * 60 * 1000);
      expect(pipe.transform(created, T)).toBe('5 phút trước');
    });

    // AC 5
    it('AC5: should display "59 phút trước" when created at T - 59 minutes', () => {
      const created = new Date(T.getTime() - 59 * 60 * 1000);
      expect(pipe.transform(created, T)).toBe('59 phút trước');
    });

    // Boundary check: T - 59 minutes and 59 seconds
    it('should display "59 phút trước" when created at T - 59 minutes 59 seconds', () => {
      const created = new Date(T.getTime() - (59 * 60 + 59) * 1000);
      expect(pipe.transform(created, T)).toBe('59 phút trước');
    });

    // Boundary check: T - 1 hour exactly
    it('should display "1 giờ trước" when created exactly at T - 1 hour', () => {
      const created = new Date(T.getTime() - 3600 * 1000);
      expect(pipe.transform(created, T)).toBe('1 giờ trước');
    });

    // AC 6
    it('AC6: should display "1 giờ trước" when created at T - 1 hour and 1 second', () => {
      const created = new Date(T.getTime() - (3600 + 1) * 1000);
      expect(pipe.transform(created, T)).toBe('1 giờ trước');
    });

    // AC 7
    it('AC7: should display "23 giờ trước" when created at T - 23 hours and 59 minutes', () => {
      const created = new Date(T.getTime() - (23 * 3600 + 59 * 60) * 1000);
      expect(pipe.transform(created, T)).toBe('23 giờ trước');
    });

    // Boundary check: T - 24 hours exactly
    it('should display "1 ngày trước" when created exactly at T - 24 hours (1 day)', () => {
      const created = new Date(T.getTime() - 86400 * 1000);
      expect(pipe.transform(created, T)).toBe('1 ngày trước');
    });

    // AC 8
    it('AC8: should display "1 ngày trước" when created at T - 1 day and 1 second', () => {
      const created = new Date(T.getTime() - (86400 + 1) * 1000);
      expect(pipe.transform(created, T)).toBe('1 ngày trước');
    });

    // AC 9
    it('AC9: should display "7 ngày trước" when created at T - 7 days', () => {
      const created = new Date(T.getTime() - 7 * 86400 * 1000);
      expect(pipe.transform(created, T)).toBe('7 ngày trước');
    });

    // AC 10
    it('AC10: should display full date format "DD/MM/YYYY HH:mm" when created at T - 7 days and 1 minute', () => {
      const created = new Date(T.getTime() - (7 * 86400 + 60) * 1000);
      const expected = formatFullDate(created);
      expect(pipe.transform(created, T)).toBe(expected);
      expect(expected).toMatch(/^\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}$/);
    });

    // AC 11
    it('AC11: should display full date format "DD/MM/YYYY HH:mm" when created at T - 8 days', () => {
      const created = new Date(T.getTime() - 8 * 86400 * 1000);
      const expected = formatFullDate(created);
      expect(pipe.transform(created, T)).toBe(expected);
      expect(expected).toMatch(/^\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}$/);
    });
  });

  describe('Edge cases and inputs', () => {
    it('should handle ISO string date representations', () => {
      const createdIso = new Date(T.getTime() - 120 * 1000).toISOString();
      expect(pipe.transform(createdIso, T)).toBe('2 phút trước');
    });

    it('should handle epoch timestamp numbers', () => {
      const createdTs = T.getTime() - 10 * 60 * 1000;
      expect(pipe.transform(createdTs, T)).toBe('10 phút trước');
    });

    it('should return "vừa xong" for future timestamps (e.g., slight clock skew)', () => {
      const futureDate = new Date(T.getTime() + 10 * 1000);
      expect(pipe.transform(futureDate, T)).toBe('vừa xong');
    });

    it('should return empty string for null, undefined, or empty string', () => {
      expect(pipe.transform(null)).toBe('');
      expect(pipe.transform(undefined)).toBe('');
      expect(pipe.transform('')).toBe('');
    });

    it('should return empty string for invalid date values', () => {
      expect(pipe.transform('not-a-valid-date')).toBe('');
    });

    it('should default to new Date() when referenceTime is omitted', () => {
      // 10 giây trước thời điểm hiện tại
      const recent = new Date(Date.now() - 10 * 1000);
      expect(pipe.transform(recent)).toBe('vừa xong');
    });
  });

  describe('Template Integration', () => {
    it('should render relative time properly in an Angular component template', async () => {
      await TestBed.configureTestingModule({
        imports: [TestHostComponent]
      }).compileComponents();

      const fixture = TestBed.createComponent(TestHostComponent);
      fixture.componentInstance.testRef = T;
      fixture.componentInstance.testDate = new Date(T.getTime() - 15 * 60 * 1000); // 15m ago
      fixture.detectChanges();

      const span = fixture.nativeElement.querySelector('[data-testid="time-display"]');
      expect(span?.textContent?.trim()).toBe('15 phút trước');
    });
  });
});
