# Arena3Q – Prompt hình ảnh và danh sách animation cho 4 chủ tướng

Phiên bản: v0.3 · 2026-10-17 (bỏ tên riêng – chủ tướng mang tên người chơi; thêm icon kỹ năng; đổi tên kỹ năng 2/3; animation đánh theo tốc đánh). Đi kèm `docs/LordHeroes.md` (thiết kế, kỹ năng) và cùng phong cách với bản đồ `map1.png` (`docs/ArtPrompts.md`).

Đã chốt ✅: Đông Á cổ trang · vẽ tay màu mềm, góc 3/4 · animation **skeletal** (Unity 2D Animation, đã có trong project) từ **PSD tách bộ phận** · **2 hướng** (trước-phải, sau-phải) + lật gương ra trái · 2 nam + 2 nữ.

> **Ý tưởng chính:** với skeletal, mỗi nhân vật chỉ cần vẽ **một lần cho mỗi hướng** nhưng **tách rời từng bộ phận** (đầu, thân, tay trên, tay dưới, bàn tay, đùi, ống chân, áo choàng, vũ khí…). Unity gắn xương vào các bộ phận và làm **mọi động tác** (đứng, đi, đánh, chết, kỹ năng…). Vì vậy ta **không** cần AI vẽ từng khung hình – việc mà AI làm rất kém (nhân vật đổi mặt, đổi áo giữa các khung).

---

## 1. Nên dùng công cụ nào? (đánh giá cho đúng việc này)

Việc này cần 3 thứ: (1) **làm đúng chỉ dẫn** (đúng danh sách bộ phận, đúng góc nhìn, tách rời từng phần), (2) **giữ một nhân vật giống hệt nhau** qua nhiều ảnh (concept, bộ phận hướng trước, bộ phận hướng sau, chân dung, kỹ năng), (3) **nền trong suốt**.

| Tiêu chí | ChatGPT (tạo ảnh) | Gemini (tạo / sửa ảnh) | Midjourney | Stable Diffusion / ComfyUI |
|---|---|---|---|---|
| Làm đúng chỉ dẫn chi tiết (danh sách bộ phận, bố cục bảng) | **Tốt** | **Tốt** | Kém – đẹp nhưng hay tự ý | Trung bình – cần ControlNet |
| Giữ nhân vật nhất quán | **Tốt** khi sửa tiếp trên ảnh cũ trong cùng cuộc trò chuyện | **Tốt** (mạnh về sửa ảnh theo ảnh tham chiếu) | Khá (tham chiếu nhân vật) | **Rất tốt** nếu train LoRA riêng, nhưng tốn công |
| Nền trong suốt | Có tuỳ chọn | Thường phải tách nền sau | Không – phải tách nền | Có (với tiện ích phù hợp) |
| Độ đẹp / "chất game" | Tốt | Tốt | **Đẹp nhất** | Tuỳ model |
| Chi phí cài đặt | Thấp | Thấp | Thấp | Cao (cần GPU mạnh, nhiều cấu hình) |

**Khuyến nghị:**
- **Công cụ chính: ChatGPT hoặc Gemini** – hợp nhất với kiểu việc "vẽ đúng một nhân vật, tách đúng các bộ phận, giữ nguyên qua nhiều ảnh". Làm **mỗi chủ tướng trong một cuộc trò chuyện riêng**, luôn gửi lại ảnh concept đã chốt khi xin ảnh tiếp theo.
- **Midjourney (tuỳ chọn):** chỉ để **khám phá concept** ban đầu nếu muốn nhiều phương án đẹp; sau đó đưa ảnh đã chọn sang ChatGPT/Gemini để làm bộ phận.
- **Stable Diffusion:** chỉ đáng khi cần sản xuất **rất nhiều** ảnh cho cùng nhân vật (train LoRA riêng).
- Tính năng các công cụ thay đổi nhanh – trước khi làm hàng loạt, thử một nhân vật trên 2 công cụ và giữ công cụ cho kết quả ổn định hơn.

---

## 2. Quy trình cho mỗi chủ tướng

