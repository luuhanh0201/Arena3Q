# Arena3Q – Bốn tướng thường (thiết kế nháp)

> **VFX (người dùng 2026-10-22):** hiệu ứng kỹ năng lấy từ gói có sẵn rồi chỉnh cho hợp dự án (`docs/QuyTac/QuyTacLamViec.md` §3). Việc chính của agent với tướng: **sửa chuyển động cho khớp từng chiêu** (đà, mốc sự kiện, hướng, theo đà, thu về) và **kiểm lỗi kỹ**.

Phiên bản: v0.1 · 2026-10-17 · Trạng thái: **🟡 bản nháp chờ duyệt** (tên, ngoại hình, kỹ năng, con số). Kỹ năng **đã có trong code** (2026-10-17, hình tạm; con số = bảng dưới, asset ở `Assets/Arena3Q/Content/Skills/`).

Đã chốt ✅ (spec §3 "Kỹ năng trên thẻ"): mọi tướng có **kỹ năng 1 = nội tại + kỹ năng 2, 3 (tự dùng, 2 trước 3) + tuyệt kỹ (thanh nộ, dùng bằng thẻ)**; luật nộ, vận chiêu, tự nhắm giống hệt chủ tướng (`docs/Tuong/LordHeroes.md` §2, §3). Animation **skeletal từ PSD tách bộ phận**, 2 hướng + lật gương, phong cách **A** (chibi ~2.5 đầu, render 3D).

✅ (2026-10-17) Bốn tướng này là **tướng khởi đầu**: **không phe**, chỉ số gốc thấp hơn danh tướng, dễ tăng sao; kỹ năng **mở theo sao** (1★ nội tại + kỹ năng 2 · 2★ kỹ năng 3 · 3★ tuyệt kỹ · 4–5★ tăng cấp kỹ năng). Chỉ số đề xuất và tăng theo cấp: `docs/KeHoach/HeroProgression.md` §4, §7, §8.

Đi kèm: prompt ảnh `docs/art/concepts/TuongThuong_Prompts.md` · VFX `docs/art/concepts/VFX_Prompts.md` §20–41 · icon `Icon_Prompts.md` · chân dung thẻ `ChanDung_Prompts.md` · quy ước animation chung `docs/Tuong/LordAnimations.md` §0.

> ✅ **Tên đã chốt 2026-10-17** (spec §13 mục 4): tướng thường **Xạ Thủ** (id `archer`, asset `Hero_Archer`) và **Đao Khách** (id `swordsman`, asset `Hero_Swordsman`), thay tên cũ "Cung Thủ" / "Kiếm Sĩ" vốn trùng hai class chủ tướng. Id và tên file giữ nguyên để không hỏng hồ sơ đã lưu. Ngoại hình và kỹ năng cũng được làm **khác hẳn** chủ tướng cùng loại.

---

## 1. Tổng quan 🟡

Chỉ số máu / sát thương / tầm / tốc chạy / tốc đánh lấy từ asset hiện có (`Assets/Arena3Q/Content/Heroes`), tốc đánh = 1 / `attackInterval`. Đề xuất cân bằng (Xạ Thủ Công 30 → 52, Giáp Sĩ 25 → 20…): `docs/KeHoach/HeroProgression.md` §8 – asset chưa đổi.

