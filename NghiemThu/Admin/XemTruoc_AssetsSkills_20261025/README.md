# Admin: ảnh, model, animation và chỉnh kỹ năng

Ngày 25/10/2026. Mở `xem_truoc_sau.html` để xem cùng trang trước/sau của media, animation và kỹ năng. Có ảnh hai model đầy đủ texture, form sửa thông số và zoom ảnh.

- Ảnh full PNG giữ đúng pixel nguồn, thumbnail lossless WebP; gallery/portrait không tràn khung.
- GLB dẫn xuất không giảm lưới từ LuBu_Arena3Q_Grip.blend và Final/MacAnh_Portrait.blend. Hai model có tổng 18 clip; browser kiểm mỗi clip tua 0 → giữa clip làm thay đổi pixel thực, không có pageerror khi chuyển route. Không coi đây là QA skin/animation game hoặc đo FPS thiết bị.
- Skill có 42 ID duy nhất (46 liên kết), chỉ số/mô tả sửa được; rõ equipped / animation-only / unused. Lưu nháp không sửa Unity; áp dụng cần quyền riêng, kiểm SHA/revision, whitelist trường và có backup/journal/audit.
- Production build/TypeScript đạt. Unit/API 19/19 (gồm phục hồi journal sau crash khi DB đã/chưa commit); browser 6/6 workflow, gồm 91 routes, 11 tab, ảnh nguyên pixel và 18 clip thật. Unity AdminSkillImportTests 1/1, không skip: đọc output HTTP apply thành SkillDefinition với cooldown9/effect2/mô tả; fixture SHA256 `ffec4cc9e2d3df96c1a5815f41ae44f7065d534fd8ea36dd9d94873aa6ce998e` không đổi sau lần chạy API cuối.
- Không sửa số liệu game thật trong quá trình test; fixture Unity đã dọn. PostgreSQL/Redis/R2/container chưa integration test, không nằm trong vòng thay đổi này.

42 ảnh kỹ năng mới ở trang duyệt riêng `../XemTruoc_SkillIcons_20261025/xem_anh_ky_nang.html`, chưa gán vào Admin hoặc game. Chờ người dùng duyệt.

Ảnh/HTML/log là bằng chứng local ngoài Git; README và mã tái tạo được commit. Log Unity: `Admin/.local/unity-skill-import-tests.xml`; log production: `Admin/.local/production.*.log`.
