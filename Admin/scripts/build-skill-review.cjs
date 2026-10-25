const fs = require("node:fs");
const path = require("node:path");
const folder = path.resolve(
  __dirname,
  "../../NghiemThu/Admin/XemTruoc_SkillIcons_20261025",
);
const manifest = JSON.parse(
  fs.readFileSync(path.join(folder, "manifest.json"), "utf8"),
);
const escape = (value) =>
  String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll('"', "&quot;");
const cards = manifest.skills
  .map(
    (skill, index) =>
      `<article><a href="${escape(skill.file)}" target="_blank"><img src="${escape(skill.file)}" alt="${escape(skill.name)}" width="256" height="256"></a><div><strong>${String(index + 1).padStart(2, "0")} · ${escape(skill.name)}</strong><p>${escape(skill.heroes.join(" / "))}</p><small>${escape(skill.id)}</small><details><summary>Mô tả chiêu</summary><p>${escape(skill.description ?? "")}</p></details></div></article>`,
  )
  .join("");
const count = manifest.skills.length;
const applied = manifest.applied === true;
const approved = applied || manifest.status === "APPROVED";
const heading = `${count} ảnh kỹ năng ${applied ? "đã áp dụng" : approved ? "đã duyệt" : "chờ duyệt"}`;
const notice = applied
  ? "Bạn đã duyệt toàn bộ bộ ảnh. Đã gắn vào kỹ năng Unity local và Admin; lần build tiếp theo sẽ có bộ ảnh này."
  : approved
    ? "Bạn đã duyệt toàn bộ bộ ảnh. Đang áp dụng vào Admin và Unity local."
    : "Đây là bản dự thảo để bạn duyệt. Chưa gắn ảnh mới vào Admin hoặc Unity. Bạn có thể duyệt cả bộ hoặc nêu số ảnh cần sửa.";
const html = `<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Arena3Q · Duyệt 42 ảnh kỹ năng</title><style>body{margin:0;padding:32px;background:#101827;color:#e4ecf7;font:14px/1.65 system-ui,sans-serif}main{max-width:1600px;margin:auto}h1{font-size:30px;color:#edc185}p{color:#a6b6ce}.note{padding:18px;border:1px solid #a88345;border-radius:12px;color:#edc185;background:#b58a3514}.grid{display:grid;grid-template-columns:repeat(6,minmax(0,1fr));gap:18px;margin-top:24px}article{border:1px solid #30425b;border-radius:12px;overflow:hidden;background:#162132}article img{display:block;width:100%;height:auto;aspect-ratio:1;object-fit:contain}article>div{padding:12px}strong{font-size:13px}article p{font-size:11px;margin:6px 0}small{font-size:9px;color:#7790b1;overflow-wrap:anywhere}details{font-size:11px;color:#9eaec3;margin-top:8px}a{color:#edc185}@media(max-width:1200px){.grid{grid-template-columns:repeat(4,minmax(0,1fr))}}@media(max-width:700px){body{padding:16px}.grid{grid-template-columns:repeat(2,minmax(0,1fr))}}</style><main><h1>Arena3Q · 42 ảnh kỹ năng chờ duyệt</h1><p>Đánh số 01–42 theo ID kỹ năng. Bấm ảnh để xem PNG nguyên bản. Bao gồm 39 kỹ năng đang được tham chiếu và 3 kỹ năng nguồn cũ chưa gán.</p><div class="note">Đây là bản dự thảo để bạn duyệt. Chưa gắn ảnh mới vào Admin hoặc Unity. Bạn có thể duyệt cả bộ hoặc nêu số ảnh cần sửa.</div><div class="grid">${cards}</div></main></html>`;
const reviewedHtml = html
  .replace("Arena3Q · Duyệt 42 ảnh kỹ năng", `Arena3Q · ${heading}`)
  .replace("Arena3Q · 42 ảnh kỹ năng chờ duyệt", `Arena3Q · ${heading}`)
  .replace(
    "Đây là bản dự thảo để bạn duyệt. Chưa gắn ảnh mới vào Admin hoặc Unity. Bạn có thể duyệt cả bộ hoặc nêu số ảnh cần sửa.",
    notice,
  );
fs.writeFileSync(path.join(folder, "xem_anh_ky_nang.html"), reviewedHtml);
console.log(path.join(folder, "xem_anh_ky_nang.html"));
