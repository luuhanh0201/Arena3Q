![Hình tham khảo](images/a_vertical_character_concept_sheet_game_artwork.png)

# ARENA3Q — MẶC ẢNH: PRODUCTION SPEC (MODEL 3D ĐÃ CÓ · 1,70 M)

Phiên bản v1.1 · 19/10/2026 · Đặc tả/thiết kế, không phải tài sản đã triển khai.

### Quyết định bổ sung đã duyệt ngày 20/10/2026

Người dùng đã duyệt qua AgentComms hai lưới mới từ chính nguồn hiện hữu: ngoài trận 150–300 nghìn tam giác, thử trong trận 30–60 nghìn (mục tiêu 40 nghìn). Giữ `../muu-si.blend` bất biến để render/bake; mọi sửa action/weight và giảm lưới lưu trong bản riêng. Cả hai dùng chung rig 87 xương, 11 action và tối đa 4 ảnh hưởng mỗi đỉnh. Giữ UV/màu nguồn, bake tangent normal từ nguồn mật độ cao; chi tiết silhouette phải kiểm bằng ảnh sáu góc, không thay bằng normal map.

Thử 3D thật trong trận là ngoại lệ kiến trúc URP 2D đã được duyệt: công tắc ScriptableObject mặc định **tắt**, giữ đường sprite. Phải giữ chiếu dọc 0,75, ánh sáng trên-trái, bóng mềm, che khuất/y-sort với sprite và toàn bộ vị trí logic `UnitBase.Position`. Chưa chọn cách renderer cuối cùng; phải ghi kết quả tích hợp và chi phí đo trước khi đề xuất.

Điều kiện QA bổ sung: audit mọi frame và nửa frame quanh đổi hướng sau limit-4; cả giãn >1,35 hoặc ép <0,65 khi độ lệch >0,004 lần chiều cao, cùng tiêu chí gai >3 lần/+2 mm. So sánh nguồn/LOD cùng pose/camera cho Idle và ít nhất ba chiêu, sáu góc. So sánh sprite/3D trong trận ở ortho 4,5 và 3,5 cho Idle/Run/Attack01/Skill02/Ultimate. Đo Redmi Note 8 Pro với 1 model không VFX ngoài trận và cùng workload 5 tướng+lính+địch+VFX cho hai đường trong trận; báo FPS/p95/graphics memory/PSS/load time. Chỉ người dùng quyết định có đổi mặc định.


---


## 01  /  Tổng quan & phạm vi

**Mục tiêu: GIỮ NGUYÊN model 3D Mặc Ảnh đã có (cao 1,70 m); tạo vũ khí, animation, skill và VFX tương thích.**

Mặc Ảnh là Mưu Sĩ chibi fantasy gốc, sử dụng Quạt Lông Huyền Tinh để phát huyền thuật, điều khiển gió, trận pháp, khống chế và hỗ trợ đồng minh. NGƯỜI DÙNG ĐÃ CÓ MODEL 3D MẶC ẢNH, cao 1,70 m; đây là đặc tả phát triển từ model đó, tuyệt đối không yêu cầu Codex dựng lại nhân vật. Model/rig thực tế chỉ được xác minh khi Codex đọc đúng file nguồn.

| Hạng mục | Phạm vi chính | Đầu ra kỳ vọng |
| --- | --- | --- |
| Nhân vật & rig | Sử dụng model 3D có sẵn cao 1,70 m, kiểm tra rig/socket hiện hữu | Biên bản preflight + Action mới (không dựng model mới) |
| Vũ khí | Quạt mở, tùy chọn quạt đóng; vật liệu & chốt cầm | BLEND, GLB, script bpy, render kiểm tra |
| Chuyển động | Idle, Walk, Run, Dodge và 7 animation chiêu | Action/FBX độc lập + playback test |
| Gameplay & VFX | Projectile, trận pháp, lốc, khiên, đại trận | Prefab, C# controller, event bridge |
| Kiểm thử | Không sai màu, mất vật liệu, lệch quạt, trùng event | Checklist + báo cáo rõ đã/chưa test |

> **Trạng thái đầu vào đã xác nhận** Người dùng đã có model 3D Mặc Ảnh, chiều cao mục tiêu 1,70 m. Tài liệu chưa đính kèm tệp model 3D nên CHƯA xác minh rig, transforms, số đo bàn tay, quạt hoặc Action có sẵn. Codex cần xác định file nguồn trong dự án, không tự tạo model thay thế.


### Nguyên tắc bất biến

- Không chỉnh sửa hoặc ghi đè mesh, UV, texture, material, weight, Action hay prefab hiện hữu nếu không có lý do và bản sao an toàn.
- Không tự áp rig hoặc animation Lữ Bố cho Mặc Ảnh; không thay đổi asset của nhân vật khác.
- MODEL 3D MẶC ẢNH ĐÃ CÓ là nguồn hình học chính; ảnh concept chỉ để đối chiếu. Không dựng lại nhân vật, không tự rescale hay retopology.
- Mỗi kỹ năng gồm hoạt ảnh cơ thể, VFX hình ảnh, âm thanh (nếu có asset), hit/damage và cơ chế trạng thái tách rời.
- Không ghi là “đã kiểm thử” nếu không thực sự chạy Blender hoặc Unity.
Nhân vật 3D đã có phải được giữ nguyên: số polygon, UV, texture và tỷ lệ chibi hiện hữu là dữ liệu gốc, KHÔNG ép về 15–25k triangles, PBR 2K hoặc A-pose nếu việc đó phá thiết kế/rig. Chỉ tối ưu theo báo cáo và có phê duyệt. Vũ khí tách riêng, lắp theo hand socket.


### Thông số chuẩn đã chốt — model có sẵn cao 1,70 m

