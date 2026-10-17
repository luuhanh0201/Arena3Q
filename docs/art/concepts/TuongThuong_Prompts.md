# Tướng thường – prompt tạo ảnh (dán nguyên văn)

Phong cách đã chốt: **A – chibi ~2.5 đầu, render 3D** theo ảnh mẫu `chutuong1.png` (cùng thư mục này).
Công cụ: **ChatGPT** (tạo ảnh). Làm **mỗi tướng trong một cuộc trò chuyện riêng**. Kết quả lưu **vào chính thư mục này** với tên ghi dưới mỗi prompt.
Thiết kế, kỹ năng, lớp PSD, xương và clip: `docs/Tuong/NormalHeroes.md`. Quy trình tách bộ phận và gắn xương: `docs/Tuong/StrategistArtGuide.md` (làm y hệt Mưu sĩ).

> ⚠️ Tên **Xạ Thủ** và **Đao Khách** là tên đề xuất (trùng tên chủ tướng – spec §13 mục 4). Tên file ảnh dùng mã tiếng Anh (`hunter`, `cavalry`, `guardian`, `blade`) nên **không phải đổi** dù tên tiếng Việt thay đổi.

Mỗi tướng có các ảnh sau (thay `<mã>` bằng `hunter` / `cavalry` / `guardian` / `blade`):

| # | Ảnh | Đính kèm | Lưu thành |
|---|---|---|---|
| 1 | Bảng xoay 4 hướng (ảnh gốc) | `chutuong1.png` | `<mã>_turnaround.png` |
| 2 | Tư thế chuẩn hướng trước-phải | `<mã>_turnaround.png` | `<mã>_rig_front.png` |
| 3 | Tư thế chuẩn hướng sau-phải | `<mã>_turnaround.png` + `<mã>_rig_front.png` | `<mã>_rig_back.png` |
| 4 | 4 biểu cảm | `<mã>_rig_front.png` | `<mã>_faces.png` |
| 5 | Vá phần bị che | PNG bộ phận + `<mã>_rig_front.png` | dán lại vào Krita – dùng **prompt ảnh 5 chung** ở cuối file |
| 6 | Tư thế tham khảo animation (tuỳ chọn) | `<mã>_rig_front.png` | `<mã>_poses.png` |

Nếu phong cách bị "trôi", gửi kèm thêm `chutuong1.png` và nói: `keep the exact rendering style of this reference`.
Tướng thường phải **không giống chủ tướng**: nếu ảnh ra giống Cung thủ / Kiếm sĩ / Thống soái, nhắc lại `clearly different from the lord heroes` và gửi kèm ảnh chủ tướng để so.

---

# A. Xạ Thủ (`hunter`) – nam, thợ săn núi, cung ngắn

**Tóm tắt:** nam ~30 tuổi, thợ săn vùng núi lạnh, ít nói, mắt tinh; săn một mục tiêu, kết liễu. Nhận ra từ xa nhờ: **mũ lông thú có đuôi**, **áo lông vai**, **cung ngắn cong mạnh**, **ống tên đeo ngang hông** (khác chủ tướng Cung thủ: nữ, ống tên sau lưng, cung dài).

| Phần | Màu | Mã |
|---|---|---|
| Áo khoác | Nâu hạt dẻ | `#8A5A3B` |
| Áo trong, viền | Vàng lúa | `#D8B25A` |
| Lông thú (mũ, vai) | Xám nâu | `#9C8B78` |
| Quần, giày, dây | Nâu đậm | `#3E2B20` |
| Cung | Gỗ sừng nâu đen + đồng | `#4A3426` / `#C9A55A` |
| Dải màu đội (khăn quàng cổ) | Xám nhạt trung tính | `#BDBDBD` |

