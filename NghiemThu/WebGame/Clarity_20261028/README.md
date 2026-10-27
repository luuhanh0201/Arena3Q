# Độ nét toàn luồng và hoàn thiện sảnh, 28/10

Đang thực hiện theo bàn giao2355/0005/0015 và yêu cầu người dùng theo dõi, tiếp tục. Đây là điểm nguồn ổn định để chuyển Unity cho lượt build APK riêng; chưa phải kết quả nghiệm thu web cuối.

- Web chọn tỉ lệ theo mode, render theo kích thước CSS thực × DPR màn hình, có giới hạn DPR2/cạnh3840/tổng8.294.400pixel trong JSON. Cập nhật khi resize/đổi DPR, không ép về1600/1280/1024 như trước.
- `web-import.json`: 103ảnh được áp dụng WebGL; UI/atlasRGBA32, nềnDXT chất lượng100. Kiểm nguyên trạng override Android/iOS, không chạm icon kỹ năng. Nền startup dùng nguồn1672×941; giữ GUID, tham chiếu và bố cục. Nguồn PNG3.088.958byte có SHA256`2bf9a45dd4ab36a9dc568f1a8d040f2d6b1fc1c74a7e1db8ec6449ecf9bd46c2`; asset lớn giữ local, builder tái tạo từ gói handoff local đã có, không đưa PNG lớn vào commit.
- Model có modelTint riêng theo giờ, da/vải trắng đêm giữ ấm; RT theo kích thước quad trên màn hình, tối thiểu1024/tối đa2048, bước128. Không sửa rig, mesh, clip, shader hoặc texture tướng.
- Tooltip co theo chữ và padding; mũi tên kép đưa vào tấm đỏ, tăng bề rộng; StatusBar nền0,85.

## Kiểm đã chạy tại điểm bàn giao

Unity: compile đạt. Lượt UI đầu31/33qua; hai bài chưa qua là fixture giới hạnRT (quad4×chưa lớn đủ trong GameView) và deadline tải legacy20giây. Sửa fixture, giữ assertion thực và chạy lại2/2qua: `Logs/Clarity-ui.xml`, `Logs/Clarity-ui-recheck.xml`. Các bài runtime cũ còn lại qua trong lượt33bài. Chưa lấy kết quả này thay cho nghiệm thu APK hoặc FPS thiết bị.

Baseline đúng DPR: `before_2026-10-27T17-18-06-488Z/result.json`,69ảnh/63trường hợp,0pageerror/consoleerror. Context riêng cho mỗi DPR1/1,25/2; trước đó các lượt dùng CDP bị Playwright khôi phục DPR1 và đã bị loại khỏi bằng chứng đạt. Kiểm nhanh wrapper sau sửa: `wrapper_2026-10-27T17-17-54-368Z/result.json`,9trường hợp đăng nhập qua; ở PC DPR2, CSS1564×879,75 cho buffer3128×1760. Chưa chạy toàn luồng với payloadUnity mới tại thời điểm bàn giao.

Ảnh Unity sau sửa: `captures_20261028_001626`, `ui_refinements_20261028_001724` dưới thư mục sảnh27/10. Trang trước/sau đã mở tại http://127.0.0.1:4331/clarity-review.html, bản bất biến `clarity_2026-10-27T17-25-09-190Z/review.html`. Có cận mặt đêm, tooltip, mũi tên, StatusBar và nền keyart nguồn cùng kích thước hiển thị.

## Đề xuất ảnh2× và phần còn lại

Đã dùng skillimagegen tạo đề xuất nút vàng phục hồi; bảnAI thay hoa văn nên chỉ lưu trong `ArtSource/UI/Clarity_20261028/Proposals`, không gắn game. Trang trước/sau ghi rõ trạng thái. Chưa thay bộ nút/khung/trang trí bằng ảnh phóng chưa được xem/chốt chất lượng. Các nguồn độ phân giải cao hơn do người dùng cung cấp sẽ được ưu tiên.

Payload web cũ được lưu riêng ở `Builds/WebGL/Arena3Q-before-clarity-20261028` để so kích thước/hình. Cần build web mới sau khi Claude nhả Unity từ lượt APK, chụp lại63trường hợp và so ảnh/dung lượng/nhịp trình duyệt. RequestAnimationFrame không thay FPS game/Android. Ảnh/log/build/nguồn lớn giữ local, chỉ README và công cụ vào Git.
