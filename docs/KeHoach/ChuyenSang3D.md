# Kế hoạch chuyển Arena3Q sang hiển thị 3D

Trạng thái: **nháp chờ người dùng duyệt** (2026-10-21). Người dùng đã quyết định chuyển sang 3D; tài liệu này là cách làm. Khi duyệt xong, chuyển file sang thư mục chủ đề và cập nhật các tài liệu ở mục 10.

Ký hiệu: ✅ đã chốt · 🟡 đề xuất, cần người dùng xác nhận · ⏳ để sau.

---

## 1. Quyết định và phạm vi

- ✅ (người dùng 2026-10-21) **Chuyển phần hiển thị sang 3D.** Lý do: 9 tướng + lính + quân địch bằng sprite 8 hướng sẽ làm APK 300–450 MB+, quay người giật theo 45°, mỗi lần sửa phải render lại hàng nghìn khung; 3D nhẹ hơn (~8–15 MB/tướng) và mượt hơn ở 60 FPS.
- ✅ **Luật chơi không đổi.** Toàn bộ logic vẫn chạy trên mặt phẳng 2D (`UnitBase.Position`, `BattlefieldLayout`, kỹ năng, AI, Bát Quái Trận). Chỉ thay **lớp hiển thị**: logic (x, y) ↔ thế giới 3D (x, 0, y).
- ✅ **Tối thiểu 60 FPS** trên Redmi Note 8 Pro (spec §1) – điều kiện chặn của mọi giai đoạn.
- 🟡 Góc nhìn giữ cảm giác "Clash of Clans": camera nghiêng cố định từ trên xuống (mục 3).
- Không đổi: HUD uGUI, luồng Frontend, dữ liệu ScriptableObject, test luật (EditMode), quy tắc AgentComms.

## 2. Ngân sách hiệu năng (Redmi Note 8 Pro: Helio G90T, Mali-G76 MC4, 1080×2340, 60 Hz)

| Mục | Ngân sách 🟡 | Ghi chú |
|---|---|---|
| Khung hình | trung bình ≥ 60 FPS, p95 ≤ 18 ms, sau 15 phút chơi vẫn ≥ 55 | đo bằng build Development trên máy, trận đủ 5 tướng + lính + địch + VFX |
| Tam giác / khung | ≤ 600–800 nghìn | tướng 30–45 nghìn, lính / quân thường 1,5–3 nghìn, boss ≤ 60 nghìn, đạo cụ map tổng ≤ 150 nghìn |
| Lượt vẽ | ≤ 150 | lính cùng loại dùng chung material, GPU instancing / SRP batcher |
| Nhân vật skin xương | GPU skinning, tối đa 4 xương / đỉnh | lính xa: animation nướng sẵn vào texture (VAT) nếu cần |
| Texture nhân vật | tướng 1 atlas 2048 (màu + normal), lính 512–1024, ASTC 6×6 | |
| Ánh sáng | 1 đèn chính + ambient; **không bóng thời gian thực** | bóng giả dưới chân (blob / decal mềm) |
| Độ phân giải vẽ | render scale 0,75–0,8, MSAA tắt hoặc 2× | màn 2340×1080 rất nhiều điểm ảnh |
| VFX | hạn chế lớp trong suốt lớn; không tấm phủ kín màn hình | **rủi ro chính** (vẽ chồng trên Mali) |
| RAM | ≤ ~1,2 GB PSS khi trận | |

## 3. Camera và chuyển tọa độ

- 🟡 **Camera phối cảnh góc hẹp** (FOV ~30°, nghiêng ~50–55°, nhìn hướng về phía bắc của map), tất cả là dữ liệu cấu hình. Phương án dự phòng: orthographic nghiêng (giống hiện tại, dễ khớp map vẽ sẵn).
- `BattleController.ToView` đổi từ "nén trục Y" sang ánh xạ (x, y) → (x, 0, y). `viewTiltDegrees`, thanh "Góc nhìn" và `ViewYScale` bỏ (hoặc chỉ để debug).
- `BattleCameraController.ScreenToWorld / WorldToScreen` dùng **tia từ camera chạm mặt phẳng đất** (y = 0); kéo / zoom / theo tướng giữ nguyên hành vi, giới hạn zoom tính lại theo phối cảnh.
- Thứ tự vẽ theo trục Y (`sortingOrder = -y*20*4`) bỏ – 3D dùng độ sâu. Thanh máu, biểu tượng choáng / khiên, vòng chọn tướng thành billboard / decal (mục 6).

