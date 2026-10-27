# Sảnh tích hợp, bản gửi review ngày 27/10

Đã hoàn tất bản sửa code và tự kiểm. Chờ review hình trước khi build APK mới. Trang ảnh đã mở tại http://127.0.0.1:4331/review.html; ảnh model riêng tại http://127.0.0.1:4331/model-review.html.

## Thay đổi

- Sảnh nằm trong Frontend: người cũ chọn máy chủ rồi vào Sảnh; người mới qua trận hướng dẫn; Xuất Chiến vào trận và kết quả trở về đúng nhân vật/máy chủ, không đăng nhập lại.
- Khung VIP bao cụm avatar, tên, cấp, EXP và Chiến Lực. Avatar tròn lấy từ chính model chủ tướng. EXP vuông, rãnh tối, gradient cam sang vàng; lấy EXP và cấp từ hồ sơ/ProgressionConfig.
- Tài nguyên chỉ hiện icon, số, nút cộng; số lớn làm tròn xuống theo vạn. Hover/chạm hiện tên và số đầy đủ; hết thời gian hoặc nhấn ngoài thì đóng. Thư/Cài đặt dùng Heroicons Solid, gradient/viền/bóng; bỏ Bạn bè trùng Hảo Hữu. StatusBar dùng chung.
- Menu, chấm đỏ, bảng Chủ Tướng, banner có dòng phụ/mũi tên, thanh điều hướng chọn Sảnh và nút Xuất Chiến đã sửa theo mẫu. Banner Chiêu Mộ dùng art sáng mới. Năm thời điểm tự chọn theo giờ máy; không có nút đổi giờ/chạy/tấn công trên màn người chơi.
- Bốn tỉ lệ 1920/2160/2340/2400 được chụp với safe area. Cụm trái/phải bám cạnh; nền căn đáy, vị trí chân theo bệ, cỡ model theo chiều cao vùng an toàn.

## Một nguồn dữ liệu

Model/Idle ở HeroDefinition; LobbyConfig chỉ giữ thông số camera/góc/ánh sáng riêng. Bố cục ở LobbyLayout.asset, màu ở token lobby của StartupUi.asset. Ví, VIP, Chiến Lực, EXP, sao đọc CharacterRecord qua ILobbyProfileSource. Số mẫu chỉ trong test; hồ sơ cũ thiếu ví đọc 0 và không bị cấp tiền demo.

Code sảnh cũ chỉ biên dịch khi UNITY_EDITOR; công cụ legacy dùng atlas riêng và builder APK riêng đã nghỉ. Release kiểm dependency/BuildReport để chặn scene/catalog/rig/atlas legacy. Chưa có BuildReport mới để kết luận nội dung nhị phân APK.

## Kiểm đã chạy

- Full EditMode: 301/301, không lỗi, Logs/Lobby-full-edit.xml.
- Full PlayMode: 176 qua, không lỗi, 1 ignored có sẵn, tổng 177 bài, Logs/Lobby-full-play-final.xml. Bao gồm sảnh mới và smoke test công cụ cũ.
- Tooltip riêng: 1/1, Logs/Lobby-hint.xml. Có kiểm hover, chạm, hết hạn, nhấn ngoài, số chính xác; EXP 0/50/100% và cấp tối đa; VIP 1/12 và safe area; JSON giữ số long lớn.
- Idle: 61 mẫu, 46.213.356 lượt cạnh, 0 gai/giãn/ép. Editor 60,00 FPS trung bình, p95 16,76 ms. Chưa đo thiết bị thật.
- Browser trang sảnh: 61 ảnh sảnh,12ảnh EXP,9ảnh bố cục và12điều khiển đều qua, không lỗi tải/pageerror, integrated-browser-check.json. Trang model: 5 ảnh cận đều tải đủ.
- Metadata: 101 GUID giữ nguyên; 100 ảnh nguồn giữ SHA256. Avatar 512 được chụp lại có chủ ý; atlas Android/iOS ASTC 4x4, mip UI tắt. Xem README thư mục LobbyCompression_20261027.

## Bằng chứng

