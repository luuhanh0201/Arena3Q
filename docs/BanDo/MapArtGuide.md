# Arena3Q – Hướng dẫn gửi file và viết prompt để vẽ map

Phiên bản: v0.2 · 2026-10-17 (prompt chuyển sang `docs/art/concepts/BanDo_Prompts.md`). Dựa trên `docs/BanDo/MapSpecification.md` v0.4 (cỡ M, PPU 128, góc 3/4 cố định tỉ lệ 🟡 0.75, vẽ tay màu mềm, Tilemap + SpriteShape + sprite PSD).

> **Nguyên tắc quan trọng nhất:** *không* xin AI (hay họa sĩ) vẽ cả bản đồ thành **một tấm ảnh dùng trong game**. Ảnh toàn bản đồ chỉ để **chốt phong cách (concept)**. Thứ đưa vào game là **từng mảnh**: bộ ô nền, dải sông, cầu, thành, cây/đá – mỗi mảnh đúng kích thước pixel ở mục 4. Lý do: ảnh 6144 × 2496 px quá nặng cho điện thoại, AI không giữ đúng kích thước/tỉ lệ, và đổi bố cục là phải vẽ lại cả tấm (xem MapSpecification §8.4).

---

## 0. Ảnh bản đồ hiện có: `map1.png`

- Nằm ở `Assets/Arena3Q/Art/Maps/map1.png` (1971 × 798 px, vẽ tay, góc 3/4). Đã được **dùng tạm làm ảnh nền** cho cả hai ải (`StageConfig.backgroundArt`, menu **Arena3Q > Import Map Art (map1)**); khi ải có ảnh nền, game tự đặt góc nhìn bằng góc của ảnh (tỉ lệ 0.75 ≈ 41°) và ẩn hình placeholder của mặt đất/sông/cầu.
- Bố cục logic (sông, 2 cầu, 2 thành, vùng triệu hồi) đã được **đo từ ảnh này và chỉnh cho khớp** – `map1` hiện là ảnh chuẩn về phong cách và bố cục.
- Hạn chế: độ phân giải ≈ 41 px/đơn vị (cần 128) nên hơi mờ khi zoom gần; là một tấm liền (bản chính cần tách mảnh – mục 3). Cầu vẽ chỉ rộng ≈ 2.4 đơn vị → là điểm nghẽn.

**Khi có một ảnh bản đồ mới** (tạo bằng AI hoặc họa sĩ): (1) đo vị trí sông/cầu/thành trên ảnh, (2) hoặc sửa dữ liệu ải cho khớp ảnh (`PrototypeContentBuilder.StandardRiverPath`, `StandardBridges`, vị trí thành) rồi chạy **Apply Standard River Layout**, hoặc sửa ảnh cho khớp `*_mask.png`, (3) xuất lại blueprint và **chồng viền mask lên ảnh** để kiểm tra lệch ≤ 0.2 đơn vị.

## 1. File cần gửi kèm

Sinh tự động từ dữ liệu ải (luôn khớp game) bằng menu **Arena3Q > Export Map Blueprints** (hoặc batch `-executeMethod Arena3Q.Editor.MapBlueprintExporter.ExportAll`). Ảnh ở tỉ lệ 1/4 (32 px mỗi đơn vị ngang), đã nghiêng 3/4 (dọc × 0.75), **1536 × 624 px (tỉ lệ ≈ 2.46 : 1)**:

| File | Dùng để |
|---|---|
| `docs/art/blueprints/attack_01_mask.png` | **Ảnh bố cục gửi cho AI / làm nền cho họa sĩ vẽ đè**: chỉ có mảng màu phẳng |
| `docs/art/blueprints/attack_01_layout.png` | Ảnh **đọc** bố cục: lưới, vùng triệu hồi, điểm triệu hồi, quân phòng thủ, lề HUD (không gửi cho AI làm ảnh điều khiển – lưới và chấm sẽ bị vẽ vào tranh) |
| `docs/art/blueprints/defense_01_*.png` | Như trên cho ải Thủ (cùng địa hình, khu bên phải là chỗ quân địch xuất hiện) |
| `docs/BanDo/MapSpecification.md` mục 8 | Gửi cho họa sĩ: kích thước, góc nhìn, pivot |

**Bảng màu của ảnh mask** (ghi rõ trong prompt khi gửi ảnh):

| Màu | Mã | Nghĩa |
|---|---|---|
| Xanh lá | `#5C8C4A` | đất cỏ – đi lại được |
| Xanh nước | `#3A7BD5` | sông – vùng cấm, **không** được vẽ lấn ra ngoài |
| Nâu | `#8B5A2B` | cầu – lối qua sông |
| Xanh dương tròn | `#3D6FD9` | chỗ đặt **thành ta** (để trống, thành là sprite riêng) |
| Đỏ tròn | `#C0392B` | chỗ đặt **thành địch** (để trống) |
| Xám tối | `#4A4A58` | vùng cấm chữ nhật (hiện chưa có trong ải nào) |

Kèm thêm **1–3 ảnh tham khảo phong cách** (ảnh game bạn thích) nếu công cụ cho phép – giúp các mảnh vẽ ra cùng một phong cách.

---

## 2. Prompt

**Toàn bộ prompt bản đồ (đã ghép sẵn, dán nguyên văn) nằm cạnh ảnh mẫu: `docs/art/concepts/BanDo_Prompts.md`.** Mỗi prompt đã gồm khối phong cách chung: vẽ tay màu mềm, góc 3/4 nén dọc 0.75, sáng từ trên-trái – mọi mảnh phải cùng góc nhìn, hướng sáng và phong cách, nếu không ghép lại sẽ lệch nhau.

