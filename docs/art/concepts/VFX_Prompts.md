# Hiệu ứng kỹ năng (VFX) – prompt tạo ảnh (dán nguyên văn)

> **Hướng mới (người dùng 2026-10-22):** VFX lấy từ **gói hiệu ứng có sẵn** (Asset Store, gói miễn phí) rồi chỉnh cho hợp dự án (URP, trục trận, đồng hồ trận, màu / nhịp theo tướng), không tự dựng từ đầu. Các prompt / mô tả dưới đây giữ làm **mô tả "look" cần đạt** khi chọn và chỉnh gói. Quy trình: `docs/QuyTac/QuyTacLamViec.md` §3.

Phong cách: **A – render 3D stylised** cùng tông với ảnh mẫu `chutuong1.png`; kỹ năng của từng chủ tướng: `docs/Tuong/LordHeroes.md` §2; tướng thường: `docs/Tuong/NormalHeroes.md` §2.
Hiệu ứng **không** vẽ từng khung: mình dựng bằng **hệ thống hạt (particle) của Unity** từ các **mảnh hiệu ứng** dưới đây (xoay, phóng to, mờ dần, lặp lại).

**Quy tắc chung**
- Mỗi prompt cho ra **một tấm nhiều mảnh** xếp cách nhau; bạn cắt từng mảnh ra PNG riêng trong Krita.
- **Nền trong suốt.** Nếu ChatGPT không cho nền trong suốt, dùng **nền đen tuyệt đối** (`pure black background`) – mình dùng chế độ cộng sáng (additive) nên nền đen tự biến mất. **Không** dùng nền trắng.
- Hình **nằm trên mặt đất** (vòng trận, vùng lửa, vết nứt) vẽ **nhìn thẳng từ trên xuống, hình tròn** – game tự nén theo góc 3/4.
- Kích thước mỗi mảnh sau khi cắt: **128–256 px** (mảnh nhỏ: tia lửa, lá) · **512 px** (vòng trận, vụ nổ lớn).
- Tên file: `vfx_<chủ tướng / tướng>_<kỹ năng>_<mảnh>.png`, ví dụ `vfx_strategist_firefield_decal.png`. Lưu vào thư mục này trước, mình kiểm tra rồi chuyển vào `Assets/Arena3Q/Art/VFX/`.
- Đính kèm: `chutuong1.png` (phong cách) + ảnh bảng xoay của chủ tướng đó (để màu hiệu ứng hợp nhân vật).

| # | Chủ tướng | Kỹ năng | Tên kỹ năng trong file |
|---|---|---|---|
| 0 | Chung | Trúng đòn, xuất trận, thanh nộ đầy, choáng | `common` |
| 1–4 | Mỗi chủ tướng | Đánh thường | `basic` |
| 5–7 | Cung thủ | Liên Châu Tiễn · Tiễn Vũ · Phá Thành Hỏa Tiễn | `multishot` · `arrowrain` · `firearrow` |
| 8–10 | Kiếm sĩ | Phi Ảnh Đột Kích · Toàn Phong Trảm · Vạn Kiếm Quy Tông | `dash` · `whirlwind` · `swordrain` |
| 11–13 | Mưu sĩ | Hỏa Kế · Bát Quái Trận · Đông Phong | `firefield` · `bagua` · `eastwind` |
| 14–16 | Thống soái | Hiệu Triệu · Trống Trận · Thiết Kỵ Xung Phong | `rally` · `wardrum` · `cavalry` |
| 17–19 | Cung thủ, Kiếm sĩ, Mưu sĩ | Nội tại: Ưng Nhãn · Kiếm Tâm · Thao Lược | `passive` |
| 20–24 | Tướng thường Xạ Thủ (`hunter`) | Đánh thường · Độc Tiễn · Ưng Kích · Xuyên Dương Tiễn · nội tại Liệp Sát | `basic` · `poison` · `hawk` · `pierce` · `passive` |
| 25–30 | Tướng thường Kỵ Binh (`cavalry`) | Đánh thường · Mã Đạp · Phong Mã · Thiên Quân Đột Kích · nội tại Xung Phong · bụi chân ngựa | `basic` · `stomp` · `windrider` · `charge` · `passive` · `dust` |
| 31–36 | Tướng thường Giáp Sĩ (`guardian`) | Đánh thường · Thuẫn Kích · Hộ Vệ Trận · Kim Thành Thiết Bích · nội tại Kiên Thủ · đỡ đòn | `basic` · `bash` · `aegis` · `fortress` · `passive` · `block` |
| 37–41 | Tướng thường Đao Khách (`blade`) | Đánh thường · Bạt Đao Trảm · Đao Khí · Cuồng Đao Lục Trảm · nội tại Liên Trảm | `basic` · `draw` · `wave` · `frenzy` · `passive` |