1. **Concept** (ảnh A): xin ảnh nhân vật toàn thân, 2 hướng trên cùng một tấm → chọn và chỉnh tới khi ưng. **Đây là ảnh gốc** cho mọi bước sau.
2. **Bộ phận hướng trước-phải** (ảnh B) và **hướng sau-phải** (ảnh C): từ ảnh A, xin tấm "bộ phận tách rời" (mục 4).
3. **Biểu cảm** (ảnh D): các khuôn mặt thay đổi được (bình thường, hô lệnh, đau, gục).
4. **Hiệu ứng kỹ năng** (ảnh E): các mảnh hiệu ứng nền trong suốt (mục 6).
5. **Chân dung thẻ + ảnh màn chọn chủ tướng** (ảnh F, G).
6. **Hậu kỳ trong Krita/Photoshop** (mục 8): cắt từng bộ phận, xoá nền, ghép lại thành **PSD nhiều lớp** ở tư thế đứng mặc định.
7. Gửi PSD cho mình (hoặc đặt vào `Assets/Arena3Q/Art/Heroes/`): mình gắn xương trong Unity 2D Animation và làm các động tác ở mục 7.

---

## 3. Khối phong cách nhân vật (dán vào đầu MỌI prompt)

```
Hand-painted 2D game character art for a mobile strategy game, ancient East Asian setting,
soft colours, gentle painterly shading, Clash of Clans–like stylised proportions (about 3.5 heads tall,
slightly oversized head, hands and weapon), clean bold silhouette that reads at small size.
Fixed 3/4 top-down camera, about 41 degrees from above, orthographic, no perspective distortion.
Soft light from the top-left, soft shadow side on the bottom-right. Thin dark outline.
No text, no watermark, no background scenery.
```

Kích thước dùng trong game: chủ tướng cao ≈ **1.6 đơn vị** (map spec §8.5). Để gắn xương nét, vẽ **gấp đôi**: nhân vật cao **≈ 420 px** trong PSD và import với PPU 256 (mình cài khi nhận file).

---

## 4. Tấm bộ phận tách rời (dùng cho cả 4 chủ tướng)

Đây là prompt quan trọng nhất. Gửi kèm ảnh concept đã chốt.
```
[khối phong cách ở mục 3]
Using the attached character exactly (same face, outfit, colours and weapon), create a character PARTS SHEET
for 2D skeletal animation, facing FRONT-RIGHT (3/4 view). Every part drawn separately with clear empty space
between parts, all on a plain transparent background, nothing overlapping:
head (no hair), back hair, front hair or hat, neck, torso (chest), pelvis/waist with belt,
cloak in 2 pieces (upper, lower), right upper arm, right forearm, right hand open, right hand gripping,
left upper arm, left forearm, left hand open, left hand gripping, right thigh, right shin with boot, right foot,
left thigh, left shin with boot, left foot, main weapon, secondary item, [phụ kiện riêng của nhân vật],
and a separate TEAM-COLOUR piece: [dải màu đội] painted in neutral light grey.
Limbs drawn slightly longer at the joints (rounded ends) so they can rotate without gaps.
Same scale for every part.
```
Sau đó xin lại y hệt với **"facing BACK-RIGHT (3/4 view from behind)"** – hướng sau dùng khi nhân vật đi lên phía trên màn hình.

**Lớp màu đội (team colour):** một mảnh vẽ **xám nhạt trung tính** (dải lụa, chùm lông mũ, tua cờ…) để game tô lại **xanh** (quân ta) hoặc **đỏ** (đối thủ khi có PvP) mà không đổi màu chủ đạo của nhân vật.

**Biểu cảm (ảnh D):**
```
[khối phong cách ở mục 3]
Using the attached character, an expression sheet of the SAME head facing front-right, 4 separate heads
on a transparent background: calm, shouting a command, hurt (wincing), knocked out (eyes closed).
Identical size and angle for all four.
```

---

## 5. Từng chủ tướng – concept và chi tiết riêng

Thay các phần trong `[...]` ở mục 4 bằng chi tiết dưới đây.

