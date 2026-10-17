# Chương 3 – Yêu quái Âm Sơn

Vùng: **Âm Sơn** – núi đá đen phủ sương tím, rừng cây trụi lá, **đầm lầy ma** có hoa bỉ ngạn đỏ, đèn lồng giấy trôi lơ lửng. Thành địch = **miếu yêu** (ngôi miếu cổ đổ nát, cổng tam quan gãy, ánh lửa ma xanh). Phe địch **hoàn toàn là yêu quái** – nơi tà khí của chương 2 bắt nguồn.
Màu phe: **tím đêm + xanh lửa ma + đỏ bỉ ngạn**. Yêu quái **dễ thương – hơi rùng rợn** (kiểu yêu quái hoạt hình), không ghê rợn, không máu me.

Chỉ số: 🟡 gợi ý, cân bằng sau (spec §13 ⏳). Mạnh hơn chương 2 khoảng 30 %; nhiều cơ chế lạ hơn.
Animation (khung xương, clip, mốc cảnh báo / ra đòn): `Animation.md` · VFX: `VFX_Prompts.md` mục "Chương 3".

| Loại | Cấp | Vai trò | Máu | Sát thương | Tốc đánh | Tầm | Tốc chạy |
|---|---|---|---|---|---|---|---|
| Tiểu Quỷ | Thường | nhanh, bầy đàn | 90 | 11 | 1.5 | 0.5 | 3.4 |
| Cương Thi | Thường | cận chiến chậm, trâu nhẹ | 380 | 24 | 0.8 | 0.6 | 1.5 (nhảy) |
| Quỷ Hoả | Thường | tầm xa, bay | 120 | 20 | 0.7 | 4.0 | 2.4 |
| Thạch Tinh | Thường | trâu | 1100 | 48 | 0.5 | 0.8 | 1.3 |
| Ngưu Đầu | Tinh anh | đột phá | 2200 | 60 | 0.6 | 0.9 | 1.8 |
| Mã Diện | Tinh anh | khống chế | 1500 | 40 | 0.8 | 1.2 | 2.0 |
| **Cửu Vĩ Yêu Hồ** | **Boss** | boss | 12000 | 80 | 0.8 | 3.5 | 2.4 |

---

## Lính thường

### Tiểu Quỷ
Quỷ nhỏ da xanh tím, một sừng ngắn, răng nanh nhỏ, mắt to vàng, khố da hổ, cầm **chày gỗ** nhỏ. Nghịch ngợm, chạy theo bầy.

### Cương Thi
Cương thi kiểu Á Đông: **mũ quan triều đình** và **quan phục xanh đen** cũ, **lá bùa vàng dán trán**, hai tay duỗi thẳng ra trước, **nhảy bật** hai chân. Da xanh nhạt, mặt ngộ ngộ.
- 🟡 Đặc tính: chết thì **có 30 % đứng dậy một lần** với 30 % máu (spec §8: đặc tính bị động). Ý "bị đánh bằng lửa thì không" cần hệ loại sát thương – chưa có, để sau ⏳.

### Quỷ Hoả
Ngọn **lửa ma xanh lam** lơ lửng có mặt cười tinh quái, đuôi lửa, quanh có vài đốm lửa nhỏ. Bắn cầu lửa ma.
- ⚠️ Đặc tính **bay** – không bị sông chặn: xung đột với luật vùng cấm chặn mọi unit, chờ chốt (spec §13).

### Thạch Tinh
Yêu đá to: thân là khối **đá đen phủ rêu tím**, tinh thể tím mọc trên vai, mắt hai đốm sáng tím, tay đá to như búa.

**Prompt – lính thường** (đính kèm `docs/art/concepts/chutuong1.png`; thay khối `UNIT`):
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. A cute, slightly spooky East Asian yokai monster in a cartoon game style, not scary, no blood, no gore. Talisman writing is abstract red strokes, not readable. No text, no watermark.

UNIT

