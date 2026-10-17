# Arena3Q – Bốn chủ tướng (thiết kế nháp)

Phiên bản: v0.3 · 2026-10-17 · Trạng thái: **🟡 bản nháp chờ duyệt** (ngoại hình, kỹ năng, con số). Kỹ năng **chưa code** (⏳). Chủ tướng **không có tên riêng** – mang tên người chơi đặt khi đăng nhập (✅). Skin: ⏳ sau này.

Đã chốt ✅ (spec §2): người chơi chọn **1 trong 4 chủ tướng** khi vào game – nhân vật cố định; không chọn phe; bối cảnh **Đông Á cổ trang**; mỗi chủ tướng (như mọi tướng) có **kỹ năng 1 = nội tại (bị động) + kỹ năng 2, 3 (phụ, tự dùng) + tuyệt kỹ (thanh nộ)**; 2 nam + 2 nữ; animation **skeletal từ PSD tách bộ phận**, vẽ **2 hướng (trước-phải, sau-phải) + lật gương**.

Hình ảnh và prompt: `docs/Tuong/LordArtPrompts.md`.

✅ (2026-10-17) Chủ tướng **bắt đầu chưa có kỹ năng**: học nội tại ở Lv2, kỹ năng 2 ở Lv5, kỹ năng 3 ở Lv10, tuyệt kỹ ở Lv15 (🟡 mốc cấp), nâng cấp bằng điểm kỹ năng khi lên cấp. Chỉ số theo cấp, mốc nâng cấp từng kỹ năng, đột phá: `docs/KeHoach/HeroProgression.md`.

---

## 1. Tổng quan 🟡

| | **Cung thủ** | **Kiếm sĩ** | **Mưu sĩ** | **Thống soái** |
|---|---|---|---|---|
| Giới tính | Nữ | Nam | Nữ | Nam |
| Vai trò | Sát thương tầm xa, phá thành | Đột phá cận chiến, tự hồi máu | Khống chế vùng, tăng sức đội lính | Chỉ huy: tăng sức toàn quân, thêm lính |
| Vũ khí | Cung sừng dài, ống tên sau lưng | Song kiếm (kiếm dài + đoản kiếm) | Quạt lông vũ, sách binh thư | Thương dài + cờ lệnh cắm sau lưng |
| Tầm đánh thường | Xa (4.5) | Cận (0.6) | Trung (3.0, phóng luồng khí từ quạt) | Cận (0.9, thương dài) |
| Máu / sát thương | Thấp / cao | Trung / cao | Thấp / thấp (kỹ năng đánh vùng) | Cao / thấp (mạnh nhờ đội lính) |
| Chỉ số Lv1: Máu / Công (`HeroProgression.md` §5) | 560 / 58 | 760 / 36 | 600 / 32 | 950 / 26 |
| Tốc chạy | Nhanh (2.9) | Nhanh (3.0) | Trung (2.6) | Chậm (2.3) |
| Tốc đánh (đòn/giây) | 0.9 | 1.4 | 0.8 | 1.0 |
| Màu chủ đạo (cố định của nhân vật) | Xanh lục ngọc + nâu da | Trắng bạc + xanh lam đậm | Tím nhạt + trắng ngà | Đỏ son + vàng đồng |
| Độ khó chơi | Dễ | Trung | Khó | Dễ |

Màu đội (ta / địch, sau này PvP) **không** dùng màu chủ đạo ở trên mà dùng một **lớp màu đội riêng** (dải lụa, chùm lông mũ…) tô lại được – xem `LordArtPrompts.md` §4.

---

## 2. Bộ kỹ năng 🟡

Cách dùng (✅ spec §3 "Kỹ năng trên thẻ"): **kỹ năng 2 và 3 tự dùng** ngay khi hồi chiêu xong (cùng sẵn sàng thì **kỹ năng 2 trước, rồi kỹ năng 3**) và **tự nhắm** (mục tiêu đang đánh → cụm địch đông nhất trong tầm → thành địch; không có mục tiêu thì chờ). **Tuyệt kỹ dùng thanh nộ** (tích khi tướng gây sát thương, không có thời gian hồi): **chạm thẻ** = dùng, tự nhắm (chưa đầy nộ → hiện "kỹ năng chưa thể sử dụng"); **kéo thẻ** = tự chọn điểm / hướng; **giữ thẻ 1.5 s** = chọn tướng. Thẻ hiển thị icon kỹ năng 2, 3 + đếm ngược, và thanh nộ. HC = hồi chiêu (giây). Mọi con số là gợi ý để chơi thử.

