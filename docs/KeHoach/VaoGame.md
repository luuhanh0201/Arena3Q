# Vào game: từ lúc chạm biểu tượng tới cốt truyện mở đầu – kế hoạch và prompt

Phiên bản: v0.1 · 2026-10-17 · Trạng thái: **🟡 kế hoạch chờ duyệt** · code ⏳ (spec §11: đăng nhập, máy chủ, màn chọn chủ tướng, cấp / kỹ năng chưa làm ở prototype).
**Plan tạm** trong `docs/KeHoach/` để bạn duyệt: plan và prompt nằm chung một file. Duyệt xong, phần prompt chuyển vào `docs/art/concepts/` (quy ước: mọi prompt nằm cạnh ảnh mẫu), phần luật vào spec.
Phong cách: **A – chibi ~2.5 đầu, render 3D** theo ảnh mẫu `docs/art/concepts/chutuong1.png` – mọi chỗ "đính kèm `chutuong1.png`" là file này. Công cụ: **ChatGPT** (ảnh), **Kling / Veo / Sora** (video), **ElevenLabs Sound Effects** (tiếng), **Suno / Udio** (nhạc). Kết quả lưu **vào `docs/art/concepts/`** (cùng chỗ với ảnh mẫu và ảnh nhân vật) với tên ghi dưới mỗi prompt.

Dùng lại lựa chọn đã có (2026-10-17): intro làm bằng **video AI**, logo chữ **"Arena3Q"**, **có âm thanh**. Nội dung intro theo yêu cầu mới: **logo + màn hình banner** (thay cho "camera bay qua chiến trường" của bản đã bỏ).
Liên quan: cấp, kỹ năng học theo cấp, sao, phe – `docs/KeHoach/HeroProgression.md` · chủ tướng – `docs/Tuong/LordHeroes.md` · tướng khởi đầu – `docs/Tuong/NormalHeroes.md` · quân Hắc Phong Trại – `docs/QuanDich/Chuong1_SonTac.md`.

---

## 0. Luồng màn hình

| # | Màn | Người chơi làm gì | Quy tắc chính | Ảnh / tiếng (mục) |
|---|---|---|---|---|
| 1 | **Biểu tượng ứng dụng** | chạm để mở game | – | `app_icon` (§2) |
| 2 | Màn khởi động của Unity | chờ ~1 s | nền tối `#231F20` như cài đặt hiện tại; Unity 6 cho tắt logo "Made with Unity" (Player Settings → Splash Image) – mình kiểm tra khi làm | – |
| 3 | **Intro ~3 s** | xem; từ lần thứ hai **chạm để bỏ qua** | hiện logo rồi mở ra **banner**; cảnh cuối đứng yên trên banner và chuyển thẳng sang màn đăng nhập | §3, §4, §5 |
| 4 | (⏳) Kiểm tra cập nhật / tải dữ liệu | chờ | thanh tiến trình trên nền banner – chỉ khi có tải nội dung từ máy chủ | banner |
| 5 | **Đăng nhập / đăng ký** | Google · tài khoản + mật khẩu · đăng ký · **Chơi ngay** | §6 | UI kit §7 |
| 6 | **Chọn máy chủ** | dùng máy chủ đề xuất hoặc chọn khác → Vào game | §8 | UI kit §7 |
| 7 | **Chọn chủ tướng** → xác nhận → **đặt tên** | chọn 1 trong 4 (cố định cả game) | §9 | `bg_lord_select`; chân dung và splash đã có prompt |
| 8 | **Cốt truyện mở đầu** | chạm để qua từng câu, có nút **Bỏ qua** | 6 khung tranh + hộp thoại | §10 |
| 9 | **Ải hướng dẫn 1-1** "Giải vây Thanh Thạch" | chơi | ải Thủ dễ (ít quân địch); đội hình: chủ tướng + **Giáp Sĩ 3★** (có tuyệt kỹ) + Xạ Thủ 1★; dạy kéo thẻ triệu hồi, chạm đất ra lệnh đi, vuốt vào địch ra lệnh đánh | bản đồ hiện có |
| 10 | Thắng ải → chủ tướng **lên Lv2** | đọc thông báo | "Học được kỹ năng nội tại!" – chủ tướng bắt đầu chưa có kỹ năng (`docs/KeHoach/HeroProgression.md` §6) | icon kỹ năng (đã có prompt) |

**Người chơi cũ:** intro (bỏ qua được) → tự đăng nhập → màn **"Chạm để vào game"** với máy chủ lần trước → sảnh chính (⏳ chưa thiết kế).
**Prototype offline:** chỉ có **Chơi ngay** và **một máy chủ giả**; đăng nhập Google / tài khoản cần backend (⏳).

---

## 1. Quy ước chung cho ảnh giao diện

