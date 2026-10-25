// Test-only GLB: one triangle and one translation clip. Never imported into the game or local DB.
export function viewerFixture(): Buffer {
  const bin = Buffer.alloc(76);
  [-0.5, 0, 0, 0.5, 0, 0, 0, 1, 0].forEach((n, i) =>
    bin.writeFloatLE(n, i * 4),
  );
  [0, 1, 2].forEach((n, i) => bin.writeUInt16LE(n, 36 + i * 2));
  [0, 1].forEach((n, i) => bin.writeFloatLE(n, 44 + i * 4));
  [0, 0, 0, 0, 0.2, 0].forEach((n, i) => bin.writeFloatLE(n, 52 + i * 4));
  const document = {
    asset: { version: "2.0", generator: "Arena3Q browser test fixture" },
    scene: 0,
    scenes: [{ nodes: [0] }],
    nodes: [{ name: "FixtureTriangle", mesh: 0 }],
    meshes: [
      {
        primitives: [{ attributes: { POSITION: 0 }, indices: 1, material: 0 }],
      },
    ],
    materials: [
      {
        pbrMetallicRoughness: {
          baseColorFactor: [0.85, 0.45, 0.15, 1],
          metallicFactor: 0,
          roughnessFactor: 1,
        },
        doubleSided: true,
      },
    ],
    buffers: [{ byteLength: 76 }],
    bufferViews: [
      { buffer: 0, byteOffset: 0, byteLength: 36, target: 34962 },
      { buffer: 0, byteOffset: 36, byteLength: 6, target: 34963 },
      { buffer: 0, byteOffset: 44, byteLength: 8 },
      { buffer: 0, byteOffset: 52, byteLength: 24 },
    ],
    accessors: [
      {
        bufferView: 0,
        componentType: 5126,
        count: 3,
        type: "VEC3",
        min: [-0.5, 0, 0],
        max: [0.5, 1, 0],
      },
      { bufferView: 1, componentType: 5123, count: 3, type: "SCALAR" },
      {
        bufferView: 2,
        componentType: 5126,
        count: 2,
        type: "SCALAR",
        min: [0],
        max: [1],
      },
      { bufferView: 3, componentType: 5126, count: 2, type: "VEC3" },
    ],
    animations: [
      {
        name: "FixtureMove",
        samplers: [{ input: 2, output: 3, interpolation: "LINEAR" }],
        channels: [{ sampler: 0, target: { node: 0, path: "translation" } }],
      },
    ],
  };
  const raw = Buffer.from(JSON.stringify(document));
  const json = Buffer.alloc(Math.ceil(raw.length / 4) * 4, 32);
  raw.copy(json);
  const glb = Buffer.alloc(12 + 8 + json.length + 8 + bin.length);
  glb.writeUInt32LE(0x46546c67, 0);
  glb.writeUInt32LE(2, 4);
  glb.writeUInt32LE(glb.length, 8);
  glb.writeUInt32LE(json.length, 12);
  glb.writeUInt32LE(0x4e4f534a, 16);
  json.copy(glb, 20);
  const offset = 20 + json.length;
  glb.writeUInt32LE(bin.length, offset);
  glb.writeUInt32LE(0x004e4942, offset + 4);
  bin.copy(glb, offset + 8);
  return glb;
}
