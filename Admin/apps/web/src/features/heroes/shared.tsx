"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import {
  Box,
  Check,
  Copy,
  Film,
  ImageIcon,
  Network,
  Sparkles,
} from "lucide-react";
import { allows, type Coverage } from "@arena3q/contracts";
import { useAdmin } from "@/components/admin-shell";
import { assetUrl, cn } from "@/lib/utils";
export function Portrait({
  assetId,
  name,
  className,
}: {
  assetId: string | null;
  name: string;
  className?: string;
}) {
  const [failed, setFailed] = useState(false);
  const { user } = useAdmin();
  useEffect(() => setFailed(false), [assetId]);
  return (
    <div
      className={cn(
        "grid shrink-0 place-items-center overflow-hidden rounded-xl border border-[#3c4655] bg-[#101a2b]",
        className,
      )}
    >
      {assetId && !failed && allows(user, ["admin.assets.read"]) ? (
        <img
          src={assetUrl(assetId, true)}
          alt={name}
          loading="lazy"
          className="h-full w-full object-contain"
          onError={() => setFailed(true)}
        />
      ) : (
        <ImageIcon
          aria-label={
            failed
              ? "Không thể tải ảnh đại diện"
              : assetId
                ? "Không có quyền xem ảnh"
                : "Chưa có ảnh đại diện"
          }
          className="size-6 text-[#50627f]"
        />
      )}
    </div>
  );
}
export function CopyId({ id }: { id: string }) {
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState(false);
  return (
    <button
      aria-label={`Sao chép ID ${id}`}
      title={
        error
          ? "Không thể sao chép. Hãy chọn ID và sao chép thủ công."
          : "Sao chép ID"
      }
      className="inline-flex items-center gap-1.5 text-[10px] text-muted-foreground hover:text-primary"
      onClick={async () => {
        try {
          await navigator.clipboard.writeText(id);
          setCopied(true);
          setError(false);
          setTimeout(() => setCopied(false), 1800);
        } catch {
          setError(true);
        }
      }}
    >
      <code>{id}</code>
      {copied ? (
        <Check className="size-3 text-emerald-400" />
      ) : (
        <Copy className="size-3" />
      )}
      <span role="status" className="sr-only">
        {copied ? "Đã sao chép ID" : error ? "Không thể sao chép ID" : ""}
      </span>
    </button>
  );
}
export function CoverageLinks({
  id,
  coverage,
  returnTo,
}: {
  id: string;
  coverage: Coverage;
  returnTo?: string;
}) {
  const items = [
    { key: "portrait", tab: "media", name: "Hình ảnh", Icon: ImageIcon },
    { key: "model", tab: "model", name: "Model", Icon: Box },
    { key: "rig", tab: "rig", name: "Rig", Icon: Network },
    { key: "animations", tab: "animations", name: "Animation", Icon: Film },
    { key: "skills", tab: "skills", name: "Kỹ năng", Icon: Sparkles },
  ] as const;
  return (
    <div className="flex items-center gap-1">
      {items.map(({ key, tab, name, Icon }) => (
        <Link
          key={key}
          href={`/admin/game/heroes/detail/${encodeURIComponent(id)}?tab=${tab}${returnTo ? "&returnTo=" + encodeURIComponent(returnTo) : ""}`}
          aria-label={`${name} ${id}: ${coverage[key] ? "Có dữ liệu" : "Chưa có"}`}
          title={`${name}: ${typeof coverage[key] === "number" ? coverage[key] + " mục" : coverage[key] ? "Có dữ liệu" : "Chưa có dữ liệu"}`}
          className={cn(
            "grid size-7 place-items-center rounded-md border border-transparent transition-colors hover:border-primary/40",
            coverage[key]
              ? "bg-emerald-400/[.07] text-emerald-400/90"
              : "bg-secondary/50 text-[#536580]",
          )}
        >
          <Icon className="size-3.5" />
        </Link>
      ))}
    </div>
  );
}
export const kindLabel = (kind: string) =>
  ({ Named: "Danh tướng", Lord: "Chủ tướng", Starter: "Khởi đầu" })[kind] ??
  kind;
export const statusLabel = (status: string) =>
  ({
    DRAFT: "Bản nháp",
    IN_REVIEW: "Chờ duyệt",
    PUBLISHED: "Đã phát hành",
    ARCHIVED: "Lưu trữ",
  })[status] ?? status;