- Màn hình **ngang**, giao diện thiết kế theo **1920 × 1080** (giống HUD trận: `CanvasScaler` 1920 × 1080). Ảnh nền vẽ **2400 × 1080** để phủ cả điện thoại dài 20 : 9. Ảnh nền luôn phủ kín màn hình nên mỗi loại máy **cắt bớt hai bên** khác nhau: điện thoại 16 : 9 thấy 1920 × 1080 ở giữa, máy tính bảng 4 : 3 (iPad) chỉ thấy **1440 × 1080 ở giữa** → **nhân vật và chi tiết quan trọng nằm trong 1440 × 1080 ở giữa**, phần ngoài chỉ là cảnh nối dài.
- **Ảnh không có chữ** (AI hay viết sai): chữ trên nút, tên, lời thoại do game hiện bằng font. Font phải **đủ dấu tiếng Việt** (ví dụ *Be Vietnam Pro* hoặc *Noto Sans* cho chữ thường, một font tiêu đề kiểu thư pháp có dấu); dùng font giấy phép mở (OFL).
- **Màu đội ta là xanh**: trong ảnh quảng bá (banner, truyện), dải màu đội xám `#BDBDBD` của nhân vật được tô **xanh lam hoàng gia** (royal blue).
- Tên file: `app_icon*`, `logo_*`, `banner_*`, `intro_*`, `ui_*`, `bg_*`, `story_*`, `portrait_npc_*`, `bgm_*`. Mình kiểm tra rồi chuyển vào `Assets/Arena3Q/Art/UI/` (nhạc vào `Assets/Arena3Q/Audio/`).
- **Dung lượng app** (CLAUDE.md: mỗi lần build báo dung lượng) – ước tính phần này thêm vào bản build:

| Nội dung | Ước tính |
|---|---|
| `intro.mp4` 3 s, 1080p, H.264 | ≤ 3 MB |
| 2 ảnh nền 2400 × 1080 (banner, chọn chủ tướng) – nén ASTC 6×6 | ~1.2 MB mỗi ảnh |
| 9 khung truyện 1920 × 1080 – nén ASTC 8×8 | ~0.5 MB mỗi ảnh → ~4.7 MB |
| UI kit, icon, logo | ~1.5 MB |
| 2 bản nhạc 60 s (OGG) | ~1 MB mỗi bản |
| **Tổng** | **~13 MB** |

---

## 2. Biểu tượng ứng dụng → `app_icon.png` (1024 × 1024)
Đính kèm: `chutuong1.png`
```
A mobile game app icon, 1024x1024, square, no transparency, matching the stylised 3D-rendered figurine look of the attached reference image (soft studio lighting, gentle rim light, subtle reflections, no outline).
A bold simple emblem filling the square: a crossed straight sword and long spear behind a small round bronze shield with a golden glow, on a deep crimson background with a soft radial light and a few golden sparkles. Very simple shapes and strong contrast so it reads at 48 pixels. No text, no letters, no numbers, no border, no rounded corners, no watermark.
```
**Biểu tượng thích ứng Android** (2 lớp, máy tự cắt tròn / bo góc): tạo thêm `app_icon_fg.png` bằng câu dưới, lớp nền là màu đỏ thẫm trơn.
```
The same emblem as the attached icon on a fully transparent background, centred, the whole emblem fitting inside the central 60 % of the square, 1024x1024. No background, no text.
```

---

## 3. Logo "Arena3Q" → `logo_arena3q.png` (≈ 1600 × 700, nền trong suốt)
Đính kèm: `chutuong1.png`
```
A game logo for a cute mobile strategy game set in ancient East Asia, transparent background, centred, horizontal layout about 16:7.
The word "Arena3Q" spelled exactly A-r-e-n-a-3-Q, bold chunky 3D letters in polished gold with a dark red outline and a soft inner glow, matching the glossy stylised 3D materials of the attached reference image.
Behind the letters: a crossed sword and spear and a small round bronze shield (the same emblem as the app icon), a red silk ribbon banner under the word (the ribbon left empty, no extra text), small golden sparkles.
Clean readable shapes that stay legible on a phone screen. No other text, no watermark.
```
Chữ phải đúng **"Arena3Q"**. Nếu AI viết sai 2–3 lần, tạo **huy hiệu không chữ** (`logo_emblem.png`) bằng câu dưới rồi gửi mình ghép chữ bằng font trong Krita:
```
The same logo artwork as described, but with NO letters at all: only the crossed sword and spear, the round bronze shield, the empty red silk ribbon and golden sparkles, transparent background, centred.
```

---

