# Bản đồ – prompt tạo ảnh (dán nguyên văn)

Phong cách bản đồ: **vẽ tay, màu mềm** (map spec §8.1), góc 3/4 cố định – mặt đất nén dọc **0.75** (🟡). Ảnh chuẩn hiện có: `Assets/Arena3Q/Art/Maps/map1.png`.
Giải thích cách dùng, ảnh bố cục gửi kèm, hậu kỳ, mẫu brief cho họa sĩ: `docs/BanDo/MapArtGuide.md`. Số đo: `docs/BanDo/MapSpecification.md` §8.

> Ảnh toàn bản đồ chỉ để **chốt phong cách**; thứ đưa vào game là **từng mảnh** (ô nền, dải sông, cầu, thành, cây/đá) đúng kích thước pixel bên dưới.

| # | Mảnh | Đính kèm | Kích thước cuối | Lưu thành |
|---|---|---|---|---|
| 1 | Concept toàn bản đồ (không dùng trong game) | `docs/art/blueprints/attack_01_mask.png` (+ `map1.png` để giữ phong cách) | tỉ lệ 2.46 : 1 | `map_concept_XX.png` |
| 2 | Ô nền cỏ (2–3 biến thể) | `map1.png` | **128 × 96 px** mỗi ô | `ground_grass_01.png` … |
| 3 | Ô nền đất / đường mòn / bờ cát | `map1.png` | 128 × 96 px | `ground_dirt_01.png`, `ground_path_01.png`, `ground_sand_01.png` |
| 4 | Dải nước sông | `map1.png` | **512 × 296 px** (cao 296 ≈ rộng sông 2.3 × 128) | `river_water_strip.png` |
| 5 | Dải bờ sông | `map1.png` | 512 × ~96 px | `river_bank_strip.png` |
| 6 | Cầu | `map1.png` | **464 × 320 px** (dài 3.6 bắc qua sông, mặt cầu rộng 2.4) | `bridge_wood.png` |
| 7 | Thành ta | `map1.png` | **448 × 576 px**, chân thành giữa cạnh dưới | `castle_player.png` |
| 8 | Thành địch | `castle_player.png` | 448 × 576 px | `castle_enemy.png` |
| 9 | Cây, bụi, đá | `map1.png` | cây ≈ 192 × 320 px | `prop_tree_01.png` … |

AI thường trả ảnh 1024/1536 px – thu nhỏ/cắt lại đúng kích thước trong Krita (`docs/BanDo/MapArtGuide.md` §5). Ảnh mảnh lưu vào thư mục này trước, mình kiểm tra rồi chuyển vào `Assets/Arena3Q/Art/Maps/`.

---

## 1. Concept toàn bản đồ
Đính kèm: `docs/art/blueprints/attack_01_mask.png` (ảnh 1) và `Assets/Arena3Q/Art/Maps/map1.png` (ảnh 2, phong cách)
```
Hand-painted 2D game art for a mobile strategy game, soft colours, gentle shading, Clash of Clans-like style, matching the painting style of the second attached image.
Fixed 3/4 top-down view: camera tilted about 41 degrees, the ground's vertical axis foreshortened to 0.75 (a 1x1 ground square appears 4 wide by 3 tall). No perspective vanishing points, orthographic projection.
Soft light from the top-left, short soft shadows towards the bottom-right.
No text, no UI, no characters, no watermark. Clean readable shapes at small mobile size.

Use the first attached image as the exact layout of a battlefield. Keep every shape in place: green = grassy meadow, blue = a winding river (keep its exact path and width), brown = two wooden bridges across the river, blue circle on the left = empty stone plaza for the player's castle, red circle on the right = empty stone plaza for the enemy castle.
Open field, a few trees and rocks only near the top and bottom edges, nothing blocking the meadow.
Wide panoramic image, aspect ratio 2.46:1.
```

---

## 2. Ô nền cỏ (lặp liền 4 phía)
Đính kèm: `map1.png`
```
Hand-painted 2D game art for a mobile strategy game, soft colours, gentle shading, Clash of Clans-like style, matching the painting style of the attached image.
Fixed 3/4 top-down view, the ground's vertical axis foreshortened to 0.75, orthographic, soft light from the top-left.
No text, no UI, no characters, no watermark.

Seamless tileable ground texture: short lush grass with tiny flowers and subtle colour variation, evenly lit, no strong features that repeat visibly, no shadows from objects. Tiles perfectly on all four sides.
```
Biến thể: tạo lại 2–3 lần, hoặc thêm `slightly drier grass with small clover patches`.

