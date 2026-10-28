# Giao việc Codex – Chương 1 giai đoạn B: UI map, tương tác, sương mù, chuyển động, phần còn thiếu

Claude soạn ngày 28/10/2026 theo yêu cầu người dùng: *"Viết chi tiết để Codex làm tiếp, gồm cả hiệu ứng chuyển động nhẹ nhưng phải tinh tế; NPC tương tác có dấu ? với dân làng (làm nhiệm vụ, ví dụ đi chiếm, bảo vệ); ải đánh có dấu 2 thanh kiếm chữ X; UI map phải như ảnh mẫu, icon phải đồng bộ với game; làm thêm những phần còn thiếu."*

Nguồn bắt buộc đọc trước: `ArtSource/Chapter01/Docs/Chuong1.md` (§19 đến §31), `docs/QuyTac/ThietKeUI.md`, `docs/QuyTac/QuyTacLamViec.md` §3 (UI làm trên web trước, preset 30/60/120), `docs/QuyTac/Model3D_Rules.md` (VFX không pop, khói/gió không thành dải đều), `Documentation/Chapter01_Layout.md` (bố cục map Claude đã dựng).

Đầu vào đã có:

| Thứ | Đường dẫn |
|---|---|
| Scene map (Claude dựng, sinh từ dữ liệu) | `Assets/Arena3Q/Chapters/Chapter01_YellowTurban/Scenes/CH01_Exploration.unity` |
| Bố cục, tọa độ, trạng thái, marker | `Data/CH01_MapLayout.json` (sinh từ `ArtSource/Chapter01/3D/Map/ch01-layout.cjs`) |
| Ảnh map | `ArtSource/Chapter01/3D/Renders/index.html` mục "Map tổng thể" |
| Ảnh mẫu UI (của người dùng, không commit) | `ArtSource/Chapter01/Docs/Bản đồ Chương 1 Loạn Khăn Vàng.png` |
| Ảnh mẫu đánh số thành phần | `AgentComms/codex/inbox/tu-claude_20261028-1930_UI-map-mau-chu-thich.png` (số 1 đến 16 dùng ở mục 2) |

**Không sửa scene CH01_Exploration bằng tay**: nó được dựng lại bằng **Arena3Q > Chapter 01 > Build Exploration Map** và mọi sửa tay sẽ mất. Hệ thống của bạn đặt trong prefab `Assets/Arena3Q/Chapters/Chapter01_YellowTurban/Prefabs/CH01_MapSystems.prefab`; bộ dựng tự đặt prefab này vào scene mỗi lần dựng (đã thêm). Muốn đổi vị trí, trạng thái, marker thì sửa `ch01-layout.cjs` rồi chạy `node` và dựng lại, hoặc báo Claude.

---

## 1. Thứ tự làm

1. **P1 – Khung chạy được**: dữ liệu chương (mục 9), luật tiến độ trong Core + test, lưu tiến độ, camera map, chạm NPC từ xa + NavMesh, nút ải, bảng Xuất Chiến, chuyển sang trận và quay về. Không giả lập chiến thắng (§21).
2. **P2 – UI map theo ảnh mẫu** (mục 2) và dấu tương tác (mục 3), làm trên bản web trước.
3. **P3 – Sương mù và mở khóa** (mục 5), trạng thái thế giới (mục 6).
4. **P4 – Chuyển động tinh tế** (mục 7), 2D trên map (mục 8), điểm gắn âm thanh (mục 10).

Mỗi phần xong: chụp ảnh, báo `AgentComms/codex/outbox`, Claude review.

---

## 2. UI map: phải như ảnh mẫu

Dựng bằng uGUI từ code như HUD hiện có, dùng thành phần chung của sảnh: `GameButton`, `UIAudio`, `LobbyFrameGraphic`, thanh tài nguyên của sảnh, `UiFactory.AddIcon` / `UiFactory.AddStars`, font TMP của game. Không tạo một bộ nút hay khung thứ hai. Mọi chữ hiển thị theo `ThietKeUI.md` (không gạch dài, không emoji, không ký hiệu Unicode làm icon). Vị trí dưới đây tính theo phần trăm màn hình của ảnh mẫu (1672 × 941); đưa hết vào `MapUiConfig`, áp `SafeAreaFitter` / `UiSafeArea`.