## A1 – Bảng xoay 4 hướng → `hunter_turnaround.png`
Đính kèm: `chutuong1.png` · Tạo 2–3 lần, giữ tấm đẹp nhất
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, big expressive eyes, small body. Soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Character: a male mountain hunter archer from ancient East Asia (about 30 years old), quiet and sharp-eyed, short stubble, sturdy lean build. A chestnut-brown (#8A5A3B) wrap-front hunting coat reaching mid-thigh with wheat-gold (#D8B25A) trim, a grey-brown fur mantle over the shoulders, dark brown (#3E2B20) trousers wrapped with cloth bands, fur-lined boots, leather bracers. A round grey-brown fur hat with a fluffy animal tail hanging down the back. A cloth scarf around the neck painted neutral light grey (#BDBDBD). A short, strongly curved composite horn bow (dark brown with bronze tips) held in the LEFT hand, a leather quiver of arrows worn horizontally on the RIGHT hip. Nothing covers the face. Clearly different from a female ranger: male, stocky, fur hat, hip quiver, short bow.

Character turnaround sheet: four full-body views of the SAME character in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) A-pose front view with arms slightly away from the body and the bow held upright beside the body not overlapping it.
```

## A2 – Tư thế chuẩn TRƯỚC-PHẢI → `hunter_rig_front.png`
Đính kèm: `hunter_turnaround.png`
```
Keep the same rendering style as the attached turnaround sheet: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, big expressive eyes, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Using the attached turnaround sheet, draw exactly the same character: the male mountain hunter with the chestnut-brown coat, fur mantle, fur hat with a tail, grey scarf, short curved horn bow in his left hand and the quiver on his right hip. Same face, outfit, colours, bow and proportions.

One full-body image, FRONT-RIGHT three-quarter view (body turned 45 degrees to the right of the viewer), camera looking DOWN from above at about 40 degrees, orthographic, no perspective distortion.
Neutral rig pose: standing straight, both arms held slightly away from the body (about 25 degrees) so nothing overlaps the torso, the left hand holding the bow upright beside the body (not crossing the torso or face), the right hand open and relaxed, feet slightly apart, hat tail and coat hanging naturally, calm face.
Plain flat white background, no ground shadow, high resolution, the character fills about 85 % of the image height.
```

## A3 – Tư thế chuẩn SAU-PHẢI → `hunter_rig_back.png`
Đính kèm: `hunter_turnaround.png` + `hunter_rig_front.png`
```
Keep the same rendering style as the attached images: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. No text, no watermark.

Using the attached images, draw exactly the same character (same outfit, colours, bow and proportions) in the SAME neutral rig pose as the front-right image, but seen from BEHIND: BACK-RIGHT three-quarter view (his back towards the viewer, body turned 45 degrees to the right, as if walking away up-right), camera looking DOWN from above at about 40 degrees, orthographic.
Both arms slightly away from the body, the bow in the left hand partly visible, feet slightly apart. The fur hat with its tail hanging down the back, the fur mantle, the back of the coat and the quiver on the right hip must match the turnaround sheet.
Plain flat white background, no ground shadow, high resolution, same scale as the attached front-right image.
```

## A4 – 4 biểu cảm → `hunter_faces.png`
Đính kèm: `hunter_rig_front.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, big expressive eyes, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

Using the attached front-right image, an expression sheet of the SAME head only (face and fur hat included), FRONT-RIGHT three-quarter view from about 40 degrees above, identical size, angle and position for all heads, 4 heads in a row, evenly spaced, on a plain white background:
1) calm and watchful, mouth closed,
2) shouting (mouth open, eyebrows down, focused),
3) hurt (wincing, one eye shut),
4) knocked out (eyes closed, slightly dizzy).
```

## A6 – Tư thế tham khảo → `hunter_poses.png`
Đính kèm: `hunter_rig_front.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

A pose reference sheet of the attached character, FRONT-RIGHT three-quarter view from about 40 degrees above, 6 full-body key poses in a row on a plain white background, same size:
1) idle, looking around with one hand shading the eyes,
2) crouched hunter's running stride,
3) pulling an arrow from the hip quiver and drawing the short bow (attack wind-up),
4) the moment after release, bowstring snapped forward (attack strike),
5) kneeling on one knee drawing the bow to full strength, aiming forward (ultimate skill),
6) fallen on his back, eyes closed, bow dropped beside him.
```

---

# B. Kỵ Binh (`cavalry`) – nữ kỵ tướng, ngựa trắng, yển nguyệt đao

**Tóm tắt:** nữ ~24 tuổi, kỵ tướng dũng mãnh, cười tự tin; cưỡi **ngựa trắng** có giáp ngựa nhẹ, cầm **yển nguyệt đao** (đao cán dài lưỡi trăng khuyết). Nhận ra từ xa nhờ: **dáng người + ngựa** (to nhất trong các tướng), **dải lụa bay từ mũ**, **lưỡi đao trăng khuyết**.

| Phần | Màu | Mã |
|---|---|---|
| Áo, áo choàng, vải giáp ngựa | Xanh ngọc lam đậm | `#2F6F73` |
| Giáp lá, lưỡi đao | Bạc | `#C9CED6` |
| Viền, chi tiết | Vàng đồng | `#C9A55A` |
| Ngựa | Trắng xám, bờm bạc | `#EDEDED` / `#BFC4CC` |
| Yên, cương, cán đao | Nâu da / gỗ đỏ nâu | `#7A4E2E` / `#6B3A26` |
| Dải màu đội (dải lụa mũ + tua dưới cổ ngựa) | Xám nhạt trung tính | `#BDBDBD` |

