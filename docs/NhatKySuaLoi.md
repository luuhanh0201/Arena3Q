# Nhật ký sửa lỗi – Arena3Q

Mỗi mục: **triệu chứng → nguyên nhân → cách sửa → cách tránh lặp lại → kiểm chứng**. Đọc file này trước khi sửa code gameplay hoặc chạy build/test bằng dòng lệnh.

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
