"use client";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  AlertCircle,
  ArrowLeft,
  ArrowRight,
  Construction,
  Database,
  LoaderCircle,
  LockKeyhole,
  RefreshCw,
  SearchX,
} from "lucide-react";
import type { Feature } from "@arena3q/contracts";
import { ApiFailure } from "@/lib/api";
import { Button } from "./ui/button";
export function Loading({ label = "Đang tải dữ liệu..." }: { label?: string }) {
  return (
    <div
      role="status"
      className="flex min-h-60 items-center justify-center gap-3 text-sm text-muted-foreground"
    >
      <LoaderCircle className="size-5 animate-spin" />
      {label}
    </div>
  );
}
export function EmptyState({
  title = "Chưa có dữ liệu",
  description = "Dữ liệu chưa được khai báo trong nguồn hiện tại.",
  children,
}: {
  title?: string;
  description?: string;
  children?: React.ReactNode;
}) {
  return (
    <div className="state-card">
      <div className="state-icon">
        <Database />
      </div>
      <h3>{title}</h3>
      <p>{description}</p>
      {children}
    </div>
  );
}
export function ErrorState({
  error,
  retry,
}: {
  error: unknown;
  retry?: () => unknown;
}) {
  const failure = error instanceof ApiFailure ? error : null;
  const forbidden = failure?.status === 403;
  const missing = failure?.status === 404;
  const unauthenticated = failure?.status === 401;
  const Icon =
    forbidden || unauthenticated
      ? LockKeyhole
      : missing
        ? SearchX
        : AlertCircle;
  return (
    <div role="alert" className="state-card">
      <div className="state-icon text-amber-400">
        <Icon />
      </div>
      <span className="eyebrow">{failure?.status || "API"}</span>
      <h3>
        {forbidden
          ? "Bạn không có quyền truy cập"
          : missing
            ? "Không tìm thấy dữ liệu"
            : unauthenticated
              ? "Phiên đăng nhập đã hết hạn"
              : "Không thể tải dữ liệu"}
      </h3>
      <p>
        {failure?.message ?? "Đã xảy ra lỗi khi kết nối. Vui lòng thử lại."}
      </p>
      {failure?.requestId && (
        <code className="text-xs text-muted-foreground">
          Mã yêu cầu: {failure.requestId}
        </code>
      )}
      <div className="mt-4 flex justify-center gap-3">
        {unauthenticated ? (
          <Button asChild>
            <Link href="/login">Đăng nhập lại</Link>
          </Button>
        ) : (
          retry && (
            <Button variant="outline" onClick={() => void retry()}>
              <RefreshCw />
              Thử lại
            </Button>
          )
        )}
        <Button variant="ghost" asChild>
          <Link href="/admin/game/heroes">
            Danh sách tướng
            <ArrowRight />
          </Link>
        </Button>
      </div>
    </div>
  );
}
export function FeaturePlaceholder({ feature }: { feature: Feature }) {
  const router = useRouter();
  return (
    <div className="state-card min-h-[60vh]">
      <div className="state-icon">
        <Construction />
      </div>
      <span className="badge badge-amber">Đang phát triển</span>
      <h1 className="mt-4">{feature.label}</h1>
      <p>
        {feature.placeholder?.description ??
          `${feature.label} chưa được triển khai.`}
      </p>
      <div className="mt-3 flex items-center justify-center gap-2 text-xs text-muted-foreground">
        <code>{feature.id}</code>
        <span className="badge">{feature.priority}</span>
      </div>
      <p className="mt-4 text-xs!">Chưa xác định thời điểm phát hành.</p>
      <div className="mt-6 flex justify-center gap-3">
        <Button variant="outline" onClick={() => router.back()}>
          <ArrowLeft />
          Về trang trước
        </Button>
        <Button asChild>
          <Link href="/admin/game/heroes">
            Về danh sách tướng
            <ArrowRight />
          </Link>
        </Button>
      </div>
    </div>
  );
}
