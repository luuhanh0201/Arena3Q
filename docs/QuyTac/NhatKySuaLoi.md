# Nhật ký sửa lỗi – Arena3Q

## Mặc Ảnh A2: billboard kéo cao33% khi đổi sang camera nghiêng (21/10/2026, Codex)

- Nhánh `stretchToGround` dùng scale mặt đất/renderProjection nhưng bỏ hệ số foreshortening của camera; billboard đối diện camera nên không tự bị nén. Nhân thêm hình chiếu của pháp tuyến camera; không sửa scale lớp ground.
- Kiểm camera0°,41,40962°,60° và legacy; tại camera trận hệ số1,333333→1. XML `AfterA2_1738`:22pass/0fail/1skip (test xuất chuỗi60Hz tuỳ chọn). Ảnh cùng frame/camera `NghiemThu/MacAnh/XemTruoc_Footprint3D_20261021`.
- Phòng lặp: kiểm ảnh chân tia/vòng logic khi đổi phép chiếu, không chỉ xoay billboard. Độ tự nhiên của các tia Ultimate vẫn là mục riêng chưa đạt.

## Mặc Ảnh direct3D: bóng ảnh phóng100 lần, thiếu quạt và thủng do atlas sprite (21/10/2026, Codex)

- `BakeMesh(false)` kết hợp localToWorld áp scale FBX lần nữa; CombineMeshes mặc định chỉ lấy submesh0 và bỏ sáu mesh quạt cứng. Sửa theo quy ước native audit: BakeMesh(true)+localToWorld, gộp tất cả index thân; quạt dùng mesh chia sẻ với transform chụp tại thời điểm sinh. Fade mọi renderer và tắt pose trước Destroy cuối frame khi trả pool.
- Lượt đầu3/3test số đo qua nhưng ảnh còn vỡ: material lấy alpha atlas sprite DodgeAttack qua UV model3D. Tạo material surface riêng, mask trắng và blend alpha; builder bắt buộc gắn material này. Giữ material sprite cũ cho đường sprite.
- Hồi quy: ảnh sáu góc đo silhouette, không chỉ bounds/index; trước thấp nhất39,813%, sau100%. Index130.848/130.848, tỷ lệ chiều cao0,99999988; pool và capture trận qua3/3 trong `Logs/CodexFix/20261021/MaskAfter_1735.xml`. Ảnh trước–sau `NghiemThu/MacAnh/XemTruoc_DirectAfterimage_20261021`. Chờ review độc lập, không thay nghiệm thu toàn bộ model.

## Lữ Bố: tầng Cuồng Huyết làm lệch sát thương giữa các mục tiêu và ngưỡng 30 % bị số thực đánh lừa (2026-10-21, Claude)

- Nguyên nhân 1: tầng "mỗi lần tung chiêu" được cộng ngay trong vòng gây sát thương, nên mục tiêu thứ hai của cùng một chiêu đã nhận thêm 3 %. Sửa: `SkillExecutor.Hit` cộng tầng (`HeroSkills.OnSkillHit`) **sau** khi đánh xong mọi mục tiêu.
- Nguyên nhân 2: điều kiện kết liễu viết `Health < MaxHealth × 0,3` – với 0,3f nhân ra hơi lớn hơn, đơn vị đúng 30 % vẫn được tính là "dưới". Sửa: so tỉ lệ `Health / MaxHealth < ngưỡng` như nội tại `LowHealthTargetBonus`.
- Lưu ý khi test: tướng không có animation chiêu (mọi tướng trừ Mặc Ảnh) kết thúc chiêu ngay bước nó phát động, nên có thể đánh thường trong cùng bước đó; đo kết quả của chiêu trong sự kiện `HeroSkills.Used`, không đo sau bước mô phỏng.
- Kiểm: `LuBuSkillsTests` 6/6, `SkillCoreTests` (hất tung, miễn chậm, `FrenzyMeter`).

## Mặc Ảnh A3: flipbook VFX giật giữa các khung nguồn (2026-10-21, Claude)

- Nguyên nhân: `MacAnhVfxPlayer` chọn `floor(tuổi × fps)` cho `SpriteRenderer`, nên ở 60 FPS một flipbook 12–24 khung/giây đứng yên 3–5 khung hình rồi nhảy (WindSlash: 34/60 bước 1/60 s không đổi gì).
- Sửa: mỗi lớp là một `MeshRenderer` với tứ giác phủ hợp của khung hiện tại và khung kế (khác trang atlas, khác vùng cắt, khác pivot đều được), mỗi khung có bộ UV riêng (`uv`, `uv2`) và bị che theo vùng atlas của nó (`_RectA/_RectB`); shader hoà theo `_FrameBlend` = phần lẻ của khung trên đồng hồ hiệu ứng. Loop hoà khung cuối → đầu, không loop giữ khung cuối. Lưới mỗi cặp khung được tạo một lần rồi giữ trong bộ nhớ đệm. Màu / độ mờ của lớp chuyển từ màu đỉnh sang `_Tint` (property block). Shader thêm pass `UniversalForward` dùng chung code để chạy trên renderer 3D.
- Kiểm: `MacAnhFlipbookBlendTests` 2/2 (trước: lệch tối đa 99/255 ở giữa hai khung), toàn bộ PlayMode 110/111 (1 test bằng chứng tuỳ chọn bỏ qua), EditMode 250/250. Ảnh trước–sau 60 Hz: `AgentComms/claude/NghiemThu/A3_FrameBlend/A3_before_after.png`; sau sửa WindSlash còn 1/60 bước đứng yên.
- Phòng lặp: kiểm hoà khung bằng điểm ảnh (khung giữa = trộn tuyến tính hai đầu, hai đầu = sprite gốc vẽ riêng), không chỉ đọc chỉ số khung. Test nào tìm `SpriteRenderer` trong hiệu ứng flipbook phải đổi sang `MeshRenderer` tên `Frames`.

## MặcẢnh A2: bóng realtime còn vòng trắng (2026-10-21)

- Nguyên nhân: SetAfterimage đã ẩn các lớp mesh prefab, SetCapturedAfterimage chưa ẩn. Gom PrepareAfterimage dùng cho cảhai đường; reset phép chiếu và ẩn snapshot cũ, Begin phục hồi lớp mesh theoquality khi reuse.
- Kiểm trước: capture2route pass, testcapturedfail với3mesh cònactive. Sau:2/2pass, capturedẩn hếtmesh, reuseLow phục hồi2layer, callbackcleanup đúng1lần. Ảnh cùngcamera/t0,420s trong NghiemThu/MacAnh/XemTruoc_Afterimage3D_20261021; XMLLogs/CodexFix/20261021/AfterAfterimage.xml. Phònglặp: không dùng nhánhsprite đãđạt thay kiểm nhánhcaptured3D.

## MặcẢnh A1: shader chạy khi trận dừng (2026-10-21)

- Nguyên nhân: pool/flipbook theo BattleTime nhưng drift trong MacAnhFlipbook.shader dùng_Time.y. Sửa shader dùng_EffectTime, MacAnhVfxPlayer truyền_seconds của instance qua propertyblock.
- Kiểm cùngcamera/pose/battleTime0,3/seed, đợi0,5s thực: trước4488kênh đổi>2/255 (max15), sau0kênh/max0. UnityBeforeShader.xml FAIL, AfterShader.xml PASS trong Logs/CodexFix/20261021. Phòng lặp: kiểm pixel shader khi pause, không chỉ kiểm lifetime/pool; seek/reuse mở rộng kiểm cùng frameblendA3. Chưa coi lỗi này hết là toàn bộVFXđạt.

## Android Development: cấp tối đa để test đủ skill (2026-10-20)

- Theo yêu cầu mới, asset FrontendConfig đặt developmentLordLevel=60 (mức tối đa hiện hành); trước đó15 chỉ đủ mở4chiêu. Override chỉ ở battle lineup của Development player, không ghi đè cấp hồ sơ. Editor/release giữ cấp đã lưu. Cấp kỹ năng không bị ép max; bốn slot mở từ cấp2/5/10/15, tuyệt kỹ vẫn cần nộ.
- Kiểm: Unity PlayMode nạp config thật, maxLevel=60, triệu hồi và xác nhận4slot khả dụng với stored skillLevels=0; ảnh cùng camera/t=0 Lv15/Lv60 tại NghiemThu/MacAnh/XemTruoc_Cap60_20261020. Khi đổi maxLevel cần đối chiếu mức dev trước build, không đồng nhất cấp mở đủ kỹ năng với cấp tối đa.

Mỗi mục: **triệu chứng → nguyên nhân → cách sửa → cách tránh lặp lại → kiểm chứng**. Đọc file này trước khi sửa code gameplay hoặc chạy build/test bằng dòng lệnh.