| | **Xạ Thủ** | **Kỵ Binh** | **Giáp Sĩ** | **Đao Khách** |
|---|---|---|---|---|
| Asset | `Hero_Archer` (`archer`) | `Hero_Cavalry` (`cavalry`) | `Hero_Guardian` (`guardian`) | `Hero_Swordsman` (`swordsman`) |
| Giới tính | Nam | Nữ | Nam | Nữ |
| Vai trò | Tầm xa **săn mục tiêu đơn lẻ**, kết liễu | Cơ động, **xông trận**, khống chế ngắn | **Đỡ đòn**, bảo vệ quân ta | Cận chiến **dồn sát thương** một mục tiêu |
| Khác chủ tướng cùng loại | Chủ tướng Cung thủ đánh vùng / phá thành; Xạ Thủ đánh **một mục tiêu** | – | – | Chủ tướng Kiếm sĩ song kiếm, xoay vùng; Đao Khách **đại đao một lưỡi**, dồn một mục tiêu |
| Vũ khí | Cung phức hợp ngắn, ống tên đeo hông; chim ưng (chỉ là VFX kỹ năng) | **Yển nguyệt đao** cán dài, cưỡi ngựa trắng | **Khiên chữ nhật lớn** + đao ngắn | **Đại đao một lưỡi**, vỏ đao đeo lưng |
| Máu / sát thương | 450 / 30 | 650 / 35 | 1200 / 25 | 700 / 40 |
| Tầm đánh | 4.5 | 0.6 | 0.6 | 0.6 |
| Tốc chạy | 2.6 | **3.6** | 2.2 | 2.8 |
| Tốc đánh (đòn/giây) | 0.9 | 1.0 | 0.8 | 1.1 |
| Màu chủ đạo | Nâu hạt dẻ + vàng lúa | Xanh ngọc lam đậm + bạc | Xám thép + đồng cam | Đỏ cam + trắng ngà |
| Dải màu đội (tô lại được) | Khăn quàng cổ | Dải lụa mũ + tua dưới cổ ngựa | Tua mũ + băng vải trên khiên | 2 đuôi đai lưng |

---

## 2. Bộ kỹ năng 🟡

Cách dùng như chủ tướng (`LordHeroes.md` §2): kỹ năng 2, 3 **tự dùng, tự nhắm** (mục tiêu đang đánh → cụm địch đông nhất trong tầm → thành địch; không có mục tiêu thì chờ). Tuyệt kỹ: **chạm** thẻ = tự nhắm, **kéo** thẻ = tự chọn hướng / điểm (kỹ năng không có hướng thì kéo cũng như chạm). HC = hồi chiêu (giây). Mọi con số là gợi ý để chơi thử.

**Tránh xung đột đã biết:** không có kỹ năng 2/3 nào làm **tướng dịch chuyển** (luật đã chốt: spec §3 – không tự dùng khi đang Moving) và không có **khiêu khích** (spec §13 mục 1). Dịch chuyển chỉ có ở tuyệt kỹ của Kỵ Binh – do người chơi bấm, theo luật vùng cấm.

### 2.1 Xạ Thủ
| Loại | Tên | Hiệu ứng | Cách dùng | HC (s) / Nộ |
|---|---|---|---|---|
| Kỹ năng 1 – Nội tại | **Liệp Sát** | Đòn đánh lên mục tiêu **dưới 40 % máu** +30 % sát thương (áp dụng cả kỹ năng) | – | – |
| Kỹ năng 2 | **Độc Tiễn** | Một mũi tên độc: 120 % sát thương + độc 4 giây (mỗi giây 15 % sát thương) + chậm 20 % | Tự dùng (mục tiêu đang đánh) | 9 |
| Kỹ năng 3 | **Ưng Kích** | Thả chim ưng lao vào mục tiêu trong tầm 6: 150 % sát thương, mục tiêu −30 % tốc đánh 4 giây | Tự dùng (tự nhắm) | 13 |
| Tuyệt kỹ | **Xuyên Dương Tiễn** | Một mũi tên cực mạnh bay thẳng dài 9, trúng **địch đầu tiên** trên đường: 400 % sát thương (thành: 200 %) | Thẻ: chạm (tự nhắm: mục tiêu đang đánh → địch nhiều máu nhất trong tầm) / kéo (chọn hướng) | Nộ |

### 2.2 Kỵ Binh
| Loại | Tên | Hiệu ứng | Cách dùng | HC (s) / Nộ |
|---|---|---|---|---|
| Kỹ năng 1 – Nội tại | **Xung Phong** | Sau khi chạy liên tục ≥ 2 giây, đòn đánh đầu tiên gây 200 % sát thương và choáng 0.5 giây | – | – |
| Kỹ năng 2 | **Mã Đạp** | Ngựa chồm lên dậm xuống: vòng bán kính 1.5 quanh mình, 120 % sát thương + choáng 0.8 giây | Tự dùng (có địch trong bán kính) | 10 |
| Kỹ năng 3 | **Phong Mã** | Bản thân +40 % tốc chạy, +20 % sát thương trong 5 giây | Tự dùng (khi có địch trong `aggroRange`) | 14 |
| Tuyệt kỹ | **Thiên Quân Đột Kích** | Phi ngựa lao thẳng dài 8: 250 % sát thương và **hất văng** 1.5 mọi địch trên đường | Thẻ: chạm (hướng về cụm địch đông nhất) / kéo (chọn hướng) | Nộ |

