# Lữ Bố – 00 Hiện trạng (2026-10-20) – **CHƯA ĐẠT · ~50 %**

Claude đo độc lập trên `docs/art/concepts/LuBu_Arena3Q/LuBu_Arena3Q_Smooth.blend` (mở chỉ đọc, không lưu). Lượt hiện tại của Codex chỉ làm Mặc Ảnh; Lữ Bố chưa được sửa.

## Cần nhìn gì
- `1_Model_NhieuGoc/Idle_Walk_Run.png`, `Attack_Victory_Defeat.png`: mỗi hàng là frame tệ nhất của một clip; cột = 3/4 trước, sau, bên, trên, **ảnh tô hồng đỉnh bị giãn / ép**, cận cảnh. Hàng đầu là tư thế gốc (không lỗi) để so.
  - Lông trĩ trên mũ **gãy khúc zigzag** (Run, Attack, Victory, Defeat – rõ nhất ở cột trên và bên).
  - Vạt áo đỏ bị **kéo thành mảng sắc cạnh**.
- `2_CanCanh_TungVung/Idle_f21.png`, `Attack01_f13.png`: cận cảnh theo vùng (lông, vạt áo / áo choàng, tay, vai, chân, đầu – tóc).
  - Vạt áo: **vệt bậc thang, răng cưa, khe rách** – do vạt dưới bị xương vai / tay kéo theo.
  - Tay cầm kích: mảnh nhọn quanh ngón.
- `4_SoDo/lubu_surface.json`: từng frame của 7 clip – số cạnh lỗi (2.069–3.748 / frame), cạnh tệ nhất (×35 Idle … ×77 Run), vùng lỗi nhiều nhất.

## Điểm (ước lượng theo `docs/Tuong/LuBu.md`)
Có đủ rig 73 xương, 7 clip, sprite 8 hướng và đã vào trận (~+50 %); bề mặt hỏng ở mọi clip, audit cũ không đo bề mặt nên báo đạt nhầm. Chưa chấm VFX Lữ Bố.
