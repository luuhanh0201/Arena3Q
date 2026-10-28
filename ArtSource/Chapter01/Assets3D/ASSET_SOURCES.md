# Chương 1 – Loạn Khăn Vàng: nguồn asset 3D (Claude, giai đoạn A bước 1–4)

Ngày 28/10/2026. Theo `docs/Ai/Chuong1.md` §6–§7. Người dùng chốt: **chỉ asset miễn phí (CC0, CC BY)**, **Sketchfab do người dùng tự tải theo danh sách**, chỗ thiếu thì **ghép / sửa trong Blender và được dùng AI 3D** (người dùng cung cấp file). Giai đoạn này **chỉ tìm, kiểm giấy phép, tải, lập danh mục**; chưa import Unity, chưa dựng scene, chưa code.

- Bảng đầy đủ theo các cột của §7: `AssetManifest_3D.csv` (45 dòng: 3 nguồn đã có, 5 gói CC0, 27 ứng viên Sketchfab, 10 mục MISSING).
- Ảnh duyệt: `packs.png` (gói đã tải), `sketchfab-shortlist.png` (ứng viên Sketchfab).
- File gốc tải về: `ArtSource/Chapter01/Sources3D/` (**gitignore**, chỉ lưu trên máy; SHA256 ghi trong CSV).

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

Cách tải: đăng nhập Sketchfab → mở link → **Download 3D Model** → chọn **glTF** (nếu không có thì FBX) → lưu vào `ArtSource/Chapter01/Sources3D/Sketchfab/SFxx/` (xx = mã bên dưới, giải nén giữ nguyên). Claude sẽ kiểm mesh, texture, pivot, số mặt rồi mới chuẩn hoá.

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
| SF12 | [Low Poly Watchtower (Isometric)](https://sketchfab.com/3d-models/low-poly-watchtower-isometric-239b874ebfb1474a8de0e0adb7c681e1) | Pippa | 550 | Ải 2, 3, 5, 6: tháp canh gỗ |
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
| SF08 | [Chinese Pavilion Memoriam](https://sketchfab.com/3d-models/chinese-pavilion-memoriam-87f4931714d7481baa54e5050f679be2) | HaraKiri | 14.716 | Ải 7 Tế Đàn: đình nghi lễ |
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

## 6. Xung đột cần người dùng chốt

- `docs/QuanDich/` đang xếp quân Khăn Vàng ở **Chương 2** (Chương 1 = Sơn Tặc), còn `docs/Ai/Chuong1.md` lấy **Chương 1 = Loạn Khăn Vàng**.

## 7. Bước kế (khi người dùng duyệt danh sách)

Kiểm từng file (mesh, normal, UV, texture thiếu, pivot, số tam giác) → chỉnh / ghép trong Blender (mái cong, đổi màu theo bảng màu §2) → import URP, prefab, collider đơn giản, LOD → scene mẫu so màu (nhà + cây + đá + đất + nắng) → dựng khối 7 khu vực → ảnh tham chiếu cho Codex. Mọi asset phải ghi rõ chất lượng ở 3 cấu hình 30 / 60 / 120 FPS (quy tắc 28/10).
