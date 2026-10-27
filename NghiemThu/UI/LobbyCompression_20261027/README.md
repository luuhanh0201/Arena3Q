# Nén UI/sảnh mobile, 27/10

Đã áp dụng và kiểm import bằng Unity với active target Android. Chưa build APK mới trước vòng review sảnh.

- Snapshot trước sửa: `before-platforms.json` và `before-meta/`, 101 ảnh có GUID/SHA256.
- `offline-after-verification.json`: 100 ảnh nguồn giữ nguyên SHA256; avatar sinh lại từ cùng model ở 512x512, hash khớp snapshot import mới. Toàn bộ 101 GUID giữ nguyên. Avatar là thay đổi chủ ý của bộ trình bày, không phải nén làm đổi PNG nguồn.
- Atlas LobbyUI có override Android/iOS ASTC 4x4, max 2048. Texture UI/nền mip tắt. Hai icon Heroicons mới được Unity nhập và cùng áp dụng chính sách nén.
- Unity chuẩn hóa tên API iPhone thành iOS trong metadata; override iOS có sẵn của sảnh trước đó là hợp lệ. Không tạo entry iPhone trùng.
- Nền 1920x1080 giữ ASTC 4x4, max 2048. Màn rộng phóng/cắt nền theo khung, không tăng độ phân giải ảnh nguồn.
- Resources đổi thành ResourceBar, giữ GUID folder/sprite và các đường dẫn JSON; nguồn rig/art cũ được giữ.
- Không đổi nén icon kỹ năng đã chốt, font hoặc video.

`after-import-settings.json` ghi định dạng import thực, platform settings, kích thước và ước tính ASTC. Các ảnh mẫu GPU nằm trong thư mục được ghi ở `latest-after-capture.txt`. Đây là kiểm import trên Editor với target Android, chưa phải ảnh chạy trên thiết bị hay packed bytes của APK.

Công cụ `LobbyCompressionQa` có ApplyAndCapture, BuildBaseline, BuildOptimized và VerifyBuild. Hai build dùng cùng scene/source release, chỉ đổi chính sách nén; baseline được phục hồi về ASTC trong finally. BuildReport ghi packed bytes theo nhóm và chặn code/scene/catalog/rig/atlas legacy. Đường release chung là CurrentGameRelease, WebGameBuild và BattlefieldAndroidBuild; builder APK sảnh cũ đã nghỉ.

Còn chờ sau review: build Before/After, so chất lượng runtime Android cùng khung, bảng packed bytes và chênh dung lượng APK. Chưa có số đo FPS máy thật. Không dùng ước tính ASTC hoặc metadata để thay cho kết quả BuildReport.
