# UI, âm thanh và icon theo yêu cầu 24/10

Codex tự kiểm, không dùng Claude theo chỉ đạo hiện hành.

- Loa/ngôn ngữ/cài đặt neo phải và thẻ Khách neo trái trong safe area, độc lập khung nội dung 1280. Ảnh 16:9, 2340×1080 và cutout giả lập có trong `xem_ui.html`.
- Chạm ngoài đóng form đăng nhập/đăng ký, chọn server, liên kết, cài đặt, xác nhận đăng xuất và lỗi mạng. Bên trong panel không đóng; server hủy trả lại lựa chọn cũ, không lưu lựa chọn tạm. Đăng nhập đã đóng có nút mở lại/Chơi ngay. Callback đến muộn bị bỏ qua.
- Theo nhịp cuối người dùng yêu cầu: màn hiện 0,35 s theo smoothstep, giữ ảnh cũ không nhận chạm; popup hiện/tan 0,24 s. Vào/ra Battle có lớp che xuyên lúc nạp scene. Thẻ Khách nằm dưới popup để không che nút Back; đã kiểm bằng raycast thật.
- BGM mặc định 65% thay vì 12%; giữ preference người chơi đã chỉnh. Intro nhân gain nguồn 2 lần qua limiter: trung bình từ -18,3 dB đến khoảng -13,6 dB ở bản đo đầu; giới hạn đỉnh, không đổi hình. Cả 90 hash khung giải mã trước/sau giống nhau (`before/intro-frames.md5`, `after/intro-frames.md5`). File loud mới local, script tái tạo `docs/UI/Tools/prepare-intro-audio.ps1`.
- Bổ sung yêu cầu mới: bỏ 45 giây đầu BGM, còn đúng 255 giây. Nguồn dẫn xuất WAV local giữ chính xác điểm cắt theo mẫu; runtime là Vorbis streaming, loop không quay về phần 45 giây đã bỏ. Script tái tạo `docs/UI/Tools/prepare-bgm.ps1`; không sửa OGG nguồn hoặc âm thanh intro.
- Icon dùng logo gốc `logo_arena3q.png`, không kéo méo. Icon mặc định và 6 kích thước adaptive Android đã gắn. Ảnh trước/sau icon nằm trong `comparisons/BeforeAfter_AppIcon.png`.

## Kiểm chứng

- EditMode toàn bộ: 271/271, `Logs/UI-polish-full-edit.xml`.
- PlayMode toàn bộ: 153 qua, 0 lỗi, 1 ignored có sẵn, `Logs/UI-polish-full-play.xml`.
- Sau chỉnh lớp Edge/Back, UI + Frontend cuối: 26/26, `Logs/UI-polish-play.xml`.
- Kiểm cuối đường vào trận, giữ hình truyện đến khi lớp che kín: Frontend 6/6, `Logs/UI-polish-scene-final.xml`. Kiểm asset BGM đã cắt: 4/4, `Logs/UI-polish-bgm-edit.xml`.
- BGM runtime phát/lặp/lưu setting 1/1 (`Logs/UI-polish-bgm-play.xml`); nhịp chuyển trang 0,35/0,24 s chạy lại UI + Frontend 26/26 (`Logs/UI-polish-fast-play.xml`).
- 27 ảnh Unity, 20 ảnh so sánh (có icon), các khung giữa chuyển màn và tan popup; đã mở xem. Trang `xem_ui.html` và `Overview.png`.
- Hai lỗi phát hiện trong lượt kiểm đầu đã sửa: callback dùng null-conditional với Unity fake null; test để lớp chuyển scene persistent lọt sang bài sau và chờ theo số frame. Không lấy kết quả lượt đầu làm kết quả cuối.

APK cuối build thành công 0 lỗi: `Builds/Android/Arena3Q-dev.apk`, **172,6 MB** (180.975.001 byte), tăng **19,1 MB** so với bản trước 153,5 MB. Đây là dung lượng APK đo trực tiếp, không phải BuildReport.totalSize chưa nén. ARM64, package `com.Arena3Q.Arena3Q`, chữ ký v2 đạt. SHA256 `062E536931650AEB5ADFBD74371AFE26A3C2B6456BEEB0013926B15EC07D131C`; log/metadata `Logs/UI-polish-build-android.log`, `Logs/UI-polish-apk.json`, `Logs/UI-polish-apk-signature.txt`.

Đã cài cập nhật `-r` giữ dữ liệu và mở trên Redmi Note 8 Pro/Android 11. Ảnh và log trong `device/`. Icon adaptive trích trực tiếp APK xác nhận có logo và alpha trong suốt. Không có NullReference/MissingComponent hoặc crash game trong lượt ghi log; có log Java thiếu lớp Google Play AssetPackManager (APK không dùng asset pack), cảnh báo AAC sample âm và telemetry Unity không kết nối được. Không dùng các log này làm bằng chứng FPS hoặc chất lượng âm thanh nghe qua loa. Cảnh báo WMF frame đầu 2 vốn có trong nguồn vẫn còn; màu BT.709 đã được bổ sung ở bản dẫn xuất.

Mã UI/âm thanh: commit `4579737` đã push. Bước icon + nhịp chuyển trang cuối và tài liệu có commit bàn giao tiếp theo. MP4/OGG gốc, MP4 loud, WAV dẫn xuất và APK để local theo quy tắc repo; Git chỉ giữ code, icon nhỏ và meta/script tái tạo.

Lượt cuối APK đã mở thành công, sau đó tiến trình 19967 được MIUI đóng lúc 22:13:50 với lý do `OneKeyClean` (importance 400, reason 13), không phải lỗi C# hoặc crash được ghi nhận. Log sau đóng đọc theo PID đã lưu trong `device/polish-final-logcat.txt`; lý do hệ thống lưu ở `device/exit-info.txt`. Không tự mở lại khi người dùng đã đóng ứng dụng.
