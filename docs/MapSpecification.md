# Arena3Q – Thông số bản đồ (đề xuất)

Phiên bản: v0.4 · 2026-10-17 · Trạng thái: **phần lớn đã chốt ✅** (cỡ M, PPU 128, góc 3/4 vẽ sẵn, Tilemap + SpriteShape + PSD, 2 làn, kiểm tra tự động, vẽ tay); còn 🟡: tỉ lệ góc 3/4, Sorting Layer, các con số bố cục

Tài liệu này đề xuất thông số để vẽ bản đồ cho ải Công và ải Thủ. Luật gameplay vẫn theo `docs/GameplaySpecification.md` (mục 8, 9); ở đây chỉ chốt **kích thước, toạ độ, bố cục, quy cách art**. Mọi con số dưới đây suy ra từ prototype đang chạy (kích thước unit, tầm đánh, tốc độ, camera, HUD) – mục 1 ghi lại các số gốc đó để khi đổi một số thì biết phải tính lại số nào.

Ký hiệu: 🟡 đề xuất (chờ người dùng duyệt, sau khi duyệt đổi thành ✅) · ⏳ để sau.

---

## 1. Số gốc từ prototype (không phải đề xuất, chỉ để tham chiếu)

| Đại lượng | Giá trị hiện tại | Nguồn |
|---|---|---|
| Đơn vị | 1 unit Unity = 1 đơn vị logic | toàn bộ code |
| Bản đồ hiện tại | 48 × 26 (x −24…24, y −13…13) | `Stage_*_01.asset` |
| Bán kính thân | tướng 0.45 · lính 0.30 · quân địch 0.32 · thành 1.40 | `*Unit.BodyRadius` |
| Đội hình | 1 tướng + 4 lính, vòng bán kính 1.05 → chiếm ~3 × 3 | `Squad.SlotSpacing` |
| Tầm đánh | cận chiến 0.6–0.7 · cung 4.5 (tính từ mép thân) | asset tướng/lính |
| Tầm phát hiện (aggro) | 4.5–7 | asset |
| Tốc độ | 1.6 (Lực sĩ) … 2.6 (đa số) … 3.6 (Kỵ binh) đơn vị/giây | asset |
| Camera | orthographic, khởi đầu 7, zoom 3.5–11 (bị kẹp bởi bản đồ) | `BattleCameraController` |
| Vùng nhìn thấy ở zoom 7 | cao 14; rộng 24.9 (màn 16:9) – 31.1 (màn 20:9) | tính |
| HUD (tham chiếu 1920×1080) | thanh thẻ dưới ~226 px ≈ 21 % chiều cao ≈ **2.9 đơn vị** ở zoom 7; dải trên (tên ải, đồng hồ, thanh góc nhìn) ~150 px ≈ **1.9 đơn vị** | `BattleHud` |
| Góc nghiêng 2.5D | hiện 0–45° chỉnh bằng thanh trượt, mặc định 0°; **sẽ khoá ở góc của art (tỉ lệ 0.75 ≈ 41.4°, mục 8.3)** khi có art thật | `BattleController.ViewTiltDegrees` |
| Thời gian đi thành-tới-thành | 39 đơn vị ÷ 2.6 ≈ **15 giây** | tính |

---

## 2. Hệ toạ độ và lưới 🟡

- Gốc (0, 0) ở **tâm bản đồ**. Trục **x**: phe ta bên **trái** (x âm) → phe địch bên **phải** (x dương). Trục **y**: lên trên.
- **Lưới 1 × 1 đơn vị**. Mọi vật thể gameplay (thành, điểm triệu hồi, vùng cấm, vị trí quân phòng thủ, điểm sinh quân) đặt trên **bước 0.5** để dễ căn với tile.
- Bản đồ là **hình chữ nhật**, kích thước **số nguyên chẵn** để tâm rơi đúng giữa ô.

## 3. Kích thước bản đồ ✅ (người dùng chốt 2026-10-16: dùng cỡ M)

