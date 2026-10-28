# Quy tắc thiết kế UI trong game (mọi agent)

Người dùng giao ngày 24/10/2026.

**Phạm vi:** mọi chữ và icon hiển thị cho người chơi trong game: HUD trận đánh (`Scripts/UI`), các màn hình Frontend (`Scripts/Frontend`), chữ lấy từ asset nội dung (tên, mô tả kỹ năng, cốt truyện, thông báo...). Tài liệu trong `docs/`, comment trong code, công cụ Editor / dev và nội dung chat **không** thuộc phạm vi này.

**Cách làm (28/10/2026):** UI làm và duyệt trên bản web trước, ở chế độ "Điện thoại" phải hiển thị sát máy thật; APK chỉ build khi nghiệm thu trên máy. Chi tiết: `QuyTacLamViec.md` §3.

## Định dạng và icon

1. **Không sử dụng Em Dash (`—`) hoặc En Dash (`–`)** trong văn bản. Thay bằng dấu gạch ngang thông thường (`-`), dấu phẩy, dấu hai chấm hoặc dấu chấm tùy ngữ cảnh.
2. **Không sử dụng Emoji Unicode** dưới bất kỳ hình thức nào, bao gồm emoji có màu, biểu tượng cảm xúc, dấu tích, cảnh báo, tên lửa hoặc emoji trang trí.
3. **Cho phép sử dụng icon từ các thư viện chuyên dụng**, chẳng hạn:
   - Font Awesome
   - Lucide Icons
   - Heroicons
   - Material Symbols
4. Ưu tiên icon dạng vector (SVG) hoặc monochrome, có thể tùy chỉnh màu sắc.
5. Khi viết UI, sử dụng icon từ thư viện đã cài đặt. Không thay thế icon bằng Emoji Unicode.
6. Giữ định dạng chuyên nghiệp, tối giản, nhất quán. Không lạm dụng icon trong tiêu đề hoặc danh sách.

## Áp dụng trong Unity (uGUI)

- HUD và Frontend dựng bằng uGUI từ code, không có CSS hay icon component. Icon lấy file SVG từ một thư viện ở trên, chuyển thành sprite trắng một màu (nền trong suốt) và đổi màu bằng `Image.color`; ghi rõ thư viện và giấy phép cạnh file icon (repo công khai).
- Icon hiện có: `Assets/Arena3Q/Art/UI/Icons/` (SVG gốc, PNG trắng 128 px, giấy phép Lucide), khai báo trong asset `Content/Resources/UiIcons.asset` (`UiIconSet`); code dùng `UiFactory.AddIcon` / `UiFactory.AddStars`. Thêm icon mới: tải SVG từ thư viện, xuất PNG trắng nền trong suốt, nhập dạng Sprite (Single), thêm một trường vào `UiIconSet`.
- Ký hiệu Unicode dùng làm icon (ví dụ `★`, `✓`, `⚠`) cũng tính là emoji / icon trang trí: thay bằng sprite icon.
