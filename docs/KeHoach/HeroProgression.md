# Arena3Q – Cấp, sao, kỹ năng, phe và cân bằng chỉ số tướng

Phiên bản: v0.1 · 2026-10-17 · Trạng thái: **🟡 thiết kế chờ duyệt** (plan tạm trong `docs/KeHoach/`; duyệt xong chuyển sang `docs/Tuong/`) · code ⏳ (spec §11). Chỉ số trong asset prototype **giữ nguyên**: tinh chỉnh độ khó vẫn để giai đoạn sắp phát hành (spec §13); tài liệu này là **khung** cho hệ thống cấp / sao khi làm.

Đã chốt (người dùng, 2026-10-17):
- **Chủ tướng bắt đầu chưa có kỹ năng**: lên cấp thì học dần từng kỹ năng, rồi nâng cấp từng kỹ năng bằng **điểm nhận khi lên cấp**.
- Các tướng khác **mở kỹ năng bằng sao** (gom mảnh tướng, ví dụ mảnh Lữ Bố).
- **Cấp tối đa 60** cho mọi tướng.
- Chỉ số tăng theo cấp **ưu tiên điểm mạnh của hệ**, mọi hệ phải cân bằng.
- Danh tướng nhận sau này thuộc phe **Thục, Ngụy, Ngô** hoặc **Quần** (Lữ Bố, Đổng Trác, Viên Thiệu, Điêu Thuyền…); tác dụng của phe = **cộng hưởng phe**.
- 4 tướng thường hiện có (`docs/Tuong/NormalHeroes.md`) là **tướng khởi đầu, không phe**: chỉ số gốc thấp hơn danh tướng, dễ tăng sao.

---

## 1. Ba loại tướng

| | **Chủ tướng** | **Tướng khởi đầu** | **Danh tướng** (sau này) |
|---|---|---|---|
| Gồm | Cung thủ, Kiếm sĩ, Mưu sĩ, Thống soái | Xạ Thủ, Kỵ Binh, Giáp Sĩ, Đao Khách | Lữ Bố, Quan Vũ, Tào Tháo, Chu Du… |
| Có được | chọn khi vào game (spec §2 ✅) | tặng trong cốt truyện chương 1 | gom mảnh (nhiệm vụ, sự kiện, cửa hàng – ⏳) |
| Phe | không | không | Thục / Ngụy / Ngô / Quần |
| Cấp | 1–60 | 1–60 | 1–60 |
| Mở và nâng kỹ năng | **theo cấp** + điểm kỹ năng (§6) | **theo sao** (§7) | **theo sao** (§7) |
| Sao | không có – thay bằng **đột phá** ở Lv15 / 30 / 45 / 60 (§5.3) | 1–5★, ít mảnh | 1–5★ |
| Sức mạnh ở Lv1 (1★) | **100 %** | **85 %** | **100 %** |
| Sức mạnh tối đa (cùng cấp) | 140 % (4 lần đột phá) | 119 % (5★) | 140 % (5★) |

---

## 2. Chỉ số

| Chỉ số | Ý nghĩa | Tăng theo cấp |
|---|---|---|
| **Máu** | | có – theo hệ (§4) |
| **Công** | sát thương đòn đánh thường; mọi kỹ năng tính bằng **% Công** (như bảng kỹ năng hiện có) | có – theo hệ (§4) |
| **Tốc đánh** | đòn/giây (spec §6) | không |
| **Tầm đánh**, **tốc chạy** | | không |
| **Giảm sát thương** | % sát thương nhận vào bị bớt; cộng dồn từ nội tại, phe, kỹ năng; 🟡 tối đa **75 %** | không (cố định theo tướng) |
| **Chí mạng** | tỉ lệ cơ bản **5 %**, đòn chí mạng **×1.5** | không |
| **Thống suất** | sức đội lính: lính của tướng có Máu và Công × Thống / 100 (§10) | không (sao, trang bị ⏳ có thể tăng) |

**Sát thương một đòn** = Công × hệ số (đánh thường 100 %, kỹ năng theo bảng kỹ năng) × (1 + tăng sức) × (×1.5 nếu chí mạng) × (1 − giảm sát thương của mục tiêu).

