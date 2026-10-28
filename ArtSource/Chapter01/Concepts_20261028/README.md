# Hình xem trước Chương 1, 28/10/2026

Phạm vi người dùng giao: đọc docs mới và vẽ hình trước, chưa code. Đã vẽ hai bản thiết kế bằng built-in imagegen và mở ảnh trong cuộc trò chuyện. Không sửa Assets, scene, prefab, dữ liệu gameplay hoặc dùng Unity.

Nguồn: `docs/Ai/Chuong1.md` và `docs/Ai/Bản đồ Chương 1 Loạn Khăn Vàng.png` do người dùng cung cấp. Ảnh mẫu có 12 điểm; bản mới dùng đúng 7 điểm ải theo tài liệu.

## Hai hình để xem

- [Tổng thể mở đủ 7 ải](02_chapter-overview.png): xem bố cục toàn chương, không phải trạng thái vào game lần đầu.
- [Đầu chương có mây khóa](04_chapter-opening-fog.png): chỉ mở Làng Bình An, giữ dân bị bắt và địch; không hiện tên/số ải hoặc NPC chính của Ải 2-7. Các công trình về sau chỉ còn bóng mờ dưới sương.

PNG gốc 1672×940, không phóng ảnh để tuyên bố 4K. Ảnh và bản nháp giữ local theo quy tắc tài nguyên lớn; `images.json` ghi kích thước, dung lượng và SHA256.

## Bố cục và kiểm hình

1. Làng Bình An ở trái dưới: nhà dân, cổng gỗ, giếng, trưởng làng/dân và khu giam giữ.
2. Đồng Lúa và Kho Lương ở giữa dưới: ruộng vàng, kho gỗ, hai xe và đội hộ vệ.
3. Rừng Trúc ở trái giữa: đường mòn, tiền tiêu nhỏ, tháp gỗ và lính gác.
4. Trại Lương và Thôn Trang ở trung tâm: nhà bị chiếm, kho, ba xe lương và dân bị bắt.
5. Tiền Doanh ở giữa trên: lều chỉ huy, hai tháp, Trình Viễn Chí và hộ vệ.
6. Đại Doanh Hoàng Cân ở phải giữa: cổng, hàng rào và tháp gỗ, sân quân; chỉ một điểm ải cho toàn doanh.
7. Tế Đàn Hoàng Cân ở phải trên: bậc đá lên cao, Trương Giác và ba trụ năng lượng.

Đã xem trực tiếp cả hai hình, rà bảy tên/số ải, đường và cầu nối, hai/ba xe lương, cổng gỗ, ba trụ và lớp sương. V1 có thừa một xe ở Trại Lương và cổng giống thành đá, đã chỉnh trong bản tổng thể. Fog v1 còn bóng Trương Giác ở khu khóa, đã xóa ở bản cuối. Các bản v1 giữ lại để đối chiếu, không dùng làm bản bàn giao.

Phong cách sa bàn Đông Hán 3/4 khoảng 50 độ, vật liệu vẽ tay, ánh sáng ban ngày ấm từ trái trên, công trình lớn dần từ làng tới doanh trại và tế đàn. Chưa đo góc camera hoặc xác minh đường đi bằng scene thật.

## Prompt và nguồn sản xuất

Dùng built-in imagegen, không dùng CLI/API fallback. Prompt đầy đủ:

- `overview.prompt.txt`: tổng thể bảy ải theo ảnh mẫu.
- `revision.prompt.txt`: sửa ba xe và cổng gỗ.
- `opening-fog.prompt.txt`: trạng thái đầu chương.
- `opening-fog-revision.prompt.txt`: xóa NPC khu khóa còn thấy qua mây.

## Giới hạn và phần chưa làm

Chưa có Art Bible, manifest hoặc render 3D riêng Chương 1 bàn giao trong project tại lúc bắt đầu. Đây là concept dựa trên ảnh mẫu và tài liệu người dùng, chưa phải texture/decal/sprite đã kiểm đồng màu với model thật. Chưa tách bộ asset, atlas, nhập scene, lập trình tương tác, fog runtime, NavMesh hay lưu tiến độ. Không dùng ảnh toàn cảnh này thay mọi địa hình/công trình trong game và không coi map đã hoàn thiện.

Mục 25 docs mới nêu 30 FPS mobile, khác `GameplaySpecification.md` mục 1 đã chốt tối thiểu 60 FPS. Ghi nhận xung đột; hiện chỉ vẽ, giữ yêu cầu chung 60 FPS cho giai đoạn triển khai sau, không tuyên bố đã đo hiệu năng.