- Bộ ảnh cuối: review_2026-10-27T13-56-30-329Z/review.html. Có mẫu so bản sửa, trước/sau cùng Idle frame 0 và nền trưa, bốn tỉ lệ, ba pha Idle, năm thời điểm, ảnh gợi ý và EXP/VIP.
- Ảnh runtime gốc: captures_20261027_205524 và ui_refinements_20261027_205552.
- Model: model-details_2026-10-27T11-12-09-333Z/review.html; cùng camera/pha Idle, không căn chỉnh hình. Gán cubemap riêng qua property block sửa fallback phản xạ xanh: vùng kiểm 1.305 pixel xanh xuống 0; alpha giữ nguyên 8.037 pixel biên trung gian. MSAA4 và composite premultiplied xử lý mép; không sửa rig, mesh, shader, ORM, normal hoặc texture tướng.
- Probe phân biệt diffuse/specular: surface-probe_20261027_180854; sau sửa: surface-probe_20261027_184215. Camera QA Editor nằm trong preview scene riêng, tránh nhận đèn/bóng của Battle đang mở.

## Phần chưa nghiệm thu

Chờ Claude review A1-A10 và hình model. Ba chủ tướng chưa có model hiển thị thông báo cập nhật; không tạo nguồn tướng mới. LOD Battle có vùng tối/vết loang riêng đã chụp đối chiếu, nằm ngoài sửa ánh sáng sảnh.

Chưa build APK vòng này: so chất lượng Android thực, packed bytes từng nhóm và chênh dung lượng Before/After sẽ làm sau review. Các menu chưa có gameplay vẫn mở thông báo Chưa mở. Chưa có cổng FPS thiết bị.

Ảnh bằng chứng, FBX/blend, texture/font lớn giữ local theo quy tắc repo và ArtSource/Lobby2D/local-only-assets.json. Model có sẵn dưới Art/Heroes/MacAnh/Graphics3D vẫn cần nguồn FBX/texture local; code, cấu hình, avatar 512 và asset nhỏ mới được đưa vào Git. Không dùng LFS.

## Chỉ đạo bố cục mới nhất và nhạc khi test

Người dùng đã xem đề xuất rồi gửi ảnh `codex-clipboard-4QzmLs.png` yêu cầu áp dụng. Bản runtime mới có avatar lớn trái, tên giữa thân VIP, Lv/EXP/VIP trên hàng riêng và Chiến Lực phía dưới. Tên được kiểm theo thân ảnh cả12bậcVIP; EXP có viewport clip và kiểm0/50/100%/cấp tối đa. Nhãn cạnh model chỉ còn một dòng Chủ Tướng nhỏ gọn, đặt bên trái quạt. Đây là quyết định mới thay bảng dọc tên/sao của yêu cầu trước.

Trang đối chiếu mới đã mở: http://127.0.0.1:4331/name-design.html, bản bất biến `name-layout_2026-10-27T13-56-28-683Z/review.html`. Có ảnh mẫu người dùng, ảnh runtime4tỉlệ, VIP1/12, ảnh trước/sau cùng khung và cận nhãn. Đề xuất chưa áp dụng trước đó được giữ riêng trong lịch sử, không còn là bản chờ duyệt hiện tại.

BGM được mute ngay khi tạo AudioSource trong Editor qua StartupUi.muteMusicInEditor. Kiểm riêng Music1/1, Logs/Lobby-quiet-music.xml; trang ảnh không chứa audio/video. Tùy chọn âm thanh lưu của người chơi được giữ nguyên.
Lượt cuối: Full Edit301/301 (13:43:41Z), Full Play176qua/0lỗi/1ignored (13:47:01Z). Sau chỉnh khoảng cách mũi tên/nền StatusBar, kiểm lại Lobby + StartupUi32/32 (13:55:13Z), Logs/Lobby-final-ui.xml. Browser61ảnh sảnh +12EXP +9bố cục +12điều khiển qua,0pageerror/HTTPerror/audio/video. Ảnh1920và2400đã xem: nhãn không che quạt, mũi tên có khoảng cách, giờ/Wi-Fi rõ trên mây.
