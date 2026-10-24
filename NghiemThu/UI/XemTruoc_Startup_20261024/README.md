# UI khởi động, bản gửi review ngày 24/10/2026

Trạng thái: đã tự kiểm và gửi review, chưa được Claude nghiệm thu.

- Tài liệu chuẩn: `docs/UI/Arena3Q_UI_Handoff/README.md`, mockup HTML/CSS và canvas trong cùng gói.
- `before/`: Frontend cũ, chụp bằng PlayMode `FrontendFlowTests.Screenshot_FrontendScreens`, 1600×900. Baseline 1/1 qua, `Logs/UI-baseline.xml`.
- `reference/`: ảnh từ các mockup gốc 1280×720, render bằng Chrome với font Be Vietnam Pro trong máy; adapter DC chỉ thay dữ liệu mẫu và vòng lặp/điều kiện. Thanh trạng thái của bản đối chiếu ghi Wi-Fi/giờ mẫu để tránh phụ thuộc runtime DC online.
- `after/`: 19 ảnh Unity sau sửa, gồm 6 trạng thái Loading, login/register, Start, các tab server, bind/link, settings và màn 2340×1080 với cutout; file `*_widgets.txt` lưu trạng thái tương tác thực khi chụp.
- `comparisons/`: ảnh trước/sau và mockup/Unity cùng kích thước, trái = trước hoặc mockup, phải = bản Unity.
- `Overview.png` và `xem_ui.html`: trang xem toàn bộ, đã mở trên Chrome để người dùng kiểm tra. 10 ảnh so sánh, toàn bộ ảnh/liên kết trong trang được kiểm.

Tự kiểm: EditMode **41/41**, PlayMode **15/15**, không fail/skip trong phạm vi UI/Frontend. Log/XML: `Logs/UI-editmode.*`, `Logs/UI-playmode.*`. Kiểm thực tế raycast bấm nút, giữ sprite khi nhấn/trả vị trí, đăng ký/OTP, giữ tài khoản khách khi link thất bại, server full/maintenance/confirm/cancel, retry giữ 63%, nhạc loop/âm lượng/mute/prefs, UI trong safe area và nền phủ toàn canvas. Các bước chọn tướng/tên/truyện/trận hướng dẫn hồi quy qua.

Nhạc: `OGG - in-game loop file.ogg` người dùng giao bổ sung, bắt đầu từ Loading, lặp xuyên các màn Frontend và màn Vào game; mặc định 12%, có thanh chỉnh và tắt riêng trong Cài đặt, lưu PlayerPrefs. Tắt khi rời Frontend vào Battle.

Giới hạn đã biết: tài khoản guest và dữ liệu cục bộ; API email/social/OTP/liên kết/tải mạng thật và nội dung pháp lý chưa được cung cấp. Không hiển thị tải mạng giả, không tạo nhân vật/cấp/quà giả. Video và OGG lớn giữ local theo quy tắc repo. Chưa kiểm thiết bị Android/iOS hoặc tuyên bố đạt 60 FPS.
