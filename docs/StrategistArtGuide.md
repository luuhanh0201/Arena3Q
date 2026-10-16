# Arena3Q – Mưu sĩ: đặc tả tạo hình và prompt thử phong cách

Phiên bản: v0.1 · 2026-10-17 · Trạng thái: **🟡 đang thử 3 phong cách** (A / B / C bên dưới). Chọn xong phong cách thì viết tiếp phần tách bộ phận (Krita), gắn xương và animation (Unity).

Đi kèm: `docs/LordHeroes.md` (kỹ năng Mưu sĩ §2.3), `docs/LordArtPrompts.md` (quy trình chung cho 4 chủ tướng). Ảnh mẫu phong cách: `docs/art/concepts/chutuong1.png` (**chỉ là mẫu phong cách**, không phải nhân vật trong game).

---

## 1. Đặc tả tạo hình Mưu sĩ

### 1.1 Nhân vật
| Mục | Đặc tả |
|---|---|
| Vai trò | Chủ tướng **Mưu sĩ**: khống chế vùng, tăng sức đội lính; đánh thường tầm trung (3.0) bằng **luồng khí phóng ra từ quạt** |
| Giới tính / tuổi | Nữ, khoảng 25 tuổi |
| Tính cách thể hiện | Điềm tĩnh, thông minh, tự tin; mặc định **mỉm cười nhẹ**, mắt sắc |
| Dáng người | Mảnh, đứng thẳng, thanh lịch; **không mặc giáp** (khác hẳn 3 chủ tướng còn lại) |
| Tên | Không có tên riêng – mang tên người chơi (✅) |

### 1.2 Hình bóng (silhouette) – phải nhận ra khi nhân vật chỉ cao ~200 px
- **Hình tam giác rộng dưới**: áo dài chấm mắt cá + **tay áo rộng** (thõng xuống ngang hông).
- **Đầu**: búi tóc cao + **trâm trăng khuyết** bạc nhô lên – dấu hiệu nhận ra từ xa.
- **Quạt lông hạc** ở tay phải – vật nhận dạng chính, đủ to (≈ bằng đầu).
- Không áo choàng dài, không mũ to, không vật thể dài lủng lẳng ngoài **2 đuôi dải lụa thắt lưng** (dải màu đội).

### 1.3 Trang phục (từ trong ra ngoài)
1. **Áo trong** màu ngà, cổ giao lĩnh (cổ chéo kiểu Hán phục), lộ ở cổ và vạt trước.
2. **Áo ngoài** dài màu **tím oải hương**, **tay áo rộng** (bàn tay vẫn lộ ra khỏi tay áo), viền **tím đậm** có hoa văn **mây cuộn** màu vàng nhạt.
3. **Thắt lưng** vải bản to + **2 đuôi dải lụa** thõng xuống trước hông – **vẽ màu xám nhạt trung tính** (game tô lại màu đội).
4. **Ngọc bội bát quái** bằng ngọc bích, treo dây ở thắt lưng (phía trước).
5. **Thẻ tre binh thư** (cuộn thẻ tre buộc dây) treo ở **hông trái**.
6. **Giày** vải mũi mây màu ngà viền tím; áo dài che gần hết chân, **vạt áo trước xẻ** (để bước đi tự nhiên khi làm animation).
7. **Tóc** đen nâu, búi cao gọn + **2 lọn tóc mái** hai bên mặt; **trâm trăng khuyết** bạc cài búi.
8. **Quạt lông hạc**: lông trắng, đầu lông xám nhạt, cán tre/ngọc, tua nhỏ tím.

### 1.4 Bảng màu
| Phần | Màu | Mã gợi ý |
|---|---|---|
| Áo ngoài | Tím oải hương | `#B9A3D6` |
| Viền, đai | Tím đậm | `#6B4E8C` |
| Áo trong, giày | Trắng ngà | `#F2EAD8` |
| Hoa văn, trâm | Vàng nhạt / bạc | `#C9A55A` / `#D9DCE3` |
| Ngọc bội, cán quạt | Ngọc bích | `#6FB59B` |
| Tóc | Đen nâu | `#2E2320` |
| Dải màu đội | Xám nhạt trung tính | `#BDBDBD` |

