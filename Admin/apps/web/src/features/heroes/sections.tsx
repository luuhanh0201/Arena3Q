"use client";
import { useMemo, useState } from "react";
import dynamic from "next/dynamic";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowUpRight,
  Box,
  Check,
  ChevronRight,
  Clock3,
  Code2,
  File,
  Film,
  Focus,
  Gauge,
  GitBranch,
  ImageIcon,
  Layers,
  Maximize2,
  Network,
  Search,
  ShieldCheck,
  Sparkles,
  Swords,
  Target,
  X,
} from "lucide-react";
import {
  type AnimationClip,
  type Asset,
  type HeroProfile,
  type HeroSections,
  type HeroTab,
  type Skill,
  type Stat,
  type Taxonomy,
} from "@arena3q/contracts";
import { api } from "@/lib/api";
import {
  assetUrl,
  cn,
  formatBytes,
  formatDate,
  formatNumber,
} from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog";
import { EmptyState, ErrorState, Loading } from "@/components/states";
import { usePlannedAction } from "@/components/action-dialog";
import { Portrait } from "./shared";
import {
  SkillParameterDetails,
  SkillUsageBadge,
  SkillImage,
} from "./skill-editor";
const ModelViewer = dynamic(() => import("./model-viewer"), {
  ssr: false,
  loading: () => <Loading label="Đang khởi tạo bộ xem 3D..." />,
});
type Setter = (tab: string) => void;
function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <dt className="data-label">{label}</dt>
      <dd className="data-value ml-0">{children ?? "Chưa khai báo"}</dd>
    </div>
  );
}
function Panel({
  title,
  description,
  children,
  className,
}: {
  title: string;
  description?: string;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <section className={cn("panel overflow-hidden", className)}>
      <div className="border-b border-border px-5 py-4">
        <h2>{title}</h2>
        {description && (
          <p className="mt-1 text-xs leading-5 text-muted-foreground">
            {description}
          </p>
        )}
      </div>
      <div className="p-5">{children}</div>
    </section>
  );
}
export function JsonDetails({
  data,
  label = "Xem thông số nguồn",
}: {
  data: unknown;
  label?: string;
}) {
  return (
    <details className="mt-5 overflow-hidden rounded-lg border border-border">
      <summary className="cursor-pointer px-4 py-3 text-xs text-muted-foreground">
        {label}
      </summary>
      <pre className="max-h-96 overflow-auto whitespace-pre-wrap border-t border-border bg-background p-4 text-[11px] leading-6 text-slate-300">
        {JSON.stringify(data, null, 2)}
      </pre>
    </details>
  );
}
export function HeroOverview({
  hero,
  setTab,
}: {
  hero: HeroProfile;
  setTab: Setter;
}) {
  const stats = useQuery({
    queryKey: ["hero-section", hero.id, "stats"],
    queryFn: () =>
      api<HeroSections["stats"]>(
        `/heroes/${encodeURIComponent(hero.id)}/stats`,
      ),
  });
  const taxonomy = useQuery({
    queryKey: ["taxonomy"],
    queryFn: () => api<Taxonomy>("/hero-taxonomy"),
  });
  const getStat = (code: string) =>
    stats.data?.data.base.find((item) => item.code === code)?.value;
  const coverage = [
    {
      title: "Hình ảnh",
      value: hero.coverage.portrait ? "Có ảnh đại diện" : "Chưa có ảnh",
      tab: "media",
      ready: hero.coverage.portrait,
      Icon: ImageIcon,
    },
    {
      title: "Model 3D",
      value: hero.coverage.model ? "Có tham chiếu file" : "Chưa gán model",
      tab: "model",
      ready: hero.coverage.model,
      Icon: Box,
    },
    {
      title: "Rig & xương",
      value: hero.coverage.rig ? "Có metadata rig" : "Chưa có rig 3D",
      tab: "rig",
      ready: hero.coverage.rig,
      Icon: Network,
    },
    {
      title: "Animation",
      value: `${hero.coverage.animations} binding`,
      tab: "animations",
      ready: hero.coverage.animations > 0,
      Icon: Film,
    },
    {
      title: "Kỹ năng",
      value: `${hero.coverage.skills} định nghĩa`,
      tab: "skills",
      ready: hero.coverage.skills > 0,
      Icon: Sparkles,
    },
    {
      title: "Chỉ số",
      value: hero.coverage.stats ? "Có chỉ số gốc" : "Chưa có chỉ số",
      tab: "stats",
      ready: hero.coverage.stats,
      Icon: Gauge,
    },
  ];
  return (
    <div className="grid gap-5 xl:grid-cols-[.8fr_1.4fr]">
      <section className="panel flex flex-col overflow-hidden">
        <div className="relative flex min-h-80 flex-1 items-center justify-center bg-[radial-gradient(ellipse_at_center,#b18b4b12,transparent_70%)] p-8">
          <Portrait
            assetId={hero.portraitAssetId}
            name={hero.displayName}
            className="h-[330px] w-full max-w-72 border-0 bg-transparent! [&_svg]:size-16"
          />
          {!hero.portraitAssetId && (
            <span className="absolute bottom-8 text-xs text-muted-foreground">
              Chưa có ảnh đại diện
            </span>
          )}
        </div>
        <div className="flex items-center justify-between border-t border-border px-5 py-4">
          <span className="text-xs text-muted-foreground">
            Ảnh nguồn, giữ nguyên tỷ lệ
          </span>
          <Button variant="ghost" size="sm" onClick={() => setTab("media")}>
            Xem gallery
            <ArrowUpRight />
          </Button>
        </div>
      </section>
      <div className="space-y-5">
        <Panel
          title="Hồ sơ tướng"
          description="Thông tin định danh từ dữ liệu Unity hiện tại."
        >
          <dl className="data-grid">
            <Field label="Mã định danh">
              <code className="text-primary">{hero.heroCode}</code>
            </Field>
            <Field label="Tên hiển thị">{hero.displayName}</Field>
            <Field label="Phe">
              {hero.factionId
                ? (taxonomy.data?.data.factions.find(
                    (item) => item.id === hero.factionId,
                  )?.name ?? hero.factionId)
                : "Không có phe"}
            </Field>
            <Field label="Vai trò">
              {hero.roleIds.length
                ? hero.roleIds.join(", ")
                : "Chưa khai báo trong nguồn"}
            </Field>
            <Field label="Danh hiệu">{hero.title ?? "Chưa khai báo"}</Field>
            <Field label="Phiên bản cấu hình Admin">
              {hero.configVersion ?? "Chưa có bản phát hành"}
            </Field>
          </dl>
          {hero.description && (
            <p className="mt-5 text-sm leading-7 text-muted-foreground">
              {hero.description}
            </p>
          )}
        </Panel>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          {[
            { label: "Sinh lực", code: "maxHealth" },
            { label: "Sát thương", code: "damage" },
            { label: "Tốc độ", code: "moveSpeed" },
            { label: "Tầm đánh", code: "attackRange" },
          ].map(({ label, code }) => (
            <div key={code} className="panel p-4">
              <span className="text-[11px] text-muted-foreground">{label}</span>
              <div className="mt-3 text-xl font-semibold text-primary">
                {stats.isPending ? "..." : formatNumber(getStat(code))}
              </div>
              <span className="mt-2 block text-[9px] text-muted-foreground">
                Cấp 1 / 1 sao
              </span>
            </div>
          ))}
        </div>
        {stats.error && (
          <ErrorState error={stats.error} retry={() => stats.refetch()} />
        )}
      </div>
      <div className="grid gap-3 sm:grid-cols-2 xl:col-span-2 xl:grid-cols-3">
        {coverage.map(({ title, value, tab, ready, Icon }) => (
          <button
            onClick={() => setTab(tab)}
            className="panel flex items-center gap-4 p-4 text-left transition-colors hover:border-primary/40"
            key={tab}
          >
            <div
              className={cn(
                "grid size-10 place-items-center rounded-lg",
                ready
                  ? "bg-emerald-400/10 text-emerald-400"
                  : "bg-secondary text-muted-foreground",
              )}
            >
              <Icon className="size-4" />
            </div>
            <div className="flex-1">
              <h3 className="text-xs!">{title}</h3>
              <p className="mt-1.5 text-[11px] text-muted-foreground">
                {value}
              </p>
            </div>
            <ChevronRight className="size-4 text-muted-foreground" />
          </button>
        ))}
      </div>
      <Panel title="Nguồn & khả năng truy vết" className="xl:col-span-2">
        <dl className="grid gap-5 md:grid-cols-3">
          <Field label="GUID định nghĩa Unity">
            <code className="text-xs">{hero.sourceGuid ?? "Chưa có"}</code>
          </Field>
          <Field label="Phiên bản snapshot">
            <code className="text-xs">{hero.resourceVersion.slice(0, 16)}</code>
          </Field>
          <Field label="Addressables / Unity build mapping">
            {hero.coverage.unityMapping
              ? "Có mapping"
              : "Chưa kết nối pipeline phát hành"}
          </Field>
        </dl>
      </Panel>
    </div>
  );
}
export function SectionContent({
  tab,
  data,
  hero,
  setTab,
}: {
  tab: HeroTab;
  data: HeroSections[keyof HeroSections] | Asset[] | undefined;
  hero: HeroProfile;
  setTab: Setter;
}) {
  if (tab === "media") return <MediaSection assets={(data ?? []) as Asset[]} />;
  if (tab === "model")
    return <ModelSection model={data as HeroSections["model"]} />;
  if (tab === "rig") return <RigSection rig={data as HeroSections["rig"]} />;
  if (tab === "animations")
    return (
      <AnimationsSection clips={(data ?? []) as AnimationClip[]} hero={hero} />
    );
  if (tab === "skills")
    return (
      <SkillsSection
        skills={(data ?? []) as Skill[]}
        hero={hero}
        setTab={setTab}
      />
    );
  if (tab === "stats")
    return data ? (
      <StatsSection stats={data as HeroSections["stats"]} />
    ) : (
      <EmptyState title="Chưa có dữ liệu chỉ số" />
    );
  if (tab === "progression")
    return <ProgressionSection data={data as HeroSections["progression"]} />;
  if (tab === "skins")
    return (data as unknown[])?.length ? (
      <JsonDetails data={data} label="Danh sách skin đã khai báo" />
    ) : (
      <EmptyState
        title="Chưa có dữ liệu skin"
        description="Nguồn hiện tại chưa gán skin cho tướng này."
      />
    );
  if (tab === "relations")
    return (
      <RelationsSection
        relations={(data ?? []) as HeroSections["relations"]}
        setTab={setTab}
        hero={hero}
      />
    );
  if (tab === "versions")
    return (
      <VersionsSection
        versions={(data ?? []) as HeroSections["versions"]}
        hero={hero}
      />
    );
  return <EmptyState />;
}
function MediaSection({ assets }: { assets: Asset[] }) {
  const images = assets.filter((asset) => asset.type === "IMAGE");
  const [category, setCategory] = useState("");
  const [selected, setSelected] = useState<Asset | null>(null);
  const categories = [...new Set(images.map((asset) => asset.category))];
  const shown = images.filter(
    (asset) => !category || asset.category === category,
  );
  if (!images.length)
    return (
      <EmptyState
        title="Chưa có hình ảnh cho tướng này"
        description="Chưa có tham chiếu ảnh hợp lệ trong dữ liệu nguồn. Gallery vẫn sẵn sàng hiển thị khi nguồn được bổ sung."
      />
    );
  return (
    <>
      <div className="mb-5 flex items-center justify-between">
        <div>
          <h2>Hình ảnh 2D</h2>
          <p className="mt-2 text-xs text-muted-foreground">
            {images.length} tài nguyên được gán. Ảnh được giữ đúng tỷ lệ.
          </p>
        </div>
        <select
          aria-label="Lọc loại ảnh"
          value={category}
          onChange={(event) => setCategory(event.target.value)}
        >
          <option value="">Tất cả loại ảnh</option>
          {categories.map((category) => (
            <option key={category}>{category}</option>
          ))}
        </select>
      </div>
      <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
        {shown.map((asset) => (
          <article key={asset.id} className="panel overflow-hidden">
            <button
              disabled={!asset.browserCompatible || !asset.available}
              onClick={() => setSelected(asset)}
              className="group relative block h-64 w-full overflow-hidden bg-[#0d1726]"
            >
              {asset.browserCompatible ? (
                <img
                  src={assetUrl(asset.id)}
                  alt={asset.name}
                  loading="lazy"
                  className="absolute inset-0 h-full w-full max-h-full max-w-full object-contain p-6 transition-transform duration-200 group-hover:scale-[1.03]"
                />
              ) : (
                <span className="text-xs text-muted-foreground">
                  Chưa có preview tương thích
                </span>
              )}
              <span className="absolute bottom-3 right-3 grid size-8 place-items-center rounded-lg bg-black/40 opacity-0 group-hover:opacity-100">
                <Maximize2 className="size-3.5" />
              </span>
            </button>
            <div className="space-y-3 border-t border-border p-4">
              <div className="flex items-center justify-between gap-2">
                <span className="truncate text-xs font-semibold">
                  {asset.name}
                </span>
                <span className="badge">{asset.category}</span>
              </div>
              <div className="flex justify-between text-[10px] text-muted-foreground">
                <span>
                  {asset.width && asset.height
                    ? `${asset.width} × ${asset.height} px`
                    : "Chưa có kích thước"}
                </span>
                <span>
                  {asset.format.toUpperCase()} · {formatBytes(asset.bytes)}
                </span>
              </div>
              <p className="source-path line-clamp-2">{asset.sourcePath}</p>
            </div>
          </article>
        ))}
      </div>
      <ImagePreview asset={selected} close={() => setSelected(null)} />
    </>
  );
}
function ImagePreview({
  asset,
  close,
}: {
  asset: Asset | null;
  close: () => void;
}) {
  const [zoom, setZoom] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [drag, setDrag] = useState<{
    x: number;
    y: number;
    startX: number;
    startY: number;
  } | null>(null);
  return (
    <Dialog
      open={!!asset}
      onOpenChange={(open) => {
        if (!open) {
          close();
          setZoom(1);
          setOffset({ x: 0, y: 0 });
        }
      }}
    >
      <DialogContent className="max-w-6xl gap-4 p-5">
        <DialogTitle className="pr-8 text-base font-semibold">
          {asset?.name}
        </DialogTitle>
        <DialogDescription className="sr-only">
          Xem ảnh gốc, phóng to và kéo để di chuyển ảnh.
        </DialogDescription>
        <div className="grid gap-5 lg:grid-cols-[1fr_280px]">
          <div>
            <div
              className="relative grid h-[55vh] touch-none place-items-center overflow-hidden rounded-lg border border-border bg-[#0b1422]"
              tabIndex={0}
              aria-label="Khung phóng to ảnh"
              onPointerDown={(event) => {
                event.currentTarget.setPointerCapture(event.pointerId);
                setDrag({
                  x: event.clientX,
                  y: event.clientY,
                  startX: offset.x,
                  startY: offset.y,
                });
              }}
              onPointerMove={(event) => {
                if (drag)
                  setOffset({
                    x: drag.startX + event.clientX - drag.x,
                    y: drag.startY + event.clientY - drag.y,
                  });
              }}
              onPointerUp={() => setDrag(null)}
              onPointerCancel={() => setDrag(null)}
              onKeyDown={(event) => {
                if (
                  ["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown"].includes(
                    event.key,
                  )
                ) {
                  event.preventDefault();
                  setOffset((current) => ({
                    x:
                      current.x +
                      (event.key === "ArrowLeft"
                        ? -20
                        : event.key === "ArrowRight"
                          ? 20
                          : 0),
                    y:
                      current.y +
                      (event.key === "ArrowUp"
                        ? -20
                        : event.key === "ArrowDown"
                          ? 20
                          : 0),
                  }));
                }
              }}
            >
              {asset && (
                <img
                  draggable={false}
                  src={assetUrl(asset.id)}
                  alt={asset.name}
                  className="pointer-events-none absolute inset-0 h-full w-full select-none object-contain"
                  style={{
                    transform: `translate(${offset.x}px,${offset.y}px) scale(${zoom})`,
                  }}
                />
              )}
            </div>
            <div className="mt-3 flex items-center gap-3">
              <label
                htmlFor="image-zoom"
                className="text-xs text-muted-foreground"
              >
                Phóng to
              </label>
              <input
                id="image-zoom"
                type="range"
                min="0.5"
                max="4"
                step="0.1"
                value={zoom}
                onChange={(event) => setZoom(Number(event.target.value))}
                className="min-w-0 flex-1 accent-amber-300"
              />
              <span className="w-10 text-xs">{Math.round(zoom * 100)}%</span>
              <Button
                size="sm"
                variant="outline"
                onClick={() => {
                  setZoom(1);
                  setOffset({ x: 0, y: 0 });
                }}
              >
                Đặt lại
              </Button>
            </div>
          </div>
          <div className="max-h-[60vh] overflow-auto">
            {asset && <AssetInfo asset={asset} />}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
function AssetInfo({ asset }: { asset: Asset }) {
  return (
    <dl className="space-y-4">
      <Field label="Asset ID">
        <code className="text-[10px]">{asset.id}</code>
      </Field>
      <Field label="Phiên bản nguồn">{asset.version ?? "Chưa có"}</Field>
      <Field label="Định dạng / dung lượng">
        {asset.format.toUpperCase()} / {formatBytes(asset.bytes)}
      </Field>
      <Field label="Kích thước">
        {asset.width && asset.height
          ? `${asset.width} × ${asset.height} px`
          : "Chưa có"}
      </Field>
      <Field label="Cập nhật">{formatDate(asset.updatedAt)}</Field>
      <Field label="Checksum nguồn (SHA-256)">
        <code className="break-all text-[10px]">
          {asset.checksum ?? "Chưa có"}
        </code>
      </Field>
      <Field label="Nguồn">
        <code className="source-path">{asset.sourcePath ?? "Chưa có"}</code>
      </Field>
      <JsonDetails data={asset.metadata} label="Metadata tài nguyên" />
    </dl>
  );
}
function ModelSection({ model }: { model: HeroSections["model"] }) {
  const asset = useQuery({
    queryKey: ["asset", model?.assetId],
    queryFn: () => api<Asset>("/assets/" + encodeURIComponent(model!.assetId!)),
    enabled: !!model?.assetId,
  });
  if (!model)
    return (
      <EmptyState
        title="Chưa có tài nguyên model được gán"
        description="Chưa có tham chiếu model từ định nghĩa tướng. Tên thư mục hoặc tên file không được dùng để tự suy luận một model đã gán."
      />
    );
  return (
    <div className="grid gap-5 xl:grid-cols-[1.4fr_.8fr]">
      <section className="panel min-h-[450px] overflow-hidden">
        {model.previewAssetId ? (
          <ModelViewer assetId={model.previewAssetId} />
        ) : (
          <div className="p-6">
            <EmptyState
              title="Chưa có preview GLB tương thích"
              description="Nguồn Unity hiện có metadata, nhưng chưa gán file GLB cho trình duyệt. File prefab, FBX hoặc blend không được giả làm GLB."
            />
          </div>
        )}
      </section>
      <Panel title="Model & ánh xạ Unity">
        {asset.isPending ? (
          <Loading />
        ) : asset.error ? (
          <ErrorState error={asset.error} retry={() => asset.refetch()} />
        ) : asset.data ? (
          <AssetInfo asset={asset.data.data} />
        ) : (
          <p className="text-sm text-muted-foreground">Chưa gán asset.</p>
        )}
        <JsonDetails data={model.metadata} label="Thông số model nguồn" />
      </Panel>
    </div>
  );
}
function RigSection({ rig }: { rig: HeroSections["rig"] }) {
  if (!rig)
    return (
      <EmptyState
        title="Chưa có dữ liệu rig"
        description="Chưa trích xuất metadata skeleton, xương hoặc socket từ nguồn được gán cho tướng này."
      />
    );
  return (
    <div className="grid gap-5 lg:grid-cols-2">
      <Panel
        title="Rig & skeleton"
        description="Không suy luận rig đã qua QA chỉ từ sự tồn tại của file."
      >
        <dl className="data-grid">
          <Field label="Loại dữ liệu">
            {rig.type === "SOCKET_METADATA" ? "Socket metadata 2D" : rig.type}
          </Field>
          <Field label="Số xương">
            {rig.bones.length || "Chưa có dữ liệu"}
          </Field>
          <Field label="Số socket">{rig.sockets.length}</Field>
          <Field label="Kiểm tra Unity">
            Chưa có kết quả compatibility được xác nhận
          </Field>
        </dl>
        <JsonDetails data={rig.metadata} />
        <p className="source-path mt-4">{rig.sourcePath}</p>
      </Panel>
      <Panel title="Cấu trúc & socket">
        {rig.bones.length ? (
          <ul className="space-y-2">
            {rig.bones.map((bone) => (
              <li className="text-xs" key={bone.name}>
                <code>{bone.name}</code>
                <span className="ml-3 text-muted-foreground">
                  {bone.parent ?? "Root"}
                </span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="mb-5 text-sm text-muted-foreground">
            Chưa có danh sách xương 3D.
          </p>
        )}
        {rig.sockets.length > 0 && (
          <>
            <h3 className="mb-3 text-xs! text-primary">Socket nguồn</h3>
            <div className="flex flex-wrap gap-2">
              {rig.sockets.map((socket) => (
                <span key={socket} className="badge">
                  <Network className="size-3" />
                  {socket}
                </span>
              ))}
            </div>
          </>
        )}
      </Panel>
    </div>
  );
}
function AnimationsSection({
  clips,
  hero,
}: {
  clips: AnimationClip[];
  hero: HeroProfile;
}) {
  const search = useSearchParams();
  const router = useRouter();
  const [term, setTerm] = useState("");
  const selected =
    clips.find((clip) => clip.id === search.get("clipId")) ??
    clips.find((clip) => clip.action === "Idle") ??
    clips[0] ??
    null;
  const shown = clips.filter((clip) =>
    (clip.name + " " + clip.action).toLowerCase().includes(term.toLowerCase()),
  );
  const select = (clip: AnimationClip) => {
    const params = new URLSearchParams(search);
    params.set("clipId", clip.id);
    router.push(
      `/admin/game/heroes/detail/${encodeURIComponent(hero.id)}?${params}`,
      { scroll: false },
    );
  };
  if (!clips.length)
    return (
      <EmptyState
        title="Chưa có animation được gán"
        description="Khi có binding animation trong nguồn Unity, danh sách clip sẽ xuất hiện tại đây kể cả khi chưa có video hoặc GLB preview."
      />
    );
  return (
    <>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2>Thư viện animation</h2>
          <p className="mt-2 text-xs text-muted-foreground">
            {clips.length} logical binding từ nguồn Unity. Trạng thái QA chưa
            được tự suy luận.
          </p>
        </div>
        <Input
          className="max-w-64"
          aria-label="Tìm animation"
          placeholder="Tìm tên hoặc action..."
          value={term}
          onChange={(event) => setTerm(event.target.value)}
        />
      </div>
      <div className="grid gap-5 xl:grid-cols-[1.25fr_1fr]">
        <div className="panel table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Action / clip</th>
                <th>Thời lượng</th>
                <th>FPS</th>
                <th>Loop</th>
                <th>Preview</th>
              </tr>
            </thead>
            <tbody>
              {shown.map((clip) => (
                <tr
                  className={cn(
                    "cursor-pointer",
                    selected?.id === clip.id && "bg-primary/[.06]",
                  )}
                  key={clip.id}
                  onClick={() => select(clip)}
                >
                  <td>
                    <button
                      className="text-xs font-semibold text-primary"
                      onClick={() => select(clip)}
                    >
                      {clip.name}
                    </button>
                    <code className="mt-1 block max-w-56 truncate text-[9px] text-muted-foreground">
                      {clip.id}
                    </code>
                  </td>
                  <td>{formatNumber(clip.duration)} s</td>
                  <td>{formatNumber(clip.fps)}</td>
                  <td>
                    {clip.loop == null ? "Chưa có" : clip.loop ? "Có" : "Không"}
                  </td>
                  <td>
                    <span className="badge">
                      {clip.previewAssetId ? "Có preview" : "Chưa có preview"}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!shown.length && <EmptyState title="Không có clip phù hợp" />}
        </div>
        <Panel title={selected ? selected.name : "Chi tiết animation"}>
          {selected ? (
            <>
              <dl className="data-grid">
                <Field label="Action">{selected.action}</Field>
                <Field label="Số frame / hướng">
                  {selected.frames ?? "Chưa có"}
                </Field>
                <Field label="Thời lượng">
                  {formatNumber(selected.duration)} s
                </Field>
                <Field label="Rig tương thích">
                  {selected.rigId ?? "Chưa khai báo"}
                </Field>
              </dl>
              <div className="mt-5">
                {selected.previewAssetId ? (
                  <AssetPreview
                    assetId={selected.previewAssetId}
                    clipName={selected.name}
                  />
                ) : (
                  <EmptyState
                    title="Chưa có tài nguyên preview"
                    description="Metadata clip vẫn được hiển thị. Cần gán video hoặc clip GLB tương ứng để phát xem trước."
                  />
                )}
              </div>
              <Timeline events={selected.events} />
              <p className="source-path mt-4">{selected.sourcePath}</p>
              <JsonDetails data={selected.metadata} />
            </>
          ) : (
            <EmptyState
              title="Chọn một animation"
              description="Nhấn tên clip để xem metadata và timeline trong cùng hồ sơ tướng."
            />
          )}
        </Panel>
      </div>
    </>
  );
}
function AssetPreview({
  assetId,
  clipName,
}: {
  assetId: string;
  clipName?: string;
}) {
  const asset = useQuery({
    queryKey: ["asset", assetId],
    queryFn: () => api<Asset>("/assets/" + encodeURIComponent(assetId)),
  });
  if (asset.isPending) return <Loading />;
  if (asset.error)
    return <ErrorState error={asset.error} retry={() => asset.refetch()} />;
  const record = asset.data?.data;
  if (!record?.browserCompatible)
    return <EmptyState title="Chưa có preview tương thích" />;
  if (record.format === "glb")
    return <ModelViewer assetId={assetId} initialClip={clipName} />;
  if (record.type === "VIDEO")
    return (
      <video
        controls
        preload="metadata"
        src={assetUrl(assetId)}
        className="max-h-80 w-full rounded-lg"
      />
    );
  if (record.type === "AUDIO")
    return (
      <audio
        controls
        preload="metadata"
        src={assetUrl(assetId)}
        className="w-full"
      />
    );
  if (record.type === "IMAGE")
    return (
      <img
        src={assetUrl(assetId)}
        alt={record.name}
        className="max-h-72 w-full object-contain"
      />
    );
  return <EmptyState title="Định dạng này chỉ có metadata" />;
}
function SkillsSection({
  skills,
  hero,
  setTab,
}: {
  skills: Skill[];
  hero: HeroProfile;
  setTab: Setter;
}) {
  const search = useSearchParams();
  const router = useRouter();
  const [term, setTerm] = useState("");
  const [slot, setSlot] = useState("");
  const [usage, setUsage] = useState("");
  const [asset, setAsset] = useState<string | null>(null);
  const [effect, setEffect] = useState<Skill["effects"][number] | null>(null);
  const selected =
    skills.find((skill) => skill.id === search.get("skillId")) ??
    skills[0] ??
    null;
  const shown = skills.filter(
    (skill) =>
      (skill.name + " " + skill.id)
        .toLocaleLowerCase("vi")
        .includes(term.toLocaleLowerCase("vi")) &&
      (!slot || skill.slot === slot) &&
      (!usage || skill.usage?.status === usage),
  );
  function select(id: string) {
    const params = new URLSearchParams(search);
    params.set("skillId", id);
    router.push(
      `/admin/game/heroes/detail/${encodeURIComponent(hero.id)}?${params}`,
      { scroll: false },
    );
  }
  if (!skills.length)
    return (
      <EmptyState
        title="Chưa có định nghĩa kỹ năng"
        description="Nguồn hiện tại chưa có kỹ năng được gán cho tướng này."
      />
    );
  const slotLabel = (value: string) =>
    ({
      Passive: "Nội tại",
      Skill2: "Kỹ năng 2",
      Skill3: "Kỹ năng 3",
      Ultimate: "Tuyệt kỹ",
      ANIMATION_BINDING: "Binding animation",
    })[value] ?? value;
  return (
    <>
      <div className="mb-5">
        <h2>Kỹ năng & hiệu ứng</h2>
        <p className="mt-2 text-xs text-muted-foreground">
          Định nghĩa skill, animation binding và timeline trong cùng hồ sơ
          tướng.
        </p>
      </div>
      <div className="grid gap-5 lg:grid-cols-[280px_1fr]">
        <aside className="panel self-start overflow-hidden">
          <div className="space-y-3 border-b border-border p-4">
            <Input
              aria-label="Tìm kỹ năng"
              placeholder="Tìm tên hoặc ID kỹ năng..."
              value={term}
              onChange={(event) => setTerm(event.target.value)}
            />
            <select
              aria-label="Lọc loại kỹ năng"
              className="w-full"
              value={slot}
              onChange={(event) => setSlot(event.target.value)}
            >
              <option value="">Tất cả loại</option>
              {[...new Set(skills.map((skill) => skill.slot))].map((slot) => (
                <option key={slot} value={slot}>
                  {slotLabel(slot)}
                </option>
              ))}
            </select>
            <select
              aria-label="Lọc kỹ năng đang dùng"
              className="w-full"
              value={usage}
              onChange={(event) => setUsage(event.target.value)}
            >
              <option value="">Tất cả trạng thái</option>
              <option value="EQUIPPED">Đang dùng trong bộ kỹ năng</option>
              <option value="ANIMATION_ONLY">Chỉ gán animation</option>
              <option value="UNUSED">Chưa gán trong game</option>
            </select>
          </div>
          <div className="space-y-1 p-2">
            {shown.map((skill, index) => (
              <button
                key={skill.id}
                onClick={() => select(skill.id)}
                className={cn(
                  "flex w-full items-start gap-3 rounded-lg border border-transparent p-3 text-left transition-colors hover:bg-accent",
                  selected?.id === skill.id &&
                    "border-primary/25 bg-primary/[.065]",
                )}
              >
                <SkillImage skill={skill} />
                <span className="min-w-0">
                  <span className="block text-xs font-semibold leading-5">
                    {skill.name}
                  </span>
                  <span className="mt-1 block text-[10px] text-muted-foreground">
                    {slotLabel(skill.slot)}
                  </span>
                  <span className="mt-1 block">
                    <SkillUsageBadge skill={skill} />
                  </span>
                </span>
              </button>
            ))}
            {!shown.length && (
              <p className="p-4 text-xs text-muted-foreground">
                Không có kỹ năng phù hợp.
              </p>
            )}
          </div>
        </aside>
        {selected && (
          <Panel
            title={selected.name}
            description={selected.description ?? "Chưa có mô tả kỹ năng."}
          >
            <div className="mb-5 flex flex-wrap items-center gap-2">
              <span className="badge badge-amber">
                {slotLabel(selected.slot)}
              </span>
              <code className="text-[10px] text-muted-foreground">
                {selected.id}
              </code>
            </div>
            <dl className="data-grid">
              <Field label="Hồi chiêu">
                {formatNumber(
                  selected.cooldownMs == null
                    ? null
                    : selected.cooldownMs / 1000,
                )}{" "}
                s
              </Field>
              <Field label="Vận chiêu">
                {formatNumber(
                  selected.castTimeMs == null
                    ? null
                    : selected.castTimeMs / 1000,
                )}{" "}
                s
              </Field>
              <Field label="Cách chọn mục tiêu">
                {selected.targeting ?? "Chưa khai báo"}
              </Field>
              <Field label="Vùng tác động">
                {selected.shape ?? "Chưa khai báo"}
              </Field>
              <Field label="Tầm sử dụng">{formatNumber(selected.range)}</Field>
              <Field label="Bán kính">{formatNumber(selected.radius)}</Field>
            </dl>
            <SkillParameterDetails skill={selected} />
            <div className="mt-6 border-t border-border pt-5">
              <h3 className="mb-3">Hiệu ứng chiến đấu</h3>
              {selected.effects.length ? (
                <div className="table-wrap">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Hiệu ứng</th>
                        <th>Mục tiêu</th>
                        <th>Giá trị nguồn</th>
                        <th>Thời gian</th>
                      </tr>
                    </thead>
                    <tbody>
                      {selected.effects.map((entry, index) => (
                        <tr key={index}>
                          <td>
                            <button
                              className="text-xs text-primary"
                              onClick={() => setEffect(entry)}
                            >
                              {entry.kind}
                            </button>
                          </td>
                          <td>{entry.affects ?? "Chưa có"}</td>
                          <td>{formatNumber(entry.value)}</td>
                          <td>{formatNumber(entry.duration)} s</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p className="text-xs text-muted-foreground">
                  Không có effect chủ động trong định nghĩa này.
                </p>
              )}
              {selected.passives.length > 0 && (
                <JsonDetails
                  data={selected.passives}
                  label="Thông số nội tại nguồn"
                />
              )}
            </div>
            <div className="mt-6 border-t border-border pt-5">
              <h3 className="mb-3">Animation & tài nguyên</h3>
              <div className="flex flex-wrap gap-2">
                {selected.animationAction ? (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      const params = new URLSearchParams(search);
                      params.set("tab", "animations");
                      params.set(
                        "clipId",
                        `${hero.id}_${selected.animationAction}`,
                      );
                      router.push(
                        `/admin/game/heroes/detail/${encodeURIComponent(hero.id)}?${params}`,
                        { scroll: false },
                      );
                    }}
                  >
                    <Film />
                    {selected.animationAction}
                    <ArrowUpRight />
                  </Button>
                ) : (
                  <span className="text-xs text-muted-foreground">
                    Chưa khai báo animation binding.
                  </span>
                )}
                {selected.assetIds.map((id) => (
                  <Button
                    variant="outline"
                    size="sm"
                    key={id}
                    onClick={() => setAsset(id)}
                  >
                    <Layers />
                    Asset {id.slice(0, 8)}
                  </Button>
                ))}
              </div>
            </div>
            <Timeline events={selected.timeline} />
            <JsonDetails
              data={selected.parameters}
              label="Targeting, điều kiện, hệ số và cấu hình gốc"
            />
            <p className="source-path mt-4">{selected.sourcePath}</p>
          </Panel>
        )}
      </div>
      <Dialog
        open={!!effect}
        onOpenChange={(open) => {
          if (!open) setEffect(null);
        }}
      >
        <DialogContent>
          <DialogTitle className="text-lg font-semibold">
            Hiệu ứng: {effect?.kind}
          </DialogTitle>
          <DialogDescription className="text-xs leading-6 text-muted-foreground">
            Dữ liệu effect trong định nghĩa kỹ năng đang xem. Các quy tắc
            stacking hoặc dispel không được tự thêm nếu nguồn chưa khai báo.
          </DialogDescription>
          <dl className="data-grid">
            <Field label="Mục tiêu">{effect?.affects}</Field>
            <Field label="Giá trị">{formatNumber(effect?.value)}</Field>
            <Field label="Thời gian">{formatNumber(effect?.duration)} s</Field>
          </dl>
        </DialogContent>
      </Dialog>
      <Dialog
        open={!!asset}
        onOpenChange={(open) => {
          if (!open) setAsset(null);
        }}
      >
        <DialogContent className="max-w-3xl">
          <DialogTitle className="text-lg font-semibold">
            Tài nguyên kỹ năng
          </DialogTitle>
          <DialogDescription className="sr-only">
            Metadata và preview của asset gắn với kỹ năng hiện tại.
          </DialogDescription>
          {asset && <AssetDrawer id={asset} />}
        </DialogContent>
      </Dialog>
    </>
  );
}
function AssetDrawer({ id }: { id: string }) {
  const asset = useQuery({
    queryKey: ["asset", id],
    queryFn: () => api<Asset>("/assets/" + encodeURIComponent(id)),
  });
  if (asset.isPending) return <Loading />;
  if (asset.error)
    return <ErrorState error={asset.error} retry={() => asset.refetch()} />;
  return asset.data ? (
    <div className="max-h-[70vh] overflow-auto">
      <h3>{asset.data.data.name}</h3>
      <div className="my-4">
        {asset.data.data.browserCompatible ? (
          <AssetPreview assetId={id} />
        ) : (
          <p className="text-xs text-muted-foreground">
            Định dạng Unity native hiện có metadata; chưa có preview tương thích
            trình duyệt.
          </p>
        )}
      </div>
      <AssetInfo asset={asset.data.data} />
    </div>
  ) : null;
}
function Timeline({ events }: { events: { name: string; timeMs: number }[] }) {
  return (
    <div className="mt-6 border-t border-border pt-5">
      <h3 className="mb-3 flex items-center gap-2">
        <Clock3 className="size-3.5 text-primary" />
        Event timeline
      </h3>
      {events.length ? (
        <ol className="space-y-3 border-l border-border pl-4">
          {events.map((event, index) => (
            <li key={index} className="flex gap-4 text-xs">
              <code className="min-w-20 text-primary">
                {formatNumber(event.timeMs)} ms
              </code>
              <span>{event.name}</span>
            </li>
          ))}
        </ol>
      ) : (
        <p className="text-xs text-muted-foreground">
          Chưa có event timeline được khai báo.
        </p>
      )}
    </div>
  );
}
function StatsSection({ stats }: { stats: HeroSections["stats"] }) {
  return (
    <Panel title="Chỉ số nguồn" description={stats.basis}>
      <div className="mb-5 flex gap-2">
        <span className="badge badge-amber">Cấp 1</span>
        <span className="badge">1 sao</span>
        <span className="badge badge-blue">Dữ liệu gốc</span>
      </div>
      {stats.base.length ? (
        <table className="data-table">
          <thead>
            <tr>
              <th>Chỉ số</th>
              <th>Mã</th>
              <th>Giá trị gốc</th>
              <th>Đơn vị</th>
            </tr>
          </thead>
          <tbody>
            {stats.base.map((stat) => (
              <tr key={stat.code}>
                <td>{stat.label}</td>
                <td>
                  <code className="text-[11px] text-muted-foreground">
                    {stat.code}
                  </code>
                </td>
                <td className="font-semibold text-primary">
                  {formatNumber(stat.value)}
                </td>
                <td className="text-xs text-muted-foreground">
                  {stat.unit || "Giá trị"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <EmptyState title="Chưa có chỉ số gốc" />
      )}
      <div className="mt-7">
        <h3 className="mb-4">Tăng trưởng khai báo</h3>
        {stats.growth.length ? (
          <div className="grid gap-4 sm:grid-cols-2">
            {stats.growth.map((stat) => (
              <div
                key={stat.code}
                className="rounded-lg border border-border bg-background/40 p-4"
              >
                <p className="text-xs text-muted-foreground">{stat.label}</p>
                <strong className="mt-2 block text-xl text-primary">
                  {formatNumber(stat.value)}{" "}
                  <span className="text-xs font-normal text-muted-foreground">
                    {stat.unit}
                  </span>
                </strong>
                <code className="mt-2 block text-[10px] text-muted-foreground">
                  {stat.code}
                </code>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-xs text-muted-foreground">
            Chưa có cấu hình tăng trưởng.
          </p>
        )}
      </div>
      <p className="mt-6 rounded-lg border border-primary/20 bg-primary/[.03] p-4 text-xs leading-6 text-muted-foreground">
        Chưa kết nối bộ tính combat dùng chung. Chỉ số suy ra, power score và mô
        phỏng theo cấp/sao chưa được tính trong Admin.
      </p>
    </Panel>
  );
}
function ProgressionSection({ data }: { data: HeroSections["progression"] }) {
  if (!data) return <EmptyState title="Chưa có dữ liệu phát triển" />;
  const stars = Array.isArray(data.stars)
    ? (data.stars as Record<string, unknown>[])
    : [];
  return (
    <Panel
      title="Quy tắc phát triển"
      description="Cấu hình nguồn được dùng chung; không phải ngoại lệ tự tạo cho từng tướng."
    >
      <dl className="data-grid">
        <Field label="Cấp tối đa">{String(data.maxLevel ?? "Chưa có")}</Field>
        <Field label="Cấp kỹ năng tối đa">
          {String(data.maxSkillLevel ?? "Chưa có")}
        </Field>
        <Field label="Nhận điểm kỹ năng từ cấp">
          {String(data.skillPointsFromLevel ?? "Chưa có")}
        </Field>
        <Field label="Loại tướng">{String(data.heroKind ?? "Chưa có")}</Field>
      </dl>
      {stars.length > 0 && (
        <div className="table-wrap mt-6">
          <table className="data-table">
            <thead>
              <tr>
                <th>Sao</th>
                <th>Bonus chỉ số</th>
                <th>Số kỹ năng mở</th>
                <th>Cấp kỹ năng</th>
              </tr>
            </thead>
            <tbody>
              {stars.map((row, index) => (
                <tr key={index}>
                  <td>{String(row.stars ?? "Chưa có")}</td>
                  <td>{String(row.statBonus ?? "Chưa có")}</td>
                  <td>{String(row.unlockedSkillCount ?? "Chưa có")}</td>
                  <td>{String(row.skillLevel ?? "Chưa có")}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <JsonDetails
        data={data}
        label="Cấu hình progression nguồn và đường dẫn"
      />
    </Panel>
  );
}
function RelationsSection({
  relations,
  hero,
  setTab,
}: {
  relations: HeroSections["relations"];
  hero: HeroProfile;
  setTab: Setter;
}) {
  return relations.length ? (
    <Panel
      title="Liên kết dữ liệu"
      description="Chỉ hiển thị các quan hệ có trong snapshot nguồn."
    >
      <div className="grid gap-3 md:grid-cols-2">
        {relations.map((relation) => (
          <div
            className="flex items-center gap-3 rounded-lg border border-border p-4"
            key={relation.type + relation.id}
          >
            <div className="grid size-9 place-items-center rounded-lg bg-secondary text-primary">
              {relation.type === "SKILL" ? (
                <Sparkles className="size-4" />
              ) : (
                <GitBranch className="size-4" />
              )}
            </div>
            <div className="min-w-0 flex-1">
              <p className="text-xs font-semibold">{relation.label}</p>
              <code className="mt-1 block truncate text-[10px] text-muted-foreground">
                {relation.type} / {relation.id}
              </code>
            </div>
            {relation.type === "SKILL" ? (
              <Button asChild size="icon" variant="ghost">
                <Link
                  aria-label={`Xem kỹ năng ${relation.label}`}
                  href={`/admin/game/heroes/detail/${encodeURIComponent(hero.id)}?tab=skills&skillId=${encodeURIComponent(relation.id)}`}
                >
                  <ArrowUpRight />
                </Link>
              </Button>
            ) : relation.type === "FACTION" ? (
              <Button asChild size="icon" variant="ghost">
                <Link
                  aria-label={`Lọc phe ${relation.label}`}
                  href={
                    "/admin/game/heroes?faction=" +
                    encodeURIComponent(relation.id)
                  }
                >
                  <ArrowUpRight />
                </Link>
              </Button>
            ) : null}
          </div>
        ))}
      </div>
    </Panel>
  ) : (
    <EmptyState title="Chưa có quan hệ được khai báo" />
  );
}
function VersionsSection({
  versions,
  hero,
}: {
  versions: HeroSections["versions"];
  hero: HeroProfile;
}) {
  const planned = usePlannedAction();
  return (
    <Panel
      title="Lịch sử snapshot"
      description="Các lần nhập dữ liệu nguồn; tách biệt với phiên bản cấu hình game đã publish."
    >
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <span className="badge badge-amber">Chưa có bản phát hành Admin</span>
        <Button
          variant="outline"
          size="sm"
          onClick={() => planned("Publish / rollback cấu hình")}
        >
          <GitBranch />
          Publish / rollback
        </Button>
      </div>
      {versions.length ? (
        <ol className="space-y-5 border-l border-border pl-5">
          {[...versions].reverse().map((version) => (
            <li
              key={version.id}
              className="relative rounded-lg border border-border p-4"
            >
              <div className="absolute -left-[25px] top-5 size-2 rounded-full bg-primary" />
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="badge badge-blue">{version.kind}</span>
                <span className="text-[10px] text-muted-foreground">
                  {formatDate(version.at)}
                </span>
              </div>
              <p className="mt-3 text-xs leading-6">{version.description}</p>
              <code className="mt-2 block break-all text-[10px] text-muted-foreground">
                {version.checksum ?? "Chưa có checksum"}
              </code>
            </li>
          ))}
        </ol>
      ) : (
        <EmptyState title="Chưa có dữ liệu lịch sử" />
      )}
      <p className="source-path mt-5">
        Snapshot hiện tại: {hero.resourceVersion}
      </p>
    </Panel>
  );
}
