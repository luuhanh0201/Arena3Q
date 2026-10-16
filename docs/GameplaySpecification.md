# Arena3Q – Gameplay Specification

Phiên bản: v2 · 2026-10-16

Ký hiệu:
- ✅ **Đã chốt** – làm đúng như ghi, không tự đổi.
- 🟡 **Mặc định tạm** – dùng giá trị này cho prototype, phải là **tùy chọn cấu hình**, chờ người dùng xác nhận.
- ⏳ **Để sau** – không triển khai ở prototype, chỉ chừa chỗ trong dữ liệu nếu rẻ.

Tài liệu này là **nguồn đúng duy nhất** cho gameplay; khi mâu thuẫn với prompt hay ghi chú khác thì theo tài liệu này. Đặc biệt, luật cũ "tới đích thì đứng chờ" đã được thay bằng luật Idle và chờ 5 giây ở mục 6.

---

## 1. Tổng quan ✅
- Engine Unity 6.6, 2D URP, C#. Nền tảng Android, iOS, PC. Màn hình ngang.
- Chế độ hiện tại: **PvE đánh ải**, gồm hai loại ải **Công** và **Thủ**. PvP làm sau.
- Người chơi triệu hồi **tối đa 5 tướng mỗi trận**. Mỗi tướng dẫn một đội lính riêng.
- Góc nhìn nghiêng từ trên xuống kiểu Clash of Clans (2.5D); logic chạy trên mặt phẳng 2D.

## 2. Chủ tướng
- ✅ (người dùng chốt 2026-10-17, thay luật "chọn phe" cũ) Khi bắt đầu chơi game, người chơi **chọn 1 trong 4 chủ tướng** – **Cung thủ**, **Kiếm sĩ**, **Mưu sĩ**, **Thống soái**. Chủ tướng là **nhân vật cố định** của người chơi suốt game.
- ✅ **Không còn chọn phe.** Các tướng còn lại người chơi **tự xây dựng đội hình** theo ý thích trong quá trình chơi, để lối chơi đa dạng.
- ✅ Bối cảnh **Đông Á cổ trang**. Mỗi chủ tướng có bộ kỹ năng riêng gồm **kỹ năng 1 (nội tại) + kỹ năng 2, 3 (tự dùng) + tuyệt kỹ**; thiết kế nháp (🟡) ở `docs/LordHeroes.md`. Cách dùng kỹ năng: mục 3 "Kỹ năng trên thẻ".
- ✅ Chủ tướng **không có tên riêng**: mang **tên người chơi đặt khi đăng nhập** (chỉ phân biệt theo class).
- ⏳ **Trang phục (skin)** cho chủ tướng: có, nhưng để sau (dùng chung bộ khung bộ phận để không phải làm lại animation).
- ⏳ Màn chọn chủ tướng, kỹ năng chủ tướng (code): chưa làm ở prototype. `FactionDefinition` tối thiểu vẫn giữ trong dữ liệu nhưng chưa dùng (có thể bỏ sau).
- 🟡 Prototype dùng **một chủ tướng placeholder** (`Hero_Lord`).

## 3. Thẻ tướng
- ✅ Thẻ **chỉ là giao diện**. Dùng thẻ để triệu hồi; nhân vật tướng kèm đội lính xuất hiện trên chiến trường. Thẻ không có logic chiến đấu, dữ liệu lấy từ `HeroDefinition`.
- ✅ Khi vào trận, người chơi luôn nhận **thẻ chủ tướng** mà mình đã chọn. Các thẻ tướng khác đến từ thẻ người chơi sở hữu (kiếm qua nhiệm vụ, sự kiện, cửa hàng – ⏳ để sau).
- ✅ Chủ tướng **tính vào giới hạn 5 tướng**.
- 🟡 Chủ tướng **không có sẵn trên sân**; được triệu hồi bằng thẻ như các tướng khác.
- 🟡 Prototype: trận cấp sẵn bộ thẻ thử nghiệm gồm **1 thẻ chủ tướng + 4 thẻ tướng placeholder** (cấu hình trong `StageConfig` hoặc một `TestLoadout`), để kiểm tra được giới hạn 5 tướng.
- ✅ Sau khi tướng đã ra sân, **chạm thẻ = chọn tướng đó** (không triệu hồi lần nữa). *(Luật hiện tại của prototype; được thay bằng mục "Kỹ năng trên thẻ" bên dưới khi làm kỹ năng.)*

