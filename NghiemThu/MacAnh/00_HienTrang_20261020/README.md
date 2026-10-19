# Mặc Ảnh – 00 Hiện trạng (2026-10-20 06:40) – **CHƯA ĐẠT · 47 %**

Đây là trạng thái trước khi Codex sửa theo phản hồi; dùng để đối chiếu. Chưa có gì để nghiệm thu đạt.

## Đã chạy
- Unity PlayMode (Claude tự chạy): `MacAnhCombatTests` 9/9, `MacAnhSpatialFxTests` 3/3, `SkillBattleTests` 14/14 – **gameplay đạt** (mốc sự kiện, combo 3 hit, khiên, tuyệt kỹ, huỷ khi choáng, pool).
- Chụp 7 chiêu trong trận thật (ải `attack_01`, Lv60, đứng trước quân phòng thủ, camera chơi ortho 4–5.5).

## Cần nhìn gì

### `1_TrongTran/` – thứ người chơi thấy
- `00_BangTongHop_7chieu_x_4pha.png`: mỗi hàng một chiêu, cột = chuẩn bị / tung / đỉnh / tan.
- Lỗi cần thấy: **Attack01, Attack02, Ultimate, DodgeAttack gần như không thấy hiệu ứng**; Skill02 là khối khói tím phủ lên quân (không ra dáng lốc); Skill01, Skill03 rất mờ; tím đậm chìm trên nền cát vàng; nhân vật nhỏ và tối.
- Một số khung cột 1–2 lệch tâm do camera theo tướng còn trễ – xem cột 3–4.

### `2_VFX_Preview/` – hiệu ứng 3D ở scene preview (chưa có trong trận)
- So từng `Review_FX_<Chiêu>.jpg` (3 thời điểm × 4 góc) với `00_Concept_Storyboard.png`.
- Lỗi cần thấy: thiếu phát sáng (khói mờ thay vì ánh sáng rực); mảng khói vuông biên cứng ở Attack01/02/DodgeAttack; Skill01/Skill02/Ultimate dùng chung một vòng trận; Ultimate 4 cột khói giống hệt, cách đều; Skill03 là quả trứng đứng thay vì vòm bao đồng đội.
- Ước lượng giống concept: Attack01 ~25 % · Attack02 ~20 % · Skill01 ~35 % · Skill02 ~40 % (trong trận ~15 %) · Skill03 ~50 % · Ultimate ~30 % · DodgeAttack ~20 %. Mục tiêu ≥ 90 %.
- Codex đã sửa một phần (mép khói mềm, cột / lốc bất đối xứng) sau các ảnh này; sẽ thấy ở đợt sau.

### `3_Model_TuThe/` – model ở các frame chiêu (Codex render 19/10, trước đợt sửa weight)
- `Run_006_Front.png`: chân trái gần như mất dưới vạt áo.
- `Ultimate_075_ThreeQuarter.png`: tóc dài phía sau thành vệt bậc thang; quạt xuyên tay áo phải.
- Còn thiếu ảnh góc sau / bên / trên và ảnh tô màu đỉnh lỗi – sẽ có ở đợt nghiệm thu.

### `4_SoDo/`
- `SurfaceAudit_Before.json` (đang dùng trong game): cạnh giãn tệ nhất ×167–287 ở mọi clip.
- `SurfaceAudit_Candidate8.json` (bản sửa thử): vẫn ×10–35, `passed: false`.
- `AppliedWeights.json`: bản thử dùng tới 16 xương / đỉnh.

## Điểm theo checklist §17 (MacAnh_Production_Spec_v1_1.md)

| Mục | Trọng số | Đạt |
|---|---|---|
| Model & màu | 10 | 6 |
| Vũ khí | 5 | 4 |
| Animation (gồm không méo mọi góc) | 20 | 6 |
| VFX (chiều sâu, ≥ 90 % concept, trong trận) | 25 | 4 |
| Sự kiện | 5 | 5 |
| Gameplay | 10 | 9 |
| Hiệu năng | 10 | 2 |
| Xuất file | 5 | 3 |
| Regression | 5 | 3 |
| Báo cáo | 5 | 5 |
| **Tổng** | 100 | **47 %** |

Chi tiết lỗi + hướng sửa đã gửi Codex: `AgentComms/codex/inbox/tu-claude_20261020-0620_*.md`, `tu-claude_20261020-0640_*.md`. Việc mới: 2 mức lưới (`20261020-0700_macanh-lod-*.md`).
