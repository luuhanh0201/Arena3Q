# WebGL sảnh tích hợp 27/10

Bản game đã build và mở tại http://127.0.0.1:4322, giữ cửa sổ Edge tắt tiếng ở Sảnh PC cho người dùng xem. Đây là link local trên máy này.

- Unity build thành công, 0 lỗi, 8 cảnh báo, 665,3 giây. Log: `Logs/WebGame-lobby-build.log`; kết quả chép riêng tại `build.txt`.
- Gói web 426.593.103 byte = **406,8 MB**, tăng **67,1 MB** so bản cũ339,7MB. Output `Builds/WebGL/Arena3Q`. Đây là tổng thư mục web, không phải dung lượng APK hoặc tất cả lượng tải trong mỗi lần mở.
- Payload hash: `0bb12923db2bc1d0b0ed8322c8e8e76c676d4a208b6e5daf8bb87c0a87321f96`, khác bản cũ. Scene Frontend vào Sảnh mới, trận dùng Battlefield_HighQuality. Nguồn sảnh7d89a90; wrapper sửa bổ sung theo lỗi người dùng báo trong lượt này.

## Sửa cửa sổ và chế độ hiển thị

Người dùng chụp cửa sổ có dải trắng phải và tải82%. Lượt xem thử đầu dùng viewport cố định1600trong cửa sổ rộng1900; QA còn tải lại game để đặt hồ sơ thử. Game thực đã tải vào Sảnh, không có pageerror/consoleerror. Đổi cửa sổ hiển thị sang viewport thật, bật sandboxChromium để bỏ cảnh báo cờ khởi chạy, tái dùng hồ sơ thử để tránh lần tải lại không cần thiết.

Fixture ban đầu dùng selector data-mode bắt cả button và game-frame. Sau sửa selector, kiểm thực phát hiện gán canvas.width trong chế độ tắt autoresize bị Unity đưa về kích thước cũ. Wrapper nay bật autoresizeUnity và tính pixelratio theo kích thước mode/vùng canvas đãfit; giữ CSSfractional để không sai tỉ lệ. Không cần build lại mã game. Bản wrapper đã chép vào output đang phục vụ.

Lượt cuối `node WebGame/lobby-qa.cjs --show` đạt, `lobby-browser.json`: điện thoại1280×576, iPad1024×768, PC1600×900; 6ảnh, 0pageerror/consoleerror. Cửa sổ thật inner1912/outer1920. Các lượt fixture thất bại không được tính đạt. Ảnh cũ/capture người dùng giữ tại `first-attempt` và `browser-window-before.png`; ảnh sau ở `04-lobby-pc.png`, `04-lobby-phone.png`, `04-lobby-ipad.png`.

## Hồ sơ thử và giới hạn

Hồ sơ trình duyệt riêng nằm tại `browser-profile`; fixture ghi hồ sơ Mưu sĩ Lâm Tử Yên/VIP12/số dư mẫu vào kho IndexedDB của chính game trong hồ sơ QA đó. Không thay nội dung phát hành hoặc hồ sơ trình duyệt người dùng. Lượt sau mở lại dùng hồ sơ thử đã lưu. Cửa sổ test tắt tiếng bằng tham số trình duyệt.

Kiểm này xác nhận build WebGL chạy, vào Sảnh và đổi đúng kích thước render; không thay nghiệm thu Safari iPad hoặc FPS thiết bị thật. Còn bốn điểm hoàn thiện từ review93% (tint model đêm, độ rộng tooltip, mũi tên, tương phản StatusBar) đã ghi nhận riêng, chưa áp dụng ở lượt build này. Chưa build APK vòng mới.

Ảnh, build, hồ sơ trình duyệt và log giữ local theo quy tắc repo; chỉ code/công cụ và README vào Git. Không tự theo dõi inbox sau bàn giao.
