# Kiểm bản game web 25/10/2026

Game WebGL thật dùng style UI mobile, chọn tỷ lệ Điện thoại/iPad/PC; mở luồng khởi động đầy đủ. Xem tại http://127.0.0.1:4322 trên PC này.

- Build cuối 339,7 MB, 0 lỗi / 8 cảnh báo, 382,9 giây: `build.txt`, log `Logs/WebGame-build.log`.
- Trang chọn hiển thị và Unity thật: `ui-verification.json`, `game-verification.json`, mỗi lượt 15 tổ hợp tỷ lệ/cửa sổ, 0 lỗi JS/console. Giữ lựa chọn, chỉ 1 loader, fullscreen và lỗi tải đạt.
- Chuột/bàn phím: `flow/result.json` và 16 ảnh. Chọn tướng, nhập tên, vào tutorial, triệu hồi; profile Unity ghi vào IndexedDB.
- Chạm giả lập: `flow-touch/result.json`, 16 ảnh và profile có tên Việt. Không thay thế thử thiết bị thật.
- Font: `flow-before-font/10-battle.png` trước, `flow/10-battle.png` sau; cận thẻ cùng vùng trong `font-before-after.png`, trái trước/phải sau. Đã sửa mất chữ tiếng Việt trên HUD web.
- `source-integrity.json`: 42 PNG icon và 8 tài nguyên UI đúng SHA nguồn mobile.
- `intro/result.json` và `intro/01b-intro-playing.png`: video thực sự phát 1920×1080, 42 frame đã giải mã tại giây 1,227.
- Ảnh responsive: `ui-<rộng>-<chế độ>.png`, `game-<rộng>-<chế độ>.png`.

Chưa kiểm Safari/điện thoại/iPad thật hoặc FPS thiết bị. Chưa đưa lên Internet. Gói giữ đủ scene Frontend/Battle/Battlefield_HighQuality; luồng đầy đủ dùng tutorial/default hiện có, chưa đổi map của FrontendConfig.

Báo cáo: [GameWeb_20261025.md](../../../docs/UI/GameWeb_20261025.md). Cách build/chạy/kiểm: [WebGame/README.md](../../../WebGame/README.md).
