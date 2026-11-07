import { Pipe, PipeTransform } from '@angular/core';

/**
 * Định dạng thời gian theo chuẩn DD/MM/YYYY HH:mm theo giờ địa phương.
 */
export function formatFullDate(date: Date): string {
  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${day}/${month}/${year} ${hours}:${minutes}`;
}

/**
 * Tính toán chuỗi thời gian tương đối dựa trên thời gian tham chiếu (mặc định là hiện tại).
 *
 * Quy tắc:
 * - < 60 giây (bao gồm thời gian tương lai do lệch clock): "vừa xong" (AC1, AC2)
 * - 60 giây đến < 3600 giây (dưới 1 giờ): "X phút trước" (làm tròn xuống theo AC3, AC4, AC5)
 * - 3600 giây đến < 86400 giây (dưới 24 giờ): "Y giờ trước" (AC6, AC7)
 * - 86400 giây đến <= 7 ngày (7 * 86400 giây): "Z ngày trước" (AC8, AC9)
 * - > 7 ngày: hiển thị theo định dạng ngày đầy đủ "DD/MM/YYYY HH:mm" (AC10, AC11)
 */
export function formatRelativeTime(
  value: Date | string | number | null | undefined,
  referenceTime?: Date | string | number
): string {
  if (value === null || value === undefined || value === '') {
    return '';
  }

  const date = value instanceof Date ? value : new Date(value);
  if (isNaN(date.getTime())) {
    return '';
  }

  const ref = referenceTime !== undefined && referenceTime !== null
    ? (referenceTime instanceof Date ? referenceTime : new Date(referenceTime))
    : new Date();

  if (isNaN(ref.getTime())) {
    return '';
  }

  const diffInMs = ref.getTime() - date.getTime();

  // Nếu thời gian ở tương lai do lệch đồng hồ server/client
  if (diffInMs < 0) {
    return 'vừa xong';
  }

  const diffInSeconds = Math.floor(diffInMs / 1000);

  // Dưới 60 giây (< 1 phút): "vừa xong" (AC1, AC2)
  if (diffInSeconds < 60) {
    return 'vừa xong';
  }

  // Từ 1 phút (60s) đến dưới 60 phút (3600s): "X phút trước" (AC3, AC4, AC5)
  if (diffInSeconds < 3600) {
    const minutes = Math.floor(diffInSeconds / 60);
    return `${minutes} phút trước`;
  }

  // Từ 1 giờ đến dưới 24 giờ: "Y giờ trước" (AC6, AC7)
  if (diffInSeconds < 86400) {
    const hours = Math.floor(diffInSeconds / 3600);
    return `${hours} giờ trước`;
  }

  // Từ 1 ngày đến 7 ngày (chính xác <= 7 ngày): "Z ngày trước" (AC8, AC9)
  const SEVEN_DAYS_IN_SECONDS = 7 * 86400; // 604800 giây
  if (diffInSeconds <= SEVEN_DAYS_IN_SECONDS) {
    const days = Math.floor(diffInSeconds / 86400);
    return `${days} ngày trước`;
  }

  // Vượt quá 7 ngày: hiển thị định dạng ngày đầy đủ "DD/MM/YYYY HH:mm" (AC10, AC11)
  return formatFullDate(date);
}

@Pipe({
  name: 'relativeTime',
  standalone: true
})
export class RelativeTimePipe implements PipeTransform {
  transform(
    value: Date | string | number | null | undefined,
    referenceTime?: Date | string | number
  ): string {
    return formatRelativeTime(value, referenceTime);
  }
}
