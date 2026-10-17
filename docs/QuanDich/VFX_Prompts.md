# Quân địch – prompt hiệu ứng (VFX) (dán nguyên văn)

Phong cách: **A – render 3D stylised** cùng tông với `docs/art/concepts/chutuong1.png`. Kỹ năng và mốc sự kiện: `Animation.md` (cùng thư mục) và các file chương.
Quy tắc chung **giống VFX của tướng** (`docs/art/concepts/VFX_Prompts.md`): một tấm nhiều mảnh, **nền trong suốt** (hoặc **nền đen tuyệt đối**, không dùng nền trắng), hình nằm trên đất vẽ **nhìn thẳng từ trên xuống**, mảnh 128–256 px (vòng lớn 512 px), mình dựng bằng hệ thống hạt của Unity.

- **Màu nhận biết:** vùng **cảnh báo luôn màu đỏ**; hiệu ứng mỗi chương theo màu phe – chương 1 **đen than + đỏ sẫm**, chương 2 **vàng đất + xanh lục tà khí**, chương 3 **tím đêm + xanh lửa ma + hồng tím**. Không máu me, yêu quái dễ thương.
- **Tên file:** `vfx_<enemy | c1 | c2 | c3>_<nhóm>_sheet.png` → cắt thành `vfx_<…>_<nhóm>_<mảnh>.png`. Lưu vào `docs/QuanDich/art/`, mình kiểm tra rồi chuyển vào `Assets/Arena3Q/Art/VFX/Enemies/`.
- **Đính kèm:** `chutuong1.png` (phong cách) + ảnh `sheet` / `turnaround` của loại địch đó (để màu hợp nhân vật).

---

## 0a. Chung – vùng cảnh báo
Đính kèm: `chutuong1.png` · Lưu: `vfx_enemy_telegraph_sheet.png`
```
Clean flat game UI ground decals for a mobile strategy game, slightly glossy, soft glow, no characters, no text, no watermark.
A set of separate warning area markers, all seen from DIRECTLY ABOVE, evenly spaced on a transparent background, nothing overlapping:
1) a circle with a translucent red fill and a bright red glowing rim, 2) the same circle as a solid soft red disc with no rim (used to fill up as a timer), 3) a 90-degree fan / cone shape with a translucent red fill and a bright red rim, 4) a long straight rectangle with a translucent red fill, a bright red rim and three white-red chevron arrows pointing along it, 5) a small red target reticle (circle with four ticks) to float above a single unit.
Keep shapes simple and symmetrical so they can be scaled and rotated.
```

## 0b. Chung – chết, xuất hiện, đổi pha, tăng sức
Đính kèm: `chutuong1.png` · Lưu: `vfx_enemy_common_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached reference image (soft, clean, mobile game quality), cute, no characters, no blood, no text, no watermark.
A set of separate VFX texture elements for enemy units, evenly spaced on a transparent background, nothing overlapping:
1) a cute round grey smoke "poof" cloud with two tiny white stars (enemy defeated), 2) a bigger version of the smoke poof (elite defeated), 3) a burst of brown dust and small pebbles from the ground (enemy appears or is summoned), 4) a dark red shockwave ring seen from directly above (boss changes phase), 5) a small red upward arrow glow (enemy buff marker above a unit), 6) a wide ground crack with dust seen from directly above (boss entrance).
```

---

## Chương 1 – Hắc Phong Trại (đen than + đỏ sẫm)

### c1-1. Lính thường
Đính kèm: `chutuong1.png` + các `c1_*_sheet.png` · Lưu: `vfx_c1_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), no characters, no blood, no text, no watermark.
A set of separate VFX texture elements for bandit attacks, evenly spaced on a transparent background, nothing overlapping:
1) a crescent slash arc in dull white with a dark red edge, 2) a simple wooden arrow with dark red cloth fletching pointing right, 3) a heavy ground impact: brown dust burst with a small ground crack seen from directly above, 4) a wolf bite effect: two curved white fang-shaped arcs closing together, 5) a small white-red hit spark.
```

### c1-2. Đầu Mục Song Phủ – Cuồng Phủ Xoáy (xoay 1.5 giây, bán kính 1.8)
Đính kèm: `chutuong1.png` + `c1_elite_axes_sheet.png` · Lưu: `vfx_c1_axes_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no blood, no text, no watermark.
A set of separate VFX texture elements for a spinning twin-axe whirlwind, evenly spaced on a transparent background, nothing overlapping:
1) a full circular slash ring seen from directly above in dark smoky black with a dark red glowing edge, motion blurred, 2) a single curved axe slash arc in dark red, 3) a swirling dust spiral seen from directly above, 4) small orange metal sparks.
```

