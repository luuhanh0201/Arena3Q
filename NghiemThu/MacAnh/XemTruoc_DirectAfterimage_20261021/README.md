# Xem trước – bóng ảnh Mặc Ảnh direct3D

Codex tự kiểm ngày21/10, chờ Claude nghiệm thu độc lập. Chưa nghiệm thu tổng thể Mặc Ảnh.

- `SauMatNa_6Goc.png`: mỗi cặp trái trước/phải sau; cùng camera, pose DodgeAttack0,42s, cấu hình tướng1,3x.
- `DodgeAttack_4Pha.png`: trái trước/phải sau, trận thật ở0,28/0,35/0,42/0,82s, zoom3,5. Các pha khác và zoom4,5 trong log capture.
- Sửa scale nướng, giữ mọi submesh và sáu mesh quạt, fade đồng bộ, tắt snapshot ngay khi trả pool. Material riêng `AfterimageSurface` không dùng alpha atlas sprite trên UV của model; blend alpha giới hạn cháy sáng. Builder gắn lại đúng material khi dựng prefab.
- Baseline đầu: chiều cao bóng169,752m / model1,69752m =100x; index99.000/130.848. Sau sửa và cấu hình hiển thị1,3x: bóng2,2067747m / model2,2067750m, index130.848/130.848.
- Phép đo ảnh với mask trắng làm tham chiếu cùng mesh/camera: tỷ lệ silhouette nhỏ nhất39,813% →100% qua sáu góc. Đây là phép kiểm mất hình do material, không thay cho audit biến dạng toàn bộ clip.
- `Logs/CodexFix/20261021/MaskBefore_1733.xml`:1đạt/1lỗi đúng silhouette. `MaskAfter_1735.xml`:3/3đạt (snapshot+bounds/index/silhouette/fade; pool reuse/cleanup; capture trận).
- Nguồn ảnh trước: `MaskBefore_1733`, `MaskBattleBefore_1733`. Sau: `MaskAfter_1735`, `MaskBattleAfter_1735` cùng thư mục log. Ảnh ghép chỉ xếp cạnh/thu nhỏ, không sửa nội dung.
- Còn mở: A2 chân tia phép, vật liệu/seam, audit import/quạt, motion/VFX và ngân sách. Không dùng phần này để kết luận tất cả mục §17đạt.
