# Arena3Q – Animation, xương và mốc kỹ năng của 4 chủ tướng

Phiên bản: v0.1 · 2026-10-17 · **🟡 bản nháp** (thời lượng, mốc sự kiện, tốc độ đạn là gợi ý để chơi thử). Đi kèm:
- Kỹ năng: `docs/LordHeroes.md` §2 · luật kỹ năng: spec §3 "Kỹ năng trên thẻ".
- Quy trình mẫu (Krita, Skinning Editor, phần mình code): `docs/StrategistArtGuide.md` §3, §4, §6.
- Prompt ảnh: `docs/art/concepts/<ChuTuong>_Prompts.md` · VFX: `docs/art/concepts/VFX_Prompts.md`.

---

## 0. Quy ước chung (cả 4 chủ tướng)

### 0.1 Bộ clip
| Clip | Độ dài chuẩn | Lặp | Sự kiện | Dùng khi |
|---|---|---|---|---|
| `idle` | 2.0 s | có | – | Idle, đứng chờ |
| `run` | 0.5–0.7 s | có | `Step` (tuỳ chọn, tiếng bước) | Moving, Advancing, đuổi mục tiêu |
| `attack` | **1.0 s** | – | **`Hit` @ 0.4 s** | mỗi đòn đánh thường |
| `hit` | 0.3 s | – | – | nhận sát thương (không chen ngang `attack` / kỹ năng) |
| `die` | 1.0–1.2 s | – | – | chết, giữ khung cuối rồi mờ dần |
| `spawn` | 0.8–1.0 s | – | – | vừa triệu hồi |
| `command` | 0.6 s | – | – | nhận `AttackCommand` |
| `victory` | 1.5 s | có | – | thắng trận |
| `select` | 3.0 s | có | – | màn chọn chủ tướng |
| `stunned` | 0.6 s | có | – | bị khống chế (choáng, trói, mê hoặc) – mặt `ko`, sao quay trên đầu (VFX chung) |
| `skill2`, `skill3` | 0.5–1.5 s | – | **`Cast`** (có thể nhiều lần) | kỹ năng 2, 3 tự dùng |
| `ultimate` | 1.0–1.2 s | – | **`Cast`** | tuyệt kỹ (chạm / kéo thẻ) |

### 0.2 Tốc đánh và đòn đánh
- `attack` vẽ ở **nhịp chuẩn 1 đòn/giây**; Animator chạy với tốc độ = **tốc đánh** của tướng (spec §6). Ví dụ Kiếm sĩ 1.4 → clip dài ≈ 0.71 s, `Hit` ở ≈ 0.29 s.
- 0–0.4 s là **khoảng vung** (spec §6 🟡 40 %); `Hit` = lúc gây sát thương (⏳ làm cùng đợt animation).
- 🟡 Đánh **tầm xa** (Cung thủ, Mưu sĩ): tại `Hit` sinh **đạn** bay tới mục tiêu, **sát thương khi đạn tới** (mục tiêu chết trước thì đạn tan). Tốc độ đạn: mũi tên **15 đơn vị/s**, luồng khí quạt **10 đơn vị/s**.
- Hiệu ứng "+% tốc đánh" (Kiếm Tâm, Trống Trận) nhân vào tốc độ clip.

### 0.3 Kỹ năng
- Trong lúc vận chiêu, tướng **đứng yên** (trừ kỹ năng có di chuyển, do code đẩy vị trí); hết clip thì quay về trạng thái / lệnh cũ (LordHeroes §3).
- Hiệu lực của kỹ năng xảy ra ở **`Cast`**. 🟡 Bị khống chế hoặc chết **trước `Cast`** → kỹ năng **không bị tính** (không vào hồi chiêu, không mất nộ).
- Nhận lệnh mới khi đang vận chiêu: kỹ năng vẫn chạy hết, lệnh có hiệu lực ngay sau.
- Mặt đổi theo clip qua **Sprite Resolver**: `calm` mặc định, `shout` khi ra lệnh / tuyệt kỹ, `hurt` khi `hit`, `ko` khi `die` / `stunned`.

### 0.4 Hướng, lật và thứ tự lớp
- Hai prefab: **trước-phải** (`F_`) và **sau-phải** (`B_`); đi lên trên (Y logic tăng) → sau, còn lại → trước; sang trái → `scale.x = −1`.
- Kỹ năng có hướng (tuyệt kỹ kéo thẻ) quay mặt về hướng nhắm trước khi chạy clip.
- Khi tay xa vung ra **trước** thân (Cung thủ giương cung, Thống soái đâm thương), **key `sortingOrder`** của tay/vũ khí trong clip để nó nổi lên trên thân, hết đòn trả về.