✅ **Mọi ải dùng cỡ M: 48 × 26, `mapBounds` = (−24, −13, 48, 26).** Cỡ S và L chỉ giữ lại để tham khảo, ⏳ chưa dùng (cân nhắc lại nếu sau này cần ải hướng dẫn nhỏ hoặc ải nhiều làn).

| Cỡ | Rộng × Cao | Bounds | Dùng cho | Thời gian đi thành-tới-thành* |
|---|---|---|---|---|
| S ⏳ | 40 × 22 | (−20, −11, 40, 22) | ải đầu/hướng dẫn | ~12 s |
| **M ✅** | **48 × 26** | **(−24, −13, 48, 26)** | **mọi ải** (= hiện tại) | ~15 s |
| L ⏳ | 60 × 32 | (−30, −16, 60, 32) | ải lớn, nhiều làn | ~20 s |

\* tốc độ 2.6, thành cách mép 4–5 đơn vị.

Lý do chọn M làm chuẩn:
- **Rộng ≈ 2× vùng nhìn** ở zoom mặc định (24.9 trên màn 16:9) → spec ✅ "bản đồ rộng hơn màn hình", phải kéo camera / theo tướng.
- **Cao 26** > vùng nhìn 14 ở zoom 7 nhưng ≤ vùng nhìn khi zoom xa nhất (camera kẹp ở 13) → zoom ra là thấy trọn chiều cao.
- Thời gian hành quân 15 s trên `timeLimit` 150–180 s → đủ 3–4 lượt giao tranh, không quá lê thê.
- Ở góc nghiêng 45° bản đồ M chỉ còn cao 26 × 0.71 ≈ 18.4 trên màn hình → vẫn vừa khi zoom ra.

## 4. Bố cục chuẩn – ải Công (cỡ M) 🟡

```
 y=+13 ┌──────────────────────────────────────────────────────────────┐
       │ lề 2 (dưới dải HUD trên)                                      │
       │ ┌──────────┐                                    ┌─────────┐  │
       │ │ VÙNG     │          GIỮA BẢN ĐỒ               │ VÙNG    │  │
       │ │ TRIỆU    │   SÔNG uốn khúc cắt ngang x≈0      │ PHÒNG   │  │
       │ │ HỒI      │   cầu Bắc (0.67, 6.5), cầu Nam     │ THỦ     │  │
   y=0 │ [T]  ◎ ◎ │   (−0.67, −6.5) → 2 làn đường      │   [Đ]   │  │
       │ │   ◎ ◎ ◎  │                                    │ ≤ 8 quanh│  │
       │ │ x −18…−11│                                    │ thành   │  │
       │ └──────────┘                                    └─────────┘  │
       │ lề 3 (trên thanh thẻ)                                        │
 y=−13 └──────────────────────────────────────────────────────────────┘
      x=−24       −11                                   11          +24
   [T] thành ta (−20, 0)   [Đ] thành địch (19, 0)   ◎ điểm triệu hồi nhanh
```

| Khu | Thông số đề xuất | Hiện tại trong `Stage_Attack_01` |
|---|---|---|
| **Căn cứ ta** | x −24…−11 (rộng 13) | khớp |
| Thành ta | tâm (−20, 0); mép thành cách mép bản đồ ≥ 2.5 | (−20, 0) |
| Vùng triệu hồi (`summonZone`) | x −18…−11, y −9…9 (7 × 18 = 126 đơn vị²; đủ cho 5 đội × ~9 đơn vị² cộng khoảng trống); cách mép thành ≥ 0.5 | (−18, −9, 7, 18) |
| Điểm triệu hồi nhanh | 5 điểm, cách nhau ≥ 3 (một đội hình rộng ~3): (−15.5, 0), (−15.5, ±3.5), (−13, ±1.75) | khớp |
| **Giữa bản đồ** | x −11…11 (rộng 22) – nơi giao tranh chính | khớp |
| Vùng cấm | **1 con sông** cắt ngang từ mép trên xuống mép dưới, rộng 2.5, có **2 cầu** rộng 4 (mục 6.2); thêm 0–1 vùng chữ nhật nếu cần | sông "Sông" theo đúng thông số này |
| **Căn cứ địch** | x 11…24 (rộng 13) | khớp |
| Thành địch | tâm (19, 0) | (19, 0) |
| Quân phòng thủ | mọi vị trí trong bán kính **8** quanh thành địch (✅ spec §8, có test `ShippedAttackStages_DefendersStayAroundTheEnemyCastle`); `guardRadius` 4.5 → vùng "lao ra đánh" phủ x ~7…24 | 18 quân, khớp |

