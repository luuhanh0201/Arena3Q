# Chương 1 – Loạn Khăn Vàng: nguồn asset 3D (Claude, giai đoạn A bước 1–4)

Ngày 28/10/2026. Theo `ArtSource/Chapter01/Docs/Chuong1.md` §6–§7. Người dùng chốt: **chỉ asset miễn phí (CC0, CC BY)**, **Sketchfab do người dùng tự tải theo danh sách**, chỗ thiếu thì **ghép / sửa trong Blender và được dùng AI 3D** (người dùng cung cấp file). Giai đoạn này **chỉ tìm, kiểm giấy phép, tải, lập danh mục**; chưa import Unity, chưa dựng scene, chưa code.

- Bảng đầy đủ theo các cột của §7: `AssetManifest_3D.csv` (45 dòng: 3 nguồn đã có, 5 gói CC0, 27 ứng viên Sketchfab, 10 mục MISSING).
- Ảnh duyệt: `packs.png` (gói đã tải), `sketchfab-shortlist.png` (ứng viên Sketchfab).
- File gốc tải về: `ArtSource/Chapter01/3D/Sources/` (**gitignore**, chỉ lưu trên máy; SHA256 ghi trong CSV).

## 1. Đã có trong dự án (dùng lại trước)

| ID | Nguồn | Giấy phép | Dùng cho |
|---|---|---|---|
| EX01 | Quaternius Stylized Nature MegaKit Standard (68 model) | CC0 | cây, bụi, cỏ, đá toàn map |
| EX02 | Quaternius Fantasy Props MegaKit Standard (94 model) | CC0 | giá vũ khí, cũi nhỏ, xe hàng, cờ, đuốc, vạc, thùng, rương, bao |
| EX03 | Mô hình Arena3Q (Meshy, dự án sở hữu): trại chỉ huy, tháp canh gỗ, hàng rào, cột cờ, cột đá và đài Bát Quái | dự án | Ải 5–6 doanh trại; Ải 7 cột / nền (bỏ hoạ tiết Bát Quái) |

## 2. Gói CC0 Claude đã tải (tải trực tiếp, không cần tài khoản)

