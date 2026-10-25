# Sửa mobile thu nhỏ lộ nền đen 25/10/2026

Người dùng phát hiện lỗi trên Redmi Note 8 Pro: camera thu nhỏ vượt mặt đất, lộ vùng clear màu đen ở bốn phía. Ảnh gốc máy thật giữ tại `before/zoom-out.png`; `before/navigation-bar.png` còn cho thấy thanh điều hướng hệ thống che mép HUD.

Nguyên nhân: `allowMapOverview` bỏ giới hạn zoom theo bounds; `FitCamera` cố chứa toàn map kèm padding. Chỉ chặn tâm camera không xử lý được viewport lớn hơn mặt đất. Camera chơi nay giới hạn cả kích thước lẫn tâm theo bounds đã chiếu, kể cả lúc khởi tạo, resize, đổi góc và fit từ bên ngoài. Bỏ bypass trong scene và builder. Không thêm terrain ngoài biên, không sửa model hoặc luật/chỉ số trận.

HUD dùng vùng an toàn cập nhật khi thanh hệ thống xuất hiện/ẩn, giữ style sẵn có. Camera dùng toàn vùng render, HUD dùng safe area riêng.

Nguồn sửa: `39d0d00` đã push. PlayMode **17/17 qua**, không lỗi/skip, 90,3giây: `BattlefieldViewportTests`, `FrontendFlowTests`, `BattlefieldHighQualityTests`. Log/XML: `Logs/Mobile-screen-play.log`, `Logs/Mobile-screen-play.xml`.

Kiểm hồi quy giữ controller camera thật: ray bốn góc viewport nằm trong map ở tỷ lệ Redmi 2340/1080, 20:9, 16:9, 4:3; góc 0/40/45 độ; zoom rất xa, pan bốn hướng, đổi aspect và fit ngoài. Kiểm Stage/Timer/Info/CardBar trong vùng an toàn giả lập rồi trả lại toàn màn hình.

APK trước sửa được giữ riêng: `Builds/Android/Arena3Q-Latest-before-screenfix-20261025.apk` (276.167.265 byte). Bản sửa tiếp tục dùng tên tải `Arena3Q-Latest-20261025.apk`.

APK sửa đã build thành công: **263,4MiB** (276.168.981 byte, tăng1.716 byte),350,3giây,0lỗi/8cảnhbáo. SHA256: `56abe240f754db49dbfa3f9fb9c12fc46914baa8290db498b268865dc33ce2a5`. Chữ kýv2, cùng signer/package với bản trước, ARM64. `Android-content.json` xác nhận Frontend và stage campaign mới trong gói; tải HTTP HEAD200/Range206 đúng dung lượng/chữ kýZIP. Bằng chứng `android-build.txt`, `android-signature.txt`, `delivery-verification.json`.

Đã cài cập nhật thành công (`device/install.txt`); `lastUpdateTime=2026-10-25 20:18:00`, `firstInstallTime=2026-10-20 22:55:16` giữ nguyên. Khi mở game, điện thoại đang tắt màn hình (`mWakefulness=Asleep`, Display OFF), tiến trình8391 bị hệ thống pause trước render; đã yêu cầu người dùng bật/mở khóa máy để chụp lại. Chưa kết luận hình máy thật sau sửa hoặc hiệu năng từ test tự động.