## 5. Bố cục chuẩn – ải Thủ (cỡ M) 🟡

- Căn cứ ta, thành ta, vùng triệu hồi, điểm triệu hồi nhanh: **giống ải Công** (người chơi quen một bố cục).
- **Không có thành địch** trên bản đồ (prototype hiện tại); khu x 11…24 là **khu sinh quân**.
- **Điểm sinh quân (`waves[].spawns[].spawnPoint`)**: x 21–22.5, y trải −6…6, cách mép bản đồ ≥ 1.5 để cả cụm quân (vòng xếp 0.8) không bị kẹp vào mép.
- Đường quân địch đi: về thành ta **qua một trong hai cầu**, ~43–45 đơn vị ÷ 2.2 ≈ **20 giây** → đợt đầu nên đến ở giây ~5 để người chơi có ~20 s triệu hồi và bố trí.
- Thành ta bắn (🟡 spec §8): tầm 6 (tính từ mép thành tới mép quân) → bắn tới tâm quân địch cách tâm thành ~7.7, tức x tới ~−12.3 phủ gần hết vùng triệu hồi: quân địch lọt vào căn cứ là bị thành bắn.

## 6. Quy tắc vùng cấm 🟡

Vùng cấm **chặn di chuyển**: unit đi theo đường ngắn nhất vòng qua các vùng cấm (`BattlefieldLayout.NextWaypoint`, đồ thị điểm nút dựng sẵn quanh vùng cấm), không phải chỉ là chỗ không được ra lệnh tới. Có **hai loại** (người dùng chốt 2026-10-16); loại khác (hồ tròn, vực đa giác…) → ⏳.

### 6.1 Quy tắc chung
1. **Không được cắt đứt** đường từ căn cứ ta tới thành địch / từ điểm sinh quân tới thành ta – `StageConfig.Validate` đã kiểm tra bằng cách tìm đường thật (`HasPath`) từ mọi điểm triệu hồi nhanh (ải Công) và mọi điểm sinh quân (ải Thủ).
2. **Lối đi rộng ≥ 4 đơn vị** (một đội hình rộng ~3 + lề): giữa hai vùng cấm, giữa vùng cấm và mép bản đồ, và bề rộng cầu.
3. Cách **≥ 3 đơn vị** so với: thành, vùng triệu hồi, điểm triệu hồi nhanh, vị trí quân phòng thủ, điểm sinh quân.
4. Hình vẽ phải **trùng hình logic**: unit đi cách mép ~0.1 + bán kính thân; viền art lấn vào trong ≤ 0.2 là đẹp, lấn ra ngoài sẽ làm unit trông như lội qua vật cản.

