# Xem trước – A2 phép chiếu chân tia Mặc Ảnh

Codex tự kiểm21/10, chờ Claude review độc lập. Trái trước/phải sau, cùng camera/frame.

- `Footprint_TruocSau.png`: MagicArray tại0,5s, camera projection0,75. Hệ số upright1,333333→1,0; lớp ground giữ nguyên.
- `VongPhep_Ultimate_TruocSau.png`: trận thật Skill01t1,65s và Ultimatet2,75s, zoom3,5. Chân tia khớp vòng hơn sau sửa.
- Nguyên nhân: billboard không bị camera nén như mặt đất; nhánh stretchToGround bỏ quên hệ số camera. Sửa nhân hệ số chiếu trước chia renderProjection; camera phẳng giữ phép chiếu cũ.
- Kiểm camera0°,41,40962°,60° và legacy. Baseline `Logs/CodexFix/20261021/FootprintBefore_1737.xml` fail đúng1,333≠1; `AfterA2_1738.xml`:22pass/0fail/1skip. Skip chỉ test xuất chuỗi60Hz vì chưa bật ARENA3Q_BLEND_EVIDENCE; test hoà khung bằng pixel đã chạy.
- Ảnh đơn trong `FootprintBefore_1737` / `AfterA2_1738`. Trận trước `MaskBattleAfter_1735`, sau `BattleAfterA2_1738` trong cùng thư mục log. Không chỉnh nội dung ảnh.
- Đây chỉ là sửa phép chiếu. Tia Ultimate còn sắc/đều, chất lượng khí tự nhiên và các mục khác vẫn mở; chưa nghiệm thu toàn bộVFX.
