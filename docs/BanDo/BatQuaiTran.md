# Arena3Q – Map "Bát Quái Trận" (BatTran)

Phiên bản: v0.1 · 2026-10-18 · Trạng thái: **luật đã chốt ✅** (người dùng chốt 2026-10-18); các điểm luật chưa nói rõ được ghi là 🟡 (mặc định tạm, là tuỳ chọn cấu hình).

Ký hiệu: ✅ đã chốt · 🟡 mặc định tạm · ⏳ để sau · ⚠️ mâu thuẫn chờ chốt.

- **Dùng cho:** ✅ mọi ải hiện tại (Công, Thủ, Hướng dẫn, 2 ải boss) – thay `map1.png` và bố cục sông + 2 cầu. Sau này chỉnh riêng từng ải bằng admin panel, nên map / bố cục của mỗi ải nằm trong dữ liệu ải (`StageConfig`), không viết cứng.
- **Nguồn dữ liệu:** `Assets/Arena3Q/Art/Maps/batQuaiTran/BatTran_Terrain/BatTran_Layout.json` (toạ độ thành, vòng trận, 8 cửa, 8 cụm cột, làn). Hình vật thể render từ model 3D thành sprite (`docs/BanDo/MapArtGuide.md` mục 5b) – game vẫn 2D.
- **Không làm hệ thống song song:** vật cản dùng `IObstacle` + `BattlefieldLayout` (tìm đường), hiệu ứng dùng `StatusSet` (`MoveSpeedUp`, `Slow`) và `UnitBase.Heal` / sát thương, số liệu trong ScriptableObject.

---

## 0. Kích thước và bố cục chung

| Thông số | Giá trị | Ghi chú |
|---|---|---|
| Biên bản đồ | ✅ cỡ M, `mapBounds` (−24, −13, 48, 26) | = `MapSpecification.md` §3 |
| Thành | (−19.5, 0) và (19.5, 0) | từ marker `Base_A` / `Base_B` (cũ: ±19.4, đo từ `map1`); bãi cát doanh trại bán kính ~4 vẽ sẵn trong ảnh nền |
| Ảnh nền | `BatTran_Painted.png` (2048 × 1152) phủ vùng (−27, −15.19, 54, 30.38) | vẽ nhìn thẳng từ trên xuống; mặt đất phẳng nên nén dọc 0.75 khi hiển thị là đúng góc 3/4; phần ngoài `mapBounds` là gò / vách rìa |
| Góc nhìn | 0.75 (≈ 41.4°) | theo sprite vật thể (render ở 0.75) |
| Vùng triệu hồi, điểm triệu hồi nhanh, quân phòng thủ, điểm sinh quân | giữ như các ải hiện tại | marker `Spawn_A/B_*` trong JSON là kiểu map PvP, không dùng |
| Làn | giữa 39 đơn vị (~15 s ở tốc độ 2.6) qua cửa 0 & 4 + đài; trên / dưới ~44.7 (~17 s) vòng ngoài vòng cột | làn chỉ là đường mòn vẽ trên nền – unit đi tự do, chỉ cột / phiến đá chặn |
| Sông | không có | hệ thống sông vẫn giữ trong code cho map khác |

## 1. Vòng cột ✅

- Tâm (0, 0). **8 cụm cột** trên bán kính **R = 5.7**, góc **22.5° + k·45°** (k = 0..7). Mỗi cột là **vật cản tròn bán kính 1.0**, không bao giờ biến mất.
- **8 cửa** ở góc **k·45°**: `Gate_0` hướng Đông (về `Base_B`, phe địch), `Gate_4` hướng Tây (về `Base_A`, phe ta); `Gate_2` hướng Bắc. Vị trí / hướng lấy từ marker `Pillar_k`, `Gate_k` trong `BatTran_Layout.json`.
- Khe giữa hai cột liền kề: tâm cách nhau 2·5.7·sin 22.5° = 4.36 → **mép cách nhau ~2.36** (hẹp hơn lối đi ≥ 4 của `MapSpecification.md` §6.1 – chấp nhận, cửa là điểm nghẽn như cầu).
- Mỗi cửa có **1 vùng kích hoạt (trigger) hình chữ nhật 2.4 (ngang khe) × 1.0 (sâu)**, tâm tại marker `Gate_k`, cạnh dài theo hướng ngang khe.
  - 🟡 Unit "đi qua cửa" = **tâm unit đi vào** vùng kích hoạt (lần vào; đứng yên trong vùng không kích lại).

## 2. Loại cửa (Bát Môn) ✅ – cấu hình bằng ScriptableObject

| Loại | Màu rune | Hiệu ứng khi unit đi qua vùng kích hoạt |
|---|---|---|
| **Sinh** | xanh lá | hồi **8 % máu tối đa** (mỗi unit **hồi chiêu 5 s**) |
| **Khai** | vàng | **+30 % tốc chạy trong 4 s** (`StatusKind.MoveSpeedUp`) |
| **Tử** | đỏ | mất **10 % máu tối đa** + **chậm 30 % trong 2 s** (`StatusKind.Slow`) |
| **Đỗ** | xám | **CỬA ĐÓNG**: phiến đá trồi lên thành vật cản chắn kín khe |