### 1.5 Ràng buộc để sau này tách bộ phận và gắn xương được
- **Bàn tay luôn lộ ra** khỏi tay áo; **không vật gì che mặt** (quạt, tóc).
- Tay áo, đuôi dải lụa, lọn tóc **có mép rõ**, không dính vào thân.
- Ở tư thế **A-pose**: hai tay dang nhẹ khỏi thân (~25°), hai chân hơi tách, quạt cầm dựng đứng **không** chồng lên thân.
- Ánh sáng **mềm, từ trên-trái**, không bóng đổ gắt và không phản chiếu mạnh (vì khi xoay từng bộ phận, vệt sáng "vẽ chết" sẽ xoay theo trông giả).
- Kích thước trong game: cao **≈ 1.6 đơn vị ≈ 205 px** trên màn hình chuẩn (map spec §8.5).

---

## 2. Cách thử 3 phong cách

| | **A – Chibi render 3D** (theo `chutuong1`) | **B – Lai** | **C – Vẽ tay** (đã chốt trước đây) |
|---|---|---|---|
| Tỉ lệ | ~2.5 đầu, đầu rất to | ~2.5–3 đầu | ~3.5 đầu |
| Bề mặt | Bóng, kim loại phản chiếu, như mô hình figure | Chi tiết như A nhưng **tô tay mềm**, ít bóng | Tô tay mềm, đơn giản hơn |
| Viền | Không | Viền mảnh nâu đậm | Viền mảnh tối |
| Hợp với `map1` (vẽ tay) | Kém – nhân vật "nổi" khỏi nền | Tốt | Tốt nhất |
| Gắn xương | Khó hơn: vệt sáng phản chiếu xoay theo bộ phận | Dễ | Dễ |
| Đọc rõ ở ~200 px | Tốt (đầu to, mặt rõ) | Tốt | Trung bình (đầu nhỏ hơn) |

**Quy trình thử (khoảng 30 phút):**
1. Dùng **ChatGPT** (tạo ảnh, có nền trong suốt). Mỗi phong cách **một cuộc trò chuyện mới**.
2. Prompt 1 (bảng xoay 4 hướng) → nếu chưa ưng, bảo "làm lại" 2–3 lần, chọn tấm đẹp nhất.
3. Prompt 2 (góc nhìn trong game) – **gửi kèm tấm vừa chọn**.
4. Ảnh đính kèm: **A** gửi `chutuong1.png`; **B** gửi `chutuong1.png` + `Assets/Arena3Q/Art/Maps/map1.png`; **C** gửi `map1.png`.
5. Lưu kết quả vào `docs/art/concepts/` với tên `strategist_A_turnaround.png`, `strategist_A_ingame.png` (tương tự B, C) và báo mình: mình **ghép ảnh góc trong game lên `map1` đúng kích thước trong trận** để bạn so trực tiếp.

---

## 3. Prompt

Mỗi prompt = **khối phong cách** (A, B hoặc C) + **khối nhân vật** + **khối yêu cầu ảnh**. Dán nguyên văn (tiếng Anh cho kết quả ổn định hơn).

### 3.1 Khối phong cách

**A – Chibi render 3D (đính kèm `chutuong1.png`)**
```
STYLE: Match the rendering style, proportions and level of detail of the attached reference image
(stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon.
Chibi proportions about 2.5 heads tall, large head, big expressive eyes, small body.
Soft studio lighting from the top-left, gentle rim light, smooth clean materials,
keep reflections and specular highlights subtle (no strong shiny spots). No outline.
```

**B – Lai (đính kèm `chutuong1.png` + `map1.png`)**
```
STYLE: Use the first attached image ONLY for proportions and costume detail level
(chibi, about 2.5 to 3 heads tall, large head, big expressive eyes),
but paint the character as HAND-PAINTED 2D game art that matches the second attached image (a painted game map):
soft painterly shading, matte materials with gentle highlights, no glossy reflections,
soft muted colours, thin dark-brown outline, light from the top-left.
```