Tuyệt kỹ làm Kỵ Binh dịch chuyển: **dừng ở mép vùng cấm / biên bản đồ** (spec §8), lính không theo kịp thì đi theo luật `leashRadius`; xong thì tiếp tục lệnh cũ (Moving đi tiếp tới điểm đích từ vị trí mới).

### 2.3 Giáp Sĩ
| Loại | Tên | Hiệu ứng | Cách dùng | HC (s) / Nộ |
|---|---|---|---|---|
| Kỹ năng 1 – Nội tại | **Kiên Thủ** | Giảm 15 % sát thương nhận vào; dưới 50 % máu giảm 30 % | – | – |
| Kỹ năng 2 | **Thuẫn Kích** | Đập khiên vào mục tiêu đang đánh: 150 % sát thương + choáng 1 giây | Tự dùng (mục tiêu đang đánh) | 9 |
| Kỹ năng 3 | **Hộ Vệ Trận** | Quân ta (tướng + lính) trong bán kính 3 quanh Giáp Sĩ nhận **khiên** chặn 120 sát thương (10 % máu tối đa), 5 giây | Tự dùng (khi có địch trong `aggroRange`) | 15 |
| Tuyệt kỹ | **Kim Thành Thiết Bích** | Cắm khiên tạo **vùng bảo vệ** bán kính 3.5 tại chỗ trong 6 giây: quân ta trong vùng −50 % sát thương nhận vào và **miễn khống chế**. Vùng đứng yên, Giáp Sĩ vẫn tự do | Thẻ: chạm (tại vị trí Giáp Sĩ) / kéo (chọn điểm trong bán kính 4 quanh Giáp Sĩ) | Nộ |

### 2.4 Đao Khách
| Loại | Tên | Hiệu ứng | Cách dùng | HC (s) / Nộ |
|---|---|---|---|---|
| Kỹ năng 1 – Nội tại | **Liên Trảm** | Mỗi đòn trúng **cùng một mục tiêu** liên tiếp +6 % sát thương, cộng dồn tối đa 5 lần (+30 %); đổi mục tiêu thì mất | – | – |
| Kỹ năng 2 | **Bạt Đao Trảm** | Một nhát chém dồn lực vào mục tiêu đang đánh: 250 % sát thương | Tự dùng (mục tiêu đang đánh) | 8 |
| Kỹ năng 3 | **Đao Khí** | Phóng lưỡi đao khí bay thẳng dài 5 (đạn 12 đơn vị/giây), **xuyên** mọi địch: 140 % sát thương mỗi địch | Tự dùng (hướng về mục tiêu / cụm đông nhất) | 11 |
| Tuyệt kỹ | **Cuồng Đao Lục Trảm** | Chém liên tiếp 6 nhát trong 1.5 giây vào mọi địch bán kính 1.8 (5 nhát × 80 %, nhát cuối 250 %); **miễn khống chế** trong lúc chém | Thẻ: chạm (tại chỗ) | Nộ |

---

## 3. Liên hệ với luật hiện có
- Mọi luật ở `LordHeroes.md` §3 áp dụng y hệt (kỹ năng không đổi lệnh; Dead không dùng kỹ năng; nộ chỉ tính sát thương do tướng gây ra; mốc `Cast`; tốc đánh nhân vào clip).
- "Quân ta trong vùng" (Hộ Vệ Trận, Kim Thành Thiết Bích) gồm **mọi tướng và lính phe ta**, không chỉ đội của Giáp Sĩ.
- Khống chế do quân ta gây ra (Mã Đạp, Thuẫn Kích, Xung Phong) chỉ tác dụng lên **quân địch**; quân địch không có lệnh của người chơi nên không chạm luật mục 6.
- Đạn mới (spec §6 🟡, tốc độ cấu hình theo tướng): chim ưng **12**, đao khí **12**, mũi tên Độc Tiễn / Xuyên Dương Tiễn **15** đơn vị/giây.

