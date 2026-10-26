# Arena3Q - Bộ 2D sảnh và Lữ Bố

Nguồn yêu cầu: MASTER DESIGN & ANIMATION PROMPT của người dùng, 26/10/2026. Đây là hạng mục 2D mới dành cho sảnh; không thay chiến trường 3D của bản phát hành hiện tại.

## Tham chiếu đã tìm thấy

- `docs/UI/assets/Sảnh Game.png`: camera, thành trì, bệ đứng, bố cục và bảng màu.
- `docs/UI/assets/animation1.png`: identity Lữ Bố và Phương Thiên Họa Kích.
- `docs/UI/assets/farmes.png`: ý đồ chuyển động, chỉ là storyboard; không được cắt thành 72 frame giao hàng.
- Ba ảnh UUID trong cùng thư mục: style guide, UI frames và navigation.
- Model/atlas Lữ Bố cũ khác reference mới. Không dùng lại bằng cách phóng to ảnh 256 px rồi gọi là nguồn 1024 px.

## Kế hoạch trước khi tạo tài nguyên

1. Tạo bản Lữ Bố độc lập, kiểm alpha, số lông vũ, silhouette và mép canvas; giữ các bản chưa đạt trong ArtSource.
2. Tạo UI độc lập không chữ; architecture và sky riêng. Chỉ một geometry thành trì dùng chung năm thời điểm, ánh sáng/sky thay độc lập.
3. Khai báo toàn bộ đầu ra và các gate trong `ArtSource/Lobby2D/spec.json`. Công cụ kiểm không được coi danh sách đầu ra là bằng chứng file đã có.
4. Animation: tạo key pose nếu chưa có rig đáp ứng identity. Chỉ xuất 24 frame từ nguồn animation thực; không sao chép pose, tween cả ảnh, đổi hue hay phóng/thu để đủ số.
5. Nhập Sprite, pivot, 9-slice và atlas qua công cụ Editor riêng. Dữ liệu động dùng TMP. Không sửa scene/build settings phát hành để xem asset.
6. Kiểm bằng artifact thật. Ghi rõ từng mục còn thiếu; không tự ghi 'hoàn tất' khi rig, loop, chân hoặc hiệu năng chưa được kiểm.

## Hợp đồng bàn giao

- Palette: #C91E1E, #7A1319, #FFD166, #A66A28, #101018, #17243A.
- Character: 2.5-3 đầu, mắt đỏ, tóc đen dài, đúng hai lông vũ đỏ, giáp rồng đỏ/đen/vàng, cape nhiều lớp, cùng một cây kích.
- Character PNG: RGBA, không chữ/khung/nền. Năm bản: FullBody, Head, Avatar, Bust, BattlePose.
- Layers: Head, Hair_Front, Hair_Back, Helmet, Feather_Left, Feather_Right, Torso, UpperArm_L/R, Forearm_L/R, Hand_L/R, Thigh_L/R, Shin_L/R, Foot_L/R, ShoulderArmor_L/R, Cape_Upper, Cape_Left, Cape_Right, Cloth_Front, Weapon. Vẽ bổ sung chỗ khuất trước rig; không dùng mảng cắt có lỗ.
- Animation: Idle 2s/24, Run 0.9s/24, Attack_01 1.1s/24, Attack không loop. Các giá trị là mục tiêu hình ảnh, không đổi timing chiến đấu hiện tại.
- Tên frame: LuBu_Idle_000..023, LuBu_Run_000..023, LuBu_Attack_000..023. Canvas chung >=1024, pivot tại tâm đứng. Loop không chép frame đầu vào cuối.
- Mỗi clip: PNG rời2048, atlas xem6x4 dùng cell512 (3072x2048), GIF, timing CSV, pivot/frame-order JSON, events, VFX riêng. Atlas xem không nhập làm atlas mobile runtime; atlas UI runtime chia trang <=2048.
- Attack phases: 01-04 preparation, 05-08 anticipation, 09-13 acceleration, 14-16 impact, 17-20 follow-through, 21-24 recovery. Event đề xuất phải gắn clip đã kiểm, không gọi damage từ VFX.
- Lobby: 1920x1080, safe area cho 16:9/18:9/19.5:9/20:9. Sky, Far Background, Buildings, Foreground, Lighting, Ambient VFX độc lập; giữ camera và geometry qua năm thời điểm.
- Top: avatar, tên, level, chiến lực, VIP, ngân phiếu giấy, bạc, đùi gà, bạn bè, thư, cài đặt. Thư chỉ góc trên phải.
- Bottom: Sảnh, Tướng, Đội Hình, Trang Bị, Túi, Khác. Left: Nhiệm Vụ, Sự Kiện, Phúc Lợi, Hảo Hữu, Bang Hội. Right: Chiêu Mộ, sự kiện nổi bật, Đấu Trường, hoạt động mở khóa. Chiêu Mộ chỉ một nơi; không thêm Chinh Chiến.
- CTA Xuất Chiến: rồng vàng, nền đỏ, hai kiếm; chữ TMP riêng, normal/highlight/pressed/disabled cùng canvas/pivot.
- VIP 1-12 phải khác thiết kế/vật liệu/hoa văn; VIP 8-12 có light sweep riêng, có thể tắt. Không dùng chỉ đổi số trên cùng một badge.
- UI: icons, button base/highlight/pressed/disabled, panel base/border, frame 9-slice, notification, resource bar, player/avatar/card/banner/navigation frames, decorations riêng.
- Micro-animation: nhẹ, có cấu hình bật/tắt độc lập, không bật/tắt VFX đột ngột hoặc làm cả màn hình chớp.
- 60 FPS là mục tiêu; chỉ đo trên thiết bị thật mới được ghi đạt. Unity 6 URP, Canvas Scaler, GPU-friendly, hạn chế overdraw.

## Giới hạn kiểm chứng

Ảnh sinh bởi AI chưa tự trở thành rig. Alpha đúng hoặc có 24 hash khác nhau không chứng minh bước chạy, identity hay loop đúng. Các gate hình ảnh/chuyển động trong spec phải được xác nhận riêng bằng preview chạy thật; công cụ không tự đánh dấu chúng đạt.

## Kết quả27/10

Đã sản xuất nguồn AI riêng theo reference, dựng rig cố định26 nhóm/38 mảng/1840 đỉnh rồi render72 frame thật. Có96 tài nguyên cơ sở,24 VFX riêng, prefab/Animator/timing và gói import Unity. Xem [bàn giao](Delivery.md) để biết kết quả đo, cách dùng, danh sách file lớn chỉ ở máy và giới hạn kiểm chứng. Người dùng yêu cầu chỉ kiểm web, không build game trong phiên này; gate60FPS thiết bị được hoãn, không đổi thành đạt.