Enemy unit sheet for a mobile strategy game, plain white background, three full-body images of the SAME creature side by side, same size: 1) FRONT-RIGHT three-quarter view and 2) BACK-RIGHT three-quarter view, both seen from above at about 40 degrees, in a neutral pose; 3) a dynamic attack pose, front-right three-quarter view. Simple, bold silhouette readable at very small size.
```
Thay `UNIT` → lưu `c3_<tên>_sheet.png`:

`c3_imp_sheet.png`
```
Creature: a small mischievous imp demon, purple-blue skin, one short horn, tiny fangs, big yellow eyes, a tiger-skin loincloth, a small wooden club in the right hand, cheeky grin, about the size of a normal soldier or slightly smaller.
```
`c3_jiangshi_sheet.png`
```
Creature: a cute East Asian hopping vampire (jiangshi), pale blue-green skin, an old Qing-style official hat and a faded dark-teal official robe with a square embroidered badge, a yellow paper talisman stuck on the forehead covering part of the face but leaving the eyes visible, both arms stretched straight forward, feet together mid-hop, a funny blank expression.
```
`c3_wisp_sheet.png`
```
Creature: a floating ghost fire spirit (will-o'-the-wisp), a flame of glowing pale blue ghost fire with a cheeky smiling face, a long wispy flame tail, a few tiny floating blue embers around it, hovering above the ground with a soft shadow below.
```
`c3_golem_sheet.png`
```
Creature: a big stone golem spirit, a body of black rock blocks covered with purple moss, glowing purple crystals growing from the shoulders, two glowing purple dot eyes, huge rock fists, heavy and wide, about one and a half times wider than a normal soldier.
```

---

## Tinh anh – cặp Ngưu Đầu & Mã Diện
Hai quỷ sai canh cửa Âm Sơn, thường **xuất hiện cùng nhau**.

### Ngưu Đầu – đột phá
Thân người vạm vỡ, **đầu trâu** sừng cong lớn, vòng mũi vàng, giáp sắt đen viền tím, cầm **đinh ba** (xoa) lớn, xích sắt quấn tay.
- Kỹ năng 🟡 **Ngưu Ma Xung**: hồi 10 s – vạch cảnh báo đường thẳng dài 5 trong 1 s → lao tới, sát thương và **hất văng** quân ta trên đường.

### Mã Diện – khống chế
Thân người cao gầy, **đầu ngựa** bờm trắng, áo choàng đen tím, cầm **xích câu hồn** dài có móc.
- Kỹ năng 🟡 **Câu Hồn Tỏa**: hồi 9 s – quăng xích vào **tướng** gần nhất trong tầm 5, **kéo** tướng về phía mình và trói 1.5 s.

**Prompt – tinh anh** (đính kèm `chutuong1.png`):

`c3_elite_ox_sheet.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. A powerful but cute East Asian underworld guardian, not scary, no blood. No text, no watermark.

Character: Ox-Head, an underworld gate guardian from East Asian myth, a muscular humanoid body with the head of a dark ox with large curved horns and a golden nose ring, black iron armour with purple trim, iron chains wrapped around the forearms, a large iron trident held in both hands, snorting fierce expression. Larger and more detailed than a normal monster.

Enemy unit sheet for a mobile strategy game, plain white background, three full-body images of the SAME character side by side, same size: 1) FRONT-RIGHT three-quarter view and 2) BACK-RIGHT three-quarter view, both seen from above at about 40 degrees, standing in a neutral pose with arms slightly away from the body; 3) charging forward head-down with the trident, front-right three-quarter view. Bold silhouette readable at small size.
```
`c3_elite_horse_sheet.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. An eerie but cute East Asian underworld guardian, not scary, no blood. No text, no watermark.

Character: Horse-Face, an underworld gate guardian from East Asian myth, a tall slender humanoid body with the head of a pale grey horse with a long white mane, a black-and-purple flowing cloak, a long soul-catching iron chain with a hook at the end coiled in the hands, glowing pale blue eyes, calm sly expression. Larger and more detailed than a normal monster.

