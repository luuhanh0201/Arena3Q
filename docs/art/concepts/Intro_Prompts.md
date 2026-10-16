# Intro khi vào game (~3 giây) – prompt tạo video, logo và âm thanh

Nội dung đã chốt (2026-10-17): **camera bay qua chiến trường rồi dừng ở logo "Arena3Q"**, làm bằng **video AI**, có **âm thanh**.

## 0. Thông số và cách ghép

| Mục | Thông số |
|---|---|
| Thời lượng | **3.0 s** (tạo dài hơn rồi cắt) |
| Khung hình | **ngang 16 : 9**, xuất **1920 × 1080** (hoặc 1280 × 720 nếu file nặng), 30 fps |
| File | `intro.mp4` (H.264), **≤ 3 MB** · logo `logo_arena3q.png` (nền trong suốt) · âm thanh `intro.ogg` hoặc `.wav` |
| Vùng an toàn | Điện thoại dài (20 : 9) sẽ **cắt bớt trên – dưới** ≈ 10 %: mọi thứ quan trọng nằm trong **80 % chiều cao giữa** |
| Chữ | Video **không có chữ** – AI hay viết sai. Logo là **ảnh riêng**, game hiện đè lên ở giây 2.2 (sắc nét, đúng chữ, sửa được) |

**Kịch bản 3 giây**
| Thời gian | Hình | Tiếng |
|---|---|---|
| 0.0–0.8 s | Camera từ trên mây/sương lao xuống, lộ dần con sông uốn khúc lúc bình minh | một tiếng **trống trận** lớn, gió vù |
| 0.8–2.0 s | Lướt nhanh dọc sông: cầu gỗ, hai đạo quân chibi (cờ **xanh** bên trái, cờ **đỏ** bên phải) tiến về phía cầu, hai tòa thành ở hai đầu | trống dồn, tiếng gió lướt, tiếng quân hô xa |
| 2.0–3.0 s | Camera kéo lên, dừng ở **toàn cảnh chiến trường góc 3/4** (giống lúc chơi); vùng trời phía trên giữa khung **sáng bừng, trống** để đặt logo | **tiếng chiêng** + ánh kim ngân dài |

**Công cụ (gợi ý, thử 1–2 cái):**
- **Kling** hoặc **Veo (Google Flow / Gemini)** chế độ **ảnh → video với khung đầu và khung cuối** – kiểm soát bố cục tốt nhất. Veo 3 tạo được cả tiếng cùng video.
- **Sora**: tạo từ chữ, đẹp nhưng khó ép đúng bố cục cuối.
- Video AI thường dài 5–8 s: tạo bản dài rồi **cắt / tua nhanh còn 3 s** (CapCut, DaVinci Resolve), giữ cảnh cuối đứng yên 0.5–0.8 s cho logo.
- Kiểm tra điều khoản **dùng thương mại** của công cụ trước khi đưa vào bản phát hành.

**Quy trình:**
1. Tạo **khung cuối** (ảnh 1) và **khung đầu** (ảnh 2) bằng ChatGPT.
2. Tạo video (prompt 3) với 2 khung đó.
3. Tạo logo (prompt 4) và âm thanh (prompt 5, hoặc dùng tiếng Veo tạo sẵn).
4. Lưu vào thư mục này: `intro_frame_end.png`, `intro_frame_start.png`, `intro.mp4`, `logo_arena3q.png`, `intro.ogg` → báo mình. Mình làm cảnh intro trong Unity: phát video vừa màn hình (cắt trên – dưới khi cần), hiện logo với hiệu ứng sáng ở giây 2.2, phát âm thanh, **chạm để bỏ qua**, rồi chuyển vào game.

---

## 1. Ảnh khung CUỐI – toàn cảnh chiến trường (tạo trước)
Công cụ: ChatGPT · Đính kèm: `Assets/Arena3Q/Art/Maps/map1.png` + `chutuong1.png` · Lưu: `intro_frame_end.png`
```
Cinematic key frame for a mobile strategy game intro, landscape 16:9, 1920x1080.
A wide view of the battlefield shown in the first attached image (same layout: a winding river crossing from top to bottom with two wooden bridges, a stone castle with blue banners on the left side, a stone castle with red banners on the right side, green meadows), seen from a fixed 3/4 top-down camera about 40 degrees from above, painted in the soft hand-painted style of that map, at sunrise with warm golden light and light mist over the river.
Two small armies of cute chibi soldiers in the stylised 3D-rendered look of the second attached image: blue-bannered troops on the left, red-bannered troops on the right, marching towards the bridges.
The upper middle third of the frame is open bright sky with soft clouds and a glowing light burst, kept EMPTY for a game logo to be placed later.
Keep all important elements inside the central 80 % of the image height. No text, no letters, no logo, no UI, no watermark.
```