### 6.2 Sông (`StageConfig.rivers`)
| Thông số | Đề xuất | Ghi chú |
|---|---|---|
| `path` (đường tâm) | 5–9 điểm, mỗi đoạn dài 4–6, góc lệch giữa hai đoạn liền kề ≤ 45° | uốn khúc mềm; **kéo dài quá mép bản đồ ≥ 1** nếu sông cắt ngang hết bản đồ |
| `width` | **2.5** (tối thiểu 1, `Validate` kiểm tra) | rộng hơn 3 thì hai bờ cách nhau quá xa, cầu trông ngắn |
| `crossings[].position` | điểm nằm **trên lòng sông** (`Validate` báo lỗi nếu lệch ra ngoài), nên đặt trên **đoạn thẳng**, cách khúc quanh ≥ 2 | đặt trên khúc quanh vẫn chạy được nhưng lối qua bị hẹp lại ở phía trong khúc quanh |
| `crossings[].width` | **4** (tối thiểu 2) | là **bề rộng đi lại thật**; logic cắt lòng sông một đoạn dài `width cầu + width sông` để bờ nước không lấn vào lối qua |
| Số cầu | ✅ **2** cho sông cắt ngang bản đồ cỡ M (2 làn, người dùng chốt 2026-10-17), cách nhau ≥ 10 theo trục y | 1 cầu → điểm nghẽn quá chặt; 3 cầu → sông gần như không còn tác dụng |
| Bố cục chuẩn cỡ M | path (1, 14) (−1, 9.5) (1.5, 5) (0.5, 0) (−1.5, −5) (1, −9.5) (−0.5, −14); cầu (0.67, 6.5) và (−0.67, −6.5) | = `PrototypeContentBuilder.StandardRiverPath` / `StandardBridges` |

Art: nước vẽ liền theo cả đường tâm (kể cả dưới cầu), cầu vẽ đè lên, hướng cầu vuông góc dòng chảy. Placeholder: sông xanh nước, cầu nâu.

### 6.3 Hình chữ nhật (`StageConfig.forbiddenZones`)
1. Hình chữ nhật thẳng trục; tối thiểu **2 × 2**, tối đa khuyến nghị **8 × 10** ở cỡ M.
2. **Không chạm cả mép trên lẫn mép dưới**; chạm **một** mép thì được – unit vòng phía còn lại.
3. Dùng cho vật cản khối (hồ vuông, khu nhà đổ, vách đá).

## 7. Lề an toàn quanh HUD 🟡

Camera không cuộn ra ngoài bản đồ, nên vật thể sát mép dưới/trên có thể **nằm dưới HUD** khi camera đã chạm biên.

| Dải | Bề rộng đề xuất | Không đặt ở đây |
|---|---|---|
| Mép trái/phải/trên/dưới | 1 đơn vị | bất cứ vật thể gameplay nào (unit bị kẹp theo bán kính) |
| Mép **dưới** | **4** đơn vị logic (thanh thẻ ≈ 2.9 đơn vị màn hình ở zoom 7 ÷ 0.75 nén trục dọc ≈ 3.9) | điểm triệu hồi nhanh, thành, điểm sinh quân, quân phòng thủ quan trọng |
| Mép **trên** | **2.5** đơn vị logic (≈ 1.9 ÷ 0.75) | như trên |

Số đo theo **toạ độ logic** (toạ độ trong `StageConfig`); vì góc 3/4 nén trục dọc còn 0.75 (mục 8.3), một dải HUD trên màn hình che nhiều đơn vị logic hơn khi nhìn thẳng. Bản đồ M hiện tại: vùng triệu hồi y −9…9 → cách mép dưới 4, mép trên 4 ✅.

## 8. Quy cách art (người dùng chốt 2026-10-17)

### 8.1 Phong cách và phần mềm ✅
- ✅ **Vẽ tay, màu mềm** (kiểu Clash of Clans). Phần mềm: **Krita** (miễn phí) hoặc **Photoshop** / **Procreate**; lưu **PSD nhiều lớp** – Unity đọc thẳng bằng PSD Importer (đã có trong project). Không dùng pixel art (không cần Aseprite).
- Import vào Unity: lọc **Bilinear**, nén theo nền tảng (ASTC trên Android/iOS), bật mipmap cho mặt đất.

### 8.2 Độ phân giải ✅
- ✅ **PPU (Pixels Per Unit) = 128** cho mọi art thật. (Placeholder hiện dùng 64, sinh bằng code, không ảnh hưởng.)
- Lý do: ở zoom gần nhất (3.5) mỗi đơn vị chiếm 1080 ÷ 7 ≈ 154 px trên màn 1080p, ở zoom mặc định ≈ 77 px → 128 nét ở zoom mặc định, chỉ phóng nhẹ ×1.2 khi zoom gần nhất.