VFX quân địch (cảnh báo vùng đỏ, kỹ năng tinh anh / boss): `docs/QuanDich/VFX_Prompts.md`.

---

## 0. Chung
Đính kèm: `chutuong1.png` · Lưu: `vfx_common_sheet.png` → cắt thành `vfx_common_<mảnh>.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached reference image (soft, clean, glossy, mobile game quality), glowing, high contrast, no characters, no text, no watermark.
A set of separate VFX texture elements for a mobile strategy game, evenly spaced on a transparent background, nothing overlapping:
1) small white-gold hit spark star burst, 2) circular impact shockwave ring seen from directly above (thin, white), 3) soft round glow orb (white, fading edges), 4) a vertical pillar of soft golden light (summon effect), 5) small sparkle particle (four-pointed star), 6) a circular golden glow ring seen from directly above (ultimate ready), 7) a small ring of three cute spinning yellow stars (stun indicator above a head).
```

---

## 1. Cung thủ – đánh thường
Đính kèm: `chutuong1.png` + `archer_turnaround.png` · Lưu: `vfx_archer_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for an archer's basic attack, evenly spaced on a transparent background, nothing overlapping:
1) a single wooden arrow with green fletching drawn horizontally pointing right, 2) a thin jade-green motion trail streak, 3) a small green-white hit spark.
```

## 2. Kiếm sĩ – đánh thường
Đính kèm: `chutuong1.png` + `swordsman_turnaround.png` · Lưu: `vfx_swordsman_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a swordsman's basic attack, evenly spaced on a transparent background, nothing overlapping:
1) a curved crescent sword-slash arc in silver-blue light, 2) a second thinner slash arc, 3) a small blue-white hit spark.
```

## 3. Mưu sĩ – đánh thường (luồng khí từ quạt, tầm 3.0)
Đính kèm: `chutuong1.png` + `strategist_turnaround.png` · Lưu: `vfx_strategist_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a strategist's basic attack (a gust of magic wind released by a feather fan), evenly spaced on a transparent background, nothing overlapping:
1) a small swirling lavender wind bolt flying to the right with a soft tail, 2) a single white feather, 3) a small lavender-white burst of wind on impact.
```

## 4. Thống soái – đánh thường
Đính kèm: `chutuong1.png` + `marshal_turnaround.png` · Lưu: `vfx_marshal_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a spear thrust attack, evenly spaced on a transparent background, nothing overlapping:
1) a straight forward thrust streak of orange-gold light pointing right, 2) a cone-shaped impact burst, 3) a small orange hit spark.
```

---

## 5. Cung thủ – Liên Châu Tiễn (kỹ năng 2: bắn liên tiếp 3 mũi tên)
Đính kèm: `chutuong1.png` + `archer_turnaround.png` · Lưu: `vfx_archer_multishot_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a rapid triple-shot archery skill, evenly spaced on a transparent background, nothing overlapping:
1) a glowing jade-green energy arrow pointing right, 2) a short bright green motion trail, 3) a small green hit spark, 4) a small green muzzle flash burst at the bow.
```

## 6. Cung thủ – Tiễn Vũ (kỹ năng 3: mưa tên xuống vùng tròn bán kính 2.5 trong 3 giây)
Đính kèm: `chutuong1.png` + `archer_turnaround.png` · Lưu: `vfx_archer_arrowrain_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for an arrow rain skill, evenly spaced on a transparent background, nothing overlapping:
1) a single arrow falling steeply downward seen from the front-above, with a short green light trail, 2) an arrow stuck in the ground, 3) a circular dusty ground ring seen from directly above (soft brown dust, green glow edge), 4) a small dust puff.
```

## 7. Cung thủ – Phá Thành Hỏa Tiễn (tuyệt kỹ: mũi tên lửa khổng lồ bay thẳng, xuyên quân)
Đính kèm: `chutuong1.png` + `archer_turnaround.png` · Lưu: `vfx_archer_firearrow_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, dramatic, no characters, no text, no watermark.
A set of separate VFX texture elements for an ultimate fire-arrow skill, evenly spaced on a transparent background, nothing overlapping:
1) a huge flaming arrow pointing right wrapped in orange-red fire, 2) a long horizontal fire trail, 3) a big round fire explosion burst, 4) small glowing embers, 5) a dark smoke puff.
```