---

## 3. Cách cân bằng

1. **Đấu tay đôi:** tướng A thắng tướng B khi **DPS_A × EHP_A > DPS_B × EHP_B**, với DPS = Công × tốc đánh, EHP (máu hiệu dụng) = Máu / (1 − giảm sát thương). Vì vậy dùng **sức mạnh S = √(DPS × EHP)** để so sánh.
2. **Lợi thế không nằm trong con số** (tầm xa, đánh vùng, khống chế, thêm lính, che chắn đồng đội) được tính bằng **hệ số vai trò**: tướng có lợi thế lớn thì S thô thấp hơn. Mục tiêu: **S × (1 + vai trò) bằng nhau** trong cùng loại tướng.
3. **Mốc so sánh** = "tướng chuẩn" 900 Máu, 45 Công, 1 đòn/giây – đúng bằng chủ tướng placeholder hiện tại (`Hero_Lord`) → S = 201.
4. **Tăng theo cấp:** mỗi hệ chia "phần tăng" khác nhau giữa Công và Máu, nhưng **tích (1 + tăng Công) × (1 + tăng Máu) như nhau** (≈ ×1.061 mỗi cấp). Vì vậy S của mọi hệ tăng cùng tốc độ (≈ 3 %/cấp) → **cân bằng ở cấp 1 thì cân bằng ở mọi cấp**. Đã kiểm tra bằng bảng tính: tỉ lệ giữa các tướng lệch **dưới 0.3 %** từ Lv1 tới Lv60.
5. Kết quả là **mô hình**: khi code xong cấp / kỹ năng, kiểm chứng bằng test mô phỏng trận (giống các test `Balance_*` hiện có) rồi mới chỉnh.

---

## 4. Hệ tăng trưởng (theo vai trò, không theo phe)

Chỉ số ở cấp L = chỉ số Lv1 × (1 + tỉ lệ)^(L − 1).

| Hệ | Công / cấp | Máu / cấp | Lv60 so với Lv1 | Điểm mạnh được tăng | Tướng |
|---|---|---|---|---|---|
| **Công kích** | **+3.6 %** | +2.4 % | Công ×8.06 · Máu ×4.05 | sát thương (cả kỹ năng vùng) | Cung thủ, Mưu sĩ, Xạ Thủ |
| **Cân bằng** | +3.3 % | +2.7 % | Công ×6.79 · Máu ×4.82 | vừa đánh vừa trụ | Kiếm sĩ, Kỵ Binh, Đao Khách |
| **Phòng thủ** | +2.4 % | **+3.6 %** | Công ×4.05 · Máu ×8.06 | máu (trụ tuyến đầu) | Thống soái, Giáp Sĩ |

- Lên cấp, **khác biệt giữa các hệ rõ dần**: ở Lv1 Thống soái có máu gấp 1.7 lần Cung thủ, ở Lv60 gấp 3.4 lần; Cung thủ có Công gấp 2.2 lần Thống soái, ở Lv60 gấp 4.4 lần.
- Điểm mạnh **không phải chỉ số** (khống chế và hỗ trợ của Mưu sĩ, tăng sức toàn quân của Thống soái) tăng qua **mốc nâng cấp kỹ năng** (§6.1), không qua Máu / Công.
- **Phe không đổi hệ tăng trưởng**: điểm mạnh của phe thể hiện qua cộng hưởng (§9), còn chỉ số riêng của từng danh tướng vẫn theo vai trò – để phe nào cũng đủ cả đánh, trụ, hỗ trợ.

---

## 5. Chủ tướng

