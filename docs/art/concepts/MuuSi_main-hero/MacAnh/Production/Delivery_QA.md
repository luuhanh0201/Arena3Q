# Mặc Ảnh — bàn giao kỹ thuật

## Cập nhật21/10/2026 — đang sửa, chưa nghiệm thu

Phân công hiện tại: Codex sửa MặcẢnh, Claude kiểm độc lập. Baseline trước sửa: shader drift vẫn dùng thời gian thực, captured afterimage còn3vòng mesh, material3D chưa đồng bộ lụa; seam/motion/VFX còn thiếu bằng chứng đạt. Điểm review tạm64/100. Các kết quả và sốtest bên dưới là lịch sử, không chứng nhận bản sửa mới.

Yêu cầu mới ở GameplaySpecification§1: tối thiểu60FPS từ cấu hình, shader hòa hai frame VFX theo battle/effectclock; không áp frameblend lên nhân vật. Cần chuỗi ảnh/video chứng minh liên tục và đoFPS/p95/nhiệt trên đúngAPK/Redmi/workload5tướng+lính+địch+VFX. Android baseline mới nhất131,0MB, chủ tướngLv60, chưa có benchmark mới. Theo dõi sửa và artifact: AgentComms/codex/outbox/20261021-0006_sua-toan-bo-macanh.report.md và Logs/CodexFix/20261021/.

## Quyết định và phạm vi

- Đã đọc `START_HERE_Codex.md`, toàn bộ `MacAnh_Production_Spec_v1_1.md`, 5 ảnh tham chiếu, `CLAUDE.md`, gameplay specification và nhật ký lỗi của game.
- Người dùng duyệt **“làm luôn trên bản chính”**: rig, weight và Action được lưu trực tiếp vào `../../muu-si.blend`. Bản trước rig giữ tại `Backups/muu-si_before_rig.blend`; SHA256 `b1900c20bf0b1f9e269f2756d19e6f2e7b38afb990164e47db91b9f4ecd265cf`.
- Hai kỹ năng tự dùng: **Huyền Cơ Trận + Phong Vũ Khốn Trận**. Không thêm slot tự dùng thứ ba. Tam Vũ Liên Kích, Thiên Mưu Hộ Trận và Ảnh Bộ Phản Kích là các cấu hình thay thế có animation/VFX riêng.
- Đây là hình thể của chủ tướng Mưu Sĩ: giữ `lord_strategist`, loại Lord, không phe, luồng đặt tên người chơi, chỉ số và tăng trưởng hiện có. Không biến thành tướng có phe.

## Nguồn 3D và vũ khí

File chính có 87 xương, 11 Action mới và quạt riêng. Mesh gốc có 1.476.550 đỉnh / 2.953.808 tam giác; không retopology hoặc decimate. Chiều cao rest **1,700000 m tính cả vương miện**; chưa dùng mốc đỉnh sọ riêng để suy diễn chiều cao thân. Unit scale vẫn 1 m.

`MacAnh_Rig_QA.json` kiểm tra trực tiếp file chính: hash tọa độ, topology, UV, dữ liệu ảnh đóng gói và node vật liệu khớp preflight. Không có đỉnh mất weight; tối đa 4 ảnh hưởng/đỉnh, sai số tổng weight dưới 0,000001. Đã kiểm tra 498 khung nguồn: root in-place, scale xương bằng 1, quaternion hữu hạn; sai số grip lớn nhất dưới 0,0001 mm. Đây là kiểm tra biến đổi/socket, không phải chứng nhận không tự giao cắt ở mọi vị trí của mesh.

Nguồn ban đầu chưa có rig. Rig mới dùng IK hai đoạn đo theo model; weight từ proxy voxel và khuếch tán, sau đó tách vùng thân/tay/tóc/áo và làm mượt. Blender bone heat trên proxy không thành công; không ghi nhận đó là một lần auto-weight thành công. Không dùng rig, mesh hoặc animation Lữ Bố.