## Mặc Ảnh VFX flipbook: các lỗi khi chuyển sang hiệu ứng vẽ sẵn (2026-10-20, Claude)

- **Trúng đích làm vỡ trận (NullReferenceException).** `MacAnhProjectiles` lấy thứ tự vẽ của hiệu ứng trúng đòn bằng `GetComponentInChildren<MeshRenderer>()`; hiệu ứng flipbook là `SpriteRenderer` nên trả null, 4/13 test Mặc Ảnh fail. Sửa: tìm `Renderer` (mesh và sprite đều là Renderer). Tránh: code chung cho mọi loại hiệu ứng không được giả định kiểu renderer. Kiểm: MacAnhCombatTests 13/13.
- **Lốc và gió như "đường kẻ lập trình sẵn"** (người dùng + review Codex 19:40). Lốc dựng bằng dải xoắn helix đều, đuôi gió là các cung song song, sọc sin tuần hoàn: vi phạm `docs/QuyTac/Model3D_Rules.md` dòng 28–29. Sửa: lốc là volume nhiễu xoắn (trục nghiêng, bán kính thở, đỉnh tan), gió là khói nhiễu 4D; chỉ hình học trận pháp được đều. Tránh: đọc rule 28–29 trước khi dựng gió/khói; không dùng sin/khoảng cách đều cho thứ tự nhiên.
- **Ultimate mất đại trận đúng lúc phát chiêu.** Phần gom lực hết giờ đúng lúc phần phát chiêu mới bắt đầu tụ (mờ vào tối thiểu 0,1 s) nên có một khoảng trống. Sửa: gom lực sống thêm 0,3 s và tan vào phần phát chiêu (`FadeOut`), lớp Charge giữ tới lúc bàn giao (fadeOut 0), fadeIn 0 = nối tiếp; lớp tối nền của gom lực tắt ngay khi bàn giao để hai lớp tối không chồng thành chớp đen. Kiểm: ảnh Ultimate t2520 trong `claude/NghiemThu/MacAnh/XemTruoc_DangKhi_TanTu_20261020/`.
- **Vòng trắng nét cứng quanh DodgeAttack.** Prefab bóng ảnh cũ `FX_MacAnh_Afterimage` vẫn vẽ 3 vòng mesh đồng tâm dưới hình bóng ảnh. Sửa: `SetAfterimage` ẩn các lớp mesh. Tránh: khi một prefab được dùng cho việc khác (bóng ảnh), tắt phần hình gốc của nó.
- **Cấp test chủ tướng lọt vào test Frontend.** `Debug.isDebugBuild` luôn đúng trong Editor, nên cấp 15 của bản Development chen vào luồng tạo nhân vật (2 test Frontend fail). Sửa: chỉ áp khi `!Application.isEditor && Debug.isDebugBuild`. Tránh: tùy chọn "chỉ bản dev" cho thiết bị phải loại Editor.
- **Trên điện thoại chỉ thấy đánh thường** không phải lỗi: chiêu chủ tướng mở ở cấp 2/5/10/15, tài khoản mới cấp 2. Bản Development trên máy dùng `FrontendConfig.developmentLordLevel` (15) để thử đủ chiêu.
- **Ảnh trong Editor kém hơn điện thoại** không do sprite: Game view bật "Low Resolution Aspect Ratios" trên màn Windows 125 % (vẽ 822×404 rồi phóng 1,25× lọc điểm). Tắt tùy chọn đó, chọn 1920×1080, scale 1×.

## Mặc Ảnh: quạt bám tay nhưng xuyên tóc và cổ tay áo (2026-10-20)

- Khoảng cách grip bằng0 và cạnh vải không giãn vẫn bỏ sót va xuyên giữa các mesh. Clamp cổ tay làm lông quạt ngả vào tóc; hướng áo rủ và tua trang trí phẳng đưa cán/ngọc/tua vào cổ tay áo.
- Chỉnh riêng channel Socket_Hand_R không deform để giữ điểm cầm, có nghiêng theo đà chiêu; chỉnh chuỗi áo bằng một phép quay chung quanh điểm gắn, giữ độ giãn cạnh. Tua được uốn cong dần ra ngoài tối đa4cm, gốc tua giữ nguyên; property giá trị đã áp ngăn bake cộng dồn. Không thêm xương, không đổi11clip hoặc thời điểm event, không ghi đè nguồn gốc.
- Kiểm tam giác thực của cả6phần quạt với body đã skin tại985mẫu/LOD, chỉ loại tiếp xúc lòng bàn tay/ngón. Cả Battle và Portrait đã qua bản đã lưu; bề mặt hai LOD cũng qua985mẫu/LOD. Đang đối chiếu lại FBX Unity và xuất sprite; chưa coi kết quả Blender là nghiệm thu cuối.
- Thêm phép xuất mesh/weight/bone pose thực đã import Unity và đối chiếu dựng lại với BakeMesh trên65đỉnh phân bố đều mỗi mẫu. Battle:985mẫu, sai lệch lớn nhất0,000000603m; audit tam giác native tiếp theo phải sạch trước bàn giao. Báo cáo cuối ghi trong codex/outbox và Production/LOD/ImportedFanContacts_*.json.

## Mặc Ảnh: sai thuật toán skinning và nguồn bake bị ghi đè (2026-10-20)

- Blender đang dùng Preserve Volume (dual quaternion), trong khi Unity dùng skin tuyến tính; ảnh Blender có thể che lỗi mà runtime vẫn kéo rách. Bản apply weight mới tắt Preserve Volume để so với Unity. Audit đo bề mặt sau skinning, gồm giãn >1,35/ép <0,65 khi sai lệch >0,004 chiều cao, gai >3 lần/+2 mm, 20 cạnh nặng nhất và vùng lỗi; bản LOD cuối bắt buộc tối đa 4 influence. Các candidate hiện vẫn fail nên chưa nghiệm thu.
- Script bake action cũ mặc định lưu bản chính. Sau quyết định LOD ngày 20/10, mặc định chuyển sang bản làm việc và chặn rõ đường dẫn nguồn gốc. Hai lần bake normal 4096² kiểm hash nguồn trước/sau vẫn bằng `8bcb4755b01621f04758cf2a4be7fa147d1d7cd9b44f9fcfe82e16960a1f0af7`.

## Mặc Ảnh: hiệu ứng mới bị già trước lúc sinh (2026-10-20)

- Khi nhiều bước mô phỏng chạy giữa hai Update, pool lấy một delta chung từ lần Update trước và áp cho cả instance mới spawn. Ultimate/impact có lifetime ngắn có thể tan ngay trước ảnh release; hình charge vẫn có nên kiểm tra chỉ ActiveCount không đủ.
- Pool lưu clock tick riêng của từng instance, khởi tạo bằng BattleTime khi spawn và xóa khi release/clear. Thêm `NewEffectDoesNotInheritTimeBeforeItWasSpawned`; chụp cả bảy chiêu theo battle clock tại zoom 4,5 và 3,5. Kết quả lượt test sau sửa sẽ ghi trong outbox; không dùng ảnh chụp trước sửa clock để nghiệm thu.

## Lữ Bố di chuyển và animation bị cứng (2026-10-19)

- **Triệu chứng:** đổi hướng giật, nhịp chân không khớp tốc độ, chen lấn làm chớp Run/Idle; ngón tay và cổ tay xoắn khi sửa tư thế cầm kích.
- **Nguyên nhân:** chỉ bốn hướng và ít mẫu hình; chọn Run từ mọi thay đổi vị trí; đặt lại pha mỗi lần đổi clip; hàm nắm tay cộng dồn độ cong ngón và IK/khung tham chiếu cổ tay đảo hướng.
- **Sửa:** vận tốc di chuyển chủ động tách khỏi dịch chuyển do đẩy; quay có tốc độ/vùng giữ hướng; Walk/Run theo tốc độ, giữ pha, nhịp chân theo quãng đường và chờ dừng ngắn. Chỉ ảnh nhân vật được làm dịu vị trí, lệch tối đa 0,15; hòa ảnh 70 ms khi đổi trạng thái/hướng. Không sửa vị trí logic hoặc thời điểm sát thương.
- Bản Blender Smooth đặt lại ngón trước khi nắm, giải tay hai đoạn với hướng khuỷu ổn định, không kéo giãn, quaternion cùng bán cầu và nội suy Bezier. Rút ngắn cán phía sau tay để tránh xuyên bắp tay/đùi của model chibi; giữ UV và bản nguồn riêng.
- **Tránh lặp lại:** kiểm tra cả frame trung gian ở tốc độ xuất; giữ điểm chân khi cắt ảnh/đóng atlas; kiểm thử bằng lệnh di chuyển thực, không dùng teleport để giả lập bước chân.
- **Kiểm chứng:** 235/235 EditMode, 85/85 PlayMode; thêm test chen lấn và Slow → Walk → Run. `RigAuditSmooth.json`: 875 mẫu scale hợp lệ, 574 mẫu còn cầm kích không báo xuyên thân/giáp; phép kiểm tra loại vùng bàn tay nắm cán và vải/lông theo thiết kế nguồn.

