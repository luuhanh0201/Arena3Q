import "reflect-metadata";
import {
  Column,
  DataSource,
  Entity,
  PrimaryColumn,
  type EntityManager,
} from "typeorm";
import {
  databaseSchema,
  emptyDatabase,
  type DatabaseState,
} from "@arena3q/contracts";
import { protectAudit, type AdminStore } from "./store";

@Entity("admin_heroes")
export class HeroEntity {
  @PrimaryColumn("text") id!: string;
  @Column("jsonb") payload!: DatabaseState["heroes"][number];
}
@Entity("admin_assets")
export class AssetEntity {
  @PrimaryColumn("text") id!: string;
  @Column("jsonb") payload!: DatabaseState["assets"][number];
}
@Entity("admin_users")
export class UserEntity {
  @PrimaryColumn("text") id!: string;
  @Column("jsonb") payload!: DatabaseState["users"][number];
}
@Entity("admin_sessions")
export class SessionEntity {
  @PrimaryColumn("text") id!: string;
  @Column("jsonb") payload!: DatabaseState["sessions"][number];
}
@Entity("admin_audit_logs")
export class AuditEntity {
  @PrimaryColumn("text") id!: string;
  @Column("jsonb") payload!: DatabaseState["audit"][number];
}
@Entity("admin_state")
export class StateEntity {
  @PrimaryColumn("text") id!: string;
  @Column("jsonb") payload!: Pick<
    DatabaseState,
    "schemaVersion" | "revision" | "source" | "taxonomy"
  >;
}

/** Snapshot aggregates retain the shared read model. Relational query tuning can evolve behind AdminStore. */
export function postgresDataSource(url: string): DataSource {
  return new DataSource({
    type: "postgres",
    url,
    entities: [
      HeroEntity,
      AssetEntity,
      UserEntity,
      SessionEntity,
      AuditEntity,
      StateEntity,
    ],
    migrations: [InitialAdminSchema1761264000000],
    synchronize: false,
    logging: false,
  });
}
export class InitialAdminSchema1761264000000 {
  name = "InitialAdminSchema1761264000000";
  async up(runner: import("typeorm").QueryRunner) {
    for (const table of [
      "admin_heroes",
      "admin_assets",
      "admin_users",
      "admin_sessions",
      "admin_audit_logs",
      "admin_state",
    ])
      await runner.query(
        `CREATE TABLE "${table}" ("id" text PRIMARY KEY, "payload" jsonb NOT NULL)`,
      );
    await runner.query(
      `CREATE UNIQUE INDEX "admin_users_email" ON "admin_users" (lower(payload->>'email'))`,
    );
    await runner.query(
      `CREATE UNIQUE INDEX "admin_sessions_token" ON "admin_sessions" ((payload->>'tokenHash'))`,
    );
    await runner.query(
      `CREATE INDEX "admin_heroes_faction" ON "admin_heroes" ((payload->>'factionId'))`,
    );
    await runner.query(
      `CREATE INDEX "admin_audit_timestamp" ON "admin_audit_logs" ((payload->>'timestamp'))`,
    );
    await runner.query(
      `CREATE FUNCTION admin_audit_immutable() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Audit log is append-only'; END; $$`,
    );
    await runner.query(
      `CREATE TRIGGER admin_audit_no_update BEFORE UPDATE OR DELETE ON admin_audit_logs FOR EACH ROW EXECUTE FUNCTION admin_audit_immutable()`,
    );
  }
  async down(runner: import("typeorm").QueryRunner) {
    await runner.query("DROP FUNCTION admin_audit_immutable() CASCADE");
    for (const table of [
      "admin_state",
      "admin_audit_logs",
      "admin_sessions",
      "admin_users",
      "admin_assets",
      "admin_heroes",
    ])
      await runner.query(`DROP TABLE "${table}"`);
  }
}
export class PostgresStore implements AdminStore {
  constructor(readonly source: DataSource) {}
  private async load(manager: EntityManager): Promise<DatabaseState> {
    const [state, heroes, assets, users, sessions, audit] = await Promise.all([
      manager.findOneBy(StateEntity, { id: "main" }),
      manager.find(HeroEntity),
      manager.find(AssetEntity),
      manager.find(UserEntity),
      manager.find(SessionEntity),
      manager.find(AuditEntity),
    ]);
    const base = emptyDatabase();
    return databaseSchema.parse({
      ...base,
      ...state?.payload,
      heroes: heroes.map((row) => row.payload),
      assets: assets.map((row) => row.payload),
      users: users.map((row) => row.payload),
      sessions: sessions.map((row) => row.payload),
      audit: audit
        .map((row) => row.payload)
        .sort(
          (a, b) =>
            a.timestamp.localeCompare(b.timestamp) || a.id.localeCompare(b.id),
        ),
    });
  }
  async read() {
    return this.source.transaction("REPEATABLE READ", (manager) =>
      this.load(manager),
    );
  }
  async update<T>(
    change: (state: DatabaseState) => T | Promise<T>,
  ): Promise<T> {
    return this.source.transaction(async (manager) => {
      await manager.query(
        `SELECT pg_advisory_xact_lock(hashtext('arena3q-admin-state'))`,
      );
      const before = await this.load(manager);
      const state = structuredClone(before);
      const result = await change(state);
      protectAudit(before, state);
      state.revision = before.revision + 1;
      databaseSchema.parse(state);
      const pairs = [
        [HeroEntity, "heroes"],
        [AssetEntity, "assets"],
        [UserEntity, "users"],
        [SessionEntity, "sessions"],
        [AuditEntity, "audit"],
      ] as const;
      for (const [entity, key] of pairs) {
        const repository = manager.getRepository(entity as typeof HeroEntity);
        for (const row of state[key]) {
          const old = before[key].find((item) => item.id === row.id);
          if (JSON.stringify(old) !== JSON.stringify(row))
            await repository.save({ id: row.id, payload: row } as any);
        }
        if (key !== "audit")
          for (const old of before[key])
            if (!state[key].some((row) => row.id === old.id))
              await repository.delete(old.id);
      }
      const { heroes, assets, users, sessions, audit, ...metadata } = state;
      await manager.save(StateEntity, { id: "main", payload: metadata });
      return result;
    });
  }
  async close() {
    await this.source.destroy();
  }
}
