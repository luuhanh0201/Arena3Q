# Admin P0 · 25/10/2026

Tự kiểm bản production local Next standalone + NestJS, chưa triển khai public. Giao diện Admin mới; không có trang Admin cũ để dùng làm baseline. Cặp trước/sau dưới đây là sửa typography trong chính bản triển khai đầu, cùng viewport 1600×1000.

- Mở `xem_admin.html` trong thư mục này để xem ảnh và so sánh đăng nhập trước/sau.
- `login-before.png` / `login.png`: reset CSS ngoài layer làm utility margin/font bị lấn; bản sau đưa reset vào Tailwind base.
- `heroes-initial.png`: bảng 10 tướng nguồn Unity, tìm/lọc/sort/phân trang/CSV.
- `hero-skills.png`: hồ sơ Lữ Bố, tab kỹ năng và dữ liệu gốc.
- `overview.png`: Recharts theo số liệu nội dung thật, 10 tướng/9 asset refs/18 animation bindings/43 skill và binding.
- `heroes-mobile.png`: viewport 390×844, table cuộn ngang bên trong, trang không tràn ngang.
- `viewer-test-fixture.png`: kiểm GLB hình tam giác và clip translation **riêng cho test**. Đây không phải model tướng hoặc bằng chứng QA model game.

Kết quả: TypeScript và production build đạt; 11/11 unit/API, 3/3 browser workflows đạt. Browser kiểm toàn bộ 91 leaf route, 11 tab, login, tên không dấu, deep link/reload/back giữ bộ lọc, modal, 404 và responsive; GLB fixture kiểm load/render/clip/pause/seek/speed/camera/shading/screenshot. HTTP test kiểm RBAC 401/403, 404/500 thật, origin guard, range/private asset, JSON concurrent writes, dữ liệu hỏng và audit bất biến. npm audit production: 0 vulnerabilities sau pin multer 2.5.0.

Chưa chạy container vì máy chưa cài Docker. Chưa integration test PostgreSQL/Redis/R2; adapter/config/migration có sẵn. Các nghiệp vụ create/edit/publish còn PLANNED theo P0 read-only. Chi tiết chạy local và lộ trình DB trong `Admin/README.md`.

Ảnh/HTML trong thư mục này giữ local theo quy tắc repository; chỉ README vào Git. Không đưa DB/session/tài khoản bootstrap lên Git. Không thay nguồn Unity hoặc build APK trong việc Admin này.