### 5.1 Chỉ số Lv1 🟡
| | **Cung thủ** | **Kiếm sĩ** | **Mưu sĩ** | **Thống soái** |
|---|---|---|---|---|
| Hệ tăng trưởng | Công kích | Cân bằng | Công kích | Phòng thủ |
| Máu | 560 | 760 | 600 | **950** |
| Công | **58** | 36 | 32 | 26 |
| Tốc đánh (đòn/giây) | 0.9 | **1.4** | 0.8 | 1.0 |
| Giảm sát thương | 0 % | 5 % | 0 % | **10 %** |
| Tầm đánh | **4.5** (nội tại +15 % → 5.2) | 0.6 | 3.0 | 0.9 |
| Tốc chạy | 2.9 | **3.0** | 2.6 | 2.3 |
| Thống suất | 100 | 100 | 100 | **110** |
| Chí mạng | 5 % | 5 % | 5 % | 5 % |
| DPS · EHP | 52.2 · 560 | 50.4 · 800 | 25.6 · 600 | 26.0 · 1056 |
| S thô (so với chuẩn) | 0.85 | 1.00 | 0.62 | 0.82 |
| Vai trò | +18 % tầm xa, bắn xuyên | 0 (thuần cận chiến) | +62 % tầm trung, **đánh vùng**, khống chế mạnh nhất, tăng sức lính | +22 % thêm 2 lính, Thống 110, tăng máu toàn quân |
| **S × (1 + vai trò)** | **1.00** | **1.00** | **1.00** | **1.00** |

DPS đơn mục tiêu của Mưu sĩ thấp vì kỹ năng của Mưu sĩ **trúng nhiều địch một lúc**; Thống soái tự đánh yếu vì sức mạnh nằm ở **đội lính** (6 lính, Thống 110).

### 5.2 Chỉ số theo cấp (chưa tính đột phá) – Máu / Công
| Cấp | Cung thủ | Kiếm sĩ | Mưu sĩ | Thống soái |
|---|---|---|---|---|
| 1 | 560 / 58 | 760 / 36 | 600 / 32 | 950 / 26 |
| 10 | 693 / 80 | 966 / 48 | 743 / 44 | 1306 / 32 |
| 20 | 879 / 114 | 1261 / 67 | 942 / 63 | 1860 / 41 |
| 30 | 1114 / 162 | 1646 / 92 | 1194 / 89 | 2649 / 52 |
| 40 | 1412 / 230 | 2148 / 128 | 1513 / 127 | 3774 / 66 |
| 50 | 1790 / 328 | 2804 / 177 | 1918 / 181 | 5375 / 83 |
| 60 | 2269 / 467 | 3660 / 244 | 2431 / 258 | 7655 / 105 |
| Kiểm tra S × (1 + vai trò) ở Lv60 | 1.00 | 1.00 | 1.00 | 1.00 |

### 5.3 Đột phá chủ tướng 🟡
Chủ tướng không có sao, nên để **ngang danh tướng 5★**, chủ tướng **đột phá** khi đạt **Lv15, 30, 45, 60**: mỗi lần **+10 % Máu và Công** (cộng dồn, tối đa +40 % → sức mạnh ×1.4, bằng danh tướng 5★). Lv15 cũng là lúc học tuyệt kỹ → một mốc lớn. Đột phá tự động hay cần vật phẩm: câu hỏi §13.

---

## 6. Kỹ năng chủ tướng: học theo cấp, nâng bằng điểm

| Cấp chủ tướng | Học được |
|---|---|
| 1 | chưa có kỹ năng (chỉ đánh thường) |
| **2** | kỹ năng 1 – **nội tại** |
| **5** | **kỹ năng 2** |
| **10** | **kỹ năng 3** |
| **15** | **tuyệt kỹ** (+ đột phá lần 1) |

- **Điểm kỹ năng:** mỗi lần lên cấp từ **Lv5** nhận **1 điểm** → tới Lv60 có **56 điểm** = vừa đủ nâng cả 4 kỹ năng từ cấp 1 lên **cấp 15** (4 × 14).
- **Giới hạn:** cấp kỹ năng ≤ **cấp chủ tướng ÷ 4** (làm tròn lên) → không dồn hết điểm vào một kỹ năng; điểm chưa dùng được giữ lại. Với giới hạn này, ở Lv10, Lv20, Lv40, Lv60 người chơi nâng **vừa hết** số điểm đang có.
- **Mỗi cấp kỹ năng:** **+5 %** sát thương / hồi máu / khiên của kỹ năng (cấp 15: +70 %). Các hiệu ứng khác (thời gian, bán kính, % làm chậm, % tăng sức, hồi chiêu) **chỉ tăng ở mốc 5, 10, 15** (bảng dưới).
- **Trước khi học** (spec §3): icon kỹ năng hiện **ổ khoá + cấp cần**; chưa có tuyệt kỹ thì thẻ **không hiện thanh nộ** và không tích nộ; chạm thẻ hiện thông báo "kỹ năng chưa thể sử dụng" như luật hiện có.

