import { afterAll, beforeAll, describe, expect, it } from "vitest";
import { createRequire } from "node:module";
import { promises as fs } from "node:fs";
import os from "node:os";
import path from "node:path";
import {
  emptyDatabase,
  heroSchema,
  filterHeroes,
  heroQuerySchema,
  flattenMenu,
  heroTabs,
  allows,
  type Hero,
  type DatabaseState,
} from "@arena3q/contracts";
const require = createRequire(import.meta.url);
const {
  JsonFileStore,
} = require("../../apps/api/dist/persistence/json-store.js");
const { hashPassword } = require("../../apps/api/dist/auth/password.js");
const { createApp } = require("../../apps/api/dist/app.js");
const { AssetStorage } = require("../../apps/api/dist/assets/assets.js");
const { config } = require("../../apps/api/dist/config.js");
let temporary: string;
beforeAll(async () => {
  temporary = await fs.mkdtemp(path.join(os.tmpdir(), "arena3q-admin-test-"));
});
afterAll(async () => {
  await fs.rm(temporary, { recursive: true, force: true });
});
function hero(id = "fixture-hero", name = "Tướng Đỏ"): Hero {
  return heroSchema.parse({
    id,
    heroCode: id,
    displayName: name,
    title: null,
    description: null,
    kind: "Starter",
    factionId: null,
    roleIds: [],
    rarityId: null,
    status: "DRAFT",
    portraitAssetId: null,
    configVersion: null,
    resourceVersion: "fixture",
    updatedAt: "2026-01-01T00:00:00Z",
    updatedBy: null,
    sourcePath: "test-fixture",
    sourceGuid: null,
    coverage: {
      portrait: false,
      model: false,
      rig: false,
      animations: 0,
      skills: 0,
      stats: false,
      unityMapping: false,
      published: false,
    },
    sections: {
      assets: [],
      model: null,
      rig: null,
      animations: [],
      skills: [],
      stats: { base: [], growth: [], derived: null, basis: "fixture" },
      progression: null,
      skins: [],
      relations: [],
      versions: [],
    },
  });
}
describe("contracts and truthful navigation", () => {
  it("retains 91 leaf routes, six groups, three levels and eleven tabs", () => {
    const all = flattenMenu();
    const leaves = all.filter((item) => !item.children?.length);
    expect(all.filter((item) => item.level === 1)).toHaveLength(6);
    expect(leaves).toHaveLength(91);
    expect(Math.max(...all.map((item) => item.level))).toBe(3);
    expect(new Set(leaves.map((item) => item.route)).size).toBe(leaves.length);
    expect(heroTabs).toHaveLength(11);
  });
  it("filters Vietnamese without accents and never returns detail payloads in list", () => {
    const result = filterHeroes(
      [hero()],
      heroQuerySchema.parse({ q: "tuong do" }),
    );
    expect(result.total).toBe(1);
    expect(result.rows[0]).not.toHaveProperty("sections");
    expect(result.rows[0].rarityId).toBeNull();
  });
  it("paginates, combines filters, whitelists sort and enforces permissions", () => {
    const heroes = Array.from({ length: 12 }, (_, i) => hero("h" + i));
    expect(
      filterHeroes(
        heroes,
        heroQuerySchema.parse({ page: 2, pageSize: 10, missing: "portrait" }),
      ).rows,
    ).toHaveLength(2);
    expect(
      filterHeroes(heroes, heroQuerySchema.parse({ faction: "wrong" })).total,
    ).toBe(0);
    expect(() => heroQuerySchema.parse({ sort: "passwordHash" })).toThrow();
    expect(() => heroQuerySchema.parse({ pageSize: 10000 })).toThrow();
    expect(
      allows({ permissions: ["admin.heroes.read"] }, ["admin.assets.read"]),
    ).toBe(false);
  });
});
describe("local JSON persistence", () => {
  it("serializes concurrent mutations, survives reload and isolates reads", async () => {
    const file = path.join(temporary, "concurrent.json");
    const store = new JsonFileStore(file);
    await Promise.all(
      Array.from({ length: 20 }, () =>
        store.update((state: DatabaseState) => {
          state.source.warnings.push("record");
        }),
      ),
    );
    const state = await new JsonFileStore(file).read();
    expect(state.revision).toBe(20);
    expect(state.source.warnings).toHaveLength(20);
    state.source.warnings = [];
    expect((await store.read()).source.warnings).toHaveLength(20);
  });
  it("rejects corruption without replacing the original file", async () => {
    const file = path.join(temporary, "corrupt.json");
    await fs.writeFile(file, "{broken");
    await expect(new JsonFileStore(file).read()).rejects.toThrow();
    expect(await fs.readFile(file, "utf8")).toBe("{broken");
  });
  it("rejects audit edits and duplicate IDs then permits later valid writes", async () => {
    const store = new JsonFileStore(path.join(temporary, "audit.json"));
    await store.update((state: DatabaseState) => {
      state.audit.push({
        id: "a",
        actorId: null,
        action: "TEST",
        resourceType: "TEST",
        resourceId: null,
        timestamp: new Date().toISOString(),
        result: "SUCCESS",
        requestId: "r",
        metadata: {},
      });
    });
    await expect(
      store.update((state: DatabaseState) => {
        state.audit = [];
      }),
    ).rejects.toThrow();
    await expect(
      store.update((state: DatabaseState) => {
        state.heroes = [hero(), hero()];
      }),
    ).rejects.toThrow();
    await store.update((state: DatabaseState) => {
      state.heroes = [hero()];
    });
    expect((await store.read()).audit).toHaveLength(1);
  });
});
describe("real HTTP auth, read APIs and asset protection", () => {
  let app: any,
    store: any,
    base: string,
    ownerCookie: string,
    readerCookie: string;
  const request = (url: string, options: RequestInit = {}) =>
    fetch(base + url, {
      ...options,
      headers: { "Content-Type": "application/json", ...options.headers },
    });
  beforeAll(async () => {
    store = new JsonFileStore(path.join(temporary, "api.json"));
    const passwordHash = await hashPassword("test-only-password-123");
    await store.update((state: DatabaseState) => {
      state.heroes = [hero()];
      for (const [id, permissions] of [
        ["owner", ["*"]],
        ["reader", ["admin.heroes.read"]],
      ] as const)
        state.users.push({
          id,
          email: id + "@test.local",
          displayName: id,
          passwordHash,
          role: id,
          permissions: [...permissions],
          active: true,
        });
    });
    app = await createApp(store);
    await app.listen(0, "127.0.0.1");
    base = await app.getUrl();
    for (const id of ["owner", "reader"]) {
      const response = await request("/api/admin/v1/auth/login", {
        method: "POST",
        body: JSON.stringify({
          email: id + "@test.local",
          password: "test-only-password-123",
        }),
      });
      expect(response.status).toBe(200);
      const cookie = response.headers.get("set-cookie")!;
      expect(cookie).toContain("HttpOnly");
      expect(cookie).toContain("SameSite=Lax");
      expect(cookie).toContain(`Max-Age=${config.sessionHours * 3600}`);
      if (id === "owner") ownerCookie = cookie.split(";")[0];
      else readerCookie = cookie.split(";")[0];
      expect(await response.text()).not.toContain("passwordHash");
    }
  });
  afterAll(async () => {
    await app?.close();
  });
  it("keeps a fixed 365-day server expiry and synchronizes the browser cookie", async () => {
    expect(config.sessionHours).toBe(365 * 24);
    const session = (await store.read()).sessions.find(
      (item: any) => item.userId === "owner",
    );
    expect(
      Math.abs(
        Date.parse(session.expiresAt) -
          Date.parse(session.createdAt) -
          365 * 24 * 3600000,
      ),
    ).toBeLessThan(1000);
    const response = await request("/api/admin/v1/auth/me", {
      headers: { Cookie: ownerCookie },
    });
    expect(response.status).toBe(200);
    const maxAge = Number(
      response.headers.get("set-cookie")?.match(/Max-Age=(\d+)/)?.[1],
    );
    expect(maxAge).toBeLessThanOrEqual(365 * 24 * 3600);
    expect(maxAge).toBeGreaterThan(365 * 24 * 3600 - 30);
    expect(
      (await store.read()).sessions.find((item: any) => item.id === session.id)
        .expiresAt,
    ).toBe(session.expiresAt);
  });
  it("protects unauthenticated API and media; rejects cross-origin writes", async () => {
    expect((await request("/api/admin/v1/heroes")).status).toBe(401);
    expect((await request("/api/admin/v1/assets/a/content")).status).toBe(401);
    expect(
      (
        await request("/api/admin/v1/auth/logout", {
          method: "POST",
          headers: { Origin: "https://untrusted.test", Cookie: ownerCookie },
          body: "{}",
        })
      ).status,
    ).toBe(403);
  });
  it("rejects wrong password and records denial without secrets", async () => {
    const response = await request("/api/admin/v1/auth/login", {
      method: "POST",
      body: JSON.stringify({
        email: "owner@test.local",
        password: "incorrect",
      }),
    });
    expect(response.status).toBe(401);
    const state = await store.read();
    expect(
      state.audit.some((entry: any) => entry.action === "LOGIN_FAILED"),
    ).toBe(true);
    expect(JSON.stringify(state.audit)).not.toContain("incorrect");
  });
  it("returns paginated real data, validation and distinct 403/404", async () => {
    const headers = { Cookie: ownerCookie };
    const response = await request("/api/admin/v1/heroes?q=tuong%20do", {
      headers,
    });
    const result = await response.json();
    expect(result.meta.total).toBe(1);
    expect(result.meta.isDemo).toBe(false);
    expect(result.data[0]).not.toHaveProperty("sections");
    expect(
      (await request("/api/admin/v1/heroes?page=-1", { headers })).status,
    ).toBe(400);
    expect(
      (await request("/api/admin/v1/heroes/absent", { headers })).status,
    ).toBe(404);
    expect(
      (
        await request("/api/admin/v1/heroes/fixture-hero/media", {
          headers: { Cookie: readerCookie },
        })
      ).status,
    ).toBe(403);
    for (const tab of heroTabs.filter((tab) => tab !== "overview"))
      expect(
        (await request("/api/admin/v1/heroes/fixture-hero/" + tab, { headers }))
          .status,
      ).toBe(200);
  });
  it("supports private byte ranges and blocks path traversal", async () => {
    const old = config.storageRoot;
    config.storageRoot = path.join(temporary, "storage");
    await fs.mkdir(config.storageRoot);
    await fs.writeFile(path.join(config.storageRoot, "test.bin"), "abcdef");
    try {
      await store.update((state: DatabaseState) => {
        state.assets.push({
          id: "file",
          name: "fixture",
          type: "OTHER",
          category: "TEST",
          format: "bin",
          bytes: 6,
          width: null,
          height: null,
          checksum: null,
          sourcePath: null,
          guid: null,
          fileId: null,
          version: null,
          updatedAt: null,
          storageKey: "test.bin",
          thumbnailKey: null,
          contentType: "application/octet-stream",
          available: true,
          browserCompatible: true,
          metadata: {},
        });
      });
      const response = await request("/api/admin/v1/assets/file/content", {
        headers: { Cookie: ownerCookie, Range: "bytes=1-3" },
      });
      expect(response.status).toBe(206);
      expect(await response.text()).toBe("bcd");
      expect(response.headers.get("cache-control")).toContain("private");
      expect(
        (
          await request("/api/admin/v1/assets/file/content", {
            headers: { Cookie: ownerCookie, Range: "bytes=99-100" },
          })
        ).status,
      ).toBe(416);
      await expect(
        new AssetStorage().localPath("../api.json"),
      ).rejects.toThrow();
    } finally {
      config.storageRoot = old;
    }
  });
  it("returns genuine server errors and invalidates sessions at logout", async () => {
    const original = store.read.bind(store);
    store.read = async () => {
      throw new Error("private filesystem detail");
    };
    const failed = await request("/api/admin/v1/heroes", {
      headers: { Cookie: ownerCookie },
    });
    expect(failed.status).toBe(500);
    expect(await failed.text()).not.toContain("private filesystem detail");
    store.read = original;
    expect(
      (
        await request("/api/admin/v1/auth/logout", {
          method: "POST",
          headers: { Cookie: ownerCookie },
          body: "{}",
        })
      ).status,
    ).toBe(200);
    expect(
      (
        await request("/api/admin/v1/auth/me", {
          headers: { Cookie: ownerCookie },
        })
      ).status,
    ).toBe(401);
  });
});
