# Chương 1 – Bố cục map khám phá (CH01_Exploration)

Scene: `Assets/Arena3Q/Chapters/Chapter01_YellowTurban/Scenes/CH01_Exploration.unity`, dựng lại bằng **Arena3Q > Chapter 01 > Build Exploration Map** (`Chapter01MapBuilder`). Mọi vị trí, đường, suối, ruộng và luật rải cảnh nằm trong `Data/CH01_MapLayout.json` (sinh từ `ArtSource/Chapter01/3D/Map/ch01-layout.cjs`); sửa file đó rồi dựng lại, không sửa scene bằng tay. Ảnh: `ArtSource/Chapter01/3D/Renders/Map/` (00 nhìn thẳng xuống, 01 camera trận, 11 đến 17 từng ải).

Tọa độ: mặt đất XZ của Unity, Y lên, 1 đơn vị = 1 m. Vùng chơi 140 × 90 (x −70..70, z −45..45), viền đồi và rừng thêm 55 m mỗi phía. Camera trực giao nghiêng 50°, xoay 45°, ánh sáng và grading của bản phát hành (Battlefield).

| Ải | Tên | Tâm (x, z) | Nội dung 3D chính |
|---|---|---|---|
| 1 | Làng Bình An | (−48, −28) | 5 nhà CS Studio, cổng làng, giếng, xe đổ, 2 cũi giam, hòm, thùng |
| 2 | Đồng Lúa và Kho Lương | (−10, −27) | 4 ô ruộng, kho lương, 2 xe lương, bao, chòi gác, rào |
| 3 | Rừng Trúc | (−54, 5) | 30 cụm tre, trại nhỏ: 2 lều, tháp canh, rào, giá vũ khí, lửa trại |
| 4 | Trại Lương và Thôn Trang | (−8, 4) | kho Khăn Vàng, 3 xe lương, xe kéo, 2 nhà dân, rào tạm, chiến kỳ, cũi |
| 5 | Tiền Doanh | (−4, 31) | rào vòng, lều chỉ huy, trống trận, 3 cờ, 2 tháp, giá vũ khí, 2 lều |
| 6 | Đại Doanh Hoàng Cân | (36, 2) | cổng đại doanh, tường cọc, 4 tháp góc, sân luyện, 2 doanh trại, lều chỉ huy, trống lớn |
| 7 | Tế Đàn Hoàng Cân | (56, 31) | nền cao 2,6 m, bậc đá, đàn tế, 3 trụ, cờ, lư hương, đuốc, vách đá |

- Đường chính (rộng 3,4 m) đi liền 1 → 7, quanh co, vòng qua rừng trúc. Suối chảy từ bắc xuống nam ở x ≈ −30; 3 cầu gỗ đặt tự động ở chỗ đường chính cắt suối.
- `StageMarker_01..07` (rỗng) ở tâm từng ải: chỗ Codex gắn điểm tương tác, sương mù và logic mở khóa.
- `NPC placeholders`: khối viên nang theo vai (vàng = lính Khăn Vàng, xanh = nghĩa binh, be = dân, đỏ = boss, cam = hộ pháp). Không phải hình cuối; thay bằng nhân vật thật khi có.
- Địa hình có `MeshCollider` và cờ Navigation Static cho NavMesh; công trình kitbash có collider hộp.

Chưa có (báo rõ, không giả vờ đã làm): nhân vật thật, sương mù, NavMesh, tương tác, chuyển cảnh, trạng thái sau thắng (phần Codex theo §28), decal và sprite 2D trên map, VFX nước chảy / lửa / khói, âm thanh. Đàn tế vẫn dùng mẫu Bát Quái của trận (Chuong1.md §18 yêu cầu bỏ họa tiết này), tượng đá còn thiếu. Hiệu năng chưa đo trên máy thật; cụm tre nặng (8,6k đến 14,4k tam giác mỗi cụm).
