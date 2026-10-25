# Triển khai Admin local

Theo yêu cầu 25/10: Next.js/TypeScript, Tailwind/shadcn, TanStack Table/Query, React Hook Form/Zod; NestJS REST, TypeORM/PostgreSQL, Redis/BullMQ, S3, R3F/Three/Drei, Recharts, Docker Compose/reverse proxy. Giai đoạn này chạy local bằng db.json, không phụ thuộc PostgreSQL/Redis/R2 đang hoạt động.

Kế hoạch: (1) workspace, hợp đồng dữ liệu và nhập snapshot Unity đọc-only; (2) API xác thực/phân quyền, repository JSON thay được bằng TypeORM; (3) shell đầy đủ menu và hồ sơ tướng 11 tab, gallery/viewer và trạng thái; (4) kiểm API, điều hướng/browser, build và bàn giao.

Phạm vi P0 theo spec v1.1: toàn bộ 6/26/91 menu có route. Hồ sơ tướng dùng dữ liệu thật trong Unity, giữ nguyên ID và chỉ số; bản đầu READ_ONLY. Tạo/sửa/publish, kinh tế và nghiệp vụ chưa có giữ PLANNED với thông báo rõ. Dữ liệu thiếu khác lỗi API/403/404. Không tự tạo công thức combat hoặc hệ thống gameplay mới.

Mã ở `Admin/`, nằm ngoài Unity Assets. File runtime `Admin/.local/db.json`, tài khoản bootstrap và private storage không vào Git. Theo dõi tiến độ và kết quả kiểm trong `Admin/README.md` sau triển khai.

## Đối chiếu P0

| Yêu cầu | Kết quả |
|---|---|
| Cây menu theo JSON | Dùng nguyên registry v1.1; 6 nhóm/26 nhóm con/91 leaf, breadcrumb, tìm menu, thu gọn, mobile |
| Tướng là luồng trung tâm | `/admin/game/heroes` → `/admin/game/heroes/detail/:heroId?tab=...`; legacy route quay về danh sách, không chọn tướng ngẫu nhiên |
| Dữ liệu thật | Import read-only từ ScriptableObject/YAML/meta: 10 tướng, 9 asset refs, 18 animation bindings; giữ nguyên ID/stat, chưa có role/rarity thì để thiếu |
| Tải từng phần | List DTO nhẹ, profile tách 10 API section; không tải tất cả media/model/skills khi mở danh sách |
| Danh sách | Tìm có/không dấu/ID, lọc, sort, phân trang server, CSV; URL giữ trạng thái qua refresh/back |
| Hồ sơ 11 tab | Tổng quan, media, model, rig, animation, skills/effects/timeline, stats, progression, skins, relations, versions |
| Preview | Ảnh riêng có zoom/pan/metadata. GLB viewer R3F có camera, shading, grid, axes, skeleton, clip/play/pause/speed/loop/seek/screenshot khi có file hợp lệ |
| Trạng thái rõ | Không dữ liệu khác PLANNED; 401/403/404/500 có giao diện và API status riêng, có retry/request ID |
| Auth & RBAC | Session hash server + cookie HttpOnly, scrypt, quyền kiểm FE và BE, rate limit, origin guard, audit đăng nhập/đăng xuất/import |
| Lưu local | JSON atomic + validate + serialize một process, không reset DB hỏng; thư mục `.local` ngoài Git |
| Đổi DB | `AdminStore` tách khỏi nghiệp vụ; TypeORM migration + PostgreSQL adapter + import JSON một lần vào DB trống |
| Hạ tầng sau | Redis/BullMQ/S3 adapters, Addressables release schema, Docker/Compose/Nginx; chưa nối vào pipeline Unity |

## Ranh giới bản đầu

Hai trang `GAME.HEROES.LIST` và `DASHBOARD.DASHBOARD.GAME` có override `READ_ONLY` trong feature manifest. Các nghiệp vụ còn lại giữ `PLANNED`; không dùng trạng thái đó để che lỗi API. Tạo/sửa/publish chưa có mutation API và chỉ mở thông báo tính năng dự kiến.

Các giới hạn này mô tả P0 ban đầu. Phần bổ sung 25/10 bên dưới thay thế giới hạn về GLB và chỉnh kỹ năng; các nghiệp vụ khác vẫn giữ nguyên phạm vi.

PostgreSQL đang dùng các aggregate JSONB theo loại bản ghi để giữ read model và thay adapter; chưa chuẩn hóa toàn bộ bảng nghiệp vụ trong spec. Redis/BullMQ chưa có worker và chưa bật cache đọc P0. S3/R2 chỉ có adapter đọc bằng signed URL, chưa có upload UI. Addressables/CDN có schema hợp đồng, chưa sửa Unity package/build hoặc publish catalog.

## Cách tiếp tục

Chạy theo `Admin/README.md`. Sau khi có PostgreSQL thật, kiểm migration/import và backup/restore trước; sau đó mới làm tài khoản/RBAC quản trị, CRUD draft + validation, upload/registry asset, publish/rollback có audit và pipeline Addressables. Chưa có endpoint nghiệp vụ nào trả thành công giả cho những mục này.

## Bổ sung ảnh, animation và chỉnh kỹ năng (25/10)

- Ảnh gốc PNG lossless, thumbnail lossless; gallery/portrait không tràn khung.
- Hai GLB dẫn xuất từ nguồn .blend đầy đủ, texture nhúng, 7 clip Lữ Bố + 11 clip Mưu sĩ; kiểm browser với model thật, không chỉ fixture. Không sửa/nhận QA thay nguồn rig/animation.
- 42 skill duy nhất / 46 liên kết, bao gồm kỹ năng chưa gán. Hiển thị sử dụng theo tham chiếu bộ kỹ năng/animation thực.
- API GET editor, PUT draft, POST apply-unity; form RHF/Zod, RBAC, revision/SHA, whitelist trường, backup/journal/audit và phục hồi khi DB thất bại. Chỉ số và mô tả áp dụng vào Unity local và build tương lai, không đồng bộ APK cũ.
- Các thao tác tạo tướng/upload/publish/rollback khác vẫn PLANNED. Manifest menu tổng thể giữ READ_ONLY ở danh sách; quyền chỉnh kỹ năng kiểm riêng từng thao tác.
- Người dùng đã chốt42ảnh minh họa, đã gắn vào42SkillDefinition Unity và46liên kết Admin. Registry duyệt/SHA256 được lưu; PNG lớn ở local, metadata/công cụ có trong Git. Chỉ thay icon, không đổi chỉ số. Chi tiết quy trình và kiểm trong Admin/README.md.
