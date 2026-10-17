# Arena3Q – Mưu sĩ: tạo hình, tách bộ phận và animation (mẫu cho cả 4 chủ tướng)

Phiên bản: v0.2 · 2026-10-17 · **Phong cách đã chốt: phương án A – chibi ~2.5 đầu, render 3D kiểu mô hình figure** theo ảnh mẫu `docs/art/concepts/chutuong1.png` (chỉ là mẫu phong cách, không phải nhân vật trong game). Bản đồ vẫn vẽ tay.

Đi kèm: `docs/Tuong/LordHeroes.md` (kỹ năng Mưu sĩ §2.3), `docs/Tuong/LordArtPrompts.md` (quy trình chung, VFX, icon, chân dung). Làm xong Mưu sĩ theo tài liệu này thì 3 chủ tướng còn lại làm y hệt, chỉ đổi khối nhân vật.

**Thứ tự công việc:**
1. Tạo ảnh bằng AI (§2) – bạn.
2. Tách bộ phận, ghép PSD trong **Krita** (§3) – bạn.
3. Gắn xương trong **Unity Skinning Editor** (§4.2) – bạn, theo đúng tên xương ở §4.1 (phần này Unity bắt buộc làm tay).
4. Cài import, dựng prefab, IK, đổi mặt, Animator, clip mẫu và code nối vào `HeroUnit` (§4.3, §5, §6) – mình.

---

## 1. Đặc tả tạo hình

### 1.1 Nhân vật
| Mục | Đặc tả |
|---|---|
| Vai trò | Khống chế vùng, tăng sức đội lính; đánh thường tầm trung (3.0) bằng **luồng khí phóng ra từ quạt** |
| Giới tính / tuổi | Nữ, khoảng 25 tuổi |
| Thần thái | Điềm tĩnh, thông minh, tự tin; mặc định **mỉm cười nhẹ**, mắt sắc |
| Tỉ lệ | Chibi **~2.5 đầu**, đầu to, mắt to (như `chutuong1`) |
| Dáng | Mảnh, đứng thẳng, thanh lịch; **không mặc giáp** – khác hẳn 3 chủ tướng còn lại |

### 1.2 Hình bóng – phải nhận ra khi nhân vật chỉ cao ~200 px
- **Tam giác rộng dưới**: áo dài chấm mắt cá + **tay áo rộng** thõng ngang hông.
- **Đầu**: búi tóc cao + **trâm trăng khuyết** bạc nhô lên.
- **Quạt lông hạc** ở tay phải, to ≈ bằng đầu – vật nhận dạng chính.
- Không áo choàng, không mũ; vật lủng lẳng duy nhất là **2 đuôi dải lụa thắt lưng** (dải màu đội).

### 1.3 Trang phục (trong ra ngoài)
1. **Áo trong** màu ngà, cổ giao lĩnh (cổ chéo kiểu Hán phục).
2. **Áo ngoài** dài màu **tím oải hương**, **tay áo rộng** (bàn tay vẫn lộ ra), viền **tím đậm** hoa văn **mây cuộn** vàng nhạt; **vạt trước xẻ** để bước đi.
3. **Thắt lưng** vải bản to + **2 đuôi dải lụa** phía trước – **màu xám nhạt trung tính** (game tô lại màu đội).
4. **Ngọc bội bát quái** ngọc bích treo trước thắt lưng.
5. **Thẻ tre binh thư** cuộn, buộc dây, treo **hông trái**.
6. **Giày** vải mũi mây màu ngà viền tím.
7. **Tóc** đen nâu: búi cao + 2 lọn tóc hai bên mặt; **trâm trăng khuyết** bạc.
8. **Quạt lông hạc**: lông trắng đầu xám, cán ngọc bích, tua nhỏ tím.