### 6.1 Mốc nâng cấp từng kỹ năng chủ tướng 🟡
| Chủ tướng | Kỹ năng | Mốc 5 | Mốc 10 | Mốc 15 |
|---|---|---|---|---|
| Cung thủ | Ưng Nhãn (nội tại) | tầm +15 % → +20 % | bắn xuyên mỗi đòn thứ **3** | mũi xuyên trúng thêm **2** mục tiêu |
| | Liên Châu Tiễn | 4 mũi tên | hồi chiêu 8 → 6 s | 5 mũi; mũi cuối chắc chắn chí mạng |
| | Tiễn Vũ | bán kính 2.5 → 3 | địch trong vùng chậm 20 % | kéo dài 3 → 4 s |
| | Phá Thành Hỏa Tiễn | để lại vệt lửa 3 s trên đường bay | trúng thành ×3 → ×4 | nổ lửa bán kính 2 ở cuối đường bay |
| Kiếm sĩ | Kiếm Tâm (nội tại) | hồi 3 % → 5 % sát thương gây ra | ngưỡng tăng tốc đánh 30 % → 40 % máu | dưới ngưỡng thêm +20 % sát thương |
| | Phi Ảnh Đột Kích | choáng 1 → 1.3 s | hồi chiêu 10 → 8 s | +100 % sát thương nếu mục tiêu dưới 50 % máu |
| | Toàn Phong Trảm | bán kính 1.5 → 2 | 1.5 → 2 s | mỗi nhịp trúng hồi 1 % máu |
| | Vạn Kiếm Quy Tông | dài 5 → 6 | địch trúng chậm 30 % trong 2 s | thêm một đợt kiếm 50 % sát thương |
| Mưu sĩ | Thao Lược (nội tại) | lính +10 % → +15 % sát thương | bán kính 5 → 6 | lính trong vùng −10 % sát thương nhận vào |
| | Hỏa Kế | bán kính 2 → 2.5 | 5 → 7 s | địch trong lửa −20 % tốc đánh |
| | Bát Quái Trận | chậm 40 % → 50 % | 6 → 8 s | quân ta trong trận hồi 2 % máu mỗi giây |
| | Đông Phong | đẩy lùi 3 → 4 | quân ta +30 % → +40 % tốc chạy | quân ta thêm +20 % tốc đánh 6 s |
| Thống soái | Quân Lệnh Như Sơn (nội tại) | toàn quân +5 % → +7 % máu | +10 % máu | lính của mọi tướng ta +5 % Công |
| | Hiệu Triệu | bổ sung tối đa 3 → 4 lính | hồi chiêu 15 → 12 s | lính vừa bổ sung có khiên 20 % máu trong 5 s |
| | Trống Trận | +20 % → +25 % | bán kính 6 → 8 | 6 → 8 s |
| | Thiết Kỵ Xung Phong | dài 12 → 14 | choáng 1 s sau khi hất văng | 2 hàng kỵ binh (hàng sau 50 % sát thương) |

---

## 7. Sao: tướng khởi đầu và danh tướng

| Sao | Mảnh cần (danh tướng / khởi đầu) | Kỹ năng | Máu và Công |
|---|---|---|---|
| **1★** | 10 / 5 (nhận tướng) | **nội tại + kỹ năng 2**, cấp 1 | +0 % |
| **2★** | 20 / 10 | **+ kỹ năng 3**; mọi kỹ năng cấp 4 | +10 % |
| **3★** | 40 / 20 | **+ tuyệt kỹ**; mọi kỹ năng cấp 7 (qua mốc 5) | +20 % |
| **4★** | 80 / 40 | mọi kỹ năng cấp 11 (qua mốc 10) | +30 % |
| **5★** | 160 / 80 | mọi kỹ năng cấp 15 (qua mốc 15) | +40 % |

