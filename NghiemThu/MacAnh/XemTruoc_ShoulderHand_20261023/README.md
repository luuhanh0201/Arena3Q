# Bàn tay chi tiết và cầu vai – candidate 23/10

Codex thực hiện, Claude review. **Chưa nghiệm thu nghệ thuật.**

- `xem_chuyen_dong.html`: 11 clip, 7 góc, trước/sau cùng camera và khung. Trước: TailoredSleeves b8e0292. Sau: nguồn SHA256 `81aa0c07c8cde9a4a3d902a6b77d3fe2004e46eae9d31f47d29c770e34c0b88d`.
- `xem_can_tay_vai.html`: 144 ảnh trước/sau, 9 pose, 4 góc bàn tay và 4 góc vai. Có Ultimate75/90, Skill02 30/40.
- `review_*.jpg`: so sánh LeftArm/RightArm ở các khung review trước. `compare_*.jpg`: toàn thân, sau và góc trận.

## Thay đổi

Phục hồi riêng mesh tay phải từ Portrait, giữ màu/UV nguồn; sửa vùng da từng bị phân loại vào tay áo. Dáng nắm được tạo bằng khớp ngón, sau đó bake và gán toàn vùng tay vào socket quạt để giữ tiếp xúc khi quạt xoay. **Đây là dáng nắm cố định, không phải ngón tay có thể đóng/mở độc lập.**

Thêm 2 bone cầu vai: xoay theo 40% góc cánh tay, giữ hình miếng kim loại. Tách biên đang hàn vào vải trước khi xoay. Tạo độ chùng/nếp hình học ở ống áo trên. Các chuyển động tóc/tà áo và bước chân từ bản trước được giữ.

## Kiểm tra

- Surface nguồn đủ khung 11 clip: đạt. Unity imported surface: đạt (`Logs/MacAnh-detail-full-review.log`).
- Quạt 6 phần, 985 mẫu: đạt (`Logs/MacAnh-detail-fan.log`).
- Tiếp xúc 5 vùng ngón, 985 mẫu: khoảng cách lớn nhất giữa điểm bề mặt gần nhất và cán là **1,382 mm**, ngưỡng 4 mm (`Logs/MacAnh-detail-contact.json`). Đo này không chứng minh ngón cái đối diện đúng hoặc không tự xuyên; cần review ảnh.
- Trang chuyển động: 154 trạng thái, 6972 đường dẫn ảnh, không thiếu ảnh/lỗi JS; chạy/dừng/tua/slider đạt (`page-check.json`).
- PlayMode: xem XML mới `Logs/MacAnh-detail-results.xml`; kết quả cập nhật sau khi lượt chạy kết thúc.

## Điểm cần reviewer kiểm kỹ

Ngón cái và silhouette lòng bàn tay; tiếp giáp cổ tay khi quạt xoay; miệng áo/vai ở góc trên bị tóc che một phần. Viền cổ tay trái tím tối là viền áo chủ ý. Mảng trắng phía sau chưa được xác nhận đạt. Không dùng kết quả test/surface để tự duyệt hình.

Nguồn local: `docs/art/concepts/MuuSi_main-hero/MacAnh/Production/LOD/MacAnh_Battle_DetailShoulderCloth.blend`. Quy trình dựng lại: `docs/art/concepts/MuuSi_main-hero/MacAnh/rebuild_detail_grip.ps1`. Blender/FBX/ảnh lớn lưu local theo quy tắc repo.

PlayMode đã kết thúc: **28 đạt, 0 lỗi, 1 bỏ qua** (XML mới).