---

## 3. Các mảnh và kích thước

| Mảnh | Kích thước cuối | Ghi chú |
|---|---|---|
| A. Concept toàn bản đồ | tỉ lệ 2.46 : 1 | chỉ để chốt phong cách – **không** dùng trong game; gửi kèm `attack_01_mask.png` |
| B. Ô nền (Tilemap) | **128 × 96 px** mỗi ô (1 đơn vị × 0.75) | tối thiểu: cỏ (2–3 biến thể), đất, đường mòn, chuyển tiếp cỏ↔đất; lặp liền 4 phía |
| C. Dải nước sông (SpriteShape) | cao **296 px** (rộng sông 2.3 × 128), dài 512 px | lặp liền theo chiều ngang; Unity uốn theo đường tâm sông |
| C. Dải bờ sông | dài 512 px | đặt hai bên mép nước |
| D. Cầu | **464 × 320 px** (dài 3.6 bắc qua sông rộng 2.3; mặt cầu rộng 2.4 × 0.75 + lan can) | sông chạy thẳng đứng dưới mỗi cầu (map spec §6.2) → cầu vẽ nằm ngang, không xoay |
| E. Thành ta / địch | **448 × 576 px**, pivot đáy giữa | đáy thành là elip rộng ≈ 2.8 đơn vị (358 px); thành địch vẽ lại từ thành ta để cùng hình khối |
| F. Cây, bụi, đá | cây ≈ 1.5 × 2.5 đơn vị (192 × 320 px) | đặt rìa bản đồ, không chặn đường |

---

## 4. Cách dùng theo từng công cụ

| Công cụ | Gửi ảnh mask | Lặp liền (seamless) | Giữ phong cách giữa các lần |
|---|---|---|---|
| **ChatGPT / Gemini (vẽ ảnh)** | Đính kèm ảnh và nói "use the attached image as the exact layout" | Viết "seamless tileable" trong prompt, sau đó tự kiểm tra (mục 5) | Dùng cùng một cuộc trò chuyện, gửi lại ảnh đã ưng làm tham chiếu |
| **Midjourney** | Dán link ảnh vào đầu prompt (image prompt), thêm `--iw 2` để bám bố cục | Thêm `--tile` cho ô nền / dải sông | `--sref <link ảnh đã ưng>` và giữ cùng `--seed`; concept toàn map dùng `--ar 32:13` |
| **Stable Diffusion / ComfyUI** | ControlNet **Segmentation** hoặc **Canny** với ảnh mask, độ mạnh 0.8–1.0 | Bật "Tiling" / seamless | Cùng checkpoint + LoRA phong cách; negative prompt: `text, watermark, perspective distortion, vanishing point, characters, UI, blurry` |

---

## 5. Hậu kỳ trước khi đưa vào Unity (bắt buộc)
1. **Đúng kích thước pixel** ở mục 3 (ô nền 128 × 96, dải sông cao 296, cầu 464 × 320, thành 448 × 576…). AI thường trả ảnh 1024 hay 1536 – thu nhỏ/cắt lại trong Krita/Photoshop.
2. **Kiểm tra lặp liền**: dùng bộ lọc *Offset* (Photoshop: Filter > Other > Offset; Krita: Layer > Offset) dịch nửa ảnh – nếu thấy đường nối thì sửa bằng cọ clone.
3. **Nền trong suốt** cho mọi thứ không phải ô nền; xoá viền trắng/viền mờ quanh vật.
4. **Cùng góc nhìn và hướng sáng**: đặt các mảnh cạnh nhau trên một canvas để so trước khi xuất.
5. **Xuất PNG** (hoặc PSD nhiều lớp cho vật có nhiều phần), đặt tên rõ: `ground_grass_01.png`, `river_water_strip.png`, `river_bank_strip.png`, `bridge_wood.png`, `castle_player.png`, `castle_enemy.png`, `prop_tree_01.png`…
6. Gửi lại cho mình (hoặc đặt vào thư mục art khi khung đã sẵn) – mình sẽ cài PPU 128, pivot, Tilemap/SpriteShape.

---

## 6. Nếu gửi cho họa sĩ (mẫu brief)
> Game chiến thuật 2D trên điện thoại, màn ngang, phong cách vẽ tay màu mềm kiểu Clash of Clans. Góc nhìn 3/4 cố định: mặt đất nén dọc còn 0.75 (một ô vuông 1 × 1 trên đất vẽ thành 128 × 96 px), không phối cảnh, sáng từ trên-trái.
> Cần các mảnh riêng (danh sách + kích thước ở mục 3 của `docs/BanDo/MapArtGuide.md`, prompt ở `docs/art/concepts/BanDo_Prompts.md`), nền trong suốt, PSD nhiều lớp hoặc PNG.
> Bố cục bản đồ: `attack_01_mask.png` (màu: xanh lá = cỏ, xanh nước = sông, nâu = cầu, tròn xanh/đỏ = chỗ đặt thành ta/địch). Sông và cầu phải **đúng vị trí và bề rộng** trong ảnh này.

---

## 7. Lưu ý
- Tỉ lệ góc 3/4 (**0.75**) vẫn là 🟡 chờ bạn xác nhận. Nếu đổi, sửa `MapBlueprintExporter.ArtVerticalScale`, xuất lại blueprint và đổi kích thước ô nền (128 × 128·tỉ lệ) **trước khi** vẽ ô nền.
- Khi sửa bố cục ải (sông, cầu, thành…), chạy lại **Arena3Q > Export Map Blueprints** để ảnh gửi đi luôn khớp game.
