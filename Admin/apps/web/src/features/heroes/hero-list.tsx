"use client";
import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
} from "@tanstack/react-table";
import {
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  ArrowUpRight,
  Boxes,
  ChevronLeft,
  ChevronRight,
  Download,
  Eye,
  FileInput,
  Filter,
  Layers3,
  LoaderCircle,
  Plus,
  Search,
  ShieldCheck,
  Sparkles,
  UsersRound,
  X,
} from "lucide-react";
import {
  allows,
  type HeroListItem,
  type Taxonomy,
  heroQuerySchema,
} from "@arena3q/contracts";
import { useAdmin } from "@/components/admin-shell";
import { usePlannedAction } from "@/components/action-dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { EmptyState, ErrorState, Loading } from "@/components/states";
import { api } from "@/lib/api";
import { cn, formatDate } from "@/lib/utils";
import {
  CoverageLinks,
  CopyId,
  Portrait,
  kindLabel,
  statusLabel,
} from "./shared";

export function HeroList() {
  const router = useRouter();
  const search = useSearchParams();
  const { sourceImportedAt, user } = useAdmin();
  const planned = usePlannedAction();
  const [term, setTerm] = useState(search.get("q") ?? "");
  const [exporting, setExporting] = useState(false);
  const [exportError, setExportError] = useState("");
  const queryString = search.toString();
  const parsed = heroQuerySchema.safeParse(Object.fromEntries(search));
  const result = useQuery({
    queryKey: ["heroes", queryString],
    queryFn: () => api<HeroListItem[]>(`/heroes?${queryString}`),
    placeholderData: keepPreviousData,
    enabled: parsed.success,
  });
  const taxonomy = useQuery({
    queryKey: ["taxonomy"],
    queryFn: () => api<Taxonomy>("/hero-taxonomy"),
  });
  useEffect(() => setTerm(search.get("q") ?? ""), [search]);
  function change(key: string, value: string) {
    const params = new URLSearchParams(search);
    if (value) params.set(key, value);
    else params.delete(key);
    if (key !== "page") params.delete("page");
    router.push("/admin/game/heroes?" + params.toString(), { scroll: false });
  }
  function sort(key: string) {
    const params = new URLSearchParams(search);
    params.set("sort", key);
    params.set(
      "order",
      search.get("sort") === key && search.get("order") !== "desc"
        ? "desc"
        : "asc",
    );
    params.delete("page");
    router.push("/admin/game/heroes?" + params, { scroll: false });
  }
  const names = (collection: keyof Taxonomy, id: string | null) =>
    id
      ? (taxonomy.data?.data[collection].find((item) => item.id === id)?.name ??
        id)
      : "Chưa khai báo";
  const returnTo =
    "/admin/game/heroes" + (queryString ? "?" + queryString : "");
  const columns = useMemo<ColumnDef<HeroListItem>[]>(
    () => [
      {
        id: "hero",
        header: () => (
          <button
            className="flex items-center gap-2"
            onClick={() => sort("displayName")}
          >
            Tướng
            <ArrowUpDown className="size-3" />
          </button>
        ),
        cell: ({ row }) => (
          <div className="flex min-w-[210px] items-center gap-3">
            <Link
              href={`/admin/game/heroes/detail/${encodeURIComponent(row.original.id)}?tab=overview&returnTo=${encodeURIComponent(returnTo)}`}
            >
              <Portrait
                assetId={row.original.portraitAssetId}
                name={row.original.displayName}
                className="size-[54px]"
              />
            </Link>
            <div>
              <Link
                className="text-[14px] font-semibold text-slate-100 hover:text-primary"
                href={`/admin/game/heroes/detail/${encodeURIComponent(row.original.id)}?tab=overview&returnTo=${encodeURIComponent(returnTo)}`}
              >
                {row.original.displayName}
              </Link>
              <div className="mt-1">
                <CopyId id={row.original.id} />
              </div>
            </div>
          </div>
        ),
      },
      {
        id: "faction",
        header: "Phe",
        cell: ({ row }) => (
          <span
            className={
              row.original.factionId
                ? "badge badge-amber"
                : "text-xs text-muted-foreground"
            }
          >
            {row.original.factionId
              ? names("factions", row.original.factionId)
              : "Không có phe"}
          </span>
        ),
      },
      {
        id: "kind",
        header: "Loại tướng",
        cell: ({ row }) => (
          <span className="text-xs text-slate-300">
            {kindLabel(row.original.kind)}
          </span>
        ),
      },
      {
        id: "role",
        header: "Vai trò",
        cell: ({ row }) => (
          <span className="text-xs text-muted-foreground">
            {row.original.roleIds.length
              ? row.original.roleIds.map((id) => names("roles", id)).join(", ")
              : "Chưa khai báo"}
          </span>
        ),
      },
      {
        id: "rarity",
        header: "Phẩm chất",
        cell: ({ row }) => (
          <span className="text-xs text-muted-foreground">
            {names("rarities", row.original.rarityId)}
          </span>
        ),
      },
      {
        id: "assets",
        header: "Tình trạng tài nguyên",
        cell: ({ row }) => (
          <CoverageLinks
            id={row.original.id}
            coverage={row.original.coverage}
            returnTo={returnTo}
          />
        ),
      },
      {
        id: "status",
        header: () => (
          <button
            className="flex items-center gap-2"
            onClick={() => sort("status")}
          >
            Trạng thái
            <ArrowUpDown className="size-3" />
          </button>
        ),
        cell: ({ row }) => (
          <span
            className={cn(
              "badge",
              row.original.status === "PUBLISHED"
                ? "badge-green"
                : "badge-blue",
            )}
          >
            {statusLabel(row.original.status)}
          </span>
        ),
      },
      {
        id: "updated",
        header: () => (
          <button
            className="flex items-center gap-2"
            onClick={() => sort("updatedAt")}
          >
            Cập nhật
            <ArrowUpDown className="size-3" />
          </button>
        ),
        cell: ({ row }) => (
          <div className="min-w-28 text-[11px] text-muted-foreground">
            {formatDate(row.original.updatedAt)}
            <div className="mt-1 text-[10px] text-[#657c9a]">Nguồn Unity</div>
          </div>
        ),
      },
      {
        id: "actions",
        header: "",
        cell: ({ row }) => (
          <Button asChild variant="ghost" size="icon">
            <Link
              title={`Xem ${row.original.displayName}`}
              aria-label={`Xem ${row.original.displayName}`}
              href={`/admin/game/heroes/detail/${encodeURIComponent(row.original.id)}?tab=overview&returnTo=${encodeURIComponent(returnTo)}`}
            >
              <ArrowUpRight />
            </Link>
          </Button>
        ),
      },
    ],
    [taxonomy.data, queryString],
  );
  const table = useReactTable({
    data: result.data?.data ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    manualSorting: true,
  });
  async function exportCsv() {
    setExporting(true);
    setExportError("");
    try {
      const params = new URLSearchParams(search);
      params.set("pageSize", "100");
      const rows: HeroListItem[] = [];
      let page = 1;
      let pages = 1;
      do {
        params.set("page", String(page));
        const response = await api<HeroListItem[]>("/heroes?" + params);
        rows.push(...response.data);
        pages = response.meta.pages ?? 0;
        page++;
      } while (page <= pages);
      const cell = (value: string) =>
        '"' +
        ((/^[=+@\-\t\r]/.test(value) ? "'" : "") + value).replaceAll(
          '"',
          '""',
        ) +
        '"';
      const csv =
        "\uFEFF" +
        [
          ["ID", "Tên tướng", "Phe", "Loại", "Trạng thái", "Cập nhật"],
          ...rows.map((row) => [
            row.id,
            row.displayName,
            row.factionId ?? "",
            row.kind,
            row.status,
            row.updatedAt,
          ]),
        ]
          .map((row) => row.map(cell).join(","))
          .join("\r\n");
      const url = URL.createObjectURL(
        new Blob([csv], { type: "text/csv;charset=utf-8" }),
      );
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download =
        "arena3q-tuong-" + new Date().toISOString().slice(0, 10) + ".csv";
      anchor.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (error) {
      setExportError(
        error instanceof Error ? error.message : "Không thể xuất CSV.",
      );
    } finally {
      setExporting(false);
    }
  }
  const total = result.data?.meta.total ?? 0;
  const page = parsed.success ? parsed.data.page : 1;
  const size = parsed.success ? parsed.data.pageSize : 25;
  const pages = result.data?.meta.pages ?? 0;
  return (
    <>
      <div className="page-heading">
        <div>
          <div className="mb-2 flex items-center gap-2">
            <span className="eyebrow">GAME / TƯỚNG</span>
            <span className="badge badge-green">Chỉ xem</span>
          </div>
          <h1>
            Quản lý tướng{" "}
            <span className="ml-2 align-middle text-base font-normal text-muted-foreground">
              {result.data ? total : ""}
            </span>
          </h1>
          <p>
            Hồ sơ, kỹ năng và tài nguyên của từng tướng trong cùng một không
            gian.
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            onClick={() => void exportCsv()}
            disabled={exporting || !parsed.success}
          >
            {exporting ? (
              <LoaderCircle className="animate-spin" />
            ) : (
              <Download />
            )}
            Xuất CSV
          </Button>
          <Button
            disabled={!allows(user, ["admin.heroes.create"])}
            onClick={() => planned("Tạo tướng")}
          >
            <Plus />
            Tạo tướng
          </Button>
        </div>
      </div>
      <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {[
          {
            label: "Tướng theo bộ lọc",
            value: total,
            Icon: UsersRound,
            sub: "Bản ghi từ snapshot Unity",
          },
          {
            label: "Có ảnh đại diện",
            value:
              result.data?.data.filter((hero) => hero.coverage.portrait)
                .length ?? 0,
            Icon: Layers3,
            sub: "Trong trang hiện tại",
          },
          {
            label: "Có metadata animation",
            value:
              result.data?.data.filter((hero) => hero.coverage.animations > 0)
                .length ?? 0,
            Icon: Sparkles,
            sub: "Trong trang hiện tại",
          },
          {
            label: "Bản ghi đã phát hành",
            value:
              result.data?.data.filter((hero) => hero.status === "PUBLISHED")
                .length ?? 0,
            Icon: ShieldCheck,
            sub: "Trong trang hiện tại",
          },
        ].map(({ label, value, Icon, sub }) => (
          <div className="panel relative overflow-hidden p-5" key={label}>
            <div className="flex items-center justify-between gap-2">
              <span className="text-xs text-muted-foreground">{label}</span>
              <Icon className="size-4 text-[#a88a61]" />
            </div>
            <div className="mt-4 text-[28px] font-semibold leading-none tracking-tight">
              {result.isPending ? "..." : value}
            </div>
            <p className="mt-3 text-[10px] text-[#7388a4]">{sub}</p>
          </div>
        ))}
      </div>
      <section className="panel overflow-hidden">
        <div className="flex flex-wrap items-center justify-between gap-4 border-b border-border px-5 py-4">
          <div className="flex items-center gap-3">
            <div className="h-4 w-0.5 rounded bg-primary" />
            <h2 className="text-sm!">Danh sách tướng</h2>
            <span className="badge">{total} bản ghi</span>
          </div>
          <div className="flex items-center gap-3">
            <span className="hidden items-center gap-1.5 text-[10px] text-muted-foreground md:flex">
              <span className="size-1.5 rounded-full bg-emerald-400" />
              Snapshot: {formatDate(sourceImportedAt)}
            </span>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => planned("Nhập dữ liệu")}
              disabled={!allows(user, ["admin.heroes.edit"])}
            >
              <FileInput />
              Nhập dữ liệu
            </Button>
          </div>
        </div>
        <div className="space-y-3 border-b border-border p-5">
          <div className="flex flex-wrap gap-3">
            <form
              className="relative min-w-56 flex-1"
              onSubmit={(event) => {
                event.preventDefault();
                change("q", term.trim());
              }}
            >
              <Search className="absolute left-3 top-3 size-4 text-muted-foreground" />
              <Input
                aria-label="Tìm kiếm tướng"
                placeholder="Tìm tên, tên không dấu hoặc ID tướng..."
                value={term}
                onChange={(event) => setTerm(event.target.value)}
                className="pl-10 pr-16"
              />
              <button
                className="absolute right-3 top-2.5 text-[11px] text-primary"
                type="submit"
              >
                Tìm
              </button>
            </form>
            <select
              aria-label="Lọc phe"
              value={search.get("faction") ?? ""}
              onChange={(event) => change("faction", event.target.value)}
            >
              <option value="">Tất cả phe</option>
              <option value="none">Không có phe</option>
              {taxonomy.data?.data.factions.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </select>
            <select
              aria-label="Lọc loại tướng"
              value={search.get("kind") ?? ""}
              onChange={(event) => change("kind", event.target.value)}
            >
              <option value="">Tất cả loại tướng</option>
              {taxonomy.data?.data.kinds.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </select>
            <select
              aria-label="Lọc trạng thái"
              value={search.get("status") ?? ""}
              onChange={(event) => change("status", event.target.value)}
            >
              <option value="">Tất cả trạng thái</option>
              <option value="DRAFT">Bản nháp</option>
              <option value="IN_REVIEW">Chờ duyệt</option>
              <option value="PUBLISHED">Đã phát hành</option>
              <option value="ARCHIVED">Lưu trữ</option>
            </select>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <Filter className="size-3.5 text-muted-foreground" />
            <select
              aria-label="Lọc tình trạng dữ liệu"
              value={search.get("missing") ?? ""}
              onChange={(event) => change("missing", event.target.value)}
              className="h-8! text-xs!"
            >
              <option value="">Tình trạng dữ liệu</option>
              <option value="portrait">Thiếu ảnh</option>
              <option value="model">Thiếu model</option>
              <option value="rig">Thiếu rig</option>
              <option value="animations">Thiếu animation</option>
              <option value="skills">Thiếu kỹ năng</option>
              <option value="stats">Thiếu chỉ số</option>
            </select>
            {(["roles", "rarities"] as const).map((key, index) => (
              <select
                key={key}
                aria-label={key === "roles" ? "Lọc vai trò" : "Lọc phẩm chất"}
                disabled={!taxonomy.data?.data[key].length}
                value={search.get(key === "roles" ? "role" : "rarity") ?? ""}
                onChange={(event) =>
                  change(
                    key === "roles" ? "role" : "rarity",
                    event.target.value,
                  )
                }
                className="h-8! text-xs!"
              >
                <option value="">
                  {key === "roles" ? "Vai trò" : "Phẩm chất"}
                  {!taxonomy.data?.data[key].length ? " chưa khai báo" : ""}
                </option>
                {taxonomy.data?.data[key].map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            ))}
            {queryString && (
              <Button
                variant="ghost"
                size="sm"
                className="h-8"
                onClick={() => router.push("/admin/game/heroes")}
              >
                <X />
                Xóa bộ lọc
              </Button>
            )}
            {result.isFetching && !result.isPending && (
              <span className="ml-auto inline-flex items-center gap-2 text-xs text-muted-foreground">
                <LoaderCircle className="size-3 animate-spin" />
                Đang cập nhật
              </span>
            )}
          </div>
        </div>
        {exportError && (
          <p role="alert" className="px-5 pt-4 text-sm text-red-300">
            {exportError}
          </p>
        )}
        {!parsed.success ? (
          <ErrorState
            error={new Error("Bộ lọc không hợp lệ.")}
            retry={() => router.replace("/admin/game/heroes")}
          />
        ) : result.isPending ? (
          <Loading />
        ) : result.error ? (
          <ErrorState error={result.error} retry={() => result.refetch()} />
        ) : !result.data?.data.length ? (
          <div className="p-5">
            <EmptyState
              title={
                queryString ? "Không có tướng phù hợp" : "Chưa có dữ liệu tướng"
              }
              description={
                queryString
                  ? "Thử thay đổi hoặc xóa bộ lọc hiện tại."
                  : "Nhập snapshot dữ liệu Unity để bắt đầu xem hồ sơ tướng."
              }
            />
          </div>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                {table.getHeaderGroups().map((group) => (
                  <tr key={group.id}>
                    {group.headers.map((header) => (
                      <th key={header.id}>
                        {flexRender(
                          header.column.columnDef.header,
                          header.getContext(),
                        )}
                      </th>
                    ))}
                  </tr>
                ))}
              </thead>
              <tbody>
                {table.getRowModel().rows.map((row) => (
                  <tr
                    key={row.id}
                    tabIndex={0}
                    aria-label={`Mở hồ sơ ${row.original.displayName}`}
                    className="cursor-pointer outline-none focus-visible:bg-accent"
                    onKeyDown={(event) => {
                      if (
                        event.key === "Enter" &&
                        event.target === event.currentTarget
                      )
                        router.push(
                          `/admin/game/heroes/detail/${encodeURIComponent(row.original.id)}?tab=overview&returnTo=${encodeURIComponent("/admin/game/heroes?" + queryString)}`,
                        );
                    }}
                    onClick={(event) => {
                      if (
                        !(event.target as HTMLElement).closest(
                          "a,button,input,select",
                        )
                      )
                        router.push(
                          `/admin/game/heroes/detail/${encodeURIComponent(row.original.id)}?tab=overview&returnTo=${encodeURIComponent("/admin/game/heroes?" + queryString)}`,
                        );
                    }}
                  >
                    {row.getVisibleCells().map((cell) => (
                      <td key={cell.id}>
                        {flexRender(
                          cell.column.columnDef.cell,
                          cell.getContext(),
                        )}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <div className="flex flex-wrap items-center justify-between gap-4 border-t border-border px-5 py-4 text-xs text-muted-foreground">
          <span>
            Hiển thị {total ? Math.min((page - 1) * size + 1, total) : 0}–
            {Math.min(page * size, total)} / {total} tướng
          </span>
          <div className="flex items-center gap-3">
            <span className="hidden sm:inline">Số dòng</span>
            <select
              aria-label="Số dòng mỗi trang"
              className="h-8! min-w-16! text-xs!"
              value={String(size)}
              onChange={(event) => change("pageSize", event.target.value)}
            >
              {[10, 25, 50, 100].map((value) => (
                <option key={value}>{value}</option>
              ))}
            </select>
            <span className="mx-2">
              {page} / {Math.max(1, pages)}
            </span>
            <Button
              variant="outline"
              size="icon"
              className="size-8"
              aria-label="Trang trước"
              disabled={page <= 1 || result.isFetching}
              onClick={() => change("page", String(page - 1))}
            >
              <ChevronLeft />
            </Button>
            <Button
              variant="outline"
              size="icon"
              className="size-8"
              aria-label="Trang sau"
              disabled={page >= pages || result.isFetching}
              onClick={() => change("page", String(page + 1))}
            >
              <ChevronRight />
            </Button>
          </div>
        </div>
      </section>
      <p className="mt-4 flex items-center gap-2 text-[11px] leading-5 text-muted-foreground">
        <ShieldCheck className="size-3.5 shrink-0" />
        Dữ liệu đọc từ snapshot Unity. Nhóm thông tin chưa được khai báo được
        giữ nguyên trạng thái thiếu dữ liệu.
      </p>
    </>
  );
}