Quạt trong `Weapon/`: `.blend`, `.glb`, 5 góc, texture, báo cáo và ảnh nhập lại GLB. 13 lông, 7.268 tam giác, 6 mesh logic, rộng 0,60 m, pivot tại grip, có `Socket_Fan_Tip`. `MacAnh_Fan_RoundTrip.json` ghi kết quả nhập lại bằng Blender. Runtime Unity dùng quạt trong sprite đã render; không cần importer GLB để chơi.

## Animation và gameplay

| Action | Thời lượng | Mốc sự kiện ở 30 FPS |
| --- | --- | --- |
| Idle | 40/30 s, loop | Thở, tóc và tà áo chuyển động nhẹ |
| Walk | 36/30 s, loop | Nhịp bước theo quãng đường di chuyển |
| Run | 24/30 s, loop | Gối, thân nghiêng và tay phụ phối hợp |
| Dodge | 24/30 s | Pose né in-place để gameplay điều khiển hướng |
| Attack01 | 0,8 s | f14 SpawnWindSlash |
| Attack02 | 1,5 s | f15 / f24 / f33 SpawnTripleFeather, combo 0/1/2 |
| Skill01 | 1,8 s | f33 SpawnMagicArray |
| Skill02 | 2,0 s | f30 SpawnCyclone |
| Skill03 | 1,4 s | f27 ApplyShield |
| Ultimate | 3,5 s | f75 OnUltimateHit |
| DodgeAttack | 1,1 s | f24 SpawnCounterSlash; bóng ảnh f8–13 |

Frame 0 là pose chuẩn bị. Nhãn f14 nghĩa là 14/30 giây, không phải 13/30. Loop bỏ khung cuối trùng; các chiêu giữ nguyên khung hồi phục cuối. Không thêm Victory/Defeat khi chưa có concept Mặc Ảnh riêng.

Game đang dùng URP 2D. Vì vậy nguồn 3D được render thành sprite 8 hướng, giữ kích thước bằng mét và tọa độ socket từ pose thật. Không đưa mesh gần 3 triệu tam giác vào mỗi đơn vị chiến đấu. Các góc được render cùng lượt bằng collection instance, không giảm polygon. Metadata nằm cùng atlas, dưới `Assets/Arena3Q/Art/Heroes/MacAnh/Sprites`.

`MacAnhPerformanceView` xử lý hướng nhìn, nhịp bước theo vận tốc thực, chuyển pose và độ trễ hình có giới hạn. Không ghi ngược transform vào vị trí chiến đấu. `MacAnhCombatPerformance` dùng đồng hồ cast của gameplay và cổng chống sự kiện trùng; blend sprite không gây thêm hit. Projectile kiểm tra đoạn chuyển động với mục tiêu sống khác đội, chọn mặt va chạm đầu tiên, chỉ gây damage sau collision. Projectile đã phóng giữ snapshot khi người tung chiêu chết; kết thúc/khởi động lại trận sẽ dọn toàn bộ.

Độ dài chu kỳ bước được khớp với quãng bàn chân đi trong pha trụ: Walk = 0,21 / 0,62 = 0,33871 m; Run = 0,39 / 0,43 = 0,90698 m. Khi giảm tốc, nhịp sprite thay đổi theo vận tốc di chuyển thật. Thân có dao động nhẹ hai nhịp mỗi chu kỳ và đầu bù nghiêng để ánh nhìn ổn định.

Các prefab có ba mức Low/Medium/High: WindSlash, TripleFeather, MagicArray, Cyclone, Shield, Ultimate, Afterimage, CounterSlash và Impact. Hai prefab né/phản kích thuộc cùng bộ chiêu thứ bảy. Vòng trận có pha nén trước lúc phát; khiên theo buff và dừng khi bị phá/hết hạn. Pool đặt theo Battle; hủy cast dọn telegraph. Không có âm thanh nguồn trong gói nên không tạo audio giả.

## SkillData và thông số