## B1 – Bảng xoay 4 hướng → `cavalry_turnaround.png`
Đính kèm: `chutuong1.png` · Tạo 2–3 lần
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions: the rider about 2.5 heads tall with a large head and big expressive eyes; the horse is also chibi (big head, short sturdy legs, round body) and sized so that the rider and horse together are about 1.1 times as tall as they are wide. Soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Character: a young female cavalry general from ancient East Asia (about 24 years old), brave and confident, a bold grin, riding a white war horse. Dark teal (#2F6F73) riding robe with silver (#C9CED6) lamellar armour on the chest and shoulders, bronze trim, a short dark teal cape, leather riding boots in stirrups. A light silver helmet with two long silk ribbons flowing from the back, the ribbons painted neutral light grey (#BDBDBD). Long black hair in a high ponytail under the helmet. A guandao (a long polearm with a crescent-moon shaped single-edged blade, dark red-brown shaft) held in her RIGHT hand, the left hand holding the reins. The horse: white with a silver-grey mane and tail, a dark teal cloth barding with bronze trim, a brown leather saddle and reins, a neutral light grey (#BDBDBD) tassel hanging under its neck. Nothing covers the face.

Character turnaround sheet: four full-body views of the SAME rider on the SAME horse in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) side view facing right with the horse standing square on all four legs, the rider's arms slightly away from the body and the guandao held upright beside the horse's neck, not overlapping the rider or the horse's head.
```

## B2 – Tư thế chuẩn TRƯỚC-PHẢI → `cavalry_rig_front.png`
Đính kèm: `cavalry_turnaround.png`
```
Keep the same rendering style as the attached turnaround sheet: stylised 3D-rendered chibi figurine look, the rider about 2.5 heads tall, chibi horse, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Using the attached turnaround sheet, draw exactly the same rider and horse: the young female cavalry general in dark teal robe and silver armour, silver helmet with two grey ribbons, the guandao in her right hand, on the white horse with dark teal barding and the grey tassel under its neck. Same faces, outfit, colours, weapon and proportions.

One full-body image, FRONT-RIGHT three-quarter view (horse and rider turned 45 degrees to the right of the viewer), camera looking DOWN from above at about 40 degrees, orthographic, no perspective distortion.
Neutral rig pose: the horse standing square with all four legs clearly separated (legs not overlapping each other), head up, tail hanging; the rider sitting upright, the right arm slightly away from the body holding the guandao upright (the shaft not crossing the rider's face or the horse's head), the left hand holding the reins low, calm face.
Plain flat white background, no ground shadow, high resolution, horse and rider fill about 85 % of the image height.
```

## B3 – Tư thế chuẩn SAU-PHẢI → `cavalry_rig_back.png`
Đính kèm: `cavalry_turnaround.png` + `cavalry_rig_front.png`
```
Keep the same rendering style as the attached images: stylised 3D-rendered chibi figurine look, chibi rider and horse, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. No text, no watermark.

Using the attached images, draw exactly the same rider and horse in the SAME neutral rig pose as the front-right image, but seen from BEHIND: BACK-RIGHT three-quarter view (moving away up-right, the horse's hindquarters and tail towards the viewer, the horse's head partly visible beyond), camera looking DOWN from above at about 40 degrees, orthographic.
All four horse legs clearly separated, the rider's back with the short cape, the helmet with the two flowing grey ribbons, the guandao upright in the right hand, matching the turnaround sheet.
Plain flat white background, no ground shadow, high resolution, same scale as the attached front-right image.
```

## B4 – Biểu cảm → `cavalry_faces.png`
Đính kèm: `cavalry_rig_front.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, big expressive eyes, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

