# Kiếm sĩ – prompt tạo ảnh (dán nguyên văn)

Phong cách đã chốt: **A – chibi ~2.5 đầu, render 3D** theo ảnh mẫu `chutuong1.png` (cùng thư mục này).
Công cụ: **ChatGPT** (tạo ảnh). Làm cả Kiếm sĩ trong **một cuộc trò chuyện**. Kết quả lưu **vào chính thư mục này** với tên ghi dưới mỗi prompt.
Thiết kế và kỹ năng: `docs/LordHeroes.md` §1, §2.2. Quy trình tách bộ phận, gắn xương, animation: `docs/StrategistArtGuide.md` (làm y hệt Mưu sĩ, khác biệt ghi ở cuối file này).

**Tóm tắt nhân vật:** nam, kiếm khách giang hồ, đột phá cận chiến (tầm 0.6), máu trung – sát thương cao; tốc đánh 1.4 đòn/giây (nhanh nhất). Màu chủ đạo **trắng bạc + xanh lam đậm**. Nhận ra từ xa nhờ: **song kiếm** (kiếm dài trên tay + đoản kiếm bên hông), **tóc dài buộc sau có băng trán bay**, áo bào trắng.

| Phần | Màu | Mã |
|---|---|---|
| Áo bào | Trắng bạc | `#E8EAEE` |
| Viền, đai, quần | Xanh lam đậm | `#2C4A7A` |
| Giáp lá mỏng, lưỡi kiếm | Thép | `#9AA4B2` |
| Giày, bao kiếm | Đen xám | `#22252B` |
| Chi tiết kim loại | Bạc / vàng nhạt | `#D9DCE3` / `#C9A55A` |
| Tóc | Đen | `#1E1B1A` |
| Dải màu đội (2 đuôi băng trán) | Xám nhạt trung tính | `#BDBDBD` |

| # | Ảnh | Đính kèm | Lưu thành |
|---|---|---|---|
| 1 | Bảng xoay 4 hướng (ảnh gốc) | `chutuong1.png` | `swordsman_turnaround.png` |
| 2 | Tư thế chuẩn hướng trước-phải | `swordsman_turnaround.png` | `swordsman_rig_front.png` |
| 3 | Tư thế chuẩn hướng sau-phải | `swordsman_turnaround.png` + `swordsman_rig_front.png` | `swordsman_rig_back.png` |
| 4 | 4 biểu cảm | `swordsman_rig_front.png` | `swordsman_faces.png` |
| 5 | Vá phần bị che (mỗi bộ phận một lần) | PNG bộ phận vừa cắt + `swordsman_rig_front.png` | dán lại vào Krita |
| 6 | Tư thế tham khảo animation (tuỳ chọn) | `swordsman_rig_front.png` | `swordsman_poses.png` |

Nếu phong cách bị "trôi", gửi kèm thêm `chutuong1.png` và nói: `keep the exact rendering style of this reference`.

---

## Ảnh 1 – Bảng xoay 4 hướng
Đính kèm: `chutuong1.png` · Tạo 2–3 lần, giữ tấm đẹp nhất · Lưu: `swordsman_turnaround.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, big expressive eyes, small body. Soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Character: a young male swordsman hero from ancient East Asia (about 22 years old), a wandering sword saint, determined and calm, sharp eyes, lean athletic build. A silver-white (#E8EAEE) knee-length martial robe with deep blue (#2C4A7A) trim, split at the sides for movement, worn over light steel-grey (#9AA4B2) lamellar armour on the chest and one shoulder pad on the right shoulder; deep blue trousers, black (#22252B) cloth boots with steel shin guards, a deep blue waist sash. Long black hair tied back in a high ponytail, a cloth headband whose two long tails flow behind the head, the headband and its tails painted neutral light grey (#BDBDBD). A long straight double-edged sword (jian) held in the RIGHT hand, and a short sword in a black scabbard on the LEFT hip; the empty black scabbard of the long sword also hangs at the left hip. Nothing covers the face.

Character turnaround sheet: four full-body views of the SAME character in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) A-pose front view with arms slightly away from the body and the long sword held pointing down and slightly outward, not overlapping the body.
```

---

## Ảnh 2 – Tư thế chuẩn hướng TRƯỚC-PHẢI
Đính kèm: `swordsman_turnaround.png` · Lưu: `swordsman_rig_front.png`
```
Keep the same rendering style as the attached turnaround sheet: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, big expressive eyes, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Using the attached turnaround sheet, draw exactly the same character: the young male swordsman with the silver-white robe and deep blue trim, light steel lamellar armour, the grey headband with two long tails, the high ponytail, the long straight sword in his right hand and the short sword and empty scabbard on his left hip. Same face, outfit, colours, swords and proportions.

One full-body image, FRONT-RIGHT three-quarter view (body turned 45 degrees to the right of the viewer), camera looking DOWN from above at about 40 degrees, orthographic, no perspective distortion.
Neutral rig pose: standing straight, both arms held slightly away from the body (about 25 degrees) so nothing overlaps the torso, the right hand holding the long sword pointing down and slightly outward away from the legs, the left hand open and relaxed, feet slightly apart, headband tails and robe hanging naturally, calm face.
Plain flat white background, no ground shadow, high resolution, the character fills about 85 % of the image height.
```

