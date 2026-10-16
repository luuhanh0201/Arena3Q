# Cung thủ – prompt tạo ảnh (dán nguyên văn)

Phong cách đã chốt: **A – chibi ~2.5 đầu, render 3D** theo ảnh mẫu `chutuong1.png` (cùng thư mục này).
Công cụ: **ChatGPT** (tạo ảnh). Làm cả Cung thủ trong **một cuộc trò chuyện**. Kết quả lưu **vào chính thư mục này** với tên ghi dưới mỗi prompt.
Thiết kế và kỹ năng: `docs/LordHeroes.md` §1, §2.1. Quy trình tách bộ phận, gắn xương, animation: `docs/StrategistArtGuide.md` (làm y hệt Mưu sĩ, khác biệt ghi ở cuối file này).

**Tóm tắt nhân vật:** nữ, cung thủ du hiệp, nhanh nhẹn; tầm xa (4.5), máu thấp – sát thương cao; tốc đánh 0.9 đòn/giây. Màu chủ đạo **xanh lục ngọc + nâu da**. Nhận ra từ xa nhờ: **tóc đuôi ngựa cao**, **cây cung sừng cao gần bằng người**, **ống tên sau lưng**, áo choàng ngắn có mũ trùm.

| Phần | Màu | Mã |
|---|---|---|
| Áo, áo choàng | Xanh lục ngọc | `#5BAA8A` |
| Giáp da, bao tay, ủng | Nâu da | `#B98A5E` |
| Viền, dây | Nâu đậm | `#4A3426` |
| Áo trong | Kem | `#EFE3C8` |
| Chi tiết kim loại | Vàng đồng | `#C9A55A` |
| Tóc | Đen nâu | `#2E2320` |
| Dải màu đội (ruy băng tóc + ruy băng buộc cung) | Xám nhạt trung tính | `#BDBDBD` |

| # | Ảnh | Đính kèm | Lưu thành |
|---|---|---|---|
| 1 | Bảng xoay 4 hướng (ảnh gốc) | `chutuong1.png` | `archer_turnaround.png` |
| 2 | Tư thế chuẩn hướng trước-phải | `archer_turnaround.png` | `archer_rig_front.png` |
| 3 | Tư thế chuẩn hướng sau-phải | `archer_turnaround.png` + `archer_rig_front.png` | `archer_rig_back.png` |
| 4 | 4 biểu cảm | `archer_rig_front.png` | `archer_faces.png` |
| 5 | Vá phần bị che (mỗi bộ phận một lần) | PNG bộ phận vừa cắt + `archer_rig_front.png` | dán lại vào Krita |
| 6 | Tư thế tham khảo animation (tuỳ chọn) | `archer_rig_front.png` | `archer_poses.png` |

Nếu phong cách bị "trôi", gửi kèm thêm `chutuong1.png` và nói: `keep the exact rendering style of this reference`.

---

## Ảnh 1 – Bảng xoay 4 hướng
Đính kèm: `chutuong1.png` · Tạo 2–3 lần, giữ tấm đẹp nhất · Lưu: `archer_turnaround.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, big expressive eyes, small body. Soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Character: a young female archer heroine from ancient East Asia (about 20 years old), an agile wandering ranger, focused and confident, sharp calm eyes, light athletic build. Jade-green (#5BAA8A) fitted tunic with a cream inner collar, tan leather (#B98A5E) light armour: chest guard, shoulder guard on the left shoulder, leather bracers on both forearms, a wide belt and knee-high leather boots; dark brown (#4A3426) trim and straps, small bronze buckles. A short jade-green half-cape with a hood resting on her shoulders (hood down). Dark brown-black hair in a high ponytail with a bronze hairpin, the ponytail tied with a ribbon painted neutral light grey (#BDBDBD). A tall recurve horn bow almost as tall as her body held in the LEFT hand, a thin taut bowstring, a neutral light grey ribbon tied on the bow grip. A cylindrical leather quiver full of feathered arrows on her back, arrow tips up over the right shoulder. Nothing covers the face.

Character turnaround sheet: four full-body views of the SAME character in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) A-pose front view with arms slightly away from the body and the bow held upright beside the body not overlapping it.
```

---

## Ảnh 2 – Tư thế chuẩn hướng TRƯỚC-PHẢI
Đính kèm: `archer_turnaround.png` · Lưu: `archer_rig_front.png`
```
Keep the same rendering style as the attached turnaround sheet: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, big expressive eyes, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Using the attached turnaround sheet, draw exactly the same character: the young female archer with the jade-green tunic and short hooded half-cape, tan leather armour, bracers and boots, high ponytail with the grey ribbon, the tall recurve horn bow with a grey ribbon in her left hand and the quiver of arrows on her back. Same face, outfit, colours, bow and proportions.

One full-body image, FRONT-RIGHT three-quarter view (body turned 45 degrees to the right of the viewer), camera looking DOWN from above at about 40 degrees, orthographic, no perspective distortion.
Neutral rig pose: standing straight, both arms held slightly away from the body (about 25 degrees) so nothing overlaps the torso, the left hand holding the bow upright beside the body (the bow not crossing the torso or face), the right hand open and relaxed, feet slightly apart, ponytail and cape hanging naturally, calm face.
Plain flat white background, no ground shadow, high resolution, the character fills about 85 % of the image height.
```

