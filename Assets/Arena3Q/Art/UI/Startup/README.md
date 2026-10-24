# UI khởi động Arena3Q

Nguồn ảnh, video và 6 SFX: gói `docs/UI/Arena3Q_UI_Handoff` người dùng cung cấp. Code chuẩn bị: `docs/UI/Tools/prepare-assets.cjs`. Background giảm về 1280×720, logo 1024 px; giữ bố cục, alpha và màu của art. `Generated/` chứa các phần SVG xe lương/mũi giáo lấy trực tiếp từ mockup, nền mờ, dải tối và hình chữ nhật bo góc. Alpha nền được bù cho pipeline Linear để gần mockup CSS sRGB.

Icon trạng thái: Lucide, giấy phép ISC ở `Lucide/LICENSE.txt`; SVG chuẩn hóa stroke trắng vì Unity không hỗ trợ currentColor, PNG 128 px dùng trong game.

Font: Be Vietnam Pro từ kho Google Fonts, 4 weight Regular/SemiBold/Bold/ExtraBold, giấy phép OFL tại `Fonts/OFL.txt`. SDF dynamic chứa font nguồn; các atlas cache được xóa khi thoát PlayMode để tránh lưu nhiều MB ảnh trùng trong git. Cần khởi động/hiện đủ các màn trước khi đo hiệu năng vì glyph được điền lần đầu.

`TMP/` là shader, style và bảng xuống dòng thuộc TMP Essential Resources trong uGUI đi kèm Unity, trích bằng `docs/UI/Tools/prepare-tmp.cjs`; không nhập font/sprite demo. Giấy phép nằm cạnh thư mục.

Video MP4 (3,0 MB) và OGG (4,0 MB) để local theo quy tắc repo chỉ commit code/asset nhỏ. Khi chuyển máy, chép lại hai file từ gói người dùng vào `Video/`, giữ .meta trên máy khi di chuyển, rồi chạy `Arena3Q > UI > Import Startup Handoff` để bind. Không đổi tên file.

Nhạc OGG streaming, loop, âm lượng mặc định 12%, phát từ Loading xuyên các màn Frontend; tự dừng khi vào Battle. Cài đặt nhạc/âm thanh lưu PlayerPrefs, không lưu mật khẩu.
