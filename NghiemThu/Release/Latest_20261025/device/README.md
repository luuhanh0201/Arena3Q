# Cài bản Latest trên Redmi Note 8 Pro

Ngày25/10/2026, theo yêu cầu trực tiếp "cài đi".

- Thiết bị: Redmi Note 8 Pro, ADB serial `pr7drgnnij6xaa7d`.
- APK: `Builds/Android/Arena3Q-Latest-20261025.apk`,263,4MB; cùng gói và chữ ký bản trước. Dùng `adb install -r`, kết quả **Success**, không xóa dữ liệu.
- Package `com.Arena3Q.Arena3Q`: firstInstallTime giữ `2026-10-20 22:55:16`, lastUpdateTime đổi từ `2026-10-25 13:52:24` sang `2026-10-25 19:54:43`.
- Mở `UnityPlayerGameActivity`: Status ok, LaunchState COLD, TotalTime509ms, WaitTime512ms; PID3861 vẫn chạy sau chụp ảnh/lấy log.
- `startup.png` là ảnh từ điện thoại: đã ở ải `attack_01` trên map mới, có thẻ Mưu sĩ/Giáp Sĩ/Xạ Thủ/Lữ Bố. Không điều khiển chọn tướng, đổi hồ sơ hoặc chơi hộ người dùng.
- Đoạn log250dòng đã lấy không có FATAL EXCEPTION/Fatal signal/Exception; chỉ xác nhận lần mở này. Chưa đo FPS hoặc kiểm hết trận.

Bằng chứng local: `install.txt`, `launch.txt`, `package-before.txt`, `package-after.txt`, `startup.png`, `startup-logcat.txt`. Chỉ README được commit, ảnh/log giữ local. Không theo dõi thiết bị/inbox sau bàn giao.