---

## 4. Animation, lớp PSD và xương

Quy ước chung (bộ 13 clip, `Hit` @ 0.4 s ở nhịp 1 đòn/giây, `Cast`, mặt đổi qua Sprite Resolver, `F_` / `B_`, key `sortingOrder`): **`docs/Tuong/LordAnimations.md` §0**. Quy trình Krita + Skinning Editor: `docs/Tuong/StrategistArtGuide.md` §3–4. Tướng thường **không có** clip `select` (không có màn chọn).

### 4.1 Xạ Thủ (nam · cung ngắn · tầm 4.5 · tốc đánh 0.9)
**Lớp `F_` (dưới → trên):** `F_hat_tail_2` · `F_hat_tail_1` · `F_coat_back` · `F_bow` · `F_bowstring` · `F_hand_far` · `F_arm_far_lower` · `F_arm_far_upper` · `F_leg_far_upper` · `F_leg_far_lower` · `F_foot_far` · `F_body` · `F_pelvis` · `F_quiver_hip` · `F_leg_near_upper` · `F_leg_near_lower` · `F_foot_near` · `F_coat_front` · `F_cape_fur` (áo lông vai) · `F_team_scarf` · `F_neck` · `F_head` · nhóm `F_face` (`calm`, `shout`, `hurt`, `ko`) · `F_hat_fur` · `F_arm_near_upper` · `F_arm_near_lower` · `F_arrow` · nhóm `F_hand_near` (`open`, `pinch`).

Hướng sau `B_`: bỏ nhóm mặt; đuôi mũ lông và `B_coat_back` nằm trên thân; ống tên ở hông khuất một nửa.

**Xương:**
```
root
└ hip
  ├ spine → chest → neck → head → hat_tail_1 → hat_tail_2
  │         ├ arm_near_upper → arm_near_lower → hand_near → arrow
  │         └ arm_far_upper  → arm_far_lower  → hand_far  → bow → bowstring, bow_tip
  ├ leg_near_upper → leg_near_lower → foot_near
  ├ leg_far_upper  → leg_far_lower  → foot_far
  ├ coat_front_1 → coat_front_2
  ├ coat_back_1  → coat_back_2
  └ quiver_hip
```
IK 4 chi; dây cung như chủ tướng Cung thủ (`LordAnimations.md` §2.2).

| Clip | Độ dài | Động tác | Sự kiện | VFX |
|---|---|---|---|---|
| `idle` | 2.0 s | thở, ngó quanh, đuôi mũ lông lay | – | – |
| `run` | 0.55 s | chạy khom người kiểu thợ săn, cung cầm thấp | – | – |
| `attack` | 1.0 s | 0–0.15 rút tên ở hông · 0.15–0.4 giương cung (tay xa ra trước, key `sortingOrder`) · 0.4 buông · hạ cung | `Hit` @ 0.4 | hunter_basic: mũi tên (đạn 15) @ `bow_tip` |
| `hit` | 0.3 s | giật lùi, `hurt` | – | common |
| `die` | 1.0 s | ngã ngửa, cung văng, `ko` | – | – |
| `spawn` | 0.8 s | bước ra, phủi mũ, lên dây | – | common: cột sáng |
| `command` | 0.6 s | chĩa cung về phía trước, `shout` | – | – |
| `victory` | 1.5 s | giơ cung, huýt sáo (chim ưng VFX bay qua) | – | hunter_hawk: chim ưng (tuỳ chọn) |
| `stunned` | 0.6 s | loạng choạng, `ko` | – | common: sao choáng |
| `skill2` **Độc Tiễn** | 0.7 s | chấm tên vào túi độc, giương cung, bắn | `Cast` @ 0.5 | hunter_poison: mũi tên xanh độc + vệt khói @ `bow_tip`; bong bóng độc trên mục tiêu 4 s |
| `skill3` **Ưng Kích** | 0.6 s | giơ tay xa (bỏ cung xuống), hất tay thả chim, `shout` | `Cast` @ 0.35 | hunter_hawk: chim ưng (đạn 12) từ `hand_far`; vết cào khi trúng |
| `ultimate` **Xuyên Dương Tiễn** | 1.3 s | 0–0.9 quỳ một gối kéo cung hết cỡ, ánh vàng tụ đầu tên · 0.9 buông · giật lùi | `Cast` @ 0.9 | hunter_pierce: ánh tụ @ `bow_tip` (0–0.9), mũi tên vàng + vệt dài; nổ vòng khi trúng |