| Trường | Quy tắc triển khai |
| --- | --- |
| Nguồn model | Sử dụng nguyên file 3D Mặc Ảnh đã tồn tại trong dự án; không sinh model nhân vật mới từ ảnh 2D. |
| Chiều cao nhân vật | 1,70 m (170 cm) theo yêu cầu; xác nhận khoảng đo từ bàn chân đến mốc đầu, ghi riêng phần vương miện nếu ảnh hưởng chiều cao. |
| Unity / Blender units | Giữ hệ 1 Unity unit = 1 m. Kiểm tra dimensions WORLD sau mọi parent/scale; không apply transforms mù. |
| Sai lệch tỷ lệ | Nếu model hiện hữu chưa đúng 1,70 m, chỉ đề xuất phương án chỉnh tỷ lệ ở root/asset copy và chờ xác nhận; không phá mesh/armature/animation. |
| Tỷ lệ quạt | Phải tính từ kích thước model thực, chiều dài bàn tay và socket. Kích thước trong tài liệu chỉ là khởi điểm, không áp cứng. |
| Rig hiện tại | Dùng rig/Avatar đang có. Nếu chưa rig: chuẩn bị quy trình rig riêng trên bản sao, không đụng topology/UV và không tự mượn rig Lữ Bố. |
| Status | Model 3D hiện hữu: người dùng xác nhận. Có rig, hand socket, quạt và animation hay chưa: cần preflight trên file thật. |

> **Không thay model** Không được thay bằng bản chibi cao 1,1 m của Lữ Bố hoặc phiên bản nhân vật mới tạo bởi AI. Giữ form gốc của Mặc Ảnh, kể cả khi khác hình concept.


### Danh mục tài nguyên tham chiếu

- Ảnh nhân vật: a_vertical_character_concept_sheet_game_artwork.png và các hình trước/sau/bên đã tạo.
- Ảnh vũ khí nhiều góc: a_detailed_concept_sheet_style_image_on_a_dark_pu.png.
- Ảnh storyboard skill: a_high_detail_game_design_concept_sheet_in_a_dark.png và a_high_quality_cinematic_concept_board_infograp.png.

---


## 02  /  Ngôn ngữ hình ảnh nhân vật

> Hình tham khảo: Reference nhiều góc Mặc Ảnh — dùng để đối chiếu silhouette; các góc AI có thể không hoàn toàn đồng nhất.

![Hình tham khảo](images/bản_thiết_kế_chibi_hoàng_gia_tím_vàng.png)

| Thành phần | Mô tả cần giữ |
| --- | --- |
| Tỷ lệ | Nhân vật hiện có cao 1,70 m. Giữ đúng tỷ lệ đầu/thân theo mesh thật, không ép về 2,5–3 đầu; kiểm tra đầy đủ ngón tay và khuôn mặt. |
| Gương mặt | Mắt tím, sắc mặt trầm tĩnh; nhân vật có lọn tóc trắng phía trước. |
| Tóc & vương miện | Tóc đen ánh tím dài, buộc phần trên; vương miện vàng đính pha lê tím; dải nơ/tua tím. |
| Trang phục | Áo nhiều lớp trắng–tím, viền hoa văn vàng; cầu vai, đai ngọc, dây treo, giày đen–vàng. |
| Chất liệu | Vải matte, kim loại vàng, pha lê phát sáng nhẹ; đường vân sạch, không lem màu sang bộ phận khác. |
| Phong thái | Thông minh, tiết chế chuyển động, chiến đấu bằng chỉ huy trận pháp và quạt ma thuật. |


### Bảng màu chung

| Vai trò | Màu | Mã đề xuất |
| --- | --- | --- |
| Tím đậm | Deep purple | #552C91 |
| Ma thuật | Arcane violet | #9657EF |
| Lõi sáng | Lavender glow | #D6B9FF |
| Trắng linh lực | Soft white | #F3ECFF |
| Hoa văn kim loại | Warm gold | #E6C47C |

Màu sắc được sử dụng nhất quán cho vật liệu vũ khí, shader VFX và UI kỹ năng. Không xem các mã màu là thay thế bắt buộc cho texture gốc trong model: chúng chỉ là palette định hướng.


---


## 03  /  Vũ khí — Quạt Lông Huyền Tinh

> Hình tham khảo: Concept vũ khí: mặt trước/sau/bên, trạng thái đóng/mở, chi tiết vật liệu và ví dụ hiệu ứng.

![Hình tham khảo](images/a_detailed_concept_sheet_style_image_on_a_dark_pu.png)

**Tên asset chuẩn: MacAnh_Fan. “Quạt Lông Ma Thuật” là mô tả; “Quạt Lông Huyền Tinh” là tên thiết kế nội bộ.**


### Hình dạng & vật liệu

- Quạt mở dạng nan tròn phủ 11–15 lớp lông lớn cân đối; mỗi lông có bề dày, gân và độ cong dễ đọc từ góc nghiêng.
- Màu lông chuyển từ trắng ngà sang lavender rồi tím đậm ở ngọn; không tô phẳng màu hoặc dùng một plane thay toàn bộ độ dày.
- Khung và ốp mặt quạt bằng vàng có đường trổ; viên ngọc tím đa giác ở trung tâm; tay cầm đen–vàng vừa bàn tay chibi.
- Tua và chuỗi hạt tím–vàng rủ ở phần dưới, được tách mesh để tùy chọn xương phụ.
- Vật liệu tách ít nhất: feather, gold, crystal, grip, tassel. Emission chỉ là điểm nhấn; luồng năng lượng chiêu thức nằm ở prefab VFX.

### Thông số triển khai

| Thuộc tính | Giá trị hoặc nguyên tắc |
| --- | --- |
| Tri budget | ~3.500–8.000 triangles cho vũ khí (mục tiêu). |
| Kích thước | Vừa nhân vật 3D cao 1,70 m; chỉ sau khi đo tay/silhouette. Khởi điểm tham khảo: bề rộng quạt mở 0,50–0,70 m, cần xác nhận trên model. |
| Origin/Pivot | Đặt tại điểm tay phải nắm chuôi trên MODEL HIỆN CÓ; căn theo vị trí/rotation Socket_Hand_R sau khi kiểm tra rest pose, local axis Unity. |
| Trục xuất | Nên thống nhất một quy ước local duy nhất và kiểm tra sau GLB; không tự giả định Blender Z = Unity up. |
| Trạng thái | Fan_Open bắt buộc; Fan_Closed tùy chọn, chỉ dùng animation gấp thật khi rig/mesh hỗ trợ. |
| Định dạng | MacAnh_Fan.blend, MacAnh_Fan.glb và create_macanh_fan.py. |
| Mesh logic | Fan_Handle, Fan_GoldFrame, Fan_Feathers, Fan_Crystal, Fan_Tassels, Fan_Ornaments. |


---


## 04  /  Pipeline dựng vũ khí bằng Codex


### Quy trình bắt buộc

