# Mặc Ảnh — kiểm tra nguồn và phương án bổ sung rig

Nguồn yêu cầu: `../START_HERE_Codex.md`, `../MacAnh_Production_Spec_v1_1.md`.
**Quyết định của người dùng sau preflight:** “làm luôn trên bản chính”. Cho phép
thêm rig/weight/Action và lưu vào `../../muu-si.blend`, thay cho phương án chỉ
làm trên bản sao bên dưới. Bản trước rig đã sao lưu tại
`Backups/muu-si_before_rig.blend`, SHA256
`b1900c20bf0b1f9e269f2756d19e6f2e7b38afb990164e47db91b9f4ecd265cf`.
Phần bên dưới ghi lại **thời điểm preflight**, trước khi thêm xương/weight.
Trạng thái sau triển khai và kết quả kiểm tra nằm tại `Delivery_QA.md`.

## Kết quả preflight

- Source: `../../muu-si.blend`, 299.342.371 byte.
- Một object mesh `Mesh_0.001`: **1.476.550 đỉnh, 2.953.808 mặt tam giác**.
- World bounds: rộng 1,019004 m, sâu 0,736397 m, cao **1,700000 m**.
  Điểm thấp nhất xấp xỉ Z=0; số đo tổng bao gồm vương miện. Chưa tách mốc đỉnh sọ.
- Blender Metric, unit scale 1; object location/rotation bằng 0, scale=(1,1,1).
- **Không có Armature, bone, vertex group, modifier hay animation Action.**
  Chưa có Avatar Unity hoặc hand socket để tái sử dụng; không giả định đã rig.
- Một UV map, một material; ba texture đã packed: 8192², 4096², 4096².
  Chi tiết tọa độ, topology, UV, texture hashes và node vật liệu trong
  `MacAnh_Preflight.json`.
- Đã render nhân vật gốc ở năm góc trong `Preflight/`; chưa đổi mesh, UV,
  texture, vật liệu, transforms hoặc tỷ lệ của source.
- Môi trường hiện có Blender 4.5.3 LTS; prompt ví dụ nêu Blender 5.2.2.
  Dùng phiên bản thực tế để dựng quạt và kiểm chứng, ghi rõ phiên bản đầu ra.

## Phần đang được thực hiện độc lập

Dựng **chỉ quạt** trong file mới: 13 lông cong có độ dày, trắng–lavender–tím,
khung vàng chạm nổi ở hai mặt, pha lê tím, chuôi đen–vàng, tua/hạt rời.
Đo vùng bàn tay trên mesh tĩnh để chọn chuôi, không suy từ Lữ Bố. Vì chưa có
bone tay, vị trí lắp cuối cùng phải được kiểm tra lại sau khi rig được duyệt.
Xuất BLEND/GLB/script, render năm góc và GLB round-trip. Không đưa quạt vào
source nhân vật trong bước này.

## Phương án rig xin duyệt

1. Tạo **bản làm việc riêng** `MacAnh_Rig_Working.blend`; giữ source 299 MB
   bất biến. Không dựng lại nhân vật, decimate, retopology, sửa texture hoặc UV.
2. Dựng armature riêng theo landmark của chính model Mặc Ảnh: root, pelvis,
   spine/chest/neck/head; clavicle/upper arm/forearm/hand hai bên; xương ngón
   theo hình học có thật; thigh/shin/foot/toe. Không sao chép rig/rest pose Lữ Bố.
   Những vùng ngón liền nhau không tách được sẽ ghi rõ giới hạn và kiểm tra
   tư thế nắm; không tự tách/đắp lại mesh.
3. Bổ sung chain tóc, dải nơ, tay áo, tà áo trên mesh hiện hữu; vùng trang trí
   cứng bám xương tương ứng. Dùng lưới phụ để giải weight nếu cần, chỉ chuyển
   weight về source copy, tối đa bốn ảnh hưởng mỗi đỉnh và chuẩn hóa.
4. Thêm mapping `Socket_Hand_R`, `Socket_Hand_L`, `Socket_Cast`; quạt có
   `Socket_Fan_Tip`. Scale xương luôn 1, không dùng IK stretch; kiểm tra nắm
   quạt, va chạm tay–áo–tóc, không ghi đè bất kỳ tài nguyên nhân vật khác.
5. So sánh snapshot trước/sau: số đỉnh, topology, tọa độ rest mesh, UV, texture,
   material phải giữ nguyên. Armature, vertex groups/weights và Action là các
   dữ liệu mới được bổ sung trong bản sao sau khi được duyệt.
6. Tạo Idle/Walk/Run/Dodge **in-place**, vì Arena3Q điều khiển vị trí bằng mô
   phỏng 2D; animation không tự di chuyển gameplay root. Hoàn thiện Attack01
   trước (24 frame/30 fps, event f14), sau đó sáu action còn lại đúng timeline
   spec. Không tự thêm Victory/Defeat theo Lữ Bố.

## Tích hợp Unity dự kiến sau khi rig được duyệt

- Giữ URP 2D và hệ chiến đấu đang có. Nguồn 3D/rig dùng tạo animation, sau đó
  xuất sprite và socket track theo tám hướng cho bản game hiện tại. Không
  chuyển renderer toàn dự án sang 3D và không đưa mesh gần 3 triệu triangle
  vào runtime mobile khi chưa có quyết định tối ưu riêng.
- Event track dùng cùng mốc 30 fps với spec; vị trí socket được chiếu theo
  `BattleController.ToView`. Cast context cố định hướng/target, event chống
  trùng theo castId+eventKey, kiểm tra hủy cast/thiếu socket.
- Bảy nhóm VFX tách rời, pooling, cleanup, mức chất lượng; hit/buff/CC gọi hệ
  gameplay, không tính sát thương trong VFX. Các con số cân bằng lấy dữ liệu
  dự án, không suy từ hình concept.
- Spec nhân vật có bảy action chiến đấu, còn game có bốn slot kỹ năng; action
  và VFX không đồng nghĩa bảy nút kỹ năng mới. Cần bảng mapping theo framework
  hiện tại trước khi đổi bộ kỹ năng chủ tướng Mưu Sĩ. Không đổi tên người chơi
  thành “Mặc Ảnh” trong luồng chủ tướng.

## Trạng thái

- Đã kiểm chứng: mở source thật, thống kê mesh/material/UV/transforms, thiếu
  rig, chiều cao tổng, render năm góc.
- Chưa thực hiện: rig/weight, animation và tích hợp nhân vật vào trận.
- Điểm cần duyệt: **cho phép bổ sung rig và weight trên bản sao**, theo điều
  khoản dừng khi model chưa rig trong START_HERE và spec §05/§16/§17.