Nội tại **Liệp Sát**: dấu ngắm đỏ nhỏ trên đầu mục tiêu dưới 40 % máu (hunter_passive).

### 4.2 Kỵ Binh (nữ · cưỡi ngựa · yển nguyệt đao · tốc chạy 3.6 · tốc đánh 1.0)
Kích thước hình 🟡 **1.7 × 1.7** đơn vị (to hơn tướng thường 1.2 × 1.6 vì có ngựa – câu hỏi §5); PSD cao ~440 px, rộng ~460 px.

**Lớp `F_` (dưới → trên):** ngựa phía xa `F_horse_tail_2` · `F_horse_tail_1` · `F_horse_leg_back_far_upper` · `F_horse_leg_back_far_lower` · `F_horse_leg_front_far_upper` · `F_horse_leg_front_far_lower` · người phía xa `F_rider_hand_far` (cầm cương) · `F_rider_arm_far` · thân ngựa `F_horse_body` · `F_saddle` · `F_rider_leg_near` (chân trong bàn đạp) · ngựa phía gần `F_horse_leg_back_near_upper` · `F_horse_leg_back_near_lower` · `F_horse_leg_front_near_upper` · `F_horse_leg_front_near_lower` · `F_horse_neck` · `F_horse_mane` · `F_team_tassel` (tua dưới cổ) · `F_horse_head` (nhóm: `calm`, `neigh` – há miệng) · `F_reins` · người `F_cape_2` · `F_cape_1` · `F_rider_body` · `F_rider_neck` · `F_rider_head` · nhóm `F_face` · `F_helmet` · `F_team_ribbon_1` · `F_team_ribbon_2` · `F_rider_arm_near_upper` · `F_rider_arm_near_lower` · `F_guandao` · `F_rider_hand_near`.

Hướng sau `B_`: thấy mông ngựa và đuôi **trên cùng**, đầu ngựa khuất phía xa; áo choàng người cưỡi che lưng.

**Xương:**
```
root
└ horse_body
  ├ horse_neck → horse_head
  │            └ horse_mane, team_tassel
  ├ horse_tail_1 → horse_tail_2
  ├ horse_leg_front_near_upper → horse_leg_front_near_lower → hoof_front_near
  ├ horse_leg_front_far_upper  → horse_leg_front_far_lower  → hoof_front_far
  ├ horse_leg_back_near_upper  → horse_leg_back_near_lower  → hoof_back_near
  ├ horse_leg_back_far_upper   → horse_leg_back_far_lower   → hoof_back_far
  └ saddle → rider_hip
              ├ rider_leg_near
              └ rider_spine → rider_chest → rider_neck → rider_head → helmet → ribbon_1 → ribbon_2
                              ├ cape_1 → cape_2
                              ├ rider_arm_near_upper → rider_arm_near_lower → rider_hand_near → guandao → guandao_tip
                              └ rider_arm_far → rider_hand_far
```
IK: 4 móng ngựa (`ik_hoof_*`), `ik_rider_hand_near`. Dây cương `F_reins` gắn 2 xương (`rider_hand_far` và `horse_head`) để co giãn theo.

