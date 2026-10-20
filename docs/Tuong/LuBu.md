# Lữ Bố trong Arena3Q

Nguồn: `docs/art/concepts/LuBu_Arena3Q/`. Model và UV gốc được giữ riêng;
`LuBu_Arena3Q_Rigged_Validated.blend` là bản rig dự phòng. Bản chuyển động mới
được lưu riêng trong `LuBu_Arena3Q_Smooth.blend`.

## Dựng lại tài nguyên

1. Dùng Blender 4.5 LTS, chạy `BuildValidatedRig.py` trên bản blend nguồn.
2. Chạy `RepairLuBuSkin.py` trên bản kết quả để tạo `LuBu_HeatWeights.npz`.
   Script tính bone heat trên lưới voxel phụ kín, không sửa topology/UV của nhân vật.
3. Chạy lại `BuildValidatedRig.py` trên bản kết quả để áp dụng weight mới,
   giới hạn bốn ảnh hưởng mỗi đỉnh và dựng các action.
4. Chạy `FinishLuBuRig.py` nếu cần tiếp tục riêng bước Victory/Defeat. Chạy
   `RepairLuBuPoses.py` để sửa tư thế thu kích và giữ kích ngoài đầu khi chiến thắng,
   sau đó `AuditLuBuRig.py` để kiểm tra từng frame.
5. Chạy `SmoothLuBuMotion.py` trên bản `Rigged_Validated` để tạo bản `Smooth`.
   Chạy `AuditLuBuRig.py -- --smooth --dense` trên bản Smooth để kiểm tra cả
   khung nội suy ở 60 fps trước khi chụp.
   `AnalyzeLuBuMotion.py -- --after` ghi báo cáo góc xoay giữa các frame.
6. Chạy `ExportSmoothSprites.py` trên bản Smooth, rồi `BuildSmoothPreview.py` để
   kiểm tra atlas, pivot, UV và tạo ảnh xem nhanh. Atlas ghi vào
   `Assets/Arena3Q/Art/Heroes/LuBu/Smooth/`. Có thể chia xuất bằng `-- --only
   Idle,Walk,Run,Victory` và `-- --only Attack01,Attack02,Defeat`; chỉ nhập sau khi
   cả bảy clip cùng phiên bản nguồn đã hoàn tất.
7. Trong Unity: **Arena3Q → Lu Bu → Import Sprites and Add Hero**.
   Batch mode: `-executeMethod Arena3Q.Editor.LuBuContentBuilder.Build`.

Sửa da và chỗ cầm kích (2026-10-20, chuỗi hiện dùng để xuất):
`LuBu_Arena3Q_Smooth.blend` → `RebuildLuBuFeathers.py` (chuỗi xương lông trĩ dọc cả chùm, weight theo
chiều dài) → `FixLuBuSkin.py -- --rings 10 --iterations 60 --robe-iter 150 --cs-iter 30 --cs-all 1`
(`LuBu_Arena3Q_SkinFix3.blend`) → `ShiftLuBuGrip.py -- --shift 0.10` (`LuBu_Arena3Q_Grip.blend`) →
`ExportSmoothSprites.py`. Bản gốc trước sửa kèm SHA256 ở `Backups/BeforeClaudeSkinFix_20261020/`.
`DiagnoseLuBuSkin.py` in các cặp xương tại cạnh lỗi để tìm vùng cần sửa. Còn lỗi kéo da ở vai, đỉnh áo
choàng và tóc gáy (Attack/Defeat); chưa nghiệm thu.

- **Chỗ cầm kích**: tay phải nắm cao hơn 0,10 m trên cán (trước đây sát chỏm đuôi). Ở thế đâm của
  Attack02 cán trượt ra trước qua nắm tay (tối đa 0,10 m, khung 1–24, làm mượt) để đuôi kích không đâm
  vào khuỷu và áo choàng; độ trượt là keyframe vị trí `Socket_Hand_R` dọc cán, các clip khác có key 0.
  `ShiftLuBuGrip.py` đo điểm kích lọt vào thân (ngoài bàn tay đang nắm) ở mọi khung, chỉ trượt khi cần.
- **Trang atlas 2048** (mọi GPU Android/iOS nhận): `ExportSmoothSprites.py` đóng trang `PAGE = 2048`;
  `RepackLuBuPages.py` đóng lại atlas cũ 4096 sang 2048 mà không render lại (so pixel từng sprite).
  `LuBuContentBuilder` đặt `maxTextureSize` 2048. Bảy clip hiện dùng 18 trang.

Không đặt Blender portable trong `Temp/` khi chạy Unity: Unity có thể dọn tài nguyên
trong thư mục này. Dùng thư mục công cụ riêng hoặc `Library/LuBuTools/`.

## Rig và animation

- 73 xương, có ngón tay, áo choàng, lông trĩ và socket cầm kích.
- Bone heat trên lưới phụ được chuyển về model thật; vùng ngón tay, đầu,
  áo choàng và lông chỉ chịu ảnh hưởng từ các nhóm xương tương ứng.
- Armature bật Preserve Volume. Tối đa bốn weight mỗi đỉnh, tổng weight chuẩn hóa.
- Bảy action: Idle, Walk, Run, Attack01, Attack02, Victory, Defeat.
- Script dựng kiểm tra kích với thân/giáp ở cả keyframe và các frame trung gian.
  Vải/lông và bàn tay cầm cán được loại khỏi phép kiểm tra kích theo thiết kế nguồn.
  Đây không phải chứng nhận không có mọi kiểu tự xuyên lưới; cần xem lại hình và chuyển động
  sau mỗi lần sửa mesh, weight hoặc keyframe.
