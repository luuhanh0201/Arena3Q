# Arena3Q – Prompt hình ảnh và danh sách animation cho 4 chủ tướng

Phiên bản: v0.4 · 2026-10-17 (đổi phong cách sang chibi render 3D – phương án A; trước đó: bỏ tên riêng, icon kỹ năng, kỹ năng 2/3, animation theo tốc đánh). Đi kèm `docs/LordHeroes.md` (thiết kế, kỹ năng) và `docs/StrategistArtGuide.md` (mẫu chi tiết trên Mưu sĩ).

> **Phong cách nhân vật đã đổi (người dùng chốt 2026-10-17): phương án A – chibi ~2.5 đầu, render 3D kiểu mô hình figure, theo ảnh mẫu `docs/art/concepts/chutuong1.png`.** Bản đồ vẫn vẽ tay (map spec §8.1). Hướng dẫn chi tiết làm mẫu trên Mưu sĩ: `docs/StrategistArtGuide.md`; prompt dán sẵn để cạnh ảnh mẫu trong `docs/art/concepts/` (`MuuSi_Prompts.md`, các chủ tướng sau đặt tên tương tự).

Đã chốt ✅: Đông Á cổ trang · **chibi render 3D (phương án A)**, góc 3/4 · animation **skeletal** (Unity 2D Animation, đã có trong project) từ **PSD tách bộ phận** · **2 hướng** (trước-phải, sau-phải) + lật gương ra trái · 2 nam + 2 nữ.

> **Ý tưởng chính:** với skeletal, mỗi nhân vật chỉ cần vẽ **một lần cho mỗi hướng** nhưng **tách rời từng bộ phận** (đầu, thân, tay trên, tay dưới, bàn tay, đùi, ống chân, áo choàng, vũ khí…). Unity gắn xương vào các bộ phận và làm **mọi động tác** (đứng, đi, đánh, chết, kỹ năng…). Vì vậy ta **không** cần AI vẽ từng khung hình – việc mà AI làm rất kém (nhân vật đổi mặt, đổi áo giữa các khung).

---

## 1. Nên dùng công cụ nào? (đánh giá cho đúng việc này)

Việc này cần 3 thứ: (1) **làm đúng chỉ dẫn** (đúng danh sách bộ phận, đúng góc nhìn, tách rời từng phần), (2) **giữ một nhân vật giống hệt nhau** qua nhiều ảnh (concept, bộ phận hướng trước, bộ phận hướng sau, chân dung, kỹ năng), (3) **nền trong suốt**.

| Tiêu chí | ChatGPT (tạo ảnh) | Gemini (tạo / sửa ảnh) | Midjourney | Stable Diffusion / ComfyUI |
|---|---|---|---|---|
| Làm đúng chỉ dẫn chi tiết (danh sách bộ phận, bố cục bảng) | **Tốt** | **Tốt** | Kém – đẹp nhưng hay tự ý | Trung bình – cần ControlNet |
| Giữ nhân vật nhất quán | **Tốt** khi sửa tiếp trên ảnh cũ trong cùng cuộc trò chuyện | **Tốt** (mạnh về sửa ảnh theo ảnh tham chiếu) | Khá (tham chiếu nhân vật) | **Rất tốt** nếu train LoRA riêng, nhưng tốn công |
| Nền trong suốt | Có tuỳ chọn | Thường phải tách nền sau | Không – phải tách nền | Có (với tiện ích phù hợp) |
| Độ đẹp / "chất game" | Tốt | Tốt | **Đẹp nhất** | Tuỳ model |
| Chi phí cài đặt | Thấp | Thấp | Thấp | Cao (cần GPU mạnh, nhiều cấu hình) |

**Khuyến nghị:**
- **Công cụ chính: ChatGPT hoặc Gemini** – hợp nhất với kiểu việc "vẽ đúng một nhân vật, tách đúng các bộ phận, giữ nguyên qua nhiều ảnh". Làm **mỗi chủ tướng trong một cuộc trò chuyện riêng**, luôn gửi lại ảnh concept đã chốt khi xin ảnh tiếp theo.
- **Midjourney (tuỳ chọn):** chỉ để **khám phá concept** ban đầu nếu muốn nhiều phương án đẹp; sau đó đưa ảnh đã chọn sang ChatGPT/Gemini để làm bộ phận.
- **Stable Diffusion:** chỉ đáng khi cần sản xuất **rất nhiều** ảnh cho cùng nhân vật (train LoRA riêng).
- Tính năng các công cụ thay đổi nhanh – trước khi làm hàng loạt, thử một nhân vật trên 2 công cụ và giữ công cụ cho kết quả ổn định hơn.

---

## 2. Quy trình cho mỗi chủ tướng