| # | Thành phần | Vị trí, kích thước | Mô tả và hành vi |
|---|---|---|---|
| 1 | Nút quay lại | trái trên, x 1 đến 6 %, cao 6 % | khung gỗ sơn có viền vàng, icon mũi tên trái; về sảnh, có âm `UIAudio` |
| 2 | Băng tiêu đề | x 6 đến 33 %, cao 6,5 % | nền tối trong mờ (khoảng #403134, alpha 0,85) chuyển trong suốt về phải, viền vàng mảnh; chữ "Chương 1" và "Loạn Khăn Vàng" màu vàng kem (#F3D098 → #E8C26A, có bóng), giữa có vạch ngăn |
| 3 | Nút thông tin | cuối băng tiêu đề, đường kính 4 % chiều cao | vòng tròn viền vàng, icon chấm than trong vòng; mở bảng giới thiệu chương |
| 4 | Lời dẫn | dưới tiêu đề, x 2 %, 3 dòng | chữ trắng kem có bóng tối, cỡ khoảng 2 % chiều cao: "Năm 184, khởi nghĩa Khăn Vàng lan rộng. / Quân triều đình và các nghĩa binh cùng nhau dẹp loạn, / giải cứu dân chúng, ngăn chặn âm mưu của Trương Giác." (lấy từ dữ liệu chương) |
| 5 | Thưởng chương | x 1,5 đến 26,5 %, y 17,5 đến 26 % | huy hiệu tròn "Thưởng Chương" bên trái, 3 rương vẽ (gỗ, bạc xanh, vàng đỏ) kèm sao + số "x/7", "x/14", "x/21". **Chương có 7 ải × 3 sao = 21 sao, nên mốc là 7 / 14 / 21** (ảnh mẫu 12/24/36 là bản 12 ải cũ, không dùng). Đủ sao thì rương rung nhẹ và có tia sáng (mục 7); chạm để nhận |
| 6, 7 | Tài nguyên | phải trên: lương thảo x 70 đến 83 %, tiền x 85,5 đến 98 % | dùng đúng thanh tài nguyên của sảnh (icon, khung, nút cộng), không vẽ lại |
| 8 | Biển tên khu | trên mỗi khu đã mở | biển gỗ/giấy da, chữ kem đậm; tên: Làng Bình An, Đồng Lúa, Rừng Trúc, Trại Lương, Tiền Doanh, Đại Doanh Hoàng Cân, Tế Đàn Hoàng Cân. Gắn theo điểm 3D (world → screen), không co giãn theo zoom quá 0,85 đến 1,15 |
| 9 | Nút ải | tại `StageMarker_01..07` (hoặc vị trí trong dữ liệu) | huy chương tròn viền vàng, nền gỗ tối (#371E10), số ải vàng đậm; dưới là 3 sao (`UiFactory.AddStars`, sao rỗng / vàng). Đường kính khoảng 5 % chiều cao, vùng chạm tối thiểu 48 dp |
| 10 | Đường nét đứt | dọc đường chính giữa các nút | nét đứt vàng sáng có viền tối, nằm trên mặt đất (decal / line mesh bám địa hình theo đường `road` trong JSON), không phải UI phẳng; đoạn tới ải kế tiếp có nét chạy chậm (mục 7) |
| 11 | Nút ải boss | ải 5 (Trình Viễn Chí) và ải 7 (Trương Giác) | huy chương đỏ sơn mài (#A6301C), số trắng, 3 sao, tên boss bên dưới |
| 12 | Băng tên tế đàn | trên khu ải 7 | băng đỏ viền vàng "Tế Đàn Hoàng Cân" |
| 13 | Khóa | tâm khu chưa mở | vòng tròn xám bạc, icon ổ khóa; không hiện tên, số ải, sao, NPC của khu đó (§19) |
| 14 | Mây viền | quanh mép map và trên khu khóa | mây mềm, chuyển động chậm; xem mục 5 |
| 15 | Nhân vật đại diện | trên map | tướng người chơi đứng ở nút ải gần nhất đã mở; đi theo đường (mục 4) |
| 16 | Khu bị khóa | vùng dưới mây | khử bão hòa về xám, tối 20 %, phủ mây; tan khi mở |

Thêm (không có trong ảnh mẫu nhưng §4 bắt buộc): **bảng ải / Xuất Chiến** (tên ải, số sao, phần thưởng, nút "Xuất Chiến" dùng `GameButton`), **bảng hội thoại** (chân dung, tên NPC, lời thoại, nút "Nhận nhiệm vụ" / "Đóng"), **khung nhiệm vụ** nhỏ ở mép trái giữa (mục tiêu hiện tại + mũi tên chỉ hướng khi mục tiêu ở ngoài màn hình). Cùng bộ khung, màu, font với sảnh.

**Icon** (đồng bộ với game): dùng icon đã có trong `UiIconSet` trước (`Swords` là icon nút Tấn công, `Star`, `Plus`, `Shield`...). Icon mới tải từ Lucide, xuất PNG trắng 128 px như quy trình trong `ThietKeUI.md`, ghi giấy phép, khai báo vào `UiIconSet`:

| Dùng cho | Icon |
|---|---|
| Quay lại | Lucide `arrow-left` |
| Thông tin chương | Lucide `circle-alert` |
| Dấu nhiệm vụ "?" | Lucide `circle-question-mark` (tên cũ `circle-help`); nếu vòng tròn của icon trùng viền huy chương thì dùng Material Symbols `question_mark` (Apache 2.0), độ đậm khớp nét Lucide 2 px |
| Điểm giao chiến "X" | Lucide `swords` (đã có: `Assets/Arena3Q/Art/UI/Icons/Swords.svg`) |
| Khóa | Lucide `lock` |
| Đã xong | Lucide `check` |

Rương, huy hiệu, khung gỗ, biển tên là **ảnh vẽ** (2D của bạn, cùng phong cách và bảng màu với 2D Chương 1 đã duyệt), không phải icon thư viện.

Kiểm UI: làm và duyệt trên web ở chế độ "Điện thoại" (Redmi 2340 × 1080, 16:9, 18:9, 20:9, vùng an toàn giống máy). Chụp ảnh cùng tỉ lệ với ảnh mẫu, đặt cạnh nhau, liệt kê chỗ lệch. APK chỉ khi người dùng yêu cầu.

---

## 3. Dấu tương tác trên NPC

Hai loại dấu, nổi trên đầu NPC tại điểm neo `MarkerAnchor_quest` / `MarkerAnchor_battle` (con của NPC trong scene; NPC có tên `NPC_<vai> [quest]` hoặc `[battle]`):

- **Dấu hỏi "?"** – NPC giao việc (dân làng, trưởng làng, chỉ huy nghĩa binh, dân bị giam): nhiệm vụ như đi chiếm, bảo vệ, trinh sát, giải cứu. Huy chương vàng kem, icon "?" màu nâu sẫm. Chạm → nhân vật đi tới → mở hội thoại → "Nhận nhiệm vụ" chuyển ải sang `IN_PROGRESS` và làm hiện dấu kiếm của ải đó.
- **Hai kiếm chéo "X"** – điểm giao chiến (toán Khăn Vàng, đội trưởng, tướng giữ cổng, boss). Huy chương đỏ sơn mài, icon kiếm trắng; boss dùng bản lớn hơn 1,25 lần có viền vàng. Chạm → nhân vật đi tới → bảng ải / Xuất Chiến.

Danh sách đã đặt (11 điểm, xem JSON):

| Ải | Dấu "?" | Dấu kiếm |
|---|---|---|
| 1 Làng Bình An | trưởng làng cạnh giếng (giải cứu dân) | toán Khăn Vàng canh cũi |
| 2 Đồng Lúa | chỉ huy quân lương cạnh kho (bảo vệ kho), trinh sát ở lối vào ruộng (đi trinh sát) | toán cướp lương bên suối |
| 3 Rừng Trúc | | lính gác lối vào trại tiền tiêu |
| 4 Trại Lương | dân bị giam (nhờ giải cứu, chiếm lại kho) | đội trưởng Khăn Vàng cạnh xe lương |
| 5 Tiền Doanh | | Trình Viễn Chí (boss) |
| 6 Đại Doanh | | tướng giữ cổng (một marker duy nhất cho cả ải, §17) |
| 7 Tế Đàn | | Trương Giác (boss) |

Luật hiển thị: chỉ hiện ở khu đã mở; dấu "?" hiện khi ải `AVAILABLE`; dấu kiếm hiện khi ải `IN_PROGRESS` (ải không có "?" thì hiện ngay khi `AVAILABLE`); `COMPLETED` thì ẩn cả hai (NPC đổi lời thoại). Dấu luôn quay về camera, cỡ trên màn hình cố định (khoảng 4,5 % chiều cao, vùng chạm 56 dp), không bị mái nhà che: vẽ trên cùng nhưng mờ 40 % khi NPC bị che, và chạm dấu vẫn được (§20). Không chọn nhầm NPC cạnh nhau: ưu tiên dấu gần điểm chạm nhất trong bán kính chạm.

---

## 4. Di chuyển, chạm từ xa, chuyển trận (§20, §21)

- Thêm gói AI Navigation (`com.unity.ai.navigation`), NavMeshSurface trên `Terrain` (đã có MeshCollider và Navigation Static); công trình có BoxCollider. Khu khóa là vùng cấm (NavMeshModifierVolume đổi theo tiến độ) để không tìm đường xuyên khu khóa.
- Chạm NPC hoặc dấu: chọn mục tiêu, tìm đường, dừng ở bán kính tương tác (1,8 m), quay mặt về NPC, mở hội thoại hoặc bảng ải. Xử lý đủ 7 trường hợp của §20 (đổi mục tiêu, chạm đất hủy, bị che, đường khóa báo điều kiện, NPC đổi trạng thái, bị kẹt, đổi scene).
- Camera map: kéo để xem, chụm / cuộn để zoom, giữ trong map và giới hạn zoom như camera trận (`TiltedViewMath.ClampFocus`, `FootprintBox`); theo nhân vật khi đi nhưng cho xem khu nhiệm vụ.
- Xuất Chiến: lưu chapter, stage, vị trí và hướng nhân vật, camera; gọi `BattleLaunch` (trận + đội hình tối đa 5 tướng + callback kết quả) sang `Battlefield_HighQuality`. Thắng hoặc thua đều quay về đúng vị trí; chỉ mở sương và trao thưởng khi có kết quả thắng hợp lệ.

## 5. Sương mù và mở khóa (§19)

- Lần đầu: ải 1 mở, ải 2 đến 7 dưới mây. Mây mềm, viền chuyển dần (mask theo khu + shader độ trong suốt + sprite mây trôi), không có tấm chữ nhật cứng. Khu dưới mây khử màu về xám (thành phần 16).
- Mở khóa theo đúng 9 bước của §19: về map → cập nhật NPC và môi trường ải vừa xong → camera lướt tới khu kế (1,2 s, ease in-out) → mây tan dần (2,5 đến 3 s, viền nhiễu) → công trình, NPC hiện (fade 0,6 s) → dấu tương tác và biển tên hiện sau cùng → lưu tiến độ. Khu đã mở không bao giờ phủ mây lại; tải lại game hiện đúng tiến độ; mây không che HUD hay nhân vật ở khu đã mở.

## 6. Trạng thái thế giới (§22)

Mỗi khu trong scene có ba nhóm con: `Always`, `State_Before` (đến khi thắng ải), `State_After` (sau khi thắng; scene lưu ở dạng chưa thắng nên nhóm này đang tắt). Bạn bật / tắt theo trạng thái đã lưu, có chuyển tiếp mềm (không pop). Hiện đã gắn: cờ địch ải 4, 5, 6; lính Khăn Vàng, đội trưởng, boss, hộ pháp; dân bị giam ải 1 và 4 (Before); dân được cứu về sân làng ải 1, dân và nghĩa binh ở xe lương ải 4 (After). Phần trạng thái còn thiếu mô hình (cổng đại doanh mở, tháp canh rừng trúc bị phá, tế đàn tắt năng lượng) Claude sẽ dựng và gắn vào `State_After` (mục 11).

## 7. Chuyển động môi trường: nhẹ, tinh tế

Nguyên tắc: không đồng loạt (mỗi thứ có pha ngẫu nhiên theo vị trí), luôn hiện / tắt bằng fade, không bao giờ pop; gió, khói, nước dùng nhiễu, không thành dải xoắn hay vạch đều (Model3D_Rules). Mọi số trong `MapMotionConfig`. Preset 30 FPS chỉ được bớt trang trí (cột "30 FPS"), không đổi thông tin chơi (dấu, sương, trạng thái, nút ải luôn như nhau).

| Thứ | Cách làm | Số gợi ý | 30 FPS (thấp) |
|---|---|---|---|
| Cỏ, lá cây lay | hệ gió có sẵn của chiến trường (`BattlefieldEnvironment` đặt biến gió toàn cục; shader thực vật đã hỗ trợ): thêm bộ lái gió vào map | độ mạnh 35 % của trận, cơn gió 6 đến 10 s | độ mạnh 45 % |
| Tre đung đưa | shader rung theo chiều cao cho `M_Bamboo` | đỉnh lệch 8 đến 12 cm, chu kỳ 3,5 đến 5 s | biên độ 50 % |
| Nước suối chảy | cuộn UV theo chiều dòng (UV.y của mesh `River` chạy dọc suối) + lớp nhiễu thứ hai; bọt bờ bằng ảnh 08 | 0,05 đến 0,08 ô/s, lớp hai 0,03 chéo | một lớp |
| Lá rơi | hạt lá mỏng quanh cụm cây | tối đa 3 lá mỗi khu, rơi 4 đến 6 s, xoay chậm | tối đa 1 |
| Bụi đường | cụm bụi mờ ngẫu nhiên dọc đường chính | 1 cụm mỗi 6 đến 10 s, alpha tối đa 0,15 | tắt |
| Chim xa | đàn 4 đến 7 con bay qua rìa map | mỗi 25 đến 45 s, cao 15 m, không qua vùng HUD | tắt |
| Bóng mây trôi | ảnh bóng mây chiếu xuống mặt đất | độ mờ 6 đến 8 %, trôi 0,4 m/s | tắt |
| Mây viền, mây khóa | sprite mây nhiều lớp trôi, cuộn chậm | 0,2 đến 0,3 m/s | 2 lớp |
| Cờ | lưới vải rung theo nhiễu, cán đứng yên | 3 đến 5 cm, 1,5 đến 2,5 s | biên độ 60 % |
| Lửa trại, đuốc | flipbook có trộn khung kề nhau, sáng nhấp nháy theo nhiễu ±8 %, quầng sáng sprite (không thêm đèn realtime) | 12 khung/s | giữ, bỏ quầng |
| Khói | 1 đến 2 dải mảnh từ lửa trại, tan dần | 6 đến 9 s mỗi làn | 1 dải |
| NPC (khi có nhân vật thật) | thở, đổi tư thế, lính tuần 6 đến 10 m, dân đi tuyến ngắn, người gánh đồ | đổi tư thế mỗi 6 đến 12 s, lệch pha | bớt tuần tra |
| Dấu "?" / kiếm | nhấp nhô + hào quang thở | nhấp nhô 6 px / 2,4 s; hào quang alpha 0,6 → 1 / 2 s; hiện: fade + scale 0,8 → 1 trong 0,3 s; ẩn: fade 0,25 s | giữ nguyên (thông tin chơi) |
| Nút ải đang mở | vòng sáng thở | scale 1 → 1,04 / 2 s | giữ nguyên |
| Đường nét đứt tới ải kế | nét chạy chậm về phía ải | 0,5 nét/s | giữ nguyên |
| Rương đủ sao | lắc nhẹ + tia sáng mờ | 3° mỗi 4 s | bỏ tia |
| Tế đàn ải 7 | vòng phép, hạt năng lượng, sét nhỏ, hào quang boss (Chuong1.md §18) | sét 1 lần mỗi 5 đến 9 s, sáng ngắn | bớt hạt |

## 8. 2D trên map còn thiếu (của bạn)

Đặt theo vị trí trong JSON (đường `road`, `paths`, sân `yards`, ruộng `paddies`), dạng quad / decal bám đất, đúng màu dưới grading (§26, không viền alpha trắng / đen, không lơ lửng):
- vệt bánh xe và dấu chân dọc đường chính, quanh kho lương (ảnh 28), lá rụng (27), rác, vải rơi, vết cháy nhỏ (16) ở làng và trại;
- cỏ, hoa dại, lau sậy ven suối (11, 15), cụm lúa gần (12) ở ô ruộng, lá trúc dưới đất (09) ở rừng trúc;
- sửa màu đã báo: đường 02 tách sáng khỏi đất 01 khoảng 10 đến 15 %, ô ruộng 04 bớt lạnh (xem ảnh map).

## 9. Dữ liệu và code (giữ dễ sửa)

- **Data** (ScriptableObject, có `Validate()`): `ChapterMapConfig` (id chương, tên, lời dẫn, 7 ải: id, tên khu, vị trí nút, boss hay không, `StageConfig` trận, danh sách mục tiêu `Quest` / `Battle` kèm đường dẫn NPC; mốc sao 7 / 14 / 21 và phần thưởng), `MapUiConfig` (vị trí, cỡ, màu ở mục 2), `MapMotionConfig` (mục 7).
- **Core** (C# thuần, có EditMode test): luật tiến độ ải `LOCKED → AVAILABLE → IN_PROGRESS → COMPLETED`, mở ải kế khi thắng, tổng sao, rương nhận được, ai được tương tác.
- **Lưu**: tiến độ chương qua kho hồ sơ hiện có (`IPlayerProfileStore`), không ghi file trong test.
- **Gameplay / Exploration**: bộ điều khiển map, camera, chạm và tìm đường, sương, áp trạng thái thế giới; tất cả nằm trong `CH01_MapSystems.prefab` và asmdef riêng.

## 10. Điểm gắn âm thanh (§24)

Đặt điểm phát theo khu (ambience làng, gió chim, suối, lửa trại, doanh trại), âm bước chân, chọn NPC, mở nhiệm vụ, mây tan, mở rương, báo boss; giới hạn số nguồn phát cùng lúc trên điện thoại. Chưa cần lồng tiếng.

## 11. Phần còn thiếu và ai làm

| Ai | Việc |
|---|---|
| Codex | mọi mục 1 đến 10 ở trên; ảnh 2D cho UI (khung, biển, huy chương, rương), mây, VFX; báo cáo test thực tế (§31) |
| Claude | tế đàn kiểu Hoàng Cân (bỏ họa tiết Bát Quái), 3 trụ năng lượng, tượng đá; bản `State_After`: cổng đại doanh mở, tháp canh rừng trúc bị phá, xe lương có hàng; bản nhẹ cho cụm tre ở xa; dựng lại map theo góp ý; review từng phần bạn gửi |
| Người dùng | nhân vật: trưởng làng, dân, nghĩa binh, lính Khăn Vàng (đao, thương, cung), hộ pháp, Trình Viễn Chí, Trương Giác (AI 3D hoặc Sketchfab theo danh sách đã gửi) |

## 12. Nghiệm thu

- EditMode: luật tiến độ, sao, rương, ai hiện dấu gì theo trạng thái. PlayMode: mở map với tiến độ giả trong bộ nhớ, chạm dấu → đi tới → hội thoại / bảng ải; tải lại khôi phục đúng sương và trạng thái.
- Ảnh: UI so cạnh ảnh mẫu ở 16:9 / 18:9 / 20:9 và Redmi trên web; ảnh mở khóa từng ải (trước / sau); 3 preset 30 / 60 / 120 FPS cùng một cảnh (ghi rõ chất lượng từng preset).
- Không báo "hoàn thành" khi mới có code hoặc tài liệu (§31); chức năng chưa có phải ghi rõ.
