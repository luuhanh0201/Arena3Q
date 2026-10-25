import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
export function formatDate(value: string | null | undefined) {
  if (!value) return "Chưa có";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? "Chưa có"
    : new Intl.DateTimeFormat("vi-VN", {
        dateStyle: "short",
        timeStyle: "short",
      }).format(date);
}
export function formatBytes(value: number | null | undefined) {
  if (value == null) return "Chưa có";
  return value >= 1048576
    ? `${(value / 1048576).toFixed(1)} MB`
    : value >= 1024
      ? `${(value / 1024).toFixed(1)} KB`
      : `${value} B`;
}
export function formatNumber(value: number | null | undefined) {
  return value == null
    ? "Chưa có"
    : new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 3 }).format(
        value,
      );
}
export const assetUrl = (id: string, thumbnail = false) =>
  `/api/admin/v1/assets/${encodeURIComponent(id)}/content${thumbnail ? "?variant=thumbnail" : ""}`;