## 4. Các giai đoạn

### P0 – Làm thử, quyết định đi tiếp (bắt buộc trước mọi giai đoạn khác)
Mục tiêu: chứng minh 60 FPS trên Redmi với 3D trước khi chuyển hết.
1. Scene thử `Battle3DSlice` (bản sao Battle) dùng Universal Renderer (Forward), camera 3D ở mục 3, mặt đất = mặt phẳng với `BatTran_Painted.png`.
2. Mặc Ảnh 3D: dùng bản Battle LOD 43,6 nghìn tam giác có sẵn (`Graphics3D/Source/LOD/MacAnh_BattleRig.fbx`, 11 clip), hiển thị thẳng trong cảnh 3D (không qua RenderTexture như bản thử hiện tại).
3. Lữ Bố 3D: xuất FBX từ `LuBu_Arena3Q_Grip.blend` (rig + 7 clip + kích), giới hạn 4 xương, giảm lưới về ~40 nghìn. Lỗi da hiện tại chấp nhận **chỉ cho bài đo**.
4. 3 tướng còn lại + 40 lính + 30 quân địch: khối low-poly có xương đơn giản (đi / đánh), đúng ngân sách tam giác và lượt vẽ.
5. 9 đạo cụ map (cột, đài, phiến, cờ, chòi, rào, trại, quảng trường) từ model Meshy đang có, giảm lưới theo ngân sách.
6. VFX: các flipbook Mặc Ảnh hiện có, thêm pass 3D cho shader (billboard).
7. Đo trên Redmi: trung bình, p95, sau 15 phút, nhiệt; có / không VFX; ở render scale 0,75 và 1.

**Điều kiện qua P0:** đạt ngân sách mục 2. Không đạt → tìm nút thắt (VFX, skin, fill-rate) và đề xuất phương án lai (tướng 3D, lính sprite / VAT) để người dùng chọn.

### P1 – Nền hiển thị
- URP: Universal Renderer làm mặc định cho Battle (Frontend có thể giữ 2D cho UI thuần, hoặc dùng chung); 1 đèn chính, ambient, tắt bóng thời gian thực; post-processing tối thiểu (bloom thấp có công tắc).
- `PlaceholderArt` → khối 3D tạm (capsule / box theo màu phe) thay hình tròn.
- `UnitVisual`: thanh máu billboard (hoặc UI theo màn hình gom một canvas), vòng chọn / viền bậc (tinh nhuệ, boss) là decal mặt đất, nháy khi trúng đòn bằng property block.
- Trạng thái (choáng, khiên, yếu, khiêu khích) thành biểu tượng billboard / vòng decal.
- `SkillAreaVisual` (cảnh báo vùng, vùng kỹ năng) thành decal mặt đất mềm.
- Hiệu ứng tăng tốc theo spec 60 FPS: `RuntimePerformanceConfig` (Codex đang làm A4) dùng chung.

### P2 – Map Bát Quái Trận 3D
- Mặt đất: mặt phẳng + texture vẽ (`BatTran_Painted`) hoặc dựng từ bộ 3D Terrain (`BatTran_Terrain/`, script đang để ở `Scripts~`). 🟡 chọn một.
- `BaguaFormationView`: 8 cột, đài, 8 cửa với phiến đá thật 3D (nâng / hạ khi đóng cửa Đỗ), hiệu ứng cửa (hồi máu, tăng / giảm tốc, sát thương chuẩn) là decal / particle nhẹ.
- Trại / thành: `ThreeKingdomsCommand_1/2` 3D; `CastleFallView` làm lại bằng animation (rung, sụp, mảnh vỡ particle, khói), vẫn chạy thời gian thực sau khi trận kết thúc.
- `MapDecorView`: đạo cụ trang trí 3D (cờ, chòi, rào, cây `cayCau/cayKho/gocCay.blend`).
- Vùng cấm, đường đi, `MapBlueprintExporter`: không đổi (logic).
- `PropSpriteRenderer` + `PropSprites.asset`: không dùng nữa sau P2 (giữ công cụ tới khi xoá sprite).