1. Đọc ảnh concept và xác định đúng silhouette quạt mở từ mặt trước, mặt sau và độ dày nhìn bên.
1. Dựng mesh QUẠT bằng bpy trong file mới, lưu Blender source; không đụng đến source model nhân vật đã có.
1. Đặt origin ở tay nắm, kiểm tra scale và trục dọc; UV riêng có padding để tránh lem pixel sang lông hoặc khung.
1. Tạo PBR và xem render front / back / left / right / 3/4; kiểm tra kim loại vàng và mặt trái quạt.
1. Xuất GLB, nhập thử lại để kiểm tra material, orientation và kích thước.
1. Chỉ tích hợp `Socket_Hand_R` sau khi GLB độc lập đạt yêu cầu.

### Prompt Codex — dựng quạt 3D

```text
TASK: Build "MacAnh_Fan" as a REAL 3D game-ready asset in Blender 5.2.2 via bpy, using the attached feather-fan concept sheet as visual reference.
CONSTRAINTS: Work in new files only. Do not modify any character or LuBu assets.
GEOMETRY: 11–15 layered, curved, thick ivory-to-purple feathers; carved gold frame; large faceted violet center crystal; black/gold hand grip; detachable purple tassels and bead chain. Separate logical objects. A folded version is OPTIONAL.
PRODUCTION: Grip pivot at the hand contact point, correct normals, clean UV/padding, PBR materials, consistent scale for the EXISTING 1.70 m character; measure actual hand/socket dimensions first. Never rescale the character to fit the fan. Target ~3.5k–8k triangles when quality allows.
DELIVER: MacAnh_Fan.blend, MacAnh_Fan.glb, create_macanh_fan.py, front/back/left/right/3-4 renders, triangle/material/size report. Reimport GLB for validation.
REPORT: Explicitly distinguish built, exported, rendered, visually checked, and not tested.
```


### Quy tắc tỷ lệ 1,70 m & cầm quạt

- Dùng Blender world-space bounding box của Mặc Ảnh để kiểm tra chiều cao 1,70 m; loại trừ weapon khỏi đo người. Nếu vương miện được tính vào số đo, ghi rõ trong báo cáo.
- Lấy khoảng cách các khớp bàn tay/phalanx và chu vi tay thực tế để tạo grip. Không suy từ tay mẫu Lữ Bố 1,1 m.
- Đo bề rộng quạt mở và chiều dài chuôi trên model ở rest pose; mặc định gợi ý ~0,50–0,70 m bề rộng, không ép nếu silhouette khác concept.
- Khi export/import Blender → Unity, kiểm tra 1 Unity unit = 1 m, scale factor = 1 (hoặc tài liệu hóa rõ nếu project khác) và pivot giữ tại grip.

### Tiêu chí nghiệm thu riêng vũ khí

- Quạt có hình khối thật từ góc bên, không biến thành tấm phẳng.
- Lông vũ không dính vào nhau bất thường và không có màu vàng lem sang lông trắng.
- Khung vàng không xuyên qua viên ngọc; mặt sau có cấu trúc hợp lý.
- Grip/pivot không nhảy vị trí khi đổi pose hoặc khi import sang Unity.
- Các trạng thái mở/đóng, nếu có, không trùng mesh hiển thị cùng lúc.

---


## 05  /  Rig, socket & chuyển động cơ bản


### Kiểm tra rig 3D hiện hữu trước khi tạo Action

- Mở file nhân vật 3D gốc: thống kê mesh, armature, UV maps, vertex groups, modifiers, texture paths, transforms, hierarchy và các Action hiện hữu; lưu manifest trước khi sửa.
- Xác nhận model đứng đúng 1,70 m trong Blender world coordinates và Unity import scale; không scale hệ xương riêng khỏi mesh hoặc apply biến đổi lên một phần hierarchy.
- Nếu đã có Avatar/Humanoid rig thì tái sử dụng và ánh xạ socket, không dựng AutoRig khác chồng lên. Nếu CHƯA rig, tạo kế hoạch gắn xương trên BẢN SAO và chờ duyệt trước khi làm.
- Tạo snapshot trước/sau để kiểm tra vertex count, UV hash, material slots và vertex weight không đổi khi chỉ thêm Action/socket.
- Không thiết lập lại rest pose nhằm khớp các ảnh AI nếu model hiện hữu đã rig. Animation cần thích nghi với rest pose đang có.

### Xương phụ & socket

| Tên đề xuất | Vai trò | Lưu ý |
| --- | --- | --- |
| Socket_Hand_R | Gắn quạt và giữ pivot cầm | Có thể là bone hoặc transform con của tay phải. |
| Socket_Fan_Tip | Điểm sinh lưỡi gió | Gắn vào quạt, không nhầm hướng local. |
| Socket_Hand_L | Phát phù văn từ tay trái | Theo bàn tay nhưng VFX không làm biến dạng mesh. |
| Socket_Cast | Điểm sinh phép tổng quát | Dùng nếu thiếu Fan_Tip, cần thống nhất fallback. |
| Hair_Back_01–04 | Chuyển động tóc dài | Giảm biên độ để không che mặt. |
| Sleeve_L/R_01–03 | Chuyển động ống tay rộng | Không xuyên vào khuỷu/bàn tay. |
| Robe_Front / Robe_Back | Tà áo trước/sau | Có thể thay bằng chain phụ phù hợp mesh thực. |

> **Quy tắc socket** Tên socket là chuẩn đề xuất; nếu rig hiện tại đặt tên khác, Codex phải dùng bảng ánh xạ trong một cấu hình thay vì đổi tên xương hàng loạt.


### Bộ chuyển động nền

| Action | Nhịp diễn xuất | Yêu cầu |
| --- | --- | --- |
| MacAnh_Idle | Đứng bình tĩnh, thở nhẹ; quạt ở trước ngực | Tóc, nơ và tà áo dao động chậm, loop kín. |
| MacAnh_Walk | Bước phù hợp tỷ lệ 1,70 m của model thật, thân thẳng, quạt ổn định | Bàn chân ít trượt; cân bằng rõ ràng. |
| MacAnh_Run | Chạy chibi gọn, áo và tóc trễ nhịp | Quạt không xuyên vai; không rung ngón tay. |
| MacAnh_Dodge | Lướt ngắn/lùi một bước có hồi phục tư thế | Hướng né do gameplay cung cấp, không teleport root tùy tiện. |

Các chuyển động Victory/Defeat từng thiết kế cho Lữ Bố KHÔNG mặc định được xem là đã có cho Mặc Ảnh. Nếu cần bổ sung, cần chốt concept riêng và tạo Action mới.