---

## Ảnh 3 – Tư thế chuẩn hướng SAU-PHẢI
Đính kèm: `archer_turnaround.png` + `archer_rig_front.png` · Lưu: `archer_rig_back.png`
```
Keep the same rendering style as the attached images: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. No text, no watermark.

Using the attached images, draw exactly the same character (same outfit, colours, bow and proportions) in the SAME neutral rig pose as the front-right image, but seen from BEHIND: BACK-RIGHT three-quarter view (her back towards the viewer, body turned 45 degrees to the right, as if walking away up-right), camera looking DOWN from above at about 40 degrees, orthographic.
Both arms slightly away from the body, the bow held upright in the left hand and partly visible, feet slightly apart. The ponytail with its grey ribbon, the hood resting on the cape, the quiver with arrows on her back and the back of the leather armour must match the turnaround sheet.
Plain flat white background, no ground shadow, high resolution, same scale as the attached front-right image.
```

---

## Ảnh 4 – 4 biểu cảm
Đính kèm: `archer_rig_front.png` · Lưu: `archer_faces.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, big expressive eyes, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

Using the attached front-right image, an expression sheet of the SAME head only (face, hair, ponytail and hairpin included), FRONT-RIGHT three-quarter view from about 40 degrees above, identical size, angle and position for all heads, 4 heads in a row, evenly spaced, on a plain white background:
1) calm and focused, slight confident smile,
2) shouting a command (mouth open, eyebrows down, determined),
3) hurt (wincing, one eye shut),
4) knocked out (eyes closed, slightly dizzy).
```

---

## Ảnh 5 – Vá phần bị che (dùng trong lúc tách bộ phận ở Krita)
Đính kèm: PNG của **một** bộ phận vừa cắt + `archer_rig_front.png` · Thay `[...]` bằng tên bộ phận (ví dụ `torso`, `left forearm with bracer`, `quiver`, `bow`)
```
Keep the same rendering style as the attached character image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline.

The first attached image is an isolated body part cut from the character in the second image: [torso].
Complete the missing or hidden area so the part is whole, keep EXACTLY the same style, colours, size, angle and edges of the visible area, do not change the visible pixels. If the part connects to another part at a joint, extend it a little past the joint with a rounded end.
Plain flat white background, only this one part in the image, no text.
```

---

## Ảnh 6 (tuỳ chọn) – Tư thế tham khảo cho animation
Đính kèm: `archer_rig_front.png` · Lưu: `archer_poses.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

A pose reference sheet of the attached character, FRONT-RIGHT three-quarter view from about 40 degrees above, 6 full-body key poses in a row on a plain white background, same size:
1) idle, twirling an arrow in the right hand,
2) light running stride,
3) drawing the bow fully, arrow nocked, aiming forward (attack wind-up),
4) the moment after release, bowstring snapped forward, right hand open behind the ear (attack strike),
5) aiming a huge glowing arrow diagonally upward with the bow fully drawn (ultimate skill),
6) fallen on the ground, eyes closed, bow dropped beside her.
```

---

## Khác với Mưu sĩ khi tách bộ phận và gắn xương
- **Không** có tay áo rộng (`sleeve_*`) và vạt áo dài (`robe_*`); thay bằng `F_cape_1`, `F_cape_2` (áo choàng 2 mảnh) và chân đầy đủ (`leg_*_upper`, `leg_*_lower`, `foot_*`).
- Cung cầm ở **tay xa** (`hand_far` → `bow`); **dây cung tách lớp riêng** `F_bowstring` (kéo căng khi bắn, xương `bowstring` con của `bow`).
- Thêm `F_quiver` (con của `chest`), `F_arrow` (mũi tên đang cầm/lên dây, con của `hand_near`), `F_ponytail_1`, `F_ponytail_2` (xương `ponytail_1 → ponytail_2` con của `head`).
- Dải màu đội: `F_team_ribbon_hair` (ruy băng tóc), `F_team_ribbon_bow` (ruy băng trên cung).
- Bàn tay gần có 2 lớp: `F_hand_near_open`, `F_hand_near_pinch` (kẹp tên kéo dây).
- Clip đánh thường (1.0 s ở nhịp chuẩn): 0–0.4 s kéo dây (**vung**), 0.4 s buông (**`Hit`**, mũi tên bay là hiệu ứng của game), 0.4–1.0 s hạ cung.
