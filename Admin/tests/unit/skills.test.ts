import { beforeAll, beforeEach, afterEach, afterAll, it, expect } from "vitest";
import { createRequire } from "node:module";
import { promises as fs } from "node:fs";
import path from "node:path";
import os from "node:os";
import { createHash } from "node:crypto";
import { parse } from "yaml";
import {
  heroSchema,
  editableSkill,
  type DatabaseState,
  type Skill,
} from "@arena3q/contracts";
const require = createRequire(import.meta.url);
const { createApp } = require("../../apps/api/dist/app");
const { JsonFileStore } = require("../../apps/api/dist/persistence/json-store");
const { hashPassword } = require("../../apps/api/dist/auth/password");
const { config } = require("../../apps/api/dist/config");
const {
  SkillEditorService,
} = require("../../apps/api/dist/heroes/skill-editor");
const digest = (text: string) =>
  createHash("sha256").update(text).digest("hex");
const sourcePath =
  "Assets/Arena3Q/Content/Skills/LuBu/LuBu_1_PhuongThienTram.asset";
let temporary: string,
  source: string,
  filename: string,
  store: any,
  app: any,
  base: string,
  ownerCookie: string,
  readerCookie: string,
  passwordHash: string,
  originalRoot: string,
  originalUnity: string;
const request = (
  route: string,
  method = "GET",
  data?: unknown,
  cookie = ownerCookie,
) =>
  fetch(
    base +
      "/api/admin/v1/heroes/fixture/skills/lubu_phuong_thien_tram/" +
      route,
    {
      method,
      headers: { "Content-Type": "application/json", Cookie: cookie },
      ...(data ? { body: JSON.stringify(data) } : {}),
    },
  );
