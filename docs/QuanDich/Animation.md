# Quân địch – animation, xương và mốc kỹ năng

Phiên bản: v0.1 · 2026-10-17 · **🟡 bản nháp** (thời lượng, mốc sự kiện, tốc độ đạn là gợi ý để chơi thử). Đi kèm:
- Thiết kế từng loại, chỉ số, kỹ năng: `Chuong1_SonTac.md`, `Chuong2_HoangCan.md`, `Chuong3_YeuQuai.md` · luật kỹ năng địch: spec §8 "Quân địch: cấp bậc và kỹ năng".
- Prompt VFX: `VFX_Prompts.md` (cùng thư mục) · quy ước animation của tướng: `docs/LordAnimations.md` §0 · quy trình Krita + Skinning Editor: `docs/StrategistArtGuide.md` §3–4.

---

## 0. Quy ước chung

### 0.1 Giống tướng
- Hai hướng **trước-phải** / **sau-phải** + lật gương; đòn đánh thường `attack` vẽ ở **nhịp chuẩn 1 đòn/giây**, **`Hit` @ 0.4 s**, Animator chạy với tốc độ = **tốc đánh** của loại đó (bảng chỉ số trong file chương).
- 🟡 Đánh tầm xa: tại `Hit` sinh **đạn**, sát thương khi đạn tới (spec §6). Tốc độ đạn ghi trong bảng từng loại.

### 0.2 Riêng quân địch
- **`run` theo tốc chạy:** clip vẽ ở tốc chạy chuẩn của loại đó; Animator chạy với tốc độ = tốc chạy hiện tại / tốc chạy chuẩn (bị làm chậm thì bước chậm theo).
- **Lính thường không đổi mặt** (1 mặt). `hit` = **nháy trắng** (shader) + giật 0.2 s; `die` = ngã 0.5 s rồi **khói "poof"** (`vfx_enemy_common`) và biến mất – không để xác, không máu.
- **Tinh anh:** viền sáng vàng do **shader của game** vẽ – không vẽ vào ảnh.
- **Bầy đàn** (chó săn, bù nhìn, tiểu quỷ): `idle` / `run` bắt đầu ở **thời điểm ngẫu nhiên** để cả bầy không cử động đồng loạt.
- **Quân được gọi ra** (lâu la, bù nhìn rơm, phân thân) chạy clip **`spawn`** 0.5 s trước khi hoạt động.
- `stunned`: lắc lư tại chỗ + sao choáng (`vfx_common` của tướng, `docs/art/concepts/VFX_Prompts.md` §0).
- `cheer` (tuỳ chọn): nhảy mừng khi người chơi thua.

### 0.3 Kỹ năng địch: cảnh báo → ra đòn
Kỹ năng có cảnh báo (vùng tròn / quạt / đường thẳng, nhắm một mục tiêu) dùng **hai clip** để thời gian cảnh báo chỉnh trong dữ liệu mà không phải sửa animation:

| Clip | Lặp | Sự kiện | Nội dung |
|---|---|---|---|
| `skill_windup` | có | **`Telegraph`** ở khung đầu | tư thế lấy đà; game vẽ **vùng đỏ cố định** (spec §8 ✅) |
| `skill_cast` | – | **`Cast`** | ra đòn; hiệu lực xảy ra ở `Cast` |

- Vùng đỏ giữ tới đúng `Cast`. Game chạy `skill_windup` trong (**thời gian cảnh báo − thời điểm `Cast` trong `skill_cast`**) rồi chuyển sang `skill_cast` → tổng thời gian từ lúc hiện vùng đỏ tới lúc có hiệu lực **đúng bằng thời gian cảnh báo** (🟡 1 s; nhắm một mục tiêu 🟡 0.5 s).
- Hiệu ứng **rơi từ trên cao** (sét, cầu lửa) bắt đầu rơi trước `Cast` để chạm đất đúng lúc `Cast`.
- Kỹ năng không cảnh báo (hỗ trợ, triệu hồi, tự tăng sức): chỉ `skill_cast`.
- 🟡 Đề xuất, giống luật của quân ta (spec §3): địch bị **khống chế hoặc chết trước `Cast`** → kỹ năng **huỷ**, vùng đỏ biến mất, **không** vào hồi chiêu.
- Kỹ năng làm địch **tự di chuyển** (Ngưu Ma Xung) đi theo đúng vạch cảnh báo, **dừng ở mép vùng cấm / biên bản đồ**.

---

## 1. Khung xương dùng chung cho lính thường