### 8.3 Góc nhìn: nghiêng 3/4 cố định, art vẽ sẵn ✅ (tỉ lệ 🟡)
- ✅ **Không phải isometric hình thoi**: trục bản đồ giữ nguyên như hiện tại (ta bên trái, địch bên phải, x nằm ngang). Mặt đất **vẽ sẵn ở góc nghiêng 3/4 cố định**: chiều dọc bản đồ trên màn hình bị nén lại so với chiều ngang.
- 🟡 **Tỉ lệ nén trục dọc = 0.75** (tương đương nghiêng ≈ 41.4°): một ô logic 1 × 1 vẽ thành **128 × 96 px** (số nguyên, dễ vẽ; 45° đúng sẽ là 128 × 90.5 px). Cả bản đồ M trên màn hình: 48 × 19.5 đơn vị ≈ **6144 × 2496 px** (chỉ để tham khảo – không vẽ thành một tấm, xem 8.4).
- ✅ **Thanh "Góc nhìn" giữ lại để debug**: khi có art thật, góc mặc định của game = góc của art (0.75 ≈ 41.4°); thanh trượt **chỉ hiện trong Editor / bản Development**, bản phát hành ẩn đi và khoá góc. *(Thay đổi code này chưa làm – sẽ làm cùng khung art.)*
- Vì art đã vẽ sẵn ở góc nghiêng, **không vẽ phối cảnh khác** cho từng vật (mọi thứ dùng chung một góc nhìn).

### 8.4 Cách lắp bản đồ ✅ (theo đề xuất)
- ✅ **Mặt đất: Unity Tilemap**, ô logic 1 × 1 = **ảnh 128 × 96 px** (cell size Tilemap 1 × 0.75 trong không gian hiển thị). Bản đồ M = 48 × 26 ô. Vẽ một **bộ ô lặp lại** (cỏ, đất, đường mòn, chuyển tiếp) khoảng 20–40 ô; dùng Rule Tile (Tilemap Extras) cho viền chuyển tiếp tự động.
- ✅ **Sông: SpriteShape** bám theo đường tâm `rivers[].path` (đã chiếu theo góc 3/4), nên hình luôn khớp logic. Cần vẽ: một **dải nước** nối liền được, **dải bờ** hai bên, ảnh **cây cầu** (đặt vuông góc dòng chảy, rộng 4 đơn vị theo dòng chảy).
- ✅ **Vật thể đứng: sprite riêng từ PSD**, pivot ở chân (bottom-center), sắp lớp theo Y (thấp hơn trên màn hình = vẽ đè lên).
- Lớp tilemap: `Ground` (cỏ/đất), `GroundDetail` (đường mòn, vết, hoa – chỉ trang trí), `Obstacles` (nước, vách – trùng vùng cấm).
- Tilemap / SpriteShape chỉ là **hình**; dữ liệu gameplay (biên, vùng cấm, vùng triệu hồi…) vẫn nằm trong `StageConfig`. Mục 10 kiểm tra hình ↔ dữ liệu.

### 8.5 Kích thước vật thể đứng (rộng × cao trên màn hình, đơn vị; × 128 = px)
| Vật thể | Kích thước | Ghi chú |
|---|---|---|
| Thành | **3.5 × 4.5** (448 × 576 px) | thân logic tròn bán kính 1.4 |
| Tướng | **1.2 × 1.6** (154 × 205 px) | thân logic bán kính 0.45 |
| Lính / quân địch | **0.8 × 1.1** (102 × 141 px) | thân logic bán kính 0.30–0.32 |
| Cây trang trí | 1.5 × 2.5 | không chặn đường |
- Vật thể **chỉ để trang trí** đặt ngoài lối đi chính hoặc ở mép bản đồ; chúng **không chặn đường** (chỉ vùng cấm chặn).

### 8.6 Thứ tự lớp (Sorting Layer) đề xuất 🟡
`Ground` → `GroundDetail` → `Obstacles` (nước) → `Bridges` → `GroundUI` (vùng triệu hồi, điểm triệu hồi, marker lệnh, vòng chọn) → `Units` (sắp theo Y) → `Effects` → (uGUI HUD luôn trên cùng).