Các asset mới nằm tại `Assets/Arena3Q/Content/Skills/MacAnh`. Thông số sức mạnh lấy từ các skill hiện hữu, chỉnh qua ScriptableObject; chưa cân bằng riêng cho bộ mới.

| Skill mới | Baseline dữ liệu |
| --- | --- |
| Huyền Cơ Trận | Hỏa Kế: vòng sát thương theo tick |
| Phong Vũ Khốn Trận | Bát Quái Trận: slow/weaken theo vùng |
| Thiên Mưu Hộ Trận | Hộ Vệ Trận: khiên 10% máu người tạo, 5 s; đổi phạm vi nhận sang bản thân |
| Tam Vũ Liên Kích | Liên Châu Tiễn: 3 hit; interval chỉnh 0,3 s để khớp f15/24/33 |
| Vạn Tượng Thiên Cơ | Vạn Kiếm Quy Tông: hệ số damage giữ nguyên; đổi vùng sang Circle tại điểm chọn |
| Ảnh Bộ Phản Kích | Đao Khí: projectile trúng mục tiêu; bước lùi mặc định 0,6 m trong profile, có thể chỉnh |

Không tự cấp bất tử, hất tung hay trói. Đông Phong cũ vẫn là asset độc lập; regression test của Đông Phong chọn rõ skill cũ, không giả định chủ tướng luôn dùng nó.

## Mở và tái tạo

Trong Unity: **Arena3Q → Mac Anh → Open Preview Scene**, rồi Play. Chọn Action, hướng 0–7, tốc độ, pause, chất lượng hoặc thu phóng. Panel có cuộn khi cửa sổ thấp. Scene riêng: `Assets/Arena3Q/Scenes/MacAnhPreview.unity`.

**Arena3Q → Mac Anh → Play Combat Test** mở trận thật với chủ tướng Mưu Sĩ cấp 15 và nộ đầy để thử bộ chiêu đã mở. Thiết lập này chỉ dành cho Editor; không sửa TestLoadout chung.

`Lord_Strategist.asset` được gắn bộ mặc định sau khi đủ 11 clip. Preview hero nằm ở `Content/Lords/Lord_MacAnhPreview.asset`. Không thay TestLoadout chung hoặc tài nguyên Lữ Bố để ép xuất hiện nhân vật mới.

Các script bpy nằm ở thư mục cha của báo cáo: `create_macanh_fan.py`, `build_macanh_rig.py`, `macanh_motion.py`, `bake_macanh_actions.py`, `export_macanh_sprites.py`, `validate_macanh_rig.py`. Rig builder phải mở **backup trước rig**, không chạy trên file chính đã rig. Export mặc định render mới; chỉ dùng `--reuse-cache` cho Action đã xác nhận không thay đổi. Import/Build VFX thực hiện qua menu Unity, giữ GUID asset khi cập nhật.

## Trạng thái QA

- Blender: preflight, nhập lại GLB quạt, hash nguồn, weight, toàn bộ biến đổi pose/socket đã chạy và đạt. Ảnh pose thật nằm tại `Poses/`.
- Unity Attack01: 3/3 kiểm thử ban đầu đã đạt trước khi triển khai sáu chiêu tiếp theo.
- Unity cuối: **242/242 EditMode, 94/94 PlayMode**, không skip/fail. Bao gồm mốc projectile, 3 hit combo, khiên, ultimate f75, cancel giữ nộ, caster chết sau khi phóng, pause/pool/restart, cùng regression game cũ/Lữ Bố. `Unity_QA.json` ghi job và số đo thật.
- Sprite: **3.960 hình / 27 atlas 2048²**, không crop, rect/pivot/socket hợp lệ; PNG tổng 112,6 MiB. `MacAnh_Sprite_QA.json` ghi từng clip.
- Texture trong Editor đang ở ETC2_RGBA8, không Read/Write. Dữ liệu khối ETC2 của 27 atlas tương ứng 108 MiB. Unity Editor hiện chọn target Android trên Direct3D12 không hỗ trợ ETC2; Profiler của Editor báo khoảng **540 MiB** cho tập texture này. Không dùng số này để kết luận RAM trên điện thoại. Cần đo trên thiết bị trước khi chốt ngân sách bộ nhớ.
- Shader URP 2D được Editor xác nhận hỗ trợ; scene preview đã chạy trong Game view.
- Chưa đo FPS/overdraw trên điện thoại thật; chưa xác nhận build Android/iOS. Không coi các kiểm tra Editor là kiểm thử thiết bị.

