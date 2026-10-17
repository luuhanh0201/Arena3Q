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
- ✅ Bối cảnh **Đông Á cổ trang**. Mỗi chủ tướng có bộ kỹ năng riêng gồm **kỹ năng 1 (nội tại) + kỹ năng 2, 3 (tự dùng) + tuyệt kỹ**; thiết kế nháp (🟡) ở `docs/Tuong/LordHeroes.md`. Cách dùng kỹ năng: mục 3 "Kỹ năng trên thẻ".
- ✅ Chủ tướng **không có tên riêng**: mang **tên người chơi** (chỉ phân biệt theo class). ✅ (người dùng chốt 2026-10-17, thay "đặt khi đăng nhập") Tên được đặt **ngay sau khi chọn chủ tướng** trong lần vào game đầu tiên, **mỗi máy chủ một tên**.
- ⏳ **Trang phục (skin)** cho chủ tướng: có, nhưng để sau (dùng chung bộ khung bộ phận để không phải làm lại animation).
- ✅ (người dùng chốt 2026-10-17) Chủ tướng **bắt đầu chưa có kỹ năng**: lên cấp thì học dần (🟡 nội tại Lv2, kỹ năng 2 Lv5, kỹ năng 3 Lv10, tuyệt kỹ Lv15), rồi nâng cấp từng kỹ năng bằng **điểm kỹ năng nhận khi lên cấp**. Chi tiết: `docs/KeHoach/HeroProgression.md` §6.
- ✅ (2026-10-17) Luồng vào game: intro → đăng nhập (Google, tài khoản + mật khẩu, **Chơi ngay** không cần đăng nhập) → chọn máy chủ → chọn chủ tướng → cốt truyện mở đầu. Kế hoạch màn hình và prompt: `docs/KeHoach/VaoGame.md` (🟡).
  - **Đã có trong code (bản thử, hình tạm):** scene `Frontend` chạy đủ các màn theo thứ tự trong `FrontendConfig`; người chơi cũ bỏ qua chọn tướng / đặt tên / cốt truyện; ải hướng dẫn với Giáp Sĩ 3★, thắng thì chủ tướng lên Lv2; hồ sơ lưu trên máy. ⏳ Chưa có: đăng nhập Google / Apple / tài khoản (cần backend – nút báo "chưa hỗ trợ"), danh sách máy chủ thật, video intro và hình (dùng hình tạm cho tới khi có art).
- Màn chọn chủ tướng, cấp / sao / phe và kỹ năng: **đã có trong code** (bản thử, hình tạm – 2026-10-17). `FactionDefinition` dùng cho **phe của danh tướng** (mục "Cấp, sao và phe" dưới đây).
- 🟡 Prototype dùng **một chủ tướng placeholder** (`Hero_Lord`).

### Cấp, sao và phe (người dùng chốt 2026-10-17 · đã có trong code 2026-10-17)
Thiết kế và bảng chỉ số (🟡): `docs/KeHoach/HeroProgression.md`.
- ✅ **Cấp tối đa 60** cho mọi tướng. Chỉ số tăng theo cấp **ưu tiên điểm mạnh của hệ** (vai trò) và mọi hệ phải cân bằng.
- ✅ Ba loại tướng: **chủ tướng** (học kỹ năng theo cấp); **tướng khởi đầu** – 4 tướng thường hiện có (`docs/Tuong/NormalHeroes.md`), **không phe**, chỉ số gốc thấp hơn danh tướng, dễ tăng sao; **danh tướng** nhận sau này (Lữ Bố…).
- ✅ Tướng khởi đầu và danh tướng **mở kỹ năng bằng sao** (gom mảnh tướng): 1★ nội tại + kỹ năng 2 · 2★ kỹ năng 3 · 3★ tuyệt kỹ · 4–5★ tăng cấp kỹ năng.
- ✅ Danh tướng thuộc một phe: **Thục, Ngụy, Ngô** hoặc **Quần** (Lữ Bố, Đổng Trác, Viên Thiệu…). Tác dụng của phe = **cộng hưởng**: đội hình có 2 / 3 / 4 tướng cùng phe thì cả quân được cộng chỉ số theo điểm mạnh của phe (🟡 giá trị). Người chơi vẫn **không chọn phe** (luật ở trên giữ nguyên); chủ tướng và tướng khởi đầu không thuộc phe nào.

