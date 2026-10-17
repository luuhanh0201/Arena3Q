# Chương 1 – Hắc Phong Trại (sơn tặc)

Vùng: **rừng trúc và đèo núi**, suối nhỏ, trại gỗ rào cọc nhọn. Thành địch = **trại sơn tặc** (cổng gỗ, tháp canh, cờ đen vẽ đầu sói). Phe địch đầu tiên, **hoàn toàn là người** – dễ hiểu, dạy người chơi các vai trò cơ bản.
Màu phe: **đen than + đỏ sẫm + nâu da**, khăn trùm đầu đỏ sẫm là dấu hiệu chung. Biểu tượng: **đầu sói đen**.

Chỉ số: 🟡 gợi ý, cân bằng sau (spec §13 ⏳). Tốc đánh tính bằng đòn/giây.
Animation (khung xương, clip, mốc cảnh báo / ra đòn): `Animation.md` · VFX: `VFX_Prompts.md` mục "Chương 1".

| Loại | Cấp | Vai trò | Máu | Sát thương | Tốc đánh | Tầm | Tốc chạy | Asset hiện có tương ứng |
|---|---|---|---|---|---|---|---|---|
| Lâu La Đao | Thường | cận chiến | 160 | 16 | 1.0 | 0.6 | 2.2 | `Enemy_Swordsman` |
| Lâu La Cung | Thường | tầm xa | 90 | 13 | 0.8 | 4.5 | 2.2 | `Enemy_Archer` |
| Đại Hán Búa | Thường | trâu | 600 | 35 | 0.67 | 0.7 | 1.6 | `Enemy_Brute` |
| Chó Săn Trại | Thường | nhanh, bầy đàn | 70 | 8 | 1.5 | 0.5 | 3.6 | – |
| Đầu Mục Song Phủ | Tinh anh | đột phá | 900 | 40 | 0.8 | 0.8 | 2.0 | – |
| Quân Sư Sơn Trại | Tinh anh | hỗ trợ | 650 | 18 | 0.7 | 3.5 | 1.9 | – |
| **Hắc Phong Trại Chủ** | **Boss** | boss | 6000 | 70 | 0.7 | 1.2 | 1.8 | – |

---

## Lính thường

### Lâu La Đao
Tên cướp gầy, khăn đỏ sẫm trùm đầu, áo vải rách vá, thắt lưng dây thừng, **đao lưng dày** sứt mẻ. Mặt tinh nghịch – láu cá.

### Lâu La Cung
Đội nón lá rách, khăn đỏ che mũi miệng, áo khoác da thú ngắn, **cung gỗ đơn giản**, ống tên bằng tre.

### Đại Hán Búa
To béo vạm vỡ (to gấp rưỡi lâu la), cởi trần mặc áo gile da gấu, râu rậm, **búa gỗ lớn bọc sắt** vác trên vai, khăn đỏ buộc trán.

### Chó Săn Trại
Chó sói đen lông xù, vòng cổ da có đinh, **khăn đỏ buộc cổ** (để nhận ra là quân sơn trại), mắt vàng. Chạy theo bầy 3–5 con.

**Prompt – lính thường** (mỗi loại một lần, đính kèm `docs/art/concepts/chutuong1.png`; thay khối `UNIT`):
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. Cute but mischievous, not scary, no blood. No text, no watermark.

UNIT

Enemy unit sheet for a mobile strategy game, plain white background, three full-body images of the SAME character side by side, same size: 1) FRONT-RIGHT three-quarter view and 2) BACK-RIGHT three-quarter view, both seen from above at about 40 degrees, standing in a neutral pose with arms slightly away from the body; 3) a dynamic attack pose, front-right three-quarter view. Simple, bold silhouette readable at very small size.
```
Thay `UNIT` bằng một trong các đoạn sau → lưu `c1_<tên>_sheet.png`:

`c1_saber_sheet.png`
```
Character: a skinny young mountain bandit grunt from ancient East Asia, a dark red cloth wrapped around the head, patched ragged charcoal-black tunic and trousers, rope belt, straw sandals, a heavy chipped broadsword (dao) in the right hand, sly mischievous grin.
```
`c1_archer_sheet.png`
```
Character: a mountain bandit archer from ancient East Asia, a torn conical straw hat, a dark red cloth covering the nose and mouth, a short fur-trimmed leather jacket over a charcoal-black tunic, a simple wooden bow in the left hand, a bamboo quiver on the back, sharp narrowed eyes.
```
`c1_brute_sheet.png`
```
Character: a huge burly bandit brute from ancient East Asia, about one and a half times wider than a normal bandit, bare muscular arms, a brown bear-fur vest, a thick bushy beard, a dark red headband, a giant iron-banded wooden hammer resting on his right shoulder, loud grinning face.
```
`c1_wolf_sheet.png`
```
Character: a cute but fierce black wolf hound used by mountain bandits, shaggy black fur, yellow eyes, a spiked leather collar with a dark red cloth scarf tied around the neck, four-legged, low running stance. Proportions chibi: big head, short legs.
```

---

## Tinh anh

### Đầu Mục Song Phủ – đột phá
Đầu mục to cao, giáp da ghép mảnh sắt, áo choàng da sói, **hai rìu tay** lưỡi cong, sẹo chéo trên mặt, khăn đỏ sẫm buộc tay.
- Kỹ năng 🟡 **Cuồng Phủ Xoáy**: hồi 8 s – vòng đỏ cảnh báo bán kính 1.8 trong 1 s, rồi xoay hai rìu 1.5 s, sát thương liên tục xung quanh.

### Quân Sư Sơn Trại – hỗ trợ
Lão già gầy, râu dê, mũ vải đen, áo dài xám đen, **quạt giấy rách** và bàn tính đeo hông, mắt gian.
- Đánh thường: ném phi tiêu nhỏ (tầm 3.5).
- Kỹ năng 🟡 **Rượu Mạnh Tiếp Sức**: hồi 10 s – quân địch trong bán kính 4 hồi 15 % máu và +20 % tốc đánh trong 5 s. Ưu tiên giết sớm.

**Prompt – tinh anh** (đính kèm `chutuong1.png`):

`c1_elite_axes_sheet.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. Tough and intimidating but still cute, no blood. No text, no watermark.

