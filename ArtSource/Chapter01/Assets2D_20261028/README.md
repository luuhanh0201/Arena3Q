# Chi tiết 2D Chương 1

Bộ vẽ theo `docs/Ai/Arena3Q_Chuong1_Asset_2D_va_Nguon.xlsx`, `docs/Ai/Chuong1.md`, `docs/QuyTac/QuyTacLamViec.md` và `docs/QuyTac/ThietKeUI.md`. Chỉ vẽ trước theo yêu cầu người dùng, chưa sửa code game hoặc nhập Unity.

Workbook có 69 mục: 14 mục 2D, 14 hybrid, 41 giữ 3D. Bản đọc đầy đủ bốn sheet: [workbook-extract.json](workbook-extract.json). Dùng các cột P/Q để chọn phần vẽ, không thay công trình, nhân vật và vật cản cần hình khối bằng ảnh.

[Đối chiếu từng mục](Coverage.md): 24 mục có hình vẽ, 3 mục chỉ chọn nguồn particle sẵn có, 1 mục cây lá rộng chờ render từ model. Các mục dùng chung hình được ghi rõ, không cộng hai lần vào số PNG.

## Xem hình

Mở [gallery.html](gallery.html): mỗi tấm có tên, mã mục workbook và liên kết ảnh gốc. Nền sáng/tối giúp xem viền trong suốt. Đây là trang xem bộ hình, không phải giao diện game hay bằng chứng đã tích hợp.

Các tấm nhiều biến thể là sheet nguồn, chưa phải atlas runtime hoặc các sprite đã cắt/pivot. PNG lớn giữ local, không Git/LFS. Prompt và danh mục nhỏ được commit. Không có PSD/layer vẽ tay; nguồn tái tạo là prompt và PNG sinh bằng built-in imagegen.

## Quy tắc mỹ thuật áp dụng

- Stylized 2.5D vẽ tay, sa bàn nông thôn Đông Hán, gỗ cũ/đất/lúa; không PBR bóng, voxel, pixel art hoặc viền đen dày.
- Dùng palette khởi điểm của Chương 1: đất B99B6B, đường D3B786, cỏ 748358, cỏ úa A49B62, lúa C8A44C, gỗ 75553B, đá 898379, cờ C7A130, nước 66999A. Đây chưa phải palette đã đối màu model thật.
- Texture mặt đất nhìn thẳng từ trên xuống, tránh bóng đổ cố định. Sprite chỉ có sắc độ nhẹ gợi hình, tránh bóng tiếp xúc vẽ cứng gây hai bóng. Material dự kiến URP Lit + Alpha Clipping cho foliage/đạo cụ theo workbook; chưa tạo material hoặc normal/roughness map.
- Tán trúc tách khỏi thân; cờ tách cán; song cũi tách khung; vũ khí tách giá; đuốc tách lửa. Hàng rào và cây khô 2D chỉ dùng trang trí xa, không thay vật cản/NavMesh.
- Bông lúa rủ, lá hẹp; không dùng bông lúa mì. Cờ dùng họa tiết hình học đơn giản, không sinh chữ giả. Không vẽ icon UI bằng AI: tiếp tục thư viện Lucide/Heroicons của project khi làm UI.
- Giữ chi tiết thấp ở mặt đường, tán và đạo cụ lùi khỏi điểm tương tác. Không đưa núi backdrop vào tuyến di chuyển hoặc quay lại bố cục vách cao chia cắt của bản cũ.

## Phạm vi và phần dùng lại

`prompts.json` ánh xạ mỗi PNG tới STT workbook. 29 tấm gồm 8 texture kín và 21 sheet/decal nền trong suốt. Nhóm 46 và 47 dùng chung tấm vũ khí trang trí. Núi ngoài biên (3) dùng cho hậu cảnh, không phải vách sát đường.

Mục 11 cây lá rộng: workbook yêu cầu impostor render từ chính model 3D. Chưa có render chuẩn Chương 1 nên chưa tạo ảnh thay giả. Đợi model/camera/ánh sáng chuẩn để xuất impostor, không coi mục này đã xong.

Mục 36, 37, 50 và phần lửa của 51, khói nhà mục 20: dùng bộ particle có sẵn theo rule ưu tiên nguồn VFX. [Reuse-VFX.md](Reuse-VFX.md) chỉ rõ nguồn, giấy phép và phần chưa triển khai. Đây là chọn tài nguyên hình, chưa có flipbook/chuyển động hoặc hiệu ứng đã nghiệm thu.

Tranh chọn ải dùng bản concept `../Concepts_20261028/05_chapter-navigation-view.png` để tham chiếu; chưa tách các nhãn khỏi tranh. Không dùng ảnh toàn cảnh làm mặt đất thay scene 3D. Bộ này bổ sung lá/cánh hoa, vết bánh xe/dấu chân/vũng nước và vòng phù chú. Thác xa có chuyển động, mây khóa và bóng blob được ghi trong danh sách dùng lại; chưa tạo animation bằng ảnh tĩnh.

## Chất lượng 30 / 60 / 120 FPS

Định hướng cho giai đoạn ghép cảnh, chưa đo hiệu năng: 30 giảm mật độ cỏ/lau, lá bay và khói trang trí; 60 dùng mật độ mặc định; 120 tăng độ mượt trên màn hỗ trợ, không bắt buộc tăng số sprite. Texture nguồn giữ nguyên để xuất cùng atlas và mức mip thích hợp. Mọi vật thể cần nhận diện nhiệm vụ, vùng cảnh báo, đường đi, va chạm và thời gian gameplay phải giống nhau; chỉ bớt trang trí. Không coi ba cấu hình đã được test từ bộ PNG này.

## Kiểm tra và giới hạn

Kết quả lượt vẽ: đủ 29/29 PNG, tổng 55.729.917 byte (khoảng 55,73 MB). 8 texture kín alpha 255; 21 tấm cutout đều có alpha 0 và nội dung nhìn thấy qua kiểm lấy mẫu. Kích thước nguồn thực tế trong images.json, không phóng ảnh để tuyên bố 2K/4K. Trình duyệt Edge tải 29/29 ảnh, kiểm lưu trong gallery-check.json; ảnh qa-reeds-light.png, qa-reeds-dark.png và qa-gallery.png đã mở xem. Không thấy viền đỏ/vàng từ cách hiển thị ảnh alpha trong chat trên ảnh ghép nền trình duyệt.

Mở kiểm từng ảnh về silhouette, số cụm, khoảng trống, màu và nội dung. `inspect-images.ps1` đọc kích thước, hash và lấy mẫu alpha thật, không chỉ tin nền trong suốt hiển thị trong chat. Dữ liệu ghi ở `images.json`. Texture được yêu cầu lặp nhưng chưa bảo đảm seamless chỉ từ prompt; cần kiểm ghép 2x2 và mip/seam trước khi nhập game.

Chưa có Art Bible/render 3D Chương 1 làm chuẩn. Theo Chương 1 §2, chỉ đánh dấu hoàn thành tích hợp sau khi đặt 2D/3D trong cùng scene, kiểm trước/sau, che khuất, color grading, góc camera và texel density. Hiện là bộ vẽ nguồn chờ ghép, không phải map hoặc asset production đã nghiệm thu.

## Tái tạo

Dùng built-in `imagegen`, mỗi nhóm asset một lệnh, `transparent_background` đúng theo `prompts.json`; không CLI/API fallback. Lưu nguyên PNG từ generated_images vào đây. `read-workbook.ps1` chỉ đọc XLSX; `inspect-images.ps1` chỉ kiểm PNG. Không sửa workbook người dùng.