### 2.1 Cung thủ
| Loại | Tên | Hiệu ứng | Cách dùng | HC (s) / Nộ |
|---|---|---|---|---|
| Kỹ năng 1 – Nội tại | **Ưng Nhãn** | +15 % tầm đánh; cứ mỗi đòn thứ 4 bắn xuyên, trúng thêm 1 mục tiêu phía sau | – | – |
| Kỹ năng 2 | **Liên Châu Tiễn** | Bắn liên tiếp 3 mũi tên vào mục tiêu đang đánh, mỗi mũi 80 % sát thương | Tự dùng (tự nhắm) | 8 |
| Kỹ năng 3 | **Tiễn Vũ** | Mưa tên xuống vùng tròn bán kính 2.5 trong 3 giây, sát thương theo thời gian | Tự dùng (tự nhắm vùng) | 14 |
| Tuyệt kỹ | **Phá Thành Hỏa Tiễn** | Một mũi tên lửa khổng lồ bay thẳng, xuyên mọi quân trên đường; trúng thành gây ×3 sát thương | Thẻ: chạm (tự nhắm) / kéo (tự chọn hướng) | Nộ |

### 2.2 Kiếm sĩ
| Loại | Tên | Hiệu ứng | Cách dùng | HC (s) / Nộ |
|---|---|---|---|---|
| Kỹ năng 1 – Nội tại | **Kiếm Tâm** | Mỗi đòn trúng hồi 3 % sát thương gây ra; dưới 30 % máu thì +25 % tốc đánh | – | – |
| Kỹ năng 2 | **Phi Ảnh Đột Kích** | Lướt tới mục tiêu trong bán kính 6, gây sát thương và choáng 1 giây | Tự dùng (mục tiêu gần nhất) | 10 |
| Kỹ năng 3 | **Toàn Phong Trảm** | Xoay song kiếm 1.5 giây, sát thương liên tục quanh bán kính 1.5 | Tự dùng (tự nhắm) | 12 |
| Tuyệt kỹ | **Vạn Kiếm Quy Tông** | Hàng trăm kiếm khí từ trời rơi xuống vùng hình quạt phía trước (dài 5), sát thương lớn | Thẻ: chạm (tự nhắm) / kéo (tự chọn hướng) | Nộ |

### 2.3 Mưu sĩ
| Loại | Tên | Hiệu ứng | Cách dùng | HC (s) / Nộ |
|---|---|---|---|---|
| Kỹ năng 1 – Nội tại | **Thao Lược** | Đội lính của mọi tướng ta trong bán kính 5 quanh Mưu sĩ +10 % sát thương | – | – |
| Kỹ năng 2 | **Hỏa Kế** | Đặt trận lửa vùng tròn bán kính 2 trong 5 giây, đốt quân địch đi qua | Tự dùng (tự nhắm vùng) | 12 |
| Kỹ năng 3 | **Bát Quái Trận** | Vẽ trận đồ bát quái bán kính 3 trong 6 giây: quân địch trong vùng chậm 40 %, giảm 20 % sát thương | Tự dùng (tự nhắm vùng) | 16 |
| Tuyệt kỹ | **Đông Phong** | Gió lớn quét qua cả chiến trường: đẩy lùi quân địch 3 đơn vị, quân ta +30 % tốc chạy 6 giây | Thẻ: chạm (toàn bản đồ, không cần nhắm) | Nộ |

