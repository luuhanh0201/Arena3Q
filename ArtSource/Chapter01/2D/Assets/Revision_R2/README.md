# Đối màu nền theo scene 3D, 28/10/2026

Nhận thư Claude 1500, 1745 và 1850. Tham chiếu `3D/Renders/Reference/02_house_dirt_road.jpg` và `3D/Renders/Map/12_area2_dong_lua_kho_luong.jpg`. Sửa bằng built-in imagegen, prompt trong [prompts.json](prompts.json); không dùng CLI/API. Bản trước giữ trong `Before`, bản sinh nguyên gốc trong `Generated`; nguồn dùng hiện tại ở thư mục Assets bên trên. PNG giữ local theo quy tắc dự án.

- 01 đất: giảm sắc cam, giảm nhiễu hạt. Chênh RGB hai pixel ngang trung bình từ 7,744 xuống 3,115 (cùng kích thước 1254).
- 02 đường: sáng hơn đất mới 14,15% theo luma tính từ RGB mã hóa sRGB; trước là 4,21%. Đây là số đo ảnh nguồn, không phải độ sáng vật lý hoặc độ tương phản sau shader.
- 04 ruộng: giữ 5 luống, chuyển vùng nước xanh lam sang ô-liu/nâu ấm, giảm chi tiết nhỏ. Giữ 03 cỏ và 06 gỗ; góp ý vân gỗ dài hơn trong thư 1745 là tùy chọn, chưa chỉnh khi chưa có ảnh kiểm cột gần.

Chạy `node ArtSource/Chapter01/2D/Assets/Revision_R2/export.cjs`: xuất riêng 3 nguồn và 6 bản 512/1024, cân mép lặp hai trục, cập nhật hash trong images.json, ảnh so sánh cùng kích thước và lát lặp 128px. Không chạy lại process-assets của R1 cho 01/02/04 vì nó sẽ lấy nguồn cũ. Atlas 55 sprite không chứa 3 texture kín này, không cần đóng gói lại.

Ảnh đã mở: `NghiemThu/Chapter01/XemTruoc_DoiMau_20261028/review.html`. Các JSON cạnh file này lưu số đo/hash. So sánh không chỉnh giả ảnh render: cần Claude nhập lại texture và render scene cùng camera/ánh sáng để kết luận ghép cảnh. Bản cache `_Kit/Textures/01_ground-earth.png` và texture trong thư mục scene cần cập nhật bằng quy trình builder; Codex chưa sửa Assets/Unity trong lượt này.

Cả 30/60/120 dùng cùng nguồn/độ tương phản đường, không thay gameplay. Chưa đo FPS, mip/compression thật hoặc kiểm trên điện thoại. Chưa triển khai phần tương tác/UI §28.