### P3 – Quy trình nhân vật 3D (chuẩn chung cho mọi tướng / lính / quân địch)
- Nguồn → **bản trong trận** (ngân sách mục 2), tối đa 4 xương / đỉnh, giữ UV / màu gốc, bake normal từ bản chi tiết. Bản chi tiết (150–300 nghìn) chỉ dùng cho màn chọn chủ tướng nếu đo được.
- Bộ clip tối thiểu: Idle, Walk, Run, Attack01, Attack02, các clip kỹ năng (theo tài liệu từng tướng), Victory, Defeat; Mặc Ảnh thêm Dodge / DodgeAttack.
- Chạy animation bằng `PlayableGraph` / lấy mẫu clip theo đồng hồ trận (như bản thử Mặc Ảnh), giữ logic có sẵn của `HeroAnimationView` / `MacAnhPerformanceView`: hướng theo vận tốc chủ động, nhịp bước theo quãng đường, hoà clip khi đổi trạng thái — nhưng **xoay liên tục** thay cho 8 hướng.
- Socket (đầu quạt, tay trái, tâm phép, lưỡi kích) lấy từ xương, thay cho bảng socket theo khung sprite.
- **Cổng QA bắt buộc** (`docs/Tuong/Model3D_Rules.md`): đo bề mặt mọi khung + nửa khung sau giới hạn 4 xương (giãn > 1,35 / ép < 0,65 khi lệch > 0,4 % chiều cao, gai > 3× / +2 mm), ảnh 6 góc, va xuyên vũ khí, kiểm trong Unity (BakeMesh). Không qua thì không đưa vào trận.
- Thứ tự:
  1. **Mặc Ảnh** – bản Battle LOD đã đạt bề mặt (985 mẫu, 0 lỗi); còn: material lụa cho bản 3D, đường nối nách / cổ tay, chuyển sang đường hiển thị 3D chính.
  2. **Lữ Bố** – xuất rig sang Unity; sửa lỗi da vai / áo choàng / gáy (hiện 0/7 clip đạt LBS4); kích gắn `Socket_Hand_R`, rơi kích ở Defeat; 4 kỹ năng theo `Arena3Q_LuBu_4Skills_VFX_Production_Spec_v1_0.md` (cần phụ lục áp dụng cho Arena3Q, xem mục 9).
  3. 3 chủ tướng còn lại, 4 tướng khởi đầu, lính, 3 quân địch cơ bản, 18 quân địch chương 1–3 + boss — **cần model từ người dùng** (mục 7).

### P4 – VFX
- Hai loại song song: (a) **flipbook billboard** cho hiệu ứng khí / khói / thể tích (lốc Skill02 không thể dựng volume thời gian thực trên Redmi) – giữ hệ `MacAnhVfxRecipe` / `MacAnhVfxPlayer`, đồng hồ hiệu ứng (A1), hoà khung (A3), thêm pass 3D cho shader; (b) **mesh / particle 3D thời gian thực** cho vệt chém, tia lửa, vòng sóng (rẻ, có chiều sâu thật).
- Vòng trận dưới đất thành decal nằm trên mặt đất (không cần ép theo `ViewYScale`).
- Đánh giá lại các prefab VFX 3D sẵn có của Mặc Ảnh (`Graphics3D/FX_*3D*.prefab`) theo ngân sách vẽ chồng.
- Luật giữ nguyên: gió / khói tự nhiên (Model3D_Rules 28–29), dạng khí, tụ / tan mềm, ≥ 90 % so với tài liệu tướng, không phủ kín màn hình (lớp tối nền của Ultimate cần làm lại bằng cách khác hoặc bỏ – tài liệu Lữ Bố cũng cấm đổi màu toàn cảnh).