## VFX trong trận bằng flipbook (Claude, 2026-10-20)

Thay cho hiệu ứng 3D trong trận (độ giống storyboard ~15–45 %), 8 hiệu ứng được dựng trong Blender và phát
bằng sprite: `vfx_macanh_flipbooks.py` (thư mục cha) render mỗi khung hai lượt Eevee — RGB là ánh sáng phát
xạ trên nền đen có quầng (cộng lên cảnh), A là độ phủ của hình và quầng (lớp tím đậm phủ dưới, để ánh sáng
vẫn đọc được trên nền cát vàng). Ba góc: `front` (lưỡi gió, lông vũ, trúng đòn — game xoay theo hướng
bay), `game` (camera sprite nhân vật, chiếu 0,75 — cột năng lượng, lốc, vòm khiên, tia), `top` (vòng
phù văn dưới đất — game xoay và ép theo mặt đất, nên kéo dài bao lâu cũng được và ít tốn bộ nhớ).
`--preview <dir>` ghép từng khung lên nền cát và nền tối bằng đúng công thức shader để duyệt trước.

Trong Unity: **Arena3Q → Mac Anh → Graphics → Import and Bind Flipbook VFX to Battle**
(`MacAnhFlipbookBuilder.ImportAndBind`) nhập trang + `<Hiệu ứng>.recipe.json` thành sprite, asset
`MacAnhVfxRecipe` (các lớp: khung, fps, lặp, mặt phẳng, pha Charge/Release, thời điểm, mờ vào/ra, tỉ lệ,
tốc độ xoay, độ sáng, lớp phủ) và prefab `FX_MacAnh_<Hiệu ứng>Flipbook_Battle`, rồi gắn vào
`MacAnhPerformance`. `MacAnhVfxPlayer` phát công thức qua `IMacAnhSpatialEffect`, nên pool, đồng hồ trận,
socket, projectile và kiểm tra trùng sự kiện giữ nguyên; shader `Arena3Q/MacAnh Flipbook 2D` (premultiplied).
**Bind New VFX to Battle** đưa lại hiệu ứng 3D. Nhập lại sẽ ghi đè số liệu trong recipe bằng số trong script.

## Vải lót trong (Claude, 2026-10-20)

Các tấm lót Codex dựng để bịt lỗ (InnerRobe, lót tay áo, trong cổ tay/ủng; material slot 2–8) là màu xám
trơn, lộ thành mảng khi giơ tay (Ultimate, Skill02). `texture_macanh_inner_fabric.py` đổi chúng thành lụa
màu concept (trắng dưới nách, tím đậm lót tay áo, đen trong ủng) bằng node, không đổi hình khối/weight/UV:
`LOD/MacAnh_Portrait_FanClearance.blend` → `LOD/MacAnh_Portrait_InnerFabric.blend`. Đã thử và loại: mượn
vùng lụa trong atlas (atlas AI quá vụn), node AO và vân dệt (nhiễu hạt/moiré ở 8 mẫu Eevee). Hạt nhiễu
còn thấy ở ảnh cận 8 mẫu là đặc tính Eevee của mọi bề mặt và tan khi thu về cỡ sprite.
`find_macanh_noise_texture.py` dò mặt có texture xám nhiễu của model AI (chưa tìm thấy vùng đủ lớn).

## VFX flipbook – các vòng sau (Claude, 2026-10-20)

