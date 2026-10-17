# Quân địch khi đi ải – ý tưởng hình ảnh, prompt và chỉ số gợi ý

Phiên bản: v0.1 · 2026-10-17 · Trạng thái: **🟡 ý tưởng chờ duyệt**. Chỉ số là **gợi ý để chơi thử**, cân bằng để giai đoạn sắp phát hành (spec §13 ⏳). Kỹ năng của tinh anh / boss **chưa có trong spec và chưa code** (⏳).

Đã chốt (người dùng, 2026-10-17): địch gồm **người + yêu quái**; ải **chia chương theo vùng**, mỗi chương một phe địch riêng (lính thường, tinh anh, boss cuối chương); làm trước **3 chương**.

| Chương | Vùng | Phe địch | File |
|---|---|---|---|
| 1 | Rừng trúc – đèo núi | **Hắc Phong Trại** – sơn tặc | `Chuong1_SonTac.md` |
| 2 | Đồng bằng hoang – làng bỏ hoang | **Hoàng Cân Tà Đạo** – phản quân khăn vàng dùng tà thuật | `Chuong2_HoangCan.md` |
| 3 | Âm Sơn – núi sương, đầm lầy ma | **Yêu quái Âm Sơn** | `Chuong3_YeuQuai.md` |

Dùng chung cả 3 chương: `Animation.md` (xương, clip, mốc kỹ năng) · `VFX_Prompts.md` (prompt hiệu ứng).

Mạch truyện gợi ý: dẹp sơn tặc (người thường) → phản quân dùng bùa chú gọi tà khí (nửa người nửa huyền bí) → lần theo tà khí tới Âm Sơn, nơi yêu quái thật sự thức tỉnh. Độ "huyền huyễn" tăng dần qua từng chương.

---

## 1. Cấp bậc quân địch

| Cấp | Kích thước trên màn hình (đơn vị) | Bán kính thân logic | Máu so với lính thường | Có kỹ năng | Mức chi tiết hình |
|---|---|---|---|---|---|
| **Thường** | **0.8 × 1.1** (map spec §8.5) | 0.30–0.32 | ×1 | không | đơn giản, đọc rõ ở ~140 px |
| **Tinh anh** | **1.1 × 1.5** | 0.40 | ×4–6 | **1 kỹ năng** | chi tiết hơn, có **viền sáng vàng** nhẹ (game vẽ) |
| **Boss** | **2.0 × 2.6** | 0.80 | ×30–60 (≈ 3–6 lần chủ tướng) | **2–3 kỹ năng + đổi pha ở 50 % máu** | chi tiết cao, có **thanh máu riêng** trên HUD |

**Mốc chỉ số hiện có** (asset trong `Assets/Arena3Q/Content/Troops`): lính ta 120 máu / 12 sát thương / 1.0 đòn/s; kiếm binh địch 160 / 16; cung thủ địch 90 / 13, tầm 4.5; lực sĩ địch 600 / 35, chậm 1.6; chủ tướng 900 / 45. Chỉ số trong các file chương dùng cùng thang này và là chỉ số ở **cấp đầu chương**; mỗi cấp ải +3 % Máu và Công, chương N bắt đầu ở cấp 1 + 9 × (N − 1) (`docs/KeHoach/HeroProgression.md` §10).

**Vai trò** (mỗi chương có đủ để ải đa dạng): **cận chiến** · **tầm xa** · **trâu (tank)** · **nhanh / bầy đàn** · tinh anh **hỗ trợ / khống chế** · tinh anh **đột phá** · **boss**.

---

## 2. Luật kỹ năng địch

Đã đưa vào spec (người dùng chốt 2026-10-17): **`docs/GameplaySpecification.md` §8 "Quân địch: cấp bậc và kỹ năng"** – cảnh báo vùng đỏ trước khi đánh, khống chế không xoá lệnh của người chơi, kéo/hất văng tuân theo vùng cấm, boss đổi pha ở 50 % máu, boss gác thành (ải Công) / đợt cuối (ải Thủ), dữ liệu `EnemyDefinition` / `EnemySkillDefinition`. Còn **2 xung đột chờ chốt** (spec §13): khiêu khích (Hộ Pháp Đồng Giáp) và đơn vị bay (Quỷ Hoả).

---

## 3. Hình ảnh

- **Phong cách A** (giống chủ tướng): chibi ~2.5 đầu, render 3D kiểu mô hình figure, theo `docs/art/concepts/chutuong1.png`. Yêu quái vẫn **dễ thương – hơi đáng sợ** (không máu me, không ghê rợn) để hợp game mobile mọi lứa tuổi.
- **Phân biệt với quân ta**: quân ta mang màu đội **xanh**; quân địch mỗi chương có **màu phe riêng** (đen–đỏ, vàng đất, tím–xanh ma) và tông **tối hơn**.
- **Góc nhìn**: như chủ tướng – **trước-phải 3/4** và **sau-phải 3/4**, nhìn xuống ~40°, lật gương ra trái.
- **Animation**: lính thường dùng **bộ xương đơn giản** (~10 mảnh: đầu, thân, 2 tay, 2 chân, vũ khí, phụ kiện) và **dùng chung bộ clip** giữa các lính cùng dáng; tinh anh và boss có bộ xương riêng đầy đủ như chủ tướng (`docs/Tuong/StrategistArtGuide.md`). Chi tiết khung xương, bộ clip, mốc `Telegraph` / `Cast` của từng kỹ năng: **`Animation.md`**.
- **VFX**: vùng cảnh báo đỏ, chết / xuất hiện, kỹ năng tinh anh và boss – prompt dán sẵn ở **`VFX_Prompts.md`**.
- **Prompt**: mỗi file chương có prompt **dán sẵn** cho từng loại + một prompt **xếp hàng cả chương** để kiểm tra đồng bộ. Công cụ: ChatGPT, đính kèm `chutuong1.png`.
- **Lưu ảnh**: `docs/QuanDich/art/` – tên `<chương>_<loại>_<kiểu ảnh>.png`, ví dụ `c1_saber_sheet.png`, `c1_boss_turnaround.png`, `vfx_c1_boss_sheet.png`.

| Kiểu ảnh | Dùng cho | Nội dung |
|---|---|---|
| `sheet` | thường, tinh anh | 3 hình: trước-phải 3/4, sau-phải 3/4 (nhìn xuống ~40°), 1 tư thế tấn công |
| `turnaround` | boss | 4 hướng ngang tầm mắt (trước, nghiêng, sau, A-pose) – giống chủ tướng |
| `rig_front` / `rig_back` | boss (và tinh anh nếu cần) | tư thế chuẩn để tách bộ phận – dùng prompt ảnh 2, 3 trong `docs/art/concepts/MuuSi_Prompts.md`, thay mô tả nhân vật |
| `lineup` | cả chương | mọi loại đứng thành hàng, đúng tỉ lệ kích thước |