---

## 8. Kiếm sĩ – Phi Ảnh Đột Kích (kỹ năng 2: lướt tới mục tiêu, choáng 1 giây)
Đính kèm: `chutuong1.png` + `swordsman_turnaround.png` · Lưu: `vfx_swordsman_dash_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a lightning-fast sword dash skill, evenly spaced on a transparent background, nothing overlapping:
1) a translucent blue afterimage smear shaped like a running figure silhouette (no details, just a soft blue shape), 2) horizontal speed streak lines, 3) a bright blue-white impact star, 4) a small ring of three spinning stars (stun indicator).
```

## 9. Kiếm sĩ – Toàn Phong Trảm (kỹ năng 3: xoay song kiếm 1.5 giây, bán kính 1.5)
Đính kèm: `chutuong1.png` + `swordsman_turnaround.png` · Lưu: `vfx_swordsman_whirlwind_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a spinning sword whirlwind skill, evenly spaced on a transparent background, nothing overlapping:
1) a full circular sword-slash ring seen from directly above (silver-blue, motion blurred), 2) a swirling wind spiral seen from directly above, 3) a small blue slash spark.
```

## 10. Kiếm sĩ – Vạn Kiếm Quy Tông (tuyệt kỹ: kiếm khí từ trời rơi xuống vùng quạt phía trước, dài 5)
Đính kèm: `chutuong1.png` + `swordsman_turnaround.png` · Lưu: `vfx_swordsman_swordrain_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, dramatic, no characters, no text, no watermark.
A set of separate VFX texture elements for an ultimate skill where countless spirit swords fall from the sky, evenly spaced on a transparent background, nothing overlapping:
1) a single glowing translucent blue spirit sword pointing straight down, 2) a thin vertical pillar of blue-white light, 3) a ground crack decal seen from directly above with glowing blue edges, 4) a small blue impact flash.
```

---

## 11. Mưu sĩ – Hỏa Kế (kỹ năng 2: trận lửa vùng tròn bán kính 2 trong 5 giây)
Đính kèm: `chutuong1.png` + `strategist_turnaround.png` · Lưu: `vfx_strategist_firefield_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a fire trap area skill, evenly spaced on a transparent background, nothing overlapping:
1) a circular burning ground decal seen from directly above (orange embers and scorched pattern, glowing edge), 2) a single stylised flame, 3) a small grey smoke puff, 4) a burning paper talisman with a simple fire symbol (no readable text).
```

## 12. Mưu sĩ – Bát Quái Trận (kỹ năng 3: trận đồ bán kính 3 trong 6 giây, làm chậm 40 %)
Đính kèm: `chutuong1.png` + `strategist_turnaround.png` · Lưu: `vfx_strategist_bagua_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a magic formation skill, evenly spaced on a transparent background, nothing overlapping:
1) a glowing bagua trigram circle seen from directly above (eight trigrams around a yin-yang centre, purple and lavender light), 2) an outer thin rotating ring of small runes (simple abstract symbols, no readable text), 3) a single floating trigram symbol glowing purple, 4) a soft purple mist puff.
```

## 13. Mưu sĩ – Đông Phong (tuyệt kỹ: gió lớn quét cả chiến trường, đẩy lùi địch)
Đính kèm: `chutuong1.png` + `strategist_turnaround.png` · Lưu: `vfx_strategist_eastwind_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, dramatic, no characters, no text, no watermark.
A set of separate VFX texture elements for an ultimate wind skill that sweeps the whole battlefield, evenly spaced on a transparent background, nothing overlapping:
1) a long horizontal wind gust streak (white-lavender, flowing right), 2) a curling wind swirl, 3) a few green leaves blown by wind, 4) a single white crane feather, 5) a large soft lavender wind wave.
```

---

## 14. Thống soái – Hiệu Triệu (kỹ năng 2: bổ sung lính đã chết cho đội gần nhất)
Đính kèm: `chutuong1.png` + `marshal_turnaround.png` · Lưu: `vfx_marshal_rally_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a rally skill that summons reinforcements, evenly spaced on a transparent background, nothing overlapping:
1) a golden glowing war banner silhouette, 2) a vertical beam of warm golden light, 3) a circular golden summoning ring seen from directly above, 4) small rising golden sparkles.
```