## 3. Thẻ tướng
- ✅ Thẻ **chỉ là giao diện**. Dùng thẻ để triệu hồi; nhân vật tướng kèm đội lính xuất hiện trên chiến trường. Thẻ không có logic chiến đấu, dữ liệu lấy từ `HeroDefinition`.
- ✅ Khi vào trận, người chơi luôn nhận **thẻ chủ tướng** mà mình đã chọn. Các thẻ tướng khác đến từ thẻ người chơi sở hữu (kiếm qua nhiệm vụ, sự kiện, cửa hàng – ⏳ để sau).
- ✅ Chủ tướng **tính vào giới hạn 5 tướng**.
- 🟡 Chủ tướng **không có sẵn trên sân**; được triệu hồi bằng thẻ như các tướng khác.
- 🟡 Prototype: trận cấp sẵn bộ thẻ thử nghiệm gồm **1 thẻ chủ tướng + 4 thẻ tướng placeholder** (cấu hình trong `StageConfig` hoặc một `TestLoadout`), để kiểm tra được giới hạn 5 tướng.
- ~~Sau khi tướng đã ra sân, chạm thẻ = chọn tướng đó~~ → thay bằng mục "Kỹ năng trên thẻ" bên dưới (đã có trong code 2026-10-17): chạm = tuyệt kỹ, **giữ 1.5 giây = chọn tướng**.

### Kỹ năng trên thẻ (người dùng chốt 2026-10-17 · đã có trong code 2026-10-17)
- ✅ **Mọi tướng (chủ tướng và tướng thường) có cùng cấu trúc 4 kỹ năng**: **kỹ năng 1 = nội tại** (bị động, luôn có tác dụng) · **kỹ năng 2 và 3 = kỹ năng phụ** (tự dùng) · **tuyệt kỹ** (thanh nộ, dùng bằng thẻ). Thiết kế cụ thể của chủ tướng: `docs/Tuong/LordHeroes.md`; tướng thường: `docs/Tuong/NormalHeroes.md` (🟡 bản nháp chờ duyệt).
- ✅ **Kỹ năng 2 và 3 tự dùng** ngay khi hồi chiêu xong; nếu cả hai cùng sẵn sàng thì **dùng kỹ năng 2 trước, rồi đến kỹ năng 3**. **Tự nhắm** (🟡 thứ tự: mục tiêu tướng đang đánh → cụm quân địch đông nhất trong tầm → thành địch; không có mục tiêu trong tầm thì chờ, không phí chiêu).
- ✅ **Tuyệt kỹ dùng thanh nộ, không có thời gian hồi**: thanh nộ **tăng mỗi lần tướng gây sát thương**; đầy thì dùng được.
  - 🟡 (người dùng xác nhận tạm thời) **Chỉ tính sát thương do chính tướng gây ra** (không tính đội lính, không tính khi bị đánh); lượng nộ tỉ lệ với sát thương, mỗi tướng cấu hình `rageMax` và tốc độ tích (`HeroDefinition`). Thanh nộ không tự giảm.
  - ✅ Dùng tuyệt kỹ xong thanh nộ **về 0**; tướng chết thì **mất nộ**; **không** mang sang trận sau.
- ✅ **Thẻ của tướng đã ra sân hiển thị**: icon của **kỹ năng 2 và 3** kèm **đếm ngược hồi chiêu**, và **thanh nộ** của tuyệt kỹ (sáng lên khi đầy).
  - 🟡 Kỹ năng **chưa học** (chủ tướng chưa đủ cấp, tướng chưa đủ sao): icon hiện **ổ khoá**; chưa có tuyệt kỹ thì **không hiện thanh nộ** và không tích nộ; chạm thẻ hiện cùng thông báo "kỹ năng chưa thể sử dụng".
- ✅ **Thao tác trên thẻ của tướng đã ra sân**:
  - **Chạm** thẻ = dùng **tuyệt kỹ**, tự nhắm (khi thanh nộ đầy). ✅ **Chưa đầy nộ** thì hiển thị thông báo **"kỹ năng chưa thể sử dụng"** (không chọn tướng, không làm gì khác).
  - **Kéo** thẻ ra chiến trường = dùng tuyệt kỹ và **tự chọn điểm / hướng** (thả trên UI = huỷ; 🟡 chỉ khi nộ đầy).
  - **Giữ** thẻ **1.5 giây** = **chọn** tướng đó (đổi chọn tướng nhanh).
  - Thẻ chưa dùng (chạm = triệu hồi nhanh, kéo = triệu hồi tự do) và thẻ tướng đã chết: không đổi.