Using the attached front-right image, an expression sheet on a plain white background, FRONT-RIGHT three-quarter view from about 40 degrees above, identical size, angle and position for each item:
top row – the rider's head only (with helmet and ribbons), 4 heads: 1) confident grin, 2) battle cry (mouth wide open, fierce), 3) hurt (wincing, one eye shut), 4) knocked out (eyes closed, dizzy);
bottom row – the horse's head only, 2 heads: 5) calm, mouth closed, 6) neighing (mouth open, ears back).
```

## B6 – Tư thế tham khảo → `cavalry_poses.png`
Đính kèm: `cavalry_rig_front.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

A pose reference sheet of the attached rider and horse, FRONT-RIGHT three-quarter view from about 40 degrees above, 6 full-body key poses in a row on a plain white background, same size:
1) idle, the horse pawing the ground with one front hoof,
2) full gallop, the horse's legs stretched, cape and ribbons flying,
3) the guandao swung back ready to sweep (attack wind-up),
4) the guandao swept forward in a wide arc (attack strike),
5) the horse rearing on its hind legs and neighing, the rider raising the guandao (stomp skill),
6) the horse lying down with folded legs, the rider slumped forward, eyes closed (defeated).
```

---

# C. Giáp Sĩ (`guardian`) – nam, khiên lớn + đao ngắn

**Tóm tắt:** nam ~35 tuổi, vệ binh to con hiền lành, chắc như tường; đỡ đòn, bảo vệ quân ta. Nhận ra từ xa nhờ: **khiên chữ nhật lớn** (gần bằng người, mặt khiên hình đầu sư tử), **giáp nặng xám thép**, **mũ có tua**.

| Phần | Màu | Mã |
|---|---|---|
| Giáp | Xám thép | `#6C7480` |
| Viền giáp, mặt khiên | Đồng cam | `#C27A3A` |
| Vải, áo choàng ngắn | Nâu đỏ đậm | `#6E3B2E` |
| Giáp trong, giày | Sắt tối | `#3A3A40` |
| Đao ngắn | Thép | `#9AA4B2` |
| Dải màu đội (tua mũ + băng vải chéo trên khiên) | Xám nhạt trung tính | `#BDBDBD` |

## C1 – Bảng xoay 4 hướng → `guardian_turnaround.png`
Đính kèm: `chutuong1.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, big expressive eyes, small but very broad body. Soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Character: a big gentle male shield guardian from ancient East Asia (about 35 years old), calm and kind, small eyes, a short neat moustache, very broad and solid build. Heavy steel-grey (#6C7480) plate-and-lamellar armour with copper-orange (#C27A3A) trim, large rounded shoulder plates, an armoured skirt, dark iron (#3A3A40) greaves and boots, a short dark red-brown (#6E3B2E) cape. A round steel helmet with a tassel on top, the tassel painted neutral light grey (#BDBDBD). A large rectangular tower shield almost as tall as his body, copper-orange face embossed with a stylised lion head, a diagonal cloth band across the shield painted neutral light grey (#BDBDBD), held in the LEFT hand; a short broad single-edged saber in the RIGHT hand. Nothing covers the face.

Character turnaround sheet: four full-body views of the SAME character in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) A-pose front view with arms slightly away from the body, the shield held beside the body on the left (not covering the torso) and the saber pointing down on the right.
```

## C2 – Tư thế chuẩn TRƯỚC-PHẢI → `guardian_rig_front.png`
Đính kèm: `guardian_turnaround.png`
```
Keep the same rendering style as the attached turnaround sheet: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, big expressive eyes, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Using the attached turnaround sheet, draw exactly the same character: the big gentle shield guardian in steel-grey armour with copper trim, the helmet with the grey tassel, the tower shield with the lion head and grey band in his left hand, the short saber in his right hand. Same face, outfit, colours, weapons and proportions.

One full-body image, FRONT-RIGHT three-quarter view (body turned 45 degrees to the right of the viewer), camera looking DOWN from above at about 40 degrees, orthographic, no perspective distortion.
Neutral rig pose: standing firm, both arms held slightly away from the body (about 25 degrees), the left hand holding the tower shield upright BESIDE the body on the far side so the torso stays visible (the shield seen at an angle, not covering the body), the right hand holding the saber pointing down and slightly outward, feet apart, calm face.
Plain flat white background, no ground shadow, high resolution, the character fills about 85 % of the image height.
```

