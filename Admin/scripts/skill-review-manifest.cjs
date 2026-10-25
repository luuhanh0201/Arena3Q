const fs = require("node:fs");
const path = require("node:path");
const root = path.resolve(__dirname, "..");
const db = JSON.parse(
  fs.readFileSync(path.join(root, ".local/db.json"), "utf8"),
);
const themes = {
  liep_sat:
    "a sharp eagle eye sighting a wounded shadow target, bronze and forest green",
  doc_tien: "a single arrowhead dripping emerald poison, green mist",
  ung_kich: "a diving golden eagle with spread talons, amber and teal",
  xuyen_duong_tien:
    "a brilliant long arrow piercing a radiant sun disc, gold and cyan",
  xung_phong:
    "a charging warhorse silhouette emerging from a rushing silver wind trail",
  ma_dap: "a horse hoof slamming stone into a circular gold shockwave",
  phong_ma: "a galloping horse made of pale turquoise wind and flowing mane",
  thien_quan_dot_kich:
    "three armored horsemen charging as a single golden spearhead",
  kien_thu: "a massive bronze Chinese tower shield with a steady amber aura",
  thuan_kich: "a shield edge striking sparks from a shadowed helmet",
  ho_ve_tran:
    "three overlapping protective amber shields around a small battle formation",
  kim_thanh_thiet_bich:
    "a shining fortress shield dome with ancient bronze patterns, unbreakable golden ramparts",
  kiem_tam:
    "two crossed silver swords enclosing a glowing crimson heart-shaped energy core, no anatomical gore",
  phi_anh_dot_kich:
    "a twin-sword fighter silhouette dashing through a violet-blue afterimage",
  toan_phong_tram: "two curved silver swords tracing a circular blue whirlwind",
  van_kiem_quy_tong:
    "a fan-shaped storm of luminous falling swords converging into the earth",
  lubu_cuong_huyet:
    "five crimson energy sparks surrounding a fierce golden ancient war mask, scarlet battle aura, no blood",
  lubu_phuong_thien_tram:
    "a dark steel Chinese crescent halberd sweeping a broad crimson and gold crescent",
  lubu_xich_tho_xung_phong:
    "a majestic scarlet Red Hare horse charging through red and gold smoke, protective shield glint",
  lubu_thien_ha_vo_song:
    "an ornate Chinese halberd striking the earth from above in a dramatic scarlet golden radial blast",
  lien_tram: "five interlocking crimson saber slashes forming a rising combo",
  bat_dao_tram:
    "a single curved Chinese saber drawn from its sheath in a brilliant diagonal silver flash",
  dao_khi:
    "one broad turquoise crescent blade of sword energy flying through dark mist",
  cuong_dao_luc_tram:
    "six distinct curved saber energy cuts forming a fierce red and silver starburst",
  ung_nhan:
    "an eagle eye integrated into a golden recurve bow and four tiny glints",
  lien_chau_tien:
    "three polished arrows flying parallel through cyan motion trails",
  tien_vu: "a dense rain of silver arrows descending through an amber circle",
  pha_thanh_hoa_tien:
    "one flaming arrow exploding against a stone battlement in orange gold",
  quan_lenh_nhu_son:
    "a gold command banner backed by a mountain-shaped steel shield and disciplined soldiers",
  hieu_trieu:
    "a bronze war horn calling three small soldier silhouettes into a golden formation",
  trong_tran:
    "a red lacquer Chinese war drum sending concentric golden sound waves",
  thiet_ky_xung_phong:
    "a disciplined line of steel-armored cavalry charging through a deep blue gold battle lane",
  thao_luoc:
    "an open ancient military strategy scroll with jade formation stones and violet light",
  huyen_co_tran:
    "an intricate geometric violet and gold magic formation ring hovering above a folded feather fan",
  phong_vu_khon_tran:
    "an organic violet smoky cyclone with broad uneven wisps and scattered pale feathers, no hard spiral rings or laser beams",
  van_tuong_thien_co:
    "a violet and gold magic formation expanding around protective feathers with healing jade light",
  tam_vu_lien_kich:
    "three bright violet feather projectiles fanning outward from a refined feather fan",
  thien_muu_ho_tran:
    "a violet translucent curved shield surrounding a refined feather fan",
  anh_bo_phan_kich:
    "a pale violet afterimage sliding backward while a feather fan sends one sharp counter-slash",
};
Object.assign(themes, {
  bat_quai_tran:
    "a violet and jade octagonal Bagua magic array trapping small shadow silhouettes inside a luminous circle",
  dong_phong:
    "a broad irregular gust of pale jade and violet wind blowing across a battlefield with flowing banners, no laser beams",
  hoa_ke:
    "a circular strategic fire trap with rising orange flame around ancient formation stones",
});
const skills = new Map();
for (const hero of db.heroes)
  for (const skill of hero.sections.skills) {
    let item = skills.get(skill.id);
    if (!item) {
      item = {
        id: skill.id,
        name: skill.name,
        description: skill.description,
        heroes: [],
        status: "PENDING_APPROVAL",
        subject: themes[skill.id],
      };
      skills.set(skill.id, item);
    }
    item.heroes.push(hero.displayName);
  }
const folder = path.resolve(
  root,
  "../NghiemThu/Admin/XemTruoc_SkillIcons_20261025",
);
fs.mkdirSync(folder, { recursive: true });
const items = [...skills.values()];
if (items.some((s) => !s.subject))
  throw new Error("Every source skill needs a distinct visual brief");
for (const item of items)
  if (fs.existsSync(path.join(folder, item.id + ".png")))
    item.file = item.id + ".png";
fs.writeFileSync(
  path.join(folder, "manifest.json"),
  JSON.stringify(
    { status: "PENDING_APPROVAL", applied: false, skills: items },
    null,
    2,
  ) + "\n",
);
console.log(JSON.stringify(items));
