import { z } from "zod";
import menuRegistry from "./menu.json";
export * from "./addressables";

export const featureStatuses = [
  "READY",
  "PARTIAL",
  "READ_ONLY",
  "PLANNED",
  "DISABLED",
] as const;
export type FeatureStatus = (typeof featureStatuses)[number];
export const heroTabs = [
  "overview",
  "media",
  "model",
  "rig",
  "animations",
  "skills",
  "stats",
  "progression",
  "skins",
  "relations",
  "versions",
] as const;
export type HeroTab = (typeof heroTabs)[number];
export const tabLabels: Record<HeroTab, string> =
  menuRegistry.heroDetailTabLabels;
export interface Feature {
  id: string;
  label: string;
  route: string;
  level: number;
  status: FeatureStatus;
  priority: "P0" | "P1" | "P2";
  requiredPermissions?: string[];
  children?: Feature[];
  placeholder?: { title: string; description: string };
}
export const adminMenu = menuRegistry.menu as Feature[];
export function flattenMenu(nodes: Feature[] = adminMenu): Feature[] {
  return nodes.flatMap((node) => [node, ...flattenMenu(node.children ?? [])]);
}
export function featureForPath(path: string): Feature | undefined {
  return [...flattenMenu()].reverse().find((item) => item.route === path);
}
export const featureOverrides: Record<string, FeatureStatus> = {
  "GAME.HEROES.LIST": "READ_ONLY",
  "DASHBOARD.DASHBOARD.GAME": "READ_ONLY",
};
export const statLabels: Record<string, string> = {
  maxHealth: "Sinh lực",
  damage: "Sát thương",
  moveSpeed: "Tốc độ di chuyển",
  attackRange: "Tầm đánh",
  attackInterval: "Chu kỳ đánh",
  reactionRange: "Tầm phản ứng",
  aggroRange: "Tầm phát hiện",
  attackerMemorySeconds: "Ghi nhớ kẻ tấn công",
  damageReduction: "Giảm sát thương",
  critChanceBonus: "Bổ sung chí mạng",
  leadership: "Thống suất",
  rageMax: "Nộ tối đa",
  rageHitsToFill: "Số đòn đầy nộ",
  attackGrowthPerLevel: "Tăng công mỗi cấp",
  healthGrowthPerLevel: "Tăng máu mỗi cấp",
};
export type JsonObject = Record<string, unknown>;
export interface Taxon {
  id: string;
  name: string;
}
export interface Taxonomy {
  factions: Taxon[];
  roles: Taxon[];
  rarities: Taxon[];
  kinds: Taxon[];
}
export interface Source {
  kind: "UNITY_SNAPSHOT" | "EMPTY";
  importedAt: string | null;
  commit: string | null;
  warnings: string[];
}
export interface Coverage {
  portrait: boolean;
  model: boolean;
  rig: boolean;
  animations: number;
  skills: number;
  stats: boolean;
  unityMapping: boolean;
  published: boolean;
}
export interface Asset {
  id: string;
  name: string;
  type: "IMAGE" | "MODEL" | "VIDEO" | "AUDIO" | "OTHER";
  category: string;
  format: string;
  bytes: number | null;
  width: number | null;
  height: number | null;
  checksum: string | null;
  sourcePath: string | null;
  guid: string | null;
  fileId: string | null;
  version: string | null;
  updatedAt: string | null;
  storageKey: string | null;
  thumbnailKey: string | null;
  contentType: string | null;
  available: boolean;
  browserCompatible: boolean;
  metadata: JsonObject;
}
export interface AnimationClip {
  id: string;
  name: string;
  action: string;
  duration: number | null;
  fps: number | null;
  loop: boolean | null;
  frames: number | null;
  sourcePath: string | null;
  rigId: string | null;
  previewAssetId: string | null;
  events: { timeMs: number; name: string }[];
  metadata: JsonObject;
}
export interface Skill {
  id: string;
  name: string;
  description: string | null;
  slot: string;
  cooldownMs: number | null;
  castTimeMs: number | null;
  targeting: string | null;
  shape: string | null;
  range: number | null;
  radius: number | null;
  animationAction: string | null;
  effects: {
    kind: string;
    affects: string | null;
    value: number | null;
    duration: number | null;
  }[];
  passives: JsonObject[];
  timeline: { timeMs: number; name: string }[];
  assetIds: string[];
  sourcePath: string;
  parameters: JsonObject;
}
export interface Stat {
  code: string;
  label: string;
  value: number;
  unit: string;
  source: string;
}
export interface HeroSections {
  assets: string[];
  model: {
    assetId: string | null;
    previewAssetId: string | null;
    metadata: JsonObject;
    sourcePath: string;
  } | null;
  rig: {
    type: string;
    bones: { name: string; parent: string | null }[];
    sockets: string[];
    metadata: JsonObject;
    sourcePath: string;
  } | null;
  animations: AnimationClip[];
  skills: Skill[];
  stats: { base: Stat[]; growth: Stat[]; derived: null; basis: string };
  progression: JsonObject | null;
  skins: JsonObject[];
  relations: {
    type: string;
    id: string;
    label: string;
    metadata?: JsonObject;
  }[];
  versions: {
    id: string;
    kind: string;
    at: string;
    actor: string | null;
    description: string;
    checksum: string | null;
  }[];
}
export interface Hero {
  id: string;
  heroCode: string;
  displayName: string;
  title: string | null;
  description: string | null;
  kind: string;
  factionId: string | null;
  roleIds: string[];
  rarityId: string | null;
  status: "DRAFT" | "IN_REVIEW" | "PUBLISHED" | "ARCHIVED";
  portraitAssetId: string | null;
  configVersion: string | null;
  resourceVersion: string;
  updatedAt: string;
  updatedBy: string | null;
  sourcePath: string;
  sourceGuid: string | null;
  coverage: Coverage;
  sections: HeroSections;
}
export type HeroProfile = Omit<Hero, "sections">;
export type HeroListItem = Pick<
  Hero,
  | "id"
  | "heroCode"
  | "displayName"
  | "kind"
  | "factionId"
  | "roleIds"
  | "rarityId"
  | "status"
  | "portraitAssetId"
  | "updatedAt"
  | "updatedBy"
  | "configVersion"
  | "resourceVersion"
  | "coverage"