---

## Ảnh 3 – Tư thế chuẩn hướng SAU-PHẢI
Đính kèm: `swordsman_turnaround.png` + `swordsman_rig_front.png` · Lưu: `swordsman_rig_back.png`
```
Keep the same rendering style as the attached images: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. No text, no watermark.

Using the attached images, draw exactly the same character (same outfit, colours, swords and proportions) in the SAME neutral rig pose as the front-right image, but seen from BEHIND: BACK-RIGHT three-quarter view (his back towards the viewer, body turned 45 degrees to the right, as if walking away up-right), camera looking DOWN from above at about 40 degrees, orthographic.
Both arms slightly away from the body, the long sword held in the right hand pointing down and partly visible, feet slightly apart. The ponytail, the knot of the grey headband with its two long tails, the back of the robe and the scabbards at the left hip must match the turnaround sheet.
Plain flat white background, no ground shadow, high resolution, same scale as the attached front-right image.
```

---

## Ảnh 4 – 4 biểu cảm
Đính kèm: `swordsman_rig_front.png` · Lưu: `swordsman_faces.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, big expressive eyes, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

Using the attached front-right image, an expression sheet of the SAME head only (face, hair, ponytail and headband included), FRONT-RIGHT three-quarter view from about 40 degrees above, identical size, angle and position for all heads, 4 heads in a row, evenly spaced, on a plain white background:
1) calm and determined, mouth closed,
2) shouting a battle cry (mouth open, eyebrows down, fierce),
3) hurt (wincing, one eye shut),
4) knocked out (eyes closed, slightly dizzy).
```

---

## Ảnh 5 – Vá phần bị che (dùng trong lúc tách bộ phận ở Krita)
Đính kèm: PNG của **một** bộ phận vừa cắt + `swordsman_rig_front.png` · Thay `[...]` bằng tên bộ phận (ví dụ `torso with lamellar armour`, `right upper arm`, `front robe panel`, `long sword`)
```
Keep the same rendering style as the attached character image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline.

The first attached image is an isolated body part cut from the character in the second image: [torso with lamellar armour].
Complete the missing or hidden area so the part is whole, keep EXACTLY the same style, colours, size, angle and edges of the visible area, do not change the visible pixels. If the part connects to another part at a joint, extend it a little past the joint with a rounded end.
Plain flat white background, only this one part in the image, no text.
```

---

## Ảnh 6 (tuỳ chọn) – Tư thế tham khảo cho animation
Đính kèm: `swordsman_rig_front.png` · Lưu: `swordsman_poses.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

A pose reference sheet of the attached character, FRONT-RIGHT three-quarter view from about 40 degrees above, 6 full-body key poses in a row on a plain white background, same size:
1) idle, holding the long sword loosely pointing down,
2) fast running stride leaning forward, sword held back,
3) sword raised over the right shoulder ready to strike (attack wind-up),
4) diagonal downward slash finished, sword low on the left (attack strike),
5) pointing the long sword straight up to the sky, left hand forming a sword-finger gesture (ultimate skill),
6) fallen on the ground, eyes closed, sword dropped beside him.
```

---

## Khác với Mưu sĩ khi tách bộ phận và gắn xương
- **Không** có tay áo rộng; tay áo bó theo cánh tay (vẽ luôn trên `arm_*`). Vạt áo bào xẻ hai bên → `F_robe_front`, `F_robe_back`, `F_robe_side_near` (xương `robe_*` 2 đoạn); chân đầy đủ (`leg_*_upper`, `leg_*_lower`, `foot_*`).
- Vũ khí: `F_sword_long` (con của `hand_near`), `F_sword_short` + `F_scabbard_short`, `F_scabbard_long` (con của `hip`). Tuyệt kỹ có thể rút đoản kiếm: thêm lớp `F_sword_short_drawn` (ẩn), con của `hand_far`.
- Tóc: `F_ponytail_1`, `F_ponytail_2` (xương `ponytail_1 → ponytail_2` con của `head`).
- Dải màu đội: `F_team_headband` (băng trán) + `F_team_headband_tail_1`, `F_team_headband_tail_2` (xương `headband_tail_1 → headband_tail_2`).
- Bàn tay xa có 2 lớp: `F_hand_far_open`, `F_hand_far_swordfinger` (bắt kiếm quyết khi tuyệt kỹ).
- Clip đánh thường (1.0 s ở nhịp chuẩn; tốc đánh 1.4 → chạy ≈ 0.71 s): 0–0.4 s giơ kiếm (**vung**), 0.4 s chém chéo (**`Hit`**), 0.4–1.0 s thu kiếm.