Lính thường dùng **bộ xương đơn giản ~10 mảnh** (`README.md` §3); mỗi tay / chân là **1 sprite gắn 2 xương** (Skinning Editor tự uốn khớp). PSD cao ~180 px (hình trên màn hình 0.8 × 1.1 đơn vị), PPU như tướng.

| Khung | Mảnh (lớp `F_`, dưới → trên) | Xương | Dùng cho |
|---|---|---|---|
| **T1 Người** | `arm_far` · `leg_far` · `body` · `pelvis` · `leg_near` · `head` (kèm mặt) · `hat` (mũ / khăn) · `arm_near` · `weapon` · `extra` (ống tên, túi bùa…) | `root → hip → spine → chest → neck → head → hat`; `chest → arm_near_upper → arm_near_lower → weapon`; `chest → arm_far_upper → arm_far_lower`; `hip → leg_*_upper → leg_*_lower` · IK 2 chân | Lâu La Đao, Lâu La Cung, Tín Đồ, Phù Thuỷ Tán Bùa, Tiểu Quỷ |
| **T1L Người to** | như T1, thân rộng ×1.5 | như T1 + IK `hand_far` bám `weapon` (vũ khí hai tay) | Đại Hán Búa, Lực Sĩ Cuồng Tín, Thạch Tinh (tinh thể vai là con của `chest`) |
| **T2 Thú 4 chân** | `tail` · `leg_back_far` · `leg_front_far` · `body` · `leg_back_near` · `leg_front_near` · `head` · `scarf` | `root → body → neck → head`; `body → tail_1 → tail_2`; `body → leg_*_upper → leg_*_lower` · IK 4 chân | Chó Săn Trại |
| **T3 Lơ lửng** | `shadow` (con của `root`, nằm đất) · `tail` · `flame_body` · nhóm `face` (`calm`, `spit`) | `root → float → flame_1 → flame_2 → flame_3`; `float → tail_1 → tail_2` | Quỷ Hoả |
| **T4 Nhảy** | như T1 nhưng **hai chân 1 mảnh** `legs`, tay duỗi trước 1 mảnh `arms` | `hip → legs`; `chest → arms_1 → arms_2`; `head → talisman` (lá bùa phất phơ) | Cương Thi |
| **T5 Một chân** | như T1 nhưng chân là **1 cọc gỗ** `pole` | `hip → pole` | Bù Nhìn Rơm |

**Dùng chung clip:** các loại cùng khung và cùng tên xương dùng **chung một clip**; tỉ lệ khác nhiều thì chép clip rồi chỉnh lại (Animator Override Controller cho từng loại).

---

## 2. Bộ clip lính thường

| Bộ clip | `idle` | `run` | `attack` (`Hit` @ 0.4) | `die` | Khác |
|---|---|---|---|---|---|
| **melee_slash** | 1.5 s, nhún vai | 0.5 s | giơ vũ khí qua vai → chém xuống | ngã ngửa + poof | – |
| **melee_thrust** | 1.5 s | 0.5 s | kéo giáo về hông → đâm thẳng | ngã + poof | – |
| **melee_heavy** | 2.0 s, thở nặng | 0.7 s, `Step` (rung nhẹ) | vác vũ khí lên cao → **bổ xuống đất** | ngã sấp chậm + poof lớn | – |
| **ranged_bow** | 1.5 s | 0.5 s | rút tên → giương → buông | ngã + poof | – |
| **ranged_throw** | 1.5 s | 0.5 s | vung tay ra sau → ném | ngã + poof | – |
| **beast** | 1.2 s, thở, vẫy đuôi | 0.4 s phi 4 chân | chồm tới **cắn** | lăn ra + poof | `spawn`: tru (tuỳ chọn) |
| **hopper** | 1.5 s, lắc lư tại chỗ | 0.6 s **nhảy bật**, `Step` khi chạm đất | hai tay duỗi **chọc** tới | đổ thẳng ra sau (không poof nếu sẽ sống lại) | `revive` 1.0 s |
| **float** | 1.2 s, lơ lửng nhấp nhô, lửa lay | dùng `idle` + nghiêng về hướng đi | phồng má → **phun** cầu lửa | co lại, tắt phụt thành khói | – |