1. **Ảnh** theo `docs/art/concepts/<ChuTuong>_Prompts.md`: bảng xoay 4 hướng (ảnh gốc) → tư thế chuẩn trước-phải và sau-phải → 4 biểu cảm → (vá phần bị che khi cắt) → tư thế tham khảo.
2. **Tách bộ phận và ghép PSD** trong Krita – làm theo mẫu `docs/StrategistArtGuide.md` §3; danh sách lớp riêng từng chủ tướng: `docs/LordAnimations.md`.
3. **Gắn xương** trong Unity Skinning Editor – `StrategistArtGuide.md` §4; tên xương riêng: `LordAnimations.md`.
4. Mình dựng prefab, Animator, clip, VFX và code (`LordAnimations.md`, `StrategistArtGuide.md` §6).
5. **VFX, icon, chân dung**: `VFX_Prompts.md`, `Icon_Prompts.md`, `ChanDung_Prompts.md` (cùng thư mục prompt) – làm sau khi đã chốt bảng xoay.

---

## 3. Khối phong cách nhân vật (dán vào đầu MỌI prompt, đính kèm `chutuong1.png`)

```
Match the rendering style, proportions and level of detail of the attached reference image
(stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon.
Chibi proportions about 2.5 heads tall, large head, big expressive eyes, small body, ancient East Asian setting.
Soft studio lighting from the top-left, gentle rim light, smooth clean materials,
subtle reflections (no strong shiny spots), no outline, clean bold silhouette that reads at small size.
No text, no watermark, no background scenery.
```
Góc nhìn ghi riêng trong từng prompt (bảng xoay: ngang tầm mắt; ảnh dùng trong game: trước-phải 3/4 nhìn xuống ~40°).

Kích thước dùng trong game: chủ tướng cao ≈ **1.6 đơn vị** (map spec §8.5). Để gắn xương nét, vẽ **gấp đôi**: nhân vật cao **≈ 420 px** trong PSD và import với PPU 256 (mình cài khi nhận file).

---

## 4. Tấm bộ phận tách rời (dùng cho cả 4 chủ tướng)

Đây là prompt quan trọng nhất. Gửi kèm ảnh concept đã chốt.
```
[khối phong cách ở mục 3]
Using the attached character exactly (same face, outfit, colours and weapon), create a character PARTS SHEET
for 2D skeletal animation, facing FRONT-RIGHT (3/4 view). Every part drawn separately with clear empty space
between parts, all on a plain transparent background, nothing overlapping:
head (no hair), back hair, front hair or hat, neck, torso (chest), pelvis/waist with belt,
cloak in 2 pieces (upper, lower), right upper arm, right forearm, right hand open, right hand gripping,
left upper arm, left forearm, left hand open, left hand gripping, right thigh, right shin with boot, right foot,
left thigh, left shin with boot, left foot, main weapon, secondary item, [phụ kiện riêng của nhân vật],
and a separate TEAM-COLOUR piece: [dải màu đội] painted in neutral light grey.
Limbs drawn slightly longer at the joints (rounded ends) so they can rotate without gaps.
Same scale for every part.
```
Sau đó xin lại y hệt với **"facing BACK-RIGHT (3/4 view from behind)"** – hướng sau dùng khi nhân vật đi lên phía trên màn hình.

**Lớp màu đội (team colour):** một mảnh vẽ **xám nhạt trung tính** (dải lụa, chùm lông mũ, tua cờ…) để game tô lại **xanh** (quân ta) hoặc **đỏ** (đối thủ khi có PvP) mà không đổi màu chủ đạo của nhân vật.

**Biểu cảm (ảnh D):**
```
[khối phong cách ở mục 3]
Using the attached character, an expression sheet of the SAME head facing front-right, 4 separate heads
on a transparent background: calm, shouting a command, hurt (wincing), knocked out (eyes closed).
Identical size and angle for all four.
```

---

## 5. Từng chủ tướng – prompt dán sẵn

Prompt đầy đủ (phong cách A, đã ghép sẵn, dán nguyên văn) cho từng chủ tướng nằm cạnh ảnh mẫu trong `docs/art/concepts/`. Mỗi file gồm: tóm tắt + bảng màu, 6 prompt (bảng xoay, tư thế chuẩn trước-phải, sau-phải, biểu cảm, vá phần bị che, tư thế tham khảo) và **khác biệt khi tách bộ phận / gắn xương** so với mẫu Mưu sĩ (`docs/StrategistArtGuide.md`).