### 5.1 Cung thủ (nữ)
Concept:
```
[khối phong cách ở mục 3]
Character design turnaround, two views side by side on a plain white background: FRONT-RIGHT 3/4 view and
BACK-RIGHT 3/4 view, full body, standing relaxed.
A young female archer heroine, ancient East Asian ranger: jade-green and tan leather light armour,
high ponytail with a hairpin, bracers, a tall recurve horn bow in the left hand, a quiver of feathered arrows on her back,
a light hooded half-cape, calm sharp eyes, agile build.
```
- Phụ kiện riêng: `quiver with arrows, bowstring (separate thin line)` · dải màu đội: `ribbon tied on the ponytail and bow`.
- Lưu ý gắn xương: **dây cung tách riêng** để kéo căng khi bắn.

### 5.2 Kiếm sĩ (nam)
Concept:
```
[khối phong cách ở mục 3]
Character design turnaround, two views side by side on a plain white background: FRONT-RIGHT 3/4 view and
BACK-RIGHT 3/4 view, full body, standing ready.
A young male swordsman hero, wandering sword saint: silver-white robe with deep blue trim over light lamellar armour,
long hair tied back with a headband, a long straight sword in the right hand and a short sword on the left hip,
determined expression, lean athletic build.
```
- Phụ kiện riêng: `short sword with sheath, scabbard of the long sword` · dải màu đội: `long headband tails`.

### 5.3 Mưu sĩ (nữ)
Concept:
```
[khối phong cách ở mục 3]
Character design turnaround, two views side by side on a plain white background: FRONT-RIGHT 3/4 view and
BACK-RIGHT 3/4 view, full body, standing elegantly.
A female military strategist heroine: flowing lavender and ivory scholar robes with wide sleeves, a crane-feather fan in
the right hand, a bamboo book of war strategy hanging at the waist, a small jade bagua pendant,
hair in an elegant bun with a crescent-moon hairpin, wise confident expression, slender build.
```
- Phụ kiện riêng: `bamboo strategy book, jade bagua pendant, wide sleeves as separate pieces` · dải màu đội: `sash belt tails`.
- Lưu ý: **tay áo rộng tách riêng** khỏi cánh tay để lay động khi phẩy quạt.

### 5.4 Thống soái (nam)
Concept:
```
[khối phong cách ở mục 3]
Character design turnaround, two views side by side on a plain white background: FRONT-RIGHT 3/4 view and
BACK-RIGHT 3/4 view, full body, standing commanding.
A middle-aged male grand marshal: heavy crimson and bronze lamellar armour, a helmet with a tall plume,
a long red war cape, a long spear (guandao-style blade optional) in the right hand,
a command banner pole strapped on his back, a full beard, stern powerful expression, broad sturdy build.
```
- Phụ kiện riêng: `command banner on a pole (cloth as a separate piece), helmet plume` · dải màu đội: `helmet plume and banner cloth`.

---

## 6. Hiệu ứng kỹ năng (VFX)

Hiệu ứng **không** cần vẽ từng khung: mình dựng bằng **hệ thống hạt (particle) của Unity** từ vài **mảnh hiệu ứng** nền trong suốt. Mỗi prompt:
```
[khối phong cách ở mục 3]
A set of separate VFX texture elements for a mobile game skill, on a transparent background, evenly spaced,
glowing, soft painterly style: [danh sách mảnh]
```
Hình nằm trên mặt đất (vòng trận, vùng lửa) vẽ **nhìn thẳng từ trên xuống, hình tròn** – game tự nghiêng theo góc 3/4.

| Chủ tướng | Kỹ năng | Danh sách mảnh `[...]` |
|---|---|---|
| Cung thủ | Liên Châu Tiễn | glowing green arrow, short motion trail, small hit spark |
| | Tiễn Vũ | falling arrow seen from above-front, arrow stuck in ground, circular dust ring (top-down) |
| | Phá Thành Hỏa Tiễn | huge flaming arrow, fire trail, big fire explosion burst, embers |
| Kiếm sĩ | Phi Ảnh Đột Kích | blue afterimage silhouette smear, dash streak lines, impact star |
| | Toàn Phong Trảm | circular blue sword-slash ring (top-down), wind swirl |
| | Vạn Kiếm Quy Tông | single glowing spirit sword pointing down, light pillar, ground crack decal (top-down) |
| Mưu sĩ | Hỏa Kế | circular fire field decal (top-down), single flame, smoke puff |
| | Bát Quái Trận | glowing bagua trigram circle decal (top-down, purple light), floating rune symbol |
| | Đông Phong | horizontal wind gust streaks, swirling leaves, feather |
| Thống soái | Hiệu Triệu | golden rally banner glow, light beam, small soldier silhouette appearing in light |
| | Trống Trận | war drum shockwave ring (top-down, orange), musical drum beat burst |
| | Thiết Kỵ Xung Phong | armoured cavalry rider charging (side 3/4 view, ghostly golden), dust cloud trail, hoof-print decal |