- Bản Smooth dùng tám hướng: đông, đông bắc, bắc, tây bắc, tây, tây nam, nam,
  đông nam; không lật gương vũ khí. Chụp 256×256 rồi cắt phần trong suốt và đóng
  atlas theo trang 2048 px; pivot chân và PPU được bù theo từng khung chụp.
- Idle 32, Walk 48, Run 32, Attack01/02 mỗi clip 55, Victory/Defeat mỗi clip
  121 frame mỗi hướng. Tổng 3.712 frame; bước chân/đòn đánh lấy mẫu 60 fps,
  Idle/Victory/Defeat 24 fps.
- Bản Smooth đặt lại ngón tay trước khi nắm, dùng nghiệm hai đoạn cho tay phải,
  giữ độ dài xương và hướng khuỷu ổn định; quaternion cùng bán cầu, nội suy
  Bezier. Phần cán sau tay được rút ngắn cho tỷ lệ tay chibi; phần lưỡi kích,
  topology và UV giữ nguyên. Bản model gốc không bị sửa.

## Trong game

`Hero_LuBu` là danh tướng phe Quần. Chỉ số đang là bản thử và chỉnh được trong asset.
Bộ kỹ năng tạm dùng chung cơ chế cận chiến của Đao Khách; chưa thiết kế bộ kỹ năng riêng.
Builder đưa Lữ Bố vào bộ thẻ thử của scene Battle và quà nhân vật mới ở Frontend.
Hồ sơ đã tồn tại không bị sửa hoặc xóa; có thể thử ngay từ scene Battle.

`HeroAnimationView` đọc vận tốc di chuyển chủ động để chọn hướng và trạng thái,
phát hai đòn đánh luân phiên. Dịch chuyển do chen lấn không tự bật Run hoặc hủy
đòn đánh. Hướng quay có giới hạn tốc độ và vùng giữ hướng để tránh rung ở ranh
giới. Nhịp bước theo quãng đường; Walk/Run giữ pha bước khi chuyển qua lại.
Khi làm chậm, tướng chuyển sang Walk. Dừng ngắn được giữ nhịp trong 90 ms.
Ảnh nhân vật được làm dịu vị trí trong 45 ms, lệch tối đa 0,15 đơn vị so với gốc;
vị trí chiến đấu, đường đi, collider và thời điểm gây sát thương vẫn theo mô phỏng.
Đổi trạng thái/hướng có hòa ảnh trong 70 ms. Các giá trị chỉnh được trong
`LuBuAnimations.asset`.
Khi tướng chết, đối tượng chiến đấu bị loại ngay, chỉ ảnh nhân vật được giữ để phát ngã.
Victory/Defeat cuối trận dùng thời gian thực để vẫn phát khi mô phỏng trận đã dừng.

## Kiểm chứng bản Smooth 2026-10-19

- Unity: **235/235 EditMode**, **85/85 PlayMode**. Bảy test Lữ Bố được chạy lại
  sau khi nhập atlas tám hướng, gồm di chuyển thật, Slow → Walk → Run, đẩy nhẹ
  không bật Run, giới hạn độ lệch hình, đánh, chết và chiến thắng.
- `RigAuditSmooth.json`: 875 mẫu ở bước 0,5 frame, scale xương luôn 1;
  574 mẫu còn cầm kích không báo xuyên thân/giáp theo bộ lọc đã nêu trên.
- `SpriteAuditSmooth.json`: 3.712 frame, tám hướng, bảy trang atlas, không bị
  cắt mép; mọi rect/pivot hợp lệ; UV nhân vật khớp nguồn.
- `MotionBefore.json` / `MotionAfter.json`: loại xoắn tích lũy ngón tay khi
  nghỉ/đi và đảo cổ tay gần 180° giữa hai frame khi đâm.
- Xem `LuBu_SmoothPreview.png` và `LuBu_EightDirections.png` trong thư mục nguồn.
  Các atlas bốn hướng và bản blend cũ vẫn giữ làm dự phòng.

## Kiểm chứng bản đầu 2026-10-19

- Unity: **235/235 EditMode**, **83/83 PlayMode**, gồm chạy/choáng, đánh ngay khi
  vừa tới mục tiêu, chết nhưng giữ ảnh ngã, chiến thắng sau khi mô phỏng dừng.
- Rig: 441 frame được kiểm tra scale, không kéo giãn xương; 290 frame của sáu clip
  còn cầm kích không báo xuyên thân/giáp. Defeat có chuyển động thả kích riêng.
- 98.598 đỉnh đều có weight, tối đa bốn ảnh hưởng; sai số tổng weight lớn nhất
  khoảng 1,6×10⁻⁷. UV checksum của bản nguồn và bản rigged giống nhau.
- 432 khung sprite bốn hướng, không có alpha chạm mép ảnh. Attack01 và Defeat
  dùng khung chụp rộng hơn; PPU/pivot được bù để nhân vật giữ đúng cỡ và điểm chân.
- Ảnh xem nhanh: `../art/concepts/LuBu_Arena3Q/LuBu_AnimationPreview.png`.
  Ảnh trong trận: `Logs/Screenshots/attack.png` và `defense.png`.