## 3. Ô nền đất / đường mòn / bờ cát
Đính kèm: `map1.png` · Dùng prompt mục 2, thay câu cuối bằng **một** trong các câu sau:
```
Seamless tileable ground texture: packed dirt path with small pebbles, evenly lit, no strong features that repeat visibly, no shadows from objects. Tiles perfectly on all four sides.
```
```
Seamless tileable ground texture: sandy riverbank with small smooth stones, evenly lit, no strong features that repeat visibly, no shadows from objects. Tiles perfectly on all four sides.
```
```
Seamless tileable transition texture: grass on the top half fading naturally into packed dirt on the bottom half, evenly lit, tiles seamlessly left-to-right.
```

---

## 4. Dải nước sông (lặp liền theo chiều ngang)
Đính kèm: `map1.png`
```
Hand-painted 2D game art for a mobile strategy game, soft colours, gentle shading, Clash of Clans-like style, matching the painting style of the attached image.
Fixed 3/4 top-down view, orthographic, soft light from the top-left. No text, no UI, no characters, no watermark.

Seamless horizontal strip of a calm shallow river seen from above, water flowing left to right, soft ripples and light reflections, slightly darker in the middle. Tiles seamlessly left-to-right.
Straight strip, no bends, no banks, transparent outside the water. Wide image, aspect ratio about 7:4.
```

## 5. Dải bờ sông
Đính kèm: `map1.png`
```
Hand-painted 2D game art for a mobile strategy game, soft colours, gentle shading, Clash of Clans-like style, matching the painting style of the attached image.
Fixed 3/4 top-down view, orthographic, soft light from the top-left. No text, no UI, no characters, no watermark.

Seamless horizontal strip of a grassy riverbank edge: grass on top, a narrow band of wet sand and small stones along the bottom where it meets the water. Transparent background below the edge. Tiles seamlessly left-to-right. Wide image.
```

---

## 6. Cầu
Đính kèm: `map1.png`
```
Hand-painted 2D game art for a mobile strategy game, soft colours, gentle shading, Clash of Clans-like style, matching the painting style of the attached image.
Fixed 3/4 top-down view: camera tilted about 41 degrees, the ground's vertical axis foreshortened to 0.75, orthographic, no vanishing points. Soft light from the top-left, short soft shadows towards the bottom-right. No text, no UI, no characters, no watermark.

A sturdy wooden plank bridge with rope railings, crossing a river from left to right, walkable deck clearly visible, centred, the deck about as wide as two thirds of its length, transparent background, nothing else in the image.
```
Lưu ý: sông chạy thẳng đứng dưới mỗi cầu (map spec §6.2) nên cầu vẽ **nằm ngang**, không xoay.

---

## 7. Thành ta
Đính kèm: `map1.png`
```
Hand-painted 2D game art for a mobile strategy game, soft colours, gentle shading, Clash of Clans-like style, matching the painting style of the attached image.
Fixed 3/4 top-down view: camera tilted about 41 degrees, orthographic, no vanishing points. Soft light from the top-left, short soft shadows towards the bottom-right. No text, no UI, no characters, no watermark.

A small fortified ancient East Asian stone castle keep with a wooden gate and towers, blue banners and blue roofs (player side), standing on a round stone base, transparent background, the base touching the bottom centre of the image, portrait image with aspect ratio 7:9.
```

## 8. Thành địch
Đính kèm: `castle_player.png` (để hai thành cùng hình khối)
```
Redraw the attached castle with exactly the same shape, size, view angle and painting style, but as the enemy side: red banners and dark red roofs, slightly more menacing details (spikes on the walls, darker stone). Transparent background, the base touching the bottom centre of the image, no text.
```

---

## 9. Cây, bụi, đá (đặt rìa bản đồ)
Đính kèm: `map1.png`
```
Hand-painted 2D game art for a mobile strategy game, soft colours, gentle shading, Clash of Clans-like style, matching the painting style of the attached image.
Fixed 3/4 top-down view, orthographic, soft light from the top-left, short soft shadows towards the bottom-right. No text, no UI, no characters, no watermark.

A set of 6 separate decorative props on a transparent background: two round leafy trees, one pine tree, a bush, a mossy boulder, a small cluster of stones. Each object standing upright, evenly spaced, not overlapping.
```