### 1.4 Bảng màu
| Phần | Màu | Mã |
|---|---|---|
| Áo ngoài | Tím oải hương | `#B9A3D6` |
| Viền, đai | Tím đậm | `#6B4E8C` |
| Áo trong, giày | Trắng ngà | `#F2EAD8` |
| Hoa văn / trâm | Vàng nhạt / bạc | `#C9A55A` / `#D9DCE3` |
| Ngọc bội, cán quạt | Ngọc bích | `#6FB59B` |
| Tóc | Đen nâu | `#2E2320` |
| Dải màu đội | Xám nhạt trung tính | `#BDBDBD` |

### 1.5 Ràng buộc để tách bộ phận và gắn xương
- **Bàn tay luôn lộ ra** khỏi tay áo; **không gì che mặt**.
- Tay áo, đuôi dải lụa, lọn tóc **mép rõ**, không dính vào thân.
- **Phản chiếu nhẹ**: render 3D có vệt sáng "vẽ chết"; khi bộ phận xoay, vệt sáng xoay theo trông giả → luôn ghi `subtle reflections`.
- Trong game: cao **≈ 1.6 đơn vị ≈ 205 px** (map spec §8.5); PSD vẽ **gấp đôi** – nhân vật cao **≈ 420 px**, import PPU 256.

**Gần / xa:** ở cả hướng **trước-phải** và **sau-phải**, **bên phải** của nhân vật là bên **gần camera** → tay cầm quạt luôn ở phía trước. Tài liệu gọi **near** = bên phải (gần), **far** = bên trái (xa). Khi lật gương ra trái, game tự đảo.

---

## 2. Prompt tạo ảnh

**Toàn bộ prompt (đã ghép sẵn, dán nguyên văn) nằm cạnh ảnh mẫu: `docs/art/concepts/MuuSi_Prompts.md`.** Ảnh tạo ra cũng lưu vào `docs/art/concepts/`.

| # | Ảnh | Dùng để |
|---|---|---|
| 1 | Bảng xoay 4 hướng | ảnh gốc – mọi ảnh sau phải giống nhân vật này |
| 2 | Tư thế chuẩn trước-phải | tách bộ phận PSD hướng trước (§3.1) |
| 3 | Tư thế chuẩn sau-phải | tách bộ phận PSD hướng sau (§3.2) |
| 4 | 4 biểu cảm | nhóm lớp `F_face` (đổi mặt trong game) |
| 5 | Vá phần bị che | hoàn thiện bộ phận bị che khi cắt trong Krita (§3.3 bước 4) |
| 6 | Tư thế tham khảo (tuỳ chọn) | mình dựa vào để dựng clip (§5) |

VFX, icon kỹ năng, chân dung thẻ, ảnh màn chọn: prompt mẫu ở `LordArtPrompts.md` §6, §6b, §8 – dùng khối phong cách A (`LordArtPrompts.md` §3).

---

## 3. Tách bộ phận và ghép PSD trong Krita