Thứ tự sửa và lý do (chi tiết lỗi trong `docs/QuyTac/NhatKySuaLoi.md`, ảnh trước–sau trong `AgentComms/claude/NghiemThu/MacAnh/XemTruoc_*`):

1. **Kích thước, độ sáng** (vòng 2–3): lưỡi gió dài hơn (cung 165°), 22 gai + cột giữa cho Skill01, 40 tia + đầu tia + 2 sóng xung kích cho Ultimate, viền/lưới/phù hiệu khiên.
2. **Gió/khói tự nhiên** (người dùng + review Codex, Model3D_Rules 28–29): lốc Skill02 là volume Eevee (`cyclone_volume_material`: 2 trường nhiễu xoắn theo độ cao quay 1 và 2 vòng/chu kỳ, bán kính thở, trục nghiêng tuế sai, chân/đỉnh tan bằng nhiễu, cage lớn hơn density; tile 1, 160 mẫu, 150 ppu); xoáy đất là nhiễu xoắn theo bán kính; đuôi gió là khói nhiễu 4D lặp bằng cross-fade W (`smoke_ribbon_material`). Gai Skill01 và tia Ultimate: vị trí, độ nghiêng, độ cong, độ dày, độ sáng và nhịp nhấp nháy riêng từng cái. Chỉ hình học trận pháp (vòng phù văn, sao) đều theo chủ ý.
3. **Bổ sung theo spec**: `UltimateAura` (vòng phép sau lưng khi gom Ultimate, §12 f31–45, theo người tung chiêu, `MacAnhPerformance.ultimateAuraStart`), `DodgeStreak` (vệt lướt khói, §13 f8) và bản sao phát sáng của bóng ảnh (`afterimageGlowMaterial/Glow/Tint`). Bóng ảnh không còn vẽ 3 vòng mesh của prefab cũ.
4. **Ultimate làm tối nền** (`MacAnhVfxRecipe.screenDim` 0,6): lớp tối nằm trên nền vẽ, dưới lính và hiệu ứng, nên chỉ mặt đất tối; gom lực bật và giữ, phát chiêu nhận lại không chớp.
5. **Dạng khí** (người dùng test điện thoại): shader có `_Breath` (đậm nhạt trôi chậm, nhiễu trong không gian cục bộ của sprite) và `_Dissolve` (tụ/tan theo nhiễu, viền sáng). `MacAnhVfxPlayer` mờ vào ≥ 0,1 s, mờ ra ≥ 0,16 s, smoothstep; `fadeIn`/`fadeOut` = 0 nghĩa là nối tiếp lớp khác. `MacAnhFx.FadeOut`: đạn trúng tan 0,16 s, hết tầm 0,25 s; gom lực sống thêm 0,3 s và tan vào phần phát chiêu. Huỷ chiêu / vỡ khiên vẫn dừng ngay (test yêu cầu thu hồi).

Quy trình làm lại một hiệu ứng: sửa hàm `build_*` / bảng `EFFECTS` trong `vfx_macanh_flipbooks.py` → `blender -b --factory-startup --python vfx_macanh_flipbooks.py -- --only <Tên> --preview Logs/...` (xem ảnh ghép nền cát + nền tối, đúng công thức shader) → nếu Unity đang build thì thêm `--out Library/ClaudeVfxStaging` rồi chép sau → Import and Bind → `MacAnhCombatTests` (chụp trận `Logs/MacAnhBattleQa`). Số liệu trình diễn (thời gian, độ sáng, lớp phủ, xoay, tối nền, đậm nhạt) chỉnh được trực tiếp trong asset recipe; nhập lại sẽ ghi đè bằng số trong script.

Test lúc ghi: PlayMode 101/101, EditMode 250/250. Điểm review độc lập của Codex: 63/100 (chưa nghiệm thu; hiệu năng Android chưa đo theo yêu cầu người dùng). Còn mở: material realtime 3D chưa theo lụa mới, capture cuối lifetime Skill01/02/03 ở 3 tier, huỷ aura sau khi sinh.
