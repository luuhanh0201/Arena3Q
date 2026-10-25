# Arena3Q Admin · P0

Admin chạy độc lập với Unity, theo `docs/Arena3Q_Admin` v1.1. Có shell/menu đủ **6 nhóm, 26 nhóm con, 91 chức năng**, danh sách tướng và hồ sơ **11 tab**, kèm chỉnh chỉ số/mô tả kỹ năng và áp dụng vào nguồn Unity local. Các chức năng tương lai có trang “Đang phát triển”, không trả kết quả thao tác giả.

## Chạy local trên Windows

Cần Node.js 22.12+ (đã kiểm bằng Node 24). Trong thư mục `Admin`:

```powershell
npm ci
npm run setup
npm run dev
```

Mở **http://localhost:3000/admin**. Tài khoản được tạo ngẫu nhiên tại **`.local/bootstrap-credentials.json`**; mở file đó trên máy để lấy email/mật khẩu. File này, session, database và media riêng đều được bỏ qua bởi Git. Không có tài khoản/mật khẩu mặc định công khai. Có thể đặt `ADMIN_EMAIL`/`ADMIN_PASSWORD` trước lần setup đầu, mật khẩu ít nhất 12 ký tự. Chạy setup lại giữ tài khoản hiện có.

Không cần PostgreSQL/Redis để chạy. `.env.example` liệt kê cấu hình tùy chọn, có thể sao chép thành `.env`. API ở `127.0.0.1:4000`, web chuyển tiếp `/api/admin/v1` tới API cùng origin.

Chạy bản production local: `npm run build`, sau đó `npm start`. Script start dùng Next standalone và đưa static/public vào đúng vị trí; dừng cả hai dịch vụ bằng Ctrl+C.

## Dữ liệu thật và phạm vi

- `scripts/import-unity.ts` đọc `.asset`, `.meta` và enum nguồn; giữ ID, giá trị, GUID và fileID 64 bit. Không sửa file Unity, không khởi động Editor.
- Snapshot hiện có **10 tướng, 53 tham chiếu asset, 18 binding animation và 42 kỹ năng duy nhất** (46 liên kết tướng/kỹ năng). Cả 42 skill đã có ảnh được duyệt. Ảnh đầy đủ là PNG giữ nguyên pixel nguồn và alpha; thumbnail 320 px dùng WebP lossless. Khung ảnh giữ đúng tỷ lệ, có xem lớn.
- Tìm tên có/không dấu và ID; lọc, sắp xếp, phân trang tại API; trạng thái nằm trong URL. Quay lại danh sách giữ bộ lọc. CSV xuất theo toàn bộ bộ lọc và chặn công thức spreadsheet.
- Hồ sơ gồm tổng quan, hình ảnh, model, rig, animation, kỹ năng, chỉ số, phát triển, skin, liên kết, phiên bản. API tải từng tab; `tab`, `skillId`, `clipId` là deep link.
- Phẩm chất/vai trò chưa có trong nguồn được giữ trống. `DRAFT` là trạng thái snapshot admin chưa phát hành. Không suy diễn SSR, công thức chỉ số, kết quả QA, người cập nhật hoặc số liệu người chơi/doanh thu.
- Lữ Bố có GLB đầy đủ 7 clip; Mưu sĩ có GLB đầy đủ 11 clip. Các bản dẫn xuất giữ lưới nguồn, nhúng texture gốc (có ảnh 4K), không giảm polygon. Chọn clip, play/pause, speed/loop, timeline, camera, skeleton và chụp ảnh ngay trong viewer. Các tướng chưa có nguồn 3D không được gán model giả. Đây là khả năng xem trên web, không thay thế nghiệm thu skin/animation trong game.
- Kỹ năng có mô tả, chỉ số chi tiết và phân biệt **đang gán bộ kỹ năng**, **chỉ gán animation**, **chưa gán trong game**. Trạng thái căn cứ `HeroDefinition.skills` và binding animation; kỹ năng đã gán vẫn phụ thuộc điều kiện mở khóa trong game.
- Riêng kỹ năng đã có thao tác chỉnh/lưu nháp/áp dụng Unity local. Tạo tướng, nhập/upload qua web, phát hành Addressables và rollback qua UI vẫn PLANNED.