### Kỹ năng trên thẻ (người dùng chốt 2026-10-17 · ⏳ code làm cùng kỹ năng)
- ✅ **Mọi tướng (chủ tướng và tướng thường) có cùng cấu trúc 4 kỹ năng**: **kỹ năng 1 = nội tại** (bị động, luôn có tác dụng) · **kỹ năng 2 và 3 = kỹ năng phụ** (tự dùng) · **tuyệt kỹ** (thanh nộ, dùng bằng thẻ). Thiết kế cụ thể của chủ tướng: `docs/LordHeroes.md`; tướng thường: chờ thiết kế.
- ✅ **Kỹ năng 2 và 3 tự dùng** ngay khi hồi chiêu xong; nếu cả hai cùng sẵn sàng thì **dùng kỹ năng 2 trước, rồi đến kỹ năng 3**. **Tự nhắm** (🟡 thứ tự: mục tiêu tướng đang đánh → cụm quân địch đông nhất trong tầm → thành địch; không có mục tiêu trong tầm thì chờ, không phí chiêu).
- ✅ **Tuyệt kỹ dùng thanh nộ, không có thời gian hồi**: thanh nộ **tăng mỗi lần tướng gây sát thương**; đầy thì dùng được.
  - 🟡 (người dùng xác nhận tạm thời) **Chỉ tính sát thương do chính tướng gây ra** (không tính đội lính, không tính khi bị đánh); lượng nộ tỉ lệ với sát thương, mỗi tướng cấu hình `rageMax` và tốc độ tích (`HeroDefinition`). Thanh nộ không tự giảm.
  - ✅ Dùng tuyệt kỹ xong thanh nộ **về 0**; tướng chết thì **mất nộ**; **không** mang sang trận sau.
- ✅ **Thẻ của tướng đã ra sân hiển thị**: icon của **kỹ năng 2 và 3** kèm **đếm ngược hồi chiêu**, và **thanh nộ** của tuyệt kỹ (sáng lên khi đầy).
- ✅ **Thao tác trên thẻ của tướng đã ra sân**:
  - **Chạm** thẻ = dùng **tuyệt kỹ**, tự nhắm (khi thanh nộ đầy). ✅ **Chưa đầy nộ** thì hiển thị thông báo **"kỹ năng chưa thể sử dụng"** (không chọn tướng, không làm gì khác).
  - **Kéo** thẻ ra chiến trường = dùng tuyệt kỹ và **tự chọn điểm / hướng** (thả trên UI = huỷ; 🟡 chỉ khi nộ đầy).
  - **Giữ** thẻ **1.5 giây** = **chọn** tướng đó (đổi chọn tướng nhanh).
  - Thẻ chưa dùng (chạm = triệu hồi nhanh, kéo = triệu hồi tự do) và thẻ tướng đã chết: không đổi.
- 🟡 Tướng đã chết: thẻ **xám lại**, không chọn và không dùng lại được.

## 4. Triệu hồi
- ✅ Tối đa **5 tướng đã triệu hồi** mỗi trận. Tướng chết **không** trả lại lượt.
- 🟡 Triệu hồi lại tướng đã chết: **không cho phép** (cờ cấu hình `allowResummonDeadHero = false`).
- ✅ Hai cách triệu hồi, dùng được ở **cả ải Công và ải Thủ**:
  - **Triệu hồi nhanh**: chạm thẻ chưa dùng → tướng xuất hiện tại **điểm triệu hồi đặt sẵn** của ải (`quickSummonPoints`). Nếu có nhiều điểm, 🟡 chọn điểm còn trống gần thành ta nhất.
  - **Triệu hồi tự do**: kéo thẻ thả vào chiến trường → tướng xuất hiện ở vị trí đó, chỉ hợp lệ trong **vùng triệu hồi trước thành ta** (`summonZone` của ải). Hiển thị vùng hợp lệ khi đang kéo; thả ngoài vùng thì hủy, không mất thẻ.
- 🟡 Triệu hồi không tốn tài nguyên và không có thời gian hồi (giữ tham số `summonCost`, `summonCooldown` = 0 để thêm sau).
- 🟡 Sau khi xuất hiện, tướng ở trạng thái **Idle** và áp dụng luật chờ 5 giây ở mục 6: không có mục tiêu và không có lệnh trong 5 giây thì tự tiến lên.