### 0.5 Gắn VFX vào sự kiện
VFX là prefab hạt (từ mảnh trong `VFX_Prompts.md`), sinh tại **xương neo** ở đúng sự kiện. Cột "VFX" trong các bảng dưới ghi `<tấm VFX>: <mảnh>` @ `<xương / vị trí>`.

---

## 1. Mưu sĩ (nữ · quạt lông hạc · tầm 3.0 · tốc đánh 0.8)

Lớp PSD: `StrategistArtGuide.md` §3.1–3.2 · Xương: `StrategistArtGuide.md` §4.1 (thêm xương `fan_tip` con của `fan` để neo VFX).

| Clip | Độ dài | Động tác | Sự kiện | VFX |
|---|---|---|---|---|
| `idle` | 2.0 s | thở nhẹ, quạt phẩy chậm, tay áo và dải lụa lay | – | – |
| `run` | 0.6 s | bước nhỏ nhanh, vạt áo đung đưa, thân hơi nghiêng trước, quạt giữ sát người | – | – |
| `attack` | 1.0 s | 0–0.4 vung quạt ra sau vai · 0.4 phẩy mạnh tới trước · 0.4–1.0 thu về, tay áo bay theo | `Hit` @ 0.4 | strategist_basic: luồng khí (đạn) @ `fan_tip`; khi trúng: bùng khí nhỏ |
| `hit` | 0.3 s | giật lùi, mặt `hurt` | – | common: tia trúng đòn |
| `die` | 1.0 s | lảo đảo, quạt rơi, ngã nghiêng, mặt `ko` | – | – |
| `spawn` | 0.8 s | bước ra khỏi cột sáng, xoè quạt trước ngực rồi hạ xuống | – | common: cột sáng @ `root` |
| `command` | 0.6 s | chỉ quạt về phía trước, mặt `shout` | – | – |
| `victory` | 1.5 s | xoè quạt che nửa dưới mặt, cười, xoay nhẹ | – | – |
| `select` | 3.0 s | tư thế đẹp, chậm | – | – |
| `skill2` **Hỏa Kế** | 0.5 s | quạt quét xuống chỉ đất | `Cast` @ 0.3 | strategist_firefield: vùng lửa @ điểm nhắm (5 s), bùa cháy |
| `skill3` **Bát Quái Trận** | 0.6 s | quạt vẽ vòng tròn trước người | `Cast` @ 0.4 | strategist_bagua: trận đồ + vòng ký tự @ điểm nhắm (6 s) |
| `ultimate` **Đông Phong** | 1.0 s | dang hai tay (bàn tay gần `open`), tay áo bay phần phật, phẩy quạt quét ngang thật mạnh, mặt `shout` | `Cast` @ 0.7 | strategist_eastwind: sóng gió quét ngang cả màn hình, lá, lông vũ |

---

## 2. Cung thủ (nữ · cung sừng · tầm 4.5 · tốc đánh 0.9)

### 2.1 Lớp PSD hướng trước-phải (`F_`, dưới → trên)
`F_cape_2` · `F_quiver` · `F_ponytail_2` · `F_ponytail_1` · `F_team_ribbon_hair` · `F_bow` · `F_bowstring` · `F_team_ribbon_bow` · `F_hand_far` · `F_arm_far_lower` · `F_arm_far_upper` · `F_leg_far_upper` · `F_leg_far_lower` · `F_foot_far` · `F_tunic_back` · `F_body` · `F_pelvis` · `F_leg_near_upper` · `F_leg_near_lower` · `F_foot_near` · `F_tunic_front` · `F_cape_1` (vai + mũ trùm) · `F_neck` · `F_head` · nhóm `F_face` (`calm`, `shout`, `hurt`, `ko`) · `F_hair_front` · `F_hairpin` · `F_arm_near_upper` · `F_arm_near_lower` · `F_arrow` · nhóm `F_hand_near` (`F_hand_near_open`, `F_hand_near_pinch`).

Hướng sau (`B_`): bỏ nhóm mặt; `B_quiver`, `B_ponytail_*`, `B_cape_*` nằm **trên** `B_body`; `B_tunic_front` xuống dưới cùng.