### c1-3. Quân Sư Sơn Trại – phi tiêu + Rượu Mạnh Tiếp Sức (quân địch r 4 hồi máu, tăng tốc đánh 5 giây)
Đính kèm: `chutuong1.png` + `c1_elite_advisor_sheet.png` · Lưu: `vfx_c1_advisor_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), warm glow, no characters, no text, no watermark.
A set of separate VFX texture elements for a sly bandit strategist, evenly spaced on a transparent background, nothing overlapping:
1) a small iron throwing dart with a dark red tassel pointing right, 2) a splash of amber rice-wine droplets flying upward, 3) a warm orange glowing ring seen from directly above (area buff), 4) a small warm orange plus-shaped sparkle (healing), 5) a small cute amber bubble.
```

### c1-4. Boss Hắc Phong Trại Chủ
Đính kèm: `chutuong1.png` + `c1_boss_turnaround.png` · Lưu: `vfx_c1_boss_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), dramatic, glowing, no characters, no blood, no text, no watermark.
A set of separate VFX texture elements for a giant bandit king with a nine-ring broadsword, evenly spaced on a transparent background, nothing overlapping:
1) a large crescent wave of swirling black wind with a dark red edge flying to the right (black wind slash), 2) a full circular ring of black wind seen from directly above (360-degree version), 3) a big heavy sword slash arc in steel-white with a dark red edge, 4) three cute wavy whistle sound marks (simple curved lines and small notes, no letters), 5) a bright red glowing eye glint with a short trailing streak, 6) a flickering dark red rage aura flame seen from the front (to wrap around the boss).
```

---

## Chương 2 – Hoàng Cân Tà Đạo (vàng đất + xanh lục tà khí)
Chữ trên bùa chỉ là **nét đỏ trang trí**, không đọc được.

### c2-1. Lính thường
Đính kèm: `chutuong1.png` + các `c2_*_sheet.png` · Lưu: `vfx_c2_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), no characters, no blood, no text, no watermark. Any talisman writing is abstract red strokes, not readable.
A set of separate VFX texture elements for yellow-turban cult attacks, evenly spaced on a transparent background, nothing overlapping:
1) a straight forward thrust streak in earthy yellow-brown pointing right, 2) a small yellow paper talisman burning with pale green fire flying to the right with a green fire tail, 3) a small pale green fire splash burst, 4) a heavy spiked-club impact: brown dust burst with small wood splinters, 5) a burst of flying straw pieces with a torn yellow talisman (a straw scarecrow falling apart).
```

### c2-2. Hộ Pháp Đồng Giáp – Kim Cương Hộ Thể (giơ khiên 4 giây, giảm 60 % sát thương)
Đính kèm: `chutuong1.png` + `c2_elite_guardian_sheet.png` · Lưu: `vfx_c2_guardian_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a bronze guardian shield skill, evenly spaced on a transparent background, nothing overlapping:
1) a translucent golden-bronze dome bubble with a faint bagua pattern on its surface, seen from the front at a slight angle, 2) a golden bagua circle seen from directly above (eight trigram marks around a yin-yang centre), 3) a small bright golden ping spark (a hit bouncing off the dome).
```
⚠️ Nếu luật **khiêu khích** được chốt (spec §13 mục 1), thêm mảnh `4) a small red angry-face mark to float above a taunted unit` vào prompt trên.

### c2-3. Đại Phương Sư – tia sét + Thiên Lôi Phù (3 vùng r 1.2, sét, choáng 1 giây)
Đính kèm: `chutuong1.png` + `c2_elite_sorcerer_sheet.png` · Lưu: `vfx_c2_sorcerer_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark. Talisman writing is abstract red strokes, not readable.
A set of separate VFX texture elements for a cult sorcerer's lightning magic, evenly spaced on a transparent background, nothing overlapping:
1) a small horizontal zig-zag lightning bolt in pale green and white pointing right, 2) a big vertical lightning strike from the sky in pale green and white, 3) a glowing yellow paper talisman floating flat in the air, 4) a scorched ground decal seen from directly above with faint green sparks, 5) a small pale green electric spark burst.
```