## 5. Điều khiển ✅
- Chỉ **một tướng được chọn** tại một thời điểm; tướng được chọn có vòng sáng.
- **Chọn tướng**: chạm trực tiếp vào tướng, chạm thẻ của tướng đã ra sân (⏳ khi có kỹ năng: **giữ thẻ 1.5 giây**, mục 3 "Kỹ năng trên thẻ"), hoặc chạm icon tướng ở mép màn hình (mục 9).
- **Lệnh di chuyển (`MoveCommand`)**: khi đã chọn tướng, chạm vào mặt đất hợp lệ.
- 🟡 Khi đã chọn tướng, **chạm vào quân địch hoặc thành địch** = `AttackCommand` tới mục tiêu đó (cùng tác dụng với vuốt từ tướng).
- **Lệnh tấn công (`AttackCommand`)**: **vuốt bắt đầu từ tướng** tới thành địch hoặc một đơn vị địch → tướng (và đội lính) tiến tới đánh mục tiêu đó. 🟡 Vuốt bắt đầu từ thẻ của tướng đã ra sân cũng được tính *(⏳ khi có kỹ năng: kéo thẻ dùng để nhắm tuyệt kỹ, luật này bỏ)*.
- Lệnh mới **thay thế** lệnh cũ.
- Không phát lệnh khi ngón tay/con trỏ đang ở trên UI.
- Không nhận lệnh tới vị trí ngoài chiến trường hoặc vùng cấm.
- 🟡 Phân biệt thao tác: vuốt **bắt đầu trên tướng** = lệnh tấn công; kéo **bắt đầu trên mặt đất trống** = kéo camera; hai ngón = zoom; chạm nhanh (không kéo) = chọn/di chuyển.

## 6. Hành vi tướng

### Trạng thái
`Idle`, `Moving`, `Attacking`, `Advancing`, `Combat`, `Dead`.

| Trạng thái | Ý nghĩa |
|---|---|
| Moving | Đang thực hiện `MoveCommand` |
| Attacking | Đang thực hiện `AttackCommand` tới một mục tiêu cụ thể |
| Advancing | Tự tiến lên đánh khi không có lệnh |
| Combat | Tự vệ tạm thời khi bị đánh trong lúc Moving |
| Idle | Đứng chờ tại chỗ (sau khi tới đích, sau khi triệu hồi, hoặc sau khi đánh xong); tối đa `idleBeforeAdvanceSeconds` rồi chuyển Advancing |
| Dead | Kết thúc, hủy mọi lệnh, không chọn lại được |

### Luật IgnoreUntilDestination (khi Moving) ✅
1. Tướng đi tới điểm đích dù thấy địch, không đổi hướng, không đuổi theo địch.
2. **Bị tấn công** = vừa **nhận sát thương**. 🟡 "Vừa" = trong `attackerMemorySeconds` (mặc định 2 giây, trong `CombatStats`) tính từ đòn gần nhất của kẻ đó; quá thời gian mà không bị đánh tiếp thì kẻ đó không còn là mối đe dọa.
3. Chỉ tự vệ khi bị tấn công **và** kẻ tấn công ở **gần** (khoảng cách ≤ `reactionRange` của tướng, 🟡 mặc định xấp xỉ tầm đánh cận chiến; prototype dùng `min(tầm đánh, 0.7)` và luôn giữ `reactionRange ≤ tầm đánh`, vì tự vệ đánh tại chỗ nên kẻ tấn công ở gần phải nằm trong tầm với). Áp dụng như nhau cho địch cận chiến và địch tầm xa đứng quá gần.
4. Bị cung thủ bắn **từ xa** (ngoài `reactionRange`) thì **vẫn đi tiếp** theo lệnh.
5. Tự vệ đánh tại chỗ; hết mối đe dọa gần thì tiếp tục đi tới điểm đích đã lưu.
6. Tới đích → chuyển sang **Idle** và áp dụng luật chờ 5 giây bên dưới.

