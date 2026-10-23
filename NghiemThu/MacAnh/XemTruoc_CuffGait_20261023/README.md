# Mạc Ảnh — sửa cổ tay, vùng giày và chuyển động phụ, 23/10

Trang xem local: [xem_chuyen_dong.html](xem_chuyen_dong.html). Đã mở trong Edge.
Trước = bản 51e7b71 người dùng gửi ảnh lỗi; sau = bản CuffGait. 11 clip, 7 góc,
30 fps, cùng camera và thời điểm. Có nút phóng to chân khi xem Walk/Run.

Nguyên nhân xác nhận bằng ảnh tô vùng và đo mesh Unity:

- Chỉ dải nhỏ trên giày nhận weight Foot; phần lớn giày nhận Pelvis nên vẫn đứng tại chỗ.
- Tay áo dồn vào Sleeve_2, điểm treo không theo cổ tay; miệng tay áo rời bàn tay.
- Xương tóc/vạt áo có key nhưng nhiều phần bề mặt không nhận weight tương ứng.

Bản này tách lại bề mặt giày khỏi gấu áo, neo tay áo về cổ tay, sửa hình dáng vải
rủ và khoảng trống tua quạt; thêm ảnh hưởng nhẹ của chuỗi tóc/vạt áo, giữ tối đa
4 influence. Chuyển động phụ được bake, không phải mô phỏng vật lý realtime.
Không thay thời điểm hit/cast hoặc chuyển động bàn tay.

Kiểm chứng:

- Surface nguồn: 11 clip, mỗi frame, không vượt ngưỡng giãn/ép/gai.
- Surface Unity: 11 clip, 985 mẫu nửa frame, passed=true.
- Quạt nguồn: cả 6 phần, 985 mẫu; 0 giao cắt ngoài vùng bàn tay.
- PlayMode MacAnh: 28 pass, 0 fail, 1 ignored (Logs/MacAnh-cuff-results.xml).
- Trang Edge: 154 trạng thái, 6972 ảnh trước/sau tồn tại; chạy/dừng/tua/slider đạt.
- Đã xem các bảng so sánh 11 clip, ảnh tay cầm quạt cận và bước chân từ bên/trước.
- Giày nhập Unity: 735/903 đỉnh đi theo Foot phải/trái, trước là 241/422.

Nguồn local: docs/art/concepts/MuuSi_main-hero/MacAnh/Production/LOD/MacAnh_Battle_CuffGait.blend
SHA256: 65863f495de25a157991649e25679e6a7d1c1854431f357618a026ab5b9721d7.
Runtime BattleRig/LODActions FBX đã cập nhật local. File lớn và ảnh không commit.

Giới hạn: đây là bản sửa Battle để review; Portrait chưa được port. Các phép đo
không thay thế nghiệm thu nghệ thuật: vải rộng vẫn có nếp/góc cứng của topology
nguồn; không tuyên bố mọi góc đều đạt chất lượng cuối. Tóc/vạt áo có chuyển động
nhẹ, không phải toàn bộ phụ kiện đều có mô phỏng độc lập. Chưa đo FPS thiết bị.

Tái tạo: chạy repair_macanh_boot_regions.py trên MacAnh_Battle_ClothRepair.blend;
repair_macanh_cloth_binding.py trên kết quả; restore_macanh_secondary_weights.py
trên kết quả tiếp theo. Xuất export_macanh_lods.py, rồi Unity
Arena3Q.Editor.MacAnhArmBridgeQa.BuildFullReview. Baseline không được ghi đè.