- 🟡 Tướng đã chết: thẻ **xám lại**, không chọn và không dùng lại được.
- 🟡 **Vận chiêu**: kỹ năng có hiệu lực ở mốc phát chiêu trong animation (`Cast`, `docs/Tuong/LordAnimations.md`); trong lúc vận chiêu tướng đứng yên, xong thì về lệnh / trạng thái cũ. Bị khống chế hoặc chết **trước** mốc phát chiêu → kỹ năng không bị tính (không vào hồi chiêu, không mất nộ).
- ✅ (người dùng chốt 2026-10-17, mục 13 xung đột 3) Kỹ năng làm **tướng dịch chuyển** (lướt, xung phong – ví dụ Phi Ảnh Đột Kích) **không tự dùng khi tướng đang Moving**, chờ tới đích để giữ ✅ IgnoreUntilDestination; kỹ năng đứng tại chỗ vẫn tự dùng khi đang đi. Tuyệt kỹ do người chơi bấm thì không bị chặn. Dữ liệu: cờ `autoCastWhileMoving` của từng kỹ năng (mặc định tắt).
- 🟡 Kỹ năng của ta **đẩy / hất văng / kéo** quân địch (Đông Phong, Thiết Kỵ Xung Phong…) dùng cùng luật với khống chế của địch ở mục 8: tuân theo vùng cấm và biên bản đồ.
- 🟡 Gợi ý `rageMax`: thanh nộ đầy sau khoảng **15–20 đòn đánh thường** trúng mục tiêu (cân bằng sau, mục 13).
- **Đã có trong code (2026-10-17, hình tạm):** 32 kỹ năng của 4 chủ tướng + 4 tướng khởi đầu là dữ liệu (`SkillDefinition` ghép từ khối: cách nhắm, vùng, thời gian, lặp, dịch chuyển, danh sách hiệu ứng / nội tại) – thêm hay chỉnh kỹ năng không cần sửa code. Các chọn lựa 🟡 khi code (đổi được trong asset hoặc cấu hình):
  - `rageMax` 100, đầy sau **18** đòn bằng Công của tướng (`rageHitsToFill`); sát thương kỹ năng của tướng cũng tích nộ; vùng kỹ năng còn lại sau khi tướng chết vẫn gây sát thương nhưng không tích nộ.
  - Hiệu ứng cùng loại **không cộng dồn**: lấy giá trị mạnh nhất và thời gian dài nhất (độc: mỗi nguồn một lần). **Miễn khống chế** chặn và xoá choáng / làm chậm.
  - Cấp kỹ năng: **+5 %/cấp** cho sát thương, độc, khiên; % làm chậm, tăng sức, thời gian giữ nguyên. ⏳ Mốc 5 / 10 / 15 (`docs/KeHoach/HeroProgression.md` §6.1) chưa làm.
  - Tuyệt kỹ do người chơi dùng **huỷ kỹ năng 2/3 đang vận chưa tới mốc phát** (không tính chiêu đó). Trong lúc vận chiêu và các nhát lặp (Liên Châu Tiễn, Cuồng Đao Lục Trảm) tướng đứng yên.
  - Kỹ năng chưa học: icon tối kèm điều kiện mở (`Lv5`, `2★`) thay cho ổ khoá (chưa có art), hiện cả khi thẻ chưa triệu hồi.
  - Đơn giản hoá tạm: Xuyên Dương Tiễn tự nhắm theo thứ tự chung (chưa ưu tiên "địch nhiều máu nhất"); Đao Khí trúng tức thì (chưa có đạn bay 12 đv/s); Thiết Kỵ Xung Phong là đòn theo đường thẳng (chưa có đội kỵ binh chạy qua); +5 % máu toàn quân của Thống soái **giữ lại** khi Thống soái chết (không giảm máu giữa trận). Hình kỹ năng là vùng màu trên mặt đất cho tới khi có VFX.
  - `Hero_Lord` (chủ tướng placeholder) mượn bộ kỹ năng của Kiếm sĩ; tướng khởi đầu: `Hero_Archer` = Xạ Thủ, `Hero_Swordsman` = Đao Khách, `Hero_Cavalry` = Kỵ Binh, `Hero_Guardian` = Giáp Sĩ (tên hiển thị: mục 13 xung đột 4).

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
- **Chọn tướng**: chạm trực tiếp vào tướng, **giữ thẻ** của tướng đã ra sân **1.5 giây** (mục 3 "Kỹ năng trên thẻ"), hoặc chạm icon tướng ở mép màn hình (mục 9).
- **Lệnh di chuyển (`MoveCommand`)**: khi đã chọn tướng, chạm vào mặt đất hợp lệ.
- 🟡 Khi đã chọn tướng, **chạm vào quân địch hoặc thành địch** = `AttackCommand` tới mục tiêu đó (cùng tác dụng với vuốt từ tướng).
- **Lệnh tấn công (`AttackCommand`)**: **vuốt bắt đầu từ tướng** tới thành địch hoặc một đơn vị địch → tướng (và đội lính) tiến tới đánh mục tiêu đó. ~~Vuốt bắt đầu từ thẻ của tướng đã ra sân cũng được tính~~ – đã bỏ: kéo thẻ giờ dùng để nhắm tuyệt kỹ (mục 3).
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
- ✅ (người dùng chốt 2026-10-17) **Mọi tướng có thuộc tính "tốc đánh"** (số đòn mỗi giây; = 1 / thời gian giữa hai đòn). Tốc đánh quyết định **cả nhịp đánh lẫn tốc độ ra đòn**: mỗi đòn có **thời gian vung** trước khi trúng (🟡 = 40 % chu kỳ đòn), animation đánh **tua nhanh / chậm theo tốc đánh** để khung trúng đòn khớp với lúc gây sát thương (`docs/Tuong/LordAnimations.md` §0.2). ⏳ Phần "thời gian vung" làm cùng animation; prototype hiện gây sát thương ngay khi hết thời gian hồi đòn.
- 🟡 Đánh **tầm xa** (khi có animation): tới lúc trúng đòn thì sinh **đạn** bay tới mục tiêu, **sát thương khi đạn tới**; mục tiêu chết trước thì đạn tan. Tốc độ đạn cấu hình theo tướng / lính (gợi ý: mũi tên 15, luồng khí 10 đơn vị/giây).
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