### Idle và luật chờ 5 giây ✅
- Tướng đứng tại chỗ, **đánh địch đi vào tầm đánh** nhưng **không đuổi theo**.
- 🟡 "Địch trong tầm" gồm cả **thành địch**, nhưng **quân địch được ưu tiên**: chỉ đánh thành khi trong tầm không còn quân nào. Áp dụng cho cả lính khi giữ đội hình / giao chiến tự do.
- Nếu **trong 5 giây liên tục không có mục tiêu** (không có địch trong tầm) và không có lệnh mới → chuyển sang **Advancing**.
- Mỗi khi có mục tiêu hoặc có giao chiến, bộ đếm 5 giây **đặt lại từ đầu** và chỉ chạy tiếp khi không còn mục tiêu.
- 🟡 "Có mục tiêu / có giao chiến" = có địch **trong tầm đánh** hoặc tướng **đang ra đòn**. Nhận sát thương từ địch **ngoài tầm đánh** (ví dụ cung thủ bắn từ xa) **không** đặt lại bộ đếm, để tướng không bị giữ đứng yên mãi; sau 5 giây tướng chuyển Advancing và tìm mục tiêu trong `aggroRange`. Cờ cấu hình `idleTimerResetsOnOutOfRangeDamage = false`.
- Có lệnh mới (di chuyển/tấn công) thì hủy bộ đếm.
- 5 giây là tham số cấu hình `idleBeforeAdvanceSeconds = 5` (trong `HeroDefinition`, 🟡 có thể ghi đè theo ải).

### Advancing ✅ (chi tiết 🟡)
- Tướng tự tiến lên và đánh như AI thường: tìm mục tiêu địch hợp lệ trong `aggroRange`, đánh tới khi mục tiêu chết, rồi tiếp tục tiến.
- 🟡 Hướng tiến khi không thấy địch: ải **Công** → về phía **thành địch**; ải **Thủ** → về phía **đơn vị địch gần nhất**, nếu không có thì đứng tại chỗ (Idle).

### Attacking ✅
- Đi tới mục tiêu được chỉ định và đánh tới khi mục tiêu chết hoặc có lệnh mới.
- 🟡 ~~Trên đường tới mục tiêu áp dụng luật tự vệ giống Moving~~ → **Lệnh được ưu tiên** (người dùng chốt 2026-10-16): khi đang thực hiện `AttackCommand` (thành hay quân), tướng và lính bám đúng mục tiêu, **không tự vệ** khi bị đánh ở gần. Vẫn là tùy chọn `selfDefendWhileAttacking` (mặc định `false`) trong `HeroDefinition`. Lính vẫn áp dụng luật "không với tới" ở mục 7.
- Mục tiêu chết/bị hủy → chuyển sang **Idle** (áp dụng luật chờ 5 giây).

### Chung ✅
- Mỗi đòn đánh tuân theo tầm đánh, sát thương, thời gian hồi đòn.
- Không gây sát thương khi đã chết hoặc trận đã kết thúc.

## 7. Đội lính
- ✅ Mỗi tướng có đội lính riêng; số lính lấy từ `HeroDefinition` (`baseSquadSize`, `maxSquadSize`), cấu hình được để nâng cấp sau.
- ✅ Lính **luôn theo lệnh của tướng mình**:
  - Tướng Moving → lính đi theo đội hình, cũng **không đuổi địch**; 🟡 lính tự vệ theo cùng luật `reactionRange`.
  - Tướng Attacking → lính cùng đánh mục tiêu được chỉ định; 🟡 nếu không với tới thì đánh địch gần mục tiêu nhất.
    - 🟡 "Không với tới" = lính đang đuổi mục tiêu mà khoảng cách không giảm trong `troopUnreachableSeconds` (mặc định 1.5 giây), ví dụ mục tiêu bị vây kín. Khi đó lính đánh địch gần mục tiêu nhất trong bán kính `troopAssistRadius` (mặc định 3) quanh mục tiêu; địch đó chết thì quay lại thử mục tiêu chính. Không có địch nào trong bán kính thì vẫn bám mục tiêu chính. Hai tham số nằm trong `HeroDefinition`.
  - Tướng Advancing/Combat → lính tự tìm và đánh địch, nhưng không rời tướng quá `leashRadius` 🟡; vượt quá thì quay về đội hình.
- ✅ Lính không bao giờ bị gán nhầm sang đội tướng khác.
- 🟡 Tướng chết: lính **tiếp tục chiến đấu ở chế độ Advancing** cho tới khi chết (cấu hình theo ải `onHeroDeathTroopBehavior`: `KeepFighting` / `Retreat` / `Disband`).

## 8. Ải và điều kiện thắng/thua

