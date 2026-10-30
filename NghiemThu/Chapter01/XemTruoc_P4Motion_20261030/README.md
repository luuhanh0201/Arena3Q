# P4a: gió, tre và nước

Phần đầu P4 của bàn giao giai đoạn B, chưa phải hoàn tất P4 hoặc nghiệm thu cuối. Không đổi scene, mesh nguồn hoặc material dùng chung của Claude.

- Cây/cỏ dùng contract shader gió sẵn có của BattlefieldEnvironment. Hướng, tốc độ, mức uốn lấy BattlefieldConfig; map dùng35% độ mạnh, preset30 giảm còn45%; chu kỳ gió8s từ MapMotionConfig.
- Tre dùng material clone và shader theo độ cao world, chân giữ nguyên, ngọn lệch tối đa0,10m; preset30 còn0,05m. Pha lấy tâm cụm world để các cụm không đồng loạt kể cả khi static batching. Pass bóng và depth dùng cùng biến dạng.
- Suối cuộn UV.y dọc mesh với0,06 ô/s; lớp thứ hai0,03 ô/s trộn18%, preset30 tắt lớp hai. Giữ texture và tint của material nguồn, dùng ánh sáng PBR của URP.
- Đổi preset chuyển dần trong environmentFadeSeconds, không nhảy lớp nước hoặc biên độ. Khi hủy hệ thống, trả lại material/property block và global wind trước đó.
- Chỉ thay trình bày; không cấp sao, đổi tuyến đi hoặc mở ải.

Audit `Logs/Chapter01-P4-renderers.json`:1560 renderer, có30 renderer tre và1 suối; phần cây/cỏ đã dùng shader gió của chiến trường. Công cụ đọc scene không lưu lại scene. Bật data bằng `Arena3Q.Exploration.Editor.Chapter01PresentationBuilder.ConfigureEnvironment`.

Trước: `../P1_Runtime_20261028/run_20261030_233948_315` (1/1 capture,45ảnh khi chưa bật motion). Sau: `../P1_Runtime_20261028/run_20261030_234156_529` (45ảnh). Cùng camera/khung1672×941,3góc×3preset×5mốc0/0,55/1,1/1,65/2,2s. Đã xem ảnh cặp tre, ảnh suối và màu cảnh. Chuỗi này để so thay đổi, không phải video60Hz hay số đo FPS.

PlayMode Chương1 **20/20 qua**, `Logs/P4a-play.xml`. Bổ sung lifecycle sau đó **1/1 qua**, `Logs/P4a-lifecycle.xml`: tiến độ không đổi qua30/60/120, vật liệu nguồn không bị sửa, chuyển tầng nước có fade, trả lại material/property block khi hủy. Lượt baseline đầu có lỗi thiếu tiền tố Object trong test và đã sửa trước lượt baseline được dùng.

Gallery45 lựa chọn trước/sau và15 ảnh WebGL qua, không lỗi trang hoặc ảnh hỏng: `gallery-check.json`. Dựng bằng `node WebGame/chapter-p4-motion-review.cjs NghiemThu/Chapter01/P1_Runtime_20261028/run_20261030_234156_529 NghiemThu/Chapter01/P3_Web_2026-10-30T16-55-04-063Z`. Trang http://127.0.0.1:4322/chapter-p4-motion/. Đã mở ảnh so sánh và ảnh WebGL tre/suối preset60 để kiểm hình.

WebGL `P3_Web_2026-10-30T16-55-04-063Z`: **12 tổ hợp màn/preset,4 hồ sơ tiến độ,41 ảnh,0 lỗi**, có15 mẫu chuyển động thật qua30/60/120. Build `Logs/P4a-web-build.log` thành công0 lỗi/13 cảnh báo, player425.443.614 byte; content hash `cbd50c5ebb32b96b31c61a7647b5eff7e0bc328aa9ccd871e7bf4a8ae1bafd53`. Các cảnh báo API cũ/import không phải kết quả đo hiệu năng. Bộ lọc gallery và selector đã sửa sau khi phát hiện lấy cả ảnh fade khác; không thay kết quả test game.

Chưa làm trong phần này: cờ, lá rơi, bụi, chim, bóng mây/viền mây, lửa/khói/tế đàn, rương lắc/tia, decal2D, điểm âm thanh. Nhân vật thật/NPC animation chờ nguồn nhân vật; không animate capsule để coi là xong. Chưa đo FPS/p95/nhiệt/ASTC điện thoại. Material tham chiếu texture/source local đã có; nguồn lớn và ảnh/log không đưa lên Git.
