# Mặc Ảnh: sửa rời cổ tay, mảng lót áo và nhịp tay áo

Trang kiểm tra cục bộ: [xem_chuyen_dong.html](xem_chuyen_dong.html). Bản sửa 22–23/10/2026, Codex. Đây là bản xem trước để người dùng/reviewer kiểm hình; không tự cấp nghiệm thu nghệ thuật tổng thể.

## Lỗi và sửa

- Model chia bàn tay, tay áo rủ và thân thành các mảng riêng nhưng thiếu hình học che kín từ cánh tay tới cổ tay. Skeleton vẫn nối đúng: khoảng cách joint đo tại 120 Hz khoảng 0,0000002 m. Các điểm từng trùng nhau ở rest có thể tách tới 0,268 m. Không thể hàn mọi điểm gần nhau: ngón tay chạm vải và vạt áo chạm nhau phải được phép rời khi cử động.
- Thay 1.320 tam giác nắp lót thân cũ bằng thân áo trong ôm sát; thêm các đoạn tay áo trong có đầu tròn chồng kín ở khớp, tới cổ tay. Bản thử skin tuyến tính bị ép khớp đã loại. Thu cổ tay để không chạm cán quạt. Không thay mặt ngoài, UV ngoài, hoa văn hay xương gốc.
- Làm dịu chuyển động xương gốc tay áo bằng cửa sổ 2 frame và trễ 0,75 frame; giữ chính xác đầu/cuối clip. Skill01 tay phải giữ đường đi cũ vì làm trễ ở đây khiến ngọc quạt xuyên tay áo. Tay trái Skill01 giảm tốc độ xoay cực đại khoảng 1.277 → 559°/s. Các mốc ra đòn và chuyển động thân/bàn tay/quạt giữ nguyên.
- Tóc, dây và tà áo có chuyển động bake hiện hữu, đã đo toàn chuỗi và xem nhiều góc; không bổ sung hệ mô phỏng vật lý runtime. Vẫn còn đặc điểm lưới/texture nguồn faceted ở tóc và vải, không coi đó là đã retopology toàn nhân vật.

## Kiểm chứng

| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Bề mặt Battle sau Unity import, 11 clip, cả nửa frame | 985 mẫu, 0 spike/stretch/compression vượt ngưỡng; tối đa 3 influence, renderer Bone4 | `Logs/MacAnh-arm-bridge-qa.log`, `Production/LOD/ImportedSurface_Battle.json` |
| Quạt nguồn, 6 phần, cả nửa frame | 985 mẫu, 0 giao cắt ngoài lòng bàn tay/ngón | `Production/LOD/FanContactAudit_ClothRepairFinal.json` |
| PlayMode MacAnh | 28 pass, 0 fail, 1 skip (thiết bị) | `Logs/MacAnh-cloth-results.xml` |
| Chuyển/ngắt clip trong Battle | Test mới bake bề mặt ở các frame blend: không pop ngay frame bắt đầu, không NaN/collapse/explosion | `AllActionsAndInterruptionsKeepTheSkinnedBodyContinuousInTime` |
| Trang test | 6.972 ảnh đủ; 154 trạng thái đầu/cuối clip × góc; đổi khung, slider, chạy ¼×, dừng đạt; 0 JS error | `page-check.json`, `page-check.png` |

Trang có 11 clip, 7 góc (trước/sau/trái/phải/camera trận/cận hai tay), đủ frame tại 30 FPS. Cột trước lưu từ Motion4 cũ; cột sau xuất từ prefab Battle hiện hành. Các tấm `compare_*.jpg` ghép cùng frame/góc, hàng trước và sau, thêm hàng lưng và camera trận. Lượt đầu so material bằng tên bị sai đã xuất lại bằng đường dẫn material trước khi sửa thân/clip. Không tái tạo cột trước bằng model đã sửa.

Giới hạn: kiểm va xuyên quạt đầy đủ là trên nguồn Blender skin tuyến tính; Unity đã đo bề mặt và xem ảnh sau import, chưa chạy lại phép giao tam giác độc lập trên quạt Unity. Test chuyển clip là kiểm hình học trong trận, trang phát clip riêng. Chưa đo Android/FPS. LOD Portrait cũ chưa nhận hình học áo mới; bản sửa và trang này là LOD Battle đang dùng trong trận.

## Nguồn và tái tạo

- Nguồn cũ: `docs/art/concepts/MuuSi_main-hero/MacAnh/Production/LOD/MacAnh_Battle_Motion4_FanFollow.blend`.
- Nguồn sửa giữ lâu dài: `docs/art/concepts/MuuSi_main-hero/MacAnh/Production/LOD/MacAnh_Battle_ClothRepair.blend` (SHA256 `4bf0451188eb0993ed0b355b272dc6e43ed7bd8526d85c9336286a7f60d5560f`).
- Chạy Blender `repair_macanh_arm_bridges.py -- --output <bridge.blend>`, sau đó `relax_macanh_sleeve_motion.py -- --output <final.blend>` trên bản bridge. Export bằng `export_macanh_lods.py -- --lod Battle --out <staging>`; chỉ nhập FBX geometry/action và Export_Battle.json sau audit.
- Unity: `Arena3Q.Editor.MacAnhArmBridgeQa.BuildAndCapture`, `Arena3Q.Editor.MacAnhPoseCapture.RunFullReview`; lần sau chỉ xuất lại cột sau, giữ baseline.
- Trình duyệt kiểm qua CDP Edge cổng 9228 bằng `check-page.mjs`; ảnh ghép bằng `make-contact-sheets.py` (Python bundled Blender + PIL dưới Library/MacAnhTools/Python).
- FBX/.blend và ảnh lớn nằm trên máy, không commit theo quy định repo. Backup trước sửa: `Library/MacAnhArmBridgeBackup/`. Code, báo cáo nhỏ và material được lưu git.
