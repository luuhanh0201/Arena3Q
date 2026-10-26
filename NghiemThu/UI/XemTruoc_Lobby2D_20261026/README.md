# Bộ sảnh2D, xem web27/10/2026

Trang xem <http://127.0.0.1:4330>, khởi động bằng `node ArtSource/Lobby2D/serve.cjs` nếu cần. Đã tự kiểm96asset cơ sở,72frame riêng,24VFX riêng,26/26web check. Xem [bàn giao](../../../docs/UI/Lobby2D/Delivery.md) và manifest `ArtSource/Lobby2D/acceptance-report.json`.

Lobby_0..4 là capture từ prefab thật với5sky/tint/light; Lobby_safe_2160/2340/2400 có safe area giả lập. Web/ chứa ảnh browser ở tốc độ/frame đã kiểm và report.json. Atlas/PNG nguồn vẫn là file độc lập trong Assets/Arena3Q/Lobby2D. Ảnh trước–sau FullBody/Avatar/Silver/Rig_ExtraHand dùng baseline thật lưu trước sửa. BeforeMicro giữ sảnh trước bổ sung hiệu ứng.

Kết luận: bộ tài nguyên qua tự kiểm trong phạm vi web, không đo điểm thẩm mỹ thành phần trăm. Người dùng quyết định nghiệm thu cuối. Không build game/không đo FPS thiết bị. Gói đủ `Builds/Art/Arena3Q_Lobby2D.unitypackage`; ảnh và gói lớn chỉ ở máy theo quy tắc project.
