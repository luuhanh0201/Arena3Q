"use client";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { ArrowUpRight, ChevronRight, FolderOpen } from "lucide-react";
import { allows, flattenMenu } from "@arena3q/contracts";
import { useAdmin } from "@/components/admin-shell";
import { ErrorState, FeaturePlaceholder, Loading } from "@/components/states";
import { ApiFailure } from "@/lib/api";
import { HeroList } from "./heroes/hero-list";
import { HeroDetail } from "./heroes/hero-detail";
import { GameOverview } from "./heroes/overview";
export function AdminWorkspace() {
  const path = usePathname();
  const router = useRouter();
  const { user, manifest } = useAdmin();
  const legacy =
    path.startsWith("/admin/game/skills") ||
    ["stats", "animations", "skins", "media", "models"].some(
      (section) => path === `/admin/game/heroes/${section}`,
    );
  useEffect(() => {
    if (path === "/admin" || legacy) router.replace("/admin/game/heroes");
  }, [path, legacy, router]);
  if (path === "/admin" || legacy) return <Loading />;
  if (
    path === "/admin/game/heroes" ||
    path.startsWith("/admin/game/heroes/detail/")
  ) {
    if (!allows(user, ["admin.heroes.read"]))
      return (
        <ErrorState
          error={
            new ApiFailure(
              403,
              "Quyền admin.heroes.read chưa được cấp cho tài khoản này.",
            )
          }
        />
      );
    return path === "/admin/game/heroes" ? (
      <HeroList />
    ) : (
      <HeroDetail
        id={decodeURIComponent(path.slice("/admin/game/heroes/detail/".length))}
      />
    );
  }
  if (path === "/admin/dashboard/game")
    return allows(user, ["admin.heroes.read"]) ? (
      <GameOverview />
    ) : (
      <ErrorState
        error={new ApiFailure(403, "Bạn không có quyền xem dữ liệu tướng.")}
      />
    );
  const feature = [...flattenMenu(manifest.menu)]
    .reverse()
    .find((feature) => feature.route === path);
  if (!feature)
    return (
      <ErrorState
        error={
          new ApiFailure(404, "Đường dẫn không có trong cấu hình điều hướng.")
        }
      />
    );
  if (!allows(user, feature.requiredPermissions ?? []))
    return (
      <ErrorState
        error={
          new ApiFailure(403, "Bạn không có quyền truy cập chức năng này.")
        }
      />
    );
  if (feature.children?.length)
    return (
      <>
        <div className="page-heading">
          <div>
            <span className="eyebrow">KHÔNG GIAN QUẢN TRỊ</span>
            <h1 className="mt-2">{feature.label}</h1>
            <p>Chọn một chức năng để tiếp tục.</p>
          </div>
        </div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {feature.children.map((child) => (
            <Link
              className="panel group flex items-start gap-4 p-6 transition-colors hover:border-primary/40"
              key={child.id}
              href={child.route}
            >
              <div className="grid size-10 shrink-0 place-items-center rounded-lg bg-secondary">
                <FolderOpen className="size-4 text-primary" />
              </div>
              <div className="min-w-0 flex-1">
                <h2 className="text-sm!">{child.label}</h2>
                <p className="mt-2 text-xs text-muted-foreground">
                  {child.children?.length
                    ? `${child.children.length} chức năng`
                    : "Mở chức năng"}
                </p>
              </div>
              <ChevronRight className="mt-1 size-4 text-muted-foreground group-hover:text-primary" />
            </Link>
          ))}
        </div>
      </>
    );
  if ((manifest.overrides[feature.id] ?? feature.status) === "DISABLED")
    return (
      <div className="state-card">
        <h1>Tạm ngừng để bảo trì</h1>
        <p>{feature.label} hiện đang tạm khóa.</p>
      </div>
    );
  return <FeaturePlaceholder feature={feature} />;
}
