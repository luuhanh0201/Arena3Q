# Trạng thái agent

Mỗi agent chỉ sửa phần của mình. Quy tắc: `AgentComms/README.md`.

## claude
- **06:19 21/10 – ĐỔI VAI (người dùng): Codex làm tiếp chuyển 3D, Claude chỉ nghiệm thu.** Bàn giao đầy đủ: codex/inbox/tu-claude_20261021-0619_DOI-VAI-codex-lam-claude-nghiem-thu.md (đã xong P1/P1b/P2a/P3a/A3 + 4 kỹ năng Lữ Bố; đang dở Lữ Bố 3D – chưa qua cổng QA da).
- **06:40 21/10 – chia việc song song (người dùng):** Codex làm Mặc Ảnh + Lữ Bố; Claude làm phần chuyển 3D không thuộc hai tướng và vẫn nghiệm thu Codex (codex/inbox/tu-claude_20261021-0640_chia-viec-song-song.md).
- Trạng thái: đang làm – mặt đất mesh 3D, công cụ đo hiệu năng, lưu trữ file 2D không thuộc tướng, tài liệu chung 3D/map. Một agent Claude theo dõi + review Codex.
- Đang giữ (file / folder): Scripts/Gameplay/Visuals/Ground3DView.cs (mới); Content/Materials/Ground3D*.mat (mới); BattleController.cs chỉ đoạn map visuals; trường mặt đất trong BattleViewConfig.cs + Content/Resources/BattleView.asset; Scripts/Editor/BattleRenderStats*.cs + test mới; docs/GameplaySpecification.md §1/§9, docs/BanDo/MapSpecification.md §8, docs/README.md, CLAUDE.md (Rendering / Logic vs view).
- Bàn giao trước đó: claude/outbox/20261020-2330_BAN-GIAO-cong-viec-claude.md.

## codex
- **06:22 ngày 21/10 – nhận bàn giao 0619: Codex sửa, Claude nghiệm thu.** Theo lệnh mới của người dùng: kiểm và xử lý Mặc Ảnh trước, sau đó tiếp tục Lữ Bố 3D.
- Đã đọc bàn giao 0619, kế hoạch ChuyenSang3D và các báo cáo A3/P1/P3a. Không có Unity/Blender chạy lúc tiếp nhận. Mốc 0104 chỉ review đã hết hiệu lực.
- Đang giữ: nguồn/asset/script Mặc Ảnh và Lữ Bố, Scripts/Graphics3D, HeroModel3DView, HeroAnimationView, builder/test liên quan; Tests/PlayMode/CodexMacAnhDirectTests.cs; Model3D_Rules, LuBu.md, Delivery_QA/MacAnh và phần tướng trong ChuyenSang3D; báo cáo/bằng chứng riêng. Đã nhận chia việc0640: nhả docs chung và phần map cho Claude. Không sửa phần STATUS của Claude.
- Việc đầu: kiểm snapshot 3D về scale, toàn bộ submesh và quạt; kiểm footprint VFX sau đổi camera; đồng bộ docs 3D. Sau đó kiểm model/motion Mặc Ảnh và giải quyết Lữ Bố chưa qua QA da.
- Báo ngay từng phần nhỏ với ảnh trước–sau và số đo. Chưa nghiệm thu tổng thể. Chỉ test Unity, chưa build/test Android theo bàn giao 0619. Sau khi hoàn tất công việc theo dõi inbox 1 giờ.
## Unity (Editor / batch mode)
- Đang giữ bởi: không ai (Claude xong build Android + đo hiệu năng trên Redmi)
