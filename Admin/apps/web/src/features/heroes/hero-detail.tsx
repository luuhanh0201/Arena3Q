"use client";
import { useEffect } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  ArrowUpRight,
  Box,
  Clock3,
  FileClock,
  Pencil,
  ShieldCheck,
} from "lucide-react";
import {
  allows,
  heroTabs,
  tabLabels,
  type Asset,
  type HeroProfile,
  type HeroSections,
  type HeroTab,
  type Taxonomy,
} from "@arena3q/contracts";
import { useAdmin } from "@/components/admin-shell";
import { usePlannedAction } from "@/components/action-dialog";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ErrorState, Loading } from "@/components/states";
import { api } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import { Portrait, CopyId, kindLabel, statusLabel } from "./shared";
import { HeroOverview, SectionContent } from "./sections";
export function HeroDetail({ id }: { id: string }) {
  const search = useSearchParams();
  const router = useRouter();
  const { user } = useAdmin();
  const planned = usePlannedAction();
  const requested = search.get("tab") ?? "overview";
  const tab = heroTabs.includes(requested as HeroTab)
    ? (requested as HeroTab)
    : "overview";
  const hero = useQuery({
    queryKey: ["hero", id],
    queryFn: () => api<HeroProfile>("/heroes/" + encodeURIComponent(id)),
  });
  const taxonomy = useQuery({
    queryKey: ["taxonomy"],
    queryFn: () => api<Taxonomy>("/hero-taxonomy"),
  });
  const section = useQuery({
    queryKey: ["hero-section", id, tab],
    queryFn: () =>
      api<HeroSections[keyof HeroSections] | Asset[]>(
        `/heroes/${encodeURIComponent(id)}/${tab === "media" ? "assets" : tab}`,
      ),
    enabled: tab !== "overview" && !!hero.data,
  });
  useEffect(() => {
    if (requested !== tab) {
      const params = new URLSearchParams(search);
      params.set("tab", "overview");
      router.replace(
        `/admin/game/heroes/detail/${encodeURIComponent(id)}?${params}`,
        { scroll: false },
      );
    }
  }, [requested, tab, id, router, search]);
  function setTab(next: string) {
    const params = new URLSearchParams(search);
    params.set("tab", next);
    router.push(
      `/admin/game/heroes/detail/${encodeURIComponent(id)}?${params}`,
      { scroll: false },
    );
  }
  let back = "/admin/game/heroes";
  const returnTo = search.get("returnTo");
  if (returnTo?.startsWith("/admin/game/heroes?")) back = returnTo;
  if (hero.isPending) return <Loading label="Đang tải hồ sơ tướng..." />;
  if (hero.error)
    return <ErrorState error={hero.error} retry={() => hero.refetch()} />;
  if (!hero.data) return null;
  const record = hero.data.data;
  const faction =
    taxonomy.data?.data.factions.find((item) => item.id === record.factionId)
      ?.name ?? record.factionId;
  return (
    <>
      <Link
        href={back}
        className="mb-5 inline-flex items-center gap-2 text-xs text-muted-foreground hover:text-primary"
      >
        <ArrowLeft className="size-3.5" />
        Danh sách tướng
      </Link>
      <header className="panel mb-4 flex flex-wrap items-center gap-5 p-5 lg:p-6">
        <Portrait
          assetId={record.portraitAssetId}
          name={record.displayName}
          className="size-20 lg:size-24"
        />
        <div className="min-w-0 flex-1">
          <div className="mb-2 flex flex-wrap items-center gap-3">
            <h1>{record.displayName}</h1>
            <span className="badge badge-blue">
              {statusLabel(record.status)}
            </span>
            <span className="badge badge-green">Nguồn Unity local</span>
          </div>
          <CopyId id={record.id} />
          <div className="mt-3 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
            <span>{kindLabel(record.kind)}</span>
            <span className="h-3 w-px bg-border" />
            <span>{faction ?? "Không có phe"}</span>
            <span className="h-3 w-px bg-border" />
            <span>{record.rarityId ?? "Phẩm chất chưa khai báo"}</span>
          </div>
        </div>
        <div className="flex flex-col items-start gap-4 sm:items-end">
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setTab("versions")}
            >
              <FileClock />
              Lịch sử
            </Button>
            <Button
              size="sm"
              disabled={!allows(user, ["admin.heroes.edit"])}
              title={
                !allows(user, ["admin.heroes.edit"])
                  ? "Bạn chưa được cấp quyền chỉnh sửa"
                  : undefined
              }
              onClick={() => planned("Chỉnh sửa tướng")}
            >
              <Pencil />
              Chỉnh sửa
            </Button>
          </div>
          <span className="flex items-center gap-2 text-[10px] text-muted-foreground">
            <Clock3 className="size-3" />
            Nguồn cập nhật: {formatDate(record.updatedAt)}
          </span>
        </div>
      </header>
      <Tabs value={tab} onValueChange={setTab}>
        <TabsList aria-label="Các tab hồ sơ tướng" className="detail-tabs">
          {heroTabs.map((value) => (
            <TabsTrigger key={value} value={value}>
              {tabLabels[value]}
            </TabsTrigger>
          ))}
        </TabsList>
        <div role="tabpanel" aria-label={tabLabels[tab]}>
          {tab === "overview" ? (
            <HeroOverview hero={record} setTab={setTab} />
          ) : section.isPending ? (
            <Loading label={`Đang tải ${tabLabels[tab].toLowerCase()}...`} />
          ) : section.error ? (
            <ErrorState error={section.error} retry={() => section.refetch()} />
          ) : (
            <SectionContent
              tab={tab}
              data={section.data?.data}
              hero={record}
              setTab={setTab}
            />
          )}
        </div>
      </Tabs>
      <div className="mt-5 flex items-start gap-2 text-[10px] leading-5 text-muted-foreground">
        <ShieldCheck className="mt-0.5 size-3 shrink-0" />
        <span>
          Nguồn Unity local. Chỉ số kỹ năng chỉ thay đổi trong game sau khi bạn
          lưu bản nháp và bấm áp dụng.
          <br />
          <code className="source-path">{record.sourcePath}</code>
        </span>
      </div>
    </>
  );
}
