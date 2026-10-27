# Sảnh tích hợp Frontend, vòng sửa 27/10

Đang chờ review sảnh; chưa build APK mới.

## Luồng và dữ liệu

FrontendConfig có bước TutorialBattle rồi Lobby. Người cũ đã qua tutorial vào Lobby sau chọn máy chủ; Xuất Chiến mở trận. OnReturn khôi phục server từ lastServerId rồi mở Lobby, không lặp đăng nhập. Luật chọn bước ở Core; cấu hình release mobile/web tham chiếu cùng FrontendConfig và kiểm LobbyConfig trong BuildReport.

LobbyConfig giữ art, nhãn tài nguyên, menu/chấm đỏ, banner, mốc giờ và thông số camera/ánh sáng riêng của sảnh. Tên/cấp/sao/EXP/VIP/Chiến Lực/ví tài nguyên đọc CharacterRecord qua ILobbyProfileSource. Số dư dùng long; hồ sơ JSON cũ thiếu ví đọc là 0, không cấp số demo hoặc ghi đè hồ sơ. Số liệu mẫu chỉ được seed trong test. Không đổi chỉ số chiến đấu.

## Thay đổi một chỗ

- Đổi model/Idle: HeroDefinition.portraitModelPrefab/portraitIdle, có fallback HeroModel3D. LobbyConfig chỉ giữ góc và camera theo tướng; đổi nguồn model không cần sửa lại sảnh. Avatar được chụp lại khi Apply Integrated Lobby và dùng chung LordRoster.
- Đổi bố cục: asset Content/Frontend/LobbyLayout.asset (rect, cỡ chữ, khoảng cách, banner, tooltip). Những màn Frontend khác vẫn còn tọa độ trong code, ngoài phạm vi vòng này.
- Đổi màu: token lobby trong Content/Frontend/StartupUi.asset, cùng nơi với gold/text/ink của UI tổng.
- Đổi số dư/VIP/Chiến Lực/EXP: hồ sơ người chơi hoặc thay ILobbyProfileSource. Đổi tên/icon/thứ tự tài nguyên: LobbyConfig.resources.
- Định dạng vạn: LobbyResourceRules trong Core, ngưỡng/chữ/độ chính xác từ LobbyConfig; làm tròn xuống. Tooltip chạm/hover hiện số đầy đủ, tự ẩn hoặc đóng khi chạm ngoài.

Đường player duy nhất là Frontend/Screens/LobbyScreen. Lobby2D/Runtime cũ chỉ biên dịch trong Editor để giữ công cụ art; builder legacy dùng atlas riêng, không chỉnh atlas LobbyUI của sảnh. Release kiểm dependency trước build và packed assets trong BuildReport để chặn rig/scene/catalog legacy. Xem Assets/Arena3Q/Lobby2D/README.txt.

Atlas LobbyUI có override Android/iOS ASTC 4×4, max 2048. Ảnh nguồn UI/nền giữ nguyên; UI/Resources đổi thành UI/ResourceBar và giữ GUID. Số dung lượng thực/so chất lượng Android sẽ đo sau review, trong lượt build được phép.

## Trình bày

GameButton, StatusBar, UIAudio, SafeAreaFitter, SceneTransition và FrontendMusic dùng chung. Màn sảnh dùng canvas1280×720 Match0,5; các cụm UI neo safe area trái/phải/dưới, phần giữa theo tâm. Không có nút đổi thời điểm hoặc nút chạy/tấn công/FPS trên UI sảnh. Bầu trời và ánh sáng chuyển mượt theo giờ máy, mốc giờ ở data; test có giờ giả lập.

Chủ tướng lấy từ hồ sơ/LordRoster. Mưu sĩ dùng LOD Portrait sẵn có, đúng vật liệu và Idle; avatar chụp từ cùng model gán vào LordRoster.Entry.portrait. Ba chủ tướng chưa có model có thông báo cập nhật hình. Không ghi cứng chọn Mưu sĩ ở runtime.

Claude đã xác nhận lỗi chặn mặt tối/vết loang qua 9 ảnh trước–sau. Hoàn thiện MSAA4, composite alpha premultiplied và môi trường phản xạ riêng. Chẩn đoán GI tách diffuse và specular xác định vùng xanh ở gấu áo đến từ fallback sky cubemap của URP, dù reflection probe báo đúng môi trường ấm. Gán cubemap RGBAHalf linear của sảnh trực tiếp qua property block giữ nguyên shader/material/ORM/normal; vùng kiểm xanh giảm từ 1.305 pixel xuống 0, hình alpha giữ nguyên 8.037 pixel biên trung gian. Ảnh cùng camera/pha Idle nằm trong bộ model-details mới, chưa thay cho nghiệm thu hình cuối.