### Nguyên tắc animation

- 30 FPS làm chuẩn đặt keyframe; animation tách Action, không ghi đè Idle/Walk/Run hoặc asset có sẵn. Biên độ bước và vị trí socket dựa trên model cao 1,70 m.
- Dùng root motion hay in-place phải đồng bộ quy ước gameplay; Codex phải phát hiện hệ thống đang dùng trước khi chọn.
- Trong đòn quạt, thân xoay nhẹ theo vai, cổ tay theo trục cầm; bàn tay phải bám chặt grip; tay trái hỗ trợ niệm phép; tóc/áo trễ pha nhẹ.

---


## 06  /  Tổng quan 7 animation chiến đấu

> Hình tham khảo: Storyboard khung chuyển động và hiệu ứng tím–vàng. Một số chữ nhỏ trong ảnh AI chỉ mang tính minh họa; dùng bảng thời gian trong tài liệu làm chuẩn.

![Hình tham khảo](images/a_high_quality_cinematic_concept_board_infograp.png)

| Action | Thời lượng | Frame event | Loại gameplay |
| --- | --- | --- | --- |
| Attack01 — Phiến Phong Trảm | 24 f ≈ 0,8 s | 14: SpawnWindSlash | Đòn thường / projectile |
| Attack02 — Tam Vũ Liên Kích | 45 f ≈ 1,5 s | 15/24/33: SpawnTripleFeather | Combo 3 projectile |
| Skill01 — Huyền Cơ Trận | 54 f ≈ 1,8 s | 33: SpawnMagicArray | AOE đặt vị trí |
| Skill02 — Phong Vũ Khốn Trận | 60 f ≈ 2,0 s | 30: SpawnCyclone | Khống chế vùng |
| Skill03 — Thiên Mưu Hộ Trận | 42 f ≈ 1,4 s | 27: ApplyShield | Khiên / buff |
| Ultimate — Vạn Tượng Thiên Cơ | 105 f ≈ 3,5 s | 75: OnUltimateHit | Đại trận / AOE |
| DodgeAttack — Ảnh Bộ Phản Kích | 33 f ≈ 1,1 s | 24: SpawnCounterSlash | Né / phản công |

“Frame 14” là nhãn khung sáng tạo trên timeline 30 FPS, thời gian event tương ứng cần xác nhận trong clip import. Các giá trị vị trí, range, tốc độ projectile và vùng skill phải xét theo nhân vật 1,70 m trong Unity units; không dùng lại thông số scale từ Lữ Bố cao ~1,1 m. Hitbox/damage do gameplay quyết định; không dùng số sát thương tự suy đoán.


---


## 07  /  Attack01 — PHIẾN PHONG TRẢM

**Đánh thường / lưỡi gió  ·  1–24 (≈0,8 giây)**

| Frame | Chuyển động & diễn xuất |
| --- | --- |
| 1–4 | Quạt mở trước ngực, trọng tâm cân bằng; đầu hướng mục tiêu. |
| 5–9 | Xoay vai phải lấy đà, rút quạt sang phải, tay trái giữ thân. |
| 10–13 | Tích tụ tia tím quanh mép quạt, tóc và tay áo trễ theo. |
| 14 | Vung quạt ngang phải → trái, phát một lưỡi gió hình trăng khuyết. |
| 15–20 | Theo đà, chậm dần, kiểm soát khuỷu và cổ tay. |
| 21–24 | Thu quạt về thế đứng chiến đấu. |


### Hiệu ứng hiển thị

Một mesh/sprite lưỡi liềm có lõi tím sáng, viền trắng tím, gợn vàng mảnh, đuôi sáng và vài hạt bụi linh lực; khi va chạm lóe tím rồi tan thành lông vũ sáng.


### Sự kiện & prefab

| Mục | Chuẩn đặt tên / xử lý |
| --- | --- |
| Animation Event | SpawnWindSlash @ f14 |
| VFX prefab | FX_MacAnh_WindSlash |
| Luồng gameplay | Phóng projectile theo hướng nhân vật/mục tiêu; chỉ gameplay áp damage sau va chạm hợp lệ; dùng Pool. |


### Kiểm tra riêng

- Quạt bám tay phải hoặc được giải phóng có chủ ý, không tự nhảy pivot và không xuyên đầu/vai.
- VFX sinh đúng frame, đúng hướng/ground target, không gọi lặp khi clip được blend hoặc phát lại.
- Đủ pha vào chiêu, cao trào, thu chiêu; tóc và áo chuyển động phụ nhưng không gây méo tay hoặc che mặt.

---


## 08  /  Attack02 — TAM VŨ LIÊN KÍCH

**Combo 3 đòn  ·  1–45 (≈1,5 giây)**

| Frame | Chuyển động & diễn xuất |
| --- | --- |
| 1–8 | Quạt lên trước mặt, nhẹ xoay cổ tay và vai. |
| 9–15 | Vung quạt lần 1; sinh lưỡi gió thứ nhất. |
| 16–24 | Đổi hướng quạt; sinh lưỡi gió thứ hai. |
| 25–33 | Bàn tay trái đẩy ra, vung quạt dứt khoát; sinh lưỡi thứ ba lớn hơn. |
| 34–45 | Quạt trở về ngực, thân ổn định và trở lại Idle. |


### Hiệu ứng hiển thị

Ba lưỡi linh lực dạng lông vũ/phiến gió, các vệt đi lệch góc nhẹ; lưỡi thứ ba lớn và sáng hơn, nhưng không che nhân vật.


### Sự kiện & prefab

| Mục | Chuẩn đặt tên / xử lý |
| --- | --- |
| Animation Event | SpawnTripleFeather @ f15, f24, f33 |
| VFX prefab | FX_MacAnh_TripleFeather |
| Luồng gameplay | Từng projectile đánh dấu comboIndex = 0/1/2; kiểm soát event không trùng, damage cấu hình trong skill data. |


### Kiểm tra riêng

- Quạt bám tay phải hoặc được giải phóng có chủ ý, không tự nhảy pivot và không xuyên đầu/vai.
- VFX sinh đúng frame, đúng hướng/ground target, không gọi lặp khi clip được blend hoặc phát lại.
- Đủ pha vào chiêu, cao trào, thu chiêu; tóc và áo chuyển động phụ nhưng không gây méo tay hoặc che mặt.

---


## 09  /  Skill01 — HUYỀN CƠ TRẬN