---

## 6b. Icon kỹ năng (hiện trên thẻ tướng)

Thẻ của tướng đã ra sân hiển thị (spec §3 "Kỹ năng trên thẻ"): **icon kỹ năng 2 và 3** kèm **đếm ngược hồi chiêu** (game tự phủ lớp tối + số giây lên icon), và **thanh nộ** cho tuyệt kỹ (game tự vẽ thanh; icon tuyệt kỹ sáng lên khi đầy). Vì vậy chỉ cần vẽ **icon tĩnh**:

- Kích thước vẽ **256 × 256 px**, nền trong suốt, hình **tròn** (game cắt và thu nhỏ ≈ 44 px trên thẻ – phải đọc được ở cỡ rất nhỏ: một hình chính, ít chi tiết, tương phản mạnh).
- Mỗi chủ tướng **3 icon**: kỹ năng 2, kỹ năng 3, tuyệt kỹ (+ 1 icon nội tại – kỹ năng 1 – nếu muốn hiện trong màn chọn chủ tướng).
- Cùng một bộ khung viền cho cả 4 chủ tướng; màu nền icon theo màu chủ đạo của chủ tướng; **icon tuyệt kỹ có viền vàng** để phân biệt.

```
[khối phong cách ở mục 3, bỏ câu về camera 3/4]
A round skill icon for a mobile strategy game, 256x256, transparent outside the circle,
single bold central symbol: [mô tả icon], background disc in [màu chủ đạo] with a soft radial glow,
thin [bronze / gold for ultimate] ring frame, high contrast, readable at 44 pixels, no text.
```

| Chủ tướng | Kỹ năng 2 | Kỹ năng 3 | Tuyệt kỹ (viền vàng) |
|---|---|---|---|
| Cung thủ | three arrows in a row (Liên Châu Tiễn) | arrows raining down from a cloud (Tiễn Vũ) | one huge flaming arrow (Phá Thành Hỏa Tiễn) |
| Kiếm sĩ | sword with speed streak (Phi Ảnh Đột Kích) | two crossed swords in a spinning circle (Toàn Phong Trảm) | many swords falling from the sky (Vạn Kiếm Quy Tông) |
| Mưu sĩ | flame on a scroll (Hỏa Kế) | glowing bagua symbol (Bát Quái Trận) | feather fan with wind swirl (Đông Phong) |
| Thống soái | raised war banner (Hiệu Triệu) | war drum with shockwave (Trống Trận) | charging armoured horse head (Thiết Kỵ Xung Phong) |

Tên file: `icon_<class>_<skill>.png`, ví dụ `icon_archer_passive.png`, `icon_archer_skill2.png`, `icon_archer_skill3.png`, `icon_archer_ultimate.png`. Tướng thường (không phải chủ tướng) cũng có tuyệt kỹ (spec §3) – icon của họ làm theo cùng mẫu khi thiết kế xong bộ kỹ năng.

## 7. Danh sách animation (mình làm trong Unity bằng skeletal)

Bạn chỉ cần cung cấp PSD bộ phận + biểu cảm; các động tác sau mình dựng. Nếu muốn định hướng tư thế, có thể xin AI thêm một **tấm tư thế tham khảo** (không dùng trong game):
```
[khối phong cách ở mục 3]
A pose reference sheet of the attached character, front-right 3/4 view, 6 key poses in a row on white:
[liệt kê tư thế, ví dụ: idle, mid-stride walk, attack wind-up, attack strike, hurt recoil, fallen]
```

