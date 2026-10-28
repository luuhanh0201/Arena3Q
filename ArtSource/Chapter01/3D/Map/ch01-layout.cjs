// Generates Assets/Arena3Q/Chapters/Chapter01_YellowTurban/Data/CH01_MapLayout.json (Chuong1.md §9-§18).
const fs = require('fs');
const out = require('path').join(__dirname, '../../../../Assets/Arena3Q/Chapters/Chapter01_YellowTurban/Data/CH01_MapLayout.json');
const P = (asset, x, z, yaw = 0, area = 0, extra = {}) => ({ asset, pos: [x, z], yaw, roll: extra.roll || 0, y: extra.y || 0, scale: extra.scale || 1, area });
const props = [];
const add = (area, list) => list.forEach(([a, x, z, yaw, extra]) => props.push(P(a, x, z, yaw || 0, area, extra || {})));
const wall = (area, pts, yaw) => pts.forEach(([x, z]) => props.push(P('lib:WoodenPalisadeFence', x, z, yaw, area, { scale: 1.1 })));  // 1.1: neighbouring 4 m pieces overlap, no gaps

// Ải 1 – Làng Bình An (góc trái dưới)
add(1, [
  ['village_gate', -63, -29.5, 90],
  ['house_b2', -63, -19, 90], ['house_b1', -53, -19, 0], ['house_b3', -42, -19, 0],
  ['house_k2', -56, -38.5, 180], ['house_k1', -43, -38.5, 180],
  ['well', -46, -32.5, 0],
  ['wagon', -37.5, -24.6, 30, { roll: 75 }], ['lib:Crate_Wooden', -35.8, -24.2, 20], ['lib:Bag', -36.6, -23.2, 50], ['lib:Barrel', -39.6, -23.6, 0, { roll: 90 }],
  ['cage', -59, -32.8, 10], ['cage', -57.2, -33.3, -15],
  ['lib:Barrel', -53, -32, 0], ['lib:Barrel', -52.2, -32.6, 40], ['lib:Crate_Wooden', -41, -32.5, 15], ['lib:FarmCrate_Empty', -40, -33.4, 60],
  ['lib:Bucket_Wooden_1', -45, -31.4, 0], ['bags', -38.5, -32.6, 70], ['lib:Bench', -48, -33.8, 90],
  ['lib:CommonTree_5', -60.5, -24.5, 0], ['lib:CommonTree_2', -36.5, -14, 120], ['lib:CommonTree_1', -66.5, -36, 40], ['lib:CommonTree_3', -34, -36.5, 200],
]);
// Ải 2 – Đồng Lúa và Kho Lương (giữa dưới)
add(2, [
  ['granary_v3', 6, -28, 0],
  ['wagon', -8, -37.2, 10], ['wagon', -18, -37.4, -5], ['bags', -6.3, -37.8, 20], ['bags', -16.3, -38, 80], ['lib:Bag', -9.8, -36.6, 0],
  ['lib:Crate_Wooden', 0.6, -24.4, 10], ['lib:Barrel', 0.7, -32, 0], ['bags', 0.9, -26.3, 0], ['lib:Bag', 1.1, -31, 30],
  ['lib:WoodenCampWatchtower', -16.5, -30.4, 0],
  ['lib:WoodenPalisadeFence', 4, -20.6, 0], ['lib:WoodenPalisadeFence', 8.2, -20.6, 0], ['lib:WoodenPalisadeFence', 12.4, -24.2, 90],
]);
// Ải 3 – Rừng Trúc (trái giữa)
add(3, [
  ['tent', -61, 9, 90], ['tent', -61, 1.5, 90],
  ['lib:WoodenCampWatchtower', -57, -1.5, 0],
  ['lib:WeaponStand', -58, 5.3, 90], ['campfire', -57.4, 7.2, 0],
  ['lib:Crate_Wooden', -63, 12.6, 0], ['lib:Barrel', -62.4, 13.7, 0], ['lib:Barrel', -59, 12.9, 0],
  ['lib:Rock_Medium_2', -44, 8.5, 30], ['lib:Rock_Medium_3', -46, -6.5, 80],
]);
wall(3, [[-65, 1], [-65, 5], [-65, 9]], 90);
// Ải 4 – Trại Lương và Thôn Trang (trung tâm)
add(4, [
  ['granary_v1', -14, -5, 90],
  ['wagon', -5, -1.5, 80], ['wagon', -2, -0.5, 95], ['wagon', 1, 0.5, 70], ['lib:Stall_Cart_Empty', -6.8, 1.8, 0],
  ['bags', -3.5, 1.2, 0], ['bags', 0, 1.9, 40], ['bags', -8, 0.4, 90],
  ['house_b2', -15, 13.5, 90], ['house_k3', -3, 15, 0],
  ['lib:MilitaryFlagpoleBanner', -7, -7.5, 0], ['lib:MilitaryFlagpoleBanner', -23, 3.5, 0], ['lib:MilitaryFlagpoleBanner', 7.8, 6.5, 0],
  ['tent', 9, 1.5, 0], ['tent', -24.5, 13, 90],
  ['cage', -5.5, -6.5, 0],
]);
wall(4, [[-22.5, -4], [-22.5, 0]], 90); wall(4, [[-18, -10.8], [-14, -10.8], [-10, -10.8]], 0);
// Ải 5 – Tiền Doanh (giữa trên)
add(5, [
  ['lib:WoodenCampWatchtower', -2.8, 22.6, 0], ['lib:WoodenCampWatchtower', 5.2, 22.6, 0],
  ['lib:ThreeKingdomsCommand_1', -7, 34, 0], ['drum', -2, 33.6, 30],
  ['lib:MilitaryFlagpoleBanner', -9.5, 37.8, 0], ['lib:MilitaryFlagpoleBanner', -4.5, 37.8, 0], ['lib:MilitaryFlagpoleBanner', -11.5, 24, 0],
  ['lib:WeaponStand', -11.6, 30, 90], ['lib:WeaponStand', -11.6, 32.6, 90],
  ['tent', -10, 26.8, 0], ['tent', 3, 37.2, 0], ['campfire', -3.2, 27.6, 0],
  ['lib:Barrel', -12.4, 38.6, 0], ['lib:Crate_Wooden', -11.4, 39.4, 20],
]);
wall(5, [[-12, 21], [-8, 21], [-4.4, 21]], 0); wall(5, [[-14, 23], [-14, 27], [-14, 31], [-14, 35], [-14, 39]], 90);
wall(5, [[-12, 41], [-8, 41], [-4, 41], [0, 41], [4, 41]], 0); wall(5, [[6, 27], [6, 35.5], [6, 39.5]], 90);
// Ải 6 – Đại Doanh Hoàng Cân (bên phải)
add(6, [
  ['camp_gate', 22, 2, 90],
  ['lib:WoodenCampWatchtower', 48.5, -8.5, 0], ['lib:WoodenCampWatchtower', 48.5, 12.5, 0], ['lib:WoodenCampWatchtower', 23.6, -8.6, 0], ['lib:WoodenCampWatchtower', 23.6, 12.4, 0],
  ['lib:Dummy', 26.8, -5.2, 0], ['lib:Dummy', 29, -5.2, 0], ['lib:Dummy', 31.2, -5.2, 0], ['lib:Dummy', 26.8, -2.6, 0],
  ['lib:WeaponStand', 33.6, -6.6, 90], ['lib:WeaponStand', 33.6, -3.2, 90],
  ['barracks', 41, -6, 0], ['barracks', 34.5, 10.2, 180],
  ['lib:ThreeKingdomsCommand_2', 46, 3, -90], ['drum_large', 46.5, 9.2, 20],
  ['lib:MilitaryFlagpoleBanner', 20.4, -6.8, 0], ['lib:MilitaryFlagpoleBanner', 20.4, 10.8, 0], ['lib:MilitaryFlagpoleBanner', 36, -1.4, 0], ['lib:MilitaryFlagpoleBanner', 49, 3, 0],
  ['campfire', 37.5, 5.5, 0], ['lib:Barrel', 47.5, -2.6, 0], ['lib:Crate_Wooden', 48.6, -1.8, 30],
]);
wall(6, [[22, -8], [22, 11.6]], 90); wall(6, [[24, -10], [28, -10], [32, -10], [36, -10], [40, -10], [44, -10], [48, -10]], 0);
wall(6, [[24, 14], [28, 14], [32, 14], [36, 14], [40, 14], [48, 14]], 0); wall(6, [[50, -8], [50, -4], [50, 0], [50, 4], [50, 8], [50, 12]], 90);
// Ải 7 – Tế Đàn Hoàng Cân (góc phải trên)
add(7, [
  ['plateau', 56, 31, 0], ['stairs', 56, 22.9, 180],
  ['lib:BaguaRoundStoneAltar', 56, 33, 0, { y: 2.6 }],
  ['lib:BaguaStonePillar', 56, 37.5, 0, { y: 2.6 }], ['lib:BaguaStonePillar', 52.1, 30.75, 120, { y: 2.6 }], ['lib:BaguaStonePillar', 59.9, 30.75, 240, { y: 2.6 }],
  ['lib:MilitaryFlagpoleBanner', 50.5, 37.5, 0, { y: 2.6 }], ['lib:MilitaryFlagpoleBanner', 61.5, 37.5, 0, { y: 2.6 }],
  ['cauldron', 56, 28.6, 0, { y: 2.6 }],
  ['lib:Torch_Metal', 53.3, 20.6, 0], ['lib:Torch_Metal', 58.7, 20.6, 0], ['lib:Torch_Metal', 52.6, 26.3, 0, { y: 2.6 }], ['lib:Torch_Metal', 59.4, 26.3, 0, { y: 2.6 }],
  ['cliff_a', 64.8, 31, 90], ['cliff_a', 56, 40.6, 0], ['cliff_b', 47.4, 36.5, 75], ['cliff_b', 65.2, 23.5, 100],
  ['outcrop', 64.2, 41.2, 0], ['outcrop', 48, 42, 50],
]);

