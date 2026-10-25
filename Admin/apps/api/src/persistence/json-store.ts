import { promises as fs } from "node:fs";
import path from "node:path";
import { randomUUID } from "node:crypto";
import {
  databaseSchema,
  emptyDatabase,
  type DatabaseState,
} from "@arena3q/contracts";
import { protectAudit, type AdminStore } from "./store";

/** One local API process owns this store. Writes are serialized, validated, fsynced and renamed atomically. */
export class JsonFileStore implements AdminStore {
  private queue: Promise<unknown> = Promise.resolve();
  private cached: DatabaseState | null = null;
  private signature = "";
  constructor(readonly filename: string) {}
  private async load(): Promise<DatabaseState> {
    await fs.mkdir(path.dirname(this.filename), { recursive: true });
    let stat;
    try {
      stat = await fs.stat(this.filename);
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code !== "ENOENT") throw error;
      try {
        await fs.writeFile(
          this.filename,
          JSON.stringify(emptyDatabase(), null, 2),
          { flag: "wx", mode: 0o600 },
        );
      } catch (writeError) {
        if ((writeError as NodeJS.ErrnoException).code !== "EEXIST")
          throw writeError;
      }
      stat = await fs.stat(this.filename);
    }
    const signature = `${stat.mtimeMs}:${stat.size}`;
    if (!this.cached || signature !== this.signature) {
      const text = await fs.readFile(this.filename, "utf8");
      // Invalid or corrupt data is an error, never silently replaced with empty data.
      this.cached = databaseSchema.parse(
        JSON.parse(text.replace(/^\uFEFF/, "")),
      );
      this.signature = signature;
    }
    return structuredClone(this.cached);
  }
  async read(): Promise<DatabaseState> {
    await this.queue;
    return this.load();
  }
  update<T>(change: (state: DatabaseState) => T | Promise<T>): Promise<T> {
    const result = this.queue.then(async () => {
      const state = await this.load();
      const before = structuredClone(state);
      const value = await change(state);
      protectAudit(before, state);
      state.revision = before.revision + 1;
      const validated = databaseSchema.parse(state);
      for (const collection of [
        validated.heroes,
        validated.assets,
        validated.users,
        validated.sessions,
        validated.audit,
      ]) {
        if (
          new Set(collection.map((item) => item.id)).size !== collection.length
        )
          throw new Error("Duplicate record ID.");
      }
      const temporary = `${this.filename}.${randomUUID()}.tmp`;
      const handle = await fs.open(temporary, "wx", 0o600);
      try {
        await handle.writeFile(JSON.stringify(validated, null, 2) + "\n");
        await handle.sync();
      } finally {
        await handle.close();
      }
      try {
        await fs.rename(temporary, this.filename);
      } catch (error) {
        await fs.unlink(temporary).catch(() => {});
        throw error;
      }
      this.cached = validated;
      const stat = await fs.stat(this.filename);
      this.signature = `${stat.mtimeMs}:${stat.size}`;
      return value;
    });
    this.queue = result.then(
      () => undefined,
      () => undefined,
    );
    return result;
  }
}
