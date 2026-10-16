# Thống soái – prompt tạo ảnh (dán nguyên văn)

Phong cách đã chốt: **A – chibi ~2.5 đầu, render 3D** theo ảnh mẫu `chutuong1.png` (cùng thư mục này).
Công cụ: **ChatGPT** (tạo ảnh). Làm cả Thống soái trong **một cuộc trò chuyện**. Kết quả lưu **vào chính thư mục này** với tên ghi dưới mỗi prompt.
Thiết kế và kỹ năng: `docs/LordHeroes.md` §1, §2.4. Quy trình tách bộ phận, gắn xương, animation: `docs/StrategistArtGuide.md` (làm y hệt Mưu sĩ, khác biệt ghi ở cuối file này).

**Tóm tắt nhân vật:** nam trung niên, đại tướng chỉ huy; cận chiến bằng thương dài (tầm 0.9), máu cao – sát thương trung; tốc đánh 1.0 đòn/giây; chạy chậm. Màu chủ đạo **đỏ son + vàng đồng**. Nhận ra từ xa nhờ: **mũ có chùm lông cao**, **cờ lệnh cắm sau lưng**, **thương dài**, áo choàng đỏ, râu quai nón, dáng to chắc.

> `chutuong1.png` cũng là chiến tướng giáp vàng – đỏ. Thống soái **không được** giống nhân vật đó: già dặn hơn, có râu, đội mũ trụ, giáp nặng màu đồng thẫm, cầm thương (không cầm nỏ), cờ lệnh sau lưng.

| Phần | Màu | Mã |
|---|---|---|
| Áo choàng, vải giáp | Đỏ son | `#A3262A` |
| Giáp lá | Đồng thẫm | `#B07A3A` |
| Viền, hoa văn | Vàng | `#D4A84A` |
| Giáp trong, giày | Sắt tối | `#3A3A40` |
| Cán thương, cán cờ | Gỗ nâu đỏ | `#6B3A26` |
| Tóc, râu | Đen pha xám | `#2B2624` |
| Dải màu đội (chùm lông mũ + vải cờ lệnh) | Xám nhạt trung tính | `#BDBDBD` |

| # | Ảnh | Đính kèm | Lưu thành |
|---|---|---|---|
| 1 | Bảng xoay 4 hướng (ảnh gốc) | `chutuong1.png` | `marshal_turnaround.png` |
| 2 | Tư thế chuẩn hướng trước-phải | `marshal_turnaround.png` | `marshal_rig_front.png` |
| 3 | Tư thế chuẩn hướng sau-phải | `marshal_turnaround.png` + `marshal_rig_front.png` | `marshal_rig_back.png` |
| 4 | 4 biểu cảm | `marshal_rig_front.png` | `marshal_faces.png` |
| 5 | Vá phần bị che (mỗi bộ phận một lần) | PNG bộ phận vừa cắt + `marshal_rig_front.png` | dán lại vào Krita |
| 6 | Tư thế tham khảo animation (tuỳ chọn) | `marshal_rig_front.png` | `marshal_poses.png` |

Nếu phong cách bị "trôi", gửi kèm thêm `chutuong1.png` và nói: `keep the exact rendering style of this reference`.

---

## Ảnh 1 – Bảng xoay 4 hướng
Đính kèm: `chutuong1.png` · Tạo 2–3 lần, giữ tấm đẹp nhất · Lưu: `marshal_turnaround.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, face, outfit, colours or weapon - this must be a clearly different, older character. Chibi proportions about 2.5 heads tall, large head, big expressive eyes, small but broad body. Soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Character: a middle-aged male grand marshal from ancient East Asia (about 45 years old), stern, powerful and commanding, thick eyebrows, a full short black beard with a few grey strands, broad sturdy build. Heavy dark bronze (#B07A3A) lamellar armour over crimson (#A3262A) cloth, large shoulder guards, a gold (#D4A84A) tiger-face belt buckle, armoured skirt panels, dark iron (#3A3A40) greaves and boots. A rounded iron war helmet with gold trim and a tall plume on top, the plume painted neutral light grey (#BDBDBD). A long crimson war cape from the shoulders to the calves. A long spear with a dark red-brown wooden shaft and a broad steel blade held upright in the RIGHT hand. A command banner on a thin pole strapped to his back, rising above the helmet, the banner cloth painted neutral light grey (#BDBDBD) with a simple plain border. Nothing covers the face.

Character turnaround sheet: four full-body views of the SAME character in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) A-pose front view with arms slightly away from the body and the spear held upright beside the body not overlapping it.
```

---