## C3 – Tư thế chuẩn SAU-PHẢI → `guardian_rig_back.png`
Đính kèm: `guardian_turnaround.png` + `guardian_rig_front.png`
```
Keep the same rendering style as the attached images: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. No text, no watermark.

Using the attached images, draw exactly the same character (same armour, colours, shield, saber and proportions) in the SAME neutral rig pose as the front-right image, but seen from BEHIND: BACK-RIGHT three-quarter view (his back towards the viewer, body turned 45 degrees to the right, as if walking away up-right), camera looking DOWN from above at about 40 degrees, orthographic.
The back of the armour and the short cape, the helmet tassel, the BACK of the tower shield with its leather grip straps, the saber in the right hand partly visible, matching the turnaround sheet.
Plain flat white background, no ground shadow, high resolution, same scale as the attached front-right image.
```

## C4 – 4 biểu cảm → `guardian_faces.png`
Đính kèm: `guardian_rig_front.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, big expressive eyes, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

Using the attached front-right image, an expression sheet of the SAME head only (face and helmet with tassel included), FRONT-RIGHT three-quarter view from about 40 degrees above, identical size, angle and position for all heads, 4 heads in a row, evenly spaced, on a plain white background:
1) calm, kind and steady, mouth closed,
2) shouting a battle cry (mouth open, determined),
3) hurt (gritting teeth, one eye shut),
4) knocked out (eyes closed, slightly dizzy).
```

## C6 – Tư thế tham khảo → `guardian_poses.png`
Đính kèm: `guardian_rig_front.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

A pose reference sheet of the attached character, FRONT-RIGHT three-quarter view from about 40 degrees above, 6 full-body key poses in a row on a plain white background, same size:
1) idle, the tower shield resting on the ground in front of him,
2) heavy walking stride with the shield raised in front of the chest,
3) crouching behind the raised shield, saber drawn back (attack wind-up),
4) slashing with the saber over the edge of the shield (attack strike),
5) slamming the tower shield down into the ground with both hands, shouting (ultimate skill),
6) fallen face down, eyes closed, the shield lying beside him.
```

---

# D. Đao Khách (`blade`) – nữ, đại đao một lưỡi

**Tóm tắt:** nữ ~22 tuổi, nữ hiệp giang hồ phóng khoáng, nụ cười tinh nghịch; dồn sát thương một mục tiêu. Nhận ra từ xa nhờ: **đại đao một lưỡi to gần bằng người** vác trên vai, **áo khoác ngắn đỏ cam**, **bím tóc dài**, **vỏ đao đeo chéo sau lưng** (khác chủ tướng Kiếm sĩ: nam, song kiếm thẳng, áo trắng).

| Phần | Màu | Mã |
|---|---|---|
| Áo khoác ngắn | Đỏ cam | `#D2512E` |
| Áo trong, quần | Trắng ngà | `#F1E6D2` |
| Đai, bao tay, giày | Đen than | `#2A2626` |
| Lưỡi đao | Thép sáng, sống đao vàng đồng | `#B8C0CC` / `#C9A55A` |
| Tóc | Nâu đen | `#2E2320` |
| Dải màu đội (2 đuôi đai lưng) | Xám nhạt trung tính | `#BDBDBD` |

## D1 – Bảng xoay 4 hướng → `blade_turnaround.png`
Đính kèm: `chutuong1.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, big expressive eyes, small body. Soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Character: a young female wandering blade fighter from ancient East Asia (about 22 years old), free-spirited and cheeky, a playful confident smile, athletic build. A short vermilion-orange (#D2512E) cropped martial jacket with rolled sleeves over an ivory (#F1E6D2) inner top, ivory wide trousers tucked into charcoal-black (#2A2626) boots, charcoal wrist wraps, a wide charcoal waist sash with two long hanging tails painted neutral light grey (#BDBDBD). Dark brown hair with short bangs and one long thick braid down the back, a small bronze hair ornament. A huge single-edged broad dao (great saber) almost as tall as her body, bright steel blade with a bronze spine and a red-wrapped handle, held in the RIGHT hand and resting on her right shoulder; an empty black scabbard strapped diagonally across her back. Nothing covers the face. Clearly different from a male swordsman with twin straight swords.

Character turnaround sheet: four full-body views of the SAME character in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) A-pose front view with arms slightly away from the body and the great saber held pointing down and outward, not overlapping the body.
```