### 2.2 Xương
```
root
└ hip
  ├ spine → chest → neck → head → ponytail_1 → ponytail_2
  │         ├ quiver
  │         ├ cape_1 → cape_2
  │         ├ arm_near_upper → arm_near_lower → hand_near → arrow
  │         └ arm_far_upper  → arm_far_lower  → hand_far  → bow → bowstring
  ├ leg_near_upper → leg_near_lower → foot_near
  ├ leg_far_upper  → leg_far_lower  → foot_far
  ├ tunic_front
  └ tunic_back
```
IK: `ik_hand_near`, `ik_hand_far`, `ik_foot_near`, `ik_foot_far`. Xương `bowstring` đặt ở **giữa dây cung**; khi kéo cung, key nó bám theo `hand_near` (dây cung là sprite có 2 đầu gắn `bow`, giữa gắn `bowstring` → dây gập thành chữ V). Neo VFX: `bow_tip` (con của `bow`, ở đầu mũi tên khi lên dây).

### 2.3 Clip
| Clip | Độ dài | Động tác | Sự kiện | VFX |
|---|---|---|---|---|
| `idle` | 2.0 s | thở, xoay mũi tên trong tay gần, đuôi tóc lay | – | – |
| `run` | 0.55 s | chạy nhẹ nhàng, cung cầm thấp, áo choàng bay | – | – |
| `attack` | 1.0 s | 0–0.15 rút tên (`hand_near_pinch`) · 0.15–0.4 giương cung kéo dây (tay xa ra trước, key `sortingOrder`) · 0.4 buông · 0.4–1.0 hạ cung | `Hit` @ 0.4 | archer_basic: mũi tên (đạn 15 đv/s) + vệt bay @ `bow_tip`; khi trúng: tia lửa xanh |
| `hit` | 0.3 s | giật lùi, mặt `hurt` | – | common: tia trúng đòn |
| `die` | 1.0 s | khuỵu gối, cung rơi, ngã nghiêng, mặt `ko` | – | – |
| `spawn` | 0.8 s | nhảy từ trên xuống tiếp đất nhẹ, lên dây một mũi tên | – | common: cột sáng |
| `command` | 0.6 s | chĩa cung về phía trước, mặt `shout` | – | – |
| `victory` | 1.5 s | giơ cung lên trời, xoay mũi tên, nháy mắt | – | – |
| `select` | 3.0 s | tư thế giương cung chậm, gió thổi áo choàng | – | – |
| `skill2` **Liên Châu Tiễn** | 0.9 s | bắn 3 phát liên tiếp, mỗi phát rút–kéo–buông nhanh | `Cast` @ 0.25 / 0.5 / 0.75 | archer_multishot: mũi tên năng lượng + loé @ `bow_tip` ×3 |
| `skill3` **Tiễn Vũ** | 0.6 s | ngả người, bắn chếch lên trời | `Cast` @ 0.35 | archer_arrowrain: tên vút lên @ `bow_tip`; vùng mưa tên @ điểm nhắm 3 s (tên rơi, vòng bụi) |
| `ultimate` **Phá Thành Hỏa Tiễn** | 1.2 s | 0–0.8 kéo cung hết cỡ, lửa tụ ở đầu tên, mặt `shout` · 0.8 buông · giật lùi mạnh | `Cast` @ 0.8 | archer_firearrow: lửa tụ @ `bow_tip` (0–0.8); mũi tên lửa lớn bay thẳng xuyên quân + vệt lửa; nổ lửa khi trúng thành |

**Nội tại Ưng Nhãn**: đòn thứ 4 – mũi tên **vàng sáng** xuyên mục tiêu (VFX nội tại), không cần clip riêng.

---

## 3. Kiếm sĩ (nam · song kiếm · tầm 0.6 · tốc đánh 1.4)

### 3.1 Lớp PSD hướng trước-phải (`F_`, dưới → trên)
`F_team_headband_tail_2` · `F_team_headband_tail_1` · `F_ponytail_2` · `F_ponytail_1` · `F_robe_back` · `F_hand_far` (nhóm: `F_hand_far_open`, `F_hand_far_swordfinger`) · `F_sword_short_drawn` (ẩn) · `F_arm_far_lower` · `F_arm_far_upper` · `F_leg_far_upper` · `F_leg_far_lower` · `F_foot_far` · `F_body` (giáp lá) · `F_pelvis` (đai) · `F_scabbard_long` · `F_sword_short` + `F_scabbard_short` · `F_leg_near_upper` · `F_leg_near_lower` · `F_foot_near` · `F_robe_front` · `F_robe_side_near` · `F_neck` · `F_head` · nhóm `F_face` · `F_hair_front` · `F_team_headband` · `F_arm_near_upper` · `F_shoulder_pad_near` · `F_arm_near_lower` · `F_sword_long` · `F_hand_near`.

Hướng sau (`B_`): bỏ nhóm mặt; đuôi tóc, đuôi băng trán, `B_robe_back` nằm **trên** thân.