Để cập nhật snapshot, **dừng API trước**, rồi chạy `npm run import:unity` và khởi động lại. Không chạy importer/setup cùng lúc với API ghi file.

## Chỉnh kỹ năng và ảnh đã duyệt

Trong hồ sơ tướng → Kỹ năng & hiệu ứng → Chỉnh chỉ số và mô tả: **Lưu bản nháp** chỉ cập nhật db.json. **Áp dụng Unity** cập nhật đúng `SkillDefinition.asset` nguồn; Unity local nạp giá trị mới cho trận mới, các lần build tiếp theo mang giá trị đó. APK đã build không tự nhận thay đổi. Không thêm loại cơ chế mới qua form.

API kiểm quyền đọc/sửa/phát hành độc lập, kiểm SHA nguồn và revision để từ chối bản nháp cũ hoặc nguồn đã sửa bên ngoài. Chỉ ghi file whitelist trong `Assets/Arena3Q/Content/Skills`, giữ GUID/ref YAML; có backup `.local/skill-backups`, journal `.local/skill-transactions`, audit và phục hồi giao dịch dang dở khi khởi động. Nếu ghi DB thất bại, nguồn được trả về backup. Sao lưu cả `.local` cùng dự án trước khi di chuyển máy.

`UNITY_PROJECT_ROOT=..`, `UNITY_WRITE_ENABLED=true` dành cho local. Compose mặc định **tắt ghi Unity**, vì container chưa mount nguồn game; chỉ xem dữ liệu và lưu nháp. Không chạy builder tái sinh skill trong Unity đồng thời với thao tác apply trên Admin.

Bộ **42 ảnh đã được người dùng chốt ngày25/10** và gắn vào42 `SkillDefinition` /46liên kết Admin, gồm kỹ năng chưa trang bị. Registry `reviews/skill-icons-20261025.json` ghi lời duyệt, thời gian, SHA256, kích thước và đường dẫn từng ảnh. PNG gốc trong `Assets/Arena3Q/Art/SkillIcons` giữ nguyên độ phân giải, Unity dùng Sprite RGBA32 không nén; web full PNG cùng pixel, thumbnail320px lossless. Bấm ảnh chiêu trong Admin để xem nguyên bản. Chỉ thay dòng `icon`, không đổi số liệu/mô tả/slot hay trạng thái sử dụng kỹ năng.

42 PNG nguồn tổng khoảng100,8MiB giữ **local**, không vào Git theo quy tắc asset lớn; `.meta`, tham chiếu, registry và công cụ được commit. Giữ cả thư mục review gốc `../NghiemThu/Admin/XemTruoc_SkillIcons_20261025` khi sao lưu/chuyển máy. Mã Git một mình không chứa các PNG này.

Quy trình tái tạo (dừng API, giữ Unity độc quyền theo AgentComms; chuẩn bị đủ PNG đúng checksum trước):

1. `npx tsx scripts/apply-approved-skill-icons.ts prepare` kiểm duyệt/checksum/nguồn, sao lưu asset+DB rồi chép ảnh còn thiếu.
2. Unity batch `-executeMethod Arena3Q.Editor.ApprovedSkillIcons.Import` nhập texture và xuất GUID/fileID Sprite thật, không ghi SkillDefinition.
3. `npx tsx scripts/apply-approved-skill-icons.ts bind` thay riêng dòng icon từ backup, từ chối thay đổi ngoài phạm vi.
4. Unity batch `-executeMethod Arena3Q.Editor.ApprovedSkillIcons.Verify` nạp lại tất cả42SkillDefinition và xác minh đúng Sprite/độ phân giải.
5. `npx tsx scripts/apply-approved-skill-icons.ts verify`, rồi `npm run import:unity` và khởi động lại Admin.