### P5 – Frontend
- Màn chọn chủ tướng: model 3D xoay chậm (bản trong trận + material tốt, hoặc bản chi tiết nếu đo được trên Redmi), ánh sáng riêng.
- Các màn khác (intro, đăng nhập, máy chủ, đặt tên, cốt truyện) không đổi.

### P6 – Test, công cụ, dung lượng
- Cập nhật `BattleFlowTests` và các test chụp màn hình (`Logs/Screenshots`); thêm scene / test đo hiệu năng (`MacAnhLodDeviceBenchmark` mở rộng thành benchmark trận).
- Bỏ khỏi build: atlas sprite 8 hướng (Mặc Ảnh ~43 MB, Lữ Bố ~25 MB), `MacAnhSpriteLibrary`, nhánh RenderTexture của bản thử 3D, `PropSprites`. Ước APK sau chuyển: 🟡 ~120–180 MB khi đủ 9 tướng + lính + quân địch.
- Công cụ sprite (`ExportSmoothSprites.py`, `export_macanh_sprites.py`, `RepackLuBuPages.py`, `PropSpriteRenderer`) giữ trong lịch sử git, xoá khỏi luồng chính khi người dùng duyệt.
- Mỗi giai đoạn: PlayMode + EditMode xanh, đo FPS trên Redmi, ảnh trước–sau, báo cáo AgentComms.

### P7 – Tài liệu và dọn dẹp
Xem mục 10.

## 5. Còn dùng được / thừa sau khi chuyển

| Phần đã làm | Sau khi chuyển |
|---|---|
| Logic, AI, kỹ năng, Bát Quái Trận, Frontend, test luật | giữ nguyên |
| 60 FPS (A4), đồng hồ hiệu ứng (A1), hoà khung (A3), recipe flipbook | giữ (thêm pass 3D cho shader) |
| Bóng ảnh 3D (A2), đo bề mặt theo chuẩn Unity, LOD Mặc Ảnh | giữ – thành đường chính |
| Model Mặc Ảnh, rig / weight / lông trĩ / chỗ cầm kích Lữ Bố | giữ |
| Model đạo cụ map (Meshy) | giữ (giảm lưới) |
| Sprite 8 hướng Mặc Ảnh / Lữ Bố, socket theo khung, atlas 2048, sprite đạo cụ map | thừa |
| Bản thử 3D qua RenderTexture (`MacAnhRealtimeBattleView`) | thay bằng hiển thị 3D trực tiếp |

## 6. Rủi ro và cách xử lý

| Rủi ro | Xử lý |
|---|---|
| Không đạt 60 FPS | P0 trước; nút thắt thường là VFX vẽ chồng → giảm lớp, render scale, LOD VFX; phương án lai |
| Lỗi da lộ rõ ở 3D | cổng QA P3, không đưa model chưa đạt vào trận |
| Thiếu model cho 7 tướng + lính + quân địch | danh sách mục 7; khối tạm đúng ngân sách tới khi có model |
| Model AI (Meshy) nặng, rối lưới | quy trình giảm lưới + bake normal, giữ silhouette, đo trước khi nhận |
| Nóng máy sau 15 phút | đo dài, giới hạn chất lượng tự động theo nhiệt (🟡) |
| Dung lượng repo (FBX, texture) | mục 8 |
| Map vẽ 2D lệch với cảnh 3D | dùng mặt phẳng với texture vẽ theo góc nhìn trên xuống, hoặc terrain 3D |

## 7. Nội dung cần người dùng cung cấp (model 3D)

| Nhóm | Số lượng | Yêu cầu |
|---|---|---|
| Chủ tướng Cung thủ, Kiếm sĩ, Thống soái | 3 | model có thể rig (tư thế A / T), đúng phong cách chibi Đông Á cổ trang như Mặc Ảnh, kèm vũ khí riêng |
| Tướng khởi đầu Xạ Thủ, Kỵ Binh (có ngựa), Giáp Sĩ, Đao Khách | 4 | như trên; Kỵ Binh cần ngựa rig riêng |
| Lính (`Troop_Soldier`) | 1 (có thể 2–3 biến thể màu) | low-poly 1,5–3 nghìn tam giác |
| Quân địch cơ bản (cung, kiếm, cục súc) | 3 | low-poly |
| Quân địch chương 1–3 + boss (`Content/Enemies/C1..C3`) | 18 | thường ≤ 3 nghìn; tinh nhuệ ≤ 8 nghìn; boss ≤ 60 nghìn |
| Đạo cụ / cây map | đã có 9 + 3 cây | giảm lưới theo ngân sách |

