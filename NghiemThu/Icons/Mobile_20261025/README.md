# Icon mobile, 25/10/2026

Người dùng yêu cầu xử lý icon, build mobile riêng và chuẩn bị bản web riêng về sau. Đã áp dụng cấu hình Android/iOS ASTC4×4 encoder100 cho42icon, giữ kích thước1254×1254. WebGL/Standalone có override RGBA32 riêng; Admin tiếp tục dùng PNG nguồn. Chưa build WebGL hoặc iOS trong lượt này.

Cấu hình Editor `Assets/Arena3Q/Scripts/Editor/SkillIconPlatforms.asset`: mobile format, encoderQuality, maximumSize và đường dẫn Android/futureWeb riêng. Asset nằm trong Editor, không là nội dung runtime. Import Approved Icon Textures đọc cấu hình này; Verify kiểm đủ4platform, SHA nguồn, kích thước và Sprite. Build Mobile Android kiểm Verify trước khi build. Cùng Sprite/GUID, không tạo thêm bản PNG trong Assets.

Đã đối chiếu42PNG nguyên SHA phê duyệt,42SkillDefinition nguyên SHA và42Sprite nguyênGUID/fileID. Trước/sau đọc texture thật qua UnityRenderTexture, ảnh đầy đủ tại `before/` và `after/`. `image-metrics.json`: cả42ảnh thay pixel sau nén; PSNR thấp nhất41,51dB ở kích thước gốc, ảnh đó ở128px đạt54,36dB. Không coi PSNR là thay cho đánh giá hình; đã mở bảng42cặp, ảnh Phương Thiên Trảm cận và ảnh Vạn Kiếm Quy Tông sau nén để kiểm nét/màu.

`contact-before-after.png`: mỗi cặp trái trước/phải sau. `comparison-*.png`: so cận cùng kích thước, cùng render path. PNG sau là ảnh đọc lại texture đã nén trong Unity, để trình duyệt xem; không phải fileASTC trình duyệt tự giải mã. PNG nguồn giữ riêng, không bị ghi đè. Không thay animation/mesh/gameplay hoặc nén texture map trong lượt này.

4/4 PlayMode smoke đạt: thẻ phản ánh trạng thái, tap dùng kỹ năng,5tướng đi đường và đổi chế độ/chất lượng map. XML/log trong thư mục này. APKmobile đã build riêng tại `Builds/Android/Arena3Q-Mobile-20261025.apk`; APKFeedback trước được giữ. BuildReport kiểm39icon thật sự được dùng:61.528.848byte,58,7MB trước ZIP, đúng ASTC4×4 và nguyên độ phân giải; không suy từ importer.

**APK cuối276.149.760byte,263,4MB**, giảm117,1MB (30,8%) soFeedback380,5MB. Build cuối76,6giây,0lỗi/3cảnh báo, Unity thoát0; SHA256`0460f1b7416c6095efa7eb2df6c81e3d738edfc891d13b862491a9759d390221`. Chữ kýv2cùng signer, package com.Arena3Q.Arena3Q, ARM64, minAPI26/target36. Native libil2cpp.so có SHA nguyên soFeedback:8043ca4e97840ea9a5eb3d4df436eec55e14865c849485675c059822d8a70468.

Gói đầu sau nén vẫn380,5MB vìGradle incremental giữ vùng dữ liệu cũ:payload276.011.465byte/overhead122.929.229byte. Build mobile nay tạo lại đúng APKcache launcher-debug.apk để đóng gói sạch; bản cuối payload276.011.456byte/overhead138.304byte. Không xóa cachecode/asset hoặc can thiệp padding/chữ ký của APK thành phẩm. Lượt đầu còn lỗi phép tính kích thước ở bước QA, đã sửa và kiểm lại; lượt build cuối cả build lẫn QA đều đạt. Các log thất bại đầu giữ để đối chiếu.

Trang http://127.0.0.1:4311/app/icon-mobile.html kiểm đủ42cặp/84ảnh ở kích thước1254, không pageerror/tràn ngang ở412px; HEADAPK200 đúng dung lượng. Serverlocal PID31420. ADB sau build không có thiết bị, chưa cài/chạy hay đoFPS bản này. iOS/WebGL chỉ có cấu hình riêng, chưa build.

Ảnh/log/JSON/APK để local theo giới hạn repo, chỉ công cụ/cấu hình/meta/README được commit. Hướng dẫn và kết quả cuối: docs/UI/SkillIconPlatforms_20261025.md.
