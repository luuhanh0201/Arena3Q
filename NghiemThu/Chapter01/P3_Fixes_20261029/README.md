# Sửa review P3 ngày29/10 10:28

Tiếp phản hồi Claude về commit9665f64. Đây là bản tự kiểm gửi review lại, không tự đánh dấu người dùng nghiệm thu cuối.

- Dấu ải dùng danh sách vùng che trong `MapUiConfig`: Back, Title, Information, Introduction, ChapterRewards, CurrentQuest, Resources. Dấu bắt đầu fade ở lề18px; nếu nhảy camera khiến hình dấu giao HUD thì alpha0 ngay và không nhận chạm. Kiểm cả tên khu/sao/hit-area, bao gồm scale/pulse cuối của frame.
- Chữ nhiệm vụ chừa khoảng trống theo bán kính mũi tên khi xoay và lề 12 px, TMP tự co 14–17 px và có ellipsis dự phòng. Mũi tên xoay quanh tâm, giữ đúng vị trí hình gốc. Bảy câu hiện tại không overflow và không giao mũi tên ở 8 góc xoay.
- Sương dùng noise tuần hoàn128² do builder tạo, không còn ảnh khói đơn lấy bằng `frac`. Các lớp khác hướng/rotation/tỉ lệ;30FPS2lớp,60/120FPS3lớp.
- Viền tan so ngưỡng tiến độ với noise/khoảng cách tâm. Mức0 phủ đủ, mức1 sạch; thời lượng2,8s không đổi. Dải chuyển ranh nằm bên khóa theo khoảng cách tâm mở/khóa, bằng0 ở phía mở.
- Đã kiểm State_Before/After tại0/50/100% ở hai khu có art (1và4). Với asset hiện có không thấy mái xuyên tường ở frame50%; chưa đổi fade trong suốt sang dither. Các khu khác vẫn thiếu State_After, không tạo nhóm rỗng để giả hoàn tất.

Kiểm đã chạy: **PlayMode14/14**, `Logs/Chapter01-P3-review-flow.xml`; **EditMode7/7**, `Logs/Chapter01-P3-review-core.xml`; **capture trạng thái1/1**, `Logs/Chapter01-P3-review-fade.xml`. PlayMode mới kiểm7câu nhiệm vụ và giao HUD ở4tỉlệ qua7vị trí camera; thêm30ảnh tan sương (10/preset),3ảnh cận và1ảnh ranh Trại Lương.

Ảnh Unity: `../P1_Runtime_20261028/run_20261029_105045_038`, chuyển trạng thái `run_20261029_105527_261`. Capture sương tắtSRPBatcher riêng do lỗi batchđãghi trước; màu cuối phải đối chiếu playerWebGL. Nguồn cảnh/model giữ nguyên.

Sau sửa pivot, test bổ sung đạt **1/1**, `Logs/Chapter01-P3-review-arrow.xml`. Player WebGL cuối đã qua **12 tổ hợp màn hình/preset**, 3 hồ sơ tiến độ (1/4/7 ải), tương tác touch/quest/trận/về sảnh và **25 ảnh**, không lỗi trình duyệt. Kết quả: `../P3_Web_2026-10-29T08-20-20-323Z/result.json`. Đã xem ảnh 7 ải: mũi tên tách khỏi chữ nhiệm vụ dài, dấu không xuyên tiêu đề/lời dẫn. Gallery `http://127.0.0.1:4322/chapter-p3-fixes/` đã kiểm ảnh và điều khiển, kết quả `gallery-check.json`.

Build sạch dữ liệu thành công trong 112,6 giây, 0 lỗi và 3 cảnh báo; log `Logs/Chapter01-P3-review-web-clean-data.log`. Lượt trước có file `sharedassets0.assets` dư trong cache sau phiên bị ngắt, làm player tăng 232,5 MiB; đã sao lưu cache và build lại. Player cuối **425.294.529 byte (405,6 MiB)**. Không có thêm asset trong báo cáo build và hash WebAssembly giữ nguyên: `1758f147987aeea997c10eaa780a625856ca0839046054afe0f10e4576cfcb1d`. Thư mục output tổng còn chứa các gallery QA nên không dùng số MB toàn thư mục làm dung lượng game.

Chưa đo FPS/ASTC/nhiệt trên điện thoại; sương vẫn cần một bản sao màu camera mỗi frame. Bản này chờ Claude review lại và người dùng kiểm lần cuối.
