# Mưu sĩ – prompt tạo ảnh (dán nguyên văn)

Phong cách đã chốt: **A – chibi ~2.5 đầu, render 3D** theo ảnh mẫu `chutuong1.png` (cùng thư mục này).
Công cụ: **ChatGPT** (tạo ảnh). Làm cả Mưu sĩ trong **một cuộc trò chuyện**. Kết quả lưu **vào chính thư mục này** (`docs/art/concepts/`) với tên ghi dưới mỗi prompt.

Giải thích từng ảnh dùng để làm gì, cách tách bộ phận và gắn xương: `docs/StrategistArtGuide.md`.

| # | Ảnh | Đính kèm | Lưu thành |
|---|---|---|---|
| 1 | Bảng xoay 4 hướng (ảnh gốc) | `chutuong1.png` | `strategist_turnaround.png` |
| 2 | Tư thế chuẩn hướng trước-phải | `strategist_turnaround.png` | `strategist_rig_front.png` |
| 3 | Tư thế chuẩn hướng sau-phải | `strategist_turnaround.png` + `strategist_rig_front.png` | `strategist_rig_back.png` |
| 4 | 4 biểu cảm | `strategist_rig_front.png` | `strategist_faces.png` |
| 5 | Vá phần bị che (mỗi bộ phận một lần) | PNG bộ phận vừa cắt + `strategist_rig_front.png` | dán lại vào Krita |
| 6 | Tư thế tham khảo animation (tuỳ chọn) | `strategist_rig_front.png` | `strategist_poses.png` |

Nếu phong cách bị "trôi" (mất vẻ render 3D, sai tỉ lệ), gửi kèm thêm `chutuong1.png` và nói: `keep the exact rendering style of this reference`.

---

## Ảnh 1 – Bảng xoay 4 hướng
Đính kèm: `chutuong1.png` · Tạo 2–3 lần, giữ tấm đẹp nhất · Lưu: `strategist_turnaround.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, big expressive eyes, small body. Soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Character: a young female military strategist heroine from ancient East Asia (about 25 years old), calm, clever and confident, gentle slight smile, sharp intelligent eyes, slender elegant build, NO armour. Ivory inner robe with a crossed Hanfu collar; long lavender outer robe (#B9A3D6) reaching the ankles with wide sleeves hanging to the hips, both hands visible; deep violet trim (#6B4E8C) with a pale-gold cloud pattern; the front of the robe split so the legs can step; ivory cloth shoes with cloud-shaped toes. A wide cloth sash with two long hanging tails in front, painted neutral light grey (#BDBDBD); a round jade bagua pendant on the sash; a small rolled bamboo-slip strategy book tied at the left hip. Dark brown-black hair in a tall neat bun with a silver crescent-moon hairpin, two long locks framing the face. A white crane-feather fan with grey feather tips, jade-green handle and small violet tassel, held in the right hand, about the size of her head. Nothing covers the face, no cape, no hat.

Character turnaround sheet: four full-body views of the SAME character in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) A-pose front view with arms slightly away from the body and the fan held upright not overlapping the body.
```

---

## Ảnh 2 – Tư thế chuẩn hướng TRƯỚC-PHẢI
Đính kèm: `strategist_turnaround.png` · Lưu: `strategist_rig_front.png`
```
Keep the same rendering style as the attached turnaround sheet: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, big expressive eyes, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Using the attached turnaround sheet, draw exactly the same character: the young female strategist with the lavender robe with wide sleeves, deep violet trim, ivory inner robe, grey sash with two hanging tails, jade bagua pendant, bamboo-slip book at the left hip, tall bun with a silver crescent-moon hairpin, and the white crane-feather fan in the right hand. Same face, outfit, colours, fan and proportions.

One full-body image, FRONT-RIGHT three-quarter view (body turned 45 degrees to the right of the viewer), camera looking DOWN from above at about 40 degrees, orthographic, no perspective distortion.
Neutral rig pose: standing straight, both arms held slightly away from the body (about 25 degrees) so nothing overlaps the torso, hands clearly outside the sleeves, right hand holding the fan upright beside the body (not in front of the face or torso), left hand open and relaxed, feet slightly apart, sash tails hanging straight, calm face.
Plain flat white background, no ground shadow, high resolution, the character fills about 85 % of the image height.
```

---

## Ảnh 3 – Tư thế chuẩn hướng SAU-PHẢI
Đính kèm: `strategist_turnaround.png` + `strategist_rig_front.png` · Lưu: `strategist_rig_back.png`
```
Keep the same rendering style as the attached images: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. No text, no watermark.

Using the attached images, draw exactly the same character (same outfit, colours, fan and proportions) in the SAME neutral rig pose as the front-right image, but seen from BEHIND: BACK-RIGHT three-quarter view (her back towards the viewer, body turned 45 degrees to the right, as if walking away up-right), camera looking DOWN from above at about 40 degrees, orthographic.
Both arms slightly away from the body, hands outside the sleeves, the fan held upright in the right hand and partly visible, feet slightly apart. The bun, crescent-moon hairpin, the sash knot at the back, the back of the wide sleeves and the robe must match the turnaround sheet.
Plain flat white background, no ground shadow, high resolution, same scale as the attached front-right image.
```

---

## Ảnh 4 – 4 biểu cảm
Đính kèm: `strategist_rig_front.png` · Lưu: `strategist_faces.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, big expressive eyes, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

Using the attached front-right image, an expression sheet of the SAME head only (face, hair and crescent-moon hairpin included), FRONT-RIGHT three-quarter view from about 40 degrees above, identical size, angle and position for all heads, 4 heads in a row, evenly spaced, on a plain white background:
1) calm with a gentle slight smile,
2) shouting a command (mouth open, eyebrows down, determined),
3) hurt (wincing, one eye shut),
4) knocked out (eyes closed, slightly dizzy).
```

---

## Ảnh 5 – Vá phần bị che (dùng trong lúc tách bộ phận ở Krita)
Đính kèm: PNG của **một** bộ phận vừa cắt + `strategist_rig_front.png` · Thay `[...]` bằng tên bộ phận (ví dụ `torso`, `right upper arm`, `front robe piece`, `left wide sleeve`)
```
Keep the same rendering style as the attached character image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline.

The first attached image is an isolated body part cut from the character in the second image: [torso].
Complete the missing or hidden area so the part is whole, keep EXACTLY the same style, colours, size, angle and edges of the visible area, do not change the visible pixels. If the part connects to another part at a joint, extend it a little past the joint with a rounded end.
Plain flat white background, only this one part in the image, no text.
```

---

## Ảnh 6 (tuỳ chọn) – Tư thế tham khảo cho animation
Đính kèm: `strategist_rig_front.png` · Lưu: `strategist_poses.png` (không dùng trong game, chỉ để dựng clip)
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

A pose reference sheet of the attached character, FRONT-RIGHT three-quarter view from about 40 degrees above, 6 full-body key poses in a row on a plain white background, same size:
1) idle, holding the fan casually,
2) mid-stride walk,
3) fan raised behind the shoulder (attack wind-up),
4) fan swept forward releasing a gust of wind (attack strike),
5) both arms raised with the wide sleeves billowing (ultimate skill),
6) fallen on the ground, eyes closed.
```
