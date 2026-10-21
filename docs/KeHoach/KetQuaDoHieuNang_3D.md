# Kết quả đo hiệu năng 3D trên máy thật

Theo `docs/KeHoach/ChuyenSang3D.md` §2 và spec §1 (tối thiểu 60 FPS). Đo bằng `BattleBenchmark` (bản Development):

```
adb shell am start -n com.Arena3Q.Arena3Q/com.unity3d.player.UnityPlayerGameActivity --es arena3q_bench 1 --es seconds 60 [--es render_scale 0.75] [--es tag tên]
```

Bộ đo chạy như sau:
- vào thẳng trận `attack_01` (bộ tướng thử: chủ tướng + 4 tướng);
- triệu hồi mọi thẻ mỗi giây, mọi tướng có tuyệt kỹ thì tung mỗi 6 giây;
- trận kết thúc thì chơi lại;
- khởi động 8 giây rồi mới đo;
- ghi `persistentDataPath/arena3q_bench_<tag>.json` và dòng log `[Arena3Q] BENCH`, xong thì tự thoát.

## 21/10/2026 – Redmi Note 8 Pro (Helio G90T, Mali-G76 MC4, Vulkan, 2340×1080, màn 60 Hz)

APK dev: 141,4 MB lần build đầu, 156,2 MB các lần sau (xem mục còn mở).

| Lượt | Độ phân giải vẽ | Thời gian đo | FPS TB | p95 | p99 | Khung > 16,7 ms | Lượt vẽ TB | SetPass TB | Tam giác TB | Nhiệt pin | Đạt |
|---|---|---|---|---|---|---|---|---|---|---|---|
| rs100 | 1,0 | 60 s | 59,9 | 16,7 ms | 16,9 ms | 0,06 % | 73 | 50 | 101 nghìn | 35,0 → 35,6 °C | ✅ |
| rs100_15min | 1,0 | 15 phút | 59,8 | 16,7 ms | 16,9 ms | 0,31 % (khung chậm nhất 667 ms, lúc chơi lại trận) | 78 | 55 | 101 nghìn | 35,7 → 39,3 °C | ✅ (≥ 55 sau 15 phút) |
| uncapped 1,0 / 0,75 | 1,0 / 0,75 | 60 s | 59,9 / 59,9 | 16,8 / 16,8 ms | — | — | 74 / 70 | 50 / 47 | 99 / 100 nghìn | 38,8 °C | ✅ |

Ngưỡng: FPS TB ≥ 60 (≥ 59,5 sau làm tròn khung), p95 ≤ 18 ms, ≥ 55 sau 15 phút, lượt vẽ ≤ 150, tam giác ≤ 600–800 nghìn.

**Bloom (21/10 tối, commit d691c27; cùng một bản build, `--es bloom 1/0`, APK 141,2 MB):**

| Lượt | Thời gian đo | FPS TB | trung vị | p95 | p99 | Khung > 16,7 ms | Lượt vẽ TB | Nhiệt pin cuối | Đạt |
|---|---|---|---|---|---|---|---|---|---|
| bloom1 (có bloom) | 60 s | 59,9 | 16,66 ms | 16,8 ms | 16,9 ms | 0,17 % | 87 | 38,6 °C | ✅ |
| bloom0 (tắt bloom) | 60 s | 59,9 | 16,67 ms | 16,8 ms | 17,0 ms | 0,39 % | 68 | 38,2 °C | ✅ |

Bloom thêm khoảng 19 lượt vẽ (các bước thu nhỏ / làm mờ của URP), không làm rớt khung trên máy này. Màn 60 Hz vẫn khoá khung nên chưa biết còn dư bao nhiêu. Bộ tướng thử chưa có Mặc Ảnh 3D thật; đo lại khi đủ model. Số gốc: `AgentComms/claude/NghiemThu/P3_Bloom/arena3q_bench_bloom*.json`.

**Giới hạn của lượt đo này (chưa đại diện bản cuối):**
- Tướng trong trận vẫn là khối tạm 3D. Lữ Bố còn là sprite. Mặc Ảnh 3D thật không có trong bộ tướng thử.
- Khi đủ model thật (khoảng 40 nghìn tam giác mỗi tướng, skin 4 xương) phải đo lại.

**Mức dư hiệu năng: chưa đo được.** Màn 60 Hz khoá khung ở 60 FPS dù đặt mục tiêu 120. Độ phân giải vẽ 0,75 và 1,0 cho cùng kết quả, gợi ý GPU chưa phải nút thắt. Muốn biết còn dư bao nhiêu thì gắn **Unity Profiler** vào máy (bản Development) khi có model tướng thật.

## Còn mở
- **APK chênh 15 MB giữa hai lần build gần như cùng code** (141,4 → 156,2 MB). Chưa tìm ra nguyên nhân vì APK cũ đã bị ghi đè; lần sau giữ lại APK cũ để so.
- Phần nặng nhất của APK là atlas sprite 8 hướng của Mặc Ảnh và Lữ Bố (texture ~222 MB chưa nén, phần lớn trong build). Gỡ khỏi build khi hai tướng là 3D thật.
- Gói `com.coplaydev.unity-mcp` (người dùng thêm vào `Packages/manifest.json`) lọt vào cả bản build điện thoại. Lúc khởi động nó báo 14 lần lỗi `ScreenStream.LateUpdate: Failed to create texture`. Không làm hỏng trận, nhưng nên chỉ dùng trong Editor.