| Clip | Độ dài | Động tác | Sự kiện | VFX |
|---|---|---|---|---|
| `idle` | 2.0 s | ngựa thở, gõ móng, vẫy đuôi; người cưỡi xoay đao nhẹ | – | – |
| `run` | 0.5 s | **phi nước đại** 4 nhịp chân, thân ngựa nhấp nhô, áo choàng và tua bay | `Step` ×2 | cavalry_dust: bụi chân ngựa (tuỳ chọn) |
| `attack` | 1.0 s | 0–0.4 vung đao ra sau · 0.4 quét ngang tới trước · 0.4–1.0 thu đao | `Hit` @ 0.4 | cavalry_basic: vệt quét cong @ `guandao_tip` |
| `hit` | 0.3 s | ngựa lùi nửa bước, người `hurt` | – | common |
| `die` | 1.2 s | ngựa khuỵu chân trước nằm xuống, người gục trên lưng ngựa (`ko`) | – | – |
| `spawn` | 1.0 s | ngựa chồm hai chân trước, hí (`neigh`), đáp xuống | – | common: cột sáng + cavalry_dust |
| `command` | 0.6 s | chĩa đao về phía trước, `shout` | – | – |
| `victory` | 1.5 s | ngựa chồm lên, người giơ đao | – | – |
| `stunned` | 0.6 s | ngựa đứng lắc đầu, người `ko` | – | common: sao choáng |
| `skill2` **Mã Đạp** | 0.7 s | 0–0.4 ngựa chồm cao, hí (`neigh`) · 0.45 hai móng dậm xuống | `Cast` @ 0.45 | cavalry_stomp: vòng sóng đất + vết nứt @ `hoof_front_near` (bán kính 1.5) |
| `skill3` **Phong Mã** | 0.5 s | người quất cương, ngựa hí, gió quấn quanh | `Cast` @ 0.3 | cavalry_windrider: vòng gió quanh `horse_body` 5 s, vệt gió sau móng khi chạy |
| `ultimate` **Thiên Quân Đột Kích** | 1.4 s | 0–0.3 cúi rạp chĩa đao · 0.3–1.1 phi thẳng (code đẩy vị trí, `run` tốc độ ×2) · 1.1–1.4 ghìm cương, ngựa chồm | `Cast` @ 0.3 (bắt đầu lao, sát thương theo đường) | cavalry_charge: bóng mờ + vệt bụi dọc đường, sao va chạm + hất văng trên từng địch |

Nội tại **Xung Phong**: chạy đủ 2 giây → vệt sáng xanh ngọc ở móng ngựa (cavalry_passive), tắt sau đòn đầu.

### 4.3 Giáp Sĩ (nam · khiên + đao ngắn · tốc đánh 0.8)
**Lớp `F_` (dưới → trên):** `F_team_tassel` (tua mũ, phía sau) · `F_cape_short` · `F_hand_far` · `F_arm_far_lower` · `F_arm_far_upper` · `F_pauldron_far` · `F_leg_far_upper` · `F_leg_far_lower` · `F_foot_far` · `F_skirt_back` · `F_body` (giáp ngực) · `F_pelvis` · `F_leg_near_upper` · `F_leg_near_lower` · `F_foot_near` · `F_skirt_front` · `F_neck` · `F_head` · nhóm `F_face` · `F_helmet` · `F_arm_near_upper` · `F_pauldron_near` · `F_arm_near_lower` · `F_saber_short` · `F_hand_near` · **`F_shield`** + `F_team_shield_band` (khiên cầm tay xa; ở tư thế chuẩn khiên đứng **bên cạnh** thân phía xa – khi đỡ / đập khiên key `sortingOrder` lên trên thân).

Hướng sau `B_`: lưng giáp, áo choàng ngắn; khiên thấy **mặt sau** (lớp `B_shield_back`, vẽ quai cầm).

**Xương:**
```
root
└ hip
  ├ spine → chest → neck → head → helmet → tassel
  │         ├ cape_short
  │         ├ arm_near_upper → arm_near_lower → hand_near → saber_short → saber_tip
  │         └ arm_far_upper  → arm_far_lower  → hand_far  → shield → shield_center
  ├ leg_near_upper → leg_near_lower → foot_near
  ├ leg_far_upper  → leg_far_lower  → foot_far
  ├ skirt_front_1 → skirt_front_2
  └ skirt_back_1  → skirt_back_2
```

