# Chỉnh map theo phản hồi trên điện thoại, 25/10/2026

Đã sửa 3/3 nhóm yêu cầu và kiểm 15/15 PlayMode (8 map, 7 hồi quy scene cũ). APK bản sửa đang được build riêng; chưa chạy bản sửa trên điện thoại khi USB chưa kết nối.

- Bỏ toàn bộ mesh nền kéo dài ngoài biên: bốn tile đất giới hạn chính xác X ±40, Z ±22. Không còn dải đồi ngoài map. Giữ mặt sân giao tranh phẳng và khối đá/cây/công trình 3D. Camera fit theo kích thước map, khoảng trống ngoài map là màu nền camera.
- Trại trái của người chơi là RedCamp, phải là BlueCamp. Thanh HP, vùng triệu hồi, vòng chiếm tế đàn, chỉ dấu tấn công và màu dự phòng của vùng kỹ năng dùng màu phe từ BattleViewConfig. Cả Công/Thủ đều giữ phe ta đỏ; màu riêng của từng tướng/chiêu vẫn theo asset của họ.
- Tướng tăng x2 từ mức đang dùng (HeroModelScale 1,3 → 2,6 trong scene mới). Nhà chính, lều, tháp và hàng rào tăng x2. Bố trí lại lều/hàng rào để chừa lối vào. Chỉ đổi hình ảnh tướng, không đổi radius/chỉ số chiến đấu.
- Command_1 là nguyên vẹn, Command_2 là bị phá. Bỏ nhà vỡ tĩnh: Editor có preview nguyên vẹn, tắt khi Play; runtime dùng CastleUnit và CastleFallView có sẵn để rung, sập rồi giữ tàn tích. Màu và tỉ lệ giữ qua cả hai trạng thái. Không sửa mesh/model gốc.
- Cây/đá nằm trong biên, tránh mái trại. Footprint thân cây được đưa vào navigation hiện có; cả hai đường vòng và mọi spawn vẫn hợp lệ.

Ảnh `before-after.png`: trái trước, phải sau, cùng camera orthographic size28, góc và quy trình test di chuyển5tướng. `castle-alive-ruined.png`: trái còn sống, phải nhà địch đã phá. Các file `feedback-player-*` kiểm nhà ta và `feedback-enemy-*` kiểm nhà địch. Đã mở ảnh thật để kiểm tra. Một số tướng/lính trong loadout vẫn dùng hình prototype sẵn có.

`final-play-results.xml`: 15 qua, 0 lỗi. Bao gồm phá thật cả hai nhà, mesh nguyên/vỡ, màu HP/material sau sập, scale x2, thân cây chặn đường, hai tuyến đi được, spawn, Công/Thủ, gió/VFX, LOD/quality, camera/picking và hồi quy sập nhà của scene cũ. Test không chứng minh FPS trên thiết bị.

Cấu hình chỉnh ở `Content/Battlefield/BattlefieldConfig.asset` (`heroScaleMultiplier`, `buildingScaleMultiplier`); dùng Generate/Rebuild để áp dụng vào scene. Màu phe ở `BattleViewConfig`; map mới lấy từ cặp màu đỏ/xanh của BattlefieldConfig.

Ảnh, APK, XML/log giữ local theo quy tắc kích thước repo. Xem `docs/BanDo/BattlefieldFeedback_20261025.md` để biết APK và kết quả bàn giao cuối.