## 15. Thống soái – Trống Trận (kỹ năng 3: quân ta bán kính 6 +20 % tốc đánh và tốc chạy 6 giây)
Đính kèm: `chutuong1.png` + `marshal_turnaround.png` · Lưu: `vfx_marshal_wardrum_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a war drum buff skill, evenly spaced on a transparent background, nothing overlapping:
1) a big red-and-gold war drum, 2) an orange shockwave ring seen from directly above, 3) a burst of orange sound-wave arcs, 4) a small upward orange arrow glow (buff indicator above a unit).
```

## 16. Thống soái – Thiết Kỵ Xung Phong (tuyệt kỹ: hàng kỵ binh lao qua đường thẳng dài 12)
Đính kèm: `chutuong1.png` + `marshal_turnaround.png` · Lưu: `vfx_marshal_cavalry_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, dramatic, no text, no watermark.
A set of separate VFX texture elements for an ultimate cavalry charge skill, evenly spaced on a transparent background, nothing overlapping:
1) a ghostly translucent golden armoured cavalry rider on a horse charging to the right, three-quarter view from about 40 degrees above, 2) a long horizontal dust cloud trail, 3) a hoof-print ground decal seen from directly above, 4) a small golden impact burst.
```

---

## 17. Cung thủ – nội tại Ưng Nhãn (mỗi đòn thứ 4 bắn xuyên)
Đính kèm: `chutuong1.png` + `archer_turnaround.png` · Lưu: `vfx_archer_passive_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a piercing arrow passive, evenly spaced on a transparent background, nothing overlapping:
1) a bright golden glowing arrow pointing right with a sharp tip, 2) a long thin golden piercing trail streak, 3) a small golden ring burst where the arrow passes through a target.
```

## 18. Kiếm sĩ – nội tại Kiếm Tâm (hút máu; dưới 30 % máu tăng tốc đánh)
Đính kèm: `chutuong1.png` + `swordsman_turnaround.png` · Lưu: `vfx_swordsman_passive_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a sword-heart passive, evenly spaced on a transparent background, nothing overlapping:
1) a small soft green healing orb with a short tail, 2) a tiny green plus-shaped sparkle, 3) a flickering red battle aura flame seen from the front (to wrap around a character), 4) a circular red aura ring seen from directly above.
```

## 19. Mưu sĩ – nội tại Thao Lược (đội lính quanh Mưu sĩ +10 % sát thương)
Đính kèm: `chutuong1.png` + `strategist_turnaround.png` · Lưu: `vfx_strategist_passive_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), soft glow, no characters, no text, no watermark.
A set of separate VFX texture elements for a strategist aura passive, evenly spaced on a transparent background, nothing overlapping:
1) a very faint wide lavender aura ring seen from directly above, thin and subtle (it will cover a large area under many units), 2) a tiny lavender upward-arrow glow (buff marker above a soldier), 3) a small floating bamboo-slip symbol glowing lavender.
```

---

# Tướng thường (§20–41)
Kỹ năng: `docs/Tuong/NormalHeroes.md` §2 · mốc sự kiện: `docs/Tuong/NormalHeroes.md` §4. Tên file dùng mã `hunter` (Xạ Thủ), `cavalry` (Kỵ Binh), `guardian` (Giáp Sĩ), `blade` (Đao Khách) – không trùng chủ tướng. Đính kèm: `chutuong1.png` + `<mã>_turnaround.png` của tướng đó.

## 20. Xạ Thủ – đánh thường
Lưu: `vfx_hunter_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a hunter's basic bow attack, evenly spaced on a transparent background, nothing overlapping:
1) a single short wooden arrow with brown-and-white fletching drawn horizontally pointing right, 2) a thin warm wheat-gold motion trail streak, 3) a small gold-white hit spark.
```

## 21. Xạ Thủ – Độc Tiễn (kỹ năng 2: mũi tên độc, độc 4 giây, chậm 20 %)
Lưu: `vfx_hunter_poison_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a poison arrow skill, evenly spaced on a transparent background, nothing overlapping:
1) an arrow with a glowing toxic green arrowhead pointing right, 2) a wispy green poison smoke trail, 3) a small cluster of cute green poison bubbles (to float above a poisoned enemy), 4) a small green splash burst on impact.
```

## 22. Xạ Thủ – Ưng Kích (kỹ năng 3: chim ưng lao vào mục tiêu)
Lưu: `vfx_hunter_hawk_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, glossy, mobile game quality), no text, no watermark.
A set of separate VFX texture elements for a hawk strike skill, evenly spaced on a transparent background, nothing overlapping:
1) a chibi brown hunting hawk diving to the right with wings swept back, seen from the side, slightly glowing gold edges, 2) the same hawk with wings fully spread (gliding), 3) three short claw-scratch marks in gold-white light, 4) a few small brown feathers.
```

