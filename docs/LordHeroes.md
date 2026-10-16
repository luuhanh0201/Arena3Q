# Arena3Q – Bốn chủ tướng (thiết kế nháp)

Phiên bản: v0.1 · 2026-10-17 · Trạng thái: **🟡 bản nháp chờ duyệt** (tên, ngoại hình, kỹ năng, con số). Kỹ năng **chưa code** (spec §2: ⏳).

Đã chốt ✅ (spec §2): người chơi chọn **1 trong 4 chủ tướng** khi vào game – nhân vật cố định; không chọn phe; bối cảnh **Đông Á cổ trang**; mỗi chủ tướng **1 nội tại + 2 chủ động + 1 tuyệt kỹ**; 2 nam + 2 nữ; animation **skeletal từ PSD tách bộ phận**, vẽ **2 hướng (trước-phải, sau-phải) + lật gương**.

Hình ảnh và prompt: `docs/LordArtPrompts.md`.

---

## 1. Tổng quan 🟡

| | **Cung thủ** | **Kiếm sĩ** | **Mưu sĩ** | **Thống soái** |
|---|---|---|---|---|
| Tên tạm | **Lạc Nhạn** – "Thần Tiễn" | **Vân Phong** – "Kiếm Thánh" | **Minh Nguyệt** – "Quân Sư" | **Trấn Quốc** – "Đại Nguyên Soái" |
| Giới tính | Nữ | Nam | Nữ | Nam |
| Vai trò | Sát thương tầm xa, phá thành | Đột phá cận chiến, tự hồi máu | Khống chế vùng, tăng sức đội lính | Chỉ huy: tăng sức toàn quân, thêm lính |
| Vũ khí | Cung sừng dài, ống tên sau lưng | Song kiếm (kiếm dài + đoản kiếm) | Quạt lông vũ, sách binh thư | Thương dài + cờ lệnh cắm sau lưng |
| Tầm đánh thường | Xa (4.5) | Cận (0.6) | Trung (3.0, phóng luồng khí từ quạt) | Cận (0.9, thương dài) |
| Máu / sát thương | Thấp / cao | Trung / cao | Thấp / thấp | Cao / trung |
| Tốc độ | Nhanh | Nhanh | Trung | Chậm |
| Màu chủ đạo (cố định của nhân vật) | Xanh lục ngọc + nâu da | Trắng bạc + xanh lam đậm | Tím nhạt + trắng ngà | Đỏ son + vàng đồng |
| Độ khó chơi | Dễ | Trung | Khó | Dễ |

Màu đội (ta / địch, sau này PvP) **không** dùng màu chủ đạo ở trên mà dùng một **lớp màu đội riêng** (dải lụa, chùm lông mũ…) tô lại được – xem `LordArtPrompts.md` §4.

---

## 2. Bộ kỹ năng 🟡

Ký hiệu cách dùng: **Tự động** = bấm nút là dùng lên mục tiêu/khu vực quanh chủ tướng; **Chọn vùng** = kéo nút kỹ năng thả vào chiến trường (cách nhập liệu sẽ chốt khi code). Hồi chiêu (HC) tính bằng giây. Mọi con số là gợi ý để chơi thử.

### 2.1 Cung thủ – Lạc Nhạn
| Loại | Tên | Hiệu ứng | Cách dùng | HC |
|---|---|---|---|---|
| Nội tại | **Ưng Nhãn** | +15 % tầm đánh; cứ mỗi đòn thứ 4 bắn xuyên, trúng thêm 1 mục tiêu phía sau | – | – |
| Chủ động 1 | **Liên Châu Tiễn** | Bắn liên tiếp 3 mũi tên vào mục tiêu đang đánh, mỗi mũi 80 % sát thương | Tự động | 8 |
| Chủ động 2 | **Tiễn Vũ** | Mưa tên xuống vùng tròn bán kính 2.5 trong 3 giây, sát thương theo thời gian | Chọn vùng | 14 |
| Tuyệt kỹ | **Phá Thành Hỏa Tiễn** | Một mũi tên lửa khổng lồ bay thẳng, xuyên mọi quân trên đường; trúng thành gây ×3 sát thương | Chọn hướng | 60 |

