# Icon kỹ năng – prompt tạo ảnh (dán nguyên văn)

Phong cách: **A – render 3D stylised** cùng tông với ảnh mẫu `chutuong1.png`; kỹ năng: `docs/LordHeroes.md` §2.
Thẻ tướng đã ra sân hiển thị **icon kỹ năng 2 và 3** kèm đếm ngược hồi chiêu (game tự phủ lớp tối + số giây) và **thanh nộ** cho tuyệt kỹ (game tự vẽ thanh; icon tuyệt kỹ sáng lên khi đầy) – spec §3. Vì vậy chỉ cần **icon tĩnh**.

**Quy tắc chung**
- Vẽ **256 × 256 px**, hình **tròn**, nền ngoài vòng tròn **trong suốt** (game thu nhỏ còn ≈ 44 px → một hình chính, ít chi tiết, tương phản mạnh).
- Mỗi chủ tướng **4 icon**: nội tại (kỹ năng 1 – hiện ở màn chọn chủ tướng), kỹ năng 2, kỹ năng 3, tuyệt kỹ.
- **Cùng một khung viền** cho mọi icon: vòng đồng mảnh; **tuyệt kỹ viền vàng** dày hơn. Nền đĩa theo **màu chủ đạo** của chủ tướng.
- Tạo icon của **một chủ tướng trong cùng cuộc trò chuyện** để 4 icon đồng bộ; icon đầu tiên ưng ý thì gửi kèm cho các icon sau.
- Tên file: `icon_<chủ tướng>_<passive|skill2|skill3|ultimate>.png`, chủ tướng = `archer`, `swordsman`, `strategist`, `marshal`. Lưu vào thư mục này trước, mình chuyển vào `Assets/Arena3Q/Art/Icons/`.
- Đính kèm: `chutuong1.png` (phong cách) + ảnh bảng xoay của chủ tướng (màu, vũ khí).

**Prompt mẫu** – mỗi icon dưới đây đã ghép sẵn, chỉ cần dán:
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: <BIỂU TƯỢNG>, on a background disc in <MÀU NỀN> with a soft radial glow, framed by a thin <bronze ring | thick gold ring with small ornaments>. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```

| Chủ tướng | Màu nền đĩa |
|---|---|
| Cung thủ | jade green (#5BAA8A) |
| Kiếm sĩ | deep blue (#2C4A7A) |
| Mưu sĩ | lavender (#B9A3D6) |
| Thống soái | crimson (#A3262A) |

---

## Cung thủ (đính kèm `chutuong1.png` + `archer_turnaround.png`)

**`icon_archer_passive.png` – Ưng Nhãn** (+15 % tầm, mỗi đòn thứ 4 xuyên)
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: a sharp hawk's eye with a golden iris, a small arrow reflected in the pupil, on a background disc in jade green (#5BAA8A) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_archer_skill2.png` – Liên Châu Tiễn**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: three glowing arrows flying in a row diagonally up-right, on a background disc in jade green (#5BAA8A) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_archer_skill3.png` – Tiễn Vũ**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: many small arrows raining down from a small dark cloud, on a background disc in jade green (#5BAA8A) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_archer_ultimate.png` – Phá Thành Hỏa Tiễn**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: one huge flaming arrow piercing diagonally up-right, bright orange fire, on a background disc in jade green (#5BAA8A) with a soft radial glow, framed by a thick gold ring with small ornaments. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```

---

## Kiếm sĩ (đính kèm `chutuong1.png` + `swordsman_turnaround.png`)

**`icon_swordsman_passive.png` – Kiếm Tâm** (hút máu, dưới 30 % máu tăng tốc đánh)
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: a straight sword pointing down through a glowing red heart, on a background disc in deep blue (#2C4A7A) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_swordsman_skill2.png` – Phi Ảnh Đột Kích**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: a straight sword thrusting forward to the right with blue speed streaks and a faint afterimage behind it, on a background disc in deep blue (#2C4A7A) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_swordsman_skill3.png` – Toàn Phong Trảm**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: two crossed swords inside a spinning circular blue slash, on a background disc in deep blue (#2C4A7A) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_swordsman_ultimate.png` – Vạn Kiếm Quy Tông**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: many glowing spirit swords falling from the sky in a fan shape, pointing down, bright blue-white light, on a background disc in deep blue (#2C4A7A) with a soft radial glow, framed by a thick gold ring with small ornaments. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```

---

## Mưu sĩ (đính kèm `chutuong1.png` + `strategist_turnaround.png`)

**`icon_strategist_passive.png` – Thao Lược** (đội lính quanh Mưu sĩ +10 % sát thương)
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: an open bamboo-slip strategy book with a small glowing jade bagua pendant above it, on a background disc in lavender (#B9A3D6) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_strategist_skill2.png` – Hỏa Kế**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: a bright flame rising from an unrolled paper scroll, on a background disc in lavender (#B9A3D6) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_strategist_skill3.png` – Bát Quái Trận**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: a glowing purple bagua trigram circle with a yin-yang centre, on a background disc in lavender (#B9A3D6) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_strategist_ultimate.png` – Đông Phong**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: a white crane-feather fan sweeping a powerful swirling wind gust with flying leaves, on a background disc in lavender (#B9A3D6) with a soft radial glow, framed by a thick gold ring with small ornaments. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```

---

## Thống soái (đính kèm `chutuong1.png` + `marshal_turnaround.png`)

**`icon_marshal_passive.png` – Quân Lệnh Như Sơn** (đội lính +2 lính, toàn quân +5 % máu)
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: a golden command tablet standing in front of a small solid mountain peak, on a background disc in crimson (#A3262A) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_marshal_skill2.png` – Hiệu Triệu**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: a raised war banner on a pole glowing with golden light, on a background disc in crimson (#A3262A) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_marshal_skill3.png` – Trống Trận**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: a red-and-gold war drum with orange shockwave rings around it, on a background disc in crimson (#A3262A) with a soft radial glow, framed by a thin bronze ring. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
**`icon_marshal_ultimate.png` – Thiết Kỵ Xung Phong**
```
Stylised 3D-rendered mobile game skill icon matching the look of the attached reference image (soft, clean, glossy, slightly chibi), 256x256, perfectly round, transparent outside the circle.
A single bold central symbol: the head of a charging armoured war horse facing right, golden armour plates, dust behind, on a background disc in crimson (#A3262A) with a soft radial glow, framed by a thick gold ring with small ornaments. High contrast, very simple shapes, readable at 44 pixels, no text, no letters, no watermark.
```