## Rig Lữ Bố: tay và vũ khí bị phóng lớn (2026-10-19)

- **Triệu chứng:** ảnh thử cho thấy bàn tay và kích rất lớn, che thân; pose lưu có scale RightHand tới khoảng 505.
- **Nguyên nhân:** script nguồn cho IK kéo giãn, rồi liên tục gán ma trận tay mà không đặt lại scale; các action chỉ key rotation nên scale dư truyền sang animation khác.
- **Sửa:** tắt IK stretch, đặt scale vai/cánh tay/bàn tay về 1 sau khi gán ma trận, key scale 1 trong action. Thêm audit scale trên từng frame và kiểm tra ảnh bốn hướng.
- Bone heat trên mesh gốc có nhiều lớp rời thất bại cho toàn bộ 98.598 đỉnh. Dùng lưới voxel phụ kín để giải heat weights rồi chuyển lại model gốc, giữ nguyên UV; ngón tay, đầu, áo choàng và lông trĩ được giới hạn nhóm xương riêng.
- Sửa phép tìm chân từ O(n²) thành O(n): không tính lại max chiều cao trong vòng lặp từng đỉnh. Sửa bước tạo năm action gọi xem Idle thay vì Defeat chưa tồn tại.
- **Tránh lặp lại:** không coi script compile thành công là rig đúng; kiểm tra tổng weight, số ảnh hưởng, scale xương, hình các tư thế và va chạm ở frame trung gian trước khi xuất.
- Sửa thêm thao tác lấy mẫu `bpy_prop_collection` (không hỗ trợ bước nhảy khi slice), tư thế thu kích frame 22 và tay cầm kích trong Victory; nới khung chụp Attack01/Defeat, bù PPU/pivot để không đổi cỡ/điểm chân.
- **Trong trận:** chuyển động trước cú đánh cùng frame từng làm Run ghi đè Attack; ưu tiên đòn đánh vừa bắt đầu trong frame đó. Test cộng hưởng trước đây tìm Đao Khách trong bộ thẻ thử; đổi fixture sang lấy tướng từ catalog để không phụ thuộc đội hình đang dùng Lữ Bố.
- **Kiểm chứng:** 235/235 EditMode, 83/83 PlayMode; `RigAudit.json` không còn xương giãn hoặc kích xuyên thân trong các clip cầm kích; `SpriteAudit.json` ghi 432 frame không bị cắt mép và UV khớp nguồn.

---

## Bài học trước đợt nghiệm thu (2026-10-16)

