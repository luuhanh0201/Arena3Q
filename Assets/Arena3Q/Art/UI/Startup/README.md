# UI khởi động Arena3Q

Nguồn ảnh, video và 6 SFX: gói `docs/UI/Arena3Q_UI_Handoff` người dùng cung cấp. Code chuẩn bị: `docs/UI/Tools/prepare-assets.cjs`. Background giảm về 1280×720, logo 1024 px; giữ bố cục, alpha và màu của art. `Generated/` chứa các phần SVG xe lương/mũi giáo lấy trực tiếp từ mockup, nền mờ, dải tối và hình chữ nhật bo góc. Alpha nền được bù cho pipeline Linear để gần mockup CSS sRGB.

Icon trạng thái: Lucide, giấy phép ISC ở `Lucide/LICENSE.txt`; SVG chuẩn hóa stroke trắng vì Unity không hỗ trợ currentColor, PNG 128 px dùng trong game.

Font: Be Vietnam Pro từ kho Google Fonts, 4 weight Regular/SemiBold/Bold/ExtraBold, giấy phép OFL tại `Fonts/OFL.txt`. SDF dynamic chứa font nguồn; các atlas cache được xóa khi thoát PlayMode để tránh lưu nhiều MB ảnh trùng trong git. Cần khởi động/hiện đủ các màn trước khi đo hiệu năng vì glyph được điền lần đầu.

`TMP/` là shader, style và bảng xuống dòng thuộc TMP Essential Resources trong uGUI đi kèm Unity, trích bằng `docs/UI/Tools/prepare-tmp.cjs`; không nhập font/sprite demo. Giấy phép nằm cạnh thư mục.

Video MP4 (3,0 MB) và OGG (4,0 MB) để local theo quy tắc repo chỉ commit code/asset nhỏ. Khi chuyển máy, chép lại hai file từ gói người dùng vào `Video/`, giữ .meta trên máy khi di chuyển, rồi chạy `Arena3Q > UI > Import Startup Handoff` để bind. Không đổi tên file.

Nhạc OGG streaming, loop, âm lượng mặc định 65%, phát từ Loading xuyên các màn Frontend; tự dừng khi vào Battle. Cài đặt nhạc/âm thanh lưu PlayerPrefs, không lưu mật khẩu. Giữ âm lượng người dùng đã chỉnh.

Theo yêu cầu bổ sung, bỏ đúng 45 giây đầu nhạc nền: chạy `docs/UI/Tools/prepare-bgm.ps1` để tạo `Video/BGM_From45s.wav` từ OGG gốc (5:00 thành 4:15). WAV là nguồn local 42,9 MiB, không commit; Unity nhập thành Vorbis streaming nên APK không chứa WAV PCM lớn. Giữ `.meta` và dùng Import Startup Handoff để bind. Cắt trên mẫu âm thanh sau giải mã, không copy packet OGG vì packet có thể kéo theo khoảng preroll; cả phát lần đầu và loop đều chỉ chứa phần từ giây 45 trở đi. OGG gốc giữ nguyên.

Intro to hơn theo yêu cầu 24/10: chạy `docs/UI/Tools/prepare-intro-audio.ps1` (FFmpeg có trong Krita, có thể truyền đường dẫn khác). File `Arena3Q_Splash_loud.mp4` là bản dẫn xuất local, không commit; giữ `.meta` để GUID không đổi. Script nhân audio 2 lần, limiter đỉnh, bù độ trễ limiter, sao chép nguyên video và bổ sung thông tin BT.709. Hash từng khung giải mã trước/sau giống nhau cả 90 khung. `StartupUiBuilder` bind bản loud; giữ nguyên MP4 gốc.

Icon launcher dùng chính `UI/Logo/logo_arena3q.png`, chuẩn bị bằng `docs/UI/Tools/prepare-app-icon.cjs`, gắn bằng `Arena3Q > UI > Apply Supplied Logo as App Icon`. Logo giữ tỷ lệ; legacy có nền navy, adaptive tách nền/logo và giữ logo trong vùng an toàn.