- **Hai cửa đối diện (k và k+4) luôn cùng loại** → hai phe luôn đối xứng (đối xứng tâm).
- **Khởi đầu:** cửa 0 & 4 = **Khai** (làn giữa mở), 1 & 5 = **Sinh**, 2 & 6 = **Tử**, 3 & 7 = **Đỗ**.
- Các điểm chưa nói rõ (🟡, đều là trường cấu hình):
  1. Hồi chiêu mỗi unit của **Khai** và **Tử**: **5 s** như Sinh (tránh đứng ra vào khe để kích liên tục).
  2. Máu mất ở **Tử** là **sát thương chuẩn theo % máu tối đa**: bỏ qua giảm sát thương, khiên vẫn đỡ; có thể làm chết.
  3. Chậm của **Tử** là khống chế: đơn vị **miễn khống chế** không bị chậm (luật `StatusSet` có sẵn) nhưng vẫn mất máu.
  4. Áp dụng cho **mọi unit đi bộ và bay** của cả hai phe (tướng, lính, quân địch, boss); **thành không bị**.
  5. Không cộng dồn: hai lần Khai liền nhau chỉ làm mới thời gian (luật `StatusSet`: giá trị lớn nhất, thời gian dài nhất thắng).
  6. AI **không né** cửa Tử: unit vẫn đi đường ngắn nhất (⏳ có thể thêm "né cửa xấu" sau).

## 3. Xoay trận ✅

- **Mỗi 20 s**, loại cửa dịch 1 bước (xoay 45°).
  - ✅ (chốt 2026-10-18) Chiều mặc định **thuận chiều kim đồng hồ**: cửa đánh số ngược chiều kim đồng hồ (0 = Đông, 2 = Bắc, 4 = Tây), nên **cửa k nhận loại của cửa k+1** (câu "k nhận loại của k−1" trong bản luật đầu là chiều ngược lại, đã bỏ). Chiều xoay là trường cấu hình. Ví dụ lần xoay đầu: 0 & 4 = Sinh, 1 & 5 = Tử, 2 & 6 = Đỗ, 3 & 7 = Khai.
- **3 s trước khi xoay**: rune của các cửa sắp đổi (mọi cửa) **nhấp nháy** + **cảnh báo trên HUD**. 🟡 Âm thanh: project chưa có hệ thống âm thanh → để móc sự kiện sẵn, ⏳ gắn tiếng sau.
- **Cửa chuyển sang Đỗ:** phiến đá trồi từ dưới đất lên trong **0.6 s** (`y = −1.9 → 0`), **vật cản bật NGAY đầu animation**. Unit đang đứng trong khe bị **đẩy ra phía gần nhất**, không được kẹt.
- **Cửa thôi là Đỗ:** phiến hạ xuống trong **0.6 s**, **vật cản tắt ở CUỐI animation**.
- Đóng / mở cửa **cập nhật đường đi ngay**: unit đang đi qua cửa vừa đóng tự tìm cửa mở gần nhất hoặc đi làn vòng (đồ thị tìm đường dựng lại khi một phiến bật / tắt).
- 🟡 Phiến đá chắn cả **đơn vị bay** (như cột – vật cản khối; luật bay hiện tại chỉ bỏ qua sông).

## 4. Đài tâm – vùng chiếm, KHÔNG phải vật cản ✅

- Hình tròn **bán kính 2.0** tại (0, 0); unit đi lên được.
- **Chỉ một phe** có unit trong vùng **liên tục 5 s** → phe đó **"giữ trận nhãn"**. Có **cả hai phe** → **tạm dừng đếm**. **Không ai** → giữ nguyên chủ cũ.
  - 🟡 "Trong vùng" = tâm unit cách (0, 0) ≤ 2.0; mọi unit sống trừ thành.
  - 🟡 Tiến độ chiếm thuộc phe đang đếm: phe đó rời đi (không còn ai) → tiến độ về 0; phe kia vào một mình → đếm lại từ 0 cho phe kia. Phe đang giữ trận nhãn đứng một mình → không cần đếm.
- **Phe giữ trận nhãn ở thời điểm xoay**: trong **chu kỳ 20 s tiếp theo**, unit phe đó **không bị hiệu ứng cửa Tử** (cả máu lẫn chậm) và nhận hiệu ứng **Sinh / Khai mạnh hơn 50 %** (hồi 12 %, +45 % tốc chạy). Chu kỳ đầu trận chưa phe nào được.
- **Vòng sáng quanh đài đổi màu theo phe đang giữ** (ta xanh dương, địch đỏ, chưa ai trắng); có **thanh tiến độ** khi đang chiếm.

## 5. Vật trang trí ✅