const npc = (kind, area, list, y = 0) => list.map(([x, z]) => ({ kind, pos: [x, z], y, area }));
const npcs = [
  ...npc('chief', 1, [[-47, -31]]), ...npc('villager', 1, [[-59, -32.8], [-57.2, -33.3], [-36, -33.6], [-62, -26], [-50, -22.5]]),
  ...npc('yellow', 1, [[-55, -31], [-52.4, -31.4], [-54, -33], [-44, -31], [-41.2, -30.8]]),
  ...npc('militia', 2, [[-0.6, -25.6], [0.2, -27.6], [0.4, -29.8], [11.6, -26.6], [11.8, -29.2]]),
  ...npc('yellow', 3, [[-57.5, 3.8], [-59.2, 3], [-55.4, 9.6], [-56, -3.8], [-48, 11.2]]),
  ...npc('villager', 4, [[-5.5, -6.5], [-4.9, -7.2], [-3.6, -7.4]]), ...npc('yellow', 4, [[-9, -1.2], [-4, -4], [4, 3], [-12, 2.3], [-7.5, -5.2]]),
  ...npc('boss', 5, [[-4.5, 31]]), ...npc('yellow', 5, [[-6.6, 30.2], [-2.4, 30.2], [-6, 28.6], [-3, 28.4], [-2.8, 20.4], [5.2, 20.4]]),
  ...npc('yellow', 6, [[25, 0], [25, 4], [26.6, 0], [26.6, 4], [28, -3.4], [30.2, -3.4]]), ...npc('guardian', 6, [[43.4, 1], [43.4, 5]]),
  ...npc('boss', 7, [[56, 35.6]], 2.6), ...npc('guardian', 7, [[53.4, 33.6], [58.6, 33.6]], 2.6),
];