### 3.2 Xương
```
root
└ hip
  ├ spine → chest → neck → head → ponytail_1 → ponytail_2
  │         │                   └ headband_tail_1 → headband_tail_2
  │         ├ arm_near_upper → arm_near_lower → hand_near → sword_long → sword_tip
  │         └ arm_far_upper  → arm_far_lower  → hand_far  → sword_short_drawn
  ├ leg_near_upper → leg_near_lower → foot_near
  ├ leg_far_upper  → leg_far_lower  → foot_far
  ├ robe_front_1 → robe_front_2
  ├ robe_back_1  → robe_back_2
  ├ robe_side_near_1 → robe_side_near_2
  └ scabbard
```
IK: tay và chân như các chủ tướng khác. `F_shoulder_pad_near` gắn `arm_near_upper`.

### 3.3 Clip
| Clip | Độ dài | Động tác | Sự kiện | VFX |
|---|---|---|---|---|
| `idle` | 2.0 s | cầm kiếm hờ chúc xuống, đuôi băng trán bay nhẹ | – | – |
| `run` | 0.5 s | chạy nhanh nghiêng người, kiếm kéo phía sau | – | – |
| `attack` | 1.0 s | 0–0.4 giơ kiếm qua vai phải · 0.4 chém chéo xuống · 0.4–1.0 thu kiếm | `Hit` @ 0.4 | swordsman_basic: vệt chém lưỡi liềm @ `sword_tip`; tia xanh khi trúng |
| `hit` | 0.3 s | giật lùi, mặt `hurt` | – | common |
| `die` | 1.0 s | chống kiếm quỳ xuống, ngã sấp, mặt `ko` | – | – |
| `spawn` | 0.8 s | lộn từ trên xuống, xoay kiếm một vòng rồi thủ thế | – | common: cột sáng |
| `command` | 0.6 s | chĩa kiếm về phía trước, mặt `shout` | – | – |
| `victory` | 1.5 s | tra kiếm vào vỏ, khoanh tay | – | – |
| `select` | 3.0 s | thủ thế song kiếm, gió thổi áo bào | – | – |
| `skill2` **Phi Ảnh Đột Kích** | 0.5 s | 0–0.15 chùng gối · 0.15–0.3 lướt (code đẩy vị trí tới mục tiêu) · 0.3 đâm | `Cast` @ 0.3 | swordsman_dash: bóng mờ + vệt tốc độ dọc đường lướt; sao va chạm; vòng sao choáng trên đầu mục tiêu 1 s |
| `skill3` **Toàn Phong Trảm** | 1.5 s | dang song kiếm (rút `F_sword_short_drawn`), xoay tròn tại chỗ | `Cast` @ 0.25 / 0.5 / … / 1.5 (6 nhịp sát thương) | swordsman_whirlwind: vòng chém @ `root` suốt 1.5 s |
| `ultimate` **Vạn Kiếm Quy Tông** | 1.2 s | 0–0.6 giơ kiếm chỉ trời, tay xa bắt kiếm quyết (`hand_far_swordfinger`), mặt `shout` · 0.6–1.2 chém kiếm xuống phía trước | `Cast` @ 0.7 | swordsman_swordrain: kiếm khí rơi khắp vùng quạt (dài 5) 0.7–1.4 s, cột sáng, vết nứt đất |

**Nội tại Kiếm Tâm**: mỗi đòn trúng – ánh xanh lá nhỏ bay về người (hồi máu); dưới 30 % máu – hào quang đỏ quanh người, `attack` nhanh hơn 25 %.

---

## 4. Thống soái (nam · thương dài + cờ lệnh · tầm 0.9 · tốc đánh 1.0)

### 4.1 Lớp PSD hướng trước-phải (`F_`, dưới → trên)
`F_banner_cloth` (màu đội) · `F_banner_pole` · `F_cape_2` · `F_cape_1` · `F_hand_far` · `F_arm_far_lower` · `F_arm_far_upper` · `F_shoulder_guard_far` · `F_leg_far_upper` · `F_leg_far_lower` · `F_foot_far` · `F_skirt_back` · `F_body` (giáp) · `F_pelvis` (đai đầu hổ) · `F_leg_near_upper` · `F_leg_near_lower` · `F_foot_near` · `F_skirt_front` · `F_neck` · `F_head` · nhóm `F_face` (mặt + râu: `calm`, `shout`, `hurt`, `ko`) · `F_helmet` · `F_team_plume` · `F_arm_near_upper` · `F_arm_near_lower` · `F_spear` · `F_hand_near` · `F_shoulder_guard_near`.

