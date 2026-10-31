# Chi tiết Chương 1 P4b

Dùng lại sprite đã duyệt từ `../Assets/Export/Sprites`; không tạo hoặc vẽ lại ảnh.

1. `node ArtSource/Chapter01/2D/P4/pack-details.cjs` tạo atlas1024 của16 hình, cell256/content224/gutter16, kèm hash từng nguồn và UV.
2. `node ArtSource/Chapter01/2D/P4/place-details.cjs` tạo vị trí từ JSON layout hiện có. Seed731 cố định, dùng đường/sân/ruộng/suối và tâm khu. Lá đơn18–30cm, cành trúc70–105cm; bỏ điểm dưới lòng suối.
3. Unity `Arena3Q.Exploration.Editor.Chapter01DetailsBuilder.Configure` nhập atlas và ba ảnh nền R2 vào cache riêng `_Presentation`, chiếu mesh xuống collider Terrain, bỏ vị trí trong BoxCollider công trình, ghép theo7khu, lưu data/material/mesh riêng. Không lưu scene hoặc sửa cache `_Map` nguồn.

Ground offset2,5cm, lưới3×3 cho decal nằm đất; cây/lúa/sậy dùng quad đứng theo camera cố định. UV nhập qua DTO field rõ ràng, kiểm miền trước khi bake. Ground source giữ nguyên; runtime khôi phục khi hủy. Preset30/60/120 giữ các chi tiết tĩnh này; sương điều khiển độ hiện theo khu.

Atlas và nguồn PNG giữ local; script, bảng UV/vị trí và material/mesh/config nhỏ được quản lý cùng code. Metadata cache cần đi cùng để bảo toàn tham chiếu. ASTC6×6 cho mobile, DXT1 nền/DXT5 atlas cho web; mipmap/aniso4. Chưa đo chi phí thiết bị.

Ảnh08 ở đây là dải bờ suối trong bộ đã duyệt, chưa phải flipbook bọt sóng động. Âm thanh mới có điểm gắn/cue/pool, clip để trống có chủ ý tới khi có nguồn phù hợp.