>;
export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  passwordHash: string;
  role: string;
  permissions: string[];
  active: boolean;
}
export type CurrentUser = Omit<AdminUser, "passwordHash" | "active">;
export interface Session {
  id: string;
  tokenHash: string;
  userId: string;
  expiresAt: string;
  createdAt: string;
}
export interface Audit {
  id: string;
  actorId: string | null;
  action: string;
  resourceType: string;
  resourceId: string | null;
  timestamp: string;
  result: "SUCCESS" | "DENIED" | "FAILURE";
  requestId: string;
  metadata: JsonObject;
}
export interface DatabaseState {
  schemaVersion: 1;
  revision: number;
  source: Source;
  heroes: Hero[];
  assets: Asset[];
  taxonomy: Taxonomy;
  users: AdminUser[];
  sessions: Session[];
  audit: Audit[];
}
export interface ApiMeta {
  page?: number;
  pageSize?: number;
  total?: number;
  pages?: number;
  isDemo: boolean;
  source?: Source;
  storage?: string;
}
export interface ApiResponse<T> {
  data: T;
  meta: ApiMeta;
  error: null;
}
export interface ApiError {
  data: null;
  meta: null;
  error: {
    code: string;
    message: string;
    requestId: string;
    details?: unknown;
  };
}
export const loginSchema = z.object({
  email: z.email().max(254),
  password: z.string().min(1).max(256),
});
export const heroQuerySchema = z.object({
  page: z.coerce.number().int().min(1).default(1),
  pageSize: z.coerce
    .number()
    .int()
    .refine((v) => [10, 25, 50, 100].includes(v))
    .default(25),
  q: z.string().max(200).default(""),
  faction: z.string().max(100).optional(),
  role: z.string().max(100).optional(),
  rarity: z.string().max(100).optional(),
  kind: z.string().max(100).optional(),
  status: z.enum(["DRAFT", "IN_REVIEW", "PUBLISHED", "ARCHIVED"]).optional(),
  missing: z
    .enum(["portrait", "model", "rig", "animations", "skills", "stats"])
    .optional(),
  sort: z
    .enum(["displayName", "id", "updatedAt", "status"])
    .default("displayName"),
  order: z.enum(["asc", "desc"]).default("asc"),
});
export type HeroQuery = z.infer<typeof heroQuerySchema>;
const object = z.record(z.string(), z.unknown());
const strNull = z.string().nullable();
const numNull = z.number().finite().nullable();
const coverageSchema = z.object({
  portrait: z.boolean(),
  model: z.boolean(),
  rig: z.boolean(),
  animations: z.number().int().nonnegative(),
  skills: z.number().int().nonnegative(),
  stats: z.boolean(),
  unityMapping: z.boolean(),
  published: z.boolean(),
});
const assetSchema = z.object({
  id: z.string(),
  name: z.string(),
  type: z.enum(["IMAGE", "MODEL", "VIDEO", "AUDIO", "OTHER"]),
  category: z.string(),
  format: z.string(),
  bytes: numNull,
  width: numNull,
  height: numNull,
  checksum: strNull,
  sourcePath: strNull,
  guid: strNull,
  fileId: strNull,
  version: strNull,
  updatedAt: strNull,
  storageKey: strNull,
  thumbnailKey: strNull,
  contentType: strNull,
  available: z.boolean(),
  browserCompatible: z.boolean(),
  metadata: object,
});
const clipSchema = z.object({
  id: z.string(),
  name: z.string(),
  action: z.string(),
  duration: numNull,
  fps: numNull,
  loop: z.boolean().nullable(),
  frames: numNull,
  sourcePath: strNull,
  rigId: strNull,
  previewAssetId: strNull,
  events: z.array(z.object({ timeMs: z.number(), name: z.string() })),
  metadata: object,
});
const skillSchema = z.object({
  id: z.string(),
  name: z.string(),
  description: strNull,
  slot: z.string(),
  cooldownMs: numNull,
  castTimeMs: numNull,
  targeting: strNull,
  shape: strNull,
  range: numNull,
  radius: numNull,
  animationAction: strNull,
  effects: z.array(
    z.object({
      kind: z.string(),
      affects: strNull,
      value: numNull,
      duration: numNull,
    }),
  ),
  passives: z.array(object),
  timeline: z.array(z.object({ timeMs: z.number(), name: z.string() })),
  assetIds: z.array(z.string()),
  sourcePath: z.string(),
  parameters: object,
});
const statSchema = z.object({
  code: z.string(),
  label: z.string(),
  value: z.number().finite(),
  unit: z.string(),
  source: z.string(),
});
export const heroSchema = z.object({
  id: z.string().min(1),
  heroCode: z.string(),
  displayName: z.string(),
  title: strNull,
  description: strNull,
  kind: z.string(),
  factionId: strNull,
  roleIds: z.array(z.string()),
  rarityId: strNull,
  status: z.enum(["DRAFT", "IN_REVIEW", "PUBLISHED", "ARCHIVED"]),
  portraitAssetId: strNull,
  configVersion: strNull,
  resourceVersion: z.string(),
  updatedAt: z.string(),
  updatedBy: strNull,
  sourcePath: z.string(),
  sourceGuid: strNull,
  coverage: coverageSchema,
  sections: z.object({
    assets: z.array(z.string()),
    model: z
      .object({
        assetId: strNull,
        previewAssetId: strNull,
        metadata: object,
        sourcePath: z.string(),
      })
      .nullable(),
    rig: z
      .object({
        type: z.string(),
        bones: z.array(z.object({ name: z.string(), parent: strNull })),
        sockets: z.array(z.string()),
        metadata: object,
        sourcePath: z.string(),
      })
      .nullable(),
    animations: z.array(clipSchema),
    skills: z.array(skillSchema),
    stats: z.object({
      base: z.array(statSchema),
      growth: z.array(statSchema),
      derived: z.null(),
      basis: z.string(),
    }),
    progression: object.nullable(),
    skins: z.array(object),
    relations: z.array(
      z.object({
        type: z.string(),
        id: z.string(),
        label: z.string(),
        metadata: object.optional(),
      }),
    ),
    versions: z.array(
      z.object({
        id: z.string(),
        kind: z.string(),
        at: z.string(),
        actor: strNull,
        description: z.string(),
        checksum: strNull,
      }),
    ),
  }),
});
const taxonSchema = z.object({ id: z.string(), name: z.string() });
export const databaseSchema = z.object({
  schemaVersion: z.literal(1),
  revision: z.number().int().nonnegative(),
  source: z.object({
    kind: z.enum(["UNITY_SNAPSHOT", "EMPTY"]),
    importedAt: strNull,
    commit: strNull,
    warnings: z.array(z.string()),
  }),
  heroes: z.array(heroSchema),
  assets: z.array(assetSchema),
  taxonomy: z.object({
    factions: z.array(taxonSchema),
    roles: z.array(taxonSchema),
    rarities: z.array(taxonSchema),
    kinds: z.array(taxonSchema),
  }),
  users: z.array(
    z.object({
      id: z.string(),
      email: z.email(),
      displayName: z.string(),
      passwordHash: z.string(),
      role: z.string(),
      permissions: z.array(z.string()),
      active: z.boolean(),
    }),
  ),
  sessions: z.array(
    z.object({
      id: z.string(),
      tokenHash: z.string(),
      userId: z.string(),
      expiresAt: z.string(),
      createdAt: z.string(),
    }),
  ),
  audit: z.array(
    z.object({
      id: z.string(),
      actorId: strNull,
      action: z.string(),
      resourceType: z.string(),
      resourceId: strNull,
      timestamp: z.string(),
      result: z.enum(["SUCCESS", "DENIED", "FAILURE"]),
      requestId: z.string(),
      metadata: object,
    }),
  ),
});
export function emptyDatabase(): DatabaseState {
  return {
    schemaVersion: 1,
    revision: 0,
    source: { kind: "EMPTY", importedAt: null, commit: null, warnings: [] },
    heroes: [],
    assets: [],
    taxonomy: { factions: [], roles: [], rarities: [], kinds: [] },
    users: [],
    sessions: [],
    audit: [],
  };
}
export function allows(
  user: Pick<CurrentUser, "permissions"> | null | undefined,
  permissions: string[] = [],
): boolean {
  return (
    !!user &&
    (user.permissions.includes("*") ||
      permissions.every((permission) => user.permissions.includes(permission)))
  );
}
export function normalizeSearch(text: string): string {
  return text
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLocaleLowerCase("vi");
}
export function filterHeroes(
  heroes: Hero[],
  query: HeroQuery,
): { rows: HeroListItem[]; total: number } {
  const term = normalizeSearch(query.q.trim());
  const rows = heroes.filter(
    (hero) =>
      (!term ||
        normalizeSearch(
          `${hero.displayName} ${hero.id} ${hero.heroCode}`,
        ).includes(term)) &&
      (!query.faction ||
        (query.faction === "none"
          ? !hero.factionId
          : hero.factionId === query.faction)) &&
      (!query.role || hero.roleIds.includes(query.role)) &&
      (!query.rarity || hero.rarityId === query.rarity) &&
      (!query.kind || hero.kind === query.kind) &&
      (!query.status || hero.status === query.status) &&
      (!query.missing || !hero.coverage[query.missing]),
  );
  rows.sort(
    (a, b) =>
      String(a[query.sort]).localeCompare(String(b[query.sort]), "vi", {
        numeric: true,
      }) * (query.order === "asc" ? 1 : -1) || a.id.localeCompare(b.id),
  );
  return {
    total: rows.length,
    rows: rows
      .slice((query.page - 1) * query.pageSize, query.page * query.pageSize)
      .map(
        ({ sections, sourcePath, sourceGuid, title, description, ...row }) =>
          row,
      ),
  };
}