## 9. Bảng thông số tổng hợp cho một ải mới (copy khi tạo ải) 🟡

| Trường `StageConfig` | Cỡ M – ải Công | Cỡ M – ải Thủ |
|---|---|---|
| `mapBounds` | (−24, −13, 48, 26) | (−24, −13, 48, 26) |
| `playerCastle.position` | (−20, 0) | (−20, 0) |
| `enemyCastle.position` | (19, 0) | (19, 0) – không sinh ra |
| `summonZone` | (−18, −9, 7, 18) | (−18, −9, 7, 18) |
| `quickSummonPoints` | (−15.5, 0), (−15.5, ±3.5), (−13, ±1.75) | như ải Công |
| `rivers` | 1 sông chuẩn (mục 6.2) | 1 sông chuẩn (mục 6.2) |
| `forbiddenZones` | 0–1 vùng chữ nhật, theo mục 6.3 | 0–1 vùng chữ nhật, theo mục 6.3 |
| `defenders` | trong bán kính 8 quanh (19, 0), `guardRadius` 4.5 | – |
| `waves[].spawns[].spawnPoint` | – | x 21–22.5, y −6…6 |
| `timeLimit` | 180 | 150 |

## 10. Kiểm tra tự động khi nộp bản đồ ✅ (người dùng chốt làm; code chưa viết)

Các luật dưới đây sẽ được thêm vào `StageConfig.Validate` / test nội dung (✅ quyết định 2026-10-17). `[x]` = đã có, `[ ]` = sẽ làm cùng khung art:
- [ ] Kích thước bản đồ đúng cỡ M (48 × 26, bounds (−24, −13, 48, 26)).
- [ ] Vật thể gameplay nằm trên bước 0.5.
- [ ] Không vật thể gameplay quan trọng nào trong lề HUD (mục 7).
- [x] Không vùng cấm nào cắt đứt đường đi (đã có trong `Validate`: `HasPath` từ điểm triệu hồi nhanh / điểm sinh quân). Sông: đủ điểm, bề rộng ≥ 1, cầu rộng ≥ 2 và nằm trên lòng sông (đã có).
- [ ] Vùng cấm theo mục 6 (kích thước, lối đi ≥ 4, cách ≥ 3; sông: số cầu, cầu cách khúc quanh ≥ 2).
- [ ] Tilemap `Obstacles` (nước, cầu, vật cản) phủ đúng hình logic của vùng cấm (sai lệch ≤ 0.2).

## 11. Câu hỏi cần bạn chốt

1. ~~Giữ 3 cỡ S/M/L, hay chỉ dùng một cỡ M?~~ → **Đã chốt: chỉ dùng cỡ M** (mục 3).
2. ~~PPU 128?~~ → **Đã chốt: 128** (mục 8.2).
3. ~~Top-down nghiêng lúc chạy hay isometric vẽ sẵn?~~ → **Đã chốt: góc nghiêng 3/4 cố định, art vẽ sẵn** (không phải hình thoi); thanh "Góc nhìn" giữ để debug (mục 8.3).
4. ~~Tilemap hay một ảnh nền lớn?~~ → **Đã chốt: Tilemap + SpriteShape (sông) + sprite PSD** (mục 8.4).
5. ~~Số làn?~~ → **Đã chốt: 2 làn** (sông có 2 cầu, mục 6.2).
6. ~~Thêm luật kiểm tra tự động?~~ → **Đã chốt: có** (mục 10).
7. Phong cách → **Đã chốt: vẽ tay, màu mềm** (mục 8.1).

**Còn cần xác nhận:**
- 🟡 Tỉ lệ nén trục dọc của góc 3/4: **0.75** (ô 128 × 96 px, ≈ 41.4°) – hay muốn góc khác (ví dụ 0.7 ≈ 45.6°, 0.8 ≈ 36.9°)? Phải chốt **trước khi họa sĩ vẽ ô nền**.
- 🟡 Thứ tự Sorting Layer (mục 8.6).