Rig và animation có thể do agent làm (như Mặc Ảnh / Lữ Bố) sau khi có model.

## 8. Quản lý file 3D trong git

Hiện quy định "chỉ commit code và asset nhỏ" (2026-10-20). Với 3D, FBX + texture của bản trong trận là asset runtime bắt buộc (mất là game không có nhân vật). 🟡 Đề xuất: **Git LFS** cho `*.fbx`, `*.png`/`*.tga` texture nhân vật và map trong `Assets/`; nguồn `.blend` chi tiết chuyển sang `ArtSource/Heroes/<Tướng>/` (theo quy tắc chuyển file 3D ra khỏi `docs/` khi người dùng chốt), cũng qua LFS hoặc lưu ngoài repo.

## 9. Việc liên quan đang dở

- Codex đang sửa Mặc Ảnh (A1, A2 đã chốt; A3 hoà khung, A4 60 FPS đang làm; đường nối, material 3D còn mở) – **đều dùng được cho 3D**. Đề nghị **dừng mọi lần xuất lại sprite lớn** (Mặc Ảnh, Lữ Bố).
- Tài liệu 4 kỹ năng Lữ Bố: cần phụ lục "áp dụng cho Arena3Q" (kỹ năng phụ tự dùng, thanh nộ, mở theo sao, khối cơ chế mới: hất tung, tầng Cuồng Huyết, hút máu khi đủ tầng, miễn làm chậm, kết liễu riêng tuyệt kỹ). Phần Animator / Animation Event / FBX của tài liệu đó **khớp hơn** với hướng 3D (chỉ cần thay Physics bằng truy vấn `UnitRegistry` trên mặt phẳng logic).

## 10. Tài liệu phải cập nhật sau khi duyệt

`docs/GameplaySpecification.md` (§1 góc nhìn 2.5D → 3D; §9 camera / góc nghiêng), `docs/BanDo/MapSpecification.md` §8 (quy cách art), `CLAUDE.md` (Rendering: URP 2D → Universal Renderer; Logic vs view positions; Rendered heroes), `docs/Tuong/Model3D_Rules.md` (ngân sách 3D trong trận), `docs/Tuong/LuBu.md`, Delivery_QA của Mặc Ảnh, `docs/README.md`.

## 11. Thứ tự và mốc nghiệm thu

| Mốc | Nội dung | Điều kiện đạt |
|---|---|---|
| M0 | P0 làm thử | đạt ngân sách mục 2 trên Redmi; người dùng xem trên máy và đồng ý |
| M1 | P1 + P2 | trận chơi được hoàn toàn bằng 3D với khối tạm + map 3D; test xanh; ≥ 60 FPS |
| M2 | P3 Mặc Ảnh + Lữ Bố | qua cổng QA model; VFX 2 tướng ≥ 90 % so với tài liệu; Codex / Claude nghiệm thu chéo |
| M3 | P3 các tướng còn lại + lính + quân địch | theo tiến độ có model |
| M4 | P4 VFX đầy đủ, P5, P6, P7 | APK, FPS sau 15 phút, tài liệu cập nhật, sprite cũ gỡ khỏi build |

## 12. Câu hỏi cần người dùng chốt

1. Camera **phối cảnh góc hẹp** hay **orthographic nghiêng**?
2. Mặt đất: **texture vẽ** trên mặt phẳng hay **terrain 3D** từ bộ kit?
3. Model 7 tướng + lính + quân địch: người dùng tạo (Meshy) hay giao agent tìm nguồn / dựng khối?
4. Lưu file 3D: **Git LFS** có được không?
5. Phân công P0: agent nào làm, agent nào nghiệm thu?
