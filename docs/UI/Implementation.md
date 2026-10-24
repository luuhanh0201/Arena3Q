# UI khởi động Arena3Q

Nguồn: `Arena3Q_UI_Handoff/README.md`, toàn bộ `reference/mockup/*.dc.html`, `ui.css`, `canvas.json`, `PROMPTS_AI_ART.md`. Khung 1280×720; art và âm thanh dùng nguyên gói đã giao. Link online không truy cập được qua công cụ web; mockup gốc trong gói là chuẩn đối chiếu.

## Kế hoạch triển khai (24/10/2026)

1. Chụp Frontend cũ, kiểm tra baseline trước sửa.
2. Import kit với border đúng README, font Be Vietnam Pro OFL, audio/video. `StartupUiConfig` giữ art, copy, tokens, bố cục và thời gian. Dùng chung `StartupUi`, `GameButton`, `UIAudio`, `SafeAreaFitter`, `StatusBar`, modal và form.
3. Tích hợp Intro → Loading → Login/Register → Start → server popup → luồng chọn chủ tướng/tên/truyện/trận đang có. Không tạo Boot/scene thứ hai hay bản sao dịch vụ hồ sơ.
4. Loading theo interface: dịch vụ offline chuẩn bị tài nguyên cục bộ; kịch bản mạng có thể tiêm để QA. Không bịa tốc độ tải thật. Tài khoản ngoài guest trả thông báo chưa có backend; không giả thành công hoặc làm mất hồ sơ khi liên kết.
5. Test tương tác thật, retry/cancel/loading, hồ sơ theo server, hồi quy Frontend; đối chiếu ảnh mockup và trước/sau ở 16:9, màn rộng và safe area. Bàn giao Claude tự kiểm lại.

## Quyết định phạm vi

- Giữ `Frontend` là scene khởi động. Bố cục cũ của chọn tướng/tên/truyện được bảo toàn trong không gian thiết kế 1920×1080 riêng.
- Handoff mới thay phần giao diện đăng nhập cũ; các luật gameplay đã chốt giữ nguyên.
- Mẹo ngũ hành không đúng spec hiện tại, thay bằng mẹo về kỹ năng/đội hình. Không thêm ngũ hành vào gameplay.
- Dữ liệu server mẫu giữ ID `s1..s3` để tương thích hồ sơ đã lưu, thêm mẫu còn lại qua asset. Không hiển thị Lv45 hoặc nhân vật chưa tồn tại.
- Quà liên kết, API, URL điều khoản và chính sách chưa được cung cấp. Để cấu hình và thông báo trung thực khi thao tác; không phát quà hoặc gọi mạng chưa biết địa chỉ.
- Kiểm trong Unity; chưa có đo 60 FPS trên thiết bị, không build Android trong công việc này.

## Bản triển khai và kiểm tra

- Frontend dùng Canvas 1280×720, Match 0,5. `SafeAreaFitter` và `DesignFrame` giữ nội dung trong vùng an toàn; `CanvasBackdrop` phủ nền/blur toàn canvas cả khi 21:9. Các màn chọn tướng/tên/truyện cũ giữ không gian 1920×1080 riêng.
- `StartupUi` dùng chung nút 9-slice, TMP, ô nhập, toggle, slider, panel, footer, logo, trạng thái server. `AccountForm` dùng cho đăng nhập, đăng ký và liên kết. Copy nằm trong `StartupUi.asset`, nguồn biên tập `StartupCopy.tsv`; art/tokens/thời gian/layout chính cũng trong asset.
- `FrontendServiceLocator` giữ các seam `IAccountService`, `IAccountActions`, `IServerDirectory`, `IStartupPreparation`, `IPlayerProfileStore`. Không có scene Boot thứ hai hoặc dịch vụ hồ sơ thứ hai.
- Loading cục bộ đọc tài nguyên/font, hiển thị đúng "Tài nguyên trên thiết bị"; không bịa số MB/s. `StartupNetworkMonitor` nhận telemetry thật từ adapter tương lai; luật weak/stall/timeout và retry 2/4/8 s có test. F8 đổi trạng thái mạng để review trong Editor/Development, F9 trở lại chuẩn bị cục bộ. Không có URL backend nên chưa thực hiện HEAD/HTTP Range/cache tải mạng thật.
- Server popup có 4 tab, lưới cuộn 2 cột, trạng thái bằng cả chữ/màu, chọn tạm/hủy/xác nhận, thông tin nhân vật lấy từ hồ sơ. Giữ ID và khuyến nghị cũ của 3 server đầu, thêm mẫu s4-s20 qua asset. `Full` và `Good` được thêm cuối enum để không đổi dữ liệu cũ.
- Email/SĐT, password, xác nhận, điều khoản, OTP được kiểm trước service. OTP chỉ đếm ngược khi service chấp nhận gửi, không giả gửi thành công ở offline. Liên kết yêu cầu giữ accountId, không tạo hoặc thay hồ sơ khách khi service thất bại. Nút nhớ đăng nhập lưu lựa chọn và địa chỉ, không lưu mật khẩu; token/secure storage cần adapter thật.
- Theo yêu cầu bổ sung của người dùng: OGG streaming loop bắt đầu ở Loading, liên tục trong Frontend/sảnh, mặc định 12%, có slider/tắt riêng trong Cài đặt. Nút loa góc tắt/bật cả nhạc và UI SFX. Preferences được lưu. Nhạc dừng khi rời Frontend sang Battle.
- `UIAudio` có 6 SFX, chống chồng cùng clip 80 ms. `GameButton` dùng một sprite mỗi màu, nhấn .94/.90, trả vị trí, glow hover/focus, disabled alpha .55. Màn fade .2 s, modal scale từ tâm + fade .25 s.

Kết quả tự kiểm cuối ngày 24/10: **EditMode 41/41**, **PlayMode 15/15**, không fail/skip trong hai nhóm này. Bao gồm hồi quy Frontend đi đến trận hướng dẫn, khách/đăng ký/liên kết, raycast thật, button press giữ sprite, server confirm/cancel/full, retry giữ 63%, nhạc loop/âm lượng/mute và lưu qua instance mới, safe area và nền toàn canvas.

Log: `Logs/UI-import.log`, `Logs/UI-editmode.xml`, `Logs/UI-playmode.xml`. 19 ảnh Unity, 10 ảnh so sánh; trang xem `NghiemThu/UI/XemTruoc_Startup_20261024/xem_ui.html`. Đã mở trang/ảnh trước-sau và mockup/Unity trên máy. Link online không đọc được qua web tool; ảnh chuẩn lấy từ HTML gốc cùng gói.

Font source Be Vietnam Pro OFL đủ tiếng Việt cả 4 weight; SDF dynamic tự compact sau PlayMode, mỗi asset khoảng 6,5 KB. Background 508 KB, logo 598 KB. Media lớn local: MP4 3.019.605 byte, OGG 4.146.483 byte; không commit cùng code theo quy tắc repo. README art có cách khôi phục và import lại trên máy khác.

Giới hạn: đây là UI prototype với dịch vụ guest/cục bộ. Chưa có API xác thực/OTP/social/link, API danh sách server, URL pháp lý, thưởng liên kết; không hiển thị cấp 45/nhân vật/quà chưa có trong hồ sơ. Source MP4 được WMF cảnh báo frame đầu 2 và thiếu color primaries trên Windows; có still fallback, không sửa video nguồn. Chưa đo âm thanh qua loa/thiết bị thật, chưa đo 60 FPS Android/iOS, chưa build APK. Claude cần tự chạy lại và review hình trước khi kết luận nghiệm thu.
