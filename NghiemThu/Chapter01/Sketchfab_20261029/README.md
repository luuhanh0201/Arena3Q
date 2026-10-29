# Chuẩn hóa Sketchfab Chương 1, 29/10/2026

Codex tiếp bàn giao Claude theo lệnh người dùng. **22nguồn →47mảnh chuẩn hóa**, nhập Unity URP và chụp51ảnh gồm4mẫu trong game để đối chiếu. Đây là tự kiểm của Codex, chưa phải review độc lập hoặc duyệt đưa vào map.

Xem ảnh: [trang local](http://127.0.0.1:4322/sketchfab-review/), hoặc `index.html` trong thư mục này. Mỗi cặp Blender là **trái trước, phải sau**, cùng camera/khung, bốn góc, hình người xanh1,8m. Before đã tách mảnh trước khi giảm mesh/texture; riêng SF04 giữ nền để thấy phần đã bỏ. Nguồn nguyên bản có ảnh riêng. Unity dùng ánh sáng/URP của trận, cùng khung giữa ứng viên của một nhóm; đây là scene kiểm asset, không phải bản map đã thay model.

## Kết quả đã kiểm

- Chuẩn hóa theo cấu hình mét; giữ tỉ lệ tương đối cùng bộ; gốc giữa đáy hoặc giữa đỉnh với SF23/SF24. SF05 tách4nhà; SF13 tách8mảnh; SF15 lấy9cây cao,4cụm,3thân.
- Xóa18tam giác suy biến ở các nguồn; kết quả0mặt suy biến,0đỉnh không hữu hạn. Tổng186.419tam giác nếu đặt mỗi mảnh một lần, không phải ngân sách một cảnh.
- SF04 bỏ đúng10mặt nền cỏ, không còn viền xanh. SF02 bỏ2đế trùng; SF07/SF08 bỏ các phần ngoài đình theo cấu hình.
- SF08 giảm13.584→7.760tam giác; SF21 giảm11.545→5.003. Xem ảnh bốn góc không thấy rách mái/đứt chân lư hương.
- 151texture dẫn xuất không vượt1024px, kiểm kích thước thực trên đĩa. Normal/metallic map non-color; base/emission sRGB; lá tre và hoa dùng alpha clip theo từng vật liệu.
- Unity xác minh47prefab: kích thước sai lệch≤0,005m, pivot đúng, UV không mất, số tam giác khớp FBX và mọi đỉnh hữu hạn. Capture51/51ứng viên; đã xem bốn contact sheet và ảnh cận SF04/SF08/SF12/SF15/SF19/SF21.
- Nguồn glTF giữ nguyên SHA256 so với lúc normalize. Không thay scene/map/gameplay.

## Giới hạn và nguồn lệch mục đích

- Chiều cao là đề xuất của cấu hình bàn giao, chưa được người dùng duyệt. Ba nhà SF13 còn10–29k tam giác, chưa có LOD/FPS thiết bị; chưa tuyên bố sẵn sàng mobile.
- SF20 là đảo ruộng bậc thang, không thay ruộng phẳng Ải2. SF26 là hành lang có mái, không thay cầu qua suối.
- Các mảnh mang mã `LanternString01..03` của SF13 thực tế là các cặp đèn đứng trong bộ nguồn, không có dây nối. Tên mã giữ theo cấu hình bàn giao; ảnh là căn cứ chọn dùng.
- Vật liệu transmission/alpha blend được đơn giản hóa sang opaque/cutout trong preview URP; chưa có mô phỏng truyền sáng riêng.
- Đã kiểm trực tiếp cả22 `license.txt`: đều CC BY4.0. SF13 tiêu đề ghi CC0 nhưng file giấy phép ghi CC BY4.0, nên ghi công cả22nguồn tại `ArtSource/Chapter01/3D/Catalog/SKETCHFAB_CREDITS.md`.
- FBX, texture, prefab và ảnh lớn chỉ ở máy theo quy tắc repo. Git lưu script/config/importer/catalog/ghi công/báo cáo, dựng lại theo `ArtSource/Chapter01/3D/Sketchfab/README.md`.

## Bằng chứng

`ArtSource/Chapter01/3D/Renders/Sketchfab/normalization.json`, `Unity/import.tsv`, `Unity/report.tsv`; `Logs/Sketchfab-normalize-materials.log` + `Sketchfab-normalize-degenerate.log`; `Logs/Sketchfab-unity.log` (`SKETCHFAB_IMPORT_PASS pieces=47`); `Logs/Sketchfab-unity-render.log` (`SKETCHFAB_REVIEW_PASS candidates=51`, exit0). Ảnh Blender cuối ở `Comparison`, nguồn EEVEE ở `Source`.

Gallery đã kiểm bằng Edge/Playwright: HTTP200,120ảnh tải đủ,0ảnh hỏng,0pageerror, không tràn ngang ở390px. Bằng chứng `checks.json`, `browser-checks.json`, `browser-SF04.png`, `browser-mobile-SF21.png`. Cả22nguồn đã render lại EEVEE (`Logs/Sketchfab-source-eevee-fixed.log`, `PREVIEW_DONE`);47ảnh so trước/sau đầy đủ. Lượt browser trong sandbox thoát mà không có marker/kết quả, không tính đạt; lượt ngoài sandbox hoàn tất `GALLERY_QA_PASS`.

Commit công cụ đã push: `746eab1`; bản bàn giao ảnh/catalog được commit riêng tiếp theo. Browser mở tương tác bị Windows chặn trong sandbox; trang local vẫn truy cập được và ảnh đã được xem bằng công cụ.
