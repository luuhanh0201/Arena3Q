# Arena3Q web

Bản Unity WebGL riêng, dùng cùng game và tài nguyên của mobile. Trang bọc dùng nguyên logo, nền, font Be Vietnam Pro, sprite nút và khung Startup UI; không tải thư viện/font từ mạng bên ngoài.

Người chơi chọn **Điện thoại** (20:9), **iPad** (4:3), **PC** (16:9), tất cả theo chiều ngang. Mỗi lựa chọn đặt kích thước render thực của canvas, để Unity tự bố trí HUD/camera và quy đổi chuột/cảm ứng. Trang giữ tỷ lệ khi cửa sổ nhỏ, không kéo méo hình. Chuyển lựa chọn không tải lại Unity hoặc trận; lưu ở localStorage, mặc định lần đầu theo thiết bị. Đây là lựa chọn tỷ lệ hiển thị, không giả lập hệ điều hành hoặc phần cứng.

## Build

Unity 6000.6.6f1, cần Web Build Support. Không mở Editor khác cùng project.

Khi cập nhật nội dung campaign, chạy menu **Arena3Q > Build > Apply Latest Game to Mobile and Web** hoặc CLI `-executeMethod Arena3Q.Editor.CurrentGameRelease.Apply` trước. Manifest chung: `Assets/Arena3Q/Scripts/Editor/GameRelease.asset`. Ba stage campaign dẫn xuất lấy bố cục map mới và giữ luật/chỉ số từ stage gốc; cả hai builder từ chối Frontend còn trỏ map cũ.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath 'D:/UnityProjects/Arena3Q' -buildTarget WebGL -executeMethod Arena3Q.Editor.WebGameBuild.Build -logFile 'Logs/WebGame-build.log'
```

Menu: **Arena3Q > Build > Web Game (Phone, iPad, PC)**. Cấu hình trang: [web-settings.json](web-settings.json). Luồng đầy đủ: Frontend (intro, tải, đăng nhập, máy chủ, chọn tướng, đặt tên, truyện) rồi **Battlefield_HighQuality** theo hồ sơ. Nhân vật mới chơi tutorial trên map mới; nhân vật cũ vào ải mặc định trên cùng map, giữ cấp/phần thưởng. Scene Battle cũ chỉ giữ làm nguồn/test. RGBA32 của icon WebGL giữ chất lượng gốc, độc lập ASTC mobile.

Output: `Builds/WebGL/Arena3Q`. `build.json` ghi tên file Unity thực tế, hash nội dung, startupScene và battleScene; URL payload có hash để cache cũ không thay thế bản mới. Gzip có bộ giải nén dự phòng, dùng được trên máy chủ tĩnh không thiết lập Content-Encoding. Video intro được chép vào `Player/StreamingAssets`, VideoPlayer bản web dùng URL. Thiết lập template/nén tạm được khôi phục sau build.

Mobile chung nội dung: `-executeMethod Arena3Q.Editor.BattlefieldAndroidBuild.BuildMobile` (hoặc BuildScripts.BuildAndroidDev), APK hiện tại `Builds/Android/Arena3Q-Latest-20261025.apk`. Menu thử map riêng vẫn là công cụ chẩn đoán.

## Chạy local

```powershell
node WebGame/server.cjs
```

Mở **http://127.0.0.1:4322**. Máy chủ phục vụ thư mục web build và đúng một APK được cấu hình tại `/download/Arena3Q-Latest-20261025.apk`, không mở repo/Admin. Có MIME wasm và HTTP Range. Có thể đổi cổng bằng `ARENA_WEB_PORT`, bind mạng bằng `ARENA_WEB_HOST` để thử LAN. Link loopback chỉ dùng trên PC này.

Muốn cập nhật riêng trang bọc sau build, chép `index.html`, `style.css`, `player.mjs`, `web-settings.json` từ đây vào thư mục output; không cần build Unity lại.

Đưa toàn bộ output lên máy chủ HTTPS tĩnh để phát hành. Không tải trực tiếp `index.html` bằng file://. Không đồng bộ nguồn ThirdParty lên repo công khai. Tài nguyên lớn và build chỉ có ở máy local, theo quy tắc repo.

## Kiểm tra

```powershell
node WebGame/verify.cjs --ui
node WebGame/verify.cjs
node WebGame/flow-qa.cjs
node WebGame/flow-qa.cjs --touch
node WebGame/flow-qa.cjs --intro-only
node WebGame/make-review.cjs
node WebGame/flow-qa.cjs --latest
node WebGame/flow-qa.cjs --latest --touch
node WebGame/release-review.cjs
node WebGame/verify-release.cjs
node WebGame/lobby-qa.cjs
node WebGame/lobby-qa.cjs --show
```

Dùng Playwright hiện có trong Admin và Edge đã cài, không thêm package game. Bản `--ui` kiểm trang chọn màn hình trước khi chạy Unity. Bản đầy đủ đợi Unity khởi tạo thật, kiểm 15 kết hợp cửa sổ/tỷ lệ, canvas backing buffer, không tràn trang, giữ lựa chọn và chỉ tải loader một lần. Ảnh/JSON/log tại `NghiemThu/WebGame/20261025`.

`flow-qa.cjs` bấm/nhập bằng chuột và bàn phím qua luồng Frontend thật, kiểm video được tải bằng URL và hồ sơ Unity thực sự được ghi vào IndexedDB. Nhánh web của HUD dùng font nhúng từ `Content/Resources/UiTypography.asset` tham chiếu font mobile hiện có; không phụ thuộc bộ font dự phòng của hệ điều hành.

`lobby-qa.cjs` mở bản WebGL thật bằng Edge trong hồ sơ trình duyệt thử riêng, tắt tiếng. Sau lần tải đầu, công cụ đặt hồ sơ QA Mưu sĩ vào đúng kho IndexedDB của game, tải lại và bấm Vào game để kiểm Sảnh ở ba chế độ PC/điện thoại/iPad. Số dư/VIP mẫu chỉ nằm trong hồ sơ QA đó; không sửa dữ liệu phát hành hoặc hồ sơ trình duyệt đang dùng. Ảnh/log/JSON ở `NghiemThu/WebGame/Lobby_20261027`; tùy chọn `--show` giữ cửa sổ game mở sau khi kiểm thành công để người dùng xem trực tiếp.

Hồ sơ game lưu trong IndexedDB của trình duyệt qua persistentDataPath (bật tự đồng bộ); không dùng chung hồ sơ native mobile. Cửa sổ riêng tư/xóa dữ liệu trang có thể làm mất hồ sơ local. Hỗ trợ hình trên trình duyệt desktop không thay thế kiểm Safari iPad hoặc FPS trên máy thật.