### Ải Công ✅
- Quân địch **phòng thủ tập trung quanh thành địch**.
- 🟡 Quân phòng thủ đứng tại vị trí, lao ra đánh khi có đơn vị ta vào `guardRadius`, rồi quay về vị trí cũ.
- Thắng: **phá được thành địch**.
- ✅ Thua: **hết giờ** (`timeLimit`, 🟡 mặc định 180 giây).
- 🟡 Thua thêm khi: mọi tướng đã triệu hồi đều chết và không còn thẻ để triệu hồi.

### Ải Thủ ✅
- Quân địch **tấn công lên** theo từng đợt (`waves` trong `StageConfig`).
- Thắng: **trụ được hết thời gian / hết các đợt tấn công**.
- 🟡 Thua: **thành ta bị phá**.
- 🟡 Thành ta có tấn công quân địch trong tầm hay không: cấu hình được, mặc định **có**.

### Chấm sao ✅
- Tối đa **3 sao**, tính theo **số tướng đã mất** trong trận. Thua = 0 sao.
  - Thắng, không mất tướng nào → 3 sao.
  - Thắng, mất 1 tướng → 2 sao.
  - Thắng, mất từ 2 tướng → 1 sao.
- 🟡 Ngưỡng sao ở trên là mặc định, nằm trong `StageConfig` để chỉnh từng ải.

## 9. Bản đồ và camera
Thông số chi tiết để vẽ bản đồ (kích thước, bố cục, vùng cấm, quy cách art): `docs/MapSpecification.md` (🟡 đề xuất, chờ duyệt).

- ✅ Bản đồ là **bãi trống**, không chướng ngại, không cần tìm đường (pathfinding). Có biên giới hạn và có thể có vùng cấm.
- 🟡 Vùng cấm **chặn di chuyển**: unit không đi xuyên qua; nếu đường thẳng bị chặn thì đi theo đường ngắn nhất vòng qua các vùng cấm (đồ thị điểm nút dựng sẵn quanh chúng). Đây là né vài vật cản cố định, không phải tìm đường tổng quát trên lưới.
- 🟡 Có hai loại vùng cấm (người dùng chốt 2026-10-16): **hình chữ nhật** và **sông**. Sông = đường tâm uốn khúc + bề rộng, chỉ qua được ở **chỗ qua sông (cầu)**; một dòng sông có thể cắt ngang cả bản đồ, miễn là còn cầu để đi. Bản đồ chuẩn: một con sông cắt ngang giữa bản đồ từ mép trên xuống mép dưới, có 2 cầu → 2 làn đường. Thông số chi tiết ở `docs/MapSpecification.md`.
- ✅ Góc nhìn kiểu Clash of Clans; bản đồ **rộng hơn màn hình**.
- 🟡 Góc nghiêng 2.5D **điều chỉnh được từ 0° (nhìn thẳng từ trên xuống) tới 45°** bằng thanh trượt trong HUD (và `viewTiltDegrees` trên `BattleController`); **mặc định 0°** (người dùng chốt 2026-10-16). Chỉ phần hiển thị đổi (mặt đất nén theo `cos(góc)`, unit vẫn đứng thẳng); luật trận chạy trên mặt phẳng logic không đổi.
- ✅ (cập nhật 2026-10-17) Art thật sẽ **vẽ sẵn ở góc nghiêng 3/4 cố định** (không phải isometric hình thoi; trục bản đồ giữ nguyên). Khi có art: góc mặc định = góc của art (🟡 tỉ lệ nén 0.75 ≈ 41.4°), thanh "Góc nhìn" **chỉ giữ để debug** (Editor / bản Development), bản phát hành khoá góc. Chi tiết: `docs/MapSpecification.md` mục 8.
- ✅ Camera **kéo theo** (🟡: kéo theo tướng đang được chọn khi tướng di chuyển; người chơi kéo tay thì tạm ngưng theo cho tới khi chọn lại tướng).
- 🟡 Người chơi kéo màn hình để di chuyển camera, hai ngón (hoặc con lăn chuột) để zoom; giới hạn camera trong biên bản đồ.
- ✅ Tướng nằm ngoài màn hình → hiện **icon tướng ở mép màn hình** theo hướng của tướng đó. Chạm icon → chọn tướng và 🟡 đưa camera tới tướng.
- ✅ Ngoài chạm trực tiếp vào tướng, có thể chọn tướng bằng **thẻ**.

