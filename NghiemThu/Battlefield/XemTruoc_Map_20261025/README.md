# Bằng chứng chiến trường Unity ngày25/10/2026

Scene: `Assets/Arena3Q/Scenes/Battlefield_HighQuality.unity`. Xem: http://127.0.0.1:4311/app/map.html. Hướng dẫn/phạm vi/giới hạn: `docs/BanDo/BattlefieldHighQuality_20261025.md`.

- `original-battle.png`: scene nguồn trước thay đổi; `phase2-first-pass.png` và `before-instancing-high.png`: ảnh các lượt trước. `phase2-overview.png` là ảnh toàn cảnh scene hiện tại (tên giữ theo lượt capture ban đầu).
- `play-five-heroes.png`, `play-defense.png`, `play-altar.png`: PvE và mặt tế đàn thật.
- `quality-{low,medium,high}.png`, `battle-{low,medium,high}.png`: ba cấu hình và lượt có5tướng.
- `occlusion-before.png`, `occlusion-faded.png`: cây che tướng và dither fade đã hoạt động, phục hồi cũng được kiểm.
- `wind-a.png`, `wind-b.png`, `wind-pixel-check.txt`:27.822pixel đổi giữa hai pha trong cùng lượt, không chỉ đọc giá trị time.
- `motion-000..035.png`, `environment-motion.mp4`:36 khung Unity,12FPS, clip3giây. Encode bằng ffmpeg có sẵn trong Krita, không mô phỏng chuyển động bằng trình duyệt.
- `play-apk-results.xml`/log:7/7đạt, thêm kiểm nút đổi đồ họa và Công/Thủ trên bản test. `play-delivery-results.xml`/log:6/6đạt. `original-camera-results.xml`/log:2/2đạt. Các lượt trước có cả lỗi/crash công cụ đã sửa, giữ nguyên log để truy vết, không dùng làm kết quả cuối.
- `render-{Low,Medium,High}.txt`: ProfilerRecorder/UnityStats thực; drawcall201/227/231, tam giác652.414/1.070.160/1.360.186. Editor1600×900, không phải FPS máy thật. `before-instancing-render-*` giữ số đo trước tối ưu.
- `source-integrity.json`:40 file nguồn không đổi SHA256. `validation.txt` rỗng nghĩa không có lỗi Stage/spawn/material ở lượt Generate cuối.
- `web-verification.json`, `web-map-desktop.png`, `web-map-mobile.png`:10 ảnh decode, video phát, không lỗi/tràn mobile; server chỉ đọc.

APK `Builds/Android/Arena3Q-Battlefield-20261025.apk`:365,1MiB, build0lỗi/10cảnh báo directive cũ, ARM64/API26+. `android-apk-verification.json` và `apksigner.txt`: v2hợp lệ, cùng signer bản trước, downloadHEAD200/content-lengthđúng,40file nguồn không đổi. APK mở thẳng map mới, có nút chất lượng và Công/Thủ. SHA256 `6aa9abe43eedb39539a11ef54db76116d92877cb6aa6e609e444b3a0b95816d4`.

Chưa nghiệm thu hình thương mại cuối, chưa có asset đúng loài/kiến trúc còn thiếu, chưa bake lightmap và chưa chạy/đo FPS/bộ nhớ thiết bị. Ảnh/clip/log/APK lớn giữ local; README được theo dõi trong Git.