- Cấp kỹ năng của tướng khởi đầu / danh tướng **do sao quyết định** (không dùng điểm kỹ năng); +5 % mỗi cấp như chủ tướng.
- Mốc chung khi chưa có bảng riêng: **mốc 5** hiệu ứng phụ +25 % (thời gian, bán kính hoặc số mục tiêu) · **mốc 10** hồi chiêu −20 % (tuyệt kỹ: nộ cần −15 %) · **mốc 15** thêm một hiệu ứng riêng – viết khi duyệt bộ kỹ năng từng tướng.
- Lên sao không đổi hệ tăng trưởng; chỉ số sao nhân vào chỉ số theo cấp.

---

## 8. Tướng khởi đầu – chỉ số 1★ Lv1 🟡

Ngân sách 85 % chủ tướng. Cột "hiện tại" là asset prototype (**chưa đổi**).

| | **Xạ Thủ** | **Kỵ Binh** | **Giáp Sĩ** | **Đao Khách** |
|---|---|---|---|---|
| Hệ tăng trưởng | Công kích | Cân bằng | Phòng thủ | Cân bằng |
| Máu | 450 | 650 | 1200 | 700 |
| Công: đề xuất (hiện tại) | **52** (30) | 37 (35) | **20** (25) | 38 (40) |
| Tốc đánh | 0.9 | 1.0 | 0.8 | 1.1 |
| Giảm sát thương | 0 % | 0 % | 15 % (nội tại Kiên Thủ) | 0 % |
| Tầm · tốc chạy | 4.5 · 2.6 | 0.6 · 3.6 | 0.6 · 2.2 | 0.6 · 2.8 |
| Thống suất | 100 | 100 | 100 | 100 |
| Vai trò | +18 % tầm xa | +10 % nhanh nhất, choáng | +15 % che chắn đồng đội | 0 |
| **S × (1 + vai trò)** | **0.85** | **0.85** | **0.86** | **0.85** |
| Lv60 (Máu / Công) | 1824 / 419 | 3130 / 251 | 9670 / 81 | 3371 / 258 |

Theo mô hình, với asset hiện tại: Xạ Thủ **yếu nhất** (Công 30 → 0.65, mục tiêu 0.85), Giáp Sĩ **mạnh hơn mục tiêu** (Công 25 → 0.98), Kỵ Binh 0.82, Đao Khách 0.88. Chưa sửa asset – báo để biết khi cân bằng.

---

## 9. Phe và cộng hưởng 🟡

- Tính theo **đội hình mang vào trận** (5 thẻ). Chủ tướng và tướng khởi đầu **không tính** phe nào.
- Có hiệu lực cho **mọi tướng và lính ta** ngay khi ra sân; tướng chết **không** làm mất cộng hưởng.
- Mỗi bậc có **giá trị như nhau giữa các phe**: 2 tướng ≈ +6 % sức mạnh (DPS × EHP), 3 tướng ≈ +12 %, 4 tướng ≈ +21 %.

| Phe | Điểm mạnh | 2 tướng | 3 tướng | 4 tướng |
|---|---|---|---|---|
| **Thục** | tấn công | +6 % Công | +12 % Công | +20 % Công |
| **Ngụy** | phòng thủ | +3 % Máu, +3 % giảm sát thương | +6 % Máu, +6 % giảm | +10 % Máu, +10 % giảm |
| **Ngô** | tốc độ và kỹ năng | +4 % tốc đánh, +10 % tốc tích nộ | +8 % tốc đánh, +20 % tích nộ | +12 % tốc đánh, +30 % tích nộ, −10 % hồi chiêu |
| **Quần** | chí mạng | +3 % Công, +6 % chí mạng | +6 % Công, +12 % chí mạng | +8 % Công, +15 % chí mạng, chí mạng ×1.75 |

