# Arena3Q – mục lục tài liệu

Quy ước sắp xếp:
- **Gốc `docs/`** chỉ có mục lục này và spec gameplay. **Mọi quy tắc và nhật ký** (quy tắc làm việc, luật 3D, nhật ký vận hành 3D, nhật ký sửa lỗi) nằm trong **`QuyTac/`**. Tài liệu khác nằm trong **thư mục theo chủ đề**.
- **Plan mới** (chờ duyệt) để trong **`KeHoach/`**. Duyệt xong thì chuyển đi: luật vào spec, thiết kế vào thư mục chủ đề, prompt vào `art/concepts/`.
- **Prompt dán sẵn** nằm cạnh ảnh mẫu trong `art/concepts/`; riêng quân địch để chung trong `QuanDich/` (theo yêu cầu lúc tạo thư mục đó).
- Ký hiệu trong tài liệu: ✅ đã chốt · 🟡 mặc định tạm (là tuỳ chọn cấu hình) · ⏳ để sau.

| Đường dẫn | Nội dung |
|---|---|
| `GameplaySpecification.md` | **Spec gameplay – nguồn đúng duy nhất.** Mâu thuẫn với tài liệu khác thì theo file này. |
| **`QuyTac/`** | **Mọi quy tắc và nhật ký rút kinh nghiệm** (đọc `QuyTac/README.md` trước khi làm) |
| `QuyTac/QuyTacLamViec.md` | Quy tắc làm việc người dùng đã giao cho mọi agent: giao tiếp, commit/push, dữ liệu dễ sửa, asset nhỏ / không LFS, lưu trữ thay vì xoá, ảnh trước–sau, 60 FPS, VFX "khí", hai agent, review / nghiệm thu. |
| `QuyTac/Model3D_Rules.md` | Luật bắt buộc khi sửa model 3D, skinning, animation và VFX; lỗi đã gặp và các bước tự kiểm tra trước bàn giao. |
| `QuyTac/NhatKyVanHanh_3D.md` | Nhật ký vận hành 3D / animation / VFX (mọi agent): hướng sai, lỗi, bug gặp khi làm – nguyên nhân, cách phát hiện, quy tắc phòng tránh. Ghi ngay khi gặp. |
| `QuyTac/NhatKySuaLoi.md` | Nhật ký sửa lỗi gameplay / code: nguyên nhân gốc và cách tránh lặp lại. |
| **`UI/`** | **UI khởi động**: gói `Arena3Q_UI_Handoff`, mockup HTML/CSS, `Implementation.md` (kiến trúc, QA, giới hạn API), `StartupCopy.tsv` và công cụ chuẩn bị/đối chiếu ảnh. Nhạc nền bắt đầu từ Loading, chỉnh/tắt trong Cài đặt. |
| `UI/GameWeb_20261025.md` | Bản game WebGL với tỷ lệ Điện thoại/iPad/PC, style mobile, luồng vào game đầy đủ; build/chạy local, QA trình duyệt và giới hạn thiết bị. |
| **`KeHoach/`** | **Plan chờ duyệt** |
| `KeHoach/VaoGame.md` | Luồng vào game: biểu tượng → intro 3 s (logo + banner) → đăng nhập / Chơi ngay → chọn máy chủ → chọn chủ tướng + đặt tên → cốt truyện mở đầu; kèm prompt ảnh, video, tiếng, nhạc. |
| `KeHoach/HeroProgression.md` | Cấp (tối đa 60), chủ tướng học kỹ năng theo cấp, sao, đột phá, phe Thục / Ngụy / Ngô / Quần, bảng chỉ số và cách cân bằng các hệ. |
| `KeHoach/ChuyenSang3D.md` | Kế hoạch chuyển hiển thị sang 3D (logic giữ mặt phẳng 2D): ngân sách 60 FPS trên Redmi Note 8 Pro, camera, 7 giai đoạn (P0 làm thử → map → nhân vật → VFX → Frontend → test → tài liệu), model cần cung cấp, rủi ro, câu hỏi cần chốt. |
| `KeHoach/KetQuaDoHieuNang_3D.md` | Kết quả đo hiệu năng 3D trên máy thật (Redmi Note 8 Pro): FPS, p95, lượt vẽ, nhiệt, cách chạy bộ đo `BattleBenchmark`, giới hạn và việc còn mở. |
| **`Tuong/`** | **Tướng** |
| `Tuong/LordHeroes.md` | 4 chủ tướng (Cung thủ, Kiếm sĩ, Mưu sĩ, Thống soái): vai trò, chỉ số, bộ kỹ năng. |
| `Tuong/NormalHeroes.md` | 4 tướng khởi đầu (Xạ Thủ, Kỵ Binh, Giáp Sĩ, Đao Khách): kỹ năng, lớp PSD, xương, clip. |
| `Tuong/LuBu.md` | Lữ Bố: rig 73 xương, sửa weight/tư thế/da, chỗ cầm kích, 7 animation, sprite tám hướng (trang 2048) và cách đưa vào game. |
| `Tuong/LordAnimations.md` | Animation 4 chủ tướng: lớp PSD, cây xương, clip, mốc `Hit` / `Cast`, VFX theo sự kiện. |
| `Tuong/LordArtPrompts.md` | Tổng quan làm hình chủ tướng: công cụ, quy trình, khối phong cách, đặt tên lớp, link tới các file prompt. |
| `Tuong/StrategistArtGuide.md` | Mẫu quy trình cho mọi chủ tướng (làm trên Mưu sĩ): Krita, tách lớp, gắn xương, phần mình code. |
| **`BanDo/`** | **Bản đồ** |
| `BanDo/BattlefieldHighQuality_20261025.md` | Scene chiến trường3D riêng, cấu hình/prefab, PvE, navigation, gió/LOD/instancing, bằng chứng kiểm và giới hạn còn mở. |
| `BanDo/MapSpecification.md` | Kích thước, bố cục, vùng cấm, sông và cầu, quy cách art bản đồ; mục 8.7 = hiển thị 3D (mặt đất mesh + ảnh nền vẽ nhìn thẳng, đạo cụ 3D). |
| `BanDo/BatQuaiTran.md` | **Map Bát Quái Trận** (bản đồ hiện hành của mọi ải): vòng 8 cột, 8 cửa Bát Môn (Sinh / Khai / Tử / Đỗ), xoay trận mỗi 20 s, đài tâm chiếm trận nhãn, hình ảnh, debug và test. |
| `BanDo/MapArtGuide.md` | Cách gửi file và viết yêu cầu vẽ bản đồ: file đính kèm, kích thước mảnh, hậu kỳ. |
| **`QuanDich/`** | **Quân địch khi đi ải** – `README.md` (cấp bậc, luật hình ảnh), `Chuong1_SonTac.md`, `Chuong2_HoangCan.md`, `Chuong3_YeuQuai.md` (từng loại địch, chỉ số, kỹ năng, prompt), `Animation.md` (khung xương, clip, mốc cảnh báo / ra đòn), `VFX_Prompts.md`; ảnh lưu trong `QuanDich/art/`. |
| **`art/concepts/`** | **Mọi prompt dán sẵn** + ảnh mẫu `chutuong1.png` + ảnh đã tạo: 4 chủ tướng (`CungThu_`, `KiemSi_`, `MuuSi_`, `ThongSoai_Prompts.md`), tướng khởi đầu (`TuongThuong_Prompts.md`), bản đồ (`BanDo_Prompts.md`), `VFX_Prompts.md`, `Icon_Prompts.md`, `ChanDung_Prompts.md`. |
| **`art/blueprints/`** | Bản vẽ bản đồ xuất tự động từ dữ liệu ải (menu `Arena3Q > Export Map Blueprints`) – không sửa tay. |