## 4. Banner – màn hình tiêu đề → `banner_title.png` (2400 × 1080)
Đính kèm: `chutuong1.png` + `archer_turnaround.png` + `swordsman_turnaround.png` + `strategist_turnaround.png` + `marshal_turnaround.png`
Làm **sau khi** đã chốt ảnh bảng xoay của 4 chủ tướng. Banner là **cảnh cuối của intro** và **nền màn đăng nhập, chọn máy chủ**.
```
Key art banner for a cute mobile strategy game set in ancient East Asia, landscape, 2400x1080, in the stylised 3D-rendered chibi figurine look of the attached character images (soft studio lighting, gentle rim light, subtle reflections, no outline).
The four lord heroes from the attached turnaround sheets stand together in heroic poses on a small rocky hill in the centre: the young female archer drawing her tall horn bow (left), the young male swordsman with his long sword raised (centre-left), the young female strategist opening her crane-feather fan (centre-right), the bearded grand marshal planting his spear with his command banner flying (right). Exactly the same faces, outfits and colours as the attached sheets, except that their neutral grey team ribbons, plume and banner cloth are painted royal blue.
Behind them a wide battlefield at sunrise: a winding river with a wooden bridge, small chibi armies with blue banners, distant castles and misty mountains, warm golden light rays, drifting embers and petals.
The upper middle area is open bright sky kept EMPTY for the game logo. The bottom 30 % is a calm, slightly darker grassy foreground with no important details (buttons will be placed there). Keep the four heroes close together inside the central 1440x1080 area; the left and right edges are only extra scenery. No text, no letters, no logo, no UI, no watermark.
```

---

## 5. Intro ~3 giây → `intro.mp4` + `intro.ogg`

| Mục | Thông số |
|---|---|
| Thời lượng | **3.0 s** (tạo dài hơn rồi cắt) |
| Khung hình | ngang 16 : 9, **1920 × 1080**, 30 fps; vùng an toàn như banner (điện thoại 20 : 9 cắt bớt trên – dưới) |
| File | `intro.mp4` (H.264) **≤ 3 MB** · tiếng `intro.ogg` hoặc `.wav` |
| Chữ | video **không có chữ**; logo là ảnh `logo_arena3q.png`, game hiện đè lên (sắc nét, đúng chữ) |

**Kịch bản**
| Thời gian | Hình (video) | Logo (game làm) | Tiếng |
|---|---|---|---|
| 0.0–1.0 s | màn tối, đốm lửa vàng cam bay lên, khói mỏng; giữa khung ấm dần | – | một tiếng **trống trận** sâu, gió |
| 1.0–1.4 s | **bùng sáng vàng** từ giữa, vòng khói và tia lửa toả ra | logo hiện ở giữa, loé sáng | tiếng vút lên + **chiêng** |
| 1.4–2.2 s | khói tan, **lộ ra banner** (4 chủ tướng trên đồi, chiến trường bình minh) | logo thu nhỏ, bay lên vị trí trên-giữa | ngân dài, tiếng gió nhẹ |
| 2.2–3.0 s | cảnh **gần như đứng yên** (cờ, áo choàng lay nhẹ, đốm sáng bay) – gần giống `banner_title.png` | logo đứng yên | tiếng ngân tắt dần |
→ Hết 3 s: nút đăng nhập trượt lên, nền vẫn là banner. Video AI không ra **đúng từng điểm ảnh** như banner, và video 16 : 9 bị phóng khác ảnh 2400 × 1080 trên máy 20 : 9 → mình **hoà dần 0.4 s cuối** từ video sang ảnh `banner_title.png` tĩnh để không giật hình.

**Quy trình:** cắt `banner_title.png` lấy phần **1920 × 1080 ở giữa** → `banner_title_16x9.png`; tạo `intro_frame_start.png` (5a) → dùng nó làm **khung đầu** và `banner_title_16x9.png` làm **khung cuối** để tạo video (5b) → tiếng (5c, hoặc dùng tiếng Veo tạo cùng video) → gửi mình. Mình dựng intro trong Unity: phát video vừa màn hình, hiện logo theo kịch bản, phát tiếng, chạm để bỏ qua (từ lần thứ hai).
Video AI thường dài 5–8 s: tạo bản dài rồi **cắt / tua nhanh** còn 3 s (CapCut, DaVinci Resolve). Kiểm tra **điều khoản dùng thương mại** của công cụ trước khi đưa vào bản phát hành.

### 5a. Khung đầu → `intro_frame_start.png`
Đính kèm: `banner_title.png`
```
Cinematic key frame, landscape 16:9, 1920x1080, matching the colours and rendering style of the attached banner image.
An almost black night scene with drifting glowing golden-orange embers and thin smoke wisps, a faint warm glow in the exact centre of the frame as if a fire is about to burst out. No characters, no text, no letters, no logo, no watermark.
```

### 5b. Video
Công cụ: **Kling** hoặc **Veo** (ảnh → video, khung đầu + khung cuối) · Khung đầu `intro_frame_start.png` · Khung cuối `banner_title_16x9.png`
```
A 3-second cinematic intro for a cute mobile strategy game set in ancient East Asia, landscape 16:9.
Start in darkness with drifting golden embers (first frame). After about one second a brilliant golden light bursts from the centre; a ring of warm smoke and sparks sweeps outward and clears away, revealing the heroic scene of the last frame: four chibi heroes on a hill above a sunrise battlefield. In the final second the scene is almost still: banners and capes flutter gently, embers drift, light rays shimmer, and the frame ends exactly on the last frame.
The bright sky in the upper middle stays empty for a logo. Smooth motion, no camera shake, no cuts, no text, no letters, no logo, no UI.
```
Bản chỉ từ chữ (Sora / Veo không dùng khung) – kém kiểm soát cảnh cuối, chỉ dùng khi cần:
```
A 3-second cinematic intro for a cute mobile strategy game set in ancient East Asia, landscape 16:9. Darkness with drifting golden embers; a brilliant golden light bursts from the centre, warm smoke and sparks sweep outward and clear to reveal four stylised 3D chibi heroes (a female archer, a male swordsman, a female strategist with a feather fan, a bearded marshal with a spear and banner, all with royal blue ribbons) posing on a small hill above a sunrise battlefield with a river and bridge. End holding still with empty bright sky in the upper middle. No text, no letters, no logo, no UI, no cuts.
```
Nếu dùng **Veo** và muốn tiếng tạo cùng video, thêm vào cuối prompt:
```
Audio: a deep taiko war drum hit in the darkness, a rising whoosh into a bright golden burst with a Chinese gong crash, then a short heroic shimmer that fades out as the scene appears. No speech, no vocals.
```