Ví dụ làm chỉ số danh tướng: **Lữ Bố** (Quần, hệ Cân bằng, cận chiến không hỗ trợ → vai trò 0, ngân sách 100 %): chọn Máu 800, tốc đánh 1.1, giảm 5 % → tính ra Công **44** để S = 1.00. Thống suất lấy theo truyện (90–130); mỗi +10 Thống trên 100 ≈ +5 % sức mạnh (đội lính ≈ một nửa sức mạnh của tướng) → trừ vào ngân sách tương ứng.

---

## 10. Lính và quân địch theo cấp

**Lính của tướng** (lính ta, `Troop_Soldier`): Máu và Công = chỉ số gốc × **1.03^(L − 1)** (L = cấp tướng chỉ huy) × **Thống / 100**.
| Cấp tướng | 1 | 10 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|
| Lính (Thống 100) Máu / Công | 120 / 12 | 157 / 16 | 210 / 21 | 283 / 28 | 380 / 38 | 511 / 51 | 686 / 69 |

**Quân địch:** chỉ số trong các file chương (`docs/QuanDich/`) là ở **cấp đầu chương**; mỗi cấp ải thêm **+3 % Máu và Công** (cùng tốc độ với "tướng chuẩn"). Chương N bắt đầu ở **cấp 1 + 9 × (N − 1)**: chương 1 cấp 1–9, chương 2 cấp 10–18, chương 3 cấp 19–27… – khớp mức "mạnh hơn chương trước ~30 %" đã ghi trong file chương (1.03⁹ = 1.30). Lv60 ≈ chương 7.

Sao và đột phá làm người chơi mạnh hơn đường chuẩn (tối đa ×1.4): độ khó các chương cuối tính theo sức mạnh dự kiến của người chơi – chỉnh khi cân bằng (spec §13 ⏳).

---

## 11. Lực chiến (số hiển thị trên thẻ, màn chọn tướng) 🟡
**Lực chiến = 10 × S × (1 + vai trò) × (1 + 0.004 × tổng cấp các kỹ năng đã học)**, với S tính từ chỉ số hiện tại (đã gồm cấp, sao, đột phá). Bốn chủ tướng Lv1 cùng ≈ **2 010** – hai tướng cùng lực chiến là ngang sức.

---

## 12. Dữ liệu (khi code)
- `HeroDefinition` thêm: loại tướng (chủ / khởi đầu / danh), phe, hệ tăng trưởng, giảm sát thương, chí mạng, Thống suất, hệ số vai trò (để tính lực chiến), cấp mở từng kỹ năng (chủ tướng), bảng mốc kỹ năng.
- `ProgressionConfig` (mới): tỉ lệ tăng của 3 hệ, bảng sao (mảnh, % chỉ số, cấp kỹ năng), đột phá chủ tướng, điểm kỹ năng và giới hạn, bảng cộng hưởng phe, giới hạn giảm sát thương, công thức lực chiến.
- Mọi con số là **trường cấu hình** (🟡). Tiến trình người chơi (cấp, mảnh, sao) lưu ở tài khoản – cần backend (⏳).

---

## 13. Câu hỏi còn mở
1. **Đột phá chủ tướng** (§5.3): tự động khi đạt cấp, hay cần vật phẩm / nhiệm vụ cốt truyện?
2. **Tướng khởi đầu tặng ở mấy sao?** Chủ tướng tới Lv15 mới có tuyệt kỹ, tướng 1★ chưa có tuyệt kỹ → những ải đầu **không ai dùng được tuyệt kỹ**, người chơi chưa học thao tác chạm thẻ. Đề xuất: tặng **Giáp Sĩ 3★** ở ải hướng dẫn (có tuyệt kỹ để dạy), các tướng khởi đầu khác 1★.
3. **Thống soái "+2 lính"**: theo mô hình, đội lính ≈ một nửa sức mạnh của tướng nên nội tại này rất mạnh. Nếu chơi thử thấy Thống soái quá mạnh, đề xuất giảm còn **+1 lính** trước khi giảm chỉ số.
4. **Độ hiếm danh tướng** (ví dụ Lữ Bố mạnh hơn tướng ít tên tuổi): hiện mọi danh tướng cùng ngân sách – ⏳.
5. **Kinh nghiệm cần để lên cấp** và nguồn kinh nghiệm: ⏳ thiết kế cùng phần thưởng ải.
