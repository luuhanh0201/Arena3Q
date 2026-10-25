# Duyệt asset chiến trường trước khi dựng map

Ngày25/10/2026. Trạng thái **PENDING_APPROVAL**, `mapCreated:false` theo yêu cầu trực tiếp của người dùng. Web: http://127.0.0.1:4311. Có185tài nguyên,80đề xuất;9modelArena3Q+68nature+94props=171model,6mẫuđất+8textureVFX=14texture.

Đã kiểm thực:

- Render và chụp thumbnail đủ171model trong browser,0pageerror; tài nguyên glTF có đủ buffer/texture phụ thuộc.
- Mở thêm cả9GLB nguồn Arena3Q, không chỉ bản nhẹ; kiểm hashFBXnguồn vẫn đúng, xoay chính diện/3/4, bật/tắtwireframe. Ghi số tam giác/dung lượng từng nguồn trong `verification.json`.
- Đủ185thumbnail HTTP200; 14texture mở được;6mẫuđất chuyển một ô/lặp3×3 hoạt động. Search/filter80/185, nhóm cây và viewport390×844 không tràn ngang. POST405, đường ngoài mount404.
- Xem trực quan ảnh tổng quan, cây, đá, giá vũ khí, xe, tế đàn và lều nguồn. Đây là duyệt asset trên web, chưa nghiệm thu shader URP, gió, LOD, navigation hoặc chất lượng map.

`web-overview.png`, `arena_*-source-preview.png`, `nature_*.png`, `props_*.png`, `ground_*-tiled.png`, `mobile-review.png` là ảnh bằng chứng. `manifest.json` ghi số thứ tự/ID/nguồn/đề xuất; `sources.json` ghi giấy phép và SHAarchive. `models/` có18GLB dẫn xuất (source/budget) từ9model cũ; `textures/` giữ6PNG imagegen nguyên bản. Prompt trong docs/BanDo/Tools/asset-review/ground-texture-prompts.json. Đã mở ảnh để người dùng xem.

Giới hạn còn rõ: thiếu tre/đào/phong đúngloài, cổng/tườngđổ phươngĐông, cliffkit riêng, đènđá; chưa cấu hìnhVFX. Bản Standard chỉ chứa model/texture, không có shadergió/prefabURP trảphí. Không gọi asset dự phòng hay texture nguồn là tính năng hoàn tất.

Chưa sửa/tạo scene/map/gameplay/prefabgốc; chưa chạy Unity build/test/đo FPS thiết bị trong bước duyệt. Không bắt đầu dựng map trước khi được duyệt. Nguồn asset lớn giữ local, không Git; chỉREADME/công cụ/tài liệu vàoGit. Hướng dẫn và giấy phép: docs/BanDo/BattlefieldAssetReview_20261025.md.