### 2.1 Từng loại
| Chương | Loại | Khung | Bộ clip | Tại `Hit` | VFX |
|---|---|---|---|---|---|
| 1 | Lâu La Đao | T1 | melee_slash | – | `vfx_c1_basic`: vệt chém |
| 1 | Lâu La Cung | T1 | ranged_bow | mũi tên (đạn **15**) | `vfx_c1_basic`: mũi tên lông đỏ |
| 1 | Đại Hán Búa | T1L | melee_heavy | – | `vfx_c1_basic`: bụi + nứt đất |
| 1 | Chó Săn Trại | T2 | beast | – | `vfx_c1_basic`: vết cắn |
| 2 | Tín Đồ Khăn Vàng | T1 | melee_thrust | – | `vfx_c2_basic`: vệt đâm |
| 2 | Phù Thuỷ Tán Bùa | T1 | ranged_throw | lá bùa cháy (đạn **10**) | `vfx_c2_basic`: bùa lửa xanh lục |
| 2 | Lực Sĩ Cuồng Tín | T1L | melee_heavy | – | `vfx_c2_basic`: va chạm chuỳ gai |
| 2 | Bù Nhìn Rơm | T5 | melee_slash (tay que quất), `run` nhảy lò cò như hopper | – | `vfx_c2_basic`: rơm bung khi chết (thay poof) |
| 3 | Tiểu Quỷ | T1 | melee_slash | – | `vfx_c3_basic`: sao "bốp" |
| 3 | Cương Thi | T4 | hopper | – | `vfx_c3_basic`: bụi nhảy · `vfx_c3_revive` |
| 3 | Quỷ Hoả | T3 | float | cầu lửa ma (đạn **8**) | `vfx_c3_basic`: cầu lửa xanh |
| 3 | Thạch Tinh | T1L | melee_heavy (đấm hai tay) | – | `vfx_c3_basic`: mảnh đá + tinh thể tím |

- **Cương Thi sống lại** (🟡 30 %): `die` dừng ở tư thế nằm, 1.0 s sau chạy `revive` (bật thẳng dậy như lò xo, lá bùa loé sáng) – VFX `vfx_c3_revive`. Không sống lại thì poof như thường.
- **Quỷ Hoả** bay hay không: chờ chốt (spec §13 mục 2) – animation không đổi, chỉ khác luật đi.
- Yêu quái chương 3 chết bằng **khói tím** (`vfx_c3_basic`) thay khói xám.

---

## 3. Tinh anh

Bộ xương **đầy đủ như tướng** (`docs/LordAnimations.md`: tay, chân 2 đoạn, IK, vạt áo / áo choàng có xương), PSD cao ~240 px (hình 1.1 × 1.5). Có **4 mặt** (`calm`, `shout`, `hurt`, `ko`). Clip: `idle` 2.0 · `run` · `attack` · `hit` 0.3 · `die` 1.0 (+ poof lớn) · `stunned` · `spawn` 0.8 · `skill_windup` · `skill_cast` (mục 0.3).

| Chương | Tinh anh · kỹ năng | `skill_windup` (lặp, `Telegraph`) | `skill_cast` | `Cast` | VFX |
|---|---|---|---|---|---|
| 1 | **Đầu Mục Song Phủ** · Cuồng Phủ Xoáy | khom người, hai rìu bắt chéo, vai xoay lấy đà, `shout` · vùng đỏ **tròn r 1.8** quanh mình | 1.5 s xoay tròn tại chỗ, hai rìu dang rộng | @ 0 / 0.25 / … / 1.25 (6 nhịp) | `vfx_c1_axes`: vòng chém đỏ đen @ `root` |
| 1 | **Quân Sư Sơn Trại** · đánh thường | – | – | `Hit` @ 0.4: ném phi tiêu (đạn **15**) | `vfx_c1_advisor`: phi tiêu |
| 1 | **Quân Sư Sơn Trại** · Rượu Mạnh Tiếp Sức (không cảnh báo) | – | 0.9 s: giơ hồ lô uống một ngụm rồi vẩy rượu lên trời, cười | @ 0.6 | `vfx_c1_advisor`: giọt rượu, vòng cam ấm r 4, mũi tên đỏ trên quân được tăng 5 s |
| 2 | **Hộ Pháp Đồng Giáp** · Kim Cương Hộ Thể (không cảnh báo) | – | 0.5 s giơ khiên, khom gối → giữ clip **`skill_hold`** (lặp) suốt 4 s | @ 0.3 | `vfx_c2_guardian`: vòm vàng bát quái 4 s; ⚠️ dấu khiêu khích trên quân ta **chỉ làm nếu** luật khiêu khích được chốt (spec §13 mục 1) |
| 2 | **Đại Phương Sư** · đánh thường | – | – | `Hit` @ 0.4: tia sét nhỏ (đạn **30**, gần như tức thì) | `vfx_c2_sorcerer`: tia sét ngang |
| 2 | **Đại Phương Sư** · Thiên Lôi Phù | chĩa kiếm gỗ đào lên trời, bùa bay vòng quanh · **3 vùng tròn r 1.2** tại quân ta | 0.5 s chém kiếm xuống | @ 0.15 | `vfx_c2_sorcerer`: lá bùa lơ lửng trên mỗi vùng → sét đánh xuống, đất cháy xém; sao choáng 1 s |
| 3 | **Ngưu Đầu** · Ngưu Ma Xung | cào chân, khịt mũi, cúi đầu chĩa sừng · **vạch đỏ thẳng dài 5** (rộng 1.2) | 0.8 s lao theo vạch (code đẩy vị trí), dừng, lắc đầu | @ 0 (bắt đầu lao, trúng theo đường) | `vfx_c3_ox`: vệt bụi, va chạm sừng, vệt hất văng |
| 3 | **Mã Diện** · Câu Hồn Tỏa | quay xích trên đầu · **dấu ngắm đỏ** trên tướng bị nhắm (0.5 s) | 0.6 s quăng xích → giật về | @ 0.2 (xích bay, đạn **18**) | `vfx_c3_horse`: xích + móc bay; trúng: kéo, vòng xích trói 1.5 s |