## Ảnh 2 – Tư thế chuẩn hướng TRƯỚC-PHẢI
Đính kèm: `marshal_turnaround.png` · Lưu: `marshal_rig_front.png`
```
Keep the same rendering style as the attached turnaround sheet: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, big expressive eyes, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Using the attached turnaround sheet, draw exactly the same character: the bearded middle-aged grand marshal with dark bronze lamellar armour over crimson cloth, the iron helmet with the tall grey plume, the long crimson cape, the long spear in his right hand and the grey command banner on a pole on his back. Same face, outfit, colours, spear, banner and proportions.

One full-body image, FRONT-RIGHT three-quarter view (body turned 45 degrees to the right of the viewer), camera looking DOWN from above at about 40 degrees, orthographic, no perspective distortion.
Neutral rig pose: standing straight and firm, both arms held slightly away from the body (about 25 degrees) so nothing overlaps the torso, the right hand holding the spear upright beside the body (the spear not crossing the torso or face), the left hand open and relaxed, feet slightly apart, cape hanging straight behind, banner cloth hanging loosely, calm stern face.
Plain flat white background, no ground shadow, high resolution, the character including the banner fills about 90 % of the image height.
```

---

## Ảnh 3 – Tư thế chuẩn hướng SAU-PHẢI
Đính kèm: `marshal_turnaround.png` + `marshal_rig_front.png` · Lưu: `marshal_rig_back.png`
```
Keep the same rendering style as the attached images: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. No text, no watermark.

Using the attached images, draw exactly the same character (same armour, colours, spear, banner and proportions) in the SAME neutral rig pose as the front-right image, but seen from BEHIND: BACK-RIGHT three-quarter view (his back towards the viewer, body turned 45 degrees to the right, as if walking away up-right), camera looking DOWN from above at about 40 degrees, orthographic.
Both arms slightly away from the body, the spear held upright in the right hand, feet slightly apart. The long crimson cape covering the back, the banner pole strapped over the cape with the grey banner cloth, the back of the helmet with the grey plume must match the turnaround sheet.
Plain flat white background, no ground shadow, high resolution, same scale as the attached front-right image.
```

---

## Ảnh 4 – 4 biểu cảm
Đính kèm: `marshal_rig_front.png` · Lưu: `marshal_faces.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, big expressive eyes, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

Using the attached front-right image, an expression sheet of the SAME head only (face, beard and helmet with plume included), FRONT-RIGHT three-quarter view from about 40 degrees above, identical size, angle and position for all heads, 4 heads in a row, evenly spaced, on a plain white background:
1) calm and stern, mouth closed,
2) roaring a command (mouth wide open, eyebrows down, powerful),
3) hurt (gritting teeth, one eye shut),
4) knocked out (eyes closed, head slightly lowered).
```

---

## Ảnh 5 – Vá phần bị che (dùng trong lúc tách bộ phận ở Krita)
Đính kèm: PNG của **một** bộ phận vừa cắt + `marshal_rig_front.png` · Thay `[...]` bằng tên bộ phận (ví dụ `torso with armour`, `cape upper piece`, `banner cloth`, `spear`)
```
Keep the same rendering style as the attached character image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline.

The first attached image is an isolated body part cut from the character in the second image: [torso with armour].
Complete the missing or hidden area so the part is whole, keep EXACTLY the same style, colours, size, angle and edges of the visible area, do not change the visible pixels. If the part connects to another part at a joint, extend it a little past the joint with a rounded end.
Plain flat white background, only this one part in the image, no text.
```

---

## Ảnh 6 (tuỳ chọn) – Tư thế tham khảo cho animation
Đính kèm: `marshal_rig_front.png` · Lưu: `marshal_poses.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

A pose reference sheet of the attached character, FRONT-RIGHT three-quarter view from about 40 degrees above, 6 full-body key poses in a row on a plain white background, same size:
1) idle, leaning slightly on the upright spear,
2) heavy marching stride,
3) spear pulled back at waist height ready to thrust (attack wind-up),
4) full forward spear thrust (attack strike),
5) planting the spear and raising the left fist high while shouting, cape billowing (command / ultimate skill),
6) kneeling on one knee leaning on the spear, head lowered (defeated).
```

---

## Khác với Mưu sĩ khi tách bộ phận và gắn xương
Danh sách lớp đầy đủ, cây xương, bảng clip và VFX theo sự kiện: `docs/LordAnimations.md`.
- **Không** có tay áo rộng; thay bằng `F_shoulder_guard_near`, `F_shoulder_guard_far` (con của `arm_*_upper`). Váy giáp `F_skirt_front`, `F_skirt_back` (xương `skirt_*` 2 đoạn); chân đầy đủ.
- **Áo choàng** 2 mảnh: `F_cape_1`, `F_cape_2` (xương `cape_1 → cape_2` con của `chest`), nằm **dưới** thân ở hướng trước, **trên** thân ở hướng sau.
- Vũ khí và đồ sau lưng: `F_spear` (con của `hand_near`), `F_banner_pole` (con của `chest`), `F_banner_cloth` (xương `banner_1 → banner_2` để vải bay).
- Đầu: `F_helmet` (con của `head`), `F_beard` (cùng nhóm mặt nếu muốn mở miệng khi hô).
- Dải màu đội: `F_team_plume` (chùm lông mũ, xương `plume`) và `F_banner_cloth` (vải cờ – cũng là màu đội).
- Clip đánh thường (1.0 s ở nhịp chuẩn): 0–0.4 s thu thương về hông (**vung**), 0.4 s đâm tới (**`Hit`**), 0.4–1.0 s rút về. Clip `die`: quỳ một gối chống thương rồi gục (`LordArtPrompts.md` §7).
