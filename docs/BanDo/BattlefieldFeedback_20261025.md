# Battlefield: phản hồi điện thoại 25/10/2026

Đã sửa 3/3 nhóm yêu cầu: bỏ nền ngoài biên, phe ta đỏ/tướng lớn hơn, công trình lớn hơn và đúng nguyên/vỡ. Scene `Assets/Arena3Q/Scenes/Battlefield_HighQuality.unity`; scene cũ và model gốc giữ nguyên.

Chi tiết thay đổi, cách chỉnh và ảnh trước/sau ở `NghiemThu/Battlefield/XemTruoc_ChinhSua_20261025/README.md`. Kết quả tự kiểm cuối: **15/15 PlayMode**, không lỗi shader/compile trong lượt cuối. Số đo render và ảnh ở cùng thư mục; chỉ là kiểm Unity Editor, chưa có FPS của bản sửa trên thiết bị. Code/scene của bản sửa ở commit `1133172`, đã push.

Trang xem ảnh mới: http://127.0.0.1:4311/app/feedback.html. Đã kiểm cả7ảnh tải đúng kích thước, trang412px không tràn ngang, không pageerror; chỉ đọc. Bản map trước vẫn ở `/app/map.html`. Server local PID24500, không mở ra mạng ngoài.

Đối chiếu40file nguồn của map trước:39file giữ nguyên SHA256 (scene Battle cũ, model, texture, cấu hình gốc khác); Resources/BattleView.asset chỉ thêm hai màu phe, mọi trường cũ giữ nguyên. Kiểm semantic riêng cho ngoại lệ có chủ đích này, không gọi đó là40file nguyên vẹn.

Mặt đất gộp vẫn là 80×44 qua bốn mesh nhỏ; phần đồi/nền mở rộng ngoài khung đã bỏ theo lựa chọn người dùng. Cây lớn chuyển vào mép map; navigation nhận footprint thân cây. Công trình tăng x2 nên bố trí hai lều mỗi trại và nới khoảng cách hàng rào. Tế đàn, trụ và cổng giữ tỉ lệ đang dùng. Màu nhận diện phe lấy từ dữ liệu; không sơn lại da/quần áo/vũ khí tướng.

Nhà chính Editor dùng preview Command_1 (ẩn khi Play); trong trận dùng đúng lifecycle CastleUnit/CastleFallView. Phá nhà rung rồi thay sang Command_2, giữ màu đỏ của ta hoặc xanh của địch và tỉ lệ mới. Kiểm cả Attack lẫn Defense bằng sát thương thật và chụp sau khi sập.

Build riêng bằng `Arena3Q > Battlefield > Build Android Test APK`, output `Builds/Android/Arena3Q-Battlefield-Feedback-20261025.apk`, giữ APK trước để quay lại. Lần build mới và kiểm chữ ký/dung lượng sẽ ghi bên dưới sau khi hoàn tất.

Thiết bị hiện không xuất hiện trong `adb devices -l`; chưa cài hoặc xác nhận render của bản sửa trên điện thoại. Không theo dõi inbox/thiết bị sau bàn giao khi người dùng chưa yêu cầu.