### 2.2 Kiếm sĩ – Vân Phong
| Loại | Tên | Hiệu ứng | Cách dùng | HC |
|---|---|---|---|---|
| Nội tại | **Kiếm Tâm** | Mỗi đòn trúng hồi 3 % sát thương gây ra; dưới 30 % máu thì +25 % tốc đánh | – | – |
| Chủ động 1 | **Phi Ảnh Đột Kích** | Lướt tới mục tiêu trong bán kính 6, gây sát thương và choáng 1 giây | Tự động (mục tiêu gần nhất) | 10 |
| Chủ động 2 | **Toàn Phong Trảm** | Xoay song kiếm 1.5 giây, sát thương liên tục quanh bán kính 1.5 | Tự động | 12 |
| Tuyệt kỹ | **Vạn Kiếm Quy Tông** | Hàng trăm kiếm khí từ trời rơi xuống vùng hình quạt phía trước (dài 5), sát thương lớn | Chọn hướng | 60 |

### 2.3 Mưu sĩ – Minh Nguyệt
| Loại | Tên | Hiệu ứng | Cách dùng | HC |
|---|---|---|---|---|
| Nội tại | **Thao Lược** | Đội lính của mọi tướng ta trong bán kính 5 quanh Minh Nguyệt +10 % sát thương | – | – |
| Chủ động 1 | **Hỏa Kế** | Đặt trận lửa vùng tròn bán kính 2 trong 5 giây, đốt quân địch đi qua | Chọn vùng | 12 |
| Chủ động 2 | **Bát Quái Trận** | Vẽ trận đồ bát quái bán kính 3 trong 6 giây: quân địch trong vùng chậm 40 %, giảm 20 % sát thương | Chọn vùng | 16 |
| Tuyệt kỹ | **Đông Phong** | Gió lớn quét qua cả chiến trường: đẩy lùi quân địch 3 đơn vị, quân ta +30 % tốc chạy 6 giây | Toàn bản đồ | 70 |

### 2.4 Thống soái – Trấn Quốc
| Loại | Tên | Hiệu ứng | Cách dùng | HC |
|---|---|---|---|---|
| Nội tại | **Quân Lệnh Như Sơn** | Đội lính của Trấn Quốc +2 lính (vẫn ≤ `maxSquadSize`); toàn quân ta +5 % máu tối đa | – | – |
| Chủ động 1 | **Hiệu Triệu** | Bổ sung lính đã chết cho đội gần nhất (tối đa 3 lính) | Tự động | 15 |
| Chủ động 2 | **Trống Trận** | Quân ta trong bán kính 6: +20 % tốc đánh và tốc chạy trong 6 giây | Tự động | 14 |
| Tuyệt kỹ | **Thiết Kỵ Xung Phong** | Gọi một hàng kỵ binh lao qua đường thẳng dài 12, sát thương và hất văng quân địch | Chọn hướng | 60 |

---

## 3. Liên hệ với luật hiện có (để khi code không mâu thuẫn spec)
- Kỹ năng là **lệnh của người chơi**: dùng kỹ năng **không** đổi lệnh di chuyển / tấn công đang chạy; tướng vận chiêu xong thì tiếp tục trạng thái cũ (Idle / Moving / Attacking / Advancing / Combat – spec §6).
- **Dead** thì không dùng được kỹ năng (spec §6 "không gây sát thương khi đã chết").
- Nội tại tác động lên **đội lính** dùng đúng đội của từng tướng (spec §7 ✅ "lính không bao giờ bị gán nhầm").
- Kỹ năng "chọn vùng" dùng cùng luật vùng hợp lệ như lệnh (không thả ngoài chiến trường; được phép thả lên sông).
- Cân bằng con số để giai đoạn sắp phát hành (spec §13 ⏳).

## 4. Câu hỏi còn mở
1. Tên tạm của 4 chủ tướng có giữ không?
2. Có muốn bộ kỹ năng nào thay đổi (ví dụ Mưu sĩ thiên về hồi máu thay vì khống chế)?
3. Cách dùng kỹ năng trên điện thoại: nút cạnh thẻ chủ tướng, hay chạm giữ chủ tướng để hiện vòng kỹ năng?