### Quân địch: cấp bậc và kỹ năng (người dùng chốt 2026-10-17 · ⏳ code sau prototype)
Thiết kế từng loại địch theo chương (hình ảnh, chỉ số gợi ý, kỹ năng cụ thể): `docs/QuanDich/`.
- ✅ Ba cấp: **Thường** (không kỹ năng) · **Tinh anh** (1 kỹ năng, có dấu hiệu nhận biết – viền sáng) · **Boss** (2–3 kỹ năng, đổi pha, **thanh máu riêng trên HUD**). 🟡 Kích thước, bán kính thân và hệ số máu theo bảng `docs/QuanDich/README.md` §1.
- ✅ Kỹ năng địch **tự dùng khi hồi chiêu xong và có mục tiêu hợp lệ trong tầm** (không phí chiêu). Quân địch **không có thanh nộ**.
- ✅ **Cảnh báo trước**: kỹ năng đánh theo vùng (tròn / hình quạt / đường thẳng) hiện **vùng đỏ trên mặt đất** trong thời gian cảnh báo (🟡 1 giây, cấu hình theo kỹ năng) rồi mới có hiệu lực. Vùng **cố định vị trí** từ lúc bắt đầu cảnh báo: đơn vị ra khỏi vùng trước khi hết cảnh báo thì không trúng. Kỹ năng nhắm một mục tiêu (quăng xích, ném) có 🟡 0.5 giây ra tay báo hiệu.
- ✅ **Khống chế** lên quân ta (choáng, trói, kéo, hất văng, đứng yên): trong lúc bị khống chế đơn vị không di chuyển và không đánh, nhưng **không mất lệnh** của người chơi – hết khống chế thì tiếp tục lệnh / trạng thái cũ (Moving đi tiếp tới điểm đích, Attacking đánh tiếp mục tiêu; luật mục 6 giữ nguyên). Người chơi vẫn ra lệnh mới được trong lúc đó; lệnh có hiệu lực khi hết khống chế. Khống chế **không bao giờ đổi phe** đơn vị.
  - ✅ Kéo và hất văng **tuân theo vùng cấm và biên bản đồ**: không bị đẩy xuống sông hay ra ngoài; bị chặn thì dừng ở mép.
  - 🟡 Chống khoá cứng: một tướng bị khống chế liên tục quá 2 giây thì được miễn khống chế 2 giây.