**C – Vẽ tay (đính kèm `map1.png`)**
```
STYLE: Hand-painted 2D game character art for a mobile strategy game, matching the painting style and colour softness
of the attached game map. Clash of Clans-like stylised proportions (about 3.5 heads tall, slightly oversized head,
hands and fan), soft painterly shading, matte materials, thin dark outline, light from the top-left,
clean bold silhouette that reads at small size.
```

### 3.2 Khối nhân vật (giống nhau cho cả A, B, C)
```
CHARACTER: A young female military strategist heroine from ancient East Asia (about 25 years old),
calm, clever and confident, gentle slight smile, sharp intelligent eyes, slender elegant build, NO armour.
Outfit: ivory inner robe with a crossed collar (Hanfu style); long lavender outer robe (#B9A3D6) reaching the ankles,
with wide sleeves that hang to the hips while both hands stay visible; deep violet trim (#6B4E8C) with a pale-gold
cloud pattern; the front of the robe is split so the legs can step; ivory cloth shoes with cloud-shaped toes.
Waist: a wide cloth sash with two long hanging tails in front, painted in NEUTRAL LIGHT GREY (#BDBDBD);
a round jade bagua pendant on a cord at the front of the sash; a small rolled bamboo-slip strategy book
tied at the left hip.
Hair: dark brown-black, a tall neat bun with a silver crescent-moon hairpin, two long locks framing the face.
Weapon: a white crane-feather fan with grey feather tips, a jade-green handle and a small violet tassel,
held in the RIGHT hand; the fan is about the size of her head.
Rules: both hands visible outside the sleeves, nothing covers the face, sleeves, sash tails and hair locks
have clean separate edges, no cape, no hat.
```

### 3.3 Prompt 1 – bảng xoay 4 hướng (giống bố cục `chutuong1`)
```
[STYLE A / B / C]
[CHARACTER]
IMAGE: Character turnaround sheet, four full-body views of the SAME character in a row on a plain white background,
evenly spaced, same size and proportions in every view, orthographic camera at eye level:
1) front view, 2) side view facing right, 3) back view,
4) A-pose front view: arms held slightly away from the body (about 25 degrees), legs slightly apart,
fan held upright in the right hand not overlapping the body.
No text, no labels, no watermark.
```

### 3.4 Prompt 2 – góc nhìn trong game (gửi kèm tấm bảng xoay đã chọn)
```
[STYLE A / B / C]
Using the attached turnaround sheet, draw exactly the same character (same face, outfit, colours, fan and proportions).
IMAGE: One full-body image, FRONT-RIGHT three-quarter view, camera looking DOWN from above at about 40 degrees
(like a mobile strategy game battlefield), orthographic, standing idle holding the fan casually in the right hand,
soft light from the top-left, transparent background, no ground shadow, no text.
```

> Nếu ChatGPT không cho nền trong suốt ở prompt 2, thay `transparent background` bằng `plain white background` – mình tách nền khi ghép thử.

---

## 4. Tiêu chí chọn (tự chấm khi xem ảnh ghép trên `map1`)
1. **Đọc được ở cỡ nhỏ**: thu ảnh xuống cao ~200 px vẫn nhận ra quạt, búi tóc, tay áo rộng?
2. **Hợp với bản đồ**: đặt lên `map1` có "lạc tông" không?
3. **Nhất quán**: 4 hướng trong bảng xoay có cùng mặt, cùng trang phục?
4. **Dễ tách bộ phận**: mép rõ, ít bóng đổ gắt, ít vệt phản chiếu?
5. **Hợp với 3 chủ tướng còn lại**: phong cách chọn sẽ áp cho cả Cung thủ, Kiếm sĩ, Thống soái.

---

## 5. Phần tiếp theo (viết sau khi chọn phong cách)
- Ảnh tư thế chuẩn để tách bộ phận (hướng trước-phải, sau-phải) và prompt bổ sung phần bị che.
- Tách bộ phận và ghép PSD trong **Krita** (danh sách lớp, đặt tên, xuất PSD).
- Gắn xương trong **Unity 2D Animation** (tên xương chuẩn để mình dựng sẵn clip), đổi mặt bằng Sprite Library.
- Danh sách clip của Mưu sĩ (đứng, chạy, đánh thường tua theo tốc đánh, kỹ năng 2–3, tuyệt kỹ…) và phần mình code nối vào `HeroUnit`.
