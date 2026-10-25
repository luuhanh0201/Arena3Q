const fs = require("node:fs"),
  path = require("node:path"),
  crypto = require("node:crypto");
const { createRequire } = require("node:module");
const root = path.resolve(__dirname, "../../../..");
const requireAdmin = createRequire(path.join(root, "Admin/package.json"));
const sharp = requireAdmin("sharp");
const review = path.join(
  root,
  "NghiemThu/Battlefield/XemTruoc_Assets_20261025",
);
const posix = (value) => value.replaceAll("\\", "/");
const digest = (file) =>
  crypto.createHash("sha256").update(fs.readFileSync(file)).digest("hex");
const items = [];
const ownedNames = {
  BaguaRoundStoneAltar: "Tế đàn bát quái",
  BaguaStonePillar: "Trụ đá bát quái",
  BaguaWideStoneSlab: "Phiến đá cổng",
  MilitaryFlagpoleBanner: "Cột cờ quân sự",
  ThreeKingdomsCommand_1: "Đài trống chỉ huy 1",
  ThreeKingdomsCommand_2: "Đài trống chỉ huy 2",
  ThreeKingdomsSquare: "Lều doanh trại Tam Quốc",
  WoodenCampWatchtower: "Tháp canh gỗ",
  WoodenPalisadeFence: "Hàng rào doanh trại",
};
function gltfBytes(file) {
  const document = JSON.parse(fs.readFileSync(file, "utf8"));
  const resources = new Set(
    [...(document.buffers ?? []), ...(document.images ?? [])]
      .map((item) => item.uri)
      .filter((uri) => uri && !uri.startsWith("data:")),
  );
  return (
    fs.statSync(file).size +
    [...resources].reduce(
      (sum, uri) =>
        sum +
        fs.statSync(path.join(path.dirname(file), decodeURIComponent(uri)))
          .size,
      0,
    )
  );
}
const statGltf = (file) => {
  const doc = file.endsWith(".glb")
    ? (() => {
        const data = fs.readFileSync(file);
        return JSON.parse(
          data.subarray(20, 20 + data.readUInt32LE(12)).toString("utf8"),
        );
      })()
    : JSON.parse(fs.readFileSync(file, "utf8"));
  return {
    triangles: (doc.meshes ?? [])
      .flatMap((mesh) => mesh.primitives)
      .reduce(
        (total, p) =>
          total + doc.accessors[p.indices ?? p.attributes.POSITION].count / 3,
        0,
      ),
    meshes: (doc.meshes ?? []).length,
    images: (doc.images ?? []).length,
  };
};
const add = (item) => {
  item.number = items.length + 1;
  item.thumbnail = `/review/thumbs/${item.id}.png`;
  items.push(item);
};
const plan = JSON.parse(
  fs.readFileSync(path.join(__dirname, "owned-export-plan.json"), "utf8"),
);
for (const asset of plan) {
  const source = path.join(review, "models", asset.id + "-source.glb"),
    budget = path.join(review, "models", asset.id + "-budget.glb");
  if (!fs.existsSync(source) || !fs.existsSync(budget))
    throw Error("Finish owned exports before preparing review: " + asset.id);
  add({
    id: "arena_" + asset.id,
    label: ownedNames[asset.id],
    group: asset.id.startsWith("Bagua")
      ? "Trung tâm độc quyền"
      : "Doanh trại Arena3Q",
    kind: "model",
    provider: "Arena3Q",
    license: "Asset người dùng đang có trong project",
    featured: true,
    url: "/review/models/" + asset.id + "-source.glb",
    budgetUrl: "/review/models/" + asset.id + "-budget.glb",
    bytes: fs.statSync(source).size,
    budgetBytes: fs.statSync(budget).size,
    sourcePath: asset.source.model,
    use: asset.id.startsWith("Bagua")
      ? "Giữ nguyên nhận diện trung tâm; không tạo lại bằng primitive."
      : "Kiến trúc chính; hai phe dùng biến thể vật liệu khi được duyệt.",
    note: "Bản nguồn giữ toàn bộ geometry và texture gốc. Bản nhẹ là model Props3D đã có; không giảm lưới mới trong bước này.",
    stats: statGltf(source),
    budgetStats: statGltf(budget),
    sourceSha256: digest(path.join(root, asset.source.model)),
  });
}
const natureDir = path.join(
  root,
  "Assets/ThirdParty/Quaternius/StylizedNatureStandard/glTF",
);
function natureGroup(name) {
  if (/Tree|Pine/.test(name)) return "Cây";
  if (/Rock|Pebble/.test(name)) return "Đá và đường lát";
  if (/Grass/.test(name)) return "Cỏ";
  if (/Bush|Fern|Clover|Plant/.test(name)) return "Bụi và cây thấp";
  return "Hoa và chi tiết tự nhiên";
}
const natureFeatured = new Set([
  "CommonTree_1",
  "CommonTree_2",
  "CommonTree_3",
  "CommonTree_5",
  "Pine_1",
  "Pine_3",
  "Pine_5",
  "TwistedTree_1",
  "TwistedTree_3",
  "Bush_Common",
  "Bush_Common_Flowers",
  "Fern_1",
  "Clover_1",
  "Plant_1_Big",
  "Plant_7_Big",
  "Flower_3_Group",
  "Flower_4_Group",
  "Grass_Common_Short",
  "Grass_Common_Tall",
  "Grass_Wispy_Short",
  "Grass_Wispy_Tall",
  "Rock_Medium_1",
  "Rock_Medium_2",
  "Rock_Medium_3",
  "RockPath_Round_Small_1",
  "RockPath_Round_Wide",
  "RockPath_Square_Thin",
  "Pebble_Round_1",
  "Pebble_Round_3",
  "Pebble_Square_1",
]);
for (const filename of fs
  .readdirSync(natureDir)
  .filter((name) => name.endsWith(".gltf"))
  .sort()) {
  const name = path.basename(filename, ".gltf");
  add({
    id: "nature_" + name,
    label: name.replaceAll("_", " "),
    group: natureGroup(name),
    kind: "model",
    provider: "Quaternius Nature Standard",
    license: "CC0 1.0 Universal",
    featured: natureFeatured.has(name),
    url: "/nature/" + encodeURIComponent(filename),
    bytes: fs.statSync(path.join(natureDir, filename)).size,
    sourcePath: posix(path.relative(root, path.join(natureDir, filename))),
    use: /Tree|Pine/.test(name)
      ? "Cây lớn ở rìa, không che vùng giao tranh."
      : /Rock|Pebble/.test(name)
        ? "Đá viền, vách thấp hoặc chi tiết đường; không chặn lối đi."
        : "Cụm thấp ở rìa và gần doanh trại.",
    note: "Model/texture gốc của bản miễn phí. Shader URP/gió trả phí không nằm trong gói; chưa tạo prefab map.",
    stats: statGltf(path.join(natureDir, filename)),
  });
}
const propsDir = path.join(
  root,
  "Assets/ThirdParty/Quaternius/FantasyPropsStandard/Exports/glTF",
);
const propsFeatured = new Set([
  "Bag",
  "Barrel",
  "Barrel_Holder",
  "Bench",
  "Bucket_Metal",
  "Bucket_Wooden_1",
  "Candle_1",
  "Chest_Wood",
  "Crate_Wooden",
  "Crate_Metal",
  "Dummy",
  "FarmCrate_Empty",
  "Lantern_Wall",
  "Peg_Rack",
  "Pouch_Large",
  "Rope_1",
  "Rope_2",
  "Rope_3",
  "Shield_Wooden",
  "Stall_Cart_Empty",
  "Sword_Bronze",
  "Table_Large",
  "Torch_Metal",
  "Vase_Rubble_Medium",
  "WeaponStand",
  "Whetstone",
  "Workbench",
]);
for (const filename of fs
  .readdirSync(propsDir)
  .filter((name) => name.endsWith(".gltf"))
  .sort()) {
  const name = path.basename(filename, ".gltf");
  add({
    id: "props_" + name,
    label: name.replaceAll("_", " "),
    group: propsFeatured.has(name) ? "Đạo cụ doanh trại" : "Đạo cụ dự phòng",
    kind: "model",
    provider: "Quaternius Props Standard",
    license: "CC0 1.0 Universal",
    featured: propsFeatured.has(name),
    url: "/props/" + encodeURIComponent(filename),
    bytes: fs.statSync(path.join(propsDir, filename)).size,
    sourcePath: posix(path.relative(root, path.join(propsDir, filename))),
    use: propsFeatured.has(name)
      ? "Đề xuất trang trí doanh trại, không thay kiến trúc Arena3Q."
      : "Thư viện dự phòng; chưa đề xuất dùng trên chiến trường.",
    note: "Chỉ dùng các món hợp bối cảnh. Cờ, kiếm, khiên hoặc xe hàng cần bạn duyệt phong cách trước.",
    stats: statGltf(path.join(propsDir, filename)),
  });
}
const terrainNames = {
  DrySand: "Đất cát khô",
  PackedEarth: "Đất nén đường đi",
  GravelSoil: "Đất pha sỏi",
  OldPaving: "Đá lát cũ",
  DryGrass: "Cỏ khô thưa",
  SparseGreenGrass: "Cỏ xanh thưa",
};
for (const [id, label] of Object.entries(terrainNames)) {
  const file = path.join(review, "textures", id + ".png");
  add({
    id: "ground_" + id,
    label,
    group: "Mẫu vật liệu mặt đất",
    kind: "texture",
    provider: "Arena3Q · imagegen",
    license: "Ảnh mới tạo riêng cho dự án",
    featured: true,
    url: "/review/textures/" + id + ".png",
    bytes: fs.statSync(file).size,
    sourcePath: posix(path.relative(root, file)),
    use: "Mẫu albedo lặp cho mặt đất 3D, không phải ảnh nền trải toàn map.",
    note: "Chưa gắn shader, normal/roughness hoặc terrain; cần duyệt màu/chất liệu và kiểm đường nối ở chế độ lặp.",
    sha256: digest(file),
  });
}
const particleFiles = [
  "fire_01",
  "fire_02",
  "flame_01",
  "flame_04",
  "smoke_01",
  "smoke_02",
  "dirt_01",
  "dirt_02",
];
const particleDir = path.join(
  root,
  "Assets/ThirdParty/Kenney/ParticlePack/PNG (Transparent)",
);
for (const name of particleFiles) {
  const file = path.join(particleDir, name + ".png");
  if (!fs.existsSync(file)) continue;
  add({
    id: "vfx_" + name,
    label: name.replaceAll("_", " "),
    group: "Tài nguyên VFX môi trường",
    kind: "particle",
    provider: "Kenney Particle Pack",
    license: "CC0 1.0 Universal",
    featured: true,
    url: "/particles/PNG%20(Transparent)/" + name + ".png",
    bytes: fs.statSync(file).size,
    sourcePath: posix(path.relative(root, file)),
    use: "Sprite nguồn cho lửa, khói, bụi; sau duyệt mới cấu hình particle trong Unity.",
    note: "Đây là texture nguồn, chưa phải hiệu ứng môi trường đã hoạt động.",
  });
}
const sources = {
  status: "PENDING_APPROVAL",
  mapCreated: false,
  retrievedAt: new Date().toISOString(),
  sources: [
    {
      name: "Quaternius Stylized Nature MegaKit Standard",
      url: "https://quaternius.com/packs/stylizednaturemegakit.html",
      downloadPage: "https://quaternius.itch.io/stylized-nature-megakit",
      license: "CC0-1.0",
      licenseFile:
        "Assets/ThirdParty/Quaternius/StylizedNatureStandard/License_Standard.txt",
      models: 68,
      archiveSha256: digest(
        path.join(
          root,
          "Library/BattlefieldDownloads/Quaternius_StylizedNature_Standard.zip",
        ),
      ),
    },
    {
      name: "Quaternius Fantasy Props MegaKit Standard",
      url: "https://quaternius.com/packs/fantasypropsmegakit",
      downloadPage: "https://quaternius.itch.io/fantasy-props-megakit",
      license: "CC0-1.0",
      licenseFile:
        "Assets/ThirdParty/Quaternius/FantasyPropsStandard/License_Standard.txt",
      models: 94,
      archiveSha256: digest(
        path.join(
          root,
          "Library/BattlefieldDownloads/Quaternius_FantasyProps_Standard.zip",
        ),
      ),
    },
    {
      name: "Kenney Particle Pack",
      url: "https://kenney.nl/assets/particle-pack",
      license: "CC0-1.0",
      licenseFile: "Assets/ThirdParty/Kenney/ParticlePack/License.txt",
      archiveSha256: digest(
        path.join(root, "Library/BattlefieldDownloads/Kenney_ParticlePack.zip"),
      ),
    },
  ],
  ownedModels: 9,
  generatedTextures: 6,
};
const gaps = [
  "Bộ đề xuất tập trung vào kiến trúc Arena3Q, cây/đá/cỏ, đạo cụ quân trại và 6 mẫu đất. Các món dự phòng không tự động được đưa vào map.",
  "Chưa có model tre, đào nở hoa hoặc phong lá đỏ đúng loài. Có cây tán tròn/tán xoắn làm phương án thay thế; không gọi chúng là model đúng loài.",
  "Chưa có cổng trại và tường đổ phương Đông riêng. Có thể ghép prefab mới từ hàng rào/tháp canh/đá hiện có sau khi duyệt, giữ nguyên mesh gốc.",
  "Rock Medium là nguồn đá để bố trí vách thấp; chưa có cliff set riêng. Lửa/khói mới có texture nguồn; chưa cấu hình particle.",
  "Chưa sửa Scene, map, camera, navigation, combat hay prefab gốc. Wind/LOD/lighting/performance sẽ làm sau khi bạn duyệt asset.",
];
(async () => {
  const previous = path.join(review, "manifest.json");
  if (
    fs.existsSync(previous) &&
    JSON.parse(fs.readFileSync(previous, "utf8")).status !== "PENDING_APPROVAL"
  )
    throw Error("Approved review is immutable; create a new version instead.");
  for (const item of items.filter(
    (item) => item.kind === "model" && item.sourcePath.endsWith(".gltf"),
  )) {
    item.bytes = gltfBytes(path.join(root, item.sourcePath));
    item.note +=
      " Dung lượng tính cả geometry và texture dùng chung, không chỉ file JSON glTF.";
  }
  fs.mkdirSync(path.join(review, "thumbs"), { recursive: true });
  for (const item of items.filter((item) => item.kind !== "model")) {
    await sharp(path.join(root, item.sourcePath))
      .resize(420, 280, { fit: "inside", withoutEnlargement: true })
      .png()
      .toFile(path.join(review, "thumbs", item.id + ".png"));
  }
  fs.writeFileSync(
    path.join(review, "sources.json"),
    JSON.stringify(sources, null, 2) + "\n",
  );
  fs.writeFileSync(
    path.join(review, "manifest.json"),
    JSON.stringify(
      {
        status: "PENDING_APPROVAL",
        mapCreated: false,
        summary: `9 model Arena3Q · 68 model thiên nhiên · 94 đạo cụ miễn phí · 6 mẫu vật liệu mới · ${items.filter((item) => item.kind === "particle").length} texture VFX. Chưa tạo map.`,
        gaps,
        items,
      },
      null,
      2,
    ) + "\n",
  );
  console.log(
    JSON.stringify({
      items: items.length,
      featured: items.filter((item) => item.featured).length,
      models: items.filter((item) => item.kind === "model").length,
    }),
  );
})().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
