# Đồng bộ phần mới nhất lên web và mobile 25/10/2026

Đã hoàn tất hai gói và kiểm bản phát hành. Nguồn tích hợp `c1b55a0` đã push.

Lỗi bản trước: web có scene map mới trong gói nhưng Frontend vẫn mở Battle cũ; mobile tối ưu mở scene thử map trước Frontend. Bản sửa dùng chung manifest `Assets/Arena3Q/Scripts/Editor/GameRelease.asset`: **Frontend -> Battlefield_HighQuality**, giữ lựa chọn chủ tướng và hồ sơ của người chơi.

Ba stage dẫn xuất dưới `Content/Battlefield/Campaign` lấy geometry, điểm triệu hồi, vị trí thành/quái từ map mới. StageId, loại ải, máu/công/tầm đánh thành, số quái, lịch đợt, thời gian, luật thắng/thua và thưởng tutorial giữ từ stage gốc. Không sửa nguồn stage cũ. Bản test map riêng vẫn dùng được, nút đổi ải thử bị ẩn trong campaign để không bỏ qua tiến trình.

Web giữ ba tỷ lệ Điện thoại/iPad/PC và style mobile. URL các payload có hash nội dung, `build.json` ghi startupScene/battleScene/contentHash để phân biệt bản thực đang tải. Mobile có APK riêng `Arena3Q-Latest-20261025.apk`; giữ APK Mobile/Feedback cũ.

Đã kiểm Unity:

- EditMode `CurrentReleaseContentTests`: **4/4**, đối chiếu luật/chỉ số giữa stage nguồn và campaign, navigation hợp lệ.
- PlayMode FrontendFlowTests + BattlefieldHighQualityTests: **14/14**, người chơi mới/cũ vào đúng scene, chọn tướng/đặt tên, thắng tutorial lên cấp2 và học kỹ năng, quay về server, đường đi/map/preset và nút thử map.
- Low/Medium không được đặt target dưới60FPS theo quy tắc dự án. Đây là giá trị mục tiêu, chưa có số đo FPS thiết bị.

Log: `Logs/Latest-release-apply.log`, `Logs/Latest-release-edit.xml`, `Logs/Latest-release-play.xml`, `Logs/Latest-release-web-build.log`. Lượt compile test đầu phát hiện dùng Count trên mảng WaveSpawn; đã sửa Length trước lượt4/4.

Ảnh trước giữ `before/web-battle.png`, ảnh mới ở `flow/10-battle.png`. So sánh cùng khung trình duyệt: `web-map-before-after.png` (trái trước/phải sau). Hai lượt trình duyệt bằng chuột/bàn phím và chạm giả lập đều qua luồng đầy đủ tới map mới, triệu hồi tướng, lưu profile IndexedDB và đổi ba tỷ lệ; mỗi lượt17ảnh, 0lỗiJS/console. Video intro thực sự giải mã/phát hình1080p. Không ghi đè bằng chứng bản đầu.

## Gói mới để dùng

| Nền tảng | Gói và kết quả |
|---|---|
| Web | http://127.0.0.1:4322/?release=c1b55a0 ; `Builds/WebGL/Arena3Q`, **339,7MB** (356.213.518byte),315,1giây,0lỗi/8cảnhbáo |
| Mobile Android | [Tải APK](http://127.0.0.1:4322/download/Arena3Q-Latest-20261025.apk), `Builds/Android/Arena3Q-Latest-20261025.apk`, **263,4MB** (276.167.265byte),395,7giây,0lỗi/8cảnhbáo |

Web vẫn339,7MB như bản đầu, giảm8.370byte. APK vẫn263,4MB như bản Mobile trước, tăng17.505byte. Các APK cũ giữ nguyên. SHA256APK mới: `51b7ac207f8bff06d3ea054de0fba8cdc27238f0a4d253c6567abbea483c409a`. Chữ kýv2, cùng signer/package với bản Mobile trước; ARM64. Web `contentHash`: `475b2d26e681e02746fadab7c01c97817d796f222d4d7d2480af7d7c8aa14629`.

`Android-content.json` xác nhận gói thật chứa FrontendConfig và cả hai stage campaign mới, không chỉ kiểm danh sách scene nguồn. `build-icons.json`:39icon được dùng có61.528.848byte ASTC4×4 trướcZIP;42PNG nguồn đúngSHA phê duyệt. Web vẫn dùng RGBA32 theo cấu hình riêng. `delivery-verification.json`: downloadHEAD200đúng dung lượng/MIME vàRange206đúng chữ kýZIP, chữ ký/package APK đạt. Máy chủ chỉ phục vụ buildweb và đúngAPK này; không mở thư mục Android hoặc repo.

Máy chủ localPID32528 được giữ để xem/tải. Chưa đưa lên Internet. **ADB không nhận điện thoại**, nên chưa cài hoặc xác nhận render/FPS APK trên máy thật; chưa kiểm Safari/iPad thật. Không lấy kiểm trình duyệt giả lập hoặc target60 làm bằng chứng hiệu năng thiết bị.

Hướng dẫn build/chạy: [WebGame/README.md](../../../WebGame/README.md). Chỉnh EditorBuildSettings trong commit này là có chủ đích để profile Android/BuildScripts mở cùng luồng mới; các side effect ProjectSettings/URP khác không thuộc commit.

Tái kiểm: `node WebGame/flow-qa.cjs --latest`, thêm `--touch` cho chạm; `node WebGame/release-review.cjs` tạo so sánh/hash; `node WebGame/verify-release.cjs` kiểm payload/sign/package/download. BuildReport được kiểm ngay trong pipeline mobile bằng CurrentGameRelease.VerifyBuild.