### 2.4 Thống soái
| Loại | Tên | Hiệu ứng | Cách dùng | HC (s) / Nộ |
|---|---|---|---|---|
| Kỹ năng 1 – Nội tại | **Quân Lệnh Như Sơn** | Đội lính của Thống soái +2 lính (vẫn ≤ `maxSquadSize`); toàn quân ta +5 % máu tối đa | – | – |
| Kỹ năng 2 | **Hiệu Triệu** | Bổ sung lính đã chết cho đội gần nhất (tối đa 3 lính) | Tự dùng (tự nhắm) | 15 |
| Kỹ năng 3 | **Trống Trận** | Quân ta trong bán kính 6: +20 % tốc đánh và tốc chạy trong 6 giây | Tự dùng (tự nhắm) | 14 |
| Tuyệt kỹ | **Thiết Kỵ Xung Phong** | Gọi một hàng kỵ binh lao qua đường thẳng dài 12, sát thương và hất văng quân địch | Thẻ: chạm (tự nhắm) / kéo (tự chọn hướng) | Nộ |

---

## 3. Liên hệ với luật hiện có (để khi code không mâu thuẫn spec)
- Kỹ năng là **lệnh của người chơi**: dùng kỹ năng **không** đổi lệnh di chuyển / tấn công đang chạy; tướng vận chiêu xong thì tiếp tục trạng thái cũ (Idle / Moving / Attacking / Advancing / Combat – spec §6).
- **Dead** thì không dùng được kỹ năng (spec §6 "không gây sát thương khi đã chết").
- Nội tại tác động lên **đội lính** dùng đúng đội của từng tướng (spec §7 ✅ "lính không bao giờ bị gán nhầm").
- Kéo thẻ để nhắm tuyệt kỹ dùng cùng luật vùng hợp lệ như lệnh (không thả ngoài chiến trường; được phép thả lên sông; thả trên UI = huỷ).
- Thanh nộ: 🟡 (người dùng xác nhận tạm thời) chỉ tính sát thương **do chính tướng** gây ra (không tính đội lính, không tính khi bị đánh), tỉ lệ với sát thương; mỗi tướng cấu hình `rageMax` và tốc độ tích trong `HeroDefinition`; không tự giảm. ✅ Dùng xong về 0; tướng chết mất nộ; không mang sang trận sau.
- 🟡 Gợi ý `rageMax`: đầy sau khoảng 15–20 đòn đánh thường trúng mục tiêu (spec §3).
- 🟡 Vận chiêu: hiệu lực ở mốc `Cast` của animation; bị khống chế / chết trước mốc đó thì kỹ năng không bị tính (spec §3). Mốc và clip từng kỹ năng: `docs/Tuong/LordAnimations.md`.
- 🟡 Kỹ năng đẩy / hất văng quân địch (Đông Phong, Thiết Kỵ Xung Phong) tuân theo vùng cấm và biên bản đồ (spec §3, §8).
- ✅ Mọi tướng (cả tướng thường) có cùng cấu trúc: nội tại + kỹ năng 2, 3 + tuyệt kỹ; bộ kỹ năng tướng thường (🟡 nháp): `docs/Tuong/NormalHeroes.md`.
- ✅ **Tốc đánh** (spec §6): quyết định nhịp đánh và tốc độ ra đòn; animation đánh tua theo tốc đánh. Các hiệu ứng "+% tốc đánh" (Kiếm Tâm, Trống Trận) nhân vào tốc đánh này.
- Cân bằng con số để giai đoạn sắp phát hành (spec §13 ⏳).

## 4. Câu hỏi còn mở
1. Có muốn bộ kỹ năng nào thay đổi (ví dụ Mưu sĩ thiên về hồi máu thay vì khống chế)?
2. Tốc đánh gợi ý ở bảng §1 (0.9 / 1.4 / 0.8 / 1.0 đòn/giây) – để chơi thử, cân bằng sau (spec §13 ⏳).
3. ⚠️ **Phi Ảnh Đột Kích** (Kiếm sĩ, kỹ năng 2 tự dùng, lướt tới mục tiêu) xung đột với ✅ IgnoreUntilDestination khi tướng đang Moving – đề xuất: kỹ năng làm tướng dịch chuyển không tự dùng khi đang Moving (spec §13 mục 3).

Đã trả lời (2026-10-17): nộ chỉ tính sát thương do tướng gây ra (tạm thời); không giữ nộ khi chết / sang trận sau; chạm thẻ khi chưa đủ nộ hiện "kỹ năng chưa thể sử dụng"; kỹ năng 1 là nội tại, kỹ năng 2 → 3 tự dùng theo thứ tự ưu tiên.