Hướng sau (`B_`): bỏ nhóm mặt (giữ gáy, mũ); `B_cape_*` che gần hết lưng và nằm **trên** thân; cán cờ + vải cờ nằm trên cùng.

### 4.2 Xương
```
root
└ hip
  ├ spine → chest → neck → head → helmet → plume
  │         ├ banner_pole → banner_1 → banner_2
  │         ├ cape_1 → cape_2
  │         ├ arm_near_upper → arm_near_lower → hand_near → spear → spear_tip
  │         └ arm_far_upper  → arm_far_lower  → hand_far
  ├ leg_near_upper → leg_near_lower → foot_near
  ├ leg_far_upper  → leg_far_lower  → foot_far
  ├ skirt_front_1 → skirt_front_2
  └ skirt_back_1  → skirt_back_2
```
Giáp vai `F_shoulder_guard_*` gắn `arm_*_upper`. Thương dài: khi đâm, key `sortingOrder` của `F_spear` + tay gần lên trên thân.

### 4.3 Clip
| Clip | Độ dài | Động tác | Sự kiện | VFX |
|---|---|---|---|---|
| `idle` | 2.0 s | chống thương, ngực phập phồng, vải cờ và áo choàng bay | – | – |
| `run` | 0.7 s | bước nặng, áo choàng tung, cờ rung mạnh | `Step` | – |
| `attack` | 1.0 s | 0–0.4 thu thương về hông · 0.4 đâm thẳng tới · 0.4–1.0 rút về | `Hit` @ 0.4 | marshal_basic: vệt đâm @ `spear_tip`; tia cam khi trúng |
| `hit` | 0.3 s | lùi nửa bước, nghiến răng (`hurt`) | – | common |
| `die` | 1.2 s | quỳ một gối chống thương, cúi đầu, rồi gục (`ko`) | – | – |
| `spawn` | 1.0 s | dậm chân cắm thương xuống đất, cờ bung ra | – | common: cột sáng + vòng sóng @ `root` |
| `command` | 0.6 s | giơ nắm tay trái, gầm lệnh (`shout`) | – | – |
| `victory` | 1.5 s | giơ cao thương, cười lớn, cờ bay | – | – |
| `select` | 3.0 s | đứng uy nghi chống thương, áo choàng bay chậm | – | – |
| `skill2` **Hiệu Triệu** | 0.8 s | giơ thương lên cao, hô lớn (`shout`) | `Cast` @ 0.5 | marshal_rally: cột sáng vàng + vòng triệu hồi @ từng lính được bổ sung |
| `skill3` **Trống Trận** | 0.7 s | dậm chân mạnh, đấm ngực, gầm | `Cast` @ 0.4 | marshal_wardrum: vòng sóng cam @ `root` (bán kính 6); mũi tên cam nhỏ trên đầu quân được tăng sức 6 s |
| `ultimate` **Thiết Kỵ Xung Phong** | 1.2 s | 0–0.5 chĩa thương về hướng nhắm, gầm · 0.5–1.2 giữ tư thế, cờ bay mạnh | `Cast` @ 0.6 | marshal_cavalry: hàng kỵ binh ma lao dọc đường thẳng dài 12 (0.6–1.6 s, code di chuyển), vệt bụi, dấu móng ngựa |

**Nội tại Quân Lệnh Như Sơn**: không có hiệu ứng liên tục; lúc xuất trận cờ bung kèm ánh vàng.

---

## 5. Tổng hợp file VFX cần có cho 4 chủ tướng
| Chủ tướng | Đánh thường | Kỹ năng 2 | Kỹ năng 3 | Tuyệt kỹ | Nội tại |
|---|---|---|---|---|---|
| Mưu sĩ | `strategist_basic` | `strategist_firefield` | `strategist_bagua` | `strategist_eastwind` | `strategist_passive` (vòng hào quang Thao Lược) |
| Cung thủ | `archer_basic` | `archer_multishot` | `archer_arrowrain` | `archer_firearrow` | `archer_passive` (mũi tên xuyên vàng) |
| Kiếm sĩ | `swordsman_basic` | `swordsman_dash` | `swordsman_whirlwind` | `swordsman_swordrain` | `swordsman_passive` (hồi máu, hào quang đỏ) |
| Thống soái | `marshal_basic` | `marshal_rally` | `marshal_wardrum` | `marshal_cavalry` | – |
| Chung | `common`: tia trúng đòn, vòng va chạm, cột sáng xuất trận, sao lấp lánh, vòng vàng khi đầy nộ, **sao choáng** | | | | |

Prompt từng tấm: `docs/art/concepts/VFX_Prompts.md`.
