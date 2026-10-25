# 42 ảnh kỹ năng chờ duyệt

Ngày 25/10/2026. Trạng thái: **PENDING_APPROVAL**, **chưa áp dụng** vào Admin/Unity.

Mở `xem_anh_ky_nang.html` xem 42 ảnh theo số 01–42, tên, ID, tướng và mô tả nguồn; bấm ảnh xem PNG gốc. `contact-sheet.png` là ảnh tổng quan. `manifest.json` lưu mapping và trạng thái duyệt. Bao gồm cả ba kỹ năng nguồn cũ chưa gán của Mưu sĩ.

Ảnh tạo riêng từng kỹ năng bằng imagegen, giữ PNG gốc ở thư mục này. Người dùng có thể duyệt toàn bộ hoặc chỉ định số cần chỉnh. Chỉ sau khi có duyệt rõ ràng mới được gắn icon vào Admin và SkillDefinition Unity.

Script `Admin/scripts/skill-review-manifest.cjs` dựng danh sách từ snapshot, `Admin/scripts/build-skill-review.cjs` dựng trang HTML. Chúng không cập nhật asset Unity hay db.json. Ảnh/HTML/manifest là tài nguyên local bị bỏ qua trong Git; không tự sinh ảnh mới khi chạy hai script.