| Chủ tướng | File prompt | Tiền tố ảnh | Dải màu đội |
|---|---|---|---|
| Cung thủ (nữ) | `CungThu_Prompts.md` | `archer_` | ruy băng tóc + ruy băng buộc cung |
| Kiếm sĩ (nam) | `KiemSi_Prompts.md` | `swordsman_` | băng trán + 2 đuôi |
| Mưu sĩ (nữ) | `MuuSi_Prompts.md` | `strategist_` | 2 đuôi dải lụa thắt lưng |
| Thống soái (nam) | `ThongSoai_Prompts.md` | `marshal_` | chùm lông mũ + vải cờ lệnh |

Mục 4 (tấm bộ phận tách rời do AI vẽ) là cách cũ – nay ưu tiên **ảnh tư thế chuẩn rồi tự cắt trong Krita** (`StrategistArtGuide.md` §3), dùng tấm bộ phận chỉ khi cần tham khảo.

---

## 6. Hiệu ứng kỹ năng (VFX)

Prompt dán sẵn: **`docs/art/concepts/VFX_Prompts.md`** (17 tấm: hiệu ứng chung, đánh thường của 4 chủ tướng, 12 kỹ năng). Hiệu ứng **không** vẽ từng khung: mình dựng bằng hệ thống hạt của Unity từ các mảnh nền trong suốt (hoặc nền đen, dùng chế độ cộng sáng); hình nằm trên mặt đất vẽ nhìn thẳng từ trên xuống, game tự nghiêng theo góc 3/4. Tên file `vfx_<chủ tướng>_<kỹ năng>_<mảnh>.png`.

## 6b. Icon kỹ năng (hiện trên thẻ tướng)

Prompt dán sẵn: **`docs/art/concepts/Icon_Prompts.md`** (16 icon: nội tại, kỹ năng 2, kỹ năng 3, tuyệt kỹ × 4 chủ tướng). Icon tĩnh 256 × 256, tròn, đọc được ở ≈ 44 px; cùng khung viền đồng, **tuyệt kỹ viền vàng**; nền đĩa theo màu chủ đạo. Đếm ngược hồi chiêu và thanh nộ do game vẽ (spec §3). Tên file `icon_<chủ tướng>_<passive|skill2|skill3|ultimate>.png`. Tướng thường cũng có tuyệt kỹ – icon của họ làm theo cùng mẫu khi thiết kế xong bộ kỹ năng.

## 7. Danh sách animation (mình làm trong Unity bằng skeletal)

Bộ clip, độ dài, mốc `Hit` / `Cast`, xương, lớp PSD và VFX gắn theo sự kiện của **cả 4 chủ tướng**: **`docs/LordAnimations.md`**. Prompt tư thế tham khảo: ảnh 6 trong từng file `<ChuTuong>_Prompts.md`.
---

## 8. Chân dung và ảnh màn chọn

Prompt dán sẵn: **`docs/art/concepts/ChanDung_Prompts.md`** – chân dung thẻ `portrait_<chủ tướng>.png` (320 × 420, mặt 3/4 sang phải) và ảnh màn chọn `splash_<chủ tướng>.png` (dọc 2 : 3, tư thế tuyệt kỹ) cho cả 4 chủ tướng. Làm sau khi đã chốt bảng xoay của chủ tướng.

---

## 9. Hậu kỳ trước khi gửi (bắt buộc)
1. Cắt từng bộ phận ra **lớp riêng**, xoá nền, giữ mép mềm (không viền trắng).
2. Ghép lại nhân vật ở **tư thế đứng mặc định** (đúng vị trí, đúng thứ tự trước/sau) – mỗi bộ phận một lớp, các khớp chồng lên nhau một chút.
3. Kích thước: nhân vật cao ≈ **420 px**; mọi bộ phận cùng tỉ lệ.
4. **Đặt tên lớp** rõ ràng, tiếng Anh, theo hướng: `F_` (trước-phải) / `B_` (sau-phải), bên phải nhân vật = `near` (gần camera), bên trái = `far`; ví dụ `F_head`, `F_arm_near_upper`, `F_hand_near_grip`, `F_fan`, `F_sash_tail_near`, `F_face_shout`. Danh sách lớp và tên xương mẫu: `docs/StrategistArtGuide.md` §3–§4.
5. Một file PSD cho mỗi hướng: `lord_archer_front.psd`, `lord_archer_back.psd` (tương tự `swordsman`, `strategist`, `marshal`).
6. Hiệu ứng: PNG nền trong suốt, mỗi mảnh một file, tên `vfx_<chủ tướng>_<kỹ năng>_<mảnh>.png`.

## 10. Câu hỏi còn mở
- ~~Tỉ lệ 3.5 đầu?~~ → **Đổi sang ~2.5 đầu, chibi render 3D (phương án A)** (người dùng chốt 2026-10-17, sau khi thử 3 phong cách).
- ~~Skin?~~ → **Đã chốt: có, để sau (⏳)** – khi làm, giữ đúng cùng bộ khung bộ phận để dùng chung animation.
