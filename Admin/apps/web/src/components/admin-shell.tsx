"use client";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Activity,
  ArrowUpRight,
  ChevronDown,
  ChevronRight,
  CircleHelp,
  Coins,
  Gamepad2,
  LayoutDashboard,
  LogOut,
  Menu,
  PanelLeftClose,
  Search,
  Settings2,
  ShieldCheck,
  Sparkles,
  Users,
  X,
} from "lucide-react";
import {
  adminMenu,
  allows,
  flattenMenu,
  normalizeSearch,
  type CurrentUser,
  type Feature,
  type FeatureStatus,
} from "@arena3q/contracts";
import { api, ApiFailure } from "@/lib/api";
import { cn } from "@/lib/utils";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { ErrorState, Loading } from "./states";
import { PlannedActionProvider, usePlannedAction } from "./action-dialog";
type Manifest = {
  menu: Feature[];
  overrides: Record<string, FeatureStatus>;
  heroTabs: string[];
};
interface AdminContextValue {
  user: CurrentUser;
  manifest: Manifest;
  sourceImportedAt: string | null;
}
const AdminContext = createContext<AdminContextValue | null>(null);
export function useAdmin() {
  const value = useContext(AdminContext);
  if (!value) throw new Error("Admin context is required.");
  return value;
}
const groupIcons: Record<string, typeof Gamepad2> = {
  DASHBOARD: LayoutDashboard,
  OPERATIONS: Users,
  GAME: Gamepad2,
  EVENTS: Sparkles,
  FINANCE: Coins,
  SYSTEM: Settings2,
};
function pathFor(
  nodes: Feature[],
  path: string,
  parents: Feature[] = [],
): Feature[] | null {
  for (const item of nodes) {
    const nested = pathFor(item.children ?? [], path, [...parents, item]);
    if (nested) return nested;
    if (item.route === path) return [...parents, item];
  }
  return null;
}
export function AdminShell({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const client = useQueryClient();
  const user = useQuery({
    queryKey: ["me"],
    queryFn: () => api<CurrentUser>("/auth/me"),
    retry: false,
  });
  const features = useQuery({
    queryKey: ["features"],
    queryFn: () => api<Manifest>("/features"),
    enabled: !!user.data,
  });
  useEffect(() => {
    if (user.error instanceof ApiFailure && user.error.status === 401)
      router.replace(
        "/login?returnTo=" +
          encodeURIComponent(pathname + window.location.search),
      );
  }, [user.error, router, pathname]);
  if (
    user.isPending ||
    (user.error instanceof ApiFailure && user.error.status === 401)
  )
    return <Loading label="Đang kiểm tra phiên đăng nhập..." />;
  if (user.error)
    return (
      <main className="p-8">
        <ErrorState error={user.error} retry={() => user.refetch()} />
      </main>
    );
  if (features.isPending)
    return <Loading label="Đang tải cấu hình quản trị..." />;
  if (features.error)
    return (
      <main className="p-8">
        <ErrorState error={features.error} retry={() => features.refetch()} />
      </main>
    );
  if (!user.data || !features.data) return null;
  return (
    <AdminContext.Provider
      value={{
        user: user.data.data,
        manifest: features.data.data,
        sourceImportedAt: features.data.meta.source?.importedAt ?? null,
      }}
    >
      <PlannedActionProvider>
        <ShellLayout
          onLogout={async () => {
            await api("/auth/logout", { method: "POST", body: "{}" });
            client.clear();
            router.replace("/login");
          }}
        >
          {children}
        </ShellLayout>
      </PlannedActionProvider>
    </AdminContext.Provider>
  );
}
function ShellLayout({
  children,
  onLogout,
}: {
  children: ReactNode;
  onLogout: () => Promise<void>;
}) {
  const { user, manifest } = useAdmin();
  const pathname = usePathname();
  const router = useRouter();
  const planned = usePlannedAction();
  const [mobile, setMobile] = useState(false);
  const [compact, setCompact] = useState(false);
  const [menuSearch, setMenuSearch] = useState("");
  const [globalSearch, setGlobalSearch] = useState("");
  const [open, setOpen] = useState<string[]>(["GAME", "GAME.HEROES"]);
  const [restored, setRestored] = useState(false);
  const [logoutError, setLogoutError] = useState("");
  const activePath = pathname.startsWith("/admin/game/heroes/detail/")
    ? "/admin/game/heroes"
    : pathname;
  const breadcrumb = pathFor(manifest.menu, activePath) ?? [];
  useEffect(() => {
    try {
      const value = JSON.parse(
        localStorage.getItem("arena3q.admin.menu") ?? "null",
      );
      if (
        Array.isArray(value) &&
        value.every((item) => typeof item === "string")
      )
        setOpen(value);
    } catch {}
    setRestored(true);
  }, []);
  useEffect(() => {
    if (restored)
      localStorage.setItem("arena3q.admin.menu", JSON.stringify(open));
  }, [open, restored]);
  useEffect(() => {
    setMobile(false);
    const ancestors = pathFor(manifest.menu, activePath) ?? [];
    setOpen((current) => [
      ...new Set([...current, ...ancestors.map((item) => item.id)]),
    ]);
  }, [activePath, manifest.menu]);
  useEffect(() => {
    const listener = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key === "k") {
        event.preventDefault();
        document.getElementById("admin-global-search")?.focus();
      }
      if (event.key === "Escape") setMobile(false);
    };
    window.addEventListener("keydown", listener);
    return () => window.removeEventListener("keydown", listener);
  }, []);
  const term = normalizeSearch(menuSearch.trim());
  const matches = (item: Feature): boolean =>
    !term ||
    normalizeSearch(item.label + " " + item.id).includes(term) ||
    (item.children ?? []).some(matches);
  function toggle(id: string) {
    setOpen((current) =>
      current.includes(id)
        ? current.filter((value) => value !== id)
        : [...current, id],
    );
  }
  const showLabels = !compact || mobile;
  return (
    <div className="min-h-screen">
      <button
        aria-label="Đóng menu"
        className={cn(
          "fixed inset-0 z-30 bg-black/60 lg:hidden",
          !mobile && "hidden",
        )}
        onClick={() => setMobile(false)}
      />
      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-40 flex flex-col border-r border-border bg-[#0d1522] transition-[width,transform] duration-150",
          showLabels ? "w-[258px]" : "w-[76px]",
          mobile ? "translate-x-0" : "-translate-x-full lg:translate-x-0",
        )}
        aria-label="Điều hướng quản trị"
      >
        <Link
          href="/admin/game/heroes"
          className="flex h-[78px] shrink-0 items-center justify-center gap-3 border-b border-border px-5"
        >
          {showLabels ? (
            <>
              <img
                src="/arena3q-logo.png"
                alt="Arena3Q"
                width="148"
                height="50"
                className="h-12 w-[148px] object-contain"
              />
              <span className="rounded border border-primary/30 px-1.5 py-0.5 text-[9px] font-semibold tracking-wider text-primary">
                ADMIN
              </span>
            </>
          ) : (
            <ShieldCheck className="size-7 text-primary" />
          )}
        </Link>
        {showLabels && (
          <div className="px-4 pb-2 pt-5">
            <label className="relative block">
              <Search className="absolute left-3 top-3 size-3.5 text-muted-foreground" />
              <Input
                aria-label="Tìm menu"
                placeholder="Tìm chức năng..."
                value={menuSearch}
                onChange={(event) => setMenuSearch(event.target.value)}
                className="h-9 border-border bg-transparent pl-9 text-xs"
              />
            </label>
          </div>
        )}
        <nav className="flex-1 overflow-y-auto px-3 py-4">
          {manifest.menu.filter(matches).map((group) => {
            const Icon = groupIcons[group.id] ?? Gamepad2;
            return (
              <section key={group.id} className="mb-5">
                <div className="flex items-center justify-between">
                  <Link
                    title={group.label}
                    href={group.route}
                    className={cn(
                      "flex min-h-9 min-w-0 flex-1 items-center gap-3 rounded-lg px-3 text-[10px] font-semibold tracking-[1.3px] text-[#798ca8] transition-colors hover:bg-accent hover:text-foreground",
                      breadcrumb[0]?.id === group.id && "text-primary",
                    )}
                  >
                    <Icon className="size-4 shrink-0" />
                    {showLabels && group.label}
                  </Link>
                  {showLabels && (
                    <button
                      aria-label={`${open.includes(group.id) ? "Thu gọn" : "Mở rộng"} ${group.label}`}
                      onClick={() => toggle(group.id)}
                      className="p-1.5 text-muted-foreground"
                    >
                      {open.includes(group.id) || term ? (
                        <ChevronDown className="size-3.5" />
                      ) : (
                        <ChevronRight className="size-3.5" />
                      )}
                    </button>
                  )}
                </div>
                {showLabels &&
                  (open.includes(group.id) || term) &&
                  group.children?.filter(matches).map((section) => (
                    <div key={section.id} className="mt-1">
                      <div className="flex items-center justify-between">
                        <Link
                          href={section.route}
                          className="min-w-0 flex-1 rounded-lg px-3 py-2 text-xs text-slate-300 transition-colors hover:bg-accent"
                        >
                          {section.label}
                        </Link>
                        {section.children?.length ? (
                          <button
                            aria-label={`${open.includes(section.id) ? "Thu gọn" : "Mở rộng"} ${section.label}`}
                            onClick={() => toggle(section.id)}
                            className="p-2 text-muted-foreground"
                          >
                            {open.includes(section.id) || term ? (
                              <ChevronDown className="size-3" />
                            ) : (
                              <ChevronRight className="size-3" />
                            )}
                          </button>
                        ) : null}
                      </div>
                      {(open.includes(section.id) || term) &&
                        section.children?.filter(matches).map((leaf) => {
                          const status =
                            manifest.overrides[leaf.id] ?? leaf.status;
                          return (
                            <Link
                              key={leaf.id}
                              href={leaf.route}
                              aria-current={
                                activePath === leaf.route ? "page" : undefined
                              }
                              className={cn(
                                "relative ml-5 flex min-h-9 items-center gap-2 border-l border-[#29384d] py-2 pl-5 pr-2 text-xs leading-5 text-[#8fa1ba] transition-colors hover:text-foreground",
                                activePath === leaf.route &&
                                  "border-primary bg-primary/[.07] font-semibold text-primary",
                              )}
                            >
                              <span className="flex-1">{leaf.label}</span>
                              {status === "PLANNED" && (
                                <span
                                  title="Đang phát triển"
                                  className="size-1 shrink-0 rounded-full bg-[#52627b]"
                                />
                              )}
                            </Link>
                          );
                        })}
                    </div>
                  ))}
              </section>
            );
          })}
          {term && !manifest.menu.some(matches) && (
            <p className="px-3 text-xs text-muted-foreground">
              Không tìm thấy chức năng.
            </p>
          )}
        </nav>
        <div className="shrink-0 border-t border-border p-4">
          {showLabels && (
            <div className="mb-4 rounded-lg border border-primary/15 bg-primary/[.035] p-3">
              <div className="mb-1 flex items-center gap-2 text-xs font-semibold text-primary">
                <ShieldCheck className="size-3.5" />
                Không gian quản trị
              </div>
              <p className="text-[10px] leading-5 text-muted-foreground">
                Dữ liệu nguồn và trạng thái chức năng được hiển thị độc lập.
              </p>
            </div>
          )}
          <div className="flex items-center gap-3">
            <div className="grid size-8 shrink-0 place-items-center rounded-lg bg-[#23324a] text-xs font-semibold text-primary">
              {user.displayName.slice(0, 1)}
            </div>
            {showLabels && (
              <div className="min-w-0 flex-1">
                <div className="truncate text-xs font-semibold">
                  {user.displayName}
                </div>
                <div className="mt-1 truncate text-[10px] text-muted-foreground">
                  {user.role}
                </div>
              </div>
            )}
            <button
              aria-label="Đăng xuất"
              title="Đăng xuất"
              onClick={() => {
                void onLogout().catch(() =>
                  setLogoutError("Không thể đăng xuất. Hãy thử lại."),
                );
              }}
              className="rounded p-1 text-muted-foreground hover:text-foreground"
            >
              <LogOut className="size-4" />
            </button>
          </div>
          {logoutError && (
            <p role="alert" className="mt-2 text-xs text-red-300">
              {logoutError}
            </p>
          )}
        </div>
      </aside>
      <div
        className={cn(
          "transition-[margin] duration-150",
          compact ? "lg:ml-[76px]" : "lg:ml-[258px]",
        )}
      >
        <header className="sticky top-0 z-20 flex h-16 items-center gap-4 border-b border-border bg-[#111b2bd9] px-5 backdrop-blur-xl lg:px-8">
          <Button
            variant="ghost"
            size="icon"
            aria-label="Mở menu"
            className="lg:hidden"
            onClick={() => setMobile(true)}
          >
            <Menu />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            aria-label="Thu gọn sidebar"
            className="hidden text-muted-foreground lg:inline-flex"
            onClick={() => setCompact(!compact)}
          >
            <PanelLeftClose />
          </Button>
          <div className="hidden min-w-0 items-center gap-2 text-xs text-muted-foreground md:flex">
            <Link href="/admin/game/heroes">Admin</Link>
            {breadcrumb.map((item, index) => (
              <span key={item.id} className="flex items-center gap-2">
                <ChevronRight className="size-3" />
                <Link
                  className={
                    index === breadcrumb.length - 1
                      ? "max-w-48 truncate text-foreground"
                      : ""
                  }
                  href={item.route}
                >
                  {item.label}
                </Link>
              </span>
            ))}
            {pathname.includes("/detail/") && (
              <>
                <ChevronRight className="size-3" />
                <span>Hồ sơ tướng</span>
              </>
            )}
          </div>
          <form
            className="ml-auto hidden w-[250px] items-center rounded-lg border border-border bg-[#0e1725] px-3 xl:flex"
            onSubmit={(event) => {
              event.preventDefault();
              router.push(
                "/admin/game/heroes?q=" + encodeURIComponent(globalSearch),
              );
            }}
          >
            <Search className="size-3.5 shrink-0 text-muted-foreground" />
            <input
              id="admin-global-search"
              aria-label="Tìm tướng toàn cục"
              placeholder="Tìm tướng, ID..."
              value={globalSearch}
              onChange={(event) => setGlobalSearch(event.target.value)}
              className="h-8 min-w-0 flex-1 bg-transparent px-2 text-xs outline-none"
            />
            <kbd className="text-[9px] text-muted-foreground">Ctrl K</kbd>
          </form>
          <span className="badge badge-green ml-auto xl:ml-0">
            <span className="size-1.5 rounded-full bg-emerald-400" />
            LOCAL
          </span>
          <span className="hidden text-xs text-muted-foreground sm:block">
            Chỉ xem
          </span>
          <Button
            variant="ghost"
            size="icon"
            aria-label="Hướng dẫn"
            onClick={() => planned("Trung tâm hướng dẫn")}
            className="text-muted-foreground"
          >
            <CircleHelp />
          </Button>
        </header>
        <main className="mx-auto max-w-[1880px] px-5 py-7 lg:px-8">
          {children}
          <footer className="mt-10 flex flex-wrap items-center justify-between gap-3 border-t border-border pt-5 text-[10px] text-[#70839f]">
            <span>Arena3Q Admin · Phiên bản 0.1</span>
            <span className="flex items-center gap-1.5">
              <ShieldCheck className="size-3" />
              Truy cập có phân quyền · Dữ liệu Unity local
            </span>
          </footer>
        </main>
      </div>
    </div>
  );
}
