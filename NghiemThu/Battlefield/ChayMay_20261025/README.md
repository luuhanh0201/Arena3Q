# Cài và mở APK trên Redmi Note 8 Pro ngày25/10/2026

- Người dùng yêu cầu trực tiếp chạy trên điện thoại.
- Thiết bị ADB authorized: Redmi Note8Pro/begonia, Android11, độ phân giải1080×2340, màn hình bật và không khóa trước khi cài.
- APK: `Builds/Android/Arena3Q-Battlefield-20261025.apk`,382.807.684byte, SHA256 `6aa9abe43eedb39539a11ef54db76116d92877cb6aa6e609e444b3a0b95816d4`.
- `adb install -r` trả **Success**, cài cập nhật, không xóa dữ liệu app.
- Resolve activity: `com.Arena3Q.Arena3Q/com.unity3d.player.UnityPlayerGameActivity`.
- `am start -W` trả **Status:ok**, **LaunchState:COLD**, TotalTime550ms/WaitTime554ms; ngay sau đó PID16472 tồn tại.
- Khi chuẩn bị đọc foreground/screencap/logcat, ADB báo thiết bị không còn kết nối. Lần kiểm tiếp `adb devices -l` cũng không có thiết bị. ADB server vẫn chạy, không có Unity đang đóng ADB.

**Kết luận:** cài và lệnh mở Activity đã thành công. Chưa xác minh hình map thực trên màn hình, log Unity sau khởi động, input hoặc FPS. Không suy ra game đã crash hoặc chạy ổn từ việc USB mất kết nối. Không có ảnh điện thoại được chụp trong lượt này.
