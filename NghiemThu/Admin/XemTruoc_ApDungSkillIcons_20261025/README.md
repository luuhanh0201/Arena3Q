# Áp dụng 42 ảnh kỹ năng đã được người dùng duyệt

Ngày25/10/2026; người dùng chốt bộ42ảnh qua tin "oke tôi chót". Đã gắn42Sprite vào42SkillDefinition, hiển thị đủ46liên kết trong10hồ sơ Admin, kể cả kỹ năng animation-only/unused. Không thay slot, chỉ số, mô tả hay tham chiếu gameplay khác.

Ảnh `lu_bu-before.png` là baseline production trước khi gắn icon (cùng viewport/deep link Phương Thiên Trảm), `lu_bu-after.png` là sau; `lord_strategist-after.png` và hai ảnh `*-original.png` thể hiện ảnh lớn. Mở `xem_truoc_sau.html` xem so sánh. `web-verification.json` ghi kiểm thực tế10trang/46liên kết/42ảnh pixel-exact/0pageerror.

Kiểm Unity hai lượt: nhập Sprite từ PNG đã khóa SHA256, rồi nạp lại42SkillDefinition để xác minh đúng Sprite GUID/fileID và nguyên độ phân giải. Log `Admin/.local/unity-verify-approved-skill-icons.log`, report `Admin/.local/unity-approved-skill-icons.json`; batch thoát0. CLI verify so sánh trước/sau, xác nhận mọi trường ngoài icon giống nhau và PNG Unity đúng checksum đã duyệt. Production build/TypeScript đạt; browser kiểm ảnh và modal xem nguyên bản trên bản production thật. Không build APK, chưa đo RAM/FPS thiết bị cho bộ ảnh mới.

Trong lúc làm, Unity save ban đầu thêm field mặc định/đổi thứ tự YAML; kiểm sâu đã phát hiện, sửa pipeline để giữ byte nguồn và chỉ đổi một dòng icon. Không đưa các thay đổi serializer đó vào bản giao. Backup42asset và DB trong `.local/skill-icon-backups`.

PNG lớn tổng khoảng100,8MiB nằm local trong Art/SkillIcons và thư mục duyệt gốc; Git chỉ chứa metadata/registry/công cụ/tham chiếu. Không dùng Git LFS. Source best-quality không bị giảm kích thước; Unity RGBA32 không nén, full web PNG và thumbnail WebP lossless.