const layout = {
  size: [140, 90], skirt: 55, resolution: 1, seed: 7,
  road: { width: 3.4, depth: 0, points: [-72, -30, -63, -29.5, -52, -28, -40, -28.5, -33, -30.5, -26, -33, -16, -34.5, -6, -33.5, -2.5, -29, -2, -22, -4, -15, -12, -11.5, -20, -10, -30, -9, -40, -5, -47, 0, -52, 4.5, -54, 9, -49, 13, -40, 12, -30, 9, -21, 6.5, -13, 5, -4, 5.5, 3, 9, 5, 14, 2.5, 20, 0, 25, 0.5, 29, 7, 31.5, 14, 30, 17, 22, 14, 12, 14, 4, 18, 2, 22, 2, 30, 2, 37, 3, 41, 8, 44, 14, 47, 18.5, 52, 21, 56, 20.4] },
  paths: [
    { width: 2.2, depth: 0, points: [-48, -28, -48, -22, -48, -16] },
    { width: 2.2, depth: 0, points: [-49.5, -28.5, -49.5, -34, -49.5, -40] },
    { width: 2.4, depth: 0, points: [-2.5, -28.6, 0, -28.2, 1.8, -28] },
    { width: 2.2, depth: 0, points: [30, 2, 30, 4.5, 30, 6.8] },
    { width: 2.2, depth: 0, points: [37, 3, 39, 0, 41, -2.8] },
    { width: 2.2, depth: 0, points: [0.5, 29, -2, 30.5, -4.5, 31.5] },
    { width: 2.0, depth: 0, points: [-52, 4.5, -56, 5.5, -59, 6] },
  ],
  river: { width: 4.6, depth: 1.2, points: [-20, 105, -24, 75, -26, 47, -31, 34, -28, 22, -31, 12, -27, 2, -31, -9, -29, -20, -28, -32, -24, -47, -20, -75, -16, -105] },
  paddies: [[-21, -25], [-12, -25], [-21, -17], [-12, -17]].map(c => ({ centre: c, size: [8, 6] })),
  yards: [{ centre: [29, -4], size: [9, 7] }, { centre: [-4, 31], size: [18, 18] }, { centre: [-14, -4], size: [16, 12] }, { centre: [4, -28], size: [5, 6] }],
  meadows: [[-60, -40, 10], [-36, -16, 8], [-62, -14, 9], [-52, 5, 18], [-24, 18, 9], [12, -38, 10], [62, -30, 14], [30, 32, 10]].map(([x, z, r]) => ({ centre: [x, z], radius: r })),
  assets: [
    ['house_b1', 'CSStudio/bookstore/Bookstore.fbx', 5, 'CSStudio/bookstore/V1.png'], ['house_b2', 'CSStudio/bookstore/Bookstore.fbx', 5, 'CSStudio/bookstore/V2.png'],
    ['house_b3', 'CSStudio/bookstore/Bookstore.fbx', 5, 'CSStudio/bookstore/V3.png'], ['house_k1', 'CSStudio/butcher/Butcher.fbx', 4.5, 'CSStudio/butcher/V1.png'],
    ['house_k2', 'CSStudio/butcher/Butcher.fbx', 4.5, 'CSStudio/butcher/V2.png'], ['house_k3', 'CSStudio/butcher/Butcher.fbx', 4.5, 'CSStudio/butcher/V3.png'],
    ['granary_v1', 'CSStudio/horse_room/Horse_room.fbx', 4.5, 'CSStudio/horse_room/V1.png'], ['granary_v3', 'CSStudio/horse_room/Horse_room.fbx', 4.5, 'CSStudio/horse_room/V3.png'],
    ['wagon', 'MegaKit/Prop_Wagon.fbx', 1.4, 'MegaKit/T_WoodTrim_BaseColor.png'], ['bags', 'QuaterniusVillage/Bags.fbx', 0.7, null], ['well', 'QuaterniusVillage/Well.fbx', 2, null],
    ['cage', 'Assets/ThirdParty/Quaternius/FantasyPropsStandard/Exports/FBX/Cage_Small.fbx', 1.6, null],
    ['cauldron', 'Assets/ThirdParty/Quaternius/FantasyPropsStandard/Exports/FBX/Cauldron.fbx', 1, null],
    ['village_gate', 'kit:CH01_VillageGate', 0, null], ['camp_gate', 'kit:CH01_CampGate', 0, null], ['tent', 'kit:CH01_SoldierTent', 0, null],
    ['drum', 'kit:CH01_WarDrum', 0, null], ['drum_large', 'kit:CH01_WarDrumLarge', 0, null], ['cliff_a', 'kit:CH01_CliffWall_A', 0, null], ['cliff_b', 'kit:CH01_CliffWall_B', 0, null],
    ['outcrop', 'kit:CH01_CliffOutcrop', 0, null], ['plateau', 'kit:CH01_AltarPlateau', 0, null], ['stairs', 'kit:CH01_StoneStairs', 0, null],
    ['bridge', 'kit:CH01_WoodBridge', 0, null], ['barracks', 'kit:CH01_Barracks', 0, null], ['campfire', 'kit:CH01_Campfire', 0, null],
    ['bamboo0', 'kit:CH01_BambooClump_0', 0, null], ['bamboo1', 'kit:CH01_BambooClump_1', 0, null], ['bamboo2', 'kit:CH01_BambooClump_2', 0, null],
  ].map(([key, source, height, texture]) => ({ key, source, height, texture })),
  areas: [
    [1, 'Lang Binh An', -48, -28, 14], [2, 'Dong Lua va Kho Luong', -10, -27, 15], [3, 'Rung Truc', -54, 5, 13], [4, 'Trai Luong va Thon Trang', -8, 4, 13],
    [5, 'Tien Doanh', -4, 31, 12], [6, 'Dai Doanh Hoang Can', 36, 2, 16], [7, 'Te Dan Hoang Can', 56, 31, 12],
  ].map(([id, name, x, z, radius]) => ({ id, name, centre: [x, z], radius })),
  props,
  scatter: [
    { name: 'Forest beyond the map', zone: 'skirt', assets: ['lib:CommonTree_1', 'lib:CommonTree_2', 'lib:CommonTree_3', 'lib:Pine_1', 'lib:Pine_3', 'lib:Pine_5'], count: 320, spacing: 4.5, clearance: 0, scale: [0.9, 1.3] },
    { name: 'Bamboo forest', zone: 'disc', centre: [-51, 5], radius: 18, inner: 0, assets: ['bamboo0', 'bamboo1', 'bamboo2'], count: 30, spacing: 3.6, clearance: 1.6, scale: [0.9, 1.15] },
    { name: 'Trees', zone: 'wild', assets: ['lib:CommonTree_1', 'lib:CommonTree_2', 'lib:CommonTree_3'], count: 55, spacing: 6, clearance: 2.5, scale: [0.85, 1.2] },
    { name: 'Rocks', zone: 'wild', assets: ['lib:Rock_Medium_1', 'lib:Rock_Medium_2', 'lib:Rock_Medium_3'], count: 30, spacing: 6, clearance: 1.5, scale: [0.6, 1.3] },
    { name: 'Bushes', zone: 'wild', assets: ['lib:Bush_Common', 'lib:Bush_Common_Flowers', 'lib:Fern_1', 'lib:Plant_1_Big'], count: 110, spacing: 2.5, clearance: 1.2, scale: [0.8, 1.2] },
    { name: 'Village greenery', zone: 'disc', centre: [-48, -28], radius: 19, inner: 0, assets: ['lib:Bush_Common_Flowers', 'lib:Flower_3_Group', 'lib:Flower_4_Group', 'lib:Bush_Common'], count: 30, spacing: 2.2, clearance: 1, scale: [0.8, 1.1] },
    { name: 'Grass', zone: 'wild', assets: ['lib:Grass_Common_Short', 'lib:Grass_Common_Tall', 'lib:Grass_Wispy_Short', 'lib:Grass_Wispy_Tall', 'lib:Clover_1'], count: 260, spacing: 1.6, clearance: 0.8, scale: [0.8, 1.2] },
    { name: 'Flowers', zone: 'wild', assets: ['lib:Flower_3_Group', 'lib:Flower_4_Group'], count: 50, spacing: 3, clearance: 0.8 },
    { name: 'Pebbles', zone: 'wild', assets: ['lib:Pebble_Round_1', 'lib:Pebble_Round_3', 'lib:Pebble_Square_1'], count: 60, spacing: 2, clearance: 0.6 },
  ],
  npcs,
  views: [
    { name: '00_map_topdown', focus: [0, 0], size: 47, top: true },
    { name: '01_map_game_camera', focus: [5, 4], size: 55, top: false },
    { name: '11_area1_lang_binh_an', focus: [-49, -28], size: 15 }, { name: '12_area2_dong_lua_kho_luong', focus: [-8, -28], size: 15 },
    { name: '13_area3_rung_truc', focus: [-53, 5], size: 14 }, { name: '14_area4_trai_luong', focus: [-7, 4], size: 14 },
    { name: '15_area5_tien_doanh', focus: [-4, 31], size: 13 }, { name: '16_area6_dai_doanh', focus: [36, 2], size: 17 },
    { name: '17_area7_te_dan', focus: [56, 30], size: 13 },
  ],
};
fs.mkdirSync(require('path').dirname(out), { recursive: true });
fs.writeFileSync(out, JSON.stringify(layout, null, 1));
console.log('props', props.length, 'npcs', npcs.length);
