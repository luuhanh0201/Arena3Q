# Build web ngày 28/10/2026

Build từ nguồn local hiện tại trên nền commit `a831d8604c73a9ac2323640f54cecec7073b4005`, gồm các chỉnh sửa độ nét WebGL và sảnh. Không chỉnh nội dung game trong lượt build này.

- Output: `Builds/WebGL/Arena3Q`; mở tại http://127.0.0.1:4322.
- Kết quả: Succeeded, 0 lỗi, 8 cảnh báo; 523,1 giây.
- Dung lượng toàn output: 415.301.473 byte = **396,1 MB**, giảm **10,8 MB** so với 426.594.193 byte = 406,8 MB trước build.
- Log: `Logs/WebGame-build-20261028-0227-retry.log`. Lượt sandbox trước đó không truy cập được giấy phép Unity; giữ log `Logs/WebGame-build-20261028-0227.log`, chạy lại ngoài sandbox thành công.
- Scene đầu: `Frontend`; trận: `Battlefield_HighQuality`.
- Hash dữ liệu: `d842b9732b6364525fac47d11eb02403552840afbe478a33b526315b0fcf139e`.

Kiểm đóng gói đã qua: bốn SHA256 payload khớp URL trong manifest, bốn file wrapper khớp nguồn, manifest HTTP khớp output và đúng scene trận. Bằng chứng local: `build.txt`, `build.json`, `previous-build.json`, `package-verification.json`.

Kiểm trình duyệt toàn luồng **63/63 trường hợp qua**, 69 ảnh, 6 lần resize, 0 pageerror/consoleerror. Bằng chứng: `NghiemThu/WebGame/Clarity_20261028/after_2026-10-27T19-46-30-710Z/result.json`. Bao gồm Login, Server, LordSelect, Name, Story, Battle và Lobby, mỗi màn ở ba mode và DPR1/1,25/2. Đã mở ảnh sảnh PC ở cả ba DPR để kiểm đúng model, tên, ví và VIP của hồ sơ QA. Có chín đợt lấy mẫu RAF và `GetMetricsInfo` riêng; mẫu ngắn trong Edge tự động không chứng minh đạt 60 FPS trên thiết bị.

Lượt đầu dừng ở assertion lấy FPS của công cụ QA sau 19 trường hợp; chưa có pageerror/consoleerror. Không ghi nhận lượt đó là đạt. Đã sửa cách bắt instance sau khi loader khai báo hàm global; lưu cả dữ liệu đo trước khi assert để chẩn đoán. Xem ảnh còn phát hiện thiếu thao tác Chơi ngay trước Vào game khiến ảnh mang tên Lobby thực tế là Login; sửa đường đi trong cả hai công cụ. Các lượt trung gian bị dừng không tính vào bằng chứng đạt. Không dùng ảnh Lobby của baseline cũ để kết luận so sánh hình trước/sau.

`node WebGame/lobby-qa.cjs --show` qua ba mode, sáu ảnh, 0 pageerror/consoleerror; cửa sổ Edge có sandbox, tắt tiếng và dùng hồ sơ QA riêng đang mở ở sảnh PC. Kết quả: `NghiemThu/WebGame/Lobby_20261027/lobby-browser.json`. Không sửa hồ sơ trình duyệt của người dùng. Hai công cụ qua `node --check`; `git diff --check` không có lỗi khoảng trắng.

Unity đã thoát và nhả khóa. Build, ảnh và JSON bằng chứng giữ local theo quy tắc repo; README và công cụ nhỏ được đưa vào Git.