### 5c. Tiếng (nếu không dùng tiếng của Veo) → `intro.ogg`
Công cụ: **ElevenLabs Sound Effects** · độ dài **3.5 s** (để mình cắt khớp)
```
3.5-second cinematic game intro sound for an ancient East Asian strategy game: one deep taiko war drum hit with a low wind in the darkness, then a rising whoosh into a bright Chinese gong crash with sparkling golden shimmer, ending with a short heroic swell that fades out. Clean mix, no speech, no vocals.
```

---

## 6. Màn đăng nhập / đăng ký

```
┌──────────────────────────────────────────────────────────────┐
│                       [ LOGO Arena3Q ]                       │
│              (nền: banner_title – 4 chủ tướng)               │
│                                                              │
│        ┌────────────────────────────────────────────┐        │
│        │  [ Đăng nhập bằng Google ]  (nút chuẩn)    │        │
│        │  [ Đăng nhập với Apple ]    (chỉ trên iOS) │        │
│        │  [ Tài khoản ]        [ Đăng ký ]          │        │
│        │  [        CHƠI NGAY        ]  (nút chính)  │        │
│        │  Tiếp tục nghĩa là bạn đồng ý Điều khoản   │        │
│        │  và Chính sách bảo mật (2 liên kết)        │        │
│        └────────────────────────────────────────────┘        │
│ v0.1.0                                  [Thông báo] [Hỗ trợ] │
└──────────────────────────────────────────────────────────────┘
```
- **Chơi ngay** = tài khoản khách gắn với máy, vào game ngay. Game nhắc **liên kết** tài khoản (Google / tài khoản game) ở Cài đặt và sau vài ải đầu; cảnh báo: gỡ game hoặc đổi máy khi chưa liên kết thì **mất dữ liệu**.
- **Tài khoản**: tên đăng nhập 6–20 ký tự (chữ không dấu, số, `_`), mật khẩu ≥ 8 ký tự, nút hiện / ẩn mật khẩu, **Quên mật khẩu** (qua email). **Đăng ký**: tên đăng nhập, mật khẩu, nhập lại mật khẩu, email khôi phục (tuỳ chọn).
- **Google**: dùng **nút và logo chính thức** theo quy định thương hiệu Google Sign-In – **không** tạo bằng AI.
- **iOS**: App Store yêu cầu app có đăng nhập bằng Google phải có thêm một cách đăng nhập bảo vệ riêng tư tương đương (mục 4.8) → thêm **Đăng nhập với Apple**, dùng nút chính thức của Apple.
- **Bắt buộc của cửa hàng**: có **xoá tài khoản** ngay trong game (Cài đặt) và liên kết **Chính sách bảo mật**.
- Lần sau: tự đăng nhập; màn này chỉ còn **"Chạm để vào game"**, tên tài khoản và nút **Đổi tài khoản**.
- Ảnh cần: nền = banner; nút, khung, ô nhập = UI kit §7.

---

## 7. Bộ giao diện chung (UI kit)

### 7a. Nút, khung, ô nhập → `ui_kit_sheet.png`
Đính kèm: `chutuong1.png`
```
Mobile game UI kit for an ancient East Asian strategy game, matching the glossy stylised 3D-rendered look of the attached reference image, flat front view with no perspective, evenly spaced on a plain flat white background, nothing overlapping, NO text or letters anywhere:
1) a large wide primary button: red lacquered wood with a thin gold trim and a soft highlight, empty centre,
2) the same primary button pressed (slightly darker, highlight lower),
3) a wide secondary button: polished dark wood with a bronze trim,
4) a disabled button: plain grey stone,
5) a large panel frame: aged parchment centre with dark wood edges and bronze corner ornaments, empty centre, borders of even thickness,
6) a text input field: a dark recessed wooden slot with a thin bronze edge, empty,
7) a list item bar: a light wood strip with a bronze edge, empty,
8) the same list item bar selected, with a soft golden glow,
9) a round close button: a bronze disc with a simple X shape made of two crossed bars,
10) a small square checkbox, empty and ticked (a gold tick) side by side.
Simple clean shapes readable at small size, borders of even thickness so each element can be stretched (9-slice).
```

