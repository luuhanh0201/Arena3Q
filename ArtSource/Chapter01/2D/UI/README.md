# UI bản đồ Chương 1

Nguồn hình vẽ tạo bằng công cụ imagegen tích hợp: huy chương gỗ/vàng, huy chương sơn đỏ boss, biển tên trống gỗ/giấy da, rương gỗ, bạc xanh, vàng đỏ. Mỗi tài nguyên được tạo riêng, nền alpha; nguồn đầy đủ giữ tại `Generated/` (bỏ qua Git). Bản sử dụng game ở `Assets/Arena3Q/Chapters/Chapter01_YellowTurban/Textures/UI`, huy chương/rương 256 px, biển tên 512 × 128. `export-ui.cjs` chỉ cắt khoảng alpha thừa và thu nhỏ, không vẽ lại.

Icon `circle-question-mark`, `lock`, `arrow-left`, `circle-alert`, `check` tải từ [Lucide](https://github.com/lucide-icons/lucide/tree/main/icons), giấy phép ISC tại `Assets/Arena3Q/Art/UI/Icons/Lucide_LICENSE.txt`; SVG nguồn ở `Icons/`. Dùng `render-icons.cjs` để xuất PNG trắng 128 px, cùng bộ icon hiện có của game. Kiếm dùng Swords đã có, sao dùng UiFactory/UiIconSet.

`exports.json` ghi kích thước nguồn và dung lượng bản game. Tài nguyên này chưa đồng nghĩa UI map đã được nghiệm thu; xem báo cáo runtime/web của chương.

30/10: bổ sung `altar-banner` bằng imagegen, tham chiếu `Generated/nameplate.png` để giữ cùng hoa văn, đổi thân băng sang sơn đỏ và viền vàng, nền trong suốt, không chữ. Nguồn ở `Generated/altar-banner.png` chỉ giữ local; bản game 512 × 128, 89.764 byte được commit cùng `.meta`. Chỉ gán qua `ChapterMapStage.useAltarBanner`, không ràng buộc số ải trong runtime.
