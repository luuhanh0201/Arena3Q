// Run the same standalone Next.js output as the Docker image, bound to localhost.
const fs = require("node:fs");
const path = require("node:path");
const { spawn } = require("node:child_process");
const root = path.resolve(__dirname, "..");
const web = path.join(root, "apps/web/.next/standalone/apps/web");
if (
  !fs.existsSync(path.join(web, "server.js")) ||
  !fs.existsSync(path.join(root, "apps/api/dist/main.js"))
) {
  console.error("Chạy npm run build trước khi npm start.");
  process.exit(1);
}
fs.cpSync(path.join(root, "apps/web/public"), path.join(web, "public"), {
  recursive: true,
});
fs.cpSync(
  path.join(root, "apps/web/.next/static"),
  path.join(web, ".next/static"),
  { recursive: true },
);
const children = [
  spawn(process.execPath, ["apps/api/dist/main.js"], {
    cwd: root,
    stdio: "inherit",
    env: { ...process.env, NODE_ENV: "production", API_HOST: "127.0.0.1" },
  }),
  spawn(process.execPath, [path.join(web, "server.js")], {
    cwd: web,
    stdio: "inherit",
    env: {
      ...process.env,
      NODE_ENV: "production",
      HOSTNAME: "127.0.0.1",
      PORT: process.env.PORT || "3000",
    },
  }),
];
let stopping = false;
function stop(code = 0) {
  if (stopping) return;
  stopping = true;
  for (const child of children) child.kill();
  process.exitCode = code;
}
for (const signal of ["SIGINT", "SIGTERM"]) process.on(signal, () => stop());
for (const child of children) {
  child.on("error", (error) => {
    console.error(error.message);
    stop(1);
  });
  child.on("exit", (code) => stop(code ?? 0));
}