**Trận pháp diện rộng  ·  1–54 (≈1,8 giây)**

| Frame | Chuyển động & diễn xuất |
| --- | --- |
| 1–10 | Quạt đưa lên ngang mặt; ánh nhìn tập trung. |
| 11–20 | Tay trái chỉ về đất/địch, chân mở thế chắc. |
| 21–30 | Ngọc quạt phát sáng và các ký hiệu tím xuất hiện quanh tay. |
| 33 | Chỉ quạt về vị trí mục tiêu, đặt trận pháp. |
| 34–45 | Giữ tư thế điều khiển trận; áo và tóc nâng nhẹ. |
| 46–54 | Hạ tay, trở về thế chiến đấu. |


### Hiệu ứng hiển thị

2–3 vòng phù văn đồng tâm, quay ngược chiều, ánh sáng tím–vàng nổi khỏi mặt đất; nén 0,4–0,7 giây rồi cột năng lượng nổ và fade.


### Sự kiện & prefab

| Mục | Chuẩn đặt tên / xử lý |
| --- | --- |
| Animation Event | SpawnMagicArray @ f33 |
| VFX prefab | FX_MacAnh_MagicArray |
| Luồng gameplay | Spawn ở ground target được gameplay xác thực; phạm vi tác dụng và thời điểm nổ lấy từ SkillData, VFX có thể sống lâu hơn clip. |


### Kiểm tra riêng

- Quạt bám tay phải hoặc được giải phóng có chủ ý, không tự nhảy pivot và không xuyên đầu/vai.
- VFX sinh đúng frame, đúng hướng/ground target, không gọi lặp khi clip được blend hoặc phát lại.
- Đủ pha vào chiêu, cao trào, thu chiêu; tóc và áo chuyển động phụ nhưng không gây méo tay hoặc che mặt.

---


## 10  /  Skill02 — PHONG VŨ KHỐN TRẬN

**Khống chế / lốc xoáy  ·  1–60 (≈2 giây)**

| Frame | Chuyển động & diễn xuất |
| --- | --- |
| 1–12 | Hạ thấp trọng tâm, quạt gần eo. |
| 13–22 | Xoay quạt vòng lớn phía trước tạo quỹ đạo năng lượng. |
| 23–29 | Đưa quạt lên trên, tay trái chỉ hướng triệu hồi. |
| 30 | Triệu hồi lốc tím tại vùng mục tiêu. |
| 31–48 | Giữ pose chỉ huy, vạt áo rung do gió. |
| 49–60 | Thu lực, trở lại Idle. |


### Hiệu ứng hiển thị

Vòng xoáy đáy gắn mặt đất, cột gió nhiều dải, hạt/lông vũ bay, phù văn vàng lơ lửng, hiệu ứng giảm cường độ về cuối.


### Sự kiện & prefab

| Mục | Chuẩn đặt tên / xử lý |
| --- | --- |
| Animation Event | SpawnCyclone @ f30 |
| VFX prefab | FX_MacAnh_Cyclone |
| Luồng gameplay | Crowd-control slow/hold/knock-up là cấu hình gameplay; không suy ra mặc định. Tắt VFX khi trạng thái kết thúc. |


### Kiểm tra riêng

- Quạt bám tay phải hoặc được giải phóng có chủ ý, không tự nhảy pivot và không xuyên đầu/vai.
- VFX sinh đúng frame, đúng hướng/ground target, không gọi lặp khi clip được blend hoặc phát lại.
- Đủ pha vào chiêu, cao trào, thu chiêu; tóc và áo chuyển động phụ nhưng không gây méo tay hoặc che mặt.

---


## 11  /  Skill03 — THIÊN MƯU HỘ TRẬN

**Khiên & hỗ trợ  ·  1–42 (≈1,4 giây)**

| Frame | Chuyển động & diễn xuất |
| --- | --- |
| 1–10 | Khép quạt đặt trước ngực. |
| 11–20 | Nâng quạt trước trán, tay trái mở ngang. |
| 21–26 | Phù văn bảo hộ kết hình ở hai tay. |
| 27 | Tạo lá chắn trên nhân vật hoặc đồng minh được chọn. |
| 28–35 | Giữ tư thế bảo hộ, ánh tím giảm dần. |
| 36–42 | Trở về vị trí chiến đấu. |


### Hiệu ứng hiển thị

Khiên bán trong suốt tím nhạt, viền vàng, gợn lục giác và vòng rune. Trúng đòn sinh xung nháy tại điểm impact và ripple lan ra.


### Sự kiện & prefab

| Mục | Chuẩn đặt tên / xử lý |
| --- | --- |
| Animation Event | ApplyShield @ f27 |
| VFX prefab | FX_MacAnh_Shield |
| Luồng gameplay | Hệ thống buff quyết định lượng khiên/thời gian. Hiệu ứng tồn tại đến khi buff hết hoặc khiên vỡ, không theo Action. |


### Kiểm tra riêng

- Quạt bám tay phải hoặc được giải phóng có chủ ý, không tự nhảy pivot và không xuyên đầu/vai.
- VFX sinh đúng frame, đúng hướng/ground target, không gọi lặp khi clip được blend hoặc phát lại.
- Đủ pha vào chiêu, cao trào, thu chiêu; tóc và áo chuyển động phụ nhưng không gây méo tay hoặc che mặt.

---


## 12  /  Ultimate — VẠN TƯỢNG THIÊN CƠ

> **Thiết kế mới (người dùng 2026-10-22) — thay phần gameplay và hiệu ứng bên dưới:** không còn sát thương. Ở khung 75, một **vòng trận lan ra từ chân Mặc Ảnh** (tâm luôn là Mặc Ảnh, kéo thẻ không dời được), bán kính **5 m**. Mọi quân ta trong vòng (tướng và lính, không tính thành): **+25 % Công trong 8 giây**, **hồi 20 % máu tối đa**; các **tướng khác** (không phải Mặc Ảnh) nhận thêm **30 % thanh nộ** (🟡 số liệu, kiểm cân bằng trước khi phát hành). Tướng được buff có **tia tím xoáy quanh người** suốt 8 giây; lính được buff nhưng không có tia (giữ 60 FPS).
> Hiệu ứng lấy từ gói có sẵn (quy tắc `docs/QuyTac/QuyTacLamViec.md` §3): vòng = "Laser AOE" của Hovl Studio bỏ tia chiếu xuống, đổi màu tím Mặc Ảnh; tia quanh người = "Star aura" của Hovl. Chuyển động 105 khung và mốc khung 75 giữ nguyên; vòng phép sau lưng (khung 31) giữ nguyên. Dữ liệu: `Skill_van_tuong_thien_co.asset` (`MacAnhContentBuilder.ApplyUltimateBuff`).