### 7b. Hộp thoại, khung chân dung, icon nhỏ → `ui_kit_sheet2.png`
Đính kèm: `chutuong1.png` + `ui_kit_sheet.png`
```
More elements for the same mobile game UI kit, same style as the attached UI kit image, flat front view, evenly spaced on a plain flat white background, nothing overlapping, NO text or letters anywhere:
1) a very wide dialogue box: translucent dark lacquer panel with a thin gold trim and small corner ornaments, empty,
2) a small name plate: red lacquer tag with gold trim, empty,
3) a portrait frame: rounded square bronze frame with an empty centre,
4) a small gold downward triangle (tap to continue indicator),
5) a small skip button frame: dark wood pill shape, empty,
6) tab buttons: an unselected wooden tab and a selected red lacquer tab, empty,
7) small round status dots: glowing green, glowing orange, dull grey,
8) a small golden star badge,
9) a small empty red ribbon tag (for a "new" label added by the game),
10) small simple icons in gold line style: a wooden dice, a padlock, a back arrow, a gear, a speaker, a bell, a question mark in a circle (as a symbol).
Simple clean shapes readable at small size.
```

---

## 8. Chọn máy chủ

```
┌──────────────────────────────────────────────────────────────┐
│ ←  Chọn máy chủ                                              │
│ ┌──────────┐ ┌─────────────────────────────────────────────┐ │
│ │ Đề xuất  │ │ ★ S3 Trường Bản    [Mới]   ● xanh            │ │
│ │ Của tôi  │ │   S2 Xích Bích     [Đông]  ● cam             │ │
│ │ S1–S10   │ │   S1 Hổ Lao                ● xám (bảo trì)   │ │
│ └──────────┘ └─────────────────────────────────────────────┘ │
│          Đang chọn: S3 Trường Bản          [  VÀO GAME  ]    │
└──────────────────────────────────────────────────────────────┘
```
- Người chơi mới: **tự chọn máy chủ đề xuất** (mới nhất), chỉ cần bấm Vào game.
- Tên máy chủ theo địa danh Tam Quốc: Hổ Lao, Xích Bích, Trường Bản, Quan Độ, Di Lăng, Hán Trung…
- Trạng thái: **Mới** (chấm xanh), **Đông** (cam), **Bảo trì** (xám, không vào được).
- Tab **Của tôi**: máy chủ đã có nhân vật – hiện chân dung chủ tướng + cấp. Mỗi máy chủ là **một nhân vật riêng** (chọn chủ tướng và đặt tên riêng).
- Ảnh cần: nền = banner (làm tối); khung danh sách, chấm trạng thái, ngôi sao, nhãn "Mới" = UI kit §7.

---

## 9. Chọn chủ tướng và đặt tên

```
┌──────────────────────────────────────────────────────────────┐
│ Chọn chủ tướng                                               │
│                                       ┌────────────────────┐ │
│      (nền bg_lord_select)             │ MƯU SĨ             │ │
│      nhân vật đang chọn               │ Vai trò, độ khó ★★★│ │
│      đứng trên bệ đá                  │ Máu ▮▮▯ Công ▮▯▯   │ │
│                                       │ Tầm ▮▮▯ Tốc ▮▮▯    │ │
│                                       │ Kỹ năng: 4 icon 🔒 │ │
│                                       │ Lv2 · Lv5 · Lv10 · │ │
│                                       │ Lv15               │ │
│ [Cung thủ] [Kiếm sĩ] [Mưu sĩ] [Thống soái]   [   CHỌN   ]   │
└──────────────────────────────────────────────────────────────┘
```
- 4 tab dùng `portrait_<chủ tướng>.png`; nhân vật giữa màn là **nhân vật động** (rig, clip `select` – `docs/Tuong/LordAnimations.md`); khi chưa có rig dùng `<chủ tướng>_rig_front.png` đã tách nền.
- Bảng thông tin: vai trò, độ khó (`docs/Tuong/LordHeroes.md` §1), thanh chỉ số lấy từ chỉ số Lv1 (`docs/KeHoach/HeroProgression.md` §5.1), **4 icon kỹ năng có ổ khoá** kèm cấp mở (Lv2 / 5 / 10 / 15) – chạm icon xem mô tả.
- Bấm **Chọn** → hộp xác nhận có ảnh `splash_<chủ tướng>.png`: "Chủ tướng là nhân vật cố định, **không đổi được**. Chọn Mưu sĩ?" → **Đặt tên**:
  - 2–12 ký tự, **có dấu tiếng Việt**, không ký tự đặc biệt, **không trùng** trong máy chủ, lọc từ cấm;
  - nút **xúc xắc** tạo tên ngẫu nhiên kiểu cổ trang (ghép họ + tên, ví dụ "Lâm Tử Yên", "Tô Mạc Phong"; tránh trùng tên danh tướng Tam Quốc);
  - tên này là **tên chủ tướng** (spec §2 ✅ "chủ tướng mang tên người chơi").