### 3.1 Danh sách lớp hướng TRƯỚC-PHẢI (`F_`), từ **dưới cùng** (xa nhất) lên **trên cùng**
| # | Lớp | Ghi chú |
|---|---|---|
| 1 | `F_sash_tail_far` | đuôi dải lụa xa – xám (màu đội) |
| 2 | `F_hand_far` | bàn tay trái, xoè |
| 3 | `F_arm_far_lower` | cẳng tay trái |
| 4 | `F_arm_far_upper` | cánh tay trên trái |
| 5 | `F_sleeve_far` | tay áo rộng trái (tách khỏi cánh tay) |
| 6 | `F_foot_far` | bàn chân + giày trái |
| 7 | `F_leg_far` | ống chân trái (phần lộ dưới áo, có thể rất ít) |
| 8 | `F_robe_back` | vạt áo sau |
| 9 | `F_hair_bun` | búi tóc (sau đầu) |
| 10 | `F_body` | thân trên (ngực, cổ áo) |
| 11 | `F_robe_front_far` | vạt áo trước – nửa xa |
| 12 | `F_leg_near` | ống chân phải |
| 13 | `F_foot_near` | bàn chân + giày phải |
| 14 | `F_robe_front_near` | vạt áo trước – nửa gần |
| 15 | `F_belt` | thắt lưng |
| 16 | `F_book` | thẻ tre binh thư |
| 17 | `F_pendant` | ngọc bội bát quái |
| 18 | `F_sash_tail_near` | đuôi dải lụa gần – xám (màu đội) |
| 19 | `F_neck` | cổ |
| 20 | `F_head` | đầu + tai, **không** mặt, **không** tóc mái |
| 21 | nhóm `F_face` | 4 lớp: `F_face_calm` (hiện), `F_face_shout`, `F_face_hurt`, `F_face_ko` (ẩn) |
| 22 | `F_hair_lock_far` | lọn tóc xa |
| 23 | `F_hair_front` | tóc mái / phần tóc trên đầu |
| 24 | `F_hair_lock_near` | lọn tóc gần |
| 25 | `F_hairpin` | trâm trăng khuyết |
| 26 | `F_arm_near_upper` | cánh tay trên phải |
| 27 | `F_arm_near_lower` | cẳng tay phải |
| 28 | `F_fan` | quạt (cả cán) – **tách khỏi bàn tay** |
| 29 | nhóm `F_hand_near` | 2 lớp: `F_hand_near_grip` (nắm quạt, hiện), `F_hand_near_open` (xoè, ẩn – dùng khi phẩy tay tuyệt kỹ) |
| 30 | `F_sleeve_near` | tay áo rộng phải |

### 3.2 Hướng SAU-PHẢI (`B_`)
Giống bảng trên, đổi tiền tố `B_`, với các khác biệt:
- **Không có** nhóm `face`; `B_head` là đầu nhìn từ sau (gáy, tai), `B_hair_bun` nằm **trên** `B_head`, `B_hair_front` đổi thành `B_hair_back`.
- `B_robe_back` nằm **trên** `B_body` (vạt sau ở phía trước mắt người xem); `B_robe_front_*` nằm dưới cùng.
- Thêm `B_sash_knot` (nút thắt lưng sau). `B_book`, `B_pendant` có thể bị che – nếu không thấy thì bỏ.
- Tay phải (near) và quạt vẫn ở phía gần camera.

### 3.3 Các bước trong Krita (5.2 trở lên)
1. **Mở ảnh** `strategist_rig_front.png`. *Image → Scale Image to New Size*: chỉnh để nhân vật cao **≈ 420 px** (chiều cao canvas ≈ 480 px). *Image → Resize Canvas*: chừa ≈ 30 px mỗi bên.
2. **Xoá nền trắng**: công cụ *Contiguous Selection* (cây đũa thần), *Fuzziness* ≈ 15, bấm vào nền → *Select → Grow Selection* 1 px → *Delete*. Đổi tên lớp gốc thành `_ref` (để so, không xuất).
3. **Cắt từng bộ phận** theo bảng §3.1: chọn lớp `_ref`, dùng *Freehand / Polygonal Selection* khoanh bộ phận → **Copy Selection to New Layer** (*Ctrl+Alt+J*) → đặt tên ngay (*F2*). Cắt theo mép thật của bộ phận, không cắt vuông.
4. **Vá phần bị che** (ví dụ thân sau tay áo, vạt áo sau chân): tự tô bằng *Smart Patch Tool* / cọ + lấy màu (*Ctrl*), hoặc dùng prompt **Ảnh 5** rồi dán lại, căn đúng vị trí.
5. **Đầu khớp thừa**: ở mỗi khớp (vai, khuỷu, cổ tay, hông, gối, cổ) vẽ phần cuối bộ phận **dài thêm và bo tròn** chồng **dưới** bộ phận kế tiếp ≈ 10–15 px, để khi xoay không hở khe.
6. **Mặt**: mở `strategist_faces.png`, cắt riêng phần mắt–mày–miệng của từng mặt, dán vào nhóm `F_face`, căn trùng vị trí mặt gốc (giảm *Opacity* tạm để căn), rồi trả *Opacity* 100 %. Xoá mặt trên `F_head` (để da trơn) – mặt chỉ nằm trong nhóm `F_face`.
7. **Sắp thứ tự lớp** đúng bảng §3.1 (lớp trên che lớp dưới). Bật/tắt từng lớp để kiểm tra không có lỗ.
8. **Xoá lớp `_ref`** trước khi xuất.
9. Làm lại các bước cho `strategist_rig_back.png` → bảng §3.2.

