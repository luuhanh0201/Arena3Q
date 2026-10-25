# Arena3Q web

Bản Unity WebGL riêng, dùng cùng game và tài nguyên của mobile. Trang bọc dùng nguyên logo, nền, font Be Vietnam Pro, sprite nút và khung Startup UI; không tải thư viện/font từ mạng bên ngoài.

Người chơi chọn **Điện thoại** (20:9), **iPad** (4:3), **PC** (16:9), tất cả theo chiều ngang. Mỗi lựa chọn đặt kích thước render thực của canvas, để Unity tự bố trí HUD/camera và quy đổi chuột/cảm ứng. Trang giữ tỷ lệ khi cửa sổ nhỏ, không kéo méo hình. Chuyển lựa chọn không tải lại Unity hoặc trận; lưu ở localStorage, mặc định lần đầu theo thiết bị. Đây là lựa chọn tỷ lệ hiển thị, không giả lập hệ điều hành hoặc phần cứng.

## Build

Unity 6000.6.6f1, cần Web Build Support. Không mở Editor khác cùng project.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath 'D:/UnityProjects/Arena3Q' -buildTarget WebGL -executeMethod Arena3Q.Editor.WebGameBuild.Build -logFile 'Logs/WebGame-build.log'
```

Menu: **Arena3Q > Build > Web Game (Phone, iPad, PC)**. Cấu hình: [web-settings.json](web-settings.json). Scene đầu giống APK mobile mới nhất: `Battlefield_HighQuality`, kèm Frontend và Battle. Build không đổi danh sách scene mobile hoặc override nén texture. RGBA32 của icon WebGL giữ chất lượng gốc; bộ ASTC mobile độc lập.

Output: `Builds/WebGL/Arena3Q`. `build.json` được sinh theo tên file Unity thực tế; không đoán tên loader hoặc wasm. Gzip có bộ giải nén dự phòng của Unity, dùng được trên máy chủ tĩnh không thiết lập Content-Encoding. Video intro được chép vào `Player/StreamingAssets`, VideoPlayer bản web dùng URL thay cho VideoClip. Các thiết lập template/nén tạm thời được khôi phục sau build.

## Chạy local

```powershell
node WebGame/server.cjs
```

Mở **http://127.0.0.1:4322**. Máy chủ chỉ phục vụ thư mục build, có MIME wasm và HTTP Range cho video, không mở repo/Admin. Có thể đổi cổng bằng `ARENA_WEB_PORT`, bind địa chỉ mạng bằng `ARENA_WEB_HOST` khi cần thử thiết bị trong mạng LAN. Link loopback chỉ dùng trên chính PC; không phải đường dẫn Internet.

Muốn cập nhật riêng trang bọc sau build, chép `index.html`, `style.css`, `player.mjs`, `web-settings.json` từ đây vào thư mục output; không cần build Unity lại.

Đưa toàn bộ output lên máy chủ HTTPS tĩnh để phát hành. Không tải trực tiếp `index.html` bằng file://. Không đồng bộ nguồn ThirdParty lên repo công khai. Tài nguyên lớn và build chỉ có ở máy local, theo quy tắc repo.

## Kiểm tra

```powershell
node WebGame/verify.cjs --ui
node WebGame/verify.cjs
```

Dùng Playwright hiện có trong Admin và Edge đã cài, không thêm package game. Bản `--ui` kiểm trang chọn màn hình trước khi chạy Unity. Bản đầy đủ đợi Unity khởi tạo thật, kiểm 15 kết hợp cửa sổ/tỷ lệ, canvas backing buffer, không tràn trang, giữ lựa chọn và chỉ tải loader một lần. Ảnh/JSON/log tại `NghiemThu/WebGame/20261025`.

Hồ sơ game lưu trong IndexedDB của trình duyệt qua persistentDataPath (bật tự đồng bộ); không dùng chung hồ sơ native mobile. Cửa sổ riêng tư/xóa dữ liệu trang có thể làm mất hồ sơ local. Hỗ trợ hình trên trình duyệt desktop không thay thế kiểm Safari iPad hoặc FPS trên máy thật.
