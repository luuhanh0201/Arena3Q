# Xem trước bản sửa P2 theo review 30/10 17:55

Sửa sáu mục chặn trong review UI Chương 1. Đây là kết quả tự kiểm để Claude review lại, chưa phải người dùng nghiệm thu cuối.

**Cập nhật23:31:** Claude review độc lập commit8ef4ca7, xác nhận **ĐẠT khoảng92%**, cả sáu mục chặn đã sửa đúng. Claude chạy PlayMode18/18, EditMode5/5 và kiểm ảnh web. Còn góp ý nhỏ về câu chờ thưởng lặp chữ, viền lời dẫn làm mảnh nét trong Editor, tên boss hơi tối và các lệch nhỏ ghi cuối báo cáo; không còn chặn P2. Nguồn phản hồi: `AgentComms/codex/inbox/tu-claude_20261030-2331_review-P2-DAT.md`. Chưa thay nghiệm thu của người dùng/thiết bị thật.

| Mục | Kết quả sửa |
| --- | --- |
| Biển tế đàn | Thêm băng đỏ viền vàng, gán bằng `ChapterMapStage.useAltarBanner` và `MapUiConfig.altarBanner`; runtime không kiểm số ải. |
| Sao rương | Giới hạn tử số theo mốc 7/14/21; kiểm tiến độ 0, 7 và 21 sao. |
| Chữ trên map | Lời dẫn và tên boss có outline/underlay tối từ config. Hai material preset tham chiếu trong data giữ shader variant khi build WebGL, không sửa font dùng chung. |
| Phần thưởng | Bảng ải có nhãn và nội dung `rewardPreview` theo từng ải. Dữ liệu thưởng chưa biên soạn: hiện “Phần thưởng sẽ được công bố sau.” |
| Bảng hiện đủ | Panel có độ đặc 0,98; công cụ web chờ `modalReady`, alpha ≥ 0,999 và nút tương tác được trước capture. |
| Nút Đóng | Dùng GameButton xanh, tăng chiều cao theo dpi/CanvasScaler để đạt tối thiểu 48 dp; bảng tự tăng chiều cao tương ứng. |

Ảnh Unity trước: `../P1_Runtime_20261028/run_20261030_180450_134`. Sau: `../P1_Runtime_20261028/run_20261030_180608_418`. Mỗi bên 13 ảnh cùng camera/khung 1672 × 941: lời dẫn trên mây/cây/đất ở 30/60/120, tế đàn ở ba preset và bảng ải sau fade. Ảnh mẫu chú thích đặt cạnh bản chạy cùng kích thước để đối chiếu UI; không coi hình học của ảnh vẽ mẫu là cảnh 3D đã triển khai.

Unity PlayMode Chương 1: **18/18 qua**, `Logs/P2r2-play-final.xml`. EditMode ChapterProgress + DisplayMetrics: **7/7 qua**, `Logs/P2r2-edit-final.xml`. Kiểm modal ở 16:9, 18:9, 20:9 và Redmi 2340 × 1080; thưởng không tràn chữ, nút Đóng có sprite và đủ vùng chạm. Ảnh chụp có tắt SRP batching theo fixture capture hiện có; bản WebGL dùng cấu hình runtime bình thường.

Bản build web đầu bị ngắt khi phiên dừng, không dùng làm kết quả. Build tiếp thành công trong 371,2 giây, 0 lỗi/6 cảnh báo (`Logs/P2r2-web-build-resume.log`). Player thực 425.399.833 byte (405,7 MiB); số 1.066,4 MiB của builder gồm cả các gallery có sẵn trong thư mục xuất, không phải dung lượng player tải vào game. Hash data `d4fbe7e25ff4d83ad4f5f91bd6df2bbb3f6c2660a6a712a54fd49f05fc959620`.

WebGL **12/12 tổ hợp + 4 hồ sơ tiến độ qua, 27 ảnh, 0 lỗi trình duyệt**, tại `../P3_Web_2026-10-30T16-17-54-112Z/result.json`. Chạy `node WebGame/chapter-qa.cjs --world --ui-review`: chạm nhận nhiệm vụ, mở/đóng bảng, Xuất Chiến khi chưa gán battle không mở sai ải, trở về sảnh, mở hết và hướng camera tới tế đàn rồi chạm huy chương để mở bảng. Trình duyệt tắt tiếng, dùng hồ sơ QA riêng. Hai ảnh cuối `12-altar-unlocked.png`, `13-altar-stage-panel.png` đã xem trực tiếp cùng ảnh Redmi và bảng ải 1.

Gallery **9/9 lựa chọn nền/preset qua, không ảnh hỏng/không lỗi trang**, `gallery-check.json`. Chạy `node WebGame/chapter-p2-review-fixes.cjs NghiemThu/Chapter01/P3_Web_2026-10-30T16-17-54-112Z`. Đã xem ảnh tổng `text-presets-before-after.png` và ảnh cặp tế đàn/bảng ải. Unity và WebGL có khác biệt raster chữ, ảnh web thực được giữ riêng để review không dựa chỉ vào Editor.

Chưa đổi các lệch nhỏ: tiêu đề chưa có gradient về phải, mây ngoài biên sau khi mở hết, nhân vật đại diện tạm và vị trí kiếm sát biển Tiền Doanh. Nút quay lại giữ GameButton đỏ theo yêu cầu review, chờ quyết định riêng. Không thay scene/model map. Chưa đo FPS/p95/nhiệt hoặc nén ASTC trên thiết bị; web không thay nghiệm thu máy thật.

Nguồn AI `ArtSource/Chapter01/2D/UI/Generated/altar-banner.png` và ảnh/log lớn chỉ giữ local. Bản game 512 × 128, 89.764 byte cùng `.meta`, material, code và công cụ tái tạo được commit. Gallery: `http://127.0.0.1:4322/chapter-p2-review/`.
