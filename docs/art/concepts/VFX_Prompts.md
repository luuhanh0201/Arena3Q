# Hiệu ứng kỹ năng (VFX) – prompt tạo ảnh (dán nguyên văn)

Phong cách: **A – render 3D stylised** cùng tông với ảnh mẫu `chutuong1.png`; kỹ năng của từng chủ tướng: `docs/LordHeroes.md` §2.
Hiệu ứng **không** vẽ từng khung: mình dựng bằng **hệ thống hạt (particle) của Unity** từ các **mảnh hiệu ứng** dưới đây (xoay, phóng to, mờ dần, lặp lại).

**Quy tắc chung**
- Mỗi prompt cho ra **một tấm nhiều mảnh** xếp cách nhau; bạn cắt từng mảnh ra PNG riêng trong Krita.
- **Nền trong suốt.** Nếu ChatGPT không cho nền trong suốt, dùng **nền đen tuyệt đối** (`pure black background`) – mình dùng chế độ cộng sáng (additive) nên nền đen tự biến mất. **Không** dùng nền trắng.
- Hình **nằm trên mặt đất** (vòng trận, vùng lửa, vết nứt) vẽ **nhìn thẳng từ trên xuống, hình tròn** – game tự nén theo góc 3/4.
- Kích thước mỗi mảnh sau khi cắt: **128–256 px** (mảnh nhỏ: tia lửa, lá) · **512 px** (vòng trận, vụ nổ lớn).
- Tên file: `vfx_<chủ tướng>_<kỹ năng>_<mảnh>.png`, ví dụ `vfx_strategist_firefield_decal.png`. Lưu vào thư mục này trước, mình kiểm tra rồi chuyển vào `Assets/Arena3Q/Art/VFX/`.
- Đính kèm: `chutuong1.png` (phong cách) + ảnh bảng xoay của chủ tướng đó (để màu hiệu ứng hợp nhân vật).

| # | Chủ tướng | Kỹ năng | Tên kỹ năng trong file |
|---|---|---|---|
| 0 | Chung | Trúng đòn, xuất trận, thanh nộ đầy | `common` |
| 1–4 | Mỗi chủ tướng | Đánh thường | `basic` |
| 5–7 | Cung thủ | Liên Châu Tiễn · Tiễn Vũ · Phá Thành Hỏa Tiễn | `multishot` · `arrowrain` · `firearrow` |
| 8–10 | Kiếm sĩ | Phi Ảnh Đột Kích · Toàn Phong Trảm · Vạn Kiếm Quy Tông | `dash` · `whirlwind` · `swordrain` |
| 11–13 | Mưu sĩ | Hỏa Kế · Bát Quái Trận · Đông Phong | `firefield` · `bagua` · `eastwind` |
| 14–16 | Thống soái | Hiệu Triệu · Trống Trận · Thiết Kỵ Xung Phong | `rally` · `wardrum` · `cavalry` |

---

## 0. Chung
Đính kèm: `chutuong1.png` · Lưu: `vfx_common_sheet.png` → cắt thành `vfx_common_<mảnh>.png`
```
Stylised 3D-rendered game visual effects matching the look of the attached reference image (soft, clean, glossy, mobile game quality), glowing, high contrast, no characters, no text, no watermark.
A set of separate VFX texture elements for a mobile strategy game, evenly spaced on a transparent background, nothing overlapping:
1) small white-gold hit spark star burst, 2) circular impact shockwave ring seen from directly above (thin, white), 3) soft round glow orb (white, fading edges), 4) a vertical pillar of soft golden light (summon effect), 5) small sparkle particle (four-pointed star), 6) a circular golden glow ring seen from directly above (ultimate ready).
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