| ID | Gói | Số model | Ghi chú phong cách |
|---|---|---|---|
| PK01 | [Kenney Nature Kit 2.1](https://kenney.nl/assets/nature-kit) | 329 | khối màu phẳng, không texture; **có bộ vách đá (cliff kit), bờ, cầu, thác** mà đợt 25/10 còn thiếu; dùng dựng khối thô rồi đổi màu |
| PK02 | [Kenney Fantasy Town Kit](https://kenney.nl/assets/fantasy-town-kit) | 167 | màu phẳng; lấy bậc đá, cột, hàng rào, xe, cối xay nước; bỏ mái/cửa sổ kiểu châu Âu |
| PK03 | [Quaternius Medieval Village MegaKit Standard](https://opengameart.org/content/medieval-village-megakit) | 176 | có texture, hợp chất lượng map hiện tại; **tường trát/gỗ để ghép với mái ngói cong** thành nhà, kho lương Đông Hán |
| PK04 | [Quaternius Medieval Village Pack 12/2020](https://opengameart.org/content/lowpoly-medieval-village-pack) | 44 | giếng, xe, cỏ khô, bao, lửa trại, vạc, quầy hàng (nhà kiểu châu Âu không dùng nguyên trạng) |
| PK05 | [Quaternius Ultimate Nature Pack](https://quaternius.itch.io/150-lowpoly-nature-models) | 150 | **chưa tải được**: Google Drive báo "Quota exceeded"; tuỳ chọn (liễu, lúa mì làm ruộng xa, cây lá thu) |

Không dùng: Poly Pizza (kết quả tìm cần JavaScript, phần lớn là quảng cáo gói trả phí).

## 3. Sketchfab – nhờ người dùng tải (CC BY 4.0, đã kiểm "cho tải" qua API Sketchfab)

Cách tải: đăng nhập Sketchfab → mở link → **Download 3D Model** → chọn **glTF** (nếu không có thì FBX) → lưu vào `ArtSource/Chapter01/3D/Sources/Sketchfab/SFxx/` (xx = mã bên dưới, giải nén giữ nguyên). Claude sẽ kiểm mesh, texture, pivot, số mặt rồi mới chuẩn hoá.

### 3.1 Nên tải (19 model)

| Mã | Model | Tác giả | Số mặt | Dùng ở |
|---|---|---|---|---|
| SF01 | [Ancient Chinese House](https://sketchfab.com/3d-models/ancient-chinese-house-bcb41894d1b5451b9c4f6489d50766d4) | NinoJazz | 4.028 | Ải 1 Làng Bình An: nhà dân |
| SF02 | [Chines house - low poly](https://sketchfab.com/3d-models/chines-house-low-poly-8e6350e298964bb8b06bcb2c5c9b00e4) | merlincodes | 3.830 | Ải 1, Ải 4: nhà dân biến thể |
| SF03 | [chinese house](https://sketchfab.com/3d-models/chinese-house-2bb6ef8d3a5a4019b00bf86bbacd96cc) | noyou | 3.112 | Ải 1, Ải 4: nhà dân chính |
| SF04 | [Low Poly Asian House](https://sketchfab.com/3d-models/low-poly-asian-house-a404d18ba557415b96d6e639e4279e85) | Dylan_Piserchia | 3.579 | Ải 1, Ải 4: nhà nhỏ |
| SF05 | [Asian House Pack - Low Poly](https://sketchfab.com/3d-models/asian-house-pack-low-poly-70f36c58345440fe8e7b6168854881e7) | PolyDavid | 3.504 | Ải 1, Ải 4: bộ nhà nhỏ |
| SF06 | [Chinese roof. Low poly model.](https://sketchfab.com/3d-models/chinese-roof-low-poly-model-783448e879ac4f3c84beb6c836a1b241) | KSmodels | 754 | Mái ngói để ghép lên tường MegaKit |
| SF09 | [Ancient Chinese Arena](https://sketchfab.com/3d-models/ancient-chinese-arena-1bb1d60f152f4c9795ec8a411404efdf) | zhuoyi0904 | 3.832 | Ải 7 Tế Đàn: nền đài, bậc đá |
| SF11 | [Chinese drum tower](https://sketchfab.com/3d-models/chinese-drum-tower-879fef3c206e471bac0818ae29a458a0) | Murilo Rocha | 6.700 | Ải 6 Đại Doanh: lầu trống |
| SF12 | [Low Poly Watchtower (Isometric)](https://sketchfab.com/3d-models/low-poly-watchtower-isometric-239b874ebfb1474a8de0e0adb7c681e1) | Peter Primini | 550 | Ải 2, 3, 5, 6: tháp canh gỗ |
| SF15 | [Free Bamboo Set](https://sketchfab.com/3d-models/free-bamboo-set-e9f9fa5397814f81bf85ad06acf5bf30) | JonhGillessen | 37.834 | Ải 3 Rừng Trúc: thân tre |
| SF16 | [Bamboo Bush](https://sketchfab.com/3d-models/bamboo-bush-d0d5eb345ff7420bb7f5c5239fcec637) | sujirour | 1.098 | Ải 3: bụi tre |
| SF17 | [bamboo](https://sketchfab.com/3d-models/bamboo-a02bf0e3ffe44617ad49daf3cd94fe59) | evolveduk | 2.180 | Ải 3: cây tre lẻ |
| SF18 | [Sakura Tree 01 - Low Poly Model](https://sketchfab.com/3d-models/sakura-tree-01-low-poly-model-147ae7d0d332456a99ec6195e9b0cd4f) | Jogoss | 7.007 | Cây hoa đào (thay cây anh đào), Ải 1 và rìa map |
| SF19 | [Japanese Cherry Tree (low-Poly)](https://sketchfab.com/3d-models/japanese-cherry-tree-low-poly-7c9e7c4e971f4953b06faf300cbb1209) | Sereib | 5.036 | Cây hoa đào biến thể |
| SF20 | [Rice Field](https://sketchfab.com/3d-models/rice-field-7a077bd3c27c41f0a831c6a11b005c8b) | jessica.novita | 3.980 | Ải 2 Đồng Lúa: ruộng lúa |
| SF21 | [Chinese Incense Burner-LowPory-Material test](https://sketchfab.com/3d-models/chinese-incense-burner-lowpory-material-test-69dbb96caea84dcab6ac8fa83af78ff9) | chiwei | 11.545 | Ải 7: lư hương lớn |
| SF23 | [Old Chinese Lantern - lowpoly for free download](https://sketchfab.com/3d-models/old-chinese-lantern-lowpoly-for-free-download-73b1e60901e6494fb0cb39affca6f850) | Scritta | 976 | Đèn lồng cổ, Ải 1/5/6 |
| SF24 | [Chinese Lantern](https://sketchfab.com/3d-models/chinese-lantern-0fc4dbf7122141828c16bae9025ce47c) | Batuhan13 | 1.412 | Đèn lồng treo |
| SF26 | [Low Bridge](https://sketchfab.com/3d-models/low-bridge-015406b34db64859a6d17fb8c825bf52) | YanaBelaya | 9.809 | Cầu gỗ thấp |

### 3.2 Tuỳ chọn

| Mã | Model | Tác giả | Số mặt | Dùng ở |
|---|---|---|---|---|
| SF07 | [Chinese style tea  pavilion](https://sketchfab.com/3d-models/chinese-style-tea-pavilion-7625f6818cab4fe58a4e1a09f186d7b1) | AlvinBi | 4.942 | Ải 2 chòi quan sát / Ải 1 đình làng |
| SF08 | [Chinese Pavilion Memoriam](https://sketchfab.com/3d-models/chinese-pavilion-memoriam-87f4931714d7481baa54e5050f679be2) | TenatiousV | 14.716 | Ải 7 Tế Đàn: đình nghi lễ |
| SF13 | [LOWP - Lunar Year - CC0 Asset Pack](https://sketchfab.com/3d-models/lowp-lunar-year-cc0-asset-pack-db132fddf6804657a213ef3dabe16902) | Polygonal Mind | 78.837 | Đèn lồng, cổng, trang trí Tết; lấy từng món |

### 3.3 Loại (đã xem, không hợp)

SF10 Pagoda (tháp Phật giáo, muộn hơn năm 184), SF14 Polygon World Vietnam (quá hoạt hình, cây dừa), SF22 lư hương kim loại tròn, SF25 cầu dây tả thực, SF27 Village head (tả thực, không khớp nhân vật chibi Arena3Q).

## 4. Còn thiếu (MISSING) và cách bù

| Mã | Thiếu | Ải | Cách bù đề xuất |
|---|---|---|---|
| MS01 | Cổng Đại Doanh Hoàng Cân | 6 | ghép hàng rào + 2 tháp canh (EX03) + mái SF06; hoặc AI 3D |
| MS02 | Cũi giam dân lớn | 1, 4 | phóng to Cage_Small (EX02) hoặc ghép hàng rào gỗ |
| MS03 | Xe chở lương kiểu Đông Hán | 2, 4 | đổi màu Prop_Wagon (PK03) / Stall_Cart (EX02) + bao (PK04); hoặc AI 3D |
| MS04 | Kho lúa gỗ | 2, 4 | tường gỗ MegaKit + mái SF06 (Blender) |
| MS05 | Cổng làng gỗ | 1 | cột gỗ + mái SF06; hoặc AI 3D |
| MS06 | Ba trụ năng lượng Tế Đàn | 7 | cột đá Bát Quái (EX03) bỏ hoạ tiết, đổi màu |
| MS07 | Tượng đá hợp bối cảnh | 7 | Kenney "statue" đổi màu; hoặc AI 3D |
| MS08 | Trống trận lớn đứng riêng | 5, 6 | tách từ trại chỉ huy (EX03); hoặc AI 3D |
| MS09 | NPC: trưởng làng, dân, nghĩa binh, lính Khăn Vàng, Trình Viễn Chí, Trương Giác | 1–7 | dùng lại rig / nhân vật Arena3Q trước; thiếu thì AI 3D (Meshy) người dùng cung cấp |
| MS10 | Hoa đào, tre đúng phong cách | 1, 3 | SF15–SF19 đổi màu, kiểm trên camera thật |

## 5. Ghi công bắt buộc (CC BY) – đưa vào màn Credits của game khi dùng

- "Ancient Chinese House" by NinoJazz (https://sketchfab.com/3d-models/ancient-chinese-house-bcb41894d1b5451b9c4f6489d50766d4), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Chines house - low poly" by merlincodes (https://sketchfab.com/3d-models/chines-house-low-poly-8e6350e298964bb8b06bcb2c5c9b00e4), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "chinese house" by noyou (https://sketchfab.com/3d-models/chinese-house-2bb6ef8d3a5a4019b00bf86bbacd96cc), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Low Poly Asian House" by Dylan_Piserchia (https://sketchfab.com/3d-models/low-poly-asian-house-a404d18ba557415b96d6e639e4279e85), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Asian House Pack - Low Poly" by PolyDavid (https://sketchfab.com/3d-models/asian-house-pack-low-poly-70f36c58345440fe8e7b6168854881e7), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Chinese roof. Low poly model." by KSmodels (https://sketchfab.com/3d-models/chinese-roof-low-poly-model-783448e879ac4f3c84beb6c836a1b241), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Chinese style tea  pavilion" by AlvinBi (https://sketchfab.com/3d-models/chinese-style-tea-pavilion-7625f6818cab4fe58a4e1a09f186d7b1), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Chinese Pavilion Memoriam" by HaraKiri (https://sketchfab.com/3d-models/chinese-pavilion-memoriam-87f4931714d7481baa54e5050f679be2), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Ancient Chinese Arena" by zhuoyi0904 (https://sketchfab.com/3d-models/ancient-chinese-arena-1bb1d60f152f4c9795ec8a411404efdf), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Chinese drum tower" by Murilo Rocha (https://sketchfab.com/3d-models/chinese-drum-tower-879fef3c206e471bac0818ae29a458a0), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Low Poly Watchtower (Isometric)" by Pippa (https://sketchfab.com/3d-models/low-poly-watchtower-isometric-239b874ebfb1474a8de0e0adb7c681e1), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "LOWP - Lunar Year - CC0 Asset Pack" by Polygonal Mind (https://sketchfab.com/3d-models/lowp-lunar-year-cc0-asset-pack-db132fddf6804657a213ef3dabe16902), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Free Bamboo Set" by JonhGillessen (https://sketchfab.com/3d-models/free-bamboo-set-e9f9fa5397814f81bf85ad06acf5bf30), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Bamboo Bush" by sujirour (https://sketchfab.com/3d-models/bamboo-bush-d0d5eb345ff7420bb7f5c5239fcec637), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "bamboo" by evolveduk (https://sketchfab.com/3d-models/bamboo-a02bf0e3ffe44617ad49daf3cd94fe59), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Sakura Tree 01 - Low Poly Model" by Jogoss (https://sketchfab.com/3d-models/sakura-tree-01-low-poly-model-147ae7d0d332456a99ec6195e9b0cd4f), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Japanese Cherry Tree (low-Poly)" by Sereib (https://sketchfab.com/3d-models/japanese-cherry-tree-low-poly-7c9e7c4e971f4953b06faf300cbb1209), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Rice Field" by jessica.novita (https://sketchfab.com/3d-models/rice-field-7a077bd3c27c41f0a831c6a11b005c8b), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Chinese Incense Burner-LowPory-Material test" by chiwei (https://sketchfab.com/3d-models/chinese-incense-burner-lowpory-material-test-69dbb96caea84dcab6ac8fa83af78ff9), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Old Chinese Lantern - lowpoly for free download" by Scritta (https://sketchfab.com/3d-models/old-chinese-lantern-lowpoly-for-free-download-73b1e60901e6494fb0cb39affca6f850), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Chinese Lantern" by Batuhan13 (https://sketchfab.com/3d-models/chinese-lantern-0fc4dbf7122141828c16bae9025ce47c), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- "Low Bridge" by YanaBelaya (https://sketchfab.com/3d-models/low-bridge-015406b34db64859a6d17fb8c825bf52), licensed under CC BY 4.0 (http://creativecommons.org/licenses/by/4.0/).
- Kenney (www.kenney.nl) và Quaternius (quaternius.com): CC0, không bắt buộc ghi công; vẫn ghi để minh bạch.

## 6. Xung đột đã chốt

- 28/10 (người dùng): **Chương 1 là Loạn Khăn Vàng** theo `ArtSource/Chapter01/Docs/Chuong1.md`; thứ tự chương trong `docs/QuanDich/` chỉ là bản thử.

**Tạm dừng tìm asset (28/10, người dùng):** người dùng sẽ gửi nguồn asset sau; chưa tải Sketchfab, chưa làm bước 7.

## 7. Bước kế (khi người dùng duyệt danh sách)

Kiểm từng file (mesh, normal, UV, texture thiếu, pivot, số tam giác) → chỉnh / ghép trong Blender (mái cong, đổi màu theo bảng màu §2) → import URP, prefab, collider đơn giản, LOD → scene mẫu so màu (nhà + cây + đá + đất + nắng) → dựng khối 7 khu vực → ảnh tham chiếu cho Codex. Mọi asset phải ghi rõ chất lượng ở 3 cấu hình 30 / 60 / 120 FPS (quy tắc 28/10).

## 8. Lượt 2 (28/10): nguồn 3D theo file `ArtSource/Chapter01/Docs/Arena3Q_Chuong1_Asset_2D_va_Nguon.xlsx` của người dùng

Kết quả: **`Arena3Q_Chuong1_Nguon3D.xlsx`** (2 trang: "Nguồn 3D theo hạng mục" cho đủ 69 hạng mục, "Gói nguồn 3D" có giấy phép, đường dẫn, số model, SHA256) và ảnh duyệt **`sources-new.png`**.

- 55 hạng mục cần 3D: **19 dùng nguồn có sẵn** trong dự án, **32 đã có nguồn tải về máy**, **4 chưa có nguồn sẵn** (cổng Đại Doanh lớn, trống trận rời, Trình Viễn Chí, Trương Giác: ghép trong Blender hoặc AI 3D). 14 hạng mục "2D thay thế được" để Codex vẽ, có ghi dự phòng 3D gần camera.
- Gói mới tải (file gốc trong `3D/Sources/`, gitignore): Quaternius Ultimate Nature, Universal Base Characters, Modular Character Outfits Fantasy; Kenney Castle Kit; KayKit Medieval Hexagon, Forest, Adventurers; CS Studio Chinese Building Set 1; Chinese Four-corner Pavilion; Low Poly Farm & Market FREE; SOI Medieval Interior; 7 model Poly Pizza (Quaternius); OpenGameArt Yughues (tre, cây cỏ vẽ tay, bụi) và rubberduck.
- Giấy phép: CC0 cho hầu hết; **CS Studio** và **Farm & Market** là giấy phép riêng cho dùng thương mại, CS Studio **cấm phát tán lại file gốc** (giữ ngoài git); **SOI** chỉ có câu trả lời bình luận của tác giả, **rủi ro**, cần người dùng duyệt trước khi dùng. Bằng chứng lưu ở `LICENSE_NOTE.txt` cạnh từng gói.
- Cần người dùng tải (phải đăng nhập): Holotna (Unity Asset Store), 3dforgex (CGTrader), Mixamo, Fab; Sketchfab vẫn tạm hoãn.
- Không dùng: Grab3D Wooden Palisade (16.432 tam giác mỗi đoạn, quá nặng), Rice Pack Superhive (trả phí).

## 9. Lượt 3 (28/10 15:30): kiểm tài nguyên dựng map đã đủ chưa

Đối chiếu danh sách 3D của 7 ải (Chuong1.md §12–§18) với các mẫu đã render dưới ánh sáng trận (`Renders/index.html`, `Renders/GapCheck/sheet.jpg`). Kết luận: **chưa đủ**. Khu làng, đồng lúa, trại lương dựng được ngay; rừng trúc, đại doanh, tế đàn và toàn bộ nhân vật còn thiếu.

Sửa danh mục ở trên: mục 23 `building_grain` là ô ruộng lúa chứ không phải kho, thay bằng CS Studio Horse room; mục 31 `building_barracks` (lâu đài đá kiểu Âu) và mục 33 `building_stage` (đống ván) lệch phong cách; mục 21 cổng trại nông trại không phải cổng làng; mục 3, 8, 9 (Kenney vách đá, cầu, bậc đá) và tượng `statue_block` màu phẳng, lệch phong cách; 7 file GLB của Poly Pizza (lều, xe đổ, rương, tre, giếng, hàng rào, xe) **chưa nhập được vào Unity** vì project chưa có gói glTFast.

| Ải | Đủ | Tạm dùng (cần chỉnh) | Thiếu |
|---|---|---|---|
| 1 Làng Bình An | nhà CS Studio (4 mẫu x 3 bản màu), giếng, cũi `Cage_Small`, hòm, thùng | giỏ lương (Farm, màu phẳng) | cổng làng gỗ, xe hàng bị đổ (GLB) |
| 2 Đồng lúa, kho lương | kho (Horse room), xe lương (MegaKit Wagon), bao, chòi gác, rào, ruộng (2D 04, 12) | xe thứ hai lặp cùng mẫu | |
| 3 Rừng trúc | đá, tháp canh, rào, giá vũ khí, thùng | tre Nobiax (một cây mảnh, cần dựng cụm + thẻ lá 2D 09) | cầu gỗ hợp phong cách, lều lính nhỏ |
| 4 Trại lương, thôn trang | kho, nhà, rào, chiến kỳ (cột cờ + cờ 22) | 3 xe lặp một mẫu, xe kéo | |
| 5 Tiền doanh | khu chỉ huy, lều lớn, cột cờ, giá thương đao, 2 tháp, rào lớn | | trống trận đứng riêng |
| 6 Đại doanh | tường gỗ, lều chỉ huy, giá vũ khí, sân luyện (nền 2D 28) | tháp cung (dùng lại tháp canh), bậc thang | cổng đại doanh, doanh trại lính, trống trận lớn |
| 7 Tế đàn | cờ và cột nghi lễ, đuốc (2D 23) | nền tế đàn và 3 trụ (Bát Quái, phải bỏ hoạ tiết và đổi màu), lư hương (`Cauldron` là nồi) | địa hình cao / vách đá hợp phong cách, bậc đá, tượng đá |
| Địa hình chung | mặt đất, đồi thấp tự dựng, nước và bờ sông (2D 05, 08) | | vách đá |
| Nhân vật | tướng đại diện | | trưởng làng, dân, nghĩa binh, lính Khăn Vàng 3 loại, hộ pháp, Trình Viễn Chí, Trương Giác (Quaternius / KayKit là kiểu Âu, chibi) |

Phần 2D của Codex (29 nhóm) phủ đủ texture, decal và sprite trang trí; chưa có hoa dại, mây / chim xa, toàn bộ hiệu ứng (sương khóa ải, tàn lửa, bụi lá bay, vòng sáng NPC, sét, hào quang boss) và UI bản đồ (§4), vì chưa nằm trong danh sách 2D của người dùng.

## 10. Lượt 4 (28/10 17:40): Claude tự dựng phần thiếu (kitbash)

Theo bảng mục 9, phần tự làm được đã dựng xong (`Renders/index.html`, mục "Phần tự dựng"): cổng làng (Ải 1), cổng đại doanh (Ải 6), lều lính (Ải 3 đến 6), trống trận và trống lớn (Ải 5, 6), vách đá A, B và mỏm đá, nền cao tế đàn + bậc đá (Ải 7), 3 cụm tre (Ải 3). Hình khối là của dự án (`Assets/Arena3Q/Art/Chapter01/Kitbash/*.fbx`, script `3D/Kitbash/ch01_kitbash.py`); vật liệu và prefab trong `Assets/ThirdParty/Chapter01/_Kit` (gitignore, dựng lại bằng **Arena3Q > Chapter 01 > Build Kitbash Prefabs**). Texture: Quaternius MegaKit, Stylized Nature (CC0), Nobiax bamboo (CC0), ảnh 01, 06, 17 của Codex.

Còn thiếu sau lượt này: doanh trại lính (Ải 6), cầu gỗ (Ải 3), xe hàng bị đổ (Ải 1), tượng đá (Ải 7), lư hương đúng kiểu, toàn bộ nhân vật. Cụm tre nặng (8,6k đến 14,4k tam giác mỗi cụm): rừng trúc phải có LOD hoặc dùng thẻ lá 2D cho cụm xa.

## 11. Chuẩn hóa 22 nguồn Sketchfab (29/10, Codex tiếp bàn giao Claude)

Đã xuất47mảnh FBX theo mét và prefab URP local ở `Assets/ThirdParty/Chapter01/Sketchfab`; kiểm Unity đúng kích thước, pivot, UV và số tam giác, không đỉnh NaN. Texture dẫn xuất tối đa1024; nguồn tải giữ nguyên. Cấu hình và cách dựng lại: `../Sketchfab/README.md`; giấy phép và ghi công từng nguồn: `SKETCHFAB_CREDITS.md`. Cả22file license tải kèm ghi CC BY4.0. Riêng SF13 tiêu đề có CC0 nhưng license.txt ghi CC BY4.0, khác ghi chú bàn giao; vẫn ghi công theo file giấy phép.

- **SF20 Rice Field** thực tế là đảo đồi ruộng bậc thang, tên mảnh `TerraceIsland`; chưa phù hợp thay ruộng phẳng ở Ải2.
- **SF26 Low Bridge** thực tế là hành lang có mái, tên `CoveredCorridor`; không dùng thay cầu vượt suối. Cầu gỗ kitbash hiện có là hạng mục khác.
- SF04 bỏ cả10mặt nền cỏ; SF02 bỏ hai đế trùng; SF07/SF08 bỏ phần cảnh ngoài đình. SF05 tách4nhà, SF13 tách8món, SF15 lấy9cây cao/4cụm/3thân theo giới hạn cấu hình.
- SF08:13.584→7.760tam giác; SF21:11.545→5.003. Ba nhà SF13 còn10–29k mỗi mảnh; chưa có LOD, chưa đo FPS thiết bị.
- Ảnh đối chiếu Blender/Unity tại `NghiemThu/Chapter01/Sketchfab_20261029`, trang local `http://127.0.0.1:4322/sketchfab-review/`. Chiều cao và màu vẫn là ứng viên để duyệt; **chưa thay model vào map**. Các vật liệu truyền sáng/alpha blend được đơn giản hóa sang opaque/cutout cho preview URP.