### 3.4 Kiểm tra trước khi lưu (Unity đọc sai nếu thiếu)
- *Image → Properties*: **RGB/Alpha 8-bit**, sRGB.
- Mọi lớp là **Paint Layer**: lớp vector / fill / filter → chuột phải *Convert → to Paint Layer*.
- **Không còn mask**: lớp có mask → *Layer → Flatten Layer*.
- Chế độ hoà trộn **Normal**, Opacity **100 %**, không khoá Alpha.
- **Tên lớp duy nhất**, tiếng Anh, đúng bảng (mình dựng clip theo tên); **giữ nguyên tên** khi sửa PSD sau này để Unity không mất xương.
- Các lớp ẩn (mặt, bàn tay xoè) **vẫn giữ** – Unity sẽ nhập cả lớp ẩn.

### 3.5 Lưu
- *File → Save As* → định dạng **Photoshop image (.psd)**: `lord_strategist_front.psd`, `lord_strategist_back.psd`, đặt vào `Assets/Arena3Q/Art/Heroes/Strategist/`.
- File gốc Krita (`.kra`) để ở `ArtSource/Heroes/Strategist/` (ngoài `Assets/`, Unity không đọc).

---

## 4. Unity: gắn xương

### 4.1 Tên xương (bắt buộc đúng – clip mẫu của mình gắn theo tên)
```
root                         (giữa hai bàn chân, trên mặt đất)
└ hip
  ├ spine → chest → neck → head
  │         │              ├ hair_bun
  │         │              ├ hair_lock_near
  │         │              └ hair_lock_far
  │         ├ arm_near_upper → arm_near_lower → hand_near → fan
  │         │                  └ sleeve_near_1 → sleeve_near_2
  │         └ arm_far_upper  → arm_far_lower  → hand_far
  │                            └ sleeve_far_1  → sleeve_far_2
  ├ leg_near_upper → leg_near_lower → foot_near
  ├ leg_far_upper  → leg_far_lower  → foot_far
  ├ robe_front_near_1 → robe_front_near_2
  ├ robe_front_far_1  → robe_front_far_2
  ├ robe_back_1 → robe_back_2
  ├ sash_near_1 → sash_near_2
  ├ sash_far_1  → sash_far_2
  └ book
```
Hướng sau dùng **cùng tên xương** (để một bộ clip gần như dùng chung, mình chỉnh lại góc).

### 4.2 Phần bạn làm trong Skinning Editor (~1–2 giờ mỗi hướng)
1. Mình cài sẵn import (§4.3). Chọn `lord_strategist_front.psd` → Inspector → **Open Sprite Editor** → menu trên cùng chọn **Skinning Editor**.
2. **Tạo xương** (*Create Bone*, phím B): bấm điểm đầu → điểm cuối; giữ thứ tự cha–con như §4.1 (bấm *Esc* để bắt đầu nhánh mới, chọn xương cha trước khi tạo nhánh con). Đặt tên ở cửa sổ **Visibility → Bones** (bấm đúp vào tên).
   - `root` ngắn, nằm giữa hai chân trên mặt đất; `hip` từ thắt lưng lên.
   - Xương tay áo, vạt áo, dải lụa: 2 đoạn dọc theo mảnh vải.