beforeAll(async () => {
  temporary = await fs.mkdtemp(path.join(os.tmpdir(), "arena3q-skills-test-"));
  source = await fs.readFile(path.resolve("..", sourcePath), "utf8");
  passwordHash = await hashPassword("test-only-password-123");
  originalRoot = config.root;
  originalUnity = config.unityRoot;
});
beforeEach(async () => {
  const directory = await fs.mkdtemp(path.join(temporary, "case-"));
  config.root = path.join(directory, "Admin");
  config.unityRoot = directory;
  filename = path.join(directory, sourcePath);
  await fs.mkdir(path.dirname(filename), { recursive: true });
  await fs.writeFile(filename, source);
  const parameters = parse(
    source.slice(source.indexOf("MonoBehaviour:")),
  ).MonoBehaviour;
  const skill: Skill = {
    id: parameters.skillId,
    name: parameters.displayName,
    description: parameters.description,
    slot: "Skill2",
    cooldownMs: parameters.cooldown * 1000,
    castTimeMs: parameters.castTime * 1000,
    targeting: "SectorFacing",
    shape: "Sector",
    range: parameters.range,
    radius: parameters.radius,
    animationAction: null,
    effects: parameters.effects.map((effect: any) => ({
      kind: String(effect.kind),
      affects: String(effect.affects),
      value: effect.value,
      duration: effect.duration,
    })),
    passives: [],
    timeline: [],
    assetIds: [],
    sourcePath,
    sourceVersion: digest(source),
    parameters,
  };
  store = new JsonFileStore(path.join(config.root, ".local/db.json"));
  await store.update((state: DatabaseState) => {
    state.heroes = [
      heroSchema.parse({
        id: "fixture",
        heroCode: "fixture",
        displayName: "Fixture",
        title: null,
        description: null,
        kind: "Starter",
        factionId: null,
        roleIds: [],
        rarityId: null,
        status: "DRAFT",
        portraitAssetId: null,
        configVersion: null,
        resourceVersion: "test",
        updatedAt: new Date().toISOString(),
        updatedBy: null,
        sourcePath: "fixture",
        sourceGuid: null,
        coverage: {
          portrait: false,
          model: false,
          rig: false,
          animations: 0,
          skills: 1,
          stats: false,
          unityMapping: false,
          published: false,
        },
        sections: {
          assets: [],
          model: null,
          rig: null,
          animations: [],
          skills: [skill],
          stats: { base: [], growth: [], derived: null, basis: "fixture" },
          progression: null,
          skins: [],
          relations: [],
          versions: [],
        },
      }),
    ];
    for (const [id, permissions] of [
      ["owner", ["*"]],
      ["reader", ["admin.heroes.read", "admin.skills.read"]],
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
    const response = await fetch(base + "/api/admin/v1/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        email: id + "@test.local",
        password: "test-only-password-123",
      }),
    });
    const cookie = response.headers.get("set-cookie")!.split(";")[0];
    if (id === "owner") ownerCookie = cookie;
    else readerCookie = cookie;
  }
});
afterEach(async () => {
  await app.close();
  config.root = originalRoot;
  config.unityRoot = originalUnity;
});
afterAll(async () => {
  if (
    path.dirname(temporary) !== os.tmpdir() ||
    !path.basename(temporary).startsWith("arena3q-skills-test-")
  )
    throw new Error("Unexpected test cleanup path");
  await fs.rm(temporary, { recursive: true, force: true });
});
async function draft() {
  const state = await (await request("editor")).json();
  const values = {
    ...state.data.sourceValues,
    cooldown: 9,
    description: "Fixture updated description",
  };
  values.effects[0].value = 2;
  const input = {
    sourceVersion: state.data.sourceVersion,
    revision: state.data.editor.revision,
    values,
  };
  return { input, values, response: await request("draft", "PUT", input) };
}
it("authorizes edits independently and rejects invalid numbers", async () => {
  const editor = await (await request("editor")).json();
  expect(
    (
      await request(
        "draft",
        "PUT",
        {
          sourceVersion: editor.data.sourceVersion,
          revision: 0,
          values: editor.data.sourceValues,
        },
        readerCookie,
      )
    ).status,
  ).toBe(403);
  const invalid = { ...editor.data.sourceValues, cooldown: -1 };
  expect(
    (
      await request("draft", "PUT", {
        sourceVersion: editor.data.sourceVersion,
        revision: 0,
        values: invalid,
      })
    ).status,
  ).toBe(400);
  expect(await fs.readFile(filename, "utf8")).toBe(source);
});
it("persists drafts without changing Unity and detects stale revisions", async () => {
  const { input, response } = await draft();
  expect(response.status).toBe(200);
  expect(await fs.readFile(filename, "utf8")).toBe(source);
  expect(
    (await store.read()).heroes[0].sections.skills[0].editor.draft.cooldown,
  ).toBe(9);
  const conflict = await request("draft", "PUT", input);
  expect(conflict.status).toBe(409);
  expect((await conflict.json()).error.code).toBe('CONFLICT');
});
it("applies editable values into a real Unity YAML asset with backup, audit and unchanged references", async () => {
  const { response } = await draft();
  const state = (await response.json()).data;
  const applied = await request("apply-unity", "POST", {
    sourceVersion: state.sourceVersion,
    revision: state.editor.revision,
  });
  expect(applied.ok).toBe(true);
  const text = await fs.readFile(filename, "utf8");
  const old = parse(
    source.slice(source.indexOf("MonoBehaviour:")),
  ).MonoBehaviour;
  const next = parse(text.slice(text.indexOf("MonoBehaviour:"))).MonoBehaviour;
  expect(next.cooldown).toBe(9);
  expect(next.effects[0].value).toBe(2);
  expect(next.description).toBe("Fixture updated description");
  expect(next.m_Script).toEqual(old.m_Script);
  expect(next.skillId).toBe(old.skillId);
  expect(next.icon).toEqual(old.icon);
  const database = await store.read();
  expect(database.heroes[0].sections.skills[0].editor.draft).toBeNull();
  expect(
    database.audit.some((event: any) => event.action === "SKILL_UNITY_APPLY"),
  ).toBe(true);
  const folders = await fs.readdir(
    path.join(config.root, ".local/skill-backups"),
  );
  expect(
    await fs.readFile(
      path.join(
        config.root,
        ".local/skill-backups",
        folders[0],
        path.basename(filename),
      ),
      "utf8",
    ),
  ).toBe(source);
  // Isolated output consumed by Unity AdminSkillImportTests; never a game asset.
  await fs.mkdir(path.resolve(".local"), { recursive: true });
  await fs.writeFile(path.resolve(".local/unity-skill-roundtrip.asset"), text);
});
it.each([false, true])(
  "reconciles pending crash journals when database committed=%s",
  async (committed) => {
    const before = await store.read();
    const { response } = await draft();
    const editor = (await response.json()).data;
    expect(
      (
        await request("apply-unity", "POST", {
          sourceVersion: editor.sourceVersion,
          revision: editor.editor.revision,
        })
      ).ok,
    ).toBe(true);
    const directory = path.join(config.root, ".local/skill-transactions");
    const journalFile = path.join(directory, (await fs.readdir(directory))[0]);
    const journal = JSON.parse(await fs.readFile(journalFile, "utf8"));
    await fs.writeFile(
      journalFile,
      JSON.stringify({ ...journal, state: "PENDING" }),
    );
    const recoveryStore = {
      read: async () => (committed ? store.read() : before),
    };
    await new SkillEditorService(recoveryStore).onModuleInit();
    const recovered = JSON.parse(await fs.readFile(journalFile, "utf8"));
    expect(recovered.state).toBe(committed ? "COMMITTED" : "ROLLED_BACK");
    expect(digest(await fs.readFile(filename, "utf8"))).toBe(
      committed ? journal.afterVersion : journal.beforeVersion,
    );
  },
);
it("refuses source conflicts and storage traversal without modifying files", async () => {
  const { response } = await draft();
  const state = (await response.json()).data;
  await fs.appendFile(filename, "\n# externally changed\n");
  const changed = await fs.readFile(filename, "utf8");
  expect(
    (
      await request("apply-unity", "POST", {
        sourceVersion: state.sourceVersion,
        revision: state.editor.revision,
      })
    ).status,
  ).toBe(409);
  expect(await fs.readFile(filename, "utf8")).toBe(changed);
});
it("rolls back the Unity file if database persistence fails", async () => {
  const { response } = await draft();
  const state = (await response.json()).data;
  const originalUpdate = store.update.bind(store);
  store.update = async (change: any) => {
    const snapshot = await store.read();
    await change(snapshot);
    throw new Error("Simulated database write failure");
  };
  try {
    expect(
      (
        await request("apply-unity", "POST", {
          sourceVersion: state.sourceVersion,
          revision: state.editor.revision,
        })
      ).status,
    ).toBe(500);
    expect(await fs.readFile(filename, "utf8")).toBe(source);
  } finally {
    store.update = originalUpdate;
  }
  expect(
    (await store.read()).heroes[0].sections.skills[0].editor.draft,
  ).not.toBeNull();
});
