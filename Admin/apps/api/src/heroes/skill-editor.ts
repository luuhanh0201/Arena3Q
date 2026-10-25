import {
  Body,
  ConflictException,
  Controller,
  ForbiddenException,
  Get,
  Inject,
  Injectable,
  NotFoundException,
  Param,
  Post,
  Put,
  Req,
  BadRequestException,
} from "@nestjs/common";
import { promises as fs } from "node:fs";
import path from "node:path";
import { createHash, randomUUID } from "node:crypto";
import { parseDocument } from "yaml";
import {
  editableSkill,
  skillDraftRequestSchema,
  skillApplyRequestSchema,
  type DatabaseState,
  type Skill,
  type SkillEditable,
} from "@arena3q/contracts";
import { ADMIN_STORE, type AdminStore } from "../persistence/store";
import { Permissions, type AdminRequest } from "../auth/auth";
import { config } from "../config";
const hash = (bytes: string | Buffer) =>
  createHash("sha256").update(bytes).digest("hex");
const editorOf = (skill: Skill) =>
  skill.editor ?? {
    revision: 0,
    draft: null,
    updatedAt: null,
    updatedBy: null,
    lastAppliedAt: null,
  };
function findSkill(
  state: DatabaseState,
  heroId: string,
  skillId: string,
): Skill {
  const skill = state.heroes
    .find((hero) => hero.id === heroId)
    ?.sections.skills.find((skill) => skill.id === skillId);
  if (!skill) throw new NotFoundException("Không tìm thấy kỹ năng của tướng.");
  return skill;
}
function checkVersion(
  skill: Skill,
  input: { sourceVersion: string; revision: number },
) {
  if (
    skill.sourceVersion !== input.sourceVersion ||
    editorOf(skill).revision !== input.revision
  )
    throw new ConflictException(
      "Kỹ năng đã thay đổi. Tải lại để tránh ghi đè thay đổi mới.",
    );
}
function checkStructure(skill: Skill, values: SkillEditable) {
  const source = editableSkill(skill.parameters);
  for (const key of ["effects", "passives"] as const) {
    if (
      values[key].length !== source[key].length ||
      values[key].some(
        (value, index) =>
          value.kind !== source[key][index].kind ||
          ("affects" in value &&
            value.affects !== (source[key][index] as any).affects),
      )
    )
      throw new BadRequestException(
        "Giữ nguyên loại và số lượng hiệu ứng; chỉ chỉnh các thông số hiện có.",
      );
  }
  if (
    Number(skill.parameters.slot) !== 0 &&
    Number(skill.parameters.slot) !== 3 &&
    values.cooldown <= 0
  )
    throw new BadRequestException("Kỹ năng tự dùng cần hồi chiêu lớn hơn 0.");
}
export function editUnitySkill(
  text: string,
  skillId: string,
  values: SkillEditable,
): string {
  const lines = text.split(/\r?\n/);
  const start = lines.findIndex((line) => line === "MonoBehaviour:");
  if (start < 0)
    throw new BadRequestException("File không phải SkillDefinition Unity.");
  const document = parseDocument(lines.slice(start).join("\n"), {
    intAsBigInt: true,
    keepSourceTokens: true,
  });
  if (document.errors.length)
    throw new BadRequestException("YAML nguồn không hợp lệ.");
  if (
    String(document.getIn(["MonoBehaviour", "skillId"])) !== skillId ||
    !String(
      document.getIn(["MonoBehaviour", "m_EditorClassIdentifier"]),
    ).endsWith("SkillDefinition")
  )
    throw new ConflictException("SkillId hoặc loại asset nguồn không khớp.");
  for (const [key, value] of Object.entries(values))
    document.setIn(["MonoBehaviour", key], value);
  return (
    lines.slice(0, start).join("\n") +
    "\n" +
    document.toString({ lineWidth: 0 })
  );
}
async function atomicFile(filename: string, bytes: string | Buffer) {
  const temp = filename + "." + randomUUID() + ".tmp";
  const file = await fs.open(temp, "wx", 0o600);
  try {
    await file.writeFile(bytes);
    await file.sync();
  } finally {
    await file.close();
  }
  try {
    await fs.rename(temp, filename);
  } catch (error) {
    await fs.unlink(temp).catch(() => {});
    throw error;
  }
}
@Injectable()
export class SkillEditorService {
  private operations: Promise<unknown> = Promise.resolve();
  constructor(@Inject(ADMIN_STORE) private readonly store: AdminStore) {}
  async onModuleInit() {
    const directory = path.join(config.root, ".local/skill-transactions");
    const entries = await fs
      .readdir(directory)
      .catch((error: NodeJS.ErrnoException) => {
        if (error.code === "ENOENT") return [];
        throw error;
      });
    for (const entry of entries.filter((file) => file.endsWith(".json"))) {
      const journalFile = path.join(directory, entry);
      const journal = JSON.parse(await fs.readFile(journalFile, "utf8"));
      if (journal.state !== "PENDING") continue;
      const database = await this.store.read();
      const committed = database.heroes.some((hero) =>
        hero.sections.skills.some(
          (skill) =>
            skill.id === journal.skillId &&
            skill.sourceVersion === journal.afterVersion,
        ),
      );
      if (!committed) {
        const base = await fs.realpath(
          path.join(config.unityRoot, "Assets/Arena3Q/Content/Skills"),
        );
        const source = await fs.realpath(
          path.resolve(config.unityRoot, journal.sourcePath),
        );
        const relative = path.relative(base, source);
        const backup = path.resolve(config.root, journal.backup);
        const backupRelative = path.relative(
          path.join(config.root, ".local/skill-backups"),
          backup,
        );
        if (
          relative.startsWith("..") ||
          path.isAbsolute(relative) ||
          backupRelative.startsWith("..") ||
          path.isAbsolute(backupRelative)
        )
          throw new Error("Invalid pending Unity skill transaction path");
        const current = hash(await fs.readFile(source));
        if (current === journal.afterVersion) {
          const old = await fs.readFile(backup);
          if (hash(old) !== journal.beforeVersion)
            throw new Error("Skill transaction backup checksum mismatch");
          await atomicFile(source, old);
        } else if (current !== journal.beforeVersion)
          throw new Error(
            "Unity skill changed outside a pending transaction; inspect backup before continuing",
          );
      }
      await atomicFile(
        journalFile,
        JSON.stringify({
          ...journal,
          state: committed ? "COMMITTED" : "ROLLED_BACK",
        }) + "\n",
      );
    }
  }
  private serial<T>(operation: () => Promise<T>): Promise<T> {
    const next = this.operations.then(operation);
    this.operations = next.then(
      () => undefined,
      () => undefined,
    );
    return next;
  }
  async state(heroId: string, skillId: string) {
    const skill = findSkill(await this.store.read(), heroId, skillId);
    return {
      skillId: skill.id,
      sourceVersion: skill.sourceVersion,
      sourceValues: editableSkill(skill.parameters),
      editor: editorOf(skill),
      applyEnabled: config.unityWriteEnabled,
    };
  }
  save(heroId: string, skillId: string, input: unknown, request: AdminRequest) {
    const data = skillDraftRequestSchema.parse(input);
    return this.serial(async () => {
      await this.store.update((state) => {
        const skill = findSkill(state, heroId, skillId);
        checkVersion(skill, data);
        checkStructure(skill, data.values);
        const timestamp = new Date().toISOString();
        const revision = editorOf(skill).revision + 1;
        for (const hero of state.heroes)
          for (const related of hero.sections.skills)
            if (
              related.id === skill.id &&
              related.sourcePath === skill.sourcePath
            )
              related.editor = {
                ...editorOf(related),
                revision,
                draft: data.values,
                updatedAt: timestamp,
                updatedBy: request.user.id,
                baseVersion: skill.sourceVersion,
              };
        state.audit.push({
          id: randomUUID(),
          actorId: request.user.id,
          action: "SKILL_DRAFT_SAVE",
          resourceType: "SKILL",
          resourceId: skillId,
          timestamp,
          result: "SUCCESS",
          requestId: request.requestId,
          metadata: {
            heroId,
            sourceVersion: skill.sourceVersion,
            values: data.values,
          },
        });
      });
      return this.state(heroId, skillId);
    });
  }
  apply(
    heroId: string,
    skillId: string,
    input: unknown,
    request: AdminRequest,
  ) {
    const data = skillApplyRequestSchema.parse(input);
    return this.serial(async () => {
      if (!config.unityWriteEnabled)
        throw new ForbiddenException(
          "Môi trường này chưa bật ghi nguồn Unity local.",
        );
      let filename = "",
        before: Buffer | undefined,
        after: string | undefined,
        journalFile = "",
        journal: any;
      try {
        await this.store.update(async (state) => {
          const skill = findSkill(state, heroId, skillId);
          checkVersion(skill, data);
          const editor = editorOf(skill);
          if (!editor.draft)
            throw new BadRequestException(
              "Lưu bản nháp trước khi áp dụng Unity.",
            );
          checkStructure(skill, editor.draft);
          if (editor.baseVersion && editor.baseVersion !== skill.sourceVersion)
            throw new ConflictException(
              "Bản nháp dựa trên nguồn cũ. Kiểm tra và lưu lại trước khi áp dụng.",
            );
          const base = await fs.realpath(
            path.join(config.unityRoot, "Assets/Arena3Q/Content/Skills"),
          );
          filename = await fs.realpath(
            path.resolve(config.unityRoot, skill.sourcePath),
          );
          const relative = path.relative(base, filename);
          if (
            relative.startsWith("..") ||
            path.isAbsolute(relative) ||
            !filename.endsWith(".asset")
          )
            throw new ForbiddenException(
              "Chỉ được ghi SkillDefinition trong Content/Skills.",
            );
          before = await fs.readFile(filename);
          if (hash(before) !== skill.sourceVersion)
            throw new ConflictException(
              "Nguồn Unity đã đổi ngoài Admin. Nhập lại snapshot trước khi áp dụng.",
            );
          after = editUnitySkill(
            before.toString("utf8"),
            skillId,
            editor.draft,
          );
          const nextVersion = hash(after);
          const timestamp = new Date().toISOString();
          const transactionId = randomUUID();
          const backupDirectory = path.join(
            config.root,
            ".local/skill-backups",
            transactionId,
          );
          await fs.mkdir(backupDirectory, { recursive: true });
          const backup = path.join(backupDirectory, path.basename(filename));
          await fs.writeFile(backup, before, { flag: "wx", mode: 0o600 });
          const journalDirectory = path.join(
            config.root,
            ".local/skill-transactions",
          );
          await fs.mkdir(journalDirectory, { recursive: true });
          journalFile = path.join(journalDirectory, transactionId + ".json");
          journal = {
            state: "PENDING",
            skillId,
            sourcePath: skill.sourcePath,
            beforeVersion: data.sourceVersion,
            afterVersion: nextVersion,
            backup: path.relative(config.root, backup),
            timestamp,
          };
          await atomicFile(journalFile, JSON.stringify(journal) + "\n");
          await atomicFile(filename, after);
          for (const hero of state.heroes) {
            let changed = false;
            for (const related of hero.sections.skills)
              if (
                related.id === skill.id &&
                related.sourcePath === skill.sourcePath
              ) {
                const values = editor.draft;
                related.name = values.displayName;
                related.description = values.description;
                related.cooldownMs = values.cooldown * 1000;
                related.castTimeMs = values.castTime * 1000;
                related.range = values.range;
                related.radius = values.radius;
                related.parameters = { ...related.parameters, ...values };
                related.effects = related.effects.map((effect, index) => ({
                  ...effect,
                  value: values.effects[index].value,
                  duration: values.effects[index].duration,
                }));
                related.passives = values.passives;
                related.sourceVersion = nextVersion;
                related.editor = {
                  revision: editor.revision + 1,
                  draft: null,
                  updatedAt: timestamp,
                  updatedBy: request.user.id,
                  lastAppliedAt: timestamp,
                };
                changed = true;
              }
            if (changed) {
              hero.updatedAt = timestamp;
              hero.updatedBy = request.user.id;
              hero.resourceVersion = hash(hero.resourceVersion + nextVersion);
              hero.sections.versions.push({
                id: randomUUID(),
                kind: "UNITY_SKILL_APPLY",
                at: timestamp,
                actor: request.user.id,
                description: `Áp dụng chỉ số ${skillId} vào nguồn Unity local.`,
                checksum: nextVersion,
              });
            }
          }
          state.audit.push({
            id: randomUUID(),
            actorId: request.user.id,
            action: "SKILL_UNITY_APPLY",
            resourceType: "SKILL",
            resourceId: skillId,
            timestamp,
            result: "SUCCESS",
            requestId: request.requestId,
            metadata: {
              heroId,
              beforeVersion: data.sourceVersion,
              afterVersion: nextVersion,
              values: editor.draft,
              backup: path
                .relative(config.root, backupDirectory)
                .split(path.sep)
                .join("/"),
            },
          });
        });
        if (journalFile)
          await atomicFile(
            journalFile,
            JSON.stringify({ ...journal, state: "COMMITTED" }) + "\n",
          ).catch(() => {
            console.error(
              "Skill transaction committed; journal will be reconciled on next startup.",
            );
          });
      } catch (error) {
        if (filename && before && after) {
          const current = await fs.readFile(filename).catch(() => null);
          if (current && hash(current) === hash(after)) {
            await atomicFile(filename, before);
            if (journalFile)
              await atomicFile(
                journalFile,
                JSON.stringify({ ...journal, state: "ROLLED_BACK" }) + "\n",
              );
          }
        }
        throw error;
      }
      return {
        ...(await this.state(heroId, skillId)),
        applied: true,
        message:
          "Đã cập nhật nguồn Unity. Unity sẽ import lại asset; trận mới và lần build tiếp theo dùng thông số mới.",
      };
    });
  }
}
@Controller("api/admin/v1/heroes/:heroId/skills/:skillId")
export class SkillEditorController {
  constructor(private readonly editor: SkillEditorService) {}
  @Get("editor")
  @Permissions("admin.heroes.read", "admin.skills.read")
  async state(@Param("heroId") hero: string, @Param("skillId") skill: string) {
    return {
      data: await this.editor.state(hero, skill),
      meta: { isDemo: false },
      error: null,
    };
  }
  @Put("draft")
  @Permissions("admin.heroes.edit", "admin.skills.edit")
  async save(
    @Param("heroId") hero: string,
    @Param("skillId") skill: string,
    @Body() input: unknown,
    @Req() request: AdminRequest,
  ) {
    return {
      data: await this.editor.save(hero, skill, input, request),
      meta: { isDemo: false },
      error: null,
    };
  }
  @Post("apply-unity")
  @Permissions("admin.heroes.publish", "admin.skills.edit")
  async apply(
    @Param("heroId") hero: string,
    @Param("skillId") skill: string,
    @Body() input: unknown,
    @Req() request: AdminRequest,
  ) {
    return {
      data: await this.editor.apply(hero, skill, input, request),
      meta: { isDemo: false },
      error: null,
    };
  }
}