| Clip | Độ dài | Động tác | Sự kiện | VFX |
|---|---|---|---|---|
| `idle` | 2.0 s | đứng tấn, khiên chống đất, thở chậm | – | – |
| `run` | 0.7 s | bước nặng, khiên nâng trước ngực | `Step` | – |
| `attack` | 1.0 s | 0–0.4 khiên đẩy lên che, đao rút về · 0.4 chém ngắn qua mép khiên · thu về | `Hit` @ 0.4 | guardian_basic: vệt chém ngắn @ `saber_tip` |
| `hit` | 0.3 s | khiên rung, lùi nhẹ (không giật mạnh) | – | guardian_block: tia lửa trên khiên @ `shield_center` |
| `die` | 1.2 s | khiên rơi, quỳ, ngã sấp (`ko`) | – | – |
| `spawn` | 1.0 s | cắm khiên xuống đất đánh "rầm" | – | common: cột sáng + vòng va chạm |
| `command` | 0.6 s | đập đao vào khiên, `shout` | – | – |
| `victory` | 1.5 s | giơ khiên lên cao, cười | – | – |
| `stunned` | 0.6 s | lảo đảo, khiên hạ thấp, `ko` | – | common: sao choáng |
| `skill2` **Thuẫn Kích** | 0.6 s | 0–0.3 lùi khiên ra sau · 0.3 lao nửa bước đập khiên tới (không dịch vị trí logic) | `Cast` @ 0.3 | guardian_bash: sóng va chạm + sao choáng @ mục tiêu |
| `skill3` **Hộ Vệ Trận** | 0.7 s | giơ khiên lên trời, `shout` | `Cast` @ 0.45 | guardian_aegis: vòng vàng mở rộng @ `root` (bán kính 3); bong bóng khiên nhỏ trên từng quân ta 5 s |
| `ultimate` **Kim Thành Thiết Bích** | 1.1 s | 0–0.6 nâng khiên qua đầu · 0.7 cắm khiên xuống đất, `shout` · đứng dậy | `Cast` @ 0.7 | guardian_fortress: tường khiên vàng vòng tròn @ điểm nhắm (bán kính 3.5, 6 s), vết nứt đất |

Nội tại **Kiên Thủ**: dưới 50 % máu – khiên ánh đồng sáng (guardian_passive).

### 4.4 Đao Khách (nữ · đại đao một lưỡi · tốc đánh 1.1)
**Lớp `F_` (dưới → trên):** `F_braid_2` · `F_braid_1` · `F_scabbard_back` · `F_team_sash_tail_2` · `F_team_sash_tail_1` · `F_hand_far` · `F_arm_far_lower` · `F_arm_far_upper` · `F_leg_far_upper` · `F_leg_far_lower` · `F_foot_far` · `F_jacket_back` · `F_body` · `F_pelvis` (đai) · `F_leg_near_upper` · `F_leg_near_lower` · `F_foot_near` · `F_jacket_front` · `F_neck` · `F_head` · nhóm `F_face` · `F_hair_front` · `F_hair_ornament` · `F_arm_near_upper` · `F_arm_near_lower` · `F_dao` · `F_hand_near`.

Hướng sau `B_`: vỏ đao sau lưng và bím tóc nằm **trên** thân.

**Xương:**
```
root
└ hip
  ├ spine → chest → neck → head → braid_1 → braid_2
  │         ├ scabbard_back
  │         ├ arm_near_upper → arm_near_lower → hand_near → dao → dao_tip
  │         └ arm_far_upper  → arm_far_lower  → hand_far
  ├ leg_near_upper → leg_near_lower → foot_near
  ├ leg_far_upper  → leg_far_lower  → foot_far
  ├ jacket_front_1 → jacket_front_2
  ├ jacket_back_1  → jacket_back_2
  └ sash_tail_1 → sash_tail_2
```
Đòn nặng (Bạt Đao Trảm, tuyệt kỹ) dùng **hai tay**: bật IK `ik_hand_far` bám xương `dao`.