**Tuyệt kỹ nhiều pha  ·  1–105 (≈3,5 giây)**

| Frame | Chuyển động & diễn xuất |
| --- | --- |
| 1–15 | Quạt khép trước ngực; tập trung linh lực. |
| 16–30 | Ngẩng đầu, nâng quạt; tóc và áo bắt đầu bay. |
| 31–45 | Quạt xoay/lơ lửng có kiểm soát, vòng ma pháp xuất hiện phía sau. |
| 46–60 | Dang hai tay, nhiều lớp phù văn hình thành. |
| 61–74 | Đại trận xoay nhanh, gom lông vũ năng lượng. |
| 75 | Quét/hạ quạt, tia năng lượng đổ xuống chiến trường. |
| 76–90 | Giữ thế uy nghi, dư chấn tỏa sáng. |
| 91–105 | Quạt trở lại tay và nhân vật về thế Idle. |


### Hiệu ứng hiển thị

4 pha Charge → Formation → Release → Dissolve: đại trận nhiều tầng trên không, vòng rune tím/vàng, các mũi năng lượng hình lông vũ, cột sáng và sóng xung kích cuối; ưu tiên tính đọc được của gameplay.


### Sự kiện & prefab

| Mục | Chuẩn đặt tên / xử lý |
| --- | --- |
| Animation Event | OnUltimateHit @ f75 |
| VFX prefab | FX_MacAnh_Ultimate |
| Luồng gameplay | VFX là trình diễn, damage diện rộng và trạng thái do server/gameplay quyết định; dùng camera shake nhẹ, quyền tắt bloom mạnh trên mobile. |


### Kiểm tra riêng

- Quạt bám tay phải hoặc được giải phóng có chủ ý, không tự nhảy pivot và không xuyên đầu/vai.
- VFX sinh đúng frame, đúng hướng/ground target, không gọi lặp khi clip được blend hoặc phát lại.
- Đủ pha vào chiêu, cao trào, thu chiêu; tóc và áo chuyển động phụ nhưng không gây méo tay hoặc che mặt.

---


## 13  /  DodgeAttack — ẢNH BỘ PHẢN KÍCH

**Né rồi phản công  ·  1–33 (≈1,1 giây)**

| Frame | Chuyển động & diễn xuất |
| --- | --- |
| 1–7 | Nghiêng thân, chuyển trọng tâm để né. |
| 8–13 | Lướt ngắn theo hướng gameplay điều khiển, tạo bóng ảnh. |
| 14–19 | Xoay người trở lại, quét quạt từ dưới lên. |
| 24 | Bắn lưỡi gió phản kích nhỏ về trước. |
| 25–33 | Đặt chân, ngực và quạt về pose chiến đấu. |


### Hiệu ứng hiển thị

Bóng ảnh tím trong suốt tàn dần, vệt thân lướt, quầng sáng xoay quanh quạt, lưỡi gió nhỏ khi phản công.


### Sự kiện & prefab

| Mục | Chuẩn đặt tên / xử lý |
| --- | --- |
| Animation Event | SpawnCounterSlash @ f24 |
| VFX prefab | FX_MacAnh_Afterimage + FX_MacAnh_CounterSlash |
| Luồng gameplay | Quy tắc invulnerability frames/hướng né chỉ thiết lập sau khi đối chiếu combat system, không tự cấp bất tử quá mức. |


### Kiểm tra riêng

- Quạt bám tay phải hoặc được giải phóng có chủ ý, không tự nhảy pivot và không xuyên đầu/vai.
- VFX sinh đúng frame, đúng hướng/ground target, không gọi lặp khi clip được blend hoặc phát lại.
- Đủ pha vào chiêu, cao trào, thu chiêu; tóc và áo chuyển động phụ nhưng không gây méo tay hoặc che mặt.

---


## 14  /  VFX: quy ước kỹ thuật & tối ưu

### Yêu cầu bổ sung đã chốt21/10/2026

- Theo GameplaySpecification§1, mục tiêu tối thiểu60FPS trên Android/iOS/PC, trong và ngoài trận. Target lấy từ dữ liệu cấu hình, không có mức30; kiểm cả Frontend, Battle chạy riêng và khôi phục sau preview/benchmark. Đạt target cấu hình không đồng nghĩa đạtFPS thực.
- Flipbook hiệu ứng phải hòa hai frame kề trong shader theo phần lẻ thời gian effect/battle; hỗ trợ khác trang/rect atlas, cuối→đầu khi loop và giữ frame cuối khi khôngloop. Pause/seek/reuse phải quyết định toàn bộ hình, gồm drift/dissolve. Không hòa frame sprite nhân vật.
- Bằng chứng nghiệm thu độ mượt gồm chuỗi frame/video60Hz cùngcamera/timing trước-sau. Hiệu năng phải đo đúngAPK trên Redmi Note8Pro, cùngworkload5tướng+lính+địch+VFX: FPS trung bình, p95frame-time, khung tụt và nhiệt. Chưa đủ60 phải báo nút thắt và tiếp tục tối ưu, không hạ yêu cầu.

> Hình tham khảo: Concept VFX tổng hợp: phiến gió, vòng trận, lốc xoáy, khiên và đại trận. Dùng định hướng bố cục, không sao chép chữ nhỏ thành tham số kỹ thuật.

![Hình tham khảo](images/a_high_detail_game_design_concept_sheet_in_a_dark.png)


### Thành phần VFX phổ biến

| Thành phần | Vai trò | Nguyên tắc |
| --- | --- | --- |
| Mesh/Sprite VFX | Hình lưỡi gió, ký tự, lá chắn, quầng sáng | Căn kích cỡ theo model 1,70 m và camera trận đấu, không dùng preset cho người 1,1 m. |
| Particle System | Bụi linh lực, lông vũ, hạt nổ, spark | Burst có kiểm soát, pooling nếu dùng lặp. |
| Shader Graph / Shader | Emission, dissolve, UV scroll, alpha, fresnel | Chỉ dùng Shader Graph nếu Unity project dùng URP/HDRP phù hợp. |
| Trails | Vệt quạt và đường bay projectile | Tắt nhanh, tránh vệt kéo dài khi teleport/disable. |
| Impact FX | Lóe trúng đòn, ripple khiên, bụi tan | Spawn đúng chỗ va chạm; dễ nhận biết thời điểm hit. |
| Audio hook | Âm gió, mở pháp trận, va chạm | Hook riêng, không buộc phải có file tiếng mới chạy được. |