3. **Tạo lưới**: *Auto Geometry* → chọn tất cả sprite → *Generate For All Visible* (Outline Detail ~ 10, Subdivide ~ 20 cho mảnh vải lớn).
4. **Gán xương cho từng mảnh**: *Bone Influence* – mỗi sprite chỉ chịu xương của nó (ví dụ `F_sleeve_near` ← `arm_near_lower`, `sleeve_near_1`, `sleeve_near_2`; `F_robe_front_near` ← `hip`, `leg_near_upper`, `robe_front_near_1/2`).
5. **Trọng số**: *Auto Weights* → *Generate For All*; sau đó *Weight Brush* làm mượt ở khớp, mảnh vải mềm dần từ gốc xuống đuôi.
6. Thử bằng *Preview Pose* (kéo xương) – không rách, không hở; *Reset Pose* → **Apply**.
7. Làm tương tự cho `lord_strategist_back.psd`, báo mình.

### 4.3 Phần mình làm (editor script, chạy lại được)
- **Cài import PSD**: Sprite (2D and UI), Multiple, **PPU 256**, Mosaic, **Character Rig**, **Use Layer Group**, **Include Hidden Layers**, pivot **đáy giữa**, sinh **Sprite Library** từ nhóm lớp (`face`, `hand_near`).
- **Prefab** `Strategist_Front` / `Strategist_Back`: **IK Manager 2D** + **Limb Solver 2D** cho 2 tay, 2 chân (đích `ik_hand_near`, `ik_hand_far`, `ik_foot_near`, `ik_foot_far`), **Sorting Group** để cả nhân vật sắp thứ tự như một khối theo trục Y, **Sprite Resolver** cho mặt và bàn tay, lớp màu đội nhận màu theo phe.
- **Animator Controller** và **clip mẫu** (§5) dựng bằng script theo tên xương; bạn có thể tinh chỉnh trong cửa sổ Animation.

---

## 5. Clip animation của Mưu sĩ

Bảng clip (độ dài, động tác, sự kiện `Hit` / `Cast`, VFX gắn theo sự kiện) của **cả 4 chủ tướng** nằm chung ở **`docs/Tuong/LordAnimations.md`** (Mưu sĩ: mục 1; quy ước chung: mục 0). Tóm tắt: `attack` dài 1.0 s ở nhịp chuẩn, `Hit` @ 0.4 s, chạy với tốc độ = tốc đánh (Mưu sĩ 0.8 → 1.25 s); kỹ năng có hiệu lực ở `Cast`.

---

## 6. Code mình sẽ nối vào game (sau khi có prefab)
- Thành phần hiển thị riêng cho tướng (`HeroView`), chỉ đọc trạng thái `HeroUnit`, **không** đổi luật chơi:
  - đứng / chạy theo vận tốc; **hướng**: đi lên (Y logic tăng) → prefab sau, còn lại → prefab trước; trái / phải → lật gương;
  - `attack` mỗi đòn, tốc độ clip theo tốc đánh; `hit` khi mất máu; `die` khi chết; `spawn` khi triệu hồi; `command` khi nhận lệnh tấn công; `victory` khi thắng;
  - kỹ năng 2, 3, tuyệt kỹ: gọi clip khi hệ thống kỹ năng được code (spec §3, ⏳).
- `HeroDefinition` thêm ô chọn prefab hiển thị; tướng chưa có prefab vẫn dùng hình placeholder như hiện nay.
- Kích thước: nhân vật cao 1.6 đơn vị; bóng đổ dưới chân do game vẽ (không vẽ trong PSD).
- Kiểm tra: test PlayMode đổi trạng thái → đúng clip, đúng hướng; chụp ảnh màn hình trận để so với `map1`.

---

## 7. Gửi mình
- [ ] Ảnh theo `docs/art/concepts/MuuSi_Prompts.md`: `docs/art/concepts/strategist_turnaround.png`, `strategist_rig_front.png`, `strategist_rig_back.png`, `strategist_faces.png` – gửi ngay khi có, mình xem trước khi bạn tách.
- [ ] `Assets/Arena3Q/Art/Heroes/Strategist/lord_strategist_front.psd`, `lord_strategist_back.psd` (qua kiểm tra §3.4).
- [ ] Sau khi gắn xương (§4.2): báo mình để dựng prefab, Animator, clip và code.