- Ảnh cần: `bg_lord_select.png` (dưới); chân dung, splash, icon kỹ năng đã có prompt (`ChanDung_Prompts.md`, `Icon_Prompts.md`).

**`bg_lord_select.png`** (2400 × 1080) · Đính kèm: `chutuong1.png`
```
Background art for the hero selection screen of a cute mobile strategy game set in ancient East Asia, landscape 2400x1080, in the stylised 3D-rendered look of the attached reference image (soft lighting, gentle rim light, subtle reflections, no outline).
A grand war camp at golden dusk seen from a slightly low angle: a round stone platform with carved bronze edges slightly left of the centre, inside the central 1440x1080 area, EMPTY (a character will stand on it), a large open command tent behind it with red and gold curtains, war banners in royal blue, paper lanterns, a weapon rack, distant mountains and small campfires. The right third of the image is calmer and slightly darker (an information panel will cover it); the bottom strip is simple ground (tabs will cover it). No characters, no text, no letters, no logo, no UI, no watermark.
```

---

## 10. Cốt truyện mở đầu – 6 khung tranh + thoại

Bối cảnh: **cuối thời Đông Hán**, trước loạn Hoàng Cân (chương 2) – khớp mạch truyện `docs/QuanDich/README.md`. Nơi xảy ra: **trấn Thanh Thạch** dưới chân núi Hắc Phong (tên tự đặt). Tướng khởi đầu **Giáp Sĩ** và **Xạ Thủ** gia nhập ngay ở đây.
Chủ tướng không có tên riêng → lời thoại dùng **{Tên}** (tên người chơi đặt) và cách gọi **"tướng quân"** (dùng được cho cả nam và nữ). Chỉ **khung 3** vẽ chủ tướng (4 bản, mỗi chủ tướng một bản); các khung khác nhìn **qua mắt chủ tướng** nên không cần vẽ lại.

### 10.1 Kịch bản
| Khung | Ảnh | Người nói | Lời thoại |
|---|---|---|---|
| 1 | `story_01_chaos.png` | (người kể) | Cuối thời Đông Hán, triều đình suy yếu. Giặc cướp nổi lên khắp nơi, dân lành không còn chốn nương thân. |
| 2 | `story_02_raid.png` | (người kể) | Dưới chân núi Hắc Phong, trấn Thanh Thạch đêm nào cũng bị sơn tặc cướp phá. |
| | | Lão trưởng trấn | Lũ Hắc Phong Trại lại xuống núi rồi… Trời cao ơi, ai cứu lấy trấn này! |
| 3 | `story_03_<chủ tướng>.png` | {Tên} | Ta là {Tên}, phụng mệnh dẹp loạn vùng này. Ai cần giúp, cứ nói! |
| 4 | `story_04_allies.png` | Lão trưởng trấn | Tướng quân! Xin cứu lấy dân trấn Thanh Thạch! |
| | | Giáp Sĩ | Tại hạ là vệ binh của trấn. Tấm khiên này xin che chắn cho tướng quân! |
| | | Xạ Thủ | Rừng núi quanh đây ta thuộc như lòng bàn tay. Cho ta đi cùng. |
| 5 | `story_05_bandit_king.png` | Hắc Phong Trại Chủ | Ha ha ha! Kẻ nào dám cản đường Hắc Phong Trại? Các con, xông lên! Cướp sạch trấn Thanh Thạch! |
| 6 | `story_06_charge.png` | {Tên} | Toàn quân, theo ta! Giữ vững trấn Thanh Thạch! |
→ vào **ải 1-1**.

**Hộp thoại** (UI kit §7b): khung dưới màn hình chiếm ~25 % chiều cao; chân dung người nói **bên trái** cho phe ta (`portrait_<chủ tướng>.png`, `portrait_guardian.png`, `portrait_hunter.png`, `portrait_npc_elder.png` – mặt quay phải) và **bên phải** cho phe địch (`portrait_c1_boss.png` – mặt quay trái); bảng tên, chữ hiện dần, tam giác vàng "chạm để tiếp", nút **Bỏ qua** góc trên phải. Khung tranh chuyển bằng mờ dần + lia nhẹ (Ken Burns).
Tên "Giáp Sĩ", "Xạ Thủ" theo `docs/Tuong/NormalHeroes.md` (tên Xạ Thủ còn chờ chốt – spec §13 mục 4).