### Tối ưu Arena3Q / mobile

- Không quá nhiều lớp alpha lớn phủ toàn màn hình, hạn chế overdraw và bloom; kiểm tra ở camera game chứ không chỉ scene view.
- Prefab độc lập, không gắn vĩnh viễn tất cả VFX vào model; dùng object pool, timeout/fade và cleanup khi skill bị cancel.
- LOD mức High / Medium / Low: giảm số particle, vòng rune, trail subdivisions và đèn phụ khi cần.
- Các vòng trận thường dùng mặt phẳng hoặc decal phía trên đất tránh z-fighting; mặt đất có collider hoặc target position chuẩn.
- Không dùng VFX để tính damage; gameplay là nguồn chân lý cho hit, buff, cooldown và tiêu hao tài nguyên.
- Kiểm tra bán kính vòng phép, độ cao spawn, tầm quạt và projectile trực tiếp trong Unity với model cao 1,70 m; điều chỉnh particle theo tỷ lệ thị giác trên màn hình.

### Bảng màu VFX

| Layer | Màu | Độ ưu tiên thị giác |
| --- | --- | --- |
| Lõi đòn đánh | #D6B9FF / #F3ECFF | Rõ, nhưng nhỏ và không che mặt. |
| Dải linh lực | #9657EF | Chính; đủ tương phản trên nền chiến trường. |
| Bóng/ngoại vi | #552C91 | Tạo chiều sâu; không chìm hoàn toàn ở nền tối. |
| Viền khung/phù văn | #E6C47C | Dùng tiết chế để liên kết với quạt và phục trang. |


## 15  /  Unity gameplay & cấu trúc asset


### Đề xuất tổ chức thư mục

```text
Assets/Art/Characters/MacAnh/
  Model/                  # MODEL 3D CÓ SẴN: chỉ tham chiếu, KHÔNG ghi đè
  Weapon/                 # MacAnh_Fan + materials + textures
  Animations/             # 7 combat + locomotion Actions
  VFX/Prefabs/            # FX_MacAnh_*
  VFX/Materials/          # Shader/material, textures
  Data/                   # MacAnhSkillData assets
  Scripts/                # controller, event bridge, projectile
  Editor/                 # Preview & validation tools
Docs/Characters/MacAnh/   # specification, screenshots, QA reports
```

Đường dẫn là gợi ý. Codex phải đọc cấu trúc Arena3Q hiện hữu và tích hợp đúng conventions đang có; không tự tạo một hệ thống combat thứ hai nếu game đã có.


### Thành phần C# và trách nhiệm

| Module đề xuất | Trách nhiệm |
| --- | --- |
| MacAnhSkillController | Nhận lệnh cast, kiểm tra trạng thái, gửi trigger Animator; không tính damage trong VFX. |
| MacAnhAnimationEventBridge | Đọc event frame; lấy cast context hiện hành và gọi yêu cầu spawn đúng một lần. |
| MacAnhSkillData (ScriptableObject) | Ánh xạ skill → clip, prefab, thời lượng, targeting, hit windows, gameplay tags. |
| Projectile / AreaEffect | Điều khiển chuyển động, collision, hit callbacks và vòng đời từ Pool. |
| MacAnhVfxSpawner | Spawn prefab theo socket/target, quản lý transform, thời gian tồn tại và cleanup. |
| MacAnhSkillPreview (Editor) | Phát thử từng clip/VFX, đánh dấu frame event, kiểm tra socket và test sequence. |


### Quy ước Animation Event

- Animation Event chỉ làm cầu nối thời điểm; event gọi lại hệ thống gameplay đang có, không chứa lượng damage hoặc target cố định.
- Để tránh bị gọi đôi do animator blend, ghi nhận castId + eventKey; chỉ xử lý một lần trong một lần cast.
- Lưu trữ hướng projectile và vị trí đất đã được gameplay kiểm tra trước; không đọc vị trí chuột tại frame event.
- Nếu thiếu quạt/socket, báo lỗi rõ ràng trong Editor và không tạo VFX ở world origin (0,0,0).
- Thiết lập tham số Animator/clip và event sau khi xác nhận import mode Generic/Humanoid, đường trục và chuyển động gốc.

## 16  /  Master prompt dùng trực tiếp trong Codex

**Sao chép phần sau vào Codex, đồng thời đính kèm các ảnh reference và trỏ tới tài liệu này.**