- Cây, đá, bụi, đồ doanh trại trong vùng chơi: **chỉ để nhìn, không chặn đường**.
- Vách đá / gò ở rìa nằm **ngoài** `mapBounds`: chặn (biên bản đồ đã chặn), unit vốn không ra đó.
- Lều, rào, tháp canh trong doanh trại: **không chặn**, trừ khi có lý do gameplay sau này.
- Vị trí đặt: 🟡 đề xuất đầu (2026-10-18, chờ người dùng duyệt), lưu trong `Content/Maps/MapDecorations_BatTran.asset` (`MapDecorConfig`, gắn vào `StageConfig.decorations`; builder chỉ tạo khi chưa có, sửa trong asset là giữ). Doanh trại phe ta (phe địch đối xứng qua x = 0):

| Vật | Sprite | Vị trí (phe ta) |
|---|---|---|
| Thành | `ThreeKingdomsCommand_1` (ta) / `ThreeKingdomsCommand_2` – bản cờ rách, gỗ cháy (địch) | (∓19.5, 0), qua `CastleConfig.sprite` |
| 2 lều | `ThreeKingdomsSquare` | (−22, ±7.5) |
| 2 chòi canh | `WoodenCampWatchtower` | (−17, ±10) |
| 4 đoạn rào | `WoodenPalisadeFence` | (−22.3 / −19.3, ±11.8) |
| 2 cột cờ trước thành | `MilitaryFlagpoleBanner` | (−17.8, ±2.6) |
| 4 cột cờ đánh dấu làn trên / dưới | `MilitaryFlagpoleBanner` | (±7.5, ±10.5) |

  Tránh vùng triệu hồi (x −17.5…−11, y −9…9), điểm triệu hồi nhanh, làn ngoài (y ≈ ±8) và chỗ sinh quân ải Thủ (x 21–22.5, y −6…6). ⏳ Cây / đá / bụi: chưa có sprite (3 file `.blend` cây chưa render được vì máy không có Blender).

## 6. Hình ảnh (placeholder nếu chưa có asset) ✅

Luật gốc nêu prefab 3D; trong game 2D chúng là sprite render từ model (`PropSprites.asset`):

| Luật gốc | Trong game 2D |
|---|---|
| Cột `BatTran_PillarCluster_01`, đế rộng 2.0, xoay Y ngẫu nhiên | sprite `BaguaStonePillar_*` (đế 2.0, 8 góc xoay render sẵn); mỗi cột chọn ngẫu nhiên một góc (cố định theo ải để mỗi lần chơi giống nhau) |
| Phiến cửa `BatTran_SlabGate` (2.5 × 1.8 × 0.4), trục X local = ngang khe | sprite `BaguaWideStoneSlab_*` dài 2.5, render 8 góc theo hướng từng cửa; vật cản logic = thanh dài 2.5, dày 0.4. Trồi / hạ bằng mặt nạ (SpriteMask) ở mặt đất. 🟡 model hiện cao ~1.4 và dày ~1.05 – thay model sau nếu cần đúng 1.8 × 0.4 |
| Rune: decal tròn dưới mỗi cửa, màu theo loại cửa | vòng tròn phẳng trên mặt đất, tô màu theo loại cửa, nhấp nháy khi sắp xoay |
| Đài `BatTran_Altar` (Ø4.0, cao 0.4) | sprite `BaguaRoundStoneAltar` (Ø4.0; model cao ~0.6) + vòng sáng theo phe + thanh tiến độ |

## 7. Debug và kiểm thử ✅

- **Gizmo** (cửa sổ Scene): cột, khe, vùng kích hoạt, loại cửa (màu), vùng đài.
- **Phím dev** (chỉ Editor / bản Development): 🟡 `F5` xoay ngay · `F6` đổi chủ trận nhãn (không ai → ta → địch) · `Shift + 1..8` bật / tắt cửa 0..7 (đóng / mở cưỡng bức). Cùng chức năng có trong API `BattleController` cho test.
- **Test:**
  - (a) cửa Đỗ chặn hoàn toàn, không lọt unit;
  - (b) unit đổi đường khi cửa đóng giữa chừng;
  - (c) không unit nào kẹt trong khe khi phiến trồi;
  - (d) hai phe luôn gặp cùng loại cửa (k và k+4 cùng loại sau mọi lần xoay);
  - (e) làn giữa ~39 đơn vị khi cửa 0 & 4 mở.

## 8. Ảnh hưởng tới tài liệu khác

- `GameplaySpecification.md` §9: thêm vật cản **tròn** (cột) và **phiến đá bật / tắt** (cửa Đỗ) bên cạnh hình chữ nhật và sông; bản đồ chuẩn của mọi ải là Bát Quái Trận (thay "sông + 2 cầu"). Câu "✅ bãi trống, không cần tìm đường" đã được thay bằng luật vùng cấm 🟡 từ 2026-10-16; vòng cột dùng đúng cơ chế đó.
- `MapSpecification.md` §4–§6 (bố cục sông chuẩn) giữ làm tham khảo cho map có sông; bố cục hiện hành là tài liệu này.