## 2. Ảnh khung ĐẦU – trên mây
Công cụ: ChatGPT · Đính kèm: `intro_frame_end.png` · Lưu: `intro_frame_start.png`
```
Cinematic key frame for the same mobile strategy game intro, landscape 16:9, 1920x1080, same colours and painting style as the attached image.
The camera is high above the same battlefield, looking down through thick soft golden clouds and morning mist at sunrise; only glimpses of the winding river and green land below are visible through gaps in the clouds. Calm, majestic, warm light rays.
No text, no letters, no logo, no UI, no watermark.
```

---

## 3. Video – camera bay qua chiến trường
Công cụ: **Kling / Veo** (ảnh → video) · Khung đầu: `intro_frame_start.png` · Khung cuối: `intro_frame_end.png` · Lưu: `intro.mp4`

**Bản có khung đầu + cuối** (khuyến nghị):
```
A fast, smooth cinematic camera flight for a mobile strategy game intro, about 3 seconds, landscape 16:9.
The camera dives down from the golden clouds of the first frame, breaks through the mist and sweeps quickly along the winding river: past a wooden bridge, over two small armies of cute chibi soldiers (blue banners on the left, red banners on the right) marching towards the bridges, banners fluttering, then rises and settles into the wide 3/4 top-down view of the whole battlefield shown in the last frame, with the stone castles at both ends.
In the final moment a warm light burst glows in the empty upper middle sky and the camera holds still.
Sunrise golden light, soft mist, gentle cloth and water motion, consistent hand-painted map style with stylised 3D chibi characters. No text, no letters, no logo, no UI, no camera shake, no cuts.
```

**Bản chỉ từ chữ** (Sora / Veo khi không dùng khung):
```
A 3-second cinematic camera flight for a cute mobile strategy game intro set in ancient East Asia, landscape 16:9.
Start high in golden sunrise clouds, dive through the mist, sweep fast along a winding river crossing a green meadow with two wooden bridges; two small armies of chibi soldiers, blue banners on the left and red banners on the right, march towards the bridges; stone castles with blue and red banners stand at the far left and far right.
End by rising into a wide 3/4 top-down view of the whole battlefield and holding still, with an empty glowing patch of bright sky in the upper middle of the frame.
Hand-painted soft colour landscape, stylised 3D-rendered chibi characters, warm light rays, gentle mist. No text, no letters, no logo, no UI, no cuts.
```

Nếu dùng **Veo 3** và muốn tiếng tạo cùng video, thêm vào cuối prompt:
```
Audio: one deep war drum hit at the start with rushing wind, fast rolling war drums and distant army shouts during the flight, a bright Chinese gong crash with a shimmering tail at the end. No speech, no music with vocals.
```

---

## 4. Logo "Arena3Q" (ảnh riêng, game hiện đè lên video)
Công cụ: ChatGPT · Đính kèm: `chutuong1.png` (vật liệu vàng, đỏ) · Lưu: `logo_arena3q.png` (≈ 1600 × 700, nền trong suốt)
```
A game logo for a cute mobile strategy game set in ancient East Asia, transparent background, centred, horizontal layout about 16:7.
The word "Arena3Q" spelled exactly A-r-e-n-a-3-Q, bold chunky 3D letters in polished gold with a dark red outline and a soft inner glow, matching the glossy stylised 3D materials of the attached reference image.
Behind the letters: a crossed sword and spear and a small round bronze shield, a red silk ribbon banner under the word (the ribbon left empty, no extra text), small golden sparkles.
Clean readable shapes that stay legible on a phone screen. No other text, no watermark.
```
Kiểm tra kỹ chữ: phải đúng **"Arena3Q"**; nếu AI viết sai, yêu cầu lại hoặc gửi mình sửa chữ trong Krita (giữ phần trang trí).

---

## 5. Âm thanh (nếu không dùng tiếng của Veo)
Công cụ tạo hiệu ứng âm thanh từ chữ (ví dụ **ElevenLabs Sound Effects**) · Độ dài **3.5 s** (để mình cắt khớp) · Lưu: `intro.ogg` hoặc `intro.wav`
```
3.5-second cinematic intro sound for an ancient East Asian strategy game: a single deep taiko war drum hit with a whoosh of rushing wind, followed by fast rolling war drums and distant army shouts building up, ending with a bright Chinese gong crash and a shimmering magical sparkle tail. Epic but short, clean mix, no speech, no vocals.
```
Tuỳ chọn – tiếng riêng khi logo hiện (để mình canh đúng lúc):
```
1.5-second magical logo reveal sound: a bright Chinese gong hit layered with a golden shimmering chime and a soft wind swell. No speech.
```
