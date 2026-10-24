# Prompt tạo ảnh & ghi chú từ canvas

NÚT BẤM · HIỆU ỨNG & ÂM THANH (áp dụng cho mọi màn)

Sprite từ UI kit, mỗi màu có 3 trạng thái:
• Vàng (hành động chính: CHƠI NGAY, VÀO GAME, XÁC NHẬN, TẠO TÀI KHOẢN): thường / nhấn (cam đậm hơn)
• Xanh (phụ, tab chưa chọn, đăng nhập nhanh): thường / sáng (hover) / tối (nhấn)
• Đỏ (ĐĂNG NHẬP, tab đang chọn): thường / sáng / tối
Dùng 9-slice (Sprite Editor › Border) để kéo dài nút mà không méo hai đầu hoa văn.
QUAN TRỌNG: các sprite trạng thái trong kit khác kích thước và vùng phát sáng, đổi sprite khi hover/nhấn làm khung nhảy và chữ trông lệch. Hoặc xuất lại 3 trạng thái cùng một canvas (cùng kích thước, khung nằm đúng một vị trí), hoặc chỉ dùng 1 sprite và đổi màu như bên dưới.

Khi nhấn (Unity, Button Transition = Color Tint hoặc DOTween):
• Hover/Highlighted: sáng +15%, thêm quầng sáng cùng màu nút (không đổi sprite)
• PointerDown: scale 1 → 0,94 trong 0,05s, hạ 2px, tối −20%
• PointerUp: scale về 1 trong 0,12s (Ease.OutBack)
• Tab đang chọn: sáng +12% + viền sáng vàng
• Nút tròn (âm thanh, cài đặt, quay lại): scale 0,9