- ✅ Kỹ năng **hỗ trợ** (hồi máu, tăng sức, khiên) chỉ tác dụng lên quân địch.
- ✅ Kỹ năng **triệu hồi** (gọi lâu la, bù nhìn rơm, phân thân): quân được gọi là quân địch bình thường; 🟡 mỗi kỹ năng tối đa 8 quân đang sống. Không đổi điều kiện thắng/thua.
- ✅ (người dùng chốt 2026-10-17) **Boss vẫn bị khống chế và dính hiệu ứng** từ kỹ năng của ta như mọi quân địch: choáng, làm chậm, đẩy lùi / hất văng (vẫn theo vùng cấm và biên bản đồ), độc, giảm sát thương, giảm tốc đánh – đủ giá trị và thời lượng, không giảm trừ. Animation `stunned` của boss là bắt buộc.
- ✅ **Boss đổi pha** một lần khi máu xuống dưới 🟡 50 %: có hiệu ứng nhận biết, thay/thêm kỹ năng và chỉ số theo dữ liệu.
- ✅ **Vị trí boss**: ải Công – đứng gác trước thành địch như quân phòng thủ (luật `guardRadius` ở trên); ải Thủ – xuất hiện ở **đợt cuối**. Điều kiện thắng/thua của ải **không đổi** (ải Công thắng khi phá thành dù boss còn sống; ải Thủ thắng khi trụ hết giờ / hết đợt).
- 🟡 Đặc tính bị động (không phải kỹ năng), ví dụ **sống lại một lần** khi chết (xác suất và % máu cấu hình được – Cương Thi).
- ⚠️ Chưa chốt vì xung đột với luật đã có: **khiêu khích** và **đơn vị bay** – xem mục 13.

## 9. Bản đồ và camera
Thông số chi tiết để vẽ bản đồ (kích thước, bố cục, vùng cấm, quy cách art): `docs/BanDo/MapSpecification.md` (🟡 đề xuất, chờ duyệt).

- ✅ Bản đồ là **bãi trống**, không chướng ngại, không cần tìm đường (pathfinding). Có biên giới hạn và có thể có vùng cấm.
- 🟡 Vùng cấm **chặn di chuyển**: unit không đi xuyên qua; nếu đường thẳng bị chặn thì đi theo đường ngắn nhất vòng qua các vùng cấm (đồ thị điểm nút dựng sẵn quanh chúng). Đây là né vài vật cản cố định, không phải tìm đường tổng quát trên lưới.
- 🟡 Có hai loại vùng cấm (người dùng chốt 2026-10-16): **hình chữ nhật** và **sông**. Sông = đường tâm uốn khúc + bề rộng, chỉ qua được ở **chỗ qua sông (cầu)**; một dòng sông có thể cắt ngang cả bản đồ, miễn là còn cầu để đi. Bản đồ chuẩn: một con sông cắt ngang giữa bản đồ từ mép trên xuống mép dưới, có 2 cầu → 2 làn đường. Thông số chi tiết ở `docs/BanDo/MapSpecification.md`.
- ✅ Góc nhìn kiểu Clash of Clans; bản đồ **rộng hơn màn hình**.
- 🟡 Góc nghiêng 2.5D **điều chỉnh được từ 0° (nhìn thẳng từ trên xuống) tới 45°** bằng thanh trượt trong HUD (và `viewTiltDegrees` trên `BattleController`); **mặc định 0°** (người dùng chốt 2026-10-16). Chỉ phần hiển thị đổi (mặt đất nén theo `cos(góc)`, unit vẫn đứng thẳng); luật trận chạy trên mặt phẳng logic không đổi.
- ✅ (cập nhật 2026-10-17) Art thật sẽ **vẽ sẵn ở góc nghiêng 3/4 cố định** (không phải isometric hình thoi; trục bản đồ giữ nguyên). Khi có art: góc mặc định = góc của art (🟡 tỉ lệ nén 0.75 ≈ 41.4°), thanh "Góc nhìn" **chỉ giữ để debug** (Editor / bản Development), bản phát hành khoá góc. Chi tiết: `docs/BanDo/MapSpecification.md` mục 8.
- ✅ Camera **kéo theo** (🟡: kéo theo tướng đang được chọn khi tướng di chuyển; người chơi kéo tay thì tạm ngưng theo cho tới khi chọn lại tướng).
- 🟡 Người chơi kéo màn hình để di chuyển camera, hai ngón (hoặc con lăn chuột) để zoom; giới hạn camera trong biên bản đồ.
- ✅ Tướng nằm ngoài màn hình → hiện **icon tướng ở mép màn hình** theo hướng của tướng đó. Chạm icon → chọn tướng và 🟡 đưa camera tới tướng.
- ✅ Ngoài chạm trực tiếp vào tướng, có thể chọn tướng bằng **thẻ**.

