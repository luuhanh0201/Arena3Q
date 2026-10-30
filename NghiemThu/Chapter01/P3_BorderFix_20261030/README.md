# P3 vòng 3: sửa ranh sương khi mở khu

Theo review Claude ngày 30/10 16:58 cho bản b187778. Trước sửa, hai phía ranh khu đang tan/khu khóa lấy tiến độ khác nhau; khi visibility đạt 1, cả dải biên đổi trong một khung.

Shader nay tính trường sương trước và sau khi mở khu, trộn bằng cùng tiến độ/noise của khu đang tan. Ngưỡng được đẩy ra ngoài miền giá trị field ở đầu/cuối, nên không có bước nhảy khi thay tập khu mở. Trình tự mở khu vẫn tuần tự, thời lượng và dữ liệu gameplay giữ nguyên. Không sửa nguồn model/scene map.

## Kiểm bằng pixel

Render shader sản phẩm thành mask đen trên nền trắng, camera cố định, khung 960×480 bao phủ 80×40 m; loại cạnh cây, công trình, HUD và post-processing khỏi phép đo. Ba tâm vùng lần lượt mở/đang tan/khóa. Lấy 121 mức tiến độ cho mỗi preset, tổng 363 ảnh đo; không phải phép đo FPS thiết bị.

| Chênh lệch lớn nhất, thang 0–255 | Trước | Sau | Ngưỡng |
| --- | ---: | ---: | ---: |
| Hai mức tiến độ kế nhau (1/120) | 255 | 24 | ≤64 |
| Bước đầu/cuối | 255 | 0 | ≤4 |
| Hai pixel liền nhau | 255 | 26 | ≤32 |

Đối chứng cũ đúng độ phân giải: `../P1_Runtime_20261028/run_20261030_171349_351/fog-pixel-continuity.tsv`; bản sửa: `../P1_Runtime_20261028/run_20261030_171457_106/fog-pixel-continuity.tsv`. Test cố ý thất bại trên shader b187778, qua trên shader sửa. Các lượt thử thiếu uniform hoặc 320×160 không được dùng làm đối chứng cuối; diễn biến ghi trong nhật ký 3D.

PlayMode **16/16 đạt**, `Logs/P3r3-play-final.xml`. 30 khung cảnh thật (10/preset), 3 ảnh cận và ranh Trại Lương ở `run_20261030_171457_106`; ảnh trước cùng camera ở `run_20261029_105045_038`. Đã xem riêng khung 8/9 và 9/9, cầu/hàng rào xuất hiện liên tục qua biên mềm.

EditMode **7/7 đạt**, `Logs/P3r3-edit.xml`. Build WebGL thành công trong 117,3 giây, 0 lỗi/3 cảnh báo; `Logs/P3r3-web-build.log`. Player 425.292.927 byte (405,6 MiB), không phình cache. Content hash: `1713bcedeed0b72efb7a01e7c92069868595758a10b00abebdd22025f75f405f`.

WebGL đã qua **12 tổ hợp màn hình/preset + 3 hồ sơ tiến độ, 25 ảnh, 0 lỗi**; kết quả `../P3_Web_2026-10-30T10-22-29-044Z/result.json`, hash khớp player hiện tại. Gallery: `http://127.0.0.1:4322/chapter-p3-border-fix/`, dựng bằng `WebGame/chapter-p3-border-review.cjs`. Đây là bản gửi Claude review vòng 3; người dùng chưa nghiệm thu cuối, FPS/ASTC/nhiệt điện thoại chưa đo. Lời dẫn khó đọc trên cây rậm ghi nhận cho P4 theo review, chưa sửa ở đây.

Claude review vòng 3 ngày 30/10 17:34: **ĐẠT khoảng 95%**, đã chạy độc lập PlayMode16/16, EditMode5/5, xem khung 7/8/9 và xác nhận không còn đường cắt cứng hoặc nhảy cuối. P3 đã được đánh xong trong task; nghiệm thu cuối của người dùng và hiệu năng thiết bị vẫn chưa hoàn tất. P2 còn chờ review riêng theo ảnh mẫu.