Enemy unit sheet for a mobile strategy game, plain white background, three full-body images of the SAME character side by side, same size: 1) FRONT-RIGHT three-quarter view and 2) BACK-RIGHT three-quarter view, both seen from above at about 40 degrees, standing in a neutral pose with arms slightly away from the body; 3) throwing the hooked chain forward, front-right three-quarter view. Bold silhouette readable at small size.
```

---

## Boss – Cửu Vĩ Yêu Hồ
Nữ yêu hồ chín đuôi, hình người (chibi) **tai cáo trắng**, tóc bạc dài, **áo lụa trắng – đỏ son** kiểu cung nữ cổ, chín **đuôi cáo trắng** đầu đuôi nhuốm lửa xanh lam xoè sau lưng như cánh quạt, cầm **quạt xếp đỏ**; quanh người lơ lửng **hồ hoả** (lửa cáo) xanh lam. Đẹp, kiêu kỳ, nụ cười bí ẩn. Đây là nguồn tà khí của cả hành trình.

Kỹ năng 🟡:
1. **Hồ Hoả Vũ** (hồi 6 s): 6 vòng cảnh báo quanh quân ta 1 s → cầu lửa cáo xanh rơi xuống.
2. **Huyễn Ảnh** (hồi 14 s): tách 2 **phân thân** (máu 10 %, sát thương 30 %) chạy ra hai bên – giết phân thân để ngăn bị bao vây.
3. **Pha 2 – Mị Hoặc** (dưới 50 % máu): đuôi bùng lửa, mỗi 12 s **mê hoặc** 1 tướng ngẫu nhiên trong tầm 5: tướng đó đứng yên 2 s (không đổi phe – tránh rối). +25 % tốc chạy.
- Hình to: chín đuôi làm boss rộng ≈ **2.6 × 2.6** đơn vị – đuôi nên là **các mảnh riêng** gắn xương để xoè/vẫy.

**Prompt – bảng xoay boss** (đính kèm `chutuong1.png`) → `c3_boss_turnaround.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, big expressive eyes, soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections (no strong shiny spots), no outline. A beautiful, mysterious and powerful boss, elegant not scary, no blood. No text, no watermark.

Character: the Nine-Tailed Fox Queen from East Asian myth in human form, white fox ears, long silver-white hair with red hair ornaments, golden slit eyes, an elegant ancient court-lady robe in white silk with vermilion-red layers and gold embroidery, a red folding fan in the right hand, nine large fluffy white fox tails spread behind her like a fan, the tail tips glowing with pale blue fox fire, a few small blue fox-fire flames floating around her, a confident mysterious smile. Clearly a boss, grand and larger than other monsters.

Character turnaround sheet: four full-body views of the SAME character in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) A-pose front view with arms slightly away from the body, the fan held closed beside the body and the nine tails spread so that each tail is clearly separated from the others.
```
Sau khi chốt bảng xoay: tạo `c3_boss_rig_front.png`, `c3_boss_rig_back.png` bằng prompt ảnh 2, 3 của `docs/art/concepts/MuuSi_Prompts.md` (thay đoạn mô tả nhân vật). Lửa cáo bay quanh người là **VFX riêng**.

---

## Prompt xếp hàng cả chương → `c3_lineup.png`
Đính kèm: `chutuong1.png` + các ảnh `c3_*_sheet.png` đã chốt
```
Keep exactly the rendering style of the attached images (stylised 3D-rendered chibi figurine look, soft studio lighting from the top-left, subtle reflections, no outline). Cute, slightly spooky, no gore. No text, no watermark.
A lineup of all the attached yokai characters standing side by side on a plain white background, FRONT-RIGHT three-quarter view from about 40 degrees above, at their correct relative sizes: the imp and the ghost fire smallest, the hopping jiangshi normal soldier size, the stone golem one and a half times wider, Ox-Head and Horse-Face about 1.35 times taller than the jiangshi, and the Nine-Tailed Fox Queen boss about 2.4 times taller with her nine tails spread, in the middle.
```
