# Codex — START HERE (Arena3Q / Mặc Ảnh / Spec v1.1)

**Dữ liệu do người dùng xác nhận:** Nhân vật Mặc Ảnh ĐÃ CÓ model 3D, chiều cao **1,70 m**. KHÔNG dựng lại model và KHÔNG áp dụng kích thước cũ 1,1 m của nhân vật Lữ Bố.

Đọc `MacAnh_Production_Spec_v1_1.md` và ảnh trong `images/`, sau đó:

1. Tìm đúng asset 3D của Mặc Ảnh trong workspace Blender/Unity; nếu chưa được đưa vào repo, xin tệp hoặc đường dẫn. Kiểm tra rig, bàn tay, avatar, material, UV, scale, world height trước khi can thiệp.
2. Dựng **CHỈ VŨ KHÍ QUẠT** bằng Blender bpy thành asset tách biệt, đo theo bàn tay của model 1,70 m. Đừng rescale model để cho vừa quạt.
3. Dùng rig đã có để tạo các Action/Anim cần thiết, không ghi đè animation cũ. Nếu chưa rig, trình bày quy trình bổ sung rig trên bản sao để duyệt.
4. Dựng 7 bộ VFX và gameplay events Unity theo spec, dùng đúng hệ đơn vị m và socket vị trí thực tế.
5. Báo cáo file thay đổi, số đo scale, trạng thái test và phần chưa xác minh; không báo thành công nếu chưa chạy Blender/Unity.

**Thứ tự:** preflight model → weapon → rig/socket → Attack01 hoàn chỉnh → 6 chiêu còn lại → QA.

`images/` chứa ảnh concept do AI tạo làm tài liệu tham khảo, KHÔNG phải asset 3D hoặc texture game-ready. File model 3D gốc không được đóng gói trong bộ tài liệu này.