---

## 4. Boss

Bộ xương **đầy đủ** như tướng + phần riêng; PSD cao ~420 px (hình 2.0 × 2.6). Mặt: `calm`, `shout`, `hurt`, `ko` + **`rage`** (pha 2).

| Clip | Độ dài | Ghi chú |
|---|---|---|
| `entrance` | 2.0 s | xuất hiện (ải Thủ: đợt cuối; ải Công: khi trận bắt đầu) – thanh máu boss trên HUD hiện cùng lúc; `vfx_enemy_common`: vòng sóng + bụi |
| `idle`, `run`, `attack` | như tướng | `attack`: `Hit` @ 0.4, tua theo tốc đánh |
| `hit` | – | boss **không giật**, chỉ nháy trắng |
| `die` | 2.0 s | ngã chậm, nổ khói lớn, `ko` |
| `stunned` | 0.6 s | chỉ dùng nếu boss chịu choáng (câu hỏi `docs/NormalHeroes.md` §5 mục 3) |
| `skillN_windup` / `skillN_cast` | – | mục 0.3 |
| **`phase2`** | 1.5 s | khi máu < 🟡 50 % (spec §8 ✅ "có hiệu ứng nhận biết"); 🟡 trong lúc chạy clip boss không đánh, không dùng kỹ năng, **vẫn nhận sát thương**; `vfx_enemy_common`: sóng đỏ sẫm |

### 4.1 Hắc Phong Trại Chủ (chương 1 · cửu hoàn đao · tầm 1.2 · tốc đánh 0.7)
Xương thêm: `blade → blade_tip`, `rings_1…3` (vòng sắt trên sống đao lắc lư), `cloak_1 → cloak_2` (áo choàng da gấu).

| Kỹ năng | `windup` (`Telegraph`) | `cast` | `Cast` | VFX |
|---|---|---|---|---|
| đánh thường | – | bổ đao | `Hit` @ 0.4 | `vfx_c1_boss`: vệt chém lớn |
| **Hắc Phong Trảm** | kéo đao ra sau, gió đen xoáy quanh lưỡi · **quạt đỏ dài 3, góc 90°** phía trước | 0.7 s quét đao ngang | @ 0.25 | `vfx_c1_boss`: sóng gió đen lưỡi liềm, đẩy lùi |
| **Hắc Phong Trảm (pha 2)** | như trên · **vòng tròn đỏ r 3** | 0.9 s **xoay một vòng** | @ 0.35 | `vfx_c1_boss`: vòng gió đen 360° |
| **Gọi Lâu La** (không cảnh báo) | – | 1.0 s đưa hai ngón tay lên miệng **huýt sáo**, vẫy tay | @ 0.5 | `vfx_c1_boss`: nốt huýt sáo; 4 Lâu La Đao chạy `spawn` từ cổng trại (ải Công) / mép bản đồ phía địch (ải Thủ) 🟡 kèm bụi `vfx_enemy_common` |
| **pha 2 – Cuồng Nộ** | – | `phase2`: gầm, đập ngực, mắt đỏ rực (mặt `rage`) | @ 1.0 | `vfx_c1_boss`: ánh mắt đỏ + hào quang đỏ bám người tới hết trận; `attack` nhanh thêm 30 % |

### 4.2 Thiên Công Tế Tửu (chương 2 · trượng đầu rồng · tầm 4.0 · tốc đánh 0.6)
Xương thêm: `staff → staff_head`, dải lụa bùa `ribbon_L_1…3`, `ribbon_R_1…3`, `beard_1 → beard_2`. **Vòng bùa xoay dưới chân** là VFX lặp riêng (không vẽ vào nhân vật).

