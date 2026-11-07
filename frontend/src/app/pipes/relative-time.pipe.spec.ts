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

    // AC 3: Ban đầu AC3 ghi "1 phút trước" ở mốc T - 1 phút 1 giây, nhưng theo phản hồi làm rõ của PO
    // (ngưỡng "vừa xong" là dưới 5 phút), nên mốc T - 1m1s (< 5 phút) hiển thị là "vừa xong".
    it('AC3 (cập nhật theo PO): should display "vừa xong" when created at T - 1 minute and 1 second (< 5 minutes)', () => {
      const created = new Date(T.getTime() - (60 + 1) * 1000);
      expect(pipe.transform(created, T)).toBe('vừa xong');
    });

    // Boundary check for PO threshold: T - 4 minutes 59 seconds (< 300s -> "vừa xong")
    it('PO Decision: should display "vừa xong" at boundary T - 4 minutes 59 seconds (< 300s)', () => {
      const created = new Date(T.getTime() - (4 * 60 + 59) * 1000);
      expect(pipe.transform(created, T)).toBe('vừa xong');
    });

    // Boundary check for PO threshold: T - 5 minutes exactly (300s -> "5 phút trước")
    it('PO Decision: should display "5 phút trước" at exactly T - 5 minutes (300s)', () => {
      const created = new Date(T.getTime() - 5 * 60 * 1000);
      expect(pipe.transform(created, T)).toBe('5 phút trước');
    });

    // PO Decision: Floor rounding check (5 minutes 30 seconds -> 5 phút trước)
    it('PO Decision: should round down (floor) 5 minutes 30 seconds to "5 phút trước"', () => {
      const created = new Date(T.getTime() - (5 * 60 + 30) * 1000);
      expect(pipe.transform(created, T)).toBe('5 phút trước');
    });

    it('PO Decision: should round down (floor) 5 minutes 59 seconds to "5 phút trước"', () => {
      const created = new Date(T.getTime() - (5 * 60 + 59) * 1000);
      expect(pipe.transform(created, T)).toBe('5 phút trước');
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

    // AC 10: Vượt quá 7 ngày hiển thị theo định dạng ngày đầy đủ "DD/MM/YYYY HH:mm"
    it('AC10: should display full date format "DD/MM/YYYY HH:mm" with explicit value when created at T - 7 days and 1 minute', () => {
      // Dựng Date cụ thể theo giờ local: ngày 01 tháng 11 năm 2026 lúc 08:30
      const specificCreated = new Date(2026, 10, 1, 8, 30, 0);
      const refTime = new Date(specificCreated.getTime() + (7 * 86400 + 60) * 1000);
      const result = pipe.transform(specificCreated, refTime);
      expect(result).toBe('01/11/2026 08:30');
      expect(result).toMatch(/^\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}$/);
    });

    // AC 11: Vượt quá 7 ngày (8 ngày) hiển thị theo định dạng ngày đầy đủ
    it('AC11: should display full date format "DD/MM/YYYY HH:mm" with explicit value when created at T - 8 days', () => {
      // Dựng Date cụ thể theo giờ local: ngày 15 tháng 10 năm 2026 lúc 14:05
      const specificCreated = new Date(2026, 9, 15, 14, 5, 0);
      const refTime = new Date(specificCreated.getTime() + 8 * 86400 * 1000);
      const result = pipe.transform(specificCreated, refTime);
      expect(result).toBe('15/10/2026 14:05');
      expect(result).toMatch(/^\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}$/);
    });
  });

  describe('formatFullDate helper', () => {
    it('should format Date instance to "DD/MM/YYYY HH:mm" in local time with zero padding', () => {
      const date = new Date(2026, 0, 5, 4, 9, 0); // 05/01/2026 04:09
      expect(formatFullDate(date)).toBe('05/01/2026 04:09');
    });
  });

  describe('Edge cases and inputs', () => {
    it('should handle ISO string date representations', () => {
      const createdIso = new Date(T.getTime() - 10 * 60 * 1000).toISOString();
      expect(pipe.transform(createdIso, T)).toBe('10 phút trước');
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
