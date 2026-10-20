# Trạng thái agent

Mỗi agent chỉ sửa phần của mình. Quy tắc: `AgentComms/README.md`.

## claude
- Trạng thái: nhận việc làm (đổi vai 17:29: Claude làm, Codex review); chờ Codex bàn giao file Mặc Ảnh, trong lúc đó làm Lữ Bố
- Việc đang làm: Lữ Bố (codex/inbox/20261020-0710_lubu-sua-sau-macanh.md, nay của Claude); sau đó Mặc Ảnh (0614, 0700 + phản hồi tu-claude_*)
- Lữ Bố 18:45: sửa da v7 (SkinFix3.blend) + dời chỗ cầm kích lên 0,10 m có trượt cán khi đâm (Grip.blend, đang chạy) → xuất lại sprite, trang atlas 2048 (đã đổi ExportSmoothSprites.py + LuBuContentBuilder.cs). Ảnh trước–sau: claude/NghiemThu/LuBu/XemTruoc_*.
- Đang giữ (file / folder): docs/art/concepts/LuBu_Arena3Q/; Assets/Arena3Q/Art/Heroes/LuBu/; Logs/ClaudeReview/
- 18:39 nhận Mặc Ảnh theo lệnh người dùng ("Qua xử lí mặc ảnh đi"; Codex không ghi file Mặc Ảnh từ 17:00, chưa đọc 1728/1729 – xem codex/inbox/tu-claude_20261020-1839_claude-nhan-macanh.md). Giữ thêm: docs/art/concepts/MuuSi_main-hero/MacAnh/; docs/art/concepts/MuuSi_main-hero/muu-si.blend; Assets/Arena3Q/Art/Heroes/MacAnh/; Assets/Arena3Q/Scripts/Graphics3D/; Assets/Arena3Q/Scripts/Editor/MacAnh*.cs; Assets/Arena3Q/Tests/Graphics3D/; Assets/Arena3Q/Scripts/Data/MacAnh*.cs; Assets/Arena3Q/Scripts/Gameplay/Visuals/MacAnh*.cs + IMacAnhRealtimeView.cs; Assets/Arena3Q/Tests/PlayMode/MacAnhCombatTests.cs; Assets/Arena3Q/Scenes/MacAnhGraphicsPreview.unity; Content/Lords/Lord_Strategist.asset, Lord_MacAnhPreview.asset (sprite Mặc Ảnh).

## codex
- Trạng thái: review – đang chờ đến 20:25 ngày 20/10, theo dõi codex/inbox và claude/outbox; Claude đang làm theo lệnh đổi vai 1729.
- Việc đang làm: 20261020-1729_DOI-VAI-codex-review.md; người dùng yêu cầu đợi và báo tiến độ theo rule.
- Đang giữ (file / folder): không giữ model, code, asset hoặc Unity. Chỉ cập nhật báo cáo codex/outbox, phần codex của STATUS và phản hồi vào claude/inbox.
- Bàn giao: codex/outbox/20261020-1728_DUNG-ban-giao-cho-claude.md.report.md (bản tốt nhất, SHA, việc dở, lệnh dựng lại). Không còn tiến trình Unity/Blender của Codex. Không làm tiếp0710; cả LữBố và MặcẢnh do Claude sửa.
- Theo dõi 19:27: Claude đã nhập sprite Lữ Bố và flipbook VFX Mặc Ảnh. MacAnhCombatTests chạy lại lúc 19:22:57 đạt 13/13, hết 4 exception của lượt trước; có ảnh so sánh VFX Attack01. Chưa có báo xong mới từ Claude, chưa nghiệm thu. Điểm nghiệm thu cũ Mặc Ảnh 47%, Lữ Bố ~50%; chưa chấm lại bản đang sửa.
- Bằng chứng bàn giao: native surface985mẫu/LOD và native fan contact985mẫu/LOD đều sạch, nhưng underlayer/cap tay áo còn lỗi hình. Sprite3960bản FanClearance ở staging chưa cài; Production/Final là candidate cũ. Không dùng các kết quả này nghiệm thu thay bản sửa tiếp của Claude.
- Thiết bị: hoãn Android theo yêu cầu người dùng; không có FPS/p95/PSS mới.

## Unity (Editor / batch mode)
- Đang giữ bởi: claude (19:52 – Editor mở cho người dùng xem Lữ Bố / Mặc Ảnh trong Battle; batch xong)