| Lỗi đã gặp | Cách tránh lặp lại |
|---|---|
| Hình tròn placeholder hiện thành bát giác | `Sprite.Create` mặc định tạo mesh bám viền; sprite sinh bằng code phải dùng `SpriteMeshType.FullRect` (`PlaceholderArt.cs`). |
| Đặt asmdef PlayMode thành *Editor-only* khiến 17 test PlayMode chạy nhầm ở EditMode và hỏng | Không bao giờ đặt `includePlatforms: ["Editor"]` cho assembly PlayMode; code dùng `UnityEditor` trong test PlayMode thì bọc `#if UNITY_EDITOR`. |
| Test ghi `PlayerPrefs` làm game mở vào ải Thủ thay vì ải Công | Test không được ghi trạng thái lâu dài (PlayerPrefs, file). Ải đang chọn chỉ nhớ trong phiên (`BattleController._lastStageIndex`). |
| Build player làm Unity tự sửa `UniversalRP.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `UnityConnectSettings.asset`, tạo `Assets/Resources` rỗng | Trước khi commit luôn xem `git status`; không commit các thay đổi phụ do build sinh ra cùng code. |
| Batchmode lỗi vì Editor đang mở project | Kiểm tra tiến trình `Unity.exe` có `Arena3Q` trong command line trước khi chạy batchmode. |
| Build Android IL2CPP bị dừng vì máy thiếu RAM (~16 GB) | Đóng Editor và ứng dụng nặng trước khi build; không tự chạy lại nếu bị dừng vì thiếu bộ nhớ. |
| Test cân bằng/test tình huống bị ảnh hưởng khi chỉnh nội dung ải | Test luồng lệnh dùng biến thể ải riêng (`Variant(...)`) với thông số cố định, không dựa vào độ khó của nội dung thật. |

---

## Đợt nghiệm thu §12 (2026-10-16)

### B – Tự vệ (Combat) đuổi theo kẻ tấn công, trái ✅ "đánh tại chỗ"
- **Triệu chứng:** đang `Moving` bị đánh ở gần, tướng bước tới đuổi kẻ tấn công thay vì đứng đánh.
- **Nguyên nhân:** `HeroBrain` trả `Engage(attacker, mayChase: true)` khi vào/đang `Combat`; `reactionRange` (1.0) lớn hơn tầm đánh cận chiến (0.6–0.7) nên nếu không đuổi thì không đánh trúng.
- **Cách sửa:** `Combat` trả `mayChase: false`; lính tự vệ khi hành quân/giữ đội hình cũng đánh tại chỗ (`TroopBrain`). `reactionRange = min(tầm đánh, 0.7)` cho mọi tướng/lính (asset + `PrototypeContentBuilder`), `CombatStats.Validate` báo lỗi nếu `reactionRange > attackRange`; giá trị mặc định trong `HeroDefinition`/`TroopDefinition` sửa cho hợp lệ.
- **Tránh lặp lại:** mọi hành vi "tại chỗ" trong spec phải ánh xạ thành `mayChase: false`, và tham số phạm vi phát hiện phải ≤ tầm đánh; ràng buộc nằm trong `Validate()` để nội dung mới không vi phạm. Test `ShippedContent_PassesValidation` chặn asset sai.
- **Kiểm chứng:** EditMode 125/125 (thêm assert `MayChase == false`, test ràng buộc, test nội dung), PlayMode 22/22.

### C – "Vừa nhận sát thương" không có thời hạn
- **Triệu chứng:** một quân địch đánh tướng một lần rồi đứng yên cạnh đường đi vẫn khiến tướng đứng lại tự vệ mãi.
- **Nguyên nhân:** `UnitBase` nhớ kẻ tấn công cho tới khi nó chết hoặc rời `reactionRange`, không quan tâm lần đánh cuối là khi nào.
- **Cách sửa:** lưu thời điểm đòn gần nhất của từng kẻ tấn công (theo `BattleController.BattleTime`, thời gian mô phỏng); `FindCloseAttacker` bỏ kẻ không đánh trong `attackerMemorySeconds` (🟡 mặc định 2 giây, trong `CombatStats`, `Validate` yêu cầu ≥ `attackInterval`). Spec §6 luật 2 ghi rõ định nghĩa.
- **Tránh lặp lại:** mọi khái niệm "vừa/gần đây" trong spec phải có thời hạn cụ thể và là tham số; dùng thời gian mô phỏng (không dùng `Time.time`) để test bước tay vẫn đúng. Khi test "đứng tại chỗ", loại các nguồn đẩy khác (đội lính, va chạm) khỏi tình huống.
- **Kiểm chứng:** PlayMode `Moving_HitOnceByCloseEnemy_DefendsInPlace_ThenWalksOnAfterTwoSeconds` (Combat sau 1.5 s, Moving lại sau 2.5 s, không tự bước); EditMode 125/125, PlayMode 23/23.

### I1 – Unit đi xuyên qua vùng cấm
- **Triệu chứng:** vùng cấm chỉ chặn điểm đến của lệnh; tướng, lính, quân địch vẫn đi thẳng xuyên qua (ví dụ hồ nước giữa bản đồ).
- **Nguyên nhân:** `UnitBase.MoveTowards` chỉ kẹp trong biên bản đồ, không biết tới vùng cấm.
- **Cách sửa:** `BattlefieldLayout.NextWaypoint` – nếu đường thẳng cắt vùng cấm (đã nở thêm bán kính unit) thì tìm đường ngắn nhất qua 4 góc của vùng đó (Dijkstra trên ≤ 6 điểm, cạnh = nhìn thấy nhau, bỏ góc ngoài bản đồ) và đi tới điểm kế tiếp; `ResolveStep`/`PushOut` là lưới an toàn khi bị va chạm đẩy vào. `SetPosition` luôn đẩy ra khỏi vùng cấm.
- **Lỗi trung gian đã gặp (đừng lặp lại):**
  1. Trượt theo mặt + "đi về góc gần" trong một hàm không trạng thái → hai luật kéo ngược nhau, unit dao động tại chỗ.
  2. Chọn góc theo chi phí `|from→góc| + |góc→đích|` khi `góc→đích` vẫn bị chặn → góc vừa đi qua trông "rẻ" hơn, unit giật qua lại quanh góc.
  3. Góc mà unit đang đứng được coi là "đã tới" mãi → đứng yên.
  Bài học: né vật cản không trạng thái phải là đường ngắn nhất **thật** trên đồ thị nhìn-thấy (mọi cạnh đều kiểm tra va chạm), loại điểm đang đứng; và phải có test đi bộ nhiều bước (không chỉ test một bước).
- **Tránh lặp lại:** test EditMode `ForbiddenZone_*` đi bộ tới 2000 bước, kiểm tra không lọt vào vùng và tới được đích, kể cả vùng dính mép bản đồ; PlayMode `MoveCommand_PastForbiddenZone_WalksAroundIt` kiểm tra cả tướng lẫn lính trong ải thật. Khi gặp lỗi di chuyển, ghi lại đường đi thật (log vị trí) trước khi đoán nguyên nhân.
- **Kiểm chứng:** EditMode 130/130, PlayMode 24/24.

### I2 – Tướng/lính đang Idle không đánh thành địch ngay trong tầm
- **Triệu chứng:** tướng tới đứng sát thành địch rồi đứng nhìn 5 giây, chỉ đánh thành khi chuyển sang Advancing.
- **Nguyên nhân:** cảm biến `EnemyInAttackRange` (tướng và lính) chỉ tìm quân, loại thành.
- **Cách sửa:** `UnitRegistry.FindNearestHostilePreferUnits`: quân địch trong tầm trước, không có quân mới tới thành trong tầm; dùng cho `EnemyInAttackRange` của `HeroUnit` và `TroopUnit`. Có mục tiêu (kể cả thành) thì bộ đếm 5 giây được đặt lại như spec. Spec §6 ghi 🟡.
- **Tránh lặp lại:** khi spec nói "địch", xác định rõ có gồm thành không và thứ tự ưu tiên; ghi vào chú thích của interface cảm biến (`IHeroSensor`, `ITroopSensor`) vì brain chỉ nhìn thấy interface.
- **Kiểm chứng:** PlayMode `Idle_NextToEnemyCastle_HitsUnitsFirstThenTheCastle`; EditMode 130/130, PlayMode 25/25.

### Lệnh tấn công phải được ưu tiên hơn tự vệ (quyết định của người dùng khi nghiệm thu I2)
- **Triệu chứng:** đang thực hiện lệnh tấn công thành, tướng bị quân địch đánh sát người thì quay sang `Combat` (🟡 `selfDefendWhileAttacking = true`), bỏ dở lệnh của người chơi.
- **Nguyên nhân:** mặc định 🟡 cũ trong spec ("trên đường tới mục tiêu áp dụng luật tự vệ giống Moving").
- **Cách sửa:** mặc định `selfDefendWhileAttacking = false` (`HeroBrainSettings`, `HeroDefinition`, 5 asset tướng); spec §6 ghi rõ quyết định. Tùy chọn vẫn bật lại được.
- **Hệ quả cần biết:** ra lệnh cho cả đội lao thẳng vào thành sẽ không đánh trả quân phòng thủ → ải Công thua (`AllHeroesLost` ở giây 42). Test cân bằng đổi sang để tướng tự Advancing (thắng giây 61, mất 1 tướng, 2 sao). Người chơi muốn dọn quân phòng thủ thì ra lệnh tấn công từng quân hoặc để tướng tự tiến.
- **Tránh lặp lại:** test cân bằng phải mô phỏng chiến thuật hợp lý của người chơi chứ không phụ thuộc vào một mặc định 🟡; khi đổi mặc định 🟡, chạy lại test cân bằng và ghi kết quả. Test "bị đánh sát người" nên tự gây sát thương có kiểm soát và khẳng định kẻ tấn công thật sự ở trong `reactionRange`, vì va chạm có thể đẩy quân địch ra xa.
- **Kiểm chứng:** EditMode 130/130 (đổi tên/viết lại 2 test HeroBrain theo mặc định mới), PlayMode 26/26 với `AttackOrder_OnCastle_TakesPriorityOverCloseAttackers`.

### I3 – Chạm vào quân/thành địch khi đang chọn tướng lại ra lệnh di chuyển
- **Triệu chứng:** chạm vào địch thì tướng chạy tới đứng cạnh (MoveCommand) thay vì tấn công; muốn tấn công phải vuốt.
- **Nguyên nhân:** `BattleInputController.Tap` chỉ phân biệt "chạm tướng" và "chạm đất".
- **Cách sửa:** logic chạm chuyển vào `BattleController.TapAt` (chạm tướng → chọn; đang chọn tướng + chạm quân/thành địch → `AttackCommand`; chạm đất → `MoveCommand`); input chỉ gọi hàm này. Spec §5 ghi 🟡.
- **Tránh lặp lại:** giữ logic diễn giải thao tác trong `BattleController` (gọi được từ test), lớp input chỉ nhận diện cử chỉ; như vậy luật điều khiển có test PlayMode mà không cần thiết bị thật.
- **Kiểm chứng:** PlayMode `Tap_SelectsHero_AttacksTappedEnemyOrCastle_MovesOnGround`; EditMode 130/130, PlayMode 27/27.

### G – Ải Công có tiền đồn giữa bản đồ, trái ✅ "phòng thủ tập trung quanh thành địch"
- **Triệu chứng:** 3 quân phòng thủ đứng ở giữa bản đồ (x 4–6.5), cách thành địch ~14 đơn vị.
- **Nguyên nhân:** lúc tạo nội dung placeholder, mình thêm tiền đồn cho trận "thú vị hơn" mà không đối chiếu spec §8.
- **Cách sửa:** dời 3 quân về vòng trong quanh thành: (12.5, ±1.5) và (22.5, 0) – trong `Stage_Attack_01.asset` và `PrototypeContentBuilder`. Giữ nguyên số quân nên độ khó gần như không đổi (cân bằng: thắng giây 62, mất 1 tướng).
- **Tránh lặp lại:** nội dung placeholder cũng phải theo spec; luật ✅ về bố trí được khóa bằng test EditMode `ShippedAttackStages_DefendersStayAroundTheEnemyCastle` (mọi quân phòng thủ trong 8 đơn vị quanh thành). Khi sửa asset bằng tay, sửa luôn `PrototypeContentBuilder` để hai nơi khớp nhau.
- **Kiểm chứng:** EditMode 131/131, PlayMode 27/27.

### H – `BattleRuntimeState` thiếu trạng thái và máu của lính (spec §10)
- **Triệu chứng:** máu/sống-chết của lính ta và quân địch chỉ nằm trong các MonoBehaviour (`TroopUnit`, `EnemyUnit`); trạng thái trận không đầy đủ như §10 yêu cầu.
- **Nguyên nhân:** khi làm Bước 2 chỉ đưa tướng và thành vào `BattleRuntimeState`; lính thêm ở Bước 4 mà không đối chiếu lại §10.
- **Cách sửa:** `UnitRuntime` (id, phe, loại `Troop`/`Enemy`, tướng chủ quản, máu, còn sống) và `RegisterUnit` / `MarkUnitDead` / `CountAliveUnits` trong `BattleRuntimeState`. `BattleController` đăng ký mỗi lính lúc sinh ra; `UnitBase.TakeDamage` cập nhật máu; khi chết thì đánh dấu. `RegisterUnit` từ chối lính không có tướng chủ quản và quân địch có chủ, giữ đúng ✅ "lính không bao giờ bị gán nhầm đội".
- **Tránh lặp lại:** mỗi khi thêm một loại đối tượng vào trận, đối chiếu lại danh sách dữ liệu ở spec §10. Mọi trạng thái trận phải có bản ghi ở Core (để test và để sau này lưu/đồng bộ), MonoBehaviour chỉ là phần hiển thị và thực thi.
- **Kiểm chứng:** EditMode 133/133 (2 test mới), PlayMode 28/28 (`RuntimeState_TracksEverySoldiersHealthAndDeath`).

### D – Icon tướng ở mép dưới bị thanh thẻ che, không bấm được
- **Triệu chứng:** tướng nằm dưới màn hình thì icon mép hiện ở mép dưới, ngay dưới thanh thẻ (icon được vẽ dưới các thẻ) nên không thấy và không chạm được.
- **Nguyên nhân:** `OffscreenHeroIndicators` kẹp icon vào toàn bộ màn hình (`ClampToEdge`), không biết tới các UI khác.
- **Cách sửa:** `EdgeIndicatorMath.ClampToArea(viewport, vùng)` – kẹp theo tia từ tâm màn hình vào một vùng bất kỳ; HUD cung cấp `CardBarTopViewport`, vùng an toàn = màn hình trừ dải thanh thẻ và lề.
- **Tránh lặp lại:** mọi phần tử UI đặt theo vị trí thế giới (icon, nhãn, marker) phải tính tới các vùng UI cố định có thể che nó; thêm test "nằm ngoài vùng bị che".
- **Kiểm chứng:** EditMode `EdgeIcon_StaysInsideTheSafeArea_AboveTheCardBar`, PlayMode `EdgeIcon_ForHeroBelowTheScreen_IsAboveTheCardBar`; EditMode 134/134, PlayMode 29/29.

### E – Chưa có góc nhìn nghiêng 2.5D (✅ §1, §9)
- **Triệu chứng:** camera chỉ nhìn thẳng từ trên xuống, không có cảm giác nghiêng kiểu Clash of Clans.
- **Nguyên nhân:** prototype đặt `transform.position` của unit bằng đúng vị trí logic, không có lớp chiếu hiển thị.
- **Cách sửa:** tách vị trí logic (`UnitBase.Position`, field riêng) khỏi vị trí hiển thị (`BattleController.ToView`: `y × cos(góc)`); mặt đất, vùng cấm, vùng triệu hồi, điểm triệu hồi, marker và vòng chọn tướng được nén theo trục Y, thân unit đứng thẳng. Camera làm việc trong không gian hiển thị nhưng API công khai (`ScreenToWorld`, `WorldToViewport`, `WorldToScreen`, `FocusOn`) nhận/trả toạ độ logic; giữ nguyên điểm đang nhìn khi đổi góc. Thanh trượt "Góc nhìn" 0–45° trong HUD, mặc định 0° theo lựa chọn của người dùng.
- **Tránh lặp lại:** **không bao giờ đọc `transform.position` làm vị trí gameplay** – dùng `UnitBase.Position`; mọi chuyển đổi màn hình ↔ chiến trường đi qua `BattleCameraController` (đã quy đổi tilt). Đồ hoạ nằm trên mặt đất phải đi qua `AddGroundVisual`/`ToView` để theo góc nghiêng. Khi thêm phép chiếu, grep `transform.position` / `WorldToScreenPoint` / `ScreenToWorldPoint` để không sót chỗ.
- **Kiểm chứng:** PlayMode `ViewTilt_SquashesTheGround_PickingStillHitsLogicPositions` (kéo slider tới 45°, vị trí vẽ = logic × cos45°, quy đổi màn hình ↔ logic khớp, chạm vào địch trên màn hình vẫn ra lệnh tấn công, camera kẹp theo bản đồ đã nén) và ảnh `Logs/Screenshots/attack_tilt45.png`; EditMode 134/134, PlayMode 30/30.

### F – Chưa có bản build Android nào (một phần)
- **Triệu chứng:** chưa từng có APK; lần thử trước bị dừng giữa chừng vì máy thiếu RAM.
- **Lần này:** `BuildScripts.BuildAndroidDev` (IL2CPP, Development) chạy hết IL2CPP và Gradle (~130 s cho Gradle), tạo ra `Builds/Android/Arena3Q-dev.apk` (40.2 MB). Ngay sau bước chép APK, Claude Code dừng tiến trình vì RAM còn ~3.5 GB, nên **không có dòng tổng kết build của Unity**. APK mở được như file zip, có `classes.dex`, `lib/arm64-v8a/libil2cpp.so`, 20 file trong `assets/bin/Data`. **Chưa cài thử lên máy Android.** Chưa có build profile iOS (cần máy Mac + Xcode để build).
- **Tránh lặp lại:** build Android cần đóng Editor và các ứng dụng nặng (Visual Studio, trình duyệt, Discord) trước; nếu bị dừng vì thiếu bộ nhớ thì không tự chạy lại. Sau mỗi lần build, xóa Gradle daemon còn sót (`java.exe` của OpenJDK trong Unity) và thư mục tạm `.utmp/`, trả lại các file URP/UnityConnect bị Unity tự sửa.

### Bài học thêm về file rác sinh ra khi chạy test/build
- Chạy test PlayMode có thể tạo `Assets/Resources/PerformanceTestRun*.json` (+ `.meta`); build Android tạo `.utmp/` ở thư mục gốc. Không commit; xóa sau khi chạy.

### A – ProjectSettings đã commit vẫn cho xoay dọc, trái ✅ "màn hình ngang"
- **Triệu chứng:** bản trong git có `allowedAutorotateToPortrait: 1` và `allowedAutorotateToPortraitUpsideDown: 1`; bản sửa chỉ nằm trong thay đổi chưa commit ở máy người dùng.
- **Cách sửa:** commit `a0f997e` chỉ gồm 2 dòng đó (đưa vào staging một bản dựng từ HEAD), các thay đổi khác trong `ProjectSettings.asset` (`companyName`, `androidResizeableActivity`, `preloadedAssets`) vẫn chưa commit, chờ người dùng quyết.
- **Tránh lặp lại:** khi nghiệm thu, đối chiếu cài đặt nền tảng với **bản đã commit** (`git show HEAD:...`), không chỉ với file trên đĩa. Muốn commit một phần file Unity YAML mà không có `git add -p`, dựng bản từ `git show HEAD:file`, sửa đúng dòng cần, rồi `git hash-object -w` + `git update-index --cacheinfo`.

## Đổi vùng cấm chữ nhật thành sông (2026-10-16)

Không phải sửa lỗi, nhưng có 3 chỗ suýt sai trong lúc làm – ghi lại để lần sau không lặp:
1. **Lối qua cầu hẹp hơn cầu nếu cắt sông đúng bằng bề rộng cầu.** Đầu khúc sông bị cắt là nửa hình tròn bán kính `nửa bề rộng sông + bán kính unit`, nên khoảng hở trên đường tâm phải dài `bề rộng cầu + bề rộng sông` thì lối đi thật mới bằng bề rộng cầu (`RiverObstacle.BuildWetPieces`).
2. **Test hình học trên sông uốn khúc dễ sai vì khúc quanh gần cầu.** Đường thử vuông góc với cầu có thể chạm khúc sông sau chỗ quanh. Test về bề rộng/lối qua dùng sông thẳng; test trên sông chuẩn chỉ kiểm tra hành vi tổng thể (không lội nước, tới được bờ bên kia, đi qua cầu – ngưỡng theo bề rộng cầu, không theo tâm cầu, vì đường ngắn nhất men sát mép cầu).
3. **Test "lính không tiến được" phải để lính thật sự đứng yên.** Bò 0.01 đơn vị/s cộng với bị tướng đẩy dần đủ vượt ngưỡng tiến độ 0.05/1.5 s → bộ đếm bị đặt lại đúng luật. Hạ xuống 0.001 và quan sát 2 s. Bài học: tham số "gần như 0" trong test phải nhỏ hơn ngưỡng của luật ít nhất một bậc; khi không giải thích được, log biến trạng thái bên trong (`TroopUnit.StuckSeconds`) thay vì đoán.

## Khớp dữ liệu ải với ảnh `map1.png` (2026-10-17)

Không phải lỗi code; ghi lại cách làm và chỗ suýt sai:
- **Đo trước, đừng ước lượng bằng mắt:** chồng viền ảnh `*_mask.png` (xuất từ dữ liệu) lên ảnh art để thấy độ lệch, rồi đo màu theo từng hàng pixel (tâm nước, mặt cầu). Lần đầu `map1` lệch tới 1.2 đơn vị ở cầu Nam và 0.6–1 đơn vị ở đoạn sông trên. Bộ lọc màu đá (bãi đặt thành) cho quá ít pixel → kết quả không tin được; khi đó dùng ảnh chồng viền để ước lượng và **kiểm tra lại bằng ảnh chồng sau khi sửa**.
- **Cầu vẽ nằm ngang ⇒ đường tâm sông dưới cầu phải thẳng đứng**, vì lối qua logic vuông góc với đoạn sông tại cầu.
- **Ảnh vẽ sẵn ở một góc ⇒ khi ải có ảnh nền, góc nhìn phải bằng góc của ảnh** (0.75 → 41.4°), nếu không ảnh bị kéo dãn sai tỉ lệ.
- **Test PlayMode dùng toạ độ cố định có thể va với bố cục mới** (vị trí gác (0, −6) rơi sát bờ cầu Nam). Test hành vi nên đặt ở vùng chắc chắn trống, hoặc tính vị trí từ dữ liệu ải (ví dụ điểm sông xa cầu nhất).

## Luật "tốc đánh" báo đã ghi vào spec nhưng thực tế không có (2026-10-17)
- **Triệu chứng:** commit `e7c0df2` ghi là đã thêm luật "tốc đánh" vào spec §6 "Chung", nhưng dòng đó không có trong file; chỉ phần dữ liệu §10 được sửa. Phát hiện khi rà spec để viết `docs/Tuong/LordAnimations.md`.
- **Nguyên nhân:** sửa bằng `perl -pe 's/...đòn\.$/.../'` trên file xuống dòng **CRLF**: `$` khớp trước `\n` nhưng sau `\r`, nên mẫu kết thúc bằng `\.$` không bao giờ khớp. Perl không báo gì khi không thay được, và lần đó mình chỉ in lại các dòng khác để kiểm tra, không kiểm tra đúng dòng này.
- **Cách sửa:** thêm lại luật bằng công cụ Edit (khớp chuỗi chính xác), kèm 🟡 luật đạn tầm xa.
- **Tránh lặp lại:** với file tài liệu (CRLF hoặc lẫn LF/CRLF), sửa bằng Edit; nếu dùng perl thì không neo `$` (hoặc dùng `\r?$`). **Sau mỗi lệnh thay thế hàng loạt, grep đúng từng chuỗi mới** để chắc đã vào file trước khi báo xong.

## Hệ cấp / sao / phe trong code (2026-10-17)

Không phải lỗi đã phát hành; ghi lại các chỗ suýt sai và số liệu cần nhớ:
1. **Đổi tên biến bằng regex làm đổi luôn chuỗi trong code.** Đổi `offense` → `growthOffense` trong `PrototypeContentBuilder` bằng `\boffense\b` làm id hệ tăng trưởng `"offense"` thành `"growthOffense"`. Phát hiện nhờ grep lại kết quả. Tránh lặp lại: đổi tên định danh bằng công cụ hiểu code (hoặc Edit từng chỗ); nếu bắt buộc dùng regex thì loại chuỗi trong ngoặc kép và **grep lại đúng các dòng vừa đổi** trước khi build.
2. **`IReadOnlyList` trỏ vào mảng làm hỏng `Has.Count` của NUnit** (`Property Count was not found`): NUnit đọc thuộc tính `Count` của kiểu thật, mảng chỉ có `Length`. Danh sách công khai mà test đếm thì dùng `List<T>`.
3. **Chí mạng cơ bản 5 % của tướng làm ải Thủ dễ hơn rõ** dù cấp 1 / 1★ giữ nguyên Máu và Công: trước – trụ tới giây 150, mất 5 tướng, thành 2055/3000; sau – quét sạch các đợt ở giây 134, mất 3 tướng, thành 2300/3000. Ải Công gần như không đổi (giây 61 → 62, mất 1 tướng, 2 sao). Chưa chỉnh lại (cân bằng để giai đoạn sắp phát hành); muốn giữ độ khó cũ thì đặt `heroBaseCritChance = 0` trong `ProgressionConfig`.
- **Kiểm chứng:** EditMode 174/174 (thêm `ProgressionRulesTests`, `SynergyRulesTests`, `ProgressionContentTests`), PlayMode 36/36 (thêm `ProgressionBattleTests`).

## Luồng vào game trong code (2026-10-17)

### Scene Frontend lưu tham chiếu `config` rỗng
- **Triệu chứng:** cả 5 test PlayMode của luồng vào game báo "FrontendController has no FrontendConfig"; trong `Frontend.unity` có `config: {fileID: 0}` dù builder đã gán.
- **Nguyên nhân:** builder tạo `FrontendConfig`, gọi `AssetDatabase.SaveAssets()`, rồi `EditorSceneManager.OpenScene(..., Single)`. Mở scene kiểu Single làm Unity giải phóng các asset **đã lưu mà không scene nào tham chiếu** → biến C# giữ asset đó thành "null giả" và gán vào scene thành rỗng. (`ProgressionConfig` ở bước trước không bị vì lúc đó chưa lưu, còn "bẩn" nên không bị giải phóng.)
- **Cách sửa:** nhớ đường dẫn asset trước khi mở scene, mở xong thì `AssetDatabase.LoadAssetAtPath` lại (cả `FrontendContentBuilder.BuildScene` lẫn `EnsureBattleSceneReferences`); builder giờ điền tham chiếu còn trống ở scene đã có, không chỉ lúc tạo scene.
- **Tránh lặp lại:** trong code Editor, **không giữ tham chiếu asset qua `OpenScene`** – nạp lại theo đường dẫn; sau khi builder chạy, grep scene xem tham chiếu có GUID chưa.

### Bài học khác
1. **Build Settings lưu GUID cũ của scene Battle** (`956276…` thay vì `df7048…` của file `.meta`). Unity vẫn tìm theo đường dẫn nên chưa từng lỗi; builder giờ xoá và thêm lại cả hai scene nên GUID đúng.
2. **Mỗi lớp ScriptableObject một file trùng tên** (`LordRoster.cs`, `ServerList.cs`…): để nhiều lớp trong một file thì asset tạo ra mất liên kết script.
3. **`Object.Destroy` chỉ xoá ở cuối khung hình:** test đổi giao diện rồi tìm nút theo tên trong cùng khung hình sẽ thấy cả nút cũ lẫn nút mới. Chờ một khung (`yield return null`) trước khi tìm.
4. **Kiểm tra bằng mắt không cần màn hình:** `FrontendFlowTests.Screenshot_FrontendScreens` ghi `Logs/Screenshots/frontend_*.png`; lần đầu xem ảnh phát hiện 4 lỗi bố cục (hộp xác nhận bị bảng phía sau lộ qua, tiêu đề đè danh sách máy chủ, ô nhập tên chồng nút, khung thừa chỗ) – đã sửa, thêm `FrontendUi.Modal` (lớp tối phủ màn hình + khung đặc).
- **Kiểm chứng:** EditMode 198/198, PlayMode 42/42 (thêm `FrontendRulesTests`, `FrontendContentTests`, `FrontendFlowTests`).

## Hệ kỹ năng trong code (2026-10-17)

Không phải lỗi đã phát hành; ghi lại các chỗ sai khi làm và số liệu cần nhớ:
1. **Lineup test không có chủ tướng đứng đầu → 9 test PlayMode đỏ.** `StartBattle(stage, lineup)` ghi `Debug.LogError` ("the first card must be the lord"), và Test Framework coi mọi log lỗi không khai báo là test hỏng. Sửa: helper `WithLord(...)` trong `SkillBattleTests` đặt chủ tướng cấp 1 (chưa có kỹ năng) lên đầu. Tránh lặp lại: lineup tự dựng trong test luôn bắt đầu bằng chủ tướng; đọc thông báo "Unhandled log message" trước khi nghi code gameplay.
2. **Viết sai kỳ vọng mở kỹ năng theo sao** (test chờ kỹ năng 3 mở ở 3★, thật ra 2★ – spec §2: 1★ nội tại + kỹ năng 2 · 2★ kỹ năng 3 · 3★ tuyệt kỹ). Tránh lặp lại: lấy số trong test từ spec / `ProgressionRules`, không nhớ.
3. **Kiểm tra dữ liệu chặn kỹ năng đơn mục tiêu nhắm đồng minh:** `SkillDefinition.Validate` chỉ cho kỹ năng `Single` nhắm `CurrentTarget`, nên Hiệu Triệu (bổ sung lính cho đội đồng minh) sẽ bị báo lỗi. Đã cho phép `AllyNeedingSoldiers`.
4. **Bố cục thẻ** (phát hiện nhờ ảnh `Logs/Screenshots/defense.png`): icon kỹ năng đè lên chân dung, và thẻ chưa triệu hồi không hiện điều kiện mở kỹ năng (chỉ đọc từ tướng trên sân). Sửa: chân dung lệch trái và nhỏ lại, trạng thái khoá đọc từ `LineupEntry.SkillLevel`. Thêm ảnh `skills.png` (chủ tướng Lv15 + tướng 1–3★ đang đánh) vào test chụp màn hình.
5. **Cân bằng sau khi có kỹ năng** (bộ thẻ thử: chủ tướng cấp 1 chưa có kỹ năng, 4 tướng 1★ có nội tại + kỹ năng 2): ải Công giây 62, mất 1 tướng, 2 sao → **giây 52, mất 0, 3 sao**; ải Thủ giây 134, mất 3 tướng, thành 2300/3000 → **giây 135, mất 3, thành 2545/3000, 1 sao**. Chưa chỉnh lại (cân bằng để giai đoạn sắp phát hành).
- **Kiểm chứng:** EditMode 212/212 (thêm `SkillCoreTests`, `SkillContentTests`), PlayMode 56/56 (thêm `SkillBattleTests`; sửa test chạm thẻ: chạm = tuyệt kỹ, giữ = chọn).

## Cấp bậc và kỹ năng quân địch trong code (2026-10-17)

Không phải lỗi đã phát hành; ghi lại các chỗ sai khi làm và số liệu cần nhớ:
1. **Đổi kiểu asset bằng cách sửa `m_Script`:** ba lính chương 1 (`Enemy_Swordsman/Archer/Brute`) chuyển từ `TroopDefinition` sang `EnemyDefinition` (lớp con) bằng cách thay GUID script trong YAML; giữ được GUID asset nên ải và scene không mất tham chiếu. Chỉ làm được vì lớp con giữ nguyên tên mọi trường của lớp cha.
2. **Sửa tên tiếng Việt trong YAML bằng `sed` với `\u`** ghi ra chữ hỏng (`"L00E2u La…"`). Tránh lặp lại: ghi thẳng chuỗi UTF-8 trong ngoặc kép (Unity đọc được), hoặc dùng công cụ Edit; grep lại dòng vừa sửa.
3. **Xoá asset kèm `.meta` để builder tạo lại** làm scene giữ một tham chiếu GUID cũ không còn tồn tại. Đã gỡ dòng đó trong `Battle.unity`. Tránh lặp lại: muốn builder tạo lại thì chỉ xoá file asset, giữ `.meta` (GUID giữ nguyên), hoặc sửa asset bằng SerializedObject.
4. **Tầm phản ứng lớn hơn tầm đánh** (`reactionRange` 0.6 > `attackRange` 0.5) ở 3 lính nhỏ: kiểm tra dữ liệu bắt được ngay. Công thức builder giờ lấy `min(tầm đánh, 1)`.
5. **Test viết sai:** vòng lặp chống khoá cứng chạy đúng 2.0 giây trong khi luật là "quá 2 giây"; test khiêu khích đặt quân địch ngoài tầm phản ứng của tướng nên tướng không đánh lại. Tránh lặp lại: đặt số trong test có khoảng dư so với ngưỡng của luật.
6. **Ải boss quá khó với bộ thẻ thử cấp 1** (ảnh `Logs/Screenshots/boss.png`: 3 tướng chết trong 25 giây ở ải boss chương 1, cấp địch 1). Ải Thủ chương 3 để cấp địch 19 theo tài liệu nên còn khó hơn. Chưa chỉnh (cân bằng để giai đoạn sắp phát hành); hai ải gốc không đổi (Công giây 52, mất 0, 3 sao; Thủ giây 135, mất 3, thành 2545/3000).
- **Kiểm chứng:** EditMode 224/224 (thêm `EnemyRulesTests`, `EnemyContentTests`), PlayMode 69/69 (thêm `EnemyBattleTests`, ảnh `boss.png`).

## Sprite vật thể từ model 3D và map Bát Quái Trận (2026-10-18)

1. **Render sprite: cột đá nằm ngang, mọi model bị nghiêng.** Triệu chứng: lần render đầu, cụm cột Bát Quái nằm lăn ra đất. Nguyên nhân: `PropSpriteRenderer` nhân ma trận mesh với `root.worldToLocalMatrix` để "bỏ vị trí gốc", nhưng FBX của Meshy dùng trục Z hướng lên và Unity đặt phép xoay đổi trục (270° quanh X) **trên chính GameObject gốc** – nhân như vậy xoá luôn phép xoay đó. Sửa: dùng nguyên `localToWorldMatrix` (vị trí đã được canh lại theo hộp bao), thêm trường `rotation` để sửa hướng model khi cần, log kích thước + góc gốc của từng model. Tránh lặp lại: không bỏ transform của gốc model; xem log "root rotation" và ảnh ghép trước khi dùng sprite.
2. **Cỡ theo bề ngang không hợp vật cao và mảnh** (cột cờ chỉ cao 1.2). Sửa: `sizeAxis` = `Footprint` hoặc `Height`.
3. **Test và ảnh chụp dựa vào toạ độ của bố cục cũ:** 3 test PlayMode / EditMode dùng sông của ải thật, test chụp ảnh ra lệnh đi tới (−6, 2) – nay nằm trong cột số 3 (HUD hiện "Không đi vào vùng cấm được"). Sửa: test sông dùng ải biến thể có sông (`TestStages.UseRiverInsteadOfFormation`), điểm chụp ảnh đổi thành (−8, 2). Tránh lặp lại (nhắc lại bài học 2026-10-17): test hành vi đặt ở vùng chắc chắn trống hoặc lấy toạ độ từ dữ liệu ải.
4. **Dòng trạng thái trận đồ đè chữ "Góc nhìn"** trên HUD (thấy nhờ `attack.png`). Sửa: đặt dưới thanh trượt (y −160).
5. **Cân bằng sau khi đổi map** (bộ thẻ thử như cũ): ải Công **giây 52, mất 0, 3 sao** (không đổi); ải Thủ giây 135, mất 3, thành 2545/3000 → **giây 122, mất 2, thành 2760/3000, 1 sao**. Chưa chỉnh (cân bằng để giai đoạn sắp phát hành).
- **Kiểm chứng:** EditMode 235/235 (thêm `BaguaFormationTests`: test (a)–(e) của `docs/BanDo/BatQuaiTran.md` §7, đài, cửa), PlayMode 77/77 (thêm `BaguaFormationBattleTests`: đổi đường khi cửa đóng giữa chừng, không kẹt khi phiến trồi, hiệu ứng Sinh / Tử / Khai, cảnh báo HUD, đài, phím dev), ảnh `attack.png` / `defense.png`.

## Thành đài trống: gán nhầm bản "đã bị phá" cho thành địch (2026-10-18)

- **Triệu chứng:** người dùng báo "cái trống chưa có animation khi bị phá, tài nguyên đã có sẵn". **Nguyên nhân:** model `ThreeKingdomsCommand_2` (cờ rách, gỗ cháy) là **trạng thái sau khi bị phá** của đài trống `ThreeKingdomsCommand_1`, nhưng Claude đoán tên Meshy bị cắt và dùng nó làm hình thành địch. **Sửa:** cả hai thành dùng `Command_1`; khi thành bị phá, `CastleFallView` chạy rung/chớp → bụi + mảnh vỡ → đổi sang `Command_2` + khói, phần đổ nát nằm lại; màn hình kết quả chờ `CastleArtConfig.resultDelaySeconds` (1.8 s) để thấy cảnh sập. **Tránh lặp lại:** hai model gần giống nhau (một bản lành, một bản hư hại) thường là hai trạng thái của cùng một vật – hỏi người dùng trước khi gán chúng thành hai vật khác nhau.
- Ảnh chụp có canvas ở chế độ `ScreenSpaceCamera` (test chụp màn hình) làm sprite có thứ tự vẽ cao nổi lên trên HUD (`castle_ruin.png`); trong game HUD là Overlay nên không bị. Chỉ là giới hạn của ảnh chụp.
- **Kiểm chứng:** EditMode 235/235, PlayMode 78/78 (thêm `CastleFall_ShakesThenLeavesTheRuin_ThenTheResultShows`; test chiến thắng chờ màn hình kết quả), ảnh `castle_fall.png`, `castle_ruin.png`.

## Rig Mặc Ảnh và sự kiện chiến đấu (2026-10-19)

1. **Mesh co xuống 1/10 sau khi parent vào rig.** Proxy heat thử ở scale 10, reset scale nhưng dependency graph còn giữ ma trận cũ; parent inverse nhận scale 0,1. Sửa: cập nhật view layer sau reset, đặt parent inverse/basis của mesh đúng identity. Hash tọa độ/UV giữ nguyên, chiều cao rest vẫn 1,70 m.
2. **Vai/tay áo xoắn và kẽ ngón bị kéo thành gai.** Dựng frame từ pole làm cánh tay tự xoay quanh trục quá lớn; mask màu loại những pixel đỏ vốn vẫn thuộc ngón tay. Sửa: aim bằng phép quay ngắn nhất, chọn weight bàn tay theo hình học, tách vùng vải phía sau khỏi cẳng tay, normalize/limit 4 và làm mượt weight. Kiểm tra pose thật nhiều góc; quạt hạ/nghiêng ở nhịp đánh để mặt còn rõ.
3. **MaterialPropertyBlock tạo từ initializer của MonoBehaviour.** Unity có thể deserialize trên luồng không cho phép gọi native constructor. Sửa: khởi tạo trong hàm chuẩn bị layer ở main thread trước khi phát FX.
4. **FX không có audio vẫn ném MissingComponentException khi trả pool.** `?.` / `??` không xử lý đúng null giả của Unity Object. Sửa: kiểm tra `source != null` / `source == null` trước khi Stop hoặc AddComponent. Hai test collision/restart đã bắt lỗi này.
5. **Projectile mới sinh có thể nhận cả delta của bước trước thời điểm phóng.** Sửa: lưu BattleTime khi sinh, chỉ bắt đầu di chuyển từ bước simulation kế tiếp. Swept collision chọn điểm vào bề mặt đầu tiên thay vì thứ tự tâm; snapshot hit không truy cập caster đã Destroy.
6. **Trùng tên field `_cooldown` ở UnitBase và CastleUnit gây lỗi serialization khi reload.** Đổi field riêng của thành thành `_castleAttackCooldown`; không thay thời gian bắn.
7. **Bước chân trượt khi nhịp clip không khớp quãng đường chân trụ.** Hiệu chỉnh stride theo biên độ/pha trụ của Action, giữ phase khi chuyển Walk/Run, thêm nhịp thân nhỏ và bù đầu. Vị trí gameplay không lấy từ sprite/transform.

8. **VFX có thể hết lifetime theo thời gian thật trong lúc simulation đứng yên.** Pool nay tick theo BattleTime; mỗi lượt thuê có generation riêng, tham chiếu cũ không được di chuyển/dừng hiệu ứng của lượt thuê mới. Test pause theo thời gian thật kiểm tra việc không trả pool sớm.

**Kiểm chứng cuối:** EditMode **242/242**, PlayMode **94/94**, không skip/fail. Blender kiểm tra 498 pose nguồn, hash mesh/UV/material/texture giữ nguyên, 3.960 sprite không crop. Chi tiết và giới hạn đo thiết bị: `art/concepts/MuuSi_main-hero/MacAnh/Production/Delivery_QA.md`.

## Nâng đồ họa Mặc Ảnh và sửa đuôi chiêu (2026-10-19)

### Hồi quy bề mặt/clock (2026-10-20, đang sửa, chưa nghiệm thu hình)
- FBX có renderer scale100. BakeMesh(false) đã tính scale đó, rồi TransformPoint nhân thêm100 làm audit báo chiều cao169m và dùng sai ngưỡng tuyệt đối. Đổi sang BakeMesh(true), đưa toàn bộ đỉnh về world metres trước đo cạnh/chiều cao và assert1,70m±0,05m. Đã chạy985mẫu/LOD thực sau import cho Battle/Portrait: đều không vượt ngưỡng giãn/ép/gai. Ảnh vẫn chưa đạt nên kết quả số chưa cho phép bàn giao.
- Pool dùng delta toàn trận cho effect vừa spawn khiến bulk simulation đẩy tuổi hiệu ứng vượt lifetime ngay sau release. Mỗi instance lưu clock riêng từ thời điểm sinh. Test spawn trong khoảng nhảy lớn đã bắt lỗi; cần giữ test này khi thay simulation.
- Afterimage spatial trả sớm, bỏ qua fade của sprite; phép xoay/chiếu của VFX áp lên sprite đã render làm bóng dẹt. Reset transform snapshot và fade bằng battle clock; nhánh3D chụp pose vào RT riêng, trả texture khi dừng/reuse. Test hồi quy và capture mới còn đang chạy.
- Diffuse albedo bake làm vàng kim loại thành đen. Nối nguyên Base Color nguồn qua Emission để bake màu; giữ UV0 (hash), thêm UV1 cho normal. Không dùng UV atlas thay UV0 để gọi là giữ UV gốc.
- Đổi tên material sau bake làm FBX thân dùng Sprite-Lit mặc định trong renderer3D và hiện mảng trắng. Nạp lại ModelImporter sau các texture reimport, gán remap cuối theo export report và assert sharedMaterials của model thực trỏ đúng asset PBR. Thêm kiểm tra vào GraphicsContentTests; xem ảnh sau import vì test combat từng đạt dù model vẫn trắng.
- Normal ghi vào UV1 nhưng tangent bake còn lấy active render UV0: mặt/vải lốm đốm và hoa văn trông vụn. Bật NormalBakeUV làm active render trong lúc bake rồi trả UV0; đối chiếu ảnh có/tắt normal và ảnh Unity. Đã chứng minh revision normal không đổi vị trí, topology, UV0/UV1 hay weight bằng hash từng mảng (`NormalOnlyRevision_Battle/Portrait.json`); giữ kết quả surface audit của geometry hiện hành. Bộ3960sprite đầu dùng normal sai đã loại và xuất lại, không dùng làm bản bàn giao.

- Preview cũ là sprite pose nhỏ và lốc nét xoắn procedural. Tạo scene/prefab3D riêng từ model gốc1,70m, không lưu đè Blender, không đổi mesh/UV/rig/texture; profile game dùng wrapper VFX mới, giữ prefab cũ.
- Bắt/sửa qua ảnh Unity: alpha dùng nhầm Mathf.SmoothStep như HLSL; màu sRGB đưa thẳng vào shader linear; camera nhân đôi xoay180° nhìn lưng; capture linear làm PNG tối. Dùng sRGB render target1920×1080 và màu linear đúng nguồn.
- ModelImporter animation event time phải chuẩn hóa0–1; nhầm giây làm Skill02 phóng trễ và Ultimate không tới event. Test kiểm tra time ở AnimationClip sau import.
- Mesh crescent có NaN ở endpoint do Pow(sin(pi),fraction) nhận số âm rất nhỏ. Clamp sin>=0 trước Pow; test mọi vertex hữu hạn.
- Particle Shader Graph UniversalForward không được Renderer2D vẽ. Thêm material Universal2D cho presentation trận, giữ graph trong preview3D.
- Đuôi chiêu khựng: giữ tốc độ đến cuối, dissolve ngưỡng làm rune chớp, burst cuối bật đồng loạt, nhân vật giữ frame recovery. Sửa easing1giây/giảm tốc UV và quay, alpha fade rune, phát hạt dần và kết thúc lifetime trước pool, preview blend Idle0,28giây. Benchmark Android chuyển sang chủ động để không tự đổi tier khi đang xem.
- Kiểm chứng sau sửa:7/7 EditMode,12/12 PlayMode (nhóm MacAnh), gồm test đuôi3,19s gần trong suốt/không còn hạt trước release3,2s. Render Console không error/warning. Không coi test chức năng là nghiệm thu nghệ thuật/FPS.
- Redmi Note8Pro đã kết nối ADB/shell, APK riêng cài thành công. Lần đầu2340×1080:13,30/13,02/14,92FPS Low/Medium/High; chưa đạt mục tiêu. Bản cập nhật và chẩn đoán8 workload đang đo, xem báo cáo sống tại `art/concepts/MuuSi_main-hero/MacAnh/Production/GraphicsUpgrade/Graphics_QA.md`.
- Bổ sung Android: phát hiện CopyDepth shader bị build stripper loại khi các quality của platform vẫn trỏ Renderer2D. Builder dùng preview pipeline tạm thời cho default/mọi quality, rồi nạp lại pipeline gốc theo đường dẫn trong finally (tham chiếu object có thể bị unload khi build). GPU skinning giữ nguyên nguồn. Bài đo GPU mới còn chờ người dùng mở khóa màn hình; không công bố FPS chưa đo. PlayMode sau sửa benchmark/JNI12/12; SHA model nguồn khớp manifest.
- Bản cuối Android144,4MB đã cài; GPU skinning và fix build depth, clamp shader pow. Đã xác nhận khôi phục pipeline2D của cả6 quality sau build bằng cách nạp lại asset theo đường dẫn. Unity mở preview Skill02. Android FPS mới còn chờ mở khóa; không lấy số bản đầu làm kết quả của bản sửa.

## 23/10/2026 – Mặc Ảnh: cổ tay rời, lót thân phẳng và tay áo quật gắt
- Lưới đã chia mảng nhưng thiếu áo trong che kín joint; nắp lót thân cũ là các mặt rộng bám pelvis. Skeleton nối đúng, audit cạnh trong từng island không thấy khe giữa các mảng.
- Thay nắp lót bằng thân áo ôm sát, bổ sung đoạn tay trong chồng kín ở khớp tới cổ tay; thu cổ tay để tránh cán quạt. Làm dịu chuyển động tay áo có trễ 0,75 frame, giữ đầu/cuối action và event; giữ riêng đường tay phải Skill01 vì cần khoảng trống quạt.
- Không hàn tự động mọi cặp đỉnh trùng rest, không lấy surface pass thay kiểm ảnh/va xuyên. Bản bridge skin tuyến tính bị ép và bản cổ tay quá to đã loại.
- Battle sau import 985 mẫu/11 clip không vượt ngưỡng giãn/ép/gai; quạt nguồn đủ 6 phần 985 mẫu không xuyên ngoài palm; PlayMode 28 pass/0 fail/1 skip. Trang 6972 ảnh, 154 trạng thái và controls kiểm trên Edge.
- Bằng chứng và giới hạn: NghiemThu/MacAnh/XemTruoc_ArmBridges_20261022/README.md. Chưa port geometry sang Portrait, chưa đo thiết bị, không tuyên bố nghiệm thu nghệ thuật tổng thể.

