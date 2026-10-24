# Arena3Q – Bàn giao UI: Splash → Loading → Đăng nhập → Chọn máy chủ

Tài liệu này dành cho người/AI (CLI) sẽ code phần giao diện khởi động của **Arena3Q – Tam Quốc Chiến Thuật** trong **Unity 6 (C#)**, màn hình **ngang 16:9**, chạy Android / iOS / PC. Đọc hết file này trước khi code; mọi số đo dựa trên khung tham chiếu **1280 × 720**.

---

## 0. Cấu trúc gói

```
Assets/                      ← chép nguyên vào Assets/ của project Unity
  UI/Backgrounds/            keyart_3heroes.png (nền chính), splash_lastframe.jpg
  UI/Logo/                   logo_arena3q.png (logo chính thức, nền trong suốt)
  UI/Sprites/Buttons/        btn_gold.png, btn_blue.png, btn_red.png   (9-slice)
  UI/Sprites/Frames/         frame_panel.png, frame_panel_big.png      (9-slice)
  UI/Sprites/Icons/          icon_back, icon_settings, icon_sound, lang_selector_vi
  UI/Sprites/Ornaments/      crest_dragon (mặt rồng + dải lụa), tassel (tua rua), cloud_swirl (hoa văn mây)
  Video/                     Arena3Q_Splash_fire_sound.mp4 (1920×1080, 3 giây, có tiếng)
  Audio/SFX/                 ui_tap, ui_tab, ui_confirm, ui_enter, ui_back, ui_error (.wav 44.1kHz mono)
reference/
  mockup/*.dc.html, ui.css   mã mockup (HTML) – nguồn sự thật về bố cục, màu, chữ, logic demo
  mockup/canvas.json         toạ độ các màn trên canvas + toàn bộ ghi chú
  PROMPTS_AI_ART.md          prompt tạo ảnh + ghi chú kỹ thuật từng màn
  source_images/             UI kit gốc (ui_kit_sheet.png) và logo gốc
```

Mockup online (bấm Play để chạy thử, nghe âm thanh): canvas "Arena3Q – Đăng nhập & Chọn server" trên Claude.

---

## 1. Thiết lập Unity

| Mục | Giá trị |
|---|---|
| Orientation | Landscape Left + Landscape Right (khoá dọc) |
| Canvas | Screen Space – Overlay, **Canvas Scaler: Scale With Screen Size, 1280×720, Match = 0.5** |
| Safe area | Bọc các phần neo mép (thanh trạng thái, nút góc, chân trang) trong `SafeAreaFitter` |
| Font | **Be Vietnam Pro** (Google Fonts, OFL) – tạo TextMeshPro SDF Font Asset gồm Latin + **Vietnamese** (Character Set: Unicode range `0020-007E,00C0-1EF9,20AB`), các weight 400/600/700/800 |
| Import sprite | Texture Type = Sprite (2D and UI), Mesh Type = Full Rect, Filter = Bilinear, **không** Generate Mip Maps, Compression = High Quality |
| Video | VideoPlayer → RenderTexture → RawImage full màn |
| Audio | 1 `UIAudio` singleton, `AudioSource.PlayOneShot`, chống phát chồng: tối thiểu 80 ms giữa 2 lần cùng clip |

### Màu (design tokens)

| Token | Hex | Dùng cho |
|---|---|---|
| gold | `#FFD877` | tiêu đề, số %, link |
| gold-line | `#E2B85C` | viền, đường kẻ |
| ink-dark | `#3A1804` | chữ trên nút vàng |
| text | `#E6ECF5` | chữ thường trên panel xanh |
| text-dim | `#A9B8CF` / `#8FA2BF` | chữ phụ |
| panel | `#0A1424 → #132A49` | nền ô nhập |
| input-border | `#2F5584` | viền ô nhập (focus: `#E2B85C`) |
| ok / warn / bad / off | `#4ADE80` / `#F59E0B` / `#DC2626` / `#6B7280` | trạng thái máy chủ & mạng |
| bar-fill | gradient `#8A0F2E → #D61F4F → #FF4F7A` | thanh loading |

---

## 2. Sprite 9-slice (đặt Border trong Sprite Editor)

Tất cả sprite nút/khung **đã được cắt cân**: viền vàng cách đều 4 mép ảnh ⇒ hộp RectTransform căn giữa thì hình nhìn thấy cũng căn giữa. Không thêm offset bù.

| File | Kích thước | Border L / R / T / B | Ghi chú |
|---|---|---|---|
| btn_gold.png | 289×76 | 72 / 72 / 38 / 37 | Nút hành động chính. Quầng sáng thêm bằng UI Shadow/Outline (vàng cam, alpha 0.5) |
| btn_blue.png | 186×49 | 44 / 44 / 24 / 24 | Nút phụ, tab chưa chọn |
| btn_red.png | 186×52 | 44 / 44 / 26 / 25 | ĐĂNG NHẬP, tab đang chọn |
| frame_panel.png | 305×141 | 44 / 44 / 44 / 44 | Khung panel Đăng nhập, Đăng ký, Liên kết, bảng thông báo, ô máy chủ (Pixels Per Unit Multiplier = 2 cho ô nhỏ) |
| frame_panel_big.png | 476×266 | 56 / 56 / 56 / 56 | Cửa sổ Chọn máy chủ (Pixels Per Unit Multiplier ≈ 0.875 để góc to hơn) |
| crest_dragon.png | 238×59 | — | Đặt giữa mép trên panel, tràn lên 32px |
| tassel.png | 24×150 | — | Treo mép trái/phải panel, cách đỉnh 40px, tràn ra ngoài 12px (bên phải lật X) |
| cloud_swirl.png | 152×50 | — | 2 bên dòng mẹo ở Loading (bên phải lật X), hiển thị 96×32 |
| icon_*.png | 62×62 | — | Nút tròn, hiển thị 52×52 |
| lang_selector_vi.png | 180×62 | — | Nút chọn ngôn ngữ (đã có chữ "Tiếng Việt") |

**Kích thước nút chuẩn (cao):** XL 76 · LG 64 · MD 50 · SM 44 · trong ô máy chủ 40. Cỡ chữ tương ứng 28 / 22 / 16 / 14 / 14, weight 800 (nút vàng giãn chữ 0.08em).

---

## 3. Component dùng chung

### 3.1 `GameButton` (Button + Image Sliced + TMP)
- **Hover / Highlighted** (PC): sáng +10–20%, quầng sáng cùng màu nút. **Không đổi sprite** (đổi sprite làm khung nhảy cỡ → chữ lệch).
- **PointerDown:** scale 1 → 0.94 trong 0.05 s, hạ 2 px, tối −20%.
- **PointerUp:** scale về 1 trong 0.12 s, Ease.OutBack.
- **Selected (tab đang chọn):** sáng +12% + viền sáng vàng.
- **Disabled:** alpha 0.55, không phát tiếng (hoặc phát `ui_error` nếu bấm).
- Trường `SfxType` ∈ {Tap, Tab, Confirm, Enter, Back, Error} → phát âm tương ứng khi PointerClick.
- Nút tròn (âm thanh/cài đặt/quay lại): scale 0.9 khi nhấn.

### 3.2 `StatusBar` – góc trên phải **mọi màn**
Viên thuốc đen mờ (`rgba(0,0,0,.5)`, viền trắng 12%), cao 26, bo 13, cách phải 12 / trên 6:
`[icon Wi-Fi 3 vạch] | 16:47`
- Giờ: giờ máy, định dạng `HH:mm`, cập nhật mỗi 15 s.
- Wi-Fi: 3 vạch trắng (tốt) · 1 vạch vàng `#FCD34D` (yếu/đứng) · xám + gạch đỏ (mất mạng). Trên mobile dùng loại mạng thật (Wi-Fi/di động) nếu lấy được, nếu không chỉ cần mức tín hiệu suy từ ping.

### 3.3 `ModalPanel`
frame_panel + crest_dragon (+ tassel tuỳ màn), nền mờ phía sau `rgba(5,3,2,.6)`, xuất hiện scale 0.9→1 + fade 0.25 s.

### 3.4 Ô nhập
Cao 44, bo 4, nền gradient panel, viền `#2F5584`, placeholder `#7F93B0`, focus viền vàng. Label ẩn (dùng placeholder) nhưng giữ tên field cho accessibility.

---

## 4. Các màn (toạ độ trên khung 1280×720, gốc trên-trái)

### 4.1 Splash
- Phát `Arena3Q_Splash_fire_sound.mp4` full màn (3 s). Lần mở đầu không cho bỏ qua; từ lần 2 chạm để bỏ qua.
- Kết thúc → Loading. StatusBar vẫn hiện.

### 4.2 Loading
| Phần tử | Vị trí / kích thước |
|---|---|
| Nền | keyart_3heroes full màn; dải tối dưới đáy cao 330 (gradient trong suốt → `rgba(8,4,2,.96)`) |
| Logo | x 400, y 16, rộng 480 (giữ tỉ lệ) |
| Thanh | nhóm x 150, y 560, 980×36: đầu mũi giáo trái 72×36 · rãnh 868×16 (viền 2px `#B8893A`, nền `#160707`) · đầu mũi giáo phải (lật X); hai đầu chồng vào rãnh 16 px; **tất cả căn cùng một đường tâm ngang** |
| Xe lương | 132×84, bánh trước đúng mép phần đã tải: `x = clamp(fill − 118, −6, 868 − 124)` (gốc = đầu rãnh x 206); bánh xe quay, cờ bay, bụi + tia lửa sau bánh |
| Chữ trạng thái + % | đi theo xe, bên trái xe (cách 14); nếu không đủ chỗ thì chuyển sang bên phải xe. 21px/800, % màu gold |
| Tốc độ | **chính giữa dưới thanh**, y 604: `↓ 2,3 MB/s` (16px/800). Trắng = ổn định, vàng = yếu, đỏ nhấp nháy `0 KB/s` = đứng/mất mạng |
| Dòng mẹo | y 642, giữa màn, 18px/700 gold, mây 2 bên; đổi mỗi 3.5 s (fade + trượt lên 8px); vẫn chạy khi 100% |
| Chân trang | trái: `Phiên bản x.y.z` · phải: `Chơi quá 180 phút một ngày sẽ ảnh hưởng xấu đến sức khỏe.` + nhãn **16+** |

Chữ trạng thái: <20% "Đang kiểm tra phiên bản…" · <90% "Đang tải tài nguyên…" · <100% "Đang kết nối máy chủ…" · 100% "Hoàn tất! Đang vào game…" → **tự chuyển** sang Đăng nhập (không có "Chạm để tiếp tục").

Mẹo (tiếng Việt, có thể đưa ra file localization):
1. Thu thập và bồi dưỡng danh tướng, sắp xếp đội hình hợp lý để làm chủ chiến trường!
2. Chọn một tướng rồi chạm vào bản đồ để ra lệnh di chuyển!
3. Mỗi trận triệu hồi tối đa 5 tướng, mỗi tướng dẫn theo một đội quân!
4. Phá hủy nhà chính của đối phương để giành chiến thắng!
5. Dùng tướng khắc hệ Kim, Mộc, Thủy, Hỏa, Thổ để chiếm ưu thế!
6. Liên kết tài khoản để giữ tiến trình khi đổi máy hoặc cài lại game!
7. Chọn máy chủ có trạng thái Tốt để vào trận mượt hơn!

#### Máy trạng thái mạng (Loading)
| Trạng thái | Điều kiện vào | Hiển thị |
|---|---|---|
| `Good` | ping < 150 ms và tốc độ > 300 KB/s | bình thường |
| `Weak` | ping 150–1000 ms hoặc tốc độ < 300 KB/s | toast vàng dưới logo (y 204): ⚠ "Kết nối mạng không ổn định, tốc độ tải có thể chậm."; chữ trạng thái "Mạng yếu, đang tải chậm…" |
| `Stalled` | 0 byte trong 3 s | phủ tối 45%, **vòng chờ giữa màn** (tâm 640, 292): vòng cung vàng 128 px xoay 1 s/vòng, vòng nét đứt 88 px xoay ngược 3.2 s, kim cương vàng 32 px nhấp nháy; dưới: "Mạng không ổn định, đang kết nối lại…" + "Lần thử n/3 · Vui lòng không tắt game"; thanh xám đi, xe & hiệu ứng dừng; tự thử lại 3 lần (2 s, 4 s, 8 s) |
| `Lost` | `Application.internetReachability == NotReachable` hoặc 3 lần thử đều hỏng | **Bảng thông báo** (ModalPanel 560 rộng, x 360 y 168): icon Wi-Fi gạch, "Mất kết nối mạng", "Không thể kết nối Internet. Vui lòng kiểm tra Wi-Fi hoặc dữ liệu di động rồi thử lại.", dòng xanh "Tiến trình được giữ lại, sẽ tải tiếp từ N%.", "Mã lỗi: NET-1001", nút **Thoát game** (xanh) + **THỬ LẠI** (vàng) |
| `ServerTimeout` | có Internet nhưng request tới máy chủ > 5 s | cùng bảng, icon máy chủ gạch, "Máy chủ không phản hồi", "Kết nối tới máy chủ bị gián đoạn hoặc quá thời gian chờ. Vui lòng thử lại sau ít phút.", "Mã lỗi: NET-2002" |
| `Retrying` | bấm THỬ LẠI | vòng chờ "Đang thử kết nối lại…" → thành công về `Good`, thất bại về `Lost` |

- Tốc độ = byte tải được trong 1 s (trung bình trượt 3 s). Ping = `UnityWebRequest` HEAD tới máy chủ mỗi 2 s.
- **Tải tiếp từ phần đã có** (Addressables cache / HTTP Range), không tải lại từ đầu.
- Thoát game: `Application.Quit()`; **iOS không cho tự thoát** → chỉ đóng bảng.
- Mã lỗi NET-xxxx là tạm, thay bằng mã thật.

### 4.3 Đăng nhập
| Phần tử | Vị trí |
|---|---|
| Nền | keyart + phủ radial tối ở giữa (ellipse 600×440 tại 640,410: 0.7 → 0.3 → 0.5) |
| Logo | x 495, y 8, rộng 290 |
| Ngôn ngữ + âm thanh | phải 20, trên 40 (lang 180×62, tròn 52) |
| Panel | frame_panel x 380, y 134, rộng 520; padding 34/44/26; khoảng cách dọc 10; crest giữa trên (−32), tua rua 2 bên |
| Nội dung (trên → dưới) | Tab **Đăng nhập** (đang chọn, gạch vàng 3px) / **Đăng ký** (42) · ô "Email hoặc số điện thoại" · ô "Mật khẩu" · hàng [☑ Ghi nhớ đăng nhập — Quên mật khẩu?] · **ĐĂNG NHẬP** (đỏ MD, full) · "hoặc đăng nhập nhanh" · 3 nút xanh SM **Google / Apple / Facebook** · đường kẻ vàng · **CHƠI NGAY** (vàng LG, full) · "Vào game không cần tài khoản · Có thể liên kết sau" · điều khoản (link vàng) |

- **CHƠI NGAY = đăng nhập khách**: tạo/lấy `guestId` gắn thiết bị (Keychain trên iOS / Keystore trên Android để không mất khi cài lại nếu có thể), nhận token từ server, vào thẳng màn Vào game. Tài khoản khách có thể **liên kết sau** (mục 4.6).
- Kiểm tra lỗi: ô trống, sai định dạng email/SĐT, sai mật khẩu → viền ô đỏ + dòng lỗi đỏ dưới ô, phát `ui_error`.

### 4.4 Đăng ký
Cùng khung với Đăng nhập (tab Đăng ký đang chọn) + nút quay lại (icon_back, trái 20 trên 18).
Nội dung: ô "Email hoặc số điện thoại" · [ô "Mã xác minh (6 số)" + nút xanh SM **Gửi mã** rộng 120, cùng cao 44] · 2 ô cạnh nhau "Mật khẩu (tối thiểu 8 ký tự)" / "Nhập lại mật khẩu" · ☐ "Tôi đồng ý với Điều khoản dịch vụ và Chính sách bảo mật" · **TẠO TÀI KHOẢN** (vàng LG) · hàng "Đã có tài khoản? Đăng nhập" — "Chơi ngay ›".
- Gửi mã: đếm ngược 60 s ("Gửi lại 59s"), khoá nút khi đang đếm.
- TẠO TÀI KHOẢN bị khoá tới khi tick điều khoản và mật khẩu khớp.

### 4.5 Màn Vào game (sau đăng nhập)
| Phần tử | Vị trí |
|---|---|
| Logo | x 390, y 16, rộng 500 |
| Chip người chơi | trên-trái (16, 14), frame_panel nhỏ: avatar tròn 40 + "Khách #83921" + nhãn cam "Chưa liên kết · Liên kết ngay" → mở 4.6 |
| Nút tròn | phải 18, trên 40: âm thanh (bật/tắt toàn bộ SFX, lưu PlayerPrefs, khi tắt hiện gạch đỏ chéo) + cài đặt |
| Ô máy chủ | giữa dưới, frame_panel cao 62, padding trái 22 / phải 30: chấm trạng thái + "Tốt" + vạch + "S8 · Xích Bích" + nút xanh cao 40 rộng 150 "Đổi máy chủ ›" (nằm trong khung, cách đều trên dưới) → mở 4.7 |
| VÀO GAME | vàng XL 360×76, dưới ô máy chủ 14px, cách đáy 44 |
| Chân trang | như Loading (phiên bản, cảnh báo 180 phút, 16+) |

### 4.6 Liên kết tài khoản (popup)
ModalPanel x 380, y 116, rộng 520 + crest + tua rua; nền màn Vào game tối 66%; nút quay lại trái trên.
"Liên kết tài khoản" · "Bạn đang chơi bằng tài khoản khách. Liên kết để giữ tiến trình khi đổi máy hoặc cài lại game." · dải thưởng nét đứt "Thưởng liên kết lần đầu" + 3 ô quà [placeholder] · nút xanh MD **Liên kết Google / Apple / Facebook** · nút đỏ MD **Email / Số điện thoại** (→ form Đăng ký) · link "Để sau".
Hiện khi: bấm chip "Chưa liên kết", lần đầu đạt cấp [X], trước lần nạp đầu tiên, Cài đặt › Tài khoản.

### 4.7 Chọn máy chủ (popup lớn)
- Nền keyart blur 6px, tối 45%. Nút quay lại trái trên.
- Cửa sổ frame_panel_big x 76, y 46, 1128×650, padding 30/40/22, crest giữa trên; tiêu đề "CHỌN MÁY CHỦ" (22px/800, giãn 3px).
- Cột trái 200: 4 tab nút MD — **Máy chủ của tôi · Đề xuất · S1 – S10 · S11 – S20** (đang chọn = đỏ + sáng, còn lại xanh). Phát `ui_tab`.
- Phải: thẻ "ĐĂNG NHẬP GẦN ĐÂY" (avatar 56 + "S8 · Xích Bích · Lv.45" + nút xanh SM "Chọn" rộng 110, căn giữa dọc) · tiêu đề tab + "n máy chủ" · lưới 2 cột, ô cao 56, cách 8, cuộn được.
- Mỗi ô: chấm màu + chữ trạng thái (Tốt / Đông / Đầy / Bảo trì – **luôn có chữ, không chỉ màu**) · "S20 · Bạch Đế Thành" · nhãn MỚI (xanh) / HOT (đỏ) · nếu có nhân vật: avatar 36 + "Lv.N". Ô đang chọn: viền vàng 2px + sáng. Máy bảo trì: alpha 0.55, bấm phát `ui_error`.
- Chân: chú thích 4 trạng thái · "Đã chọn: S8 · Xích Bích" · **XÁC NHẬN** (vàng MD 210×50) → lưu lựa chọn, về màn Vào game.

Model dữ liệu gợi ý:
```csharp
public enum ServerStatus { Good, Busy, Full, Maintenance }
public class ServerInfo {
    public int Id; public string Name; public ServerStatus Status;
    public bool IsNew, IsHot, IsRecommended;
    public CharacterSummary MyCharacter; // null nếu chưa có (heroIcon, level)
}
```
Tên máy chủ mẫu (địa danh Tam Quốc): Lạc Dương, Trường An, Hứa Xương, Kiến Nghiệp, Thành Đô, Hổ Lao Quan, Trường Bản, Xích Bích, Quan Độ, Phàn Thành, Kinh Châu, Ích Châu, Tương Dương, Hán Trung, Nhai Đình, Hợp Phì, Di Lăng, Định Quân Sơn, Ngũ Trượng Nguyên, Bạch Đế Thành. Dữ liệu trạng thái/cấp trong mockup là **mẫu**.

---

## 5. Âm thanh (Assets/Audio/SFX)

| Clip | Âm | Dùng cho |
|---|---|---|
| ui_tap | gảy đàn tranh 1 nốt | nút phụ, chọn máy chủ, đăng nhập nhanh, Gửi mã |
| ui_tab | gõ mõ gỗ | chuyển tab |
| ui_confirm | trống trận + đàn tranh lướt ngũ cung | ĐĂNG NHẬP, TẠO TÀI KHOẢN, XÁC NHẬN, liên kết, THỬ LẠI |
| ui_enter | 2 trống trận + rút kiếm + chiêng | CHƠI NGAY, VÀO GAME |
| ui_back | đàn tranh 2 nốt đi xuống | quay lại, đóng, Để sau, Thoát game |
| ui_error | mõ trầm 2 tiếng | lỗi nhập, máy chủ bảo trì |

Các file này được **tổng hợp bằng code** để dùng tạm; khi có ngân sách nên thay bằng bản thu thật (guzheng, mõ, trống trận, chiêng, rút kiếm) cùng tên file.

---

## 6. Gợi ý cấu trúc code

```
Assets/Scripts/UI/
  Core/        UIAudio.cs, GameButton.cs, SafeAreaFitter.cs, ScreenRouter.cs (điều hướng màn + transition fade 0.2s)
  Common/      StatusBar.cs, ModalPanel.cs, Toast.cs, LoadingSpinner.cs
  Screens/     SplashScreen.cs, LoadingScreen.cs, LoginScreen.cs, RegisterScreen.cs,
               StartScreen.cs, BindAccountPopup.cs, ServerSelectPopup.cs
  Net/         NetworkMonitor.cs (ping, tốc độ, reachability → event NetState), ResumableDownloader.cs
  Data/        ServerInfo.cs, AuthService.cs (guest/email/social – để interface, mock trước)
Assets/Prefabs/UI/  Button_Gold/Blue/Red (XL/LG/MD/SM), Panel_Modal, StatusBar, ServerItem, Spinner
Assets/Scenes/      Boot.unity (Splash + Loading + Login + Start trong 1 scene, đổi bằng ScreenRouter)
```

Thứ tự làm đề xuất: (1) import + 9-slice + font TMP → (2) GameButton + UIAudio + StatusBar → (3) Loading + NetworkMonitor (mock được bằng phím tắt để test 5 trạng thái) → (4) Login/Register/Start với AuthService mock → (5) ServerSelect với dữ liệu mẫu → (6) Splash video → (7) nối backend thật.

---

## 7. Placeholder còn phải điền

- Số phiên bản `[0.1.0]`
- Phần thưởng liên kết `[QUÀ]`, cấp độ hiện nhắc liên kết `[X]`
- Mã lỗi thật thay NET-1001 / NET-2002
- Link Điều khoản dịch vụ & Chính sách bảo mật
- Dung lượng tải thật, danh sách máy chủ thật từ API
- Kiểm tra lại quy định hiện hành về cảnh báo thời gian chơi và nhãn độ tuổi (16+) trước khi phát hành
