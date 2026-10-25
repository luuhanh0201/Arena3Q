import path from "node:path";
import { config as dotenv } from "dotenv";
export const adminRoot = path.resolve(
  process.env.ADMIN_ROOT ?? path.join(__dirname, "../../.."),
);
dotenv({ path: path.join(adminRoot, ".env"), quiet: true });
export const config = {
  root: adminRoot,
  unityRoot: path.resolve(adminRoot, process.env.UNITY_PROJECT_ROOT ?? ".."),
  unityWriteEnabled: process.env.UNITY_WRITE_ENABLED !== "false",
  port: Number(process.env.API_PORT ?? 4000),
  host: process.env.API_HOST ?? "127.0.0.1",
  driver: process.env.DATABASE_DRIVER ?? "json",
  databaseFile: path.resolve(
    adminRoot,
    process.env.JSON_DB_PATH ?? ".local/db.json",
  ),
  databaseUrl: process.env.DATABASE_URL ?? "",
  storageRoot: path.resolve(
    adminRoot,
    process.env.LOCAL_STORAGE_PATH ?? ".local/storage",
  ),
  cookieSecure: process.env.COOKIE_SECURE === "true",
  sessionHours: Number(process.env.SESSION_HOURS ?? 365 * 24),
  allowedOrigins: (
    process.env.ADMIN_ORIGINS ??
    "http://localhost:3000,http://127.0.0.1:3000,http://localhost:8080"
  )
    .split(",")
    .map((value) => value.trim()),
  redisUrl: process.env.REDIS_URL ?? "",
  storageDriver: process.env.STORAGE_DRIVER ?? "local",
};