## 10. Dữ liệu
- `FactionDefinition` ⏳ (tối thiểu): id, tên, màu; dùng cho phe danh tướng (Thục, Ngụy, Ngô, Quần) và bảng cộng hưởng.
- Cấp / sao / phe (`docs/KeHoach/HeroProgression.md` §12) – **đã có trong code** (2026-10-17), mọi con số là 🟡 trong asset:
  - `HeroDefinition`: loại tướng (`Lord` / `Starter` / `Named`), phe (chỉ danh tướng), `GrowthProfile` (hệ tăng trưởng), giảm sát thương, chí mạng cộng thêm, Thống suất, hệ số vai trò.
  - `ProgressionConfig` (một asset): cấp tối đa, tăng trưởng của lính và quân địch, bảng sao, mốc mở kỹ năng chủ tướng, điểm kỹ năng và giới hạn, đột phá (tự động / cần mở), giới hạn giảm sát thương, chí mạng cơ bản, lực chiến, "tướng chuẩn" để kiểm tra cân bằng.
  - `FactionDefinition`: bảng cộng hưởng theo số tướng cùng phe. `StageConfig.enemyLevel`: cấp quân địch của ải.
  - Đội hình (`TestLoadout`, sau này hồ sơ người chơi): mỗi thẻ có **cấp** và **sao**. `CombatNumbersFactory` là nơi duy nhất tính chỉ số trận đấu; quy tắc thuần ở `ProgressionRules` (Core).
  - ⏳ Chưa có: hiệu ứng kỹ năng (nên mở kỹ năng theo cấp / sao mới chỉ là quy tắc), kinh nghiệm, mảnh tướng, lưu tiến trình.
- `HeroDefinition`: heroId, tên, sprite/placeholder, máu, sát thương, tốc độ di chuyển, tầm đánh, **tốc đánh** (đòn/giây; hiện lưu dưới dạng `attackInterval` = 1 / tốc đánh), `reactionRange`, `aggroRange`, `idleBeforeAdvanceSeconds` (mặc định 5), `baseSquadSize`, `maxSquadSize`, `TroopDefinition` của đội lính, `leashRadius`.
- `TroopDefinition`: máu, sát thương, tốc độ, tầm đánh, **tốc đánh** (đòn/giây; hiện lưu dưới dạng `attackInterval` = 1 / tốc đánh), `reactionRange`, `aggroRange`.
- `EnemyDefinition` ⏳ (hiện quân địch dùng `TroopDefinition`): các trường của `TroopDefinition` + cấp bậc (Thường / Tinh anh / Boss), kích thước và bán kính thân, danh sách kỹ năng, pha 2 (ngưỡng máu, kỹ năng và chỉ số thay đổi), đặc tính bị động (sống lại…), prefab hiển thị.
- `EnemySkillDefinition` ⏳: kiểu (vùng tròn / hình quạt / đường thẳng / một mục tiêu / hỗ trợ / triệu hồi), cách chọn chỗ đánh, kích thước vùng, số vùng, sát thương, khống chế + thời lượng, hồi chiêu, hồi chiêu đầu trận, thời gian cảnh báo, quân được gọi + giới hạn.
- `StageConfig`: id ải, loại (`Attack` / `Defense`), kích thước và biên bản đồ, vùng cấm, thành ta và thành địch (máu, có bắn hay không), `summonZone`, `quickSummonPoints`, bố trí quân phòng thủ (ải Công), các đợt quân (ải Thủ), `timeLimit`, luật thua, ngưỡng sao, bộ thẻ thử nghiệm, `onHeroDeathTroopBehavior`, `allowResummonDeadHero`.
- `BattleRuntimeState` (tạo mới mỗi trận): số tướng đã triệu hồi, tướng đang chọn, trạng thái và máu từng tướng/lính/thành, lệnh hiện tại và điểm đích đã lưu của từng tướng, đồng hồ, đợt hiện tại, kết quả và số sao.