### c2-4. Boss Thiên Công Tế Tửu
Đính kèm: `chutuong1.png` + `c2_boss_turnaround.png` · Lưu: `vfx_c2_boss_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), dramatic, glowing, no characters, no text, no watermark. Talisman writing is abstract red strokes, not readable.
A set of separate VFX texture elements for a sinister cult high priest boss, evenly spaced on a transparent background, nothing overlapping:
1) a swirling orb of pale green sinister mist with a short tail flying to the right, 2) a huge vertical golden-yellow lightning strike from the sky, 3) a burst of soil from the ground with flying yellow talismans (summoning), 4) a swirling yellow-and-green dust storm ring seen from directly above, 5) a translucent pale green shield bubble, 6) shards of a breaking green bubble, 7) a slowly rotating ring of glowing yellow talismans seen from directly above (floating under the boss's feet).
```

---

## Chương 3 – Yêu quái Âm Sơn (tím đêm + xanh lửa ma + hồng tím)

### c3-1. Lính thường
Đính kèm: `chutuong1.png` + các `c3_*_sheet.png` · Lưu: `vfx_c3_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), cute, slightly spooky, no characters, no blood, no gore, no text, no watermark.
A set of separate VFX texture elements for cute yokai monster attacks, evenly spaced on a transparent background, nothing overlapping:
1) a cartoon "bonk" impact star in yellow-white, 2) a small dust ring seen from directly above (a hopping landing), 3) a pale blue ghost-fire ball with a wispy tail flying to the right, 4) a small pale blue fire splash, 5) a burst of black rock chunks and glowing purple crystal shards, 6) a cute round purple mist puff dissolving upward with tiny sparkles (a yokai vanishing).
```

### c3-2. Cương Thi – sống lại
Đính kèm: `chutuong1.png` + `c3_jiangshi_sheet.png` · Lưu: `vfx_c3_revive_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, slightly spooky but cute, no characters, no text, no watermark. Talisman writing is abstract red strokes, not readable.
A set of separate VFX texture elements for a hopping vampire getting back up, evenly spaced on a transparent background, nothing overlapping:
1) a bright flash of a yellow paper talisman glowing, 2) a rising column of pale blue-green mist, 3) a small ring of pale green sparkles seen from directly above.
```

### c3-3. Ngưu Đầu – Ngưu Ma Xung (lao thẳng dài 5, hất văng)
Đính kèm: `chutuong1.png` + `c3_elite_ox_sheet.png` · Lưu: `vfx_c3_ox_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no blood, no text, no watermark.
A set of separate VFX texture elements for a charging ox-headed demon, evenly spaced on a transparent background, nothing overlapping:
1) a long horizontal dust trail with dark purple tint, 2) a big horn impact burst in purple and white, 3) a purple shockwave ring seen from directly above, 4) a curved upward motion streak (a unit being knocked into the air), 5) two small puffs of steam (snorting).
```

### c3-4. Mã Diện – Câu Hồn Tỏa (quăng xích kéo tướng, trói 1.5 giây)
Đính kèm: `chutuong1.png` + `c3_elite_horse_sheet.png` · Lưu: `vfx_c3_horse_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, slightly eerie, no characters, no blood, no text, no watermark.
A set of separate VFX texture elements for a soul-catching chain, evenly spaced on a transparent background, nothing overlapping:
1) a straight horizontal segment of dark iron chain with a faint pale blue glow, seamless so it can be repeated, 2) a curved iron hook at the end of a chain with a pale blue glow, 3) a ring of iron chains wrapped around an empty space, seen from the front (binding effect around a body), 4) a few small pale blue soul wisps.
```

### c3-5. Boss Cửu Vĩ Yêu Hồ
Đính kèm: `chutuong1.png` + `c3_boss_turnaround.png` · Lưu: `vfx_c3_boss_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), elegant, magical, glowing, no characters, no blood, no text, no watermark.
A set of separate VFX texture elements for a nine-tailed fox queen boss, evenly spaced on a transparent background, nothing overlapping:
1) a pale blue fox-fire ball falling steeply downward with a long flame tail, 2) a pale blue fire puddle seen from directly above, 3) a small floating pale blue fox-fire flame (orbits around the boss), 4) a large blue flame shaped to sit on the tip of a fox tail, 5) a puff of pink-purple illusion smoke with a few cherry-blossom petals, 6) a cute swirling pink-purple spiral with tiny sparkles (charm effect floating above a head).
```
