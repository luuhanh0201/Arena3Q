# P4b: chi tiết nền và điểm gắn âm thanh

P4b xong phần thực hiện và tự kiểm, chưa hoàn tất toàn bộ P4. P4a c8866c1 đã được Claude review khoảng90%; các góp ý về tre/SRP/bờ suối/viền chữ được xử lý tại đây.

**WebGL cuối đạt12 tổ hợp màn hình/preset +5 hồ sơ tiến độ,57ảnh,0lỗi trình duyệt**, `../P3_Web_2026-10-31T00-40-33-842Z/result.json`. Có30mẫu đồng lúa/rừng trúc qua30/60/120. Đã xem ảnh thật đồng lúa preset60 và rừng trúc; trang gallery có12cặp Unity và32ảnh WebGL, kiểm tải ảnh/đổi lựa chọn đạt. Log `Logs/P4b-browser-final.log`. Giữ nút quay lại đỏ theo quyết định người dùng31/10.

Build WebGL tiếp tục31/10 07:21 thành công, `Logs/P4b-web-build-resume.log`:0lỗi/3cảnh báo,750,5s. Player427.407.511byte (407,6MiB), tăng1.963.897byte so với P4a; tổng thư mục1.530,7MiB có cả nhiều gallery/ảnh QA, không phải dung lượng player. Content hash `e43e069585b6c57145242144550d44dad17b7e4be90facbcb0e3f620a2c4282a`. Lượt00:31 bị ngắt, không dùng làm kết quả thành công. Lượt browser07:36 cũng bị ngắt ở12case/20ảnh; giữ result=false, chạy lại có log riêng.

Đã làm:
- Material nền riêng dùng đúng ảnh01/02/04 bản R2 đã đối màu. Giữ material/cache nguồn và scene của Claude, trả lại material khi hủy.
- 270 vị trí theo road/paths/yards/paddies/river trong JSON; loại17 điểm trong công trình, còn253 chi tiết/1.767 đỉnh/7mesh. Vệt bánh xe/dấu chân28, lá27, cành trúc09, vết cháy16, cỏ11, lúa12, sậy15, dải bờ08. Decal lưới3×3 bám terrain, offset2,5cm; lá đơn18–30cm. Chi tiết fade theo mức hiện của khu, không thêm collider hoặc đổi NavMesh; giữ cùng mật độ tĩnh qua30/60/120.
- Điểm gắn âm thanh theo khu, suối, lửa; hook bước chân/chọn NPC/nhận nhiệm vụ/tan sương/rương/boss. Pool4 nguồn (2 ambience,2 sự kiện), không cắt cue đang phát để lấy chỗ; tôn trọng mute UI, listener volume/pause; chọn ambience quanh điểm camera nhìn xuống đất. **Chưa có clip môi trường**, không coi là đã có âm thanh hoàn chỉnh; chưa kiểm nghe hay mix.
- 17điểm âm thanh:7khu,3lửa,7điểm suối. Hash3texture nền/atlas cache khớp nguồn;16/16sprite khớp manifest đã đóng gói.
- Tre có pha không gian thấp tần trong cùng cụm; chân cố định, biên độ/chu kỳ giữ P4a. Nước có pass depth cùng CBUFFER với forward. Viền chữ giảm0,20→0,12, giữ underlay.

Ảnh trước `../P1_Runtime_20261028/run_20261031_001816_105`; sau cuối `../P1_Runtime_20261028/run_20261031_003018_007`:4góc×3preset=12cặp1672×941. Đã mở so sánh Đồng Lúa. Gallery http://127.0.0.1:4322/chapter-p4-details/, kiểm12lựa chọn/ảnh decode đạt.

PlayMode24/24 (`Logs/P4b-play.xml`); lượt này kiểm luồng qua nhưng ảnh lúa/decal chưa hiện vì UV bị đọc thành0, nên **không dùng làm nghiệm thu hình**. Đã sửa DTO UV, kiểm miền trước bake/UV trải đủ hai chiều trong test; lượt bổ sung4/4 (`Logs/P4b-after-r2.xml`) gồm mở sương/boss không phát lại sau tải. Sau chỉnh cỡ lá, kiểm/capture cuối3/3 (`Logs/P4b-final.xml`). Audit vị trí: `Logs/P4b-details-placement.json`.

SRP Batcher cùng cảnh Rừng Trúc, camera render RT trong Unity batch (không phải số FPS/điện thoại):

| Vật liệu | SRP | Draw Calls | SetPass |
|---|---|---:|---:|
| nguồn tĩnh | tắt |950|244|
| nguồn tĩnh | bật |950|192|
| chuyển động | tắt |798|244|
| chuyển động | bật |798|194|

Counter là tổng frame của lượt camera/RT kiểm, không dùng thay số đo player/device. Batches trả0 ở backend này nên không dùng. Nước/tre không sai màu khi bật SRP: chênh kênh lớn nhất trung bình0,022/255,743/1.573.352pixel lệch hơn8 (0,047%); còn nhân vật/route đang chạy. 30 renderer tre vẫn dùng MPB để giữ bounds/pha theo cụm; không khẳng định tất cả renderer tương thích SRP Batcher, nhưng số draw call của cảnh kiểm không tăng. Log/ảnh trong `batching.json`, `gallery-check.json`, `batching-*.png`.

Tái tạo bằng hai script `ArtSource/Chapter01/2D/P4/` rồi Unity `Chapter01DetailsBuilder.Configure`. Texture cache riêng `_Presentation` giữ local kèm metadata; mesh/material/config nhỏ đi cùng code. Mipmap và ASTC6×6 mobile, DXT web; chưa kiểm ASTC/hiệu năng/nhiệt trên máy.

Còn lại: bờ08 hiện là dải ảnh đã duyệt, **chưa có bọt sóng động**; cờ, lá rơi, bụi, chim, bóng mây, viền mây, lửa/khói/tế đàn, rương lắc/tia còn thuộc P4 tiếp theo. Nhân vật thật và các model trạng thái do người dùng/Claude cung cấp; không giả hoàn tất bằng capsule hoặc clip rỗng.