## 11. Ngoài phạm vi prototype ⏳
Màn chọn chủ tướng, kỹ năng chủ tướng, kỹ năng và cấp bậc quân địch (tinh anh, boss), kiếm thẻ (nhiệm vụ, sự kiện, cửa hàng), kinh nghiệm, mảnh tướng và giao diện nâng cấp, tài nguyên triệu hồi, PvP, đăng nhập thật (Google / Apple / tài khoản), backend, lưu tiến trình trên máy chủ, sảnh chính và bản đồ chương, animation và hiệu ứng thật. *(Đã làm ở bản thử: chỉ số theo cấp / sao / phe, luồng vào game với "Chơi ngay" và hồ sơ trên máy – mục 2.)*

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
- ⚠️ **Xung đột cần chốt (2026-10-17):**
  1. **Khiêu khích** (Hộ Pháp Đồng Giáp, `docs/QuanDich/Chuong2_HoangCan.md`: "buộc quân ta trong bán kính 2.5 đánh mình") xung đột với ✅ **IgnoreUntilDestination** (Moving không đổi mục tiêu) và ✅ **Attacking** (đánh đúng mục tiêu được chỉ định). Đề xuất: chỉ tác dụng lên đơn vị **không đang thi hành lệnh** (Idle / Advancing / Combat và lính đang giao chiến tự do); tướng đang Moving / Attacking bỏ qua. Hoặc đổi kỹ năng thành chỉ giảm sát thương (bỏ khiêu khích).
  2. **Đơn vị bay** (Quỷ Hoả, `docs/QuanDich/Chuong3_YeuQuai.md`: "không bị sông chặn") xung đột với mục 9 🟡 vùng cấm **chặn mọi unit**. Đề xuất: thêm cờ `flying` – đơn vị bay qua được **sông** nhưng vẫn bị **vùng cấm chữ nhật** và biên bản đồ chặn; quân ta cận chiến vẫn đánh được khi nó ở trên bờ. Hoặc bỏ đặc tính bay.
  3. ~~Kỹ năng di chuyển tướng tự dùng khi đang Moving~~ – **đã chốt 2026-10-17** theo đề xuất: kỹ năng làm tướng dịch chuyển không tự dùng khi tướng đang Moving (mục 3 "Kỹ năng trên thẻ").
  4. **Tên tướng thường trùng tên chủ tướng** (2026-10-17): asset `Hero_Archer` tên "Cung Thủ" và `Hero_Swordsman` tên "Kiếm Sĩ" trùng với hai class chủ tướng ✅ (mục 2) → thẻ và giao diện dễ nhầm. Đề xuất: đổi tên tướng thường thành **Xạ Thủ** và **Đao Khách** (`docs/Tuong/NormalHeroes.md` đang tạm dùng hai tên này; `displayName` trong asset chưa đổi). Hoặc đặt tên khác.
  5. ~~Thời điểm đặt tên chủ tướng~~ – **đã chốt 2026-10-17**: đặt tên ngay sau khi chọn chủ tướng, mỗi máy chủ một tên (mục 2).
- ~~Khống chế của quân ta lên boss~~ – **đã chốt 2026-10-17** (bỏ đề xuất "không hất văng, choáng −50 %"): boss **chịu đủ** khống chế và hiệu ứng như mọi quân địch (mục 8).
- ⏳ **Cân bằng độ khó** (thông số tướng/lính/quân địch, đợt quân, thời gian, bề rộng và số cầu): để tới **giai đoạn sắp phát hành** (người dùng chốt 2026-10-16). Khung chỉ số theo cấp / sao / phe đã thiết kế (🟡, `docs/KeHoach/HeroProgression.md`, người dùng yêu cầu 2026-10-17); asset prototype giữ nguyên, tinh chỉnh cuối vẫn để giai đoạn này. Đã biết: sau khi có sông, ải Thủ suýt thua (mất cả 5 tướng, thành còn 173/3000) và ải Công dễ hơn (3 sao, không mất tướng) – xem test `Balance_*`.
