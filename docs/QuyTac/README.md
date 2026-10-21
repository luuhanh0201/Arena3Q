# Quy tắc – mục lục

Mọi quy tắc và nhật ký rút kinh nghiệm của dự án nằm ở thư mục này (gom lại 21/10/2026 theo yêu cầu người dùng). **Agent nào cũng đọc file liên quan trước khi làm.**

| File | Nội dung | Khi nào đọc / ghi | Ai giữ |
|---|---|---|---|
| `QuyTacLamViec.md` | Quy tắc làm việc người dùng đã giao: giao tiếp, commit/push, dữ liệu dễ sửa, asset nhỏ / không LFS, lưu trữ thay vì xoá, ảnh trước–sau, 60 FPS, VFX "khí", phối hợp hai agent, review và nghiệm thu | Đầu mỗi phiên; thêm quy tắc mới khi người dùng giao | chung |
| `Model3D_Rules.md` | **Luật bắt buộc** cho model 3D, skinning, animation, VFX: điều kiện bàn giao, lỗi đã gặp và luật hồi quy, checklist | Trước mọi việc 3D / animation / VFX | Codex (đang làm Mặc Ảnh); bài học chung ai cũng đề xuất thêm |
| `NhatKyVanHanh_3D.md` | **Nhật ký vận hành** 3D / animation / VFX: mọi hướng sai, lỗi, bug gặp khi làm – nguyên nhân, cách phát hiện, cách phòng tránh | Đọc trước khi làm; **ghi ngay** khi gặp | chung (chỉ thêm dòng) |
| `NhatKySuaLoi.md` | Nhật ký sửa lỗi gameplay / code: nguyên nhân gốc và cách tránh lặp lại cho mọi bug đã sửa | Trước khi sửa gameplay hoặc chạy build/test batch; thêm mục cho mỗi bug sửa | chung |

Ở chỗ khác (không chuyển, vì công cụ / quy trình cần chúng ở đúng chỗ):
- `CLAUDE.md` (gốc repo): hướng dẫn kỹ thuật cho Claude Code – cấu hình, cấu trúc code, lệnh build/test.
- `AgentComms/README.md` và `AgentComms/STATUS.md`: giao thức trao đổi giữa hai agent, file đang giữ, khoá Unity.
- `docs/GameplaySpecification.md`: luật gameplay (nguồn đúng duy nhất).
