import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import { RoomEnvironment } from "three/addons/environments/RoomEnvironment.js";
const $ = (id) => document.getElementById(id);
const manifest = await (await fetch("/review/manifest.json")).json();
const canvas = $("canvas"),
  stage = $("stage");
const tiles = document.createElement("div");
tiles.id = "tiles";
Object.assign(tiles.style, {
  position: "absolute",
  left: "50%",
  top: "50%",
  transform: "translate(-50%, -50%)",
  display: "none",
  backgroundSize: "33.333333% 33.333333%",
});
stage.insertBefore(tiles, $("status"));
const renderer = new THREE.WebGLRenderer({
  canvas,
  antialias: true,
  preserveDrawingBuffer: true,
});
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.12;
const scene = new THREE.Scene();
scene.background = new THREE.Color("#111d2d");
const room = new RoomEnvironment(),
  pmrem = new THREE.PMREMGenerator(renderer);
scene.environment = pmrem.fromScene(room, 0.04).texture;
scene.add(new THREE.HemisphereLight(0xe5efff, 0x846e46, 1.4));
const sun = new THREE.DirectionalLight(0xffe3b6, 2.3);
sun.position.set(-4, 7, 6);
scene.add(sun);
const fill = new THREE.DirectionalLight(0xbdd6ff, 0.7);
fill.position.set(4, 3, -4);
scene.add(fill);
const camera = new THREE.PerspectiveCamera(38, 1, 0.01, 1000);
const controls = new OrbitControls(camera, canvas);
controls.enableDamping = true;
const loader = new GLTFLoader();
let current = null,
  selected = null,
  loading = 0,
  wireframe = false,
  repeated = false,
  bounds = null;
const escape = (value) =>
  String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll('"', "&quot;");
