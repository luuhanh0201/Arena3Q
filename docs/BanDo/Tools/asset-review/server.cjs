// Local, read-only asset approval. No scene generation, Unity writes or approval mutation.
const http = require("node:http");
const fs = require("node:fs");
const path = require("node:path");
const root = path.resolve(__dirname, "../../../..");
const review = path.join(
  root,
  "NghiemThu/Battlefield/XemTruoc_Assets_20261025",
);
const mounts = new Map([
  ["/review/", review],
  ["/map-evidence/",path.join(root,"NghiemThu/Battlefield/XemTruoc_Map_20261025")],
  ["/map-feedback/",path.join(root,"NghiemThu/Battlefield/XemTruoc_ChinhSua_20261025")],
  ["/test-apk/",path.join(root,"Builds/Android")],
  [
    "/nature/",
    path.join(root, "Assets/ThirdParty/Quaternius/StylizedNatureStandard/glTF"),
  ],
  [
    "/props/",
    path.join(
      root,
      "Assets/ThirdParty/Quaternius/FantasyPropsStandard/Exports/glTF",
    ),
  ],
  ["/particles/", path.join(root, "Assets/ThirdParty/Kenney/ParticlePack")],
  ["/vendor/three/", path.join(root, "Admin/node_modules/three")],
  ["/app/", __dirname],
]);
const mime = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".gltf": "model/gltf+json",
  ".glb": "model/gltf-binary",
  ".bin": "application/octet-stream",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".webp": "image/webp",
  ".mp4": "video/mp4",
  ".apk": "application/vnd.android.package-archive",
};
const server = http.createServer(async (request, response) => {
  response.setHeader("X-Content-Type-Options", "nosniff");
  if (!["GET", "HEAD"].includes(request.method)) {
    response.writeHead(405, { Allow: "GET, HEAD" }).end();
    return;
  }
  try {
    let url = decodeURIComponent(
      new URL(request.url, "http://localhost").pathname,
    );
    if (url === "/") url = "/app/index.html";
    const mount = [...mounts].find(([prefix]) => url.startsWith(prefix));
    if (!mount) {
      response.writeHead(404).end();
      return;
    }
    const [prefix, folder] = mount;
    const filename = path.resolve(folder, url.slice(prefix.length));
    const relative = path.relative(folder, filename);
    if(prefix==="/test-apk/"&&!["Arena3Q-Battlefield-20261025.apk","Arena3Q-Battlefield-Feedback-20261025.apk"].includes(relative)){
      response.writeHead(404).end();return;
    }
    const type = mime[path.extname(filename).toLowerCase()];
    if (!type || relative.startsWith("..") || path.isAbsolute(relative)) {
      response.writeHead(404).end();
      return;
    }
    const actual = await fs.promises.realpath(filename);
    const actualRelative = path.relative(
      await fs.promises.realpath(folder),
      actual,
    );
    if (actualRelative.startsWith("..") || path.isAbsolute(actualRelative)) {
      response.writeHead(404).end();
      return;
    }
    const info = await fs.promises.stat(actual);
    if (!info.isFile()) {
      response.writeHead(404).end();
      return;
    }
    response.writeHead(200, {
      "Content-Type": type,
      "Content-Length": info.size,
      ...(type==="application/vnd.android.package-archive"?{"Content-Disposition":'attachment; filename="Arena3Q-Battlefield-20261025.apk"'}:{}),
      "Cache-Control":
        type.includes("json") ||
        type.includes("html") ||
        type.includes("javascript")
          ? "no-store"
          : "private, max-age=3600",
    });
    if (request.method === "HEAD") {
      response.end();
      return;
    }
    const stream = fs.createReadStream(actual);
    stream.on("error", () => response.destroy());
    response.on("close", () => stream.destroy());
    stream.pipe(response);
  } catch (error) {
    response
      .writeHead(error.code === "ENOENT" ? 404 : 400)
      .end("Cannot load asset");
  }
});
server.on("error", (error) => {
  console.error(error.message);
  process.exitCode = 1;
});
server.listen(4311, "127.0.0.1", () =>
  console.log(
    "Battlefield asset approval: http://127.0.0.1:4311 (read-only, no map generation)",
  ),
);