### 10.2 Nhân vật phụ cần tạo trước
**Lão trưởng trấn** → `npc_elder_turnaround.png` · Đính kèm: `chutuong1.png`
```
Match the rendering style, proportions and level of detail of the attached reference image (stylised 3D-rendered chibi figurine look), but NOT its character, outfit, colours or weapon. Chibi proportions about 2.5 heads tall, large head, big expressive eyes, small body. Soft studio lighting from the top-left, gentle rim light, smooth clean materials, subtle reflections, no outline. No text, no watermark.

Character: a kind old village elder from ancient East Asia (about 70 years old), small and slightly hunched, a long white beard and long white eyebrows, a brown cloth cap, a faded brown robe with a patched sash, a gnarled wooden walking stick in the right hand, gentle worried eyes.

Character turnaround sheet: four full-body views of the SAME character in a row on a plain white background, evenly spaced, same size in every view, orthographic camera at eye level: 1) front view, 2) side view facing right, 3) back view, 4) front-right three-quarter view.
```
**Chân dung hộp thoại** (320 × 420, giống chân dung thẻ – `ChanDung_Prompts.md`):
`portrait_npc_elder.png` · Đính kèm: `npc_elder_turnaround.png`
```
Keep exactly the rendering style of the attached image: stylised 3D-rendered chibi figurine look, big expressive eyes, soft studio lighting from the top-left, gentle rim light, subtle reflections, no outline. No text, no watermark.
Bust portrait of the attached character (the kind old village elder with the long white beard and brown cloth cap), head and shoulders, face turned three-quarters to the right, worried pleading expression, soft vignette background in warm brown, vertical card composition 320x420.
```
`portrait_c1_boss.png` · Đính kèm: `docs/QuanDich/art/c1_boss_turnaround.png`
```
Keep exactly the rendering style of the attached image: stylised 3D-rendered chibi figurine look, big expressive eyes, soft studio lighting from the top-left, gentle rim light, subtle reflections, no outline. No text, no watermark.
Bust portrait of the attached character (the giant one-eyed bandit king with the leather eyepatch, bushy black beard and black bear-fur cloak), head and shoulders, face turned three-quarters to the left, laughing arrogantly, the top of the nine-ring broadsword on his shoulder, soft vignette background in dark red, vertical card composition 320x420.
```

### 10.3 Khung tranh
Mọi khung dùng chung **phần đầu và phần cuối** dưới đây, chỉ thay đoạn `SCENE`. Ảnh **1920 × 1080**. Đính kèm `chutuong1.png` + các ảnh nhân vật ghi ở từng khung.
```
Story illustration panel for a cute mobile strategy game set in ancient East Asia (late Eastern Han era), landscape 16:9, 1920x1080, in the stylised 3D-rendered chibi figurine look of the attached reference images (soft lighting, gentle rim light, subtle reflections, no outline), cinematic composition, warm colours, no blood, no gore. Characters that appear in the attached images must keep exactly the same faces, outfits and colours.

SCENE

Keep the bottom quarter of the image calm and simple (a dialogue box will cover it). Keep the main characters inside the central 75 % of the width and away from the top edge (phones and tablets crop the edges). No text, no letters, no speech bubbles, no UI, no watermark.
```
Thay `SCENE` bằng đoạn của từng khung dưới đây → lưu đúng tên file.

**Khung 1 – `story_01_chaos.png`** · đính kèm thêm: không
```
A wide view of the ancient countryside at dusk in a time of chaos: a distant burning village with orange smoke, a road of worried villagers carrying bundles and a small child holding a lantern, a torn red Han imperial banner lying on the ground, dark storm clouds over the mountains. Melancholic but not violent.
```
**Khung 2 – `story_02_raid.png`** · đính kèm thêm: `npc_elder_turnaround.png`, `docs/QuanDich/art/c1_saber_sheet.png`
```
Night in a small mountain town of wooden houses with paper lanterns at the foot of a dark mountain: cute chibi bandits with dark red head cloths and black wolf-head banners run through the street carrying stolen sacks, villagers flee, and the old village elder from the attached sheet stands in the foreground raising both hands in despair.
```
**Khung 3 – 4 bản, mỗi chủ tướng một bản** (game hiện bản của chủ tướng người chơi đã chọn)

