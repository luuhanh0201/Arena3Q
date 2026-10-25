# Audit dung lượng APK, 25/10/2026

Đối chiếu APK thật, log build24/10 và BuildReport cuối25/10; chỉ đọc, không build lại hoặc đổi texture/model/gameplay.

| APK | MB (byte/1.048.576) | Chênh so24/10 |
|---|---:|---:|
| Arena3Q-dev.apk,24/10 |172,6|0|
| Battlefield v1 |365,1|+192,5|
| Battlefield Feedback |380,5|+207,9|

Phần thêm không chỉ là map. Bộ icon kỹ năng được duyệt/gắn ngày25/10 chưa có trong APK24/10. Có39icon thật sự đi vào build, mỗi icon1254×1254 RGBA32 không mipmap, không nén GPU; mỗi ảnh khoảng6,0MB, tổng234,0MB trước ZIP. Registry có42ảnh nhưng3ảnh không được tham chiếu nên không vào build. `ApprovedSkillIcons.Import` đặt rõ Android RGBA32/Uncompressed; không được gọi đây là map200MB.

Map mới (Art/Battlefield + Quaternius + scene/config mới) khoảng84,9MB dữ liệu trước ZIP. Tất cả texture trong build551,0MB,93,7% nhóm tài nguyên người dùng; mesh chỉ14,9MB. Sáu texture đất mới1254×1254 đang dùng Automatic, chưa có Android override; mỗi ảnh cũng khoảng6MB trong BuildReport. Owned_* có ASTC6×6 riêng, không áp dụng tự động cho sáu texture đất.

`unity-build-size.json` ghi16.068packed object với byte offset/size. `apk-zip-index.json` ghi kích thước từng ZIP entry của cả3APK. `compressed-attribution.json` phân bổ compressed bytes của mỗi entry theo khoảng byte tài nguyên nằm trong entry đó: **ước tính** icon154,4MB và map50,8MB trong APK cuối. Chunk có nhiều loại asset không cho phép tách compression chính xác từng asset; đây không phải số đo nén độc lập. Các nhóm tăng còn lại nhỏ, không dùng phép cộng raw234MB với APK380MB.

**Có dư:** khi giữ nguyên ba scene và toàn bộ Resources, bỏ duy nhất cạnh tham chiếu đến BattlefieldAssetLibrary trong dependency graph cho thấy77asset chỉ bị kéo vào bởi thư viện Editor:3.532.265byte, khoảng3,4MB trước ZIP. Gồm prefab mẫu/cây/đạo cụ chưa dùng, mesh nguồn cỏ đã bake và ba LOD cây chưa dùng. Mesh nhà bị phá thật vẫn được MapProps3D tham chiếu; tên prefab dư không có nghĩa được xóa model phá nhà. Thư viện cần ở Editor để dựng/chỉnh map, nên giải pháp là tách tham chiếu khỏi player, giữ file nguồn.

APK cuối có17,5MB ngoài compressed payload (header/alignment/signing và vùng dư container), bảnv1 là1,3MB và bản24/10 là16,9MB. Payload cuối giảm0,84MB so v1 nhưng container tăng16,24MB, nên lần sửa sau tăng15,4MB. Chưa xác định byte nào chỉ là vùng rỗng để tuyên bố có thể bỏ toàn bộ17,5MB; chưa repack APK đã ký.

Build test chứa3scene (map mới,Frontend,Battle cũ); scene dùng chung asset không tự nhân bản toàn bộ. Bản cũ và art2D dự phòng còn có tham chiếu thật. Không kết luận toàn bộ chúng là file thừa, không xóa để làm nhỏ APK khi chưa tách dependency/kiểm luồng game.

Hướng xử lý tiếp: tách AssetLibrary khỏi player; thiết lập phiên bản texture Android thích hợp cho icon và đất, giữ PNG nguồn; đo ảnh cận và kiểm trên máy trước khi chốt mức nén; đóng gói sạch và đo lại. Chỉ sửa tối ưu khi triển khai bước đó, không dùng ước tính hiện tại làm cam kết dung lượng APK sau tối ưu.

Nguồn: Logs/UI-polish-build-android.log, Logs/BattlefieldFeedbackAndroid.log, Library/LastBuild.buildreport, ba APK local. Công cụ đọc report: Arena3Q.Editor.BattlefieldBuildSizeAudit.Export. Không có compile error trong lượt audit; APK Feedback sau audit đối chiếu đúng SHA256602ca28a571c4abc408466c9ca36d41e3d2948c271e78ce604c663144fe7a0bf và chữ ký trước. JSON/log lớn giữ local; README/công cụ được commit.