| Động tác | Mô tả chung | Riêng từng chủ tướng |
|---|---|---|
| **Đứng chờ** (lặp) | thở nhẹ, áo choàng lay | Cung thủ xoay tên trong tay · Kiếm sĩ cầm kiếm hờ · Mưu sĩ phẩy quạt chậm · Thống soái chống thương |
| **Đi / chạy** (lặp) | 6–8 tư thế chính | Thống soái bước nặng; Kiếm sĩ chạy nhanh nghiêng người |
| **Đánh thường** | có khung "trúng đòn"; **vẽ ở nhịp chuẩn 1 đòn/giây**, game tua nhanh / chậm theo **tốc đánh** của tướng (spec §6) – nên phần vung chiếm khoảng 40 % clip, khung trúng đòn rõ ràng | kéo cung–bắn · chém chéo · phẩy quạt phóng luồng khí · đâm thương |
| **Bị đánh** | giật lùi ngắn + mặt "đau" | |
| **Chết** | ngã, mặt "gục", mờ dần | Thống soái quỳ chống thương rồi gục |
| **Xuất trận** (khi triệu hồi) | bước ra từ ánh sáng, đứng tư thế | Thống soái cắm cờ lệnh |
| **Ra lệnh** (khi nhận lệnh tấn công) | chỉ vũ khí về phía trước, mặt "hô lệnh" | |
| **Chiến thắng** (lặp) | giơ vũ khí ăn mừng | |
| **Màn chọn chủ tướng** (lặp) | tư thế đẹp nhất, chậm | |
| **Kỹ năng 2, 3** | vận chiêu 0.3–0.6 s, có khung phát chiêu | theo bảng kỹ năng `LordHeroes.md` §2 |
| **Tuyệt kỹ** | vận chiêu dài hơn (0.8–1.2 s), tư thế mạnh | |

Nội tại không cần động tác riêng (chỉ có hiệu ứng nhẹ nếu cần).

---

## 8. Chân dung và ảnh màn chọn

**Chân dung thẻ (ảnh F)** – dùng trên thẻ tướng (thẻ 160 × 210 trong HUD, vẽ gấp đôi):
```
[khối phong cách ở mục 3, bỏ câu về camera 3/4]
Bust portrait of the attached character, head and shoulders, three-quarter face turned right, confident expression,
soft vignette background in the character's main colour, vertical card composition 320x420.
```

**Ảnh màn chọn chủ tướng (ảnh G)**:
```
[khối phong cách ở mục 3, bỏ câu về camera 3/4]
Full-body splash art of the attached character in a dynamic signature pose using [kỹ năng tuyệt kỹ],
dramatic lighting, swirling [màu chủ đạo] energy, vertical 2:3 composition, transparent or simple gradient background.
```

---

## 9. Hậu kỳ trước khi gửi (bắt buộc)
1. Cắt từng bộ phận ra **lớp riêng**, xoá nền, giữ mép mềm (không viền trắng).
2. Ghép lại nhân vật ở **tư thế đứng mặc định** (đúng vị trí, đúng thứ tự trước/sau) – mỗi bộ phận một lớp, các khớp chồng lên nhau một chút.
3. Kích thước: nhân vật cao ≈ **420 px**; mọi bộ phận cùng tỉ lệ.
4. **Đặt tên lớp** rõ ràng, tiếng Anh, theo hướng: `R_` (trước-phải) / `B_` (sau-phải), ví dụ `R_head`, `R_arm_upper_L`, `R_hand_grip_R`, `R_weapon`, `R_team_sash`, `R_face_shout`.
5. Một file PSD cho mỗi hướng: `lord_archer_front.psd`, `lord_archer_back.psd` (tương tự `swordsman`, `strategist`, `marshal`).
6. Hiệu ứng: PNG nền trong suốt, mỗi mảnh một file, tên `vfx_<chủ tướng>_<kỹ năng>_<mảnh>.png`.

## 10. Câu hỏi còn mở
- ~~Tỉ lệ 3.5 đầu?~~ → **Giữ tạm 3.5 đầu** (🟡, người dùng chốt 2026-10-17).
- ~~Skin?~~ → **Đã chốt: có, để sau (⏳)** – khi làm, giữ đúng cùng bộ khung bộ phận để dùng chung animation.