| Clip | Độ dài | Động tác | Sự kiện | VFX |
|---|---|---|---|---|
| `idle` | 2.0 s | vác đao trên vai, nhún nhẹ | – | – |
| `run` | 0.55 s | chạy nghiêng, đao kéo phía sau | – | – |
| `attack` | 1.0 s | 0–0.4 xoay đao ra sau · 0.4 chém ngang · 0.4–1.0 thu về vai | `Hit` @ 0.4 | blade_basic: vệt chém đỏ cam @ `dao_tip` |
| `hit` | 0.3 s | giật lùi, `hurt` | – | common |
| `die` | 1.0 s | cắm đao xuống, quỳ, ngã nghiêng (`ko`) | – | – |
| `spawn` | 0.8 s | nhảy xuống, cắm đao, rút lên vác vai | – | common: cột sáng |
| `command` | 0.6 s | chĩa đao về phía trước, `shout` | – | – |
| `victory` | 1.5 s | xoay đao một vòng, vác vai, nháy mắt | – | – |
| `stunned` | 0.6 s | loạng choạng, `ko` | – | common: sao choáng |
| `skill2` **Bạt Đao Trảm** | 0.7 s | 0–0.45 hạ thấp người, hai tay nắm cán, đao ra sau · 0.45 chém xéo lên cực mạnh | `Cast` @ 0.45 | blade_draw: ánh tụ trên lưỡi (0–0.45), vệt chém lớn + tia lửa |
| `skill3` **Đao Khí** | 0.6 s | vung đao chém ngang ra xa | `Cast` @ 0.35 | blade_wave: lưỡi đao khí lưỡi liềm bay (đạn 12, xuyên) @ `dao_tip` |
| `ultimate` **Cuồng Đao Lục Trảm** | 1.7 s | 0–0.2 thủ thế, `shout` · 0.2–1.45 chém 5 nhát quanh mình (xoay, đổi hướng chém) · 1.6 nhảy lên bổ xuống | `Cast` @ 0.4 / 0.65 / 0.9 / 1.15 / 1.4 / **1.6** (nhát cuối mạnh) | blade_frenzy: vệt chém đỏ cam liên tục quanh `root`, nhát cuối nứt đất + cột lửa đỏ |

Nội tại **Liên Trảm**: số chấm cam nhỏ (1–5) trên đầu mục tiêu đang bị dồn (blade_passive).

### 4.5 Tổng hợp file VFX tướng thường
| Tướng | Đánh thường | Kỹ năng 2 | Kỹ năng 3 | Tuyệt kỹ | Nội tại / khác |
|---|---|---|---|---|---|
| Xạ Thủ | `hunter_basic` | `hunter_poison` | `hunter_hawk` | `hunter_pierce` | `hunter_passive` |
| Kỵ Binh | `cavalry_basic` | `cavalry_stomp` | `cavalry_windrider` | `cavalry_charge` | `cavalry_passive`, `cavalry_dust` |
| Giáp Sĩ | `guardian_basic` | `guardian_bash` | `guardian_aegis` | `guardian_fortress` | `guardian_passive`, `guardian_block` |
| Đao Khách | `blade_basic` | `blade_draw` | `blade_wave` | `blade_frenzy` | `blade_passive` |

Tên VFX dùng tiền tố riêng (`hunter`, `blade`) để **không trùng** với chủ tướng (`archer`, `swordsman`).

---

## 5. Câu hỏi còn mở
1. ~~Tên trùng~~ – **đã chốt 2026-10-17**: Xạ Thủ và Đao Khách (spec §13 mục 4).
2. **Kỵ Binh** to hơn tướng thường (hình 🟡 1.7 × 1.7 thay vì 1.2 × 1.6): giữ thân logic bán kính 0.45 như mọi tướng, hay tăng lên 0.6?
3. ~~Khống chế của quân ta lên boss~~ – **đã chốt 2026-10-17**: boss chịu **đủ** choáng, hất văng / đẩy lùi và mọi hiệu ứng như quân địch khác (spec §8).
4. Chỉ số ở bảng §1 giữ nguyên theo asset; cân bằng để giai đoạn sắp phát hành (spec §13 ⏳).