Ảnh QA Editor dùng preview scene riêng để không nhận thêm đèn hoặc bóng của scene Battle đang mở. Vị trí chân theo bệ của nền, nền căn đáy để không bị menu che chân trên màn rộng; cỡ model vẫn theo chiều cao safe area. LOD Battle có vùng tối dưới cằm/vết loang trong ảnh trận thật; đây là việc riêng của nguồn Battle, ghi lại để quyết phạm vi sau.

## Art banner Chiêu Mộ

- Công cụ: built-in image_gen, theo skill imagegen. Không dùng CLI/API fallback.
- Edit target: `Assets/Arena3Q/Lobby2D/UI/Banners/Recruit.png`, giữ nguyên bản này.
- Bản dùng cho sảnh: `Assets/Arena3Q/Art/UI/LobbyRecruitDay.png`,960×375,987.254byte.
- Nguồn đầy đủ local: `ArtSource/Lobby2D/Generated/Recruit_Day_20261027.png`.
- Import tái lập: `node ArtSource/Lobby2D/install-recruit-day.cjs <đường-dẫn-ảnh-sinh>`.
- Prompt cuối:

> Use case: lighting-weather. Edit target: the attached wide recruitment banner for Arena3Q. Change only the night-time background and illumination to a bright blue-and-gold daytime fantasy Three Kingdoms scene matching a high quality chibi mobile game lobby. Preserve the existing male strategist on the RIGHT, his face, pose, clothing, scroll, details and identity exactly. Preserve the 2.56:1 wide banner composition. Replace the dark moonlit sky and fire-dark background with clear rich blue sky, luminous pale blue clouds and warmly sunlit golden palace architecture. Keep the LEFT HALF spacious with lower detail for overlay UI text, with blue and subtle gold tones, not a dark night scene. Keep the gold glowing scroll on the right. No text, no letters, no typography, no watermark, no border. This is a single usable game-art bitmap banner, not a UI mockup.

## Bằng chứng và giới hạn

`NghiemThu/UI/XemTruoc_Lobby2D_SuaTheoMau_20261027/README.md` ghi kết quả kiểm thực và đường dẫn ảnh. Hình nguồn lớn, FBX, atlas, ảnh bằng chứng giữ local theo quy tắc repo; chỉ code/cấu hình/asset nhỏ vào Git. Không dùng FPS Editor hoặc target60 để tuyên bố đạt trên thiết bị.

## Sửa bổ sung EXP theo ảnh người dùng

Theo ảnh bố cục người dùng chọn sau khi xem đề xuất, EXP chuyển xuống hàng chỉ số riêng rộng147đơn vị design, cùng Lv và VIP. Rãnh/fill nằm chung viewport có RectMask2D; bố cục chỉ còn một ExperienceBorder cộng inset, không giữ tọa độ ba phần riêng. 0/50/100% và cấp tối đa được chụp ở cả bốn tỉ lệ; VIP1/12 được kiểm khi đầy thanh. Không đổi tiến trình EXP hoặc hồ sơ.

## Bố cục người chơi đã được người dùng chọn

Ảnh chỉ đạo `codex-clipboard-4QzmLs.png` thay đề xuất trước đó: avatar lớn bên trái; tên căn giữa trong lòng VIP; hàng nền tối chứa Lv, EXP và thẻ VIP bên phải; Chiến Lực trên dải đỏ riêng ở dưới. Chỉnh độ neo thân ảnh theo từng bậc VIP, kiểm vùng glyph thực trên đủ12khung (kể cả VIP2 nền bạc). Tên có vùng cắt và tự giảm cỡ khi dài. Menu trái hạ xuống dưới cụm thông tin.

Nhãn cạnh nhân vật chỉ hiện một dòng “Chủ Tướng”, không lặp tên hoặc sao; neo theo model và dời sang trái để không che quạt. Dữ liệu sao trong hồ sơ không thay đổi. Trang `name-design.html` nay trỏ tới ảnh runtime đã áp dụng cùng mẫu người dùng, còn đề xuất cũ được giữ trong thư mục lịch sử riêng.

Khi chạy Editor hoặc test, `StartupUi.muteMusicInEditor` bật mặc định và `FrontendMusic` mute nguồn ngay lúc tạo; không ghi đè tùy chọn âm thanh đã lưu của người chơi. Music test riêng1/1 đạt.
Hai chỉnh đọc chữ cuối theo review19:20/20:45: Caption và DoubleChevron của Xuất Chiến có vùng riêng cách nhau11đơn vị; nền StatusBar dùng token0,75 để đọc rõ trên mây sáng trong render linear, viền trắng0,12. Không chỉ dựa vào alpha0,5 trong code cũ để đánh giá tương phản ảnh.