| Kỹ năng | `windup` (`Telegraph`) | `cast` | `Cast` | VFX |
|---|---|---|---|---|
| đánh thường | – | chĩa trượng, rồng nhả cầu tà khí | `Hit` @ 0.4 (đạn **10**) | `vfx_c2_boss`: cầu tà khí xanh lục |
| **Hoàng Thiên Lôi** | giơ trượng, mây vàng tụ trên đầu · **5 vòng đỏ r 1.2** quanh quân ta | 0.6 s đập trượng xuống đất | @ 0.2 | `vfx_c2_boss`: 5 tia sét vàng lớn, đất cháy xém |
| **Triệu Hồi Rơm Binh** (không cảnh báo) | – | 1.2 s vung tay rải bùa ra xung quanh | @ 0.7 | `vfx_c2_boss`: đất bung + bùa bay; 8 Bù Nhìn Rơm `spawn` (trồi từ đất) quanh boss |
| **pha 2 – Tà Khí Phong Bạo** | – | `phase2`: bay cao hơn một chút, dải lụa xoè, mặt `rage` | @ 1.0 | `vfx_c2_boss`: bão bụi vàng–xanh lục r 3 **bám theo boss** suốt pha 2 + **khiên tà khí** (bong bóng xanh lục, vỡ khi hấp thụ đủ 1000) |

### 4.3 Cửu Vĩ Yêu Hồ (chương 3 · quạt đỏ · tầm 3.5 · tốc đánh 0.8)
Xương thêm: **9 đuôi** – dùng chung 1–2 sprite đuôi nhân bản, mỗi đuôi chuỗi `tail_N_1 → tail_N_2 → tail_N_3` gắn `hip`; `idle` vẫy **lệch pha** từng đuôi. `ear_near`, `ear_far` (giật tai), `fan` (2 lớp: `fan_closed`, `fan_open`). Lửa cáo bay quanh người là VFX lặp riêng.

| Kỹ năng | `windup` (`Telegraph`) | `cast` | `Cast` | VFX |
|---|---|---|---|---|
| đánh thường | – | phẩy quạt | `Hit` @ 0.4 (cầu lửa cáo, đạn **10**) | `vfx_c3_boss`: cầu lửa cáo xanh |
| **Hồ Hoả Vũ** | xoè quạt, 9 đuôi dựng lên, lửa tụ đầu đuôi · **6 vòng đỏ** quanh quân ta | 0.6 s phẩy quạt xuống | @ 0.2 | `vfx_c3_boss`: 6 cầu lửa cáo rơi (bắt đầu rơi trước `Cast`), vũng lửa xanh |
| **Huyễn Ảnh** (không cảnh báo) | – | 0.8 s xoay người che quạt, cười | @ 0.5 | `vfx_c3_boss`: khói hồng tím + cánh hoa; 2 phân thân `spawn` (cùng prefab, 🟡 hơi trong suốt, không có thanh máu boss) |
| **pha 2** | – | `phase2`: đuôi bùng lửa xanh, cười lớn (`rage`) | @ 1.0 | `vfx_c3_boss`: lửa đầu đuôi bùng to (giữ suốt pha 2) |
| **Mị Hoặc** (pha 2, mỗi 12 s) | che nửa mặt bằng quạt, mắt ánh hồng · **dấu ngắm đỏ** trên tướng bị nhắm (0.5 s) | 0.4 s phẩy quạt về phía tướng | @ 0.1 | `vfx_c3_boss`: vòng xoáy hồng tím trên đầu tướng 2 s (tướng chạy clip `stunned`) |

---

## 5. Tổng hợp file VFX quân địch
| Nhóm | File (`docs/QuanDich/art/`) |
|---|---|
| Chung mọi chương | `vfx_enemy_telegraph_sheet.png` (vùng đỏ), `vfx_enemy_common_sheet.png` (poof, bụi xuất hiện, sóng đổi pha, mũi tên tăng sức) |
| Chương 1 | `vfx_c1_basic`, `vfx_c1_axes`, `vfx_c1_advisor`, `vfx_c1_boss` |
| Chương 2 | `vfx_c2_basic`, `vfx_c2_guardian`, `vfx_c2_sorcerer`, `vfx_c2_boss` |
| Chương 3 | `vfx_c3_basic`, `vfx_c3_revive`, `vfx_c3_ox`, `vfx_c3_horse`, `vfx_c3_boss` |

Prompt từng tấm: `VFX_Prompts.md` (cùng thư mục).
