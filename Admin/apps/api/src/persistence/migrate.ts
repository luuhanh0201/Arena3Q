import { config } from "../config";
import { postgresDataSource } from "./postgres-store";
import { PostgresStore } from "./postgres-store";
import { JsonFileStore } from "./json-store";
async function main() {
  if (!config.databaseUrl) throw new Error("DATABASE_URL is required.");
  const source = postgresDataSource(config.databaseUrl);
  await source.initialize();
  try {
    const migrations = await source.runMigrations();
    console.log(`Applied ${migrations.length} migrations.`);
    if (process.env.IMPORT_LOCAL_JSON === "true") {
      const snapshot = await new JsonFileStore(config.databaseFile).read();
      const store = new PostgresStore(source);
      await store.update((state) => {
        if (
          state.revision ||
          state.users.length ||
          state.heroes.length ||
          state.audit.length ||
          state.assets.length ||
          state.sessions.length
        )
          throw new Error("Refusing import: PostgreSQL target must be empty.");
        Object.assign(state, snapshot, { sessions: [] });
      });
      console.log(
        `Imported ${snapshot.heroes.length} heroes, ${snapshot.assets.length} assets, ${snapshot.users.length} users. Existing browser sessions were not copied.`,
      );
    }
  } finally {
    await source.destroy();
  }
}
main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
