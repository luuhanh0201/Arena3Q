# UI khởi động, sửa review vòng 2 ngày 24/10/2026

Trạng thái: bản sửa gửi Claude kiểm lại, chưa tự coi là nghiệm thu.

Nguồn trước sửa: `bf2eb28`, ảnh Claude chụp lại 18:52. `before/` giữ riêng, không sửa vòng 1.

| Mục Claude yêu cầu | Bản sửa / bằng chứng |
|---|---|
| Vòng chờ Stalled/Retrying không hiện | Alpha sprite outer128px, inner88px từ hình CSS mockup, quay ngược nhau1s/3,2s; navy disc và kim cương vuông quay45° có nhịp thở. `02_loading_Stalled.png`, `qa_spinner_isolated.png`; test đếm pixel vàng riêng từng vòng trên nền navy. |
| Dải tối bị cắt trên màn rộng | `CanvasBackdrop.bottomHeight` neo dải xuống đáy canvas thật, phủ toàn ngang. Ảnh `10_start_ultrawide.png`, `11_start_cutout.png`; test bounds của dải nằm ngoài khung safe16:9 và chạm hai mép canvas. |
| Câu điều khoản Login | Một câu TMP, hai link vàng inline, click bằng `UiLegalLinks`; không còn dòng link riêng. `03_login.png`. |
| Lặp điều khoản Register, panel cao | Một câu trong checkbox với hai link inline; click chữ thường đổi checkbox, click link chỉ mở link. Panel414px, gap10. `04_register.png`; test links/toggle/panel. |
| Chân chọn server | Bốn chấm màu/chữ, Đã chọn nằm cạnh XÁC NHẬN cùng hàng. `06_servers.png`. |
| Nút popup xỉn | Hủy ColorTween Disabled đã khởi động khi Button sinh dưới CanvasGroup modal. Reset CanvasRenderer tint white, giữ Image/transition riêng. Test kiểm `canvasRenderer.GetColor`, không chỉ Image.color/interactable. Nút gold và cả bốn nút link giờ sáng như mockup. `06_servers_widgets.txt`, `07_bind_widgets.txt`, ảnh trước/sau Bind. |
| Chip khách | Avatar40px trong vòng tròn viền vàng2px/nền13284A, tagcamF0A63A chữ tối. `05_start.png`. |
| Thứ tự loa/cài đặt và status | Loa trước bánh răng; chữ trạng thái13px/700, dùng màu sáng tương ứng trạng thái. `05_start.png`. |
| Mockup Loading trắng | Adapter render đúng component Loading với scenario weak/stall/lost/timeout của wrapper dc-import, dừng animation ở khung có hình. Đã mở và kiểm ảnh đối chiếu có cả hai phía. |

Trang `xem_ui.html` có20 ảnh Unity (gồm spinner cô lập),17 ảnh ghép trước/sau và mockup/Unity, cùng Overview. Đã mở trang và các ảnh so sánh cho người dùng.

Tự kiểm riêng UI/Frontend sau sửa: **PlayMode19/19**. Full EditMode **271/271**. Full PlayMode **146 qua / 0 lỗi / 1 ignored có từ trước** (CaptureRealRecipesAtSixtyHz, tổng147). Log ở `Logs/UI-review2-*`. Font atlas sau test tự compact về khoảng6,5KB mỗi asset.

Âm nhạc và phạm vi dịch vụ giữ như bản đầu: OGG loop từ Loading, default12%, volume/mute/prefs trong Cài đặt. Chưa có API thật hoặc đo thiết bị. Chỉ build Android sau khi Claude xác nhận UI đạt theo yêu cầu người dùng đã giao cho Claude.