## 23. Xạ Thủ – Xuyên Dương Tiễn (tuyệt kỹ: mũi tên cực mạnh bay thẳng dài 9)
Lưu: `vfx_hunter_pierce_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, dramatic, no characters, no text, no watermark.
A set of separate VFX texture elements for an ultimate sniper arrow skill, evenly spaced on a transparent background, nothing overlapping:
1) a large glowing golden arrow pointing right with a sharp diamond-shaped tip, 2) a very long thin straight golden light streak, 3) a small swirling golden energy gathering orb (charging at the arrow tip), 4) a circular golden shockwave ring burst seen from the side (impact).
```

## 24. Xạ Thủ – nội tại Liệp Sát (mục tiêu dưới 40 % máu)
Lưu: `vfx_hunter_passive_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a hunter's mark passive, evenly spaced on a transparent background, nothing overlapping:
1) a small red-orange crosshair target mark (four short ticks around a circle, to float above a weak enemy), 2) a small red-orange critical hit spark.
```

## 25. Kỵ Binh – đánh thường (quét yển nguyệt đao)
Lưu: `vfx_cavalry_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a sweeping polearm attack from horseback, evenly spaced on a transparent background, nothing overlapping:
1) a wide curved horizontal sweep arc in teal-silver light, 2) a thinner second sweep arc, 3) a small teal-white hit spark.
```

## 26. Kỵ Binh – Mã Đạp (kỹ năng 2: ngựa dậm, vòng bán kính 1.5, choáng 0.8 giây)
Lưu: `vfx_cavalry_stomp_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a horse stomp skill, evenly spaced on a transparent background, nothing overlapping:
1) a circular ground shockwave ring seen from directly above (dusty brown with a teal glow edge), 2) a ground crack decal seen from directly above, 3) a burst of small rock and dirt chunks, 4) a big soft dust cloud puff.
```

## 27. Kỵ Binh – Phong Mã (kỹ năng 3: +40 % tốc chạy, +20 % sát thương 5 giây)
Lưu: `vfx_cavalry_windrider_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a swift wind buff on a rider, evenly spaced on a transparent background, nothing overlapping:
1) a swirling teal wind ring seen from the front at a slight angle (to wrap around a horse), 2) short horizontal teal wind streaks (speed lines), 3) a small upward teal arrow glow (buff indicator).
```

## 28. Kỵ Binh – Thiên Quân Đột Kích (tuyệt kỹ: lao thẳng dài 8, hất văng)
Lưu: `vfx_cavalry_charge_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, dramatic, no characters, no text, no watermark.
A set of separate VFX texture elements for an ultimate cavalry charge, evenly spaced on a transparent background, nothing overlapping:
1) a translucent teal afterimage smear shaped like a galloping horse and rider silhouette (no details, soft shape), 2) a long horizontal dust trail, 3) a bright teal-white impact star burst, 4) a curved upward motion streak (an enemy being knocked into the air).
```

## 29. Kỵ Binh – nội tại Xung Phong (chạy đủ 2 giây, đòn đầu ×2)
Lưu: `vfx_cavalry_passive_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a charge-ready passive, evenly spaced on a transparent background, nothing overlapping:
1) a short glowing teal light streak (trailing behind a running hoof), 2) a small teal spark burst, 3) a bright teal-white heavy impact burst (the empowered first hit).
```

## 30. Kỵ Binh – bụi chân ngựa (khi chạy, xuất trận)
Lưu: `vfx_cavalry_dust_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), soft, no characters, no text, no watermark.
A set of separate VFX texture elements for galloping hooves, evenly spaced on a transparent background, nothing overlapping:
1) a small soft light-brown dust puff, 2) a medium dust puff, 3) a small cluster of flying dirt specks.
```

## 31. Giáp Sĩ – đánh thường
Lưu: `vfx_guardian_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a short saber slash, evenly spaced on a transparent background, nothing overlapping:
1) a short compact crescent slash arc in steel-white light with a copper-orange edge, 2) a small copper-orange hit spark.
```

## 32. Giáp Sĩ – Thuẫn Kích (kỹ năng 2: đập khiên, choáng 1 giây)
Lưu: `vfx_guardian_bash_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a shield bash skill, evenly spaced on a transparent background, nothing overlapping:
1) a flat rectangular burst of copper-orange light shaped like the face of a tower shield, 2) a cone-shaped forward impact shockwave, 3) a big white-orange impact star.
```