`story_03_archer.png` · đính kèm thêm: `archer_turnaround.png`
```
Dawn at the open wooden gate of a small town: the young female archer from the attached sheet stands in the gateway in a confident heroic pose, the tall horn bow in her hand with an arrow nocked and pointed down, warm sunrise light behind her, ponytail and cape moving in the wind; a few small chibi soldiers with royal blue banners stand behind her. Her neutral grey team ribbons are painted royal blue.
```
`story_03_swordsman.png` · đính kèm thêm: `swordsman_turnaround.png`
```
Dawn at the open wooden gate of a small town: the young male swordsman from the attached sheet stands in the gateway in a confident heroic pose, the long sword resting on his shoulder, warm sunrise light behind him, headband tails and robe moving in the wind; a few small chibi soldiers with royal blue banners stand behind him. His neutral grey headband and its tails are painted royal blue.
```
`story_03_strategist.png` · đính kèm thêm: `strategist_turnaround.png`
```
Dawn at the open wooden gate of a small town: the young female strategist from the attached sheet stands in the gateway in a calm confident pose, the crane-feather fan opened in front of her chest, warm sunrise light behind her, wide sleeves and sash tails moving in the wind; a few small chibi soldiers with royal blue banners stand behind her. Her neutral grey sash tails are painted royal blue.
```
`story_03_marshal.png` · đính kèm thêm: `marshal_turnaround.png`
```
Dawn at the open wooden gate of a small town: the bearded grand marshal from the attached sheet stands in the gateway in a commanding pose, the long spear planted in the ground, warm sunrise light behind him, crimson cape and command banner flying in the wind; a few small chibi soldiers with royal blue banners stand behind him. His neutral grey plume and banner cloth are painted royal blue.
```
**Khung 4 – `story_04_allies.png`** · đính kèm thêm: `npc_elder_turnaround.png`, `guardian_turnaround.png`, `hunter_turnaround.png`
```
Morning in the town square, seen through the eyes of the hero who has just arrived (the hero is the camera and is not shown): the old village elder bows deeply with his hands clasped; the big gentle shield guardian steps forward raising his tower shield; the mountain hunter archer gives a confident nod holding his short bow; villagers peek out from doorways. Everyone looks towards the viewer. Their neutral grey team ribbons are painted royal blue.
```
**Khung 5 – `story_05_bandit_king.png`** · đính kèm thêm: `docs/QuanDich/art/c1_boss_turnaround.png`, `c1_saber_sheet.png`, `c1_wolf_sheet.png`
```
The giant one-eyed bandit king from the attached sheet stands on a rocky cliff above the town at night, laughing with his huge nine-ring broadsword on his shoulder; black wolf-head banners, bandits holding torches and black wolf hounds behind him, a dark red sky. Intimidating but still cute.
```
**Khung 6 – `story_06_charge.png`** · đính kèm thêm: `guardian_turnaround.png`, `hunter_turnaround.png`, `c1_saber_sheet.png`
```
Seen from behind and slightly above: the shield guardian, the hunter archer and a group of small chibi soldiers with royal blue banners charge out of the town gate along a dusty road at sunrise towards a crowd of bandits with black wolf-head banners; dust clouds, energetic action, no blood.
```

---

## 11. Nhạc nền
Công cụ: **Suno** hoặc **Udio** (chỉ gói trả phí mới được dùng thương mại – kiểm tra điều khoản). Không lời.

**Nhạc màn tiêu đề / đăng nhập** → `bgm_title.ogg` (lặp ~60 s)
```
Instrumental only, no vocals. Epic but warm ancient Chinese orchestral theme for a cute mobile strategy game title screen: guzheng and dizi melody over soft strings, steady taiko drums, a heroic brass swell in the middle, bright and adventurous mood, 90 BPM, seamless loop about 60 seconds.
```
**Nhạc cốt truyện mở đầu** → `bgm_story.ogg` (~60 s)
```
Instrumental only, no vocals. Ancient Chinese storytelling music: a lonely erhu and guzheng opening with a sad, tense mood (a land in chaos), building with low taiko drums and strings into a hopeful heroic rise at the end, 80 BPM, about 60 seconds.
```

---

## 12. Thứ tự làm
1. Chốt ảnh **bảng xoay 4 chủ tướng** và **Giáp Sĩ, Xạ Thủ**, boss **Hắc Phong Trại Chủ**, lính `c1_saber`, `c1_wolf` (các file prompt đã có).
2. `app_icon` → `logo_arena3q` → `banner_title` (cần ảnh bước 1).
3. `intro_frame_start` → video `intro.mp4` → `intro.ogg`.
4. `ui_kit_sheet` → `ui_kit_sheet2` → `bg_lord_select`.
5. `npc_elder_turnaround` → các chân dung hộp thoại → 9 khung truyện.
6. `bgm_title`, `bgm_story`.
7. Gửi mình theo từng nhóm; mình dựng màn hình trong Unity (⏳ sau prototype) và báo dung lượng bản build.

---

## 13. Lưu ý và điểm cần chốt
1. ✅ **Thời điểm đặt tên** (đã chốt 2026-10-17, spec §2): đặt tên **ngay sau khi chọn chủ tướng** trong lần vào game đầu tiên, **mỗi máy chủ một tên**.
2. **Đăng nhập với Apple** trên iOS (App Store 4.8) và **xoá tài khoản trong game** (App Store, Google Play) là bắt buộc khi phát hành.
3. ✅ **Tuyệt kỹ ở các ải đầu** (đã chốt 2026-10-17): tặng **Giáp Sĩ 3★** ở ải hướng dẫn để dạy chạm thẻ dùng tuyệt kỹ (`docs/KeHoach/HeroProgression.md` §13).
4. **Sau cốt truyện và ải 1-1** người chơi đi đâu: sảnh chính / bản đồ chương chưa thiết kế (⏳).
5. Đăng nhập, máy chủ, lưu tiến trình cần **backend** (⏳); prototype chỉ có Chơi ngay + một máy chủ giả.
6. Nếu phát hành ở Việt Nam, game có máy chủ và đăng nhập là game trực tuyến: cần giấy phép và tuân thủ quy định quản lý trò chơi điện tử trên mạng (hiện là Nghị định 147/2024/NĐ-CP – ví dụ xác thực tài khoản, giới hạn thời gian chơi của người dưới 18 tuổi). Cần kiểm tra lại khi chuẩn bị phát hành.
7. **Bản quyền công cụ AI**: video, nhạc, hình đưa vào bản phát hành phải dùng gói / điều khoản cho phép thương mại.