Backup ở `.local/skill-icon-backups`, report Unity `.local/unity-approved-skill-icons.json`. Không dùng builder tái sinh số liệu để gắn ảnh. Hai script review chỉ dựng danh sách/HTML; script manifest từ chối ghi đè bộ đã được duyệt, HTML phản ánh trạng thái đã áp dụng.

Để xem qua HTTP, chạy `npm run review:skills`, mở **http://127.0.0.1:4310**. Server chỉ phục vụ trang duyệt/42 PNG gốc/ảnh tổng hợp trên loopback; không ghi trạng thái duyệt hay thay đổi Admin/Unity. Ctrl+C để dừng. Trang Admin chính vẫn ở http://localhost:3000/admin.

GLB/texture là tài nguyên dẫn xuất local, không vào Git. Có thể xuất lại bằng Blender 4.5.3 (không lưu thay đổi vào .blend nguồn), rồi dừng API/import lại snapshot:

```powershell
$blenderPath = '../Library/LuBuTools/blender-4.5.3-windows-x64/blender.exe'
& $blenderPath -b '../docs/art/concepts/LuBu_Arena3Q/LuBu_Arena3Q_Grip.blend' --python scripts/export-web-preview.py -- --hero lu_bu
& $blenderPath -b '../docs/art/concepts/MuuSi_main-hero/MacAnh/Production/Final/MacAnh_Portrait.blend' --python scripts/export-web-preview.py -- --hero lord_strategist
npm run import:unity
```

Importer kiểm checksum nguồn của manifest, không dùng GLB cũ khi .blend đã đổi. Bản hiện tại khoảng 72,2 MiB và 64,4 MiB, nên lần mở đầu có thời gian tải; sau đó trình duyệt cache. Các cảnh báo tangent trên phụ kiện nguồn và QA biến dạng nguồn không được coi là đã sửa bởi exporter.

## Stack đã thiết lập

| Thành phần | Triển khai |
|---|---|
| Admin | Next.js App Router + React + TypeScript |
| UI | Tailwind CSS v4, shadcn/ui theo source, Radix primitives, Lucide; font Be Vietnam Pro local |
| Dữ liệu/form | TanStack Table + Query, React Hook Form + Zod |
| Viewer | React Three Fiber + Three.js + Drei: GLB, camera, shading, skeleton, clip/timeline, ảnh chụp |
| Biểu đồ | Recharts hiển thị số liệu nội dung thật ở `/admin/dashboard/game` |
| API | NestJS REST, cookie session, RBAC, validation, rate limit, audit |
| Lưu hiện tại | `AdminStore` → `JsonFileStore`, `.local/db.json` |
| PostgreSQL | TypeORM entities/migration và `PostgresStore`, chọn bằng `DATABASE_DRIVER` |
| Cache/queue | Adapter ioredis/BullMQ tùy chọn; P0 chưa đưa cache/job vào luồng đọc, chưa có worker |
| Object storage | Kho local hiện dùng; adapter S3/R2 trả signed URL 60 s khi được cấu hình |
| Unity/CDN | Schema release Addressables chuẩn bị sẵn; chưa có Unity build/upload/publish pipeline |
| Triển khai | Dockerfile web/API, Compose, Nginx reverse proxy; profile PostgreSQL/Redis |

Phiên bản chính được khóa bằng `package-lock.json`. Model viewer không thể đánh giá chất lượng model/hoạt ảnh trong game; cần nghiệm thu Unity riêng.

## Lưu file, bảo mật và sao lưu

`JsonFileStore` serialize các mutation **trong một API process**, kiểm schema/ID, bảo vệ audit chỉ thêm, ghi file tạm + fsync + rename. JSON hỏng tạo lỗi rõ ràng, không bị thay bằng DB rỗng. Đây là chế độ local một instance; không chạy nhiều API process hoặc sửa file khi API đang chạy. Giữ một bản sao toàn bộ `.local` khi dừng dịch vụ để sao lưu DB và media nhất quán.

