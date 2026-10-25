import { promises as fs, constants } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createHash, randomUUID } from "node:crypto";
import { isDeepStrictEqual } from "node:util";
import { parse } from "yaml";
import sharp from "sharp";

const admin = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const root = path.resolve(admin, "..");
async function main() {
  const registry = JSON.parse(
    await fs.readFile(
      path.join(admin, "reviews/skill-icons-20261025.json"),
      "utf8",
    ),
  );
  const digest = (data: Buffer) =>
    createHash("sha256").update(data).digest("hex");
  const content = (data: Buffer) =>
    parse(data.toString("utf8").slice(data.indexOf("MonoBehaviour:")), {
      intAsBigInt: true,
    }).MonoBehaviour;
  const backupState = path.join(
    admin,
    ".local/approved-skill-icons-backup.json",
  );
  if (
    registry.status !== "APPROVED" ||
    registry.skills.length !== 42 ||
    new Set(registry.skills.map((skill: any) => skill.id)).size !== 42
  )
    throw new Error("A complete user-approved registry is required");
  for (const skill of registry.skills) {
    if (
      !/^[a-z0-9_]+$/.test(skill.id) ||
      skill.texturePath !== `Assets/Arena3Q/Art/SkillIcons/${skill.id}.png` ||
      skill.sourceFile !==
        `NghiemThu/Admin/XemTruoc_SkillIcons_20261025/${skill.id}.png` ||
      !skill.skillPaths.every(
        (source: string) =>
          source.startsWith("Assets/Arena3Q/Content/Skills/") &&
          !source.includes("..") &&
          source.endsWith(".asset"),
      )
    )
      throw new Error("Invalid approved path: " + skill.id);
    const data = await fs.readFile(path.join(root, skill.sourceFile));
    const info = await sharp(data).metadata();
    if (
      digest(data) !== skill.sha256 ||
      data.length !== skill.bytes ||
      info.width !== skill.width ||
      info.height !== skill.height
    )
      throw new Error("Approved artwork changed: " + skill.id);
  }
  const mode = process.argv[2];
  if (mode === "prepare") {
    const db = JSON.parse(
      await fs.readFile(path.join(admin, ".local/db.json"), "utf8"),
    );
    const directory = path.join(
      admin,
      ".local/skill-icon-backups",
      randomUUID(),
    );
    await fs.mkdir(directory, { recursive: true });
    await fs.copyFile(
      path.join(admin, ".local/db.json"),
      path.join(directory, "db.json"),
    );
    const files = [];
    for (const skill of registry.skills) {
      for (const source of skill.skillPaths) {
        const bytes = await fs.readFile(path.join(root, source));
        const snapshot = db.heroes
          .flatMap((hero: any) => hero.sections.skills)
          .find(
            (entry: any) =>
              entry.id === skill.id && entry.sourcePath === source,
          );
        if (
          !snapshot ||
          snapshot.sourceVersion !== digest(bytes) ||
          content(bytes).skillId !== skill.id
        )
          throw new Error("Unity source differs from snapshot: " + skill.id);
        const backup = path.join(directory, skill.id + ".asset");
        await fs.writeFile(backup, bytes, { flag: "wx" });
        files.push({
          id: skill.id,
          source,
          backup: path.relative(admin, backup),
        });
      }
    }
    await fs.writeFile(
      backupState,
      JSON.stringify({ createdAt: new Date().toISOString(), files }, null, 2) +
        "\n",
    );
    for (const skill of registry.skills) {
      const target = path.join(root, skill.texturePath);
      await fs.mkdir(path.dirname(target), { recursive: true });
      try {
        const existing = await fs.readFile(target);
        if (digest(existing) !== skill.sha256)
          throw new Error(
            "Refusing to replace existing different artwork: " + skill.id,
          );
      } catch (error: any) {
        if (error.code !== "ENOENT") throw error;
        await fs.copyFile(
          path.join(root, skill.sourceFile),
          target,
          constants.COPYFILE_EXCL,
        );
      }
    }
    console.log(
      JSON.stringify({
        prepared: registry.skills.length,
        backup: path.relative(admin, directory),
      }),
    );
  } else if (mode === "bind" || mode === "verify") {
    const backup = JSON.parse(await fs.readFile(backupState, "utf8"));
    const report = JSON.parse(
      await fs.readFile(
        path.join(admin, ".local/unity-approved-skill-icons.json"),
        "utf8",
      ),
    );
    if (report.skills.length !== registry.skills.length)
      throw new Error("Incomplete Unity verification report");
    for (const entry of backup.files) {
      const original = await fs.readFile(path.join(admin, entry.backup));
      const before = content(original);
      const filename = path.join(root, entry.source);
      let after = content(await fs.readFile(filename));
      const actual = report.skills.find((item: any) => item.id === entry.id);
      if (
        !actual ||
        !/^[a-f0-9]{32}$/.test(actual.guid) ||
        !/^-?\d+$/.test(actual.fileId)
      )
        throw new Error("Missing actual Unity Sprite identity: " + entry.id);
      if (mode === "bind") {
        // Reject every source edit outside the icon field before binding.
        const normalized = structuredClone(after);
        delete normalized.icon;
        const comparison = { ...before };
        delete comparison.icon;
        if (!isDeepStrictEqual(comparison, normalized))
          throw new Error(
            "Refusing to overwrite external gameplay edits: " + entry.id,
          );
        const text = original.toString("utf8");
        const iconLine = /^  icon: [^\r\n]*$/m;
        if (!iconLine.test(text))
          throw new Error("Expected one icon field: " + entry.id);
        const bound = text.replace(
          iconLine,
          `  icon: {fileID: ${actual.fileId}, guid: ${actual.guid}, type: 3}`,
        );
        const temporary = filename + "." + randomUUID() + ".tmp";
        await fs.writeFile(temporary, bound, { flag: "wx" });
        await fs.rename(temporary, filename);
        after = content(Buffer.from(bound));
      }
      const newIcon = after.icon;
      delete before.icon;
      delete after.icon;
      if (!isDeepStrictEqual(before, after))
        throw new Error("Non-icon gameplay data changed: " + entry.id);
      if (
        !actual ||
        newIcon.guid !== actual.guid ||
        String(newIcon.fileID) !== String(actual.fileId)
      )
        throw new Error("Sprite reference does not match Unity: " + entry.id);
      const approved = registry.skills.find(
        (item: any) => item.id === entry.id,
      );
      if (actual.width !== approved.width || actual.height !== approved.height)
        throw new Error("Unity texture was resized: " + entry.id);
      if (
        digest(await fs.readFile(path.join(root, approved.texturePath))) !==
        approved.sha256
      )
        throw new Error("Unity source pixels file changed: " + entry.id);
    }
    console.log(
      JSON.stringify({
        mode,
        verified: backup.files.length,
        unchangedGameplay: true,
        originalArtwork: true,
      }),
    );
  } else {
    throw new Error(
      "Usage: tsx scripts/apply-approved-skill-icons.ts prepare|bind|verify",
    );
  }
}
main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