Âm thanh phong cách Tam Quốc (AudioSource.PlayOneShot, không phát chồng quá 1 lần/80ms):
• ui_tap – gảy đàn tranh 1 nốt (La5) ~0,4s: nút phụ, chọn máy chủ, đăng nhập nhanh
• ui_tab – gõ mõ gỗ ~70ms: chuyển tab Đăng nhập/Đăng ký, tab máy chủ
• ui_confirm – trống trận + đàn tranh lướt lên ngũ cung (Rê–Mi–Fa#–La–Rê) ~0,7s: ĐĂNG NHẬP, TẠO TÀI KHOẢN, XÁC NHẬN, liên kết
• ui_enter – 2 tiếng trống trận + tiếng rút kiếm + chiêng ngân ~2s: CHƠI NGAY, VÀO GAME
• ui_back – đàn tranh 2 nốt đi xuống: quay lại, đóng, Để sau
• ui_error – mõ gỗ trầm 2 tiếng: bấm máy chủ đang bảo trì
Nút loa ở màn Vào game bật/tắt toàn bộ SFX (lưu PlayerPrefs).
Bản mẫu trên canvas tổng hợp sẵn các âm này để nghe thử khi bấm Play; bản game nên thay bằng file thu thật (đàn tranh/guzheng, mõ, trống trận, chiêng, rút kiếm).

---

PROMPT 9 · LOADING – TRẠNG THÁI MẠNG (dùng chung bố cục prompt 2)

On EVERY screen, in the top-right corner: a tiny translucent black pill with a Wi-Fi icon (3 arcs), a thin divider and the current time '16:47' in white. Directly under the center of the loading bar: only the download speed with a small down-arrow, e.g. '↓ 2,3 MB/s' in bold white (amber when slow, red when 0). Nothing else under the bar.

State A – WEAK NETWORK: Wi-Fi icon with only 1 amber arc, speed '↓ 86 KB/s' in amber; a small amber toast under the logo with a warning triangle: 'Kết nối mạng không ổn định, tốc độ tải có thể chậm.' Cart keeps moving slowly.
State B – STALLED: screen dimmed 45%; in the exact center an ornate loading spinner: outer gold arc ring spinning, inner dashed gold ring spinning the opposite way, a glowing gold diamond pulsing in the middle on a navy disc; text below 'Mạng không ổn định, đang kết nối lại…' and 'Lần thử 2/3 · Vui lòng không tắt game'. Bar fill desaturated, cart frozen, speed '↓ 0 KB/s' red blinking. In State C the top-right Wi-Fi icon is grey with a red slash.
State C – CONNECTION LOST: screen dimmed 60%; centered notice panel (navy, ornate gold frame, dragon crest with red ribbons on top): red circle icon with crossed-out Wi-Fi, title 'Mất kết nối mạng', text 'Không thể kết nối Internet. Vui lòng kiểm tra Wi-Fi hoặc dữ liệu di động rồi thử lại.', green line 'Tiến trình được giữ lại, sẽ tải tiếp từ 63%.', small grey 'Mã lỗi: NET-1001', two buttons: blue 'Thoát game' and gold 'THỬ LẠI'.
State D – SERVER TIMEOUT: same panel, server-rack icon with an X, title 'Máy chủ không phản hồi', text 'Kết nối tới máy chủ bị gián đoạn hoặc quá thời gian chờ. Vui lòng thử lại sau ít phút.', 'Mã lỗi: NET-2002'.

Logic gợi ý cho Unity:
• Đo tốc độ = số byte tải trong 1 giây (trung bình trượt 3 giây); ping = UnityWebRequest HEAD tới máy chủ mỗi 2 giây.
• Ổn định: ping < 150 ms và > 300 KB/s · Mạng yếu: ping 150–1000 ms hoặc < 300 KB/s · Đứng mạng: 0 byte trong 3 giây → hiện vòng chờ, tự thử lại 3 lần (cách 2s, 4s, 8s) · Mất mạng: Application.internetReachability == NotReachable hoặc thử lại 3 lần đều hỏng → bảng thông báo · Máy chủ không phản hồi: có Internet nhưng request tới máy chủ timeout (> 5 giây).
• Tải tiếp từ phần đã có (HTTP Range / Addressables cache), không tải lại từ đầu.
• Bấm 'Thử lại' → vòng chờ 'Đang thử kết nối lại…' → thành công thì ẩn và chạy tiếp; 'Thoát game' → Application.Quit() (iOS: chỉ đóng bảng, vì iOS không cho app tự thoát).

---

PROMPT 1 · SPLASH (đã có video Arena3Q_Splash_fire_sound.mp4 – dùng luôn)

Mobile game splash screen, landscape 16:9, 1920x1080. Pure near-black background with drifting orange embers and a soft smoky fire glow along the bottom edge. In the center, the ARENA3Q logo (gold hammered-metal letters, red ink splash behind, a small gold dragon coiled around the Q, subtitle 'TAM QUỐC CHIẾN THUẬT' in gold serif) ignites from burning edges and settles. No buttons, no other text. Duration 3 seconds, then auto-transition to the loading screen.

Unity: phát video bằng VideoPlayer, không cho bỏ qua lần đầu; từ lần mở thứ 2 cho chạm để bỏ qua.

---

PROMPT 2 · LOADING

Mobile game loading screen, landscape 16:9, premium Three Kingdoms chibi gacha style. Full-bleed key art background: chibi Guan Yu (green), Lu Bu (red) and Zhuge Liang (blue-white) charging across a sunset battlefield in front of a fortress. ARENA3Q logo at the top center in the empty sky. Bottom area darkened with a soft vignette for readability. At the bottom, a long THIN loading bar across about 70% of the screen width: dark track with a gold frame, a slim glossy crimson-pink fill with a light highlight line and a sheen sweeping across it. Both ends finish in ornate gold spear-tip ornaments (red lacquer diamond with gold edge) connected by a thin gold rail. Riding ON the bar, its front wheel exactly at the leading edge of the fill: a small wooden supply cart (grain sacks, two little red flags waving, spinning spoked wheels), dust puffs and fire sparks kicking up behind the wheels. The cart, the track and both end ornaments are perfectly aligned on one horizontal center line. Directly to the left of the cart, moving with it, bold white text with dark outline 'Đang tải tài nguyên…' and the percentage in gold '68%'. Below the bar, centered: one bold gold tip line with a gold swirling-cloud ornament on each side ('Thu thập và bồi dưỡng danh tướng, sắp xếp đội hình hợp lý để làm chủ chiến trường!'). No characters on the bar, no 'tap to continue' text. Bottom-left: tiny version text. Bottom-right: tiny health notice and age rating badge. Clean, readable, no clutter, Vietnamese text with correct diacritics.

Negative: extra buttons, popups, gibberish text, cluttered UI, cropped logo, cart floating above or below the bar, misaligned end ornaments.

Unity: xe lương là sprite riêng (bánh xe xoay bằng animation), anchoredPosition.x = clamp(fillAmount × chiều rộng thanh − chiều rộng xe, 0, chiều rộng thanh − chiều rộng xe); thanh, hai đầu mũi giáo và xe dùng chung một pivot Y. Chữ trạng thái đổi sang bên phải xe khi bên trái không đủ chỗ. Mẹo đổi mỗi 3,5 giây (fade + trượt lên 8px), vẫn chạy tiếp khi đã 100%; tải xong tự chuyển sang màn Đăng nhập, không cần chạm.

---

PROMPT 3 · ĐĂNG NHẬP + CHƠI NGAY

Mobile game login screen, landscape 16:9, Three Kingdoms chibi gacha style. Background: the same key art, darkened with a soft dark vignette in the center so the form is readable while the heroes stay visible at the sides. ARENA3Q logo small at the top center. In the CENTER of the screen, below the logo, a wide login panel (about 40% of screen width): deep navy panel with subtle gold cloud patterns, ornate gold scroll corners, a golden dragon-head crest with red silk ribbons on the top center, and gold dragon tassels with red fringe hanging on the left and right edges. Input fields are dark navy slots with a thin glowing blue border. Buttons follow the UI kit: glossy gold ornate button for the main action, red lacquer button with gold edge, blue lacquer buttons with gold edge. Inside, from top to bottom: two tabs 'Đăng nhập' (active, gold underline) and 'Đăng ký'; input field 'Tài khoản' (placeholder 'Email hoặc số điện thoại'); input field 'Mật khẩu'; a row with checkbox 'Ghi nhớ đăng nhập' on the left and a small 'Quên mật khẩu?' link on the right; a red lacquer button with gold edge 'ĐĂNG NHẬP'; a thin divider 'hoặc đăng nhập nhanh'; three equal blue lacquer buttons 'Google', 'Apple', 'Facebook'. Top-right corner: a 'Tiếng Việt' language pill with a globe icon and a round gold-rimmed sound button. Then, separated by a gold line, the largest and brightest button on the screen: a glowing gold metallic button 'CHƠI NGAY' with a small caption under it 'Vào game không cần tài khoản · Có thể liên kết sau'. At the bottom, tiny terms text. Guan Yu on the left and Zhuge Liang on the right remain visible around the panel.

Negative: 'CHƠI NGAY' smaller than 'ĐĂNG NHẬP', panel off-center, logo overlapping the panel, wrong Vietnamese diacritics, fake text.

---

PROMPT 4 · ĐĂNG KÝ

Same layout and style as the login screen (key art background with center vignette, small logo top center, wide lacquer panel with gold border in the CENTER of the screen). Tab 'Đăng ký' is active. Fields from top to bottom: 'Email hoặc số điện thoại'; 'Mã xác minh' with a small gold outline button 'Gửi mã' on the right; then one row with two fields side by side: 'Mật khẩu' (hint: 'tối thiểu 8 ký tự') and 'Nhập lại mật khẩu'. A checkbox 'Tôi đồng ý với Điều khoản dịch vụ và Chính sách bảo mật' with the two terms as gold links. Main red lacquer button 'TẠO TÀI KHOẢN'. Under it a small text link 'Đã có tài khoản? Đăng nhập' and a secondary small link 'Chơi ngay không cần tài khoản'. Clear spacing, large touch targets, Vietnamese text with correct diacritics.

Negative: crowded fields, tiny buttons, fake characters.

---

PROMPT 5 · MÀN HÌNH VÀO GAME (sau khi đăng nhập / Chơi ngay)

Mobile game start screen, landscape 16:9, Three Kingdoms chibi gacha style. Full-bleed key art with the three chibi heroes. ARENA3Q logo large at the top center. Top-left: a compact player chip with a round avatar, name 'Khách #83921' and a small orange warning tag 'Chưa liên kết'. Top-right: three round gold-rimmed icon buttons (announcement bell, customer support, settings gear). Bottom center, over a soft dark gradient: a pill-shaped server selector on dark lacquer with gold trim showing a green status dot, 'S8 · Xích Bích', the tag 'Tốt' and a gold link 'Đổi máy chủ ›'; directly below it a large glowing gold metallic button 'VÀO GAME'. Bottom-left tiny version text; bottom-right tiny health notice.

Negative: more than one primary button, server pill unreadable, clutter covering the heroes' faces.

---

PROMPT 6 · CHỌN MÁY CHỦ

Mobile game server selection popup, landscape 16:9. Background: the key art blurred and darkened. A large centered modal window (about 88% of screen width) in dark red-black lacquer with ornate gold borders and a gold dragon crest on the title bar. Title 'Chọn máy chủ' with a round close X at the top-right. Left sidebar with vertical tabs: 'Máy chủ của tôi', 'Đề xuất' (active, red lacquer with gold edge), 'S1 – S10', 'S11 – S20'. Right area: a highlighted card 'Đăng nhập gần đây' showing a round chibi Zhuge Liang avatar, 'S8 · Xích Bích', 'Lv.45'. Below it a two-column grid of server buttons, each a dark rounded plate with a colored status dot and status word (green 'Tốt', orange 'Đông', red 'Đầy', grey 'Bảo trì'), the server name like 'S20 · Bạch Đế Thành', small tags 'MỚI' (green) or 'HOT' (red), and on servers where the player has a character, a tiny round hero avatar with level. The selected server has a glowing gold border. Bottom bar: a status legend on the left and a gold button 'XÁC NHẬN' on the right. Server names are Three Kingdoms places: Lạc Dương, Trường An, Hứa Xương, Kiến Nghiệp, Thành Đô, Hổ Lao Quan, Trường Bản, Xích Bích, Quan Độ, Phàn Thành…

Negative: inconsistent row heights, unreadable small text, colors as the only status indicator, gibberish names.

---

PROMPT 7 · LIÊN KẾT TÀI KHOẢN KHÁCH (nhắc tạo tài khoản sau)

Mobile game popup, landscape 16:9. Background: the start screen dimmed to 60% black. A centered medium modal in dark lacquer with a gold border and a small gold shield-and-lock emblem at the top. Title 'Liên kết tài khoản'. Short text: 'Bạn đang chơi bằng tài khoản khách. Liên kết để giữ tiến trình khi đổi máy hoặc cài lại game.' A small reward strip with item icons: 'Thưởng liên kết lần đầu'. Four full-width buttons stacked: 'Liên kết Google', 'Liên kết Apple', 'Liên kết Facebook', 'Email / Số điện thoại'. A quiet text button 'Để sau' at the bottom. Round close X at the top-right.

Khi nào hiện: lần đầu đạt cấp [X], khi bấm chip 'Chưa liên kết', trước khi nạp tiền, và khi vào Cài đặt › Tài khoản.