```text
Bạn là Senior Blender Technical Animator + Unity Gameplay/VFX Technical Artist cho Arena3Q. Thực thi đặc tả Mặc Ảnh trong tài liệu này và tạo asset THẬT khi có đủ môi trường, không chỉ viết kế hoạch.

ĐẦU VÀO CỐ ĐỊNH — NGƯỜI DÙNG ĐÃ CÓ MODEL 3D MẶC ẢNH, CHIỀU CAO NHÂN VẬT 1,70 M (170 CM). KHÔNG DỰNG LẠI MODEL. KHÔNG DÙNG CHIỀU CAO 1,1 M CỦA LỮ BỐ. Model 3D có sẵn là dữ liệu hình học chính; concept chỉ để tham khảo thiết kế quạt và VFX. Không thay texture, UV, normal, topology, tỷ lệ, rest pose hoặc animation sẵn có nếu chưa xin phép.

BƯỚC 0 — KIỂM TRA MODEL CÓ SẴN: Tìm file .blend/.fbx/.glb Mặc Ảnh trong workspace; nếu không có, hỏi người dùng đường dẫn/file, KHÔNG sinh nhân vật mới. Mở và kiểm tra World Dimensions (170 cm theo quy ước đo nhân vật), rotation/scale hierarchy, material, UV, rig, avatar, animation Actions, tên bone tay và hand socket. Kiểm tra Unity version/render pipeline/combat framework. Báo khác biệt nhưng KHÔNG tự scale nhân vật để chữa lỗi.

BƯỚC 1 — DỰNG RIÊNG VŨ KHÍ: Đo bàn tay phải và rest-pose của rig thật. Tạo `MacAnh_Fan` bằng Blender bpy đúng concept: lông trắng/tím nhiều lớp có độ dày; khung vàng; pha lê tím; chuôi vừa bàn tay; tua tím. Bề rộng quạt mở chỉ tham khảo 0,50–0,70 m, điều chỉnh theo silhouette và tay nhân vật cao 1,70 m. Origin tại điểm nắm. Quạt tách khỏi mesh nhân vật, không scaling người để vừa vũ khí. Xuất .blend + .glb + .py, render 5 góc và reimport kiểm tra tỷ lệ/pivot.

BƯỚC 2 — RIG & LOCOMOTION: Sử dụng rig/Avatar hiện hữu. Ánh xạ Socket_Hand_R, Socket_Fan_Tip, Socket_Hand_L, Socket_Cast. Nếu chưa rig, chỉ đề xuất kế hoạch rig trên bản sao, xin phê duyệt trước khi chỉnh. Giữ animation Idle/Walk/Run/Dodge đang có; chỉ thêm Action nào còn thiếu mà không ghi đè. Chuyển động theo bước/độ dài tay thực tế, không dùng số đo bản mẫu khác.

BƯỚC 3 — 7 ACTION ĐỘC LẬP 30 FPS: Attack01 f1–24 (event f14); Attack02 f1–45 (events f15/24/33); Skill01 f1–54 (f33); Skill02 f1–60 (f30); Skill03 f1–42 (f27); Ultimate f1–105 (f75); DodgeAttack f1–33 (f24). Tạo keyframe dựa rig hiện có, giữ grip quạt, tóc/áo có secondary motion và kiểm tra các khung trung gian. Không đổi mesh, weights hoặc rest pose.

BƯỚC 4 — UNITY VFX: Tạo 7 nhóm prefab phù hợp URP/Built-in đúng project: lưỡi gió, 3 projectile, trận pháp, lốc, khiên, đại trận, bóng ảnh/đòn phản. Kích thước và vị trí spawn tính theo model 1,70 m, socket và Unity units; không sao chép preset của Lữ Bố. Có start/impact/end, object pooling, cleanup, tùy chọn mobile quality.

BƯỚC 5 — GAMEPLAY: Gắn Animator/Animation Events/ScriptableObject vào framework đã có; quản lý cast, hit/damage, buff/CC tách khỏi VFX. Chặn event bị gọi đôi, cast đã hủy, thiếu socket và target không hợp lệ. Không hardcode balance trái với dự án.

BƯỚC 6 — TEST & BÁO CÁO: Build/compile và test Unity nếu chạy được, preview từng Action trong Blender nếu có. Kiểm tra chiều cao 1,70 m, quạt trong tay, texture/UV mesh nhân vật bất biến, clip/event/prefab hoạt động, GLB round-trip. Xuất manifest file được thêm/sửa và các bước đã test/chưa test. Không tuyên bố hoàn tất nếu không có kết quả thực tế.

AN TOÀN: Backup/commit trước chỉnh sửa; chỉ tạo file mới cho QUẠT, clip, prefab, script. Tuyệt đối không tạo/ghi đè model nhân vật Mặc Ảnh, không sửa Lữ Bố và các nhân vật khác.
```


### Gợi ý triển khai theo đợt

- Đợt A: Preflight model 3D có sẵn cao 1,70 m; đo tay/socket, dựng quạt theo tỷ lệ và xuất GLB.
- Đợt B: Hoàn thiện Attack01 từ animation đến projectile hit thực, sau đó chuẩn hóa pipeline.
- Đợt C: Attack02 / Skill01 / Skill02 / Skill03.
- Đợt D: Ultimate / DodgeAttack / preview tool / test regression.

---


## 17  /  Checklist nghiệm thu cuối

| Kiểm tra | Trạng thái cần đạt |
| --- | --- |
| Model & màu | Chính model 3D đã có cao 1,70 m, mesh/UV/normal/vật liệu/rig và chi tiết không đổi ngoài những thao tác được phê duyệt. |
| Vũ khí | Quạt có tỷ lệ đúng tay nhân vật 1,70 m; đủ khung vàng/lông/ngọc/tua; GLB đúng hướng/scale và grip không rời tay. |
| Animation | Đủ Action 7 skill; frame đúng, pose rõ, in-between không xuyên mesh hoặc lật cổ tay. |
| VFX | Đủ prefab, không Missing Script/Material; pha xuất hiện, hit, biến mất chạy đúng. |
| Sự kiện | Không bị phát đôi; animation event trỏ đúng hàm/sự kiện. |
| Gameplay | Damage, hit confirm, buff/CC/cooldown tích hợp theo hệ thống hiện có. |
| Hiệu năng | Kiểm tra trên mobile target, profile overdraw/particles, có mức giảm chất lượng. |
| Xuất file | Blender lưu được, GLB/FBX reimport ổn, không mất texture hay scale. |
| Regression | Không thay đổi LuBu và các model khác; git diff chỉ chứa file được phép. |
| Báo cáo | Danh sách asset tạo mới, test pass/fail, ảnh preview và blocker công khai. |


### Điều kiện dừng & cảnh báo

- Người dùng xác nhận ĐÃ CÓ model 3D; nếu Codex chưa tìm được file 3D trong workspace thì phải xin đường dẫn/tệp, không thay bằng mesh tự sinh từ concept. Nếu model chưa rig: hoãn animation phụ thuộc rig và đề xuất rig an toàn trên bản sao.
- Nếu ảnh concept nhiều góc có chi tiết mâu thuẫn: dùng ảnh chính diện làm chuẩn mặt/trang phục và mặt sau/bên làm định hướng hình khối; ghi chỗ chưa xác nhận.
- Nếu Unity pipeline hoặc combat architecture khác mô tả: ưu tiên tích hợp theo cấu trúc thực tế của repository, nêu thay đổi trong báo cáo.
- Các hình minh họa kỹ năng/UV/wireframe do AI tạo không thay thế kiểm tra hình học thật hoặc asset runtime.

### Bàn giao tối thiểu

MODEL NHÂN VẬT: dùng source 3D đã có, không xuất thêm model Mặc Ảnh do AI tạo. BÀN GIAO MỚI: 01 source .blend QUẠT, 01 GLB quạt, script bpy dựng quạt, render 5 góc, các Action Mặc Ảnh áp lên rig hiện hữu (sau khi xác minh), 7 prefab VFX, controller & SkillData, preview tool, báo cáo chiều cao 1,70 m và QA. Các sự kiện bám theo chương 06–13.