## 10. Dữ liệu
- `FactionDefinition` ⏳ (tối thiểu): id, tên, màu.
- `HeroDefinition`: heroId, tên, sprite/placeholder, máu, sát thương, tốc độ di chuyển, tầm đánh, **tốc đánh** (đòn/giây; hiện lưu dưới dạng `attackInterval` = 1 / tốc đánh), `reactionRange`, `aggroRange`, `idleBeforeAdvanceSeconds` (mặc định 5), `baseSquadSize`, `maxSquadSize`, `TroopDefinition` của đội lính, `leashRadius`.
- `TroopDefinition`: máu, sát thương, tốc độ, tầm đánh, **tốc đánh** (đòn/giây; hiện lưu dưới dạng `attackInterval` = 1 / tốc đánh), `reactionRange`, `aggroRange`.
- `StageConfig`: id ải, loại (`Attack` / `Defense`), kích thước và biên bản đồ, vùng cấm, thành ta và thành địch (máu, có bắn hay không), `summonZone`, `quickSummonPoints`, bố trí quân phòng thủ (ải Công), các đợt quân (ải Thủ), `timeLimit`, luật thua, ngưỡng sao, bộ thẻ thử nghiệm, `onHeroDeathTroopBehavior`, `allowResummonDeadHero`.
- `BattleRuntimeState` (tạo mới mỗi trận): số tướng đã triệu hồi, tướng đang chọn, trạng thái và máu từng tướng/lính/thành, lệnh hiện tại và điểm đích đã lưu của từng tướng, đồng hồ, đợt hiện tại, kết quả và số sao.

## 11. Ngoài phạm vi prototype ⏳
Màn chọn chủ tướng, kỹ năng chủ tướng, kiếm thẻ (nhiệm vụ, sự kiện, cửa hàng), nâng cấp, tài nguyên triệu hồi, PvP, tài khoản, backend, lưu tiến trình, animation và hiệu ứng thật.

## 12. Tiêu chí nghiệm thu prototype
- [ ] Mở scene Battle, chạy không lỗi compile.
- [ ] Triệu hồi nhanh (chạm thẻ) và triệu hồi tự do (kéo thả trong vùng hợp lệ); thả ngoài vùng bị hủy, không mất thẻ.
- [ ] Tối đa 5 tướng, tướng thứ 6 bị từ chối; tướng chết thì thẻ xám, không dùng lại.
- [ ] Chọn tướng bằng chạm tướng, chạm thẻ, chạm icon mép màn hình; chuột và cảm ứng đều chạy.
- [ ] Lệnh di chuyển và thay điểm đích; không phát lệnh khi chạm vào UI.
- [ ] Đang di chuyển: bị bắn từ xa vẫn đi; bị đánh ở gần thì tự vệ rồi đi tiếp.
- [ ] Tới đích: đứng chờ, đánh địch vào tầm; 5 giây liên tục không có mục tiêu → tự tiến lên đánh. Có giao chiến thì bộ đếm đặt lại.
- [ ] Vuốt từ tướng tới thành/đơn vị địch → tướng và lính đánh đúng mục tiêu.
- [ ] Lính theo đúng tướng, không vượt `leashRadius`, xử lý đúng khi tướng chết.
- [ ] Ải Công: phá thành địch thắng; hết giờ thua. Ải Thủ: trụ hết giờ thắng; mất thành thua.
- [ ] Số sao đúng theo số tướng mất.
- [ ] Camera kéo/zoom/theo tướng trong biên bản đồ; icon tướng ngoài màn hình hiện đúng hướng.
- [ ] Chơi lại không giữ trạng thái trận trước.

## 13. Câu hỏi còn mở
- Không có câu hỏi chặn prototype. Các mục 🟡 là mặc định tạm, người dùng có thể đổi bất cứ lúc nào bằng cách sửa file này.
- ⏳ **Cân bằng độ khó** (thông số tướng/lính/quân địch, đợt quân, thời gian, bề rộng và số cầu): để tới **giai đoạn sắp phát hành** (người dùng chốt 2026-10-16). Đã biết: sau khi có sông, ải Thủ suýt thua (mất cả 5 tướng, thành còn 173/3000) và ải Công dễ hơn (3 sao, không mất tướng) – xem test `Balance_*`.