Mật khẩu dùng scrypt có salt, DB chỉ lưu hash. Session random lưu hash server, cookie HttpOnly/SameSite=Lax; logout thu hồi session. API kiểm quyền độc lập với UI. Media riêng đi qua API có auth, chống path traversal/symlink thoát kho và có range request. 401/403/404/500 khác nhau; lỗi server không lộ đường dẫn/stack cho trình duyệt. Audit không chứa mật khẩu/token. Khi triển khai HTTPS, đặt `COOKIE_SECURE=true` và `ADMIN_ORIGINS` đúng origin được phép.

Theo yêu cầu 25/10, phiên đăng nhập mặc định **365 ngày** (`SESSION_HOURS=8760`), tính cố định từ lúc đăng nhập. Cookie và bản ghi server cùng thời hạn; `/auth/me` đồng bộ lại cookie theo thời hạn còn lại, không tự kéo dài thời hạn server mỗi lần truy cập. Logout vẫn thu hồi phiên ngay.

## Chuyển sang PostgreSQL sau này

1. Dừng API và sao lưu `.local`. Cấu hình `DATABASE_URL` cho DB trống.
2. `npm run db:migrate` chạy migration, `synchronize:false`.
3. Muốn chuyển cả snapshot/tài khoản/audit từ local, PowerShell:

```powershell
$env:IMPORT_LOCAL_JSON='true'
npm run db:migrate
Remove-Item Env:IMPORT_LOCAL_JSON
```

Import từ chối DB đích đã có dữ liệu; không sao chép session đang đăng nhập. Giữ media local hoặc đồng bộ key sang object storage trước khi đổi driver.

4. Đặt `DATABASE_DRIVER=postgres`, khởi động lại API và đăng nhập lại.

Schema TypeORM đầu tiên gồm aggregate JSONB theo hero/asset/user/session/audit/state, index email/token/phe/thời gian, trigger audit bất biến. Đây là lớp chuyển tiếp cho read model hiện tại; chuẩn hóa bảng và query hiệu năng lớn thuộc giai đoạn DB tiếp theo. **Chưa chạy integration test PostgreSQL/R2/Redis thực trong phiên này.**

## Docker Compose

Chạy setup trên máy trước để tạo `.local` cùng media, sau đó:

```powershell
docker compose up --build -d
```

Mở http://localhost:8080. Chỉ reverse proxy mở cổng local; API/web ở mạng Compose. PostgreSQL và Redis chỉ được bật khi cần:

```powershell
docker compose --profile postgres --profile redis up -d postgres redis
```

URL API trong container dùng hostname `postgres`/`redis`; ví dụ `redis://redis:6379`. Host chạy migration dùng `localhost`. Đổi mật khẩu dev trước triển khai thật. Cấu hình TLS/domain/CDN thuộc môi trường triển khai sau. Máy hiện tại chưa cài Docker nên Compose/Dockerfile mới được kiểm cấu trúc, chưa chạy container.

## Kiểm tra

```powershell
npm run typecheck
npm test
npm run build
# Cần web/API đang chạy + setup local. Dùng Edge có sẵn trên Windows.
npm run test:e2e
```

Có thể đặt `EDGE_PATH` hoặc `ADMIN_TEST_URL` khi máy khác. Unit/API test dùng DB và tài khoản fixture trong thư mục tạm, không thay DB local. Browser test dùng tài khoản bootstrap local để kiểm dữ liệu thực. Screenshot/trace nằm trong `test-results` và không vào Git.

Mã nguồn: `apps/web`, `apps/api`, `packages/contracts`; import/setup: `scripts`. Chi tiết đối chiếu tài liệu: `../docs/Arena3Q_Admin/IMPLEMENTATION.md`.