const resize = () => {
  const w = stage.clientWidth,
    h = stage.clientHeight;
  renderer.setSize(w, h, false);
  camera.aspect = w / h;
  camera.updateProjectionMatrix();
};
new ResizeObserver(resize).observe(stage);
resize();
function dispose(object) {
  object?.traverse((node) => {
    if (node.isMesh) {
      node.geometry.dispose();
      for (const material of Array.isArray(node.material)
        ? node.material
        : [node.material]) {
        for (const value of Object.values(material))
          if (value?.isTexture) value.dispose();
        material.dispose();
      }
    }
  });
}
function frame(front = false) {
  if (!bounds) return;
  const sphere = bounds.getBoundingSphere(new THREE.Sphere());
  const vertical = THREE.MathUtils.degToRad(camera.fov / 2),
    horizontal = Math.atan(Math.tan(vertical) * camera.aspect);
  const distance =
    (sphere.radius / Math.sin(Math.min(vertical, horizontal))) * 1.15;
  const direction = new THREE.Vector3(
    ...(front ? [0, 0.08, 1] : [1, 0.65, 1.25]),
  ).normalize();
  camera.position.copy(sphere.center).addScaledVector(direction, distance);
  camera.near = Math.max(0.001, distance / 1000);
  camera.far = Math.max(100, distance * 20);
  camera.updateProjectionMatrix();
  controls.target.copy(sphere.center);
  controls.update();
}
function render() {
  controls.update();
  renderer.render(scene, camera);
  requestAnimationFrame(render);
}
render();
function showDetail(item, stats = {}) {
  stats.bytes ??= item.bytes;
  $("name").textContent = item.label;
  $("subtitle").textContent = item.group + " · " + item.provider;
  $("detail").innerHTML =
    `<p><strong>${escape(item.id)}</strong> · ${escape(item.license)}</p><p>${escape(item.use)}</p><p>${escape(item.note ?? "")}</p>${stats.triangles ? `<p>${stats.triangles.toLocaleString("vi-VN")} tam giác · ${stats.meshes} mesh</p>` : ""}${stats.bytes ? `<p>Dung lượng ${(stats.bytes / 1048576).toFixed(2)} MiB</p>` : ""}<p><code>${escape(item.sourcePath)}</code></p>`;
}
async function select(item, { thumbnail = false } = {}) {
  const token = ++loading;
  selected = item;
  wireframe = false;
  repeated = false;
  tiles.style.display = "none";
  $("repeat").textContent = "Xem lặp 3 × 3";
  $("wireframe").classList.remove("active");
  $("error").textContent = "";
  if (current) {
    scene.remove(current);
    dispose(current);
    current = null;
  }
  $("quality").style.display = item.budgetUrl ? "" : "none";
  $("repeat").style.display = item.kind === "texture" ? "inline-block" : "none";
  if (item.kind === "texture" || item.kind === "particle") {
    canvas.style.display = "none";
    $("texture").style.display = "block";
    $("texture").src = item.url;
    $("texture").alt = item.label;
    $("texture").style.visibility = "visible";
    stage.style.backgroundImage = "none";
    $("original").href = item.url;
    $("status").textContent =
      item.kind === "texture"
        ? "Mẫu albedo chưa áp dụng lên map; kiểm thử lặp để đánh giá đường nối."
        : "Texture nguồn cho hiệu ứng; chưa phải prefab hiệu ứng đang chạy.";
    bounds = null;
    showDetail(item);
    window.__assetReady = item.id;
    return;
  }
  canvas.style.display = "block";
  $("texture").style.display = "none";
  stage.style.backgroundImage = "none";
  const quality = thumbnail ? "budget" : $("quality").value;
  const url =
    quality === "budget" && item.budgetUrl ? item.budgetUrl : item.url;
  $("original").href = url;
  $("status").textContent = "Đang tải model thật và texture...";
  showDetail(item);
  try {
    const gltf = await loader.loadAsync(url);
    if (token !== loading) {
      dispose(gltf.scene);
      return;
    }
    current = gltf.scene;
    scene.add(current);
    current.updateMatrixWorld(true);
    let triangles = 0,
      meshes = 0;
    current.traverse((node) => {
      if (node.isMesh) {
        meshes++;
        triangles +=
          (node.geometry.index?.count ??
            node.geometry.attributes.position.count) / 3;
      }
    });
    bounds = new THREE.Box3().setFromObject(current, true);
    frame();
    renderer.render(scene, camera);
    showDetail(item, {
      triangles,
      meshes,
      bytes:
        quality === "budget" && item.budgetUrl ? item.budgetBytes : item.bytes,
    });
    $("status").textContent =
      "Model thật · Kéo xoay, lăn chuột zoom · " +
      (item.budgetUrl && quality === "budget"
        ? "Bản nhẹ hiện có"
        : "Chất lượng nguồn gốc");
    window.__assetReady = item.id;
  } catch (error) {
    $("error").textContent = "Không tải được model: " + error.message;
    $("status").textContent = "Có lỗi khi tải asset";
    window.__assetError = { id: item.id, message: error.message };
    throw error;
  }
}
function list() {
  const query = $("search").value.toLocaleLowerCase("vi");
  const shown = manifest.items.filter(
    (item) =>
      (!$("category").value || item.group === $("category").value) &&
      ($("selection").value === "all" || item.featured) &&
      (!query ||
        (item.id + " " + item.label + " " + item.group)
          .toLocaleLowerCase("vi")
          .includes(query)),
  );
  $("count").textContent =
    `${shown.length} asset đang hiển thị · ${manifest.items.length} tài nguyên trong danh mục`;
  $("cards").innerHTML =
    shown
      .map(
        (item) =>
          `<button class="card" data-id="${escape(item.id)}"><img src="${escape(item.thumbnail)}" alt="${escape(item.label)}" loading="lazy"><div class="meta"><strong>${String(item.number).padStart(3, "0")} · ${escape(item.label)}</strong><small>${escape(item.group)}</small><small>${escape(item.provider)}</small><span class="badge">${item.provider === "Arena3Q" ? "Đã có trong project" : item.kind === "texture" ? "Mẫu đề xuất" : "CC0"}</span></div></button>`,
      )
      .join("") || '<p class="empty">Không có asset phù hợp.</p>';
  document
    .querySelectorAll(".card")
    .forEach(
      (button) =>
        (button.onclick = () =>
          select(
            manifest.items.find((item) => item.id === button.dataset.id),
          ).catch(() => {})),
    );
}
$("summary").textContent = manifest.summary;
for (const group of [...new Set(manifest.items.map((item) => item.group))]) {
  $("category").add(new Option(group, group));
}
$("gaps").innerHTML = manifest.gaps
  .map((text) => `<li>${escape(text)}</li>`)
  .join("");
for (const name of ["search", "category", "selection"])
  $(name).addEventListener(name === "search" ? "input" : "change", list);
$("reset").onclick = () => frame();
$("front").onclick = () => frame(true);
$("wireframe").onclick = () => {
  wireframe = !wireframe;
  $("wireframe").classList.toggle("active", wireframe);
  current?.traverse((node) => {
    if (node.isMesh)
      for (const material of Array.isArray(node.material)
        ? node.material
        : [node.material])
        material.wireframe = wireframe;
  });
};
$("quality").onchange = () => selected && select(selected).catch(() => {});
$("repeat").onclick = () => {
  if (!selected || selected.kind !== "texture") return;
  repeated = !repeated;
  $("texture").style.visibility = repeated ? "hidden" : "visible";
  const edge = Math.min(stage.clientWidth, stage.clientHeight);
  Object.assign(tiles.style, {
    display: repeated ? "block" : "none",
    width: edge + "px",
    height: edge + "px",
    backgroundImage: `url("${selected.url}")`,
  });
  $("repeat").textContent = repeated ? "Xem một ô" : "Xem lặp 3 × 3";
};
window.__reviewManifest = manifest;
window.__previewAsset = async (id, { thumbnail = false } = {}) => {
  const item = manifest.items.find((item) => item.id === id);
  await select(item, { thumbnail });
  if (item.kind !== "model") return null;
  await new Promise((resolve) =>
    requestAnimationFrame(() => requestAnimationFrame(resolve)),
  );
  renderer.render(scene, camera);
  return canvas.toDataURL("image/png");
};
$("quality").value = "budget";
list();
await select(manifest.items[0], { thumbnail: true });