## D2 – Tư thế chuẩn TRƯỚC-PHẢI → `blade_rig_front.png`
Đính kèm: `blade_turnaround.png`
```
Keep the same rendering style as the attached turnaround sheet: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, big expressive eyes, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. No text, no watermark.

Using the attached turnaround sheet, draw exactly the same character: the young female blade fighter with the vermilion cropped jacket, ivory trousers, charcoal sash with two grey tails, long braid, the huge great saber in her right hand and the scabbard on her back. Same face, outfit, colours, weapon and proportions.

One full-body image, FRONT-RIGHT three-quarter view (body turned 45 degrees to the right of the viewer), camera looking DOWN from above at about 40 degrees, orthographic, no perspective distortion.
Neutral rig pose: standing straight, both arms held slightly away from the body (about 25 degrees), the right hand holding the great saber pointing down and outward away from the legs (not resting on the shoulder, not crossing the body), the left hand open and relaxed, feet slightly apart, braid and sash tails hanging naturally, calm smiling face.
Plain flat white background, no ground shadow, high resolution, the character fills about 85 % of the image height.
```

## D3 – Tư thế chuẩn SAU-PHẢI → `blade_rig_back.png`
Đính kèm: `blade_turnaround.png` + `blade_rig_front.png`
```
Keep the same rendering style as the attached images: stylised 3D-rendered chibi figurine look, about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. No text, no watermark.

Using the attached images, draw exactly the same character (same outfit, colours, weapon and proportions) in the SAME neutral rig pose as the front-right image, but seen from BEHIND: BACK-RIGHT three-quarter view (her back towards the viewer, body turned 45 degrees to the right, as if walking away up-right), camera looking DOWN from above at about 40 degrees, orthographic.
The long braid, the diagonal black scabbard on her back, the grey sash tails and the back of the jacket must match the turnaround sheet; the great saber in the right hand partly visible.
Plain flat white background, no ground shadow, high resolution, same scale as the attached front-right image.
```

## D4 – 4 biểu cảm → `blade_faces.png`
Đính kèm: `blade_rig_front.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, big expressive eyes, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

Using the attached front-right image, an expression sheet of the SAME head only (face, bangs, braid and hair ornament included), FRONT-RIGHT three-quarter view from about 40 degrees above, identical size, angle and position for all heads, 4 heads in a row, evenly spaced, on a plain white background:
1) playful confident smile, mouth closed,
2) fierce battle cry (mouth wide open, eyebrows down),
3) hurt (wincing, one eye shut),
4) knocked out (eyes closed, slightly dizzy).
```

## D6 – Tư thế tham khảo → `blade_poses.png`
Đính kèm: `blade_rig_front.png`
```
Keep the same rendering style as the attached image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline. No text, no watermark.

A pose reference sheet of the attached character, FRONT-RIGHT three-quarter view from about 40 degrees above, 6 full-body key poses in a row on a plain white background, same size:
1) idle, the great saber resting on her right shoulder,
2) running stride leaning forward, the saber dragged behind,
3) crouched low gripping the handle with both hands, saber drawn far back (heavy strike wind-up),
4) a powerful rising diagonal slash finished, saber high (heavy strike),
5) spinning with the saber outstretched in a full circle (ultimate skill),
6) kneeling then fallen sideways, eyes closed, the saber stuck in the ground beside her.
```

---

# Prompt ảnh 5 chung – Vá phần bị che (dùng trong lúc tách bộ phận ở Krita)
Đính kèm: PNG của **một** bộ phận vừa cắt + `<mã>_rig_front.png` · Thay `[...]` bằng tên bộ phận, ví dụ `torso with fur mantle`, `horse body with barding`, `tower shield`, `great saber`, `right upper arm`
```
Keep the same rendering style as the attached character image: stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline.

The first attached image is an isolated body part cut from the character in the second image: [torso with fur mantle].
Complete the missing or hidden area so the part is whole, keep EXACTLY the same style, colours, size, angle and edges of the visible area, do not change the visible pixels. If the part connects to another part at a joint, extend it a little past the joint with a rounded end.
Plain flat white background, only this one part in the image, no text.
```

**Kỵ Binh – lưu ý khi tách:** chân ngựa phía xa bị thân che gần hết → vá bằng prompt trên (`far front leg of the horse`, …); thân người cưỡi và yên tách riêng để người cưỡi nhún theo nhịp phi; chân người cưỡi chỉ cần **chân gần** (chân xa khuất sau thân ngựa).