Character: an elite bandit lieutenant from ancient East Asia, tall and broad, patched leather armour reinforced with iron plates, a grey wolf-pelt cape, a diagonal scar across the face, a dark red cloth tied around both forearms, a curved hand axe in each hand, fierce confident grin. Slightly larger and more detailed than a normal bandit.

Enemy unit sheet for a mobile strategy game, plain white background, three full-body images of the SAME character side by side, same size: 1) FRONT-RIGHT three-quarter view and 2) BACK-RIGHT three-quarter view, both seen from above at about 40 degrees, standing in a neutral pose with arms slightly away from the body; 3) spinning with both axes outstretched (whirlwind attack), front-right three-quarter view. Bold silhouette readable at small size.
```
`c1_elite_advisor_sheet.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. Sly and comical, not scary. No text, no watermark.

Character: the cunning old strategist of a bandit camp in ancient East Asia, thin, a long goatee, a black cloth scholar hat, a long dark-grey robe with dark red trim, a torn paper folding fan in the right hand, a wooden abacus hanging at the waist, a gourd of wine on the belt, sly narrow eyes.

Enemy unit sheet for a mobile strategy game, plain white background, three full-body images of the SAME character side by side, same size: 1) FRONT-RIGHT three-quarter view and 2) BACK-RIGHT three-quarter view, both seen from above at about 40 degrees, standing in a neutral pose with arms slightly away from the body; 3) raising the wine gourd high to cheer his allies, front-right three-quarter view. Bold silhouette readable at small size.
```

---

## Boss – Hắc Phong Trại Chủ
Gã khổng lồ chột mắt (bịt mắt da), râu quai nón đen rậm, áo choàng **da gấu đen** khoác vai, giáp ngực sắt đen viền đỏ, thắt lưng đầu sói, vác **đại đao vòng (cửu hoàn đao)** to gần bằng người. Cười to, tự phụ.

Kỹ năng 🟡:
1. **Hắc Phong Trảm** (hồi 7 s): vòng cảnh báo hình quạt phía trước (dài 3, góc 90°) 1 s → chém quét, sát thương lớn và đẩy lùi.
2. **Gọi Lâu La** (hồi 15 s): huýt sáo, 4 Lâu La Đao chạy ra từ trại.
3. **Pha 2 – Cuồng Nộ** (dưới 50 % máu): mắt đỏ rực, +30 % tốc đánh, Hắc Phong Trảm thành **vòng tròn 360°**.

**Prompt – bảng xoay boss** (đính kèm `chutuong1.png`) → `c1_boss_turnaround.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, big expressive eyes, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. Intimidating boss but still cute, no blood. No text, no watermark.

Character: the Black Wind bandit king, a giant one-eyed bandit chief from ancient East Asia, a leather eyepatch, a thick bushy black beard, a black bear-fur cloak over the shoulders, a black iron breastplate with dark red trim, a belt buckle shaped like a black wolf head, dark red cloth wraps on the arms, a huge nine-ring broadsword (jiuhuan dao) almost as tall as himself held in the right hand, laughing arrogantly. Very broad and heavy build, clearly a boss.

Character turnaround sheet: four full-body views of the SAME character in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) A-pose front view with arms slightly away from the body and the broadsword held pointing down beside the body, not overlapping it.
```
Sau khi chốt bảng xoay: tạo `c1_boss_rig_front.png`, `c1_boss_rig_back.png` bằng prompt ảnh 2, 3 của `docs/art/concepts/MuuSi_Prompts.md` (thay đoạn mô tả nhân vật bằng đoạn `Character:` ở trên).

---

## Prompt xếp hàng cả chương (kiểm tra đồng bộ) → `c1_lineup.png`
Đính kèm: `chutuong1.png` + các ảnh `c1_*_sheet.png` đã chốt
```
Keep exactly the rendering style of the attached images (stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline). No text, no watermark.
A lineup of all the attached bandit characters standing side by side on a plain white background, FRONT-RIGHT three-quarter view from about 40 degrees above, at their correct relative sizes: the wolf hound smallest, the saber grunt and the archer normal size, the hammer brute one and a half times wider, the axe lieutenant and the old advisor about 1.35 times taller than a grunt, and the one-eyed bandit king about 2.4 times taller than a grunt in the middle.
```
