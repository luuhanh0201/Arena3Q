import { promises as fs } from "node:fs";
import path from "node:path";
import { randomBytes, randomUUID } from "node:crypto";
import { fileURLToPath } from "node:url";
import { JsonFileStore } from "../apps/api/src/persistence/json-store";
import { hashPassword } from "../apps/api/src/auth/password";
import { importUnity } from "./import-unity";
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
async function setup() {
  const store = new JsonFileStore(path.join(root, ".local/db.json"));
  const state = await store.read();
  if (!state.users.length) {
    const email = process.env.ADMIN_EMAIL ?? "admin@arena3q.local";
    const password =
      process.env.ADMIN_PASSWORD ?? randomBytes(18).toString("base64url");
    if (password.length < 12)
      throw new Error("ADMIN_PASSWORD cần ít nhất 12 ký tự.");
    const passwordHash = await hashPassword(password);
    await store.update((database) => {
      if (database.users.length)
        throw new Error("Admin was initialized by another process.");
      database.users.push({
        id: randomUUID(),
        email,
        displayName: "Quản trị viên",
        passwordHash,
        role: "SUPER_ADMIN",
        permissions: ["*"],
        active: true,
      });
    });
    await fs.writeFile(
      path.join(root, ".local/bootstrap-credentials.json"),
      JSON.stringify({ email, password }, null, 2) + "\n",
      { mode: 0o600, flag: "wx" },
    );
    console.log(
      "Đã tạo admin local. Thông tin đăng nhập: Admin/.local/bootstrap-credentials.json (không vào Git).",
    );
  } else
    console.log("Giữ nguyên tài khoản admin và dữ liệu đăng nhập hiện có.");
  const result = await importUnity(store);
  await fs.mkdir(path.join(root, "apps/web/public"), { recursive: true });
  await fs.copyFile(
    path.join(
      root,
      "../Assets/Arena3Q/Art/UI/Startup/UI/Logo/logo_arena3q.png",
    ),
    path.join(root, "apps/web/public/arena3q-logo.png"),
  );
  console.log(
    `Đã nhập ${result.heroes} tướng và ${result.assets} tham chiếu asset từ Unity. ${result.warnings.length} cảnh báo nguồn, xem trong metadata API.`,
  );
  console.log("Chạy npm run dev, mở http://localhost:3000/admin.");
}
setup().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
