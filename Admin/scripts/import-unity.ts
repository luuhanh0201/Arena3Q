import { promises as fs, createReadStream } from "node:fs";
import path from "node:path";
import { createHash, randomUUID } from "node:crypto";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { parse } from "yaml";
import sharp from "sharp";
import {
  statLabels,
  type Asset,
  type Hero,
  type AnimationClip,
  type Skill,
  type Stat,
  type DatabaseState,
} from "@arena3q/contracts";
import { JsonFileStore } from "../apps/api/src/persistence/json-store";

const adminRoot = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
);
const root = path.resolve(adminRoot, "..");
const privateRoot = path.join(adminRoot, ".local/storage");
type Yaml = Record<string, any>; // Unity YAML is untyped at the import boundary; output is checked by databaseSchema.
const numberOrNull = (value: unknown): number | null =>
  typeof value === "number" && Number.isFinite(value) ? value : null;
const hash = (value: string | Buffer) =>
  createHash("sha256").update(value).digest("hex");
const posix = (value: string) => value.split(path.sep).join("/");
async function walk(directory: string, suffix: string): Promise<string[]> {
  const entries = await fs.readdir(directory, { withFileTypes: true });
  const found = await Promise.all(
    entries.map((entry) =>
      entry.isDirectory()
        ? walk(path.join(directory, entry.name), suffix)
        : Promise.resolve(
            entry.name.endsWith(suffix)
              ? [path.join(directory, entry.name)]
              : [],
          ),
    ),
  );
  return found.flat();
}
function normalizeIntegers(value: any): any {
  if (typeof value === "bigint")
    return value <= BigInt(Number.MAX_SAFE_INTEGER) &&
      value >= BigInt(Number.MIN_SAFE_INTEGER)
      ? Number(value)
      : String(value);
  if (Array.isArray(value)) return value.map(normalizeIntegers);
  if (value && typeof value === "object")
    return Object.fromEntries(
      Object.entries(value).map(([key, item]) => [
        key,
        normalizeIntegers(item),
      ]),
    );
  return value;
}
async function yamlFile(filename: string): Promise<Yaml> {
  const text = (await fs.readFile(filename, "utf8"))
    .replace(/^%[^\r\n]*\r?\n/gm, "")
    .replace(/^--- !u!\d+ &-?\d+.*$/gm, "---");
  return normalizeIntegers(parse(text, { intAsBigInt: true }));
}
function withoutUnityFields(value: Yaml): Yaml {
  return Object.fromEntries(
    Object.entries(value).filter(([key]) => !key.startsWith("m_")),
  );
}
export async function importUnity(
  store: JsonFileStore,
): Promise<{ heroes: number; assets: number; warnings: string[] }> {
  await fs.mkdir(privateRoot, { recursive: true });
  const metas = await walk(path.join(root, "Assets/Arena3Q"), ".meta");
  const byGuid = new Map<string, string>();
  for (let start = 0; start < metas.length; start += 32)
    await Promise.all(
      metas.slice(start, start + 32).map(async (filename) => {
        const handle = await fs.open(filename, "r");
        const buffer = Buffer.alloc(160);
        const { bytesRead } = await handle.read(buffer, 0, buffer.length, 0);
        await handle.close();
        const guid = buffer
          .subarray(0, bytesRead)
          .toString("utf8")
          .match(/^guid: ([a-f0-9]{32})/m)?.[1];
        if (guid) byGuid.set(guid, filename.slice(0, -5));
      }),
    );
  const enums = new Map<string, Map<number, string>>();
  for (const filename of [
    ...(await walk(path.join(root, "Assets/Arena3Q/Scripts/Core"), ".cs")),
    ...(await walk(path.join(root, "Assets/Arena3Q/Scripts/Data"), ".cs")),
  ]) {
    const code = (await fs.readFile(filename, "utf8"))
      .replace(/\/\*[\s\S]*?\*\//g, "")
      .replace(/\/\/[^\n]*/g, "");
    for (const match of code.matchAll(
      /enum\s+(\w+)(?:\s*:\s*\w+)?\s*\{([^}]+)\}/g,
    )) {
      let next = 0;
      const map = new Map<number, string>();
      for (const part of match[2].split(",")) {
        const item = part.trim().match(/^(\w+)(?:\s*=\s*(\d+))?$/);
        if (!item) continue;
        if (item[2]) next = Number(item[2]);
        map.set(next++, item[1]);
      }
      enums.set(match[1], map);
    }
  }
  const label = (type: string, value: unknown) =>
    typeof value === "number"
      ? (enums.get(type)?.get(value) ?? `${type}:${value}`)
      : null;
  const assets = new Map<string, Asset>();
  const warnings: string[] = [];
  const yamlCache = new Map<string, Yaml>();
  const load = async (filename: string) => {
    if (!yamlCache.has(filename))
      yamlCache.set(filename, await yamlFile(filename));
    return yamlCache.get(filename)!;
  };
  const resolve = (reference: any) =>
    reference?.guid ? byGuid.get(reference.guid) : undefined;
  const sourceRef = (filename: string) => posix(path.relative(root, filename));
  async function asset(
    reference: any,
    category: string,
  ): Promise<Asset | null> {
    if (!reference?.guid || reference.fileID === 0) return null;
    const filename = resolve(reference);
    const id = `${reference.guid}_${reference.fileID ?? "0"}`;
    if (assets.has(id)) return assets.get(id)!;
    const extension = filename
      ? path.extname(filename).slice(1).toLowerCase()
      : "";
    const type: Asset["type"] = ["png", "jpg", "jpeg", "webp", "tga"].includes(
      extension,
    )
      ? "IMAGE"
      : ["fbx", "blend", "glb", "gltf", "prefab"].includes(extension)
        ? "MODEL"
        : ["mp4", "webm"].includes(extension)
          ? "VIDEO"
          : ["wav", "ogg", "mp3"].includes(extension)
            ? "AUDIO"
            : "OTHER";
    const item: Asset = {
      id,
      name: filename ? path.basename(filename) : reference.guid,
      type,
      category,
      format: extension || "unknown",
      bytes: null,
      width: null,
      height: null,
      checksum: null,
      sourcePath: filename ? sourceRef(filename) : null,
      guid: reference.guid,
      fileId: String(reference.fileID ?? 0),
      version: null,
      updatedAt: null,
      storageKey: null,
      thumbnailKey: null,
      contentType: null,
      available: false,
      browserCompatible: false,
      metadata: {},
    };
    assets.set(id, item);
    if (!filename) {
      warnings.push(`Không resolve được GUID ${reference.guid}`);
      return item;
    }
    let stat;
    try {
      stat = await fs.stat(filename);
    } catch {
      warnings.push(`Thiếu file local ${sourceRef(filename)}`);
      return item;
    }
    if (!stat.isFile()) return item;
    item.available = true;
    item.bytes = stat.size;
    item.updatedAt = stat.mtime.toISOString();
    if (type === "IMAGE") {
      try {
        const input = await fs.readFile(filename);
        item.checksum = hash(input);
        item.version = item.checksum.slice(0, 12);
        const meta = await load(filename + ".meta");
        const importer = meta.TextureImporter ?? {};
        const sprite = importer.spriteSheet?.sprites?.find(
          (sprite: any) =>
            String(sprite.internalID) === String(reference.fileID),
        );
        let image = sharp(input);
        const info = await image.metadata();
        if (sprite?.rect && info.height) {
          const r = sprite.rect;
          image = image.extract({
            left: Math.round(r.x),
            top: Math.round(info.height - r.y - r.height),
            width: Math.round(r.width),
            height: Math.round(r.height),
          });
          item.width = r.width;
          item.height = r.height;
        } else {
          item.width = info.width ?? null;
          item.height = info.height ?? null;
          if (importer.spriteMode === 2) {
            item.metadata.spriteResolution = "UNRESOLVED";
            item.browserCompatible = false;
            return item;
          }
        }
        item.storageKey = `unity/${id}/original.png`;
        item.thumbnailKey = `unity/${id}/thumbnail.webp`;
        await fs.mkdir(path.join(privateRoot, "unity", id), {
          recursive: true,
        });
        const pixels = await image.png().toBuffer();
        // Full preview preserves source pixels and alpha. Only list thumbnails are resized.
        await fs.writeFile(path.join(privateRoot, item.storageKey), pixels);
        await sharp(pixels)
          .resize({
            width: 320,
            height: 320,
            fit: "inside",
            withoutEnlargement: true,
          })
          .webp({ lossless: true })
          .toFile(path.join(privateRoot, item.thumbnailKey));
        item.contentType = "image/png";
        item.browserCompatible = true;
        item.metadata = {
          ...item.metadata,
          alpha: info.hasAlpha ?? null,
          colorSpace: info.space ?? null,
          previewFormat: "png",
          quality: "SOURCE_PIXELS_LOSSLESS",
          originalFormat: extension,
        };
      } catch (error) {
        warnings.push(
          `Không tạo preview ${sourceRef(filename)}: ${(error as Error).message}`,
        );
      }
    } else if (
      ["glb", "mp4", "webm", "wav", "ogg", "mp3"].includes(extension)
    ) {
      const bytes = await fs.readFile(filename);
      item.checksum = hash(bytes);
      item.version = item.checksum.slice(0, 12);
      item.storageKey = `unity/${id}/${path.basename(filename)}`;
      await fs.mkdir(path.dirname(path.join(privateRoot, item.storageKey)), {
        recursive: true,
      });
      await fs.copyFile(filename, path.join(privateRoot, item.storageKey));
      item.contentType = (
        {
          glb: "model/gltf-binary",
          mp4: "video/mp4",
          webm: "video/webm",
          wav: "audio/wav",
          ogg: "audio/ogg",
          mp3: "audio/mpeg",
        } as Record<string, string>
      )[extension];
      item.browserCompatible = true;
    }
    if (!item.checksum) {
      const digest = createHash("sha256");
      for await (const chunk of createReadStream(filename))
        digest.update(chunk);
      item.checksum = digest.digest("hex");
      item.version = item.checksum.slice(0, 12);
    }
    return item;
  }
  const content = path.join(root, "Assets/Arena3Q/Content");
  const heroFiles = [
    ...(await walk(path.join(content, "Heroes"), ".asset")),
    ...(await walk(path.join(content, "Lords"), ".asset")),
  ]
    .filter((file) => !file.includes("Preview"))
    .sort();
  const heroes: Hero[] = [];
  const ids = new Set<string>();
  const factionMap = new Map<string, string>();
  const roleMap = new Map<string, string>();
  const progression = withoutUnityFields(
    (await load(path.join(content, "Progression/ProgressionConfig.asset")))
      .MonoBehaviour,
  );
  const importedAt = new Date().toISOString();
  for (const filename of heroFiles) {
    const raw = (await load(filename)).MonoBehaviour;
    if (!raw?.heroId) continue;
    if (ids.has(raw.heroId)) {
      warnings.push(`Bỏ bản trùng ID ${raw.heroId}: ${sourceRef(filename)}`);
      continue;
    }
    ids.add(raw.heroId);
    const heroMeta = await load(filename + ".meta");
    const sourcePath = sourceRef(filename);
    const stats: Stat[] = [];
    for (const [code, value] of Object.entries({
      ...raw.stats,
      damageReduction: raw.damageReduction,
      critChanceBonus: raw.critChanceBonus,
      leadership: raw.leadership,
      rageMax: raw.rageMax,
      rageHitsToFill: raw.rageHitsToFill,
    }))
      if (typeof value === "number")
        stats.push({
          code,
          label: statLabels[code] ?? code,
          value,
          unit:
            code.endsWith("Seconds") || code === "attackInterval"
              ? "s"
              : code === "damageReduction" || code === "critChanceBonus"
                ? "tỷ lệ"
                : "",
          source: sourcePath,
        });
    const growthPath = resolve(raw.growthProfile);
    const growth = growthPath
      ? withoutUnityFields((await load(growthPath)).MonoBehaviour)
      : {};
    // Growth profiles are not hero roles. Leave the unassigned taxonomy empty instead of inventing one.
    const roleIds: string[] = [];
    const factionPath = resolve(raw.faction);
    const faction = factionPath
      ? (await load(factionPath)).MonoBehaviour
      : null;
    const factionId = faction?.factionId ?? null;
    if (factionId) factionMap.set(factionId, faction.displayName ?? factionId);
    const portrait = await asset(
      raw.portrait?.fileID ? raw.portrait : raw.sprite,
      "PORTRAIT",
    );
    const heroAssets: string[] = portrait ? [portrait.id] : [];
    const animationPath =
      resolve(raw.macAnhPerformance) ?? resolve(raw.animations);
    const animation = animationPath
      ? (await load(animationPath)).MonoBehaviour
      : null;
    const skills: Skill[] = [];
    const references = [
      ...(raw.skills ?? []),
      ...(animation?.skills ?? []).map((binding: any) => binding.skill),
    ];
    const equippedGuids = new Set(
      (raw.skills ?? []).map((reference: any) => reference.guid),
    );
    const boundGuids = new Set(
      (animation?.skills ?? []).map((binding: any) => binding.skill?.guid),
    );
    const skillFolders = [
      ...new Set(
        references
          .map(resolve)
          .filter(Boolean)
          .map((file: string) => path.dirname(file)),
      ),
    ];
    for (const folder of skillFolders)
      for (const file of await walk(folder, ".asset")) {
        const source = (await load(file)).MonoBehaviour;
        if (!source?.skillId) continue;
        const metadata = await load(file + ".meta");
        if (
          metadata.guid &&
          !references.some((reference: any) => reference.guid === metadata.guid)
        )
          references.push({ guid: metadata.guid, fileID: 11400000 });
      }
    for (const [index, reference] of references.entries()) {
      const skillPath = resolve(reference);
      if (!skillPath) {
        if (reference?.guid)
          warnings.push(`Thiếu skill ${reference.guid} của ${raw.heroId}`);
        continue;
      }
      const skill = (await load(skillPath)).MonoBehaviour;
      if (skills.some((item) => item.id === skill.skillId)) continue;
      const icon = await asset(skill.icon, "SKILL_ICON");
      if (icon) heroAssets.push(icon.id);
      skills.push({
        sourceVersion: hash(await fs.readFile(skillPath)),
        iconAssetId: icon?.browserCompatible ? icon.id : null,
        usage: {
          status: equippedGuids.has(reference.guid)
            ? "EQUIPPED"
            : boundGuids.has(reference.guid)
              ? "ANIMATION_ONLY"
              : "UNUSED",
          heroSlots: equippedGuids.has(reference.guid)
            ? [label("SkillSlot", skill.slot) ?? String(skill.slot)]
            : [],
          detail: equippedGuids.has(reference.guid)
            ? "Được gán vào bộ bốn kỹ năng HeroDefinition; game dùng khi ô kỹ năng đã mở."
            : boundGuids.has(reference.guid)
              ? "Có animation/VFX binding nhưng chưa được trang bị trong bộ kỹ năng của tướng."
              : "Có asset nguồn cùng thư mục; chưa được gán vào bộ kỹ năng hoặc animation của tướng.",
        },
        id: skill.skillId,
        name: skill.displayName ?? skill.skillId,
        description: skill.description ?? null,
        slot: label("SkillSlot", skill.slot) ?? "UNASSIGNED",
        cooldownMs:
          typeof skill.cooldown === "number" ? skill.cooldown * 1000 : null,
        castTimeMs:
          typeof skill.castTime === "number" ? skill.castTime * 1000 : null,
        targeting: label("SkillTargeting", skill.targeting),
        shape: label("SkillShape", skill.shape),
        range: numberOrNull(skill.range),
        radius: numberOrNull(skill.radius),
        animationAction: null,
        effects: (skill.effects ?? []).map((effect: any) => ({
          kind: label("SkillEffectKind", effect.kind) ?? String(effect.kind),
          affects: label("SkillAffects", effect.affects),
          value: numberOrNull(effect.value),
          duration: numberOrNull(effect.duration),
        })),
        passives: skill.passives ?? [],
        timeline: [],
        assetIds: icon ? [icon.id] : [],
        sourcePath: sourceRef(skillPath),
        parameters: withoutUnityFields(skill),
      });
    }
    const animations: AnimationClip[] = [];
    let model: Hero["sections"]["model"] = null;
    let rig: Hero["sections"]["rig"] = null;
    for (const [index, clip] of (animation?.clips ?? []).entries()) {
      const action =
        label(
          raw.macAnhPerformance?.guid ? "MacAnhAction" : "HeroAnimationState",
          clip.action ?? clip.state,
        ) ?? String(clip.action ?? clip.state ?? clip.name ?? index);
      animations.push({
        id: `${raw.heroId}_${action}`,
        name: clip.name ?? action,
        action,
        duration: numberOrNull(clip.duration),
        fps:
          clip.framesPerDirection && clip.duration
            ? clip.framesPerDirection / clip.duration
            : null,
        loop:
          typeof clip.loop === "number"
            ? !!clip.loop
            : typeof clip.loop === "boolean"
              ? clip.loop
              : null,
        frames: numberOrNull(clip.framesPerDirection),
        sourcePath: animationPath ? sourceRef(animationPath) : null,
        rigId: null,
        previewAssetId: null,
        events: (clip.events ?? []).map((event: any) => ({
          timeMs: (event.frame / 30) * 1000,
          name: event.key,
        })),
        metadata: {
          frameDirections: animation.directionCount ?? null,
          kind: "UNITY_LOGICAL_BINDING",
          qa: "UNKNOWN",
        },
      });
    }
    for (const binding of animation?.skills ?? []) {
      const target = resolve(binding.skill);
      if (!target) continue;
      const source = (await load(target)).MonoBehaviour;
      const skill = skills.find((skill) => skill.id === source.skillId);
      if (skill) {
        skill.animationAction = label("MacAnhAction", binding.action);
        skill.timeline =
          animations.find((clip) => clip.action === skill.animationAction)
            ?.events ?? [];
        const vfx = await asset(binding.vfx, "VFX");
        if (vfx) {
          skill.assetIds.push(vfx.id);
          heroAssets.push(vfx.id);
        }
      }
    }
    const modelPath = resolve(raw.model3D);
    const modelDefinition = modelPath
      ? (await load(modelPath)).MonoBehaviour
      : null;
    let modelReference =
      modelDefinition?.prefab ?? animation?.battle3DViewPrefab;
    if (!modelReference?.guid && animation?.battle3DViewResource) {
      const resources = await walk(
        path.join(root, "Assets/Arena3Q/Art/Heroes"),
        ".prefab",
      );
      const found = resources.find((file) =>
        posix(file).endsWith(
          `/Resources/${animation.battle3DViewResource}.prefab`,
        ),
      );
      if (found) {
        const meta = await load(found + ".meta");
        modelReference = { guid: meta.guid, fileID: "100000" };
      }
    }
    if (modelReference?.guid) {
      const modelAsset = await asset(modelReference, "UNITY_MODEL");
      if (modelAsset) {
        heroAssets.push(modelAsset.id);
        model = {
          assetId: modelAsset.id,
          previewAssetId:
            modelAsset.format === "glb" && modelAsset.browserCompatible
              ? modelAsset.id
              : null,
          metadata: {
            ...modelDefinition,
            enableBattle3DTrial: animation?.enableBattle3DTrial ?? null,
            qa: "UNKNOWN",
            addressablesKey: null,
          },
          sourcePath: modelPath
            ? sourceRef(modelPath)
            : (modelAsset.sourcePath ?? sourcePath),
        };
      }
    }
    if (animation?.clips?.some((clip: any) => clip.sockets?.length)) {
      rig = {
        type: "SOCKET_METADATA",
        bones: [],
        sockets: [
          ...new Set<string>(
            animation.clips.flatMap((clip: any) =>
              clip.sockets?.[0] ? Object.keys(clip.sockets[0]) : [],
            ),
          ),
        ],
        metadata: {
          boneCount: null,
          qa: "UNKNOWN",
          note: "Tọa độ socket nguồn 2D; không phải kết quả kiểm rig 3D.",
        },
        sourcePath: sourceRef(animationPath!),
      };
    }
    const previewFolder = path.join(privateRoot, "web-preview", raw.heroId);
    try {
      const manifest = JSON.parse(
        await fs.readFile(path.join(previewFolder, "manifest.json"), "utf8"),
      );
      const glbFile = path.join(previewFolder, "model.glb");
      const bytes = await fs.readFile(glbFile);
      if (
        manifest.heroId !== raw.heroId ||
        bytes.readUInt32LE(0) !== 0x46546c67
      )
        throw new Error("Invalid GLB manifest");
      const sourceFile = path.resolve(root, manifest.sourcePath);
      if (hash(await fs.readFile(sourceFile)) !== manifest.sourceSha256)
        throw new Error("Preview source changed; export again");
      const id = "web-model-" + raw.heroId;
      assets.set(id, {
        id,
        name: raw.displayName + " — chất lượng nguồn.glb",
        type: "MODEL",
        category: "FULL_QUALITY_WEB_MODEL",
        format: "glb",
        bytes: bytes.length,
        width: null,
        height: null,
        checksum: hash(bytes),
        sourcePath: manifest.sourcePath,
        guid: null,
        fileId: null,
        version: hash(bytes).slice(0, 12),
        updatedAt: (await fs.stat(glbFile)).mtime.toISOString(),
        storageKey: `web-preview/${raw.heroId}/model.glb`,
        thumbnailKey: null,
        contentType: "model/gltf-binary",
        available: true,
        browserCompatible: true,
        metadata: manifest,
      });
      heroAssets.push(id);
      model = {
        assetId: id,
        previewAssetId: id,
        sourcePath: manifest.sourcePath,
        metadata: {
          ...model?.metadata,
          quality: manifest.quality,
          sourceSha256: manifest.sourceSha256,
          sourceBytes: manifest.sourceBytes,
          qa: "UNKNOWN",
          note: manifest.note,
          textureCount: manifest.textures.length,
          clips: manifest.clips,
        },
      };
      rig = {
        type: "SOURCE_SKELETON",
        bones: manifest.bones,
        sockets: rig?.sockets ?? [],
        metadata: { qa: "UNKNOWN", quality: manifest.quality },
        sourcePath: manifest.sourcePath,
      };
      for (const clip of animations)
        if (manifest.clips.includes(clip.action)) {
          clip.previewAssetId = id;
          clip.rigId = id;
          clip.metadata.glbClipName = clip.action;
          clip.metadata.previewSource = manifest.sourcePath;
        }
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code !== "ENOENT")
        warnings.push(`Web preview ${raw.heroId}: ${(error as Error).message}`);
    }
    const hero: Hero = {
      id: raw.heroId,
      heroCode: raw.heroId,
      displayName: raw.displayName ?? raw.heroId,
      title: null,
      description: null,
      kind: label("HeroKind", raw.kind) ?? String(raw.kind),
      factionId,
      roleIds,
      rarityId: null,
      status: "DRAFT",
      portraitAssetId: portrait?.browserCompatible ? portrait.id : null,
      configVersion: null,
      resourceVersion: hash(JSON.stringify(withoutUnityFields(raw))),
      updatedAt: (await fs.stat(filename)).mtime.toISOString(),
      updatedBy: null,
      sourcePath,
      sourceGuid: heroMeta.guid ?? null,
      coverage: {
        portrait: !!portrait?.browserCompatible,
        model: !!model,
        rig: !!rig,
        animations: animations.length,
        skills: skills.length,
        stats: stats.length > 0,
        unityMapping: false,
        published: false,
      },
      sections: {
        assets: [...new Set(heroAssets)],
        model,
        rig,
        animations,
        skills,
        stats: {
          base: stats,
          growth: Object.entries(growth)
            .filter(
              (entry): entry is [string, number] =>
                typeof entry[1] === "number",
            )
            .map(([code, value]) => ({
              code,
              label: statLabels[code] ?? code,
              value,
              unit: "tỷ lệ/cấp",
              source: growthPath ? sourceRef(growthPath) : sourcePath,
            })),
          derived: null,
          basis:
            "Giá trị nguồn Unity ở cấp 1, 1 sao; không mô phỏng công thức.",
        },
        progression: {
          ...progression,
          heroKind: label("HeroKind", raw.kind),
          growthProfile: growth,
          sourcePath:
            "Assets/Arena3Q/Content/Progression/ProgressionConfig.asset",
        },
        skins: [],
        relations: [
          ...(factionId
            ? [
                {
                  type: "FACTION",
                  id: factionId,
                  label: factionMap.get(factionId)!,
                },
              ]
            : []),
          ...skills.map((skill) => ({
            type: "SKILL",
            id: skill.id,
            label: skill.name,
          })),
        ],
        versions: [],
      },
    };
    hero.coverage.rig =
      !!rig && rig.type !== "SOCKET_METADATA" && rig.bones.length > 0;
    hero.resourceVersion = hash(
      JSON.stringify({
        profile: withoutUnityFields(raw),
        sections: hero.sections,
        assets: heroAssets.map((id) => ({
          id,
          checksum: assets.get(id)?.checksum ?? null,
        })),
      }),
    );
    heroes.push(hero);
  }
  for (const factionPath of await walk(
    path.join(content, "Factions"),
    ".asset",
  )) {
    const faction = (await load(factionPath)).MonoBehaviour;
    if (faction?.factionId)
      factionMap.set(
        faction.factionId,
        faction.displayName ?? faction.factionId,
      );
  }
  let commit: string | null = null;
  try {
    commit = execFileSync("git", ["rev-parse", "HEAD"], {
      cwd: root,
      encoding: "utf8",
    }).trim();
  } catch {}
  await store.update((state) => {
    for (const hero of heroes) {
      const before = state.heroes.find((previous) => previous.id === hero.id);
      for (const skill of hero.sections.skills) {
        const previous = before?.sections.skills.find(
          (item) =>
            item.id === skill.id && item.sourcePath === skill.sourcePath,
        );
        if (previous?.editor) skill.editor = previous.editor;
      }
      hero.sections.versions = before?.sections.versions ?? [];
      if (!before || before.resourceVersion !== hero.resourceVersion)
        hero.sections.versions.push({
          id: randomUUID(),
          kind: "UNITY_IMPORT",
          at: importedAt,
          actor: null,
          description:
            "Nhập snapshot đọc-only từ asset Unity local, không phải publish game config.",
          checksum: hero.resourceVersion,
        });
    }
    state.heroes = heroes;
    state.assets = [...assets.values()];
    state.source = { kind: "UNITY_SNAPSHOT", importedAt, commit, warnings };
    state.taxonomy = {
      factions: [...factionMap].map(([id, name]) => ({ id, name })),
      roles: [...roleMap].map(([id, name]) => ({ id, name })),
      rarities: [],
      kinds: [...new Set(heroes.map((hero) => hero.kind))].map((id) => ({
        id,
        name:
          (
            {
              Starter: "Tướng khởi đầu",
              Lord: "Chủ tướng",
              Named: "Danh tướng",
            } as Record<string, string>
          )[id] ?? id,
      })),
    };
    state.audit.push({
      id: randomUUID(),
      actorId: null,
      action: "UNITY_IMPORT",
      resourceType: "SNAPSHOT",
      resourceId: commit,
      timestamp: importedAt,
      result: "SUCCESS",
      requestId: randomUUID(),
      metadata: {
        heroCount: heroes.length,
        assetCount: assets.size,
        warnings: warnings.length,
      },
    });
  });
  return { heroes: heroes.length, assets: assets.size, warnings };
}
if (
  process.argv[1] &&
  path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)
) {
  importUnity(new JsonFileStore(path.join(adminRoot, ".local/db.json")))
    .then((result) => console.log(JSON.stringify(result, null, 2)))
    .catch((error) => {
      console.error(error);
      process.exitCode = 1;
    });
}