## 33. Giáp Sĩ – Hộ Vệ Trận (kỹ năng 3: khiên cho quân ta bán kính 3, 5 giây)
Lưu: `vfx_guardian_aegis_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a protective aura skill, evenly spaced on a transparent background, nothing overlapping:
1) a circular golden-copper protective ring expanding, seen from directly above, 2) a small translucent golden hexagon-pattern bubble shield (to cover one soldier), 3) a small golden shield-shaped icon glow (floats above a protected unit), 4) a shattering bubble fragment burst (shield broken).
```

## 34. Giáp Sĩ – Kim Thành Thiết Bích (tuyệt kỹ: vùng bảo vệ bán kính 3.5, 6 giây)
Lưu: `vfx_guardian_fortress_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, dramatic, no characters, no text, no watermark.
A set of separate VFX texture elements for an ultimate fortress shield zone, evenly spaced on a transparent background, nothing overlapping:
1) a large circular golden ground ring seen from directly above with a pattern of interlocking shields around its edge, 2) a single translucent golden tower shield standing upright (repeated around the ring to form a wall), 3) a ground crack decal seen from directly above with a golden glow, 4) small rising golden sparkles.
```

## 35. Giáp Sĩ – nội tại Kiên Thủ (dưới 50 % máu)
Lưu: `vfx_guardian_passive_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a steadfast defence passive, evenly spaced on a transparent background, nothing overlapping:
1) a soft copper-orange glow outline shaped like a tall rectangular shield (overlay on the shield), 2) a faint copper-orange aura ring seen from directly above.
```

## 36. Giáp Sĩ – đỡ đòn bằng khiên (clip `hit`)
Lưu: `vfx_guardian_block_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for blocking with a metal shield, evenly spaced on a transparent background, nothing overlapping:
1) a bright burst of metal sparks flying outward, 2) a small white flash star, 3) a short semicircular ripple of light.
```

## 37. Đao Khách – đánh thường
Lưu: `vfx_blade_basic_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a heavy great-saber slash, evenly spaced on a transparent background, nothing overlapping:
1) a wide thick crescent slash arc in vermilion-orange light with a white core, 2) a thinner second slash arc, 3) a small orange-white hit spark.
```

## 38. Đao Khách – Bạt Đao Trảm (kỹ năng 2: chém dồn lực 250 %)
Lưu: `vfx_blade_draw_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a charged heavy slash, evenly spaced on a transparent background, nothing overlapping:
1) a glowing vermilion energy shimmer running along a blade edge (long thin horizontal strip), 2) a huge rising diagonal slash arc in bright vermilion and white, 3) a burst of orange sparks, 4) a big white-orange critical impact star.
```

## 39. Đao Khách – Đao Khí (kỹ năng 3: lưỡi đao khí bay xuyên địch, dài 5)
Lưu: `vfx_blade_wave_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a flying blade-wave projectile, evenly spaced on a transparent background, nothing overlapping:
1) a vertical crescent-shaped wave of vermilion-orange energy flying to the right, 2) a short fading orange motion trail, 3) a small orange slash spark (hit on each enemy passed through).
```

## 40. Đao Khách – Cuồng Đao Lục Trảm (tuyệt kỹ: 6 nhát quanh bán kính 1.8)
Lưu: `vfx_blade_frenzy_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, dramatic, no characters, no text, no watermark.
A set of separate VFX texture elements for an ultimate frenzy of great-saber slashes, evenly spaced on a transparent background, nothing overlapping:
1) a large curved slash arc in vermilion-orange light (several will be rotated around the character), 2) a full circular slash ring seen from directly above, 3) a ground crack decal seen from directly above with glowing red-orange edges, 4) a short vertical pillar of red-orange flame (final strike), 5) small glowing embers.
```

## 41. Đao Khách – nội tại Liên Trảm (cộng dồn 1–5 trên cùng mục tiêu)
Lưu: `vfx_blade_passive_sheet.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached images (soft, clean, mobile game quality), glowing, no characters, no text, no watermark.
A set of separate VFX texture elements for a combo-stack passive, evenly spaced on a transparent background, nothing overlapping:
1) a small glowing vermilion-orange round dot (stack marker, several will be placed in a row above an enemy), 2) a small vermilion flame-shaped marker (full 5 stacks).
```
