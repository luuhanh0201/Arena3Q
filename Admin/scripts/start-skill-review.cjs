// Serve only the prepared review page and its approved-for-preview files.
// This server never writes to the review manifest, Admin database or Unity.
const fs = require("node:fs");
const path = require("node:path");
const http = require("node:http");

const folder = path.resolve(
  __dirname,
  "../../NghiemThu/Admin/XemTruoc_SkillIcons_20261025",
);
const manifest = JSON.parse(
  fs.readFileSync(path.join(folder, "manifest.json"), "utf8"),
);
const files = new Map([
  ["/", ["xem_anh_ky_nang.html", "text/html; charset=utf-8"]],
  [
    "/xem_anh_ky_nang.html",
    ["xem_anh_ky_nang.html", "text/html; charset=utf-8"],
  ],
  ["/contact-sheet.png", ["contact-sheet.png", "image/png"]],
]);
for (const skill of manifest.skills) {
  if (!/^[a-z0-9_-]+\.png$/i.test(skill.file))
    throw new Error("Invalid review image filename");
  files.set("/" + skill.file, [skill.file, "image/png"]);
}
for (const [filename] of files.values()) {
  if (!fs.statSync(path.join(folder, filename)).isFile())
    throw new Error("Missing review file: " + filename);
}

const server = http.createServer(async (request, response) => {
  response.setHeader("X-Content-Type-Options", "nosniff");
  if (request.method !== "GET" && request.method !== "HEAD") {
    response.writeHead(405, { Allow: "GET, HEAD" });
    response.end();
    return;
  }
  let pathname;
  try {
    pathname = decodeURIComponent(
      new URL(request.url, "http://localhost").pathname,
    );
  } catch {
    response.writeHead(400).end();
    return;
  }
  const entry = files.get(pathname);
  if (!entry) {
    response.writeHead(404).end("Not found");
    return;
  }
  try {
    const [filename, type] = entry;
    const source = path.join(folder, filename);
    const stat = await fs.promises.stat(source);
    response.writeHead(200, {
      "Content-Type": type,
      "Content-Length": stat.size,
      "Cache-Control": type.startsWith("text/html")
        ? "no-store"
        : "private, max-age=3600",
    });
    if (request.method === "HEAD") {
      response.end();
      return;
    }
    const stream = fs.createReadStream(source);
    stream.on("error", () => response.destroy());
    response.on("close", () => stream.destroy());
    stream.pipe(response);
  } catch {
    response.writeHead(500).end("Cannot load review file");
  }
});
server.on("error", (error) => {
  console.error(error.message);
  process.exitCode = 1;
});
server.listen(4310, "127.0.0.1", () => {
  console.log("Skill image review: http://127.0.0.1:4310");
  console.log(
    manifest.skills.length + " images; pending user approval, not applied.",
  );
});
