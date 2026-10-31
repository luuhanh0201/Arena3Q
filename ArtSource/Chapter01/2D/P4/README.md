# Chi tiết Chương 1 P4b

Dùng lại16sprite đã duyệt từ `../Assets/Export/Sprites`. Bổ sung31/10 theo review P4b: `Generated/{wildflowers,fallen-cloth,village-litter}.png` tạo bằng built-in imagegen, prompt đầy đủ trong `supplement-prompts.json`. Không gọi vết cháy/cỏ cũ là rác/vải/hoa.

1. `node ArtSource/Chapter01/2D/P4/pack-details.cjs` tạo atlas1024 của19 hình,5cột/cell204/content176/gutter14, kèm hash từng nguồn và UV. Bản5b1d605 dùng16hình/content224; giảm cỡ ô để giữ một atlas1024 và7renderer, phải đối chiếu cả hình cũ sau đóng gói.
2. `node ArtSource/Chapter01/2D/P4/place-details.cjs` tạo vị trí từ JSON layout hiện có. Seed731 cố định, dùng đường/sân/ruộng/suối và tâm khu. Lá đơn18–30cm, cành trúc70–105cm; bỏ điểm dưới lòng suối.
3. Unity `Arena3Q.Exploration.Editor.Chapter01DetailsBuilder.Configure` nhập atlas và ba ảnh nền R2 vào cache riêng `_Presentation`, chiếu mesh xuống collider Terrain, bỏ vị trí trong BoxCollider công trình, ghép theo7khu, lưu data/material/mesh riêng. Không lưu scene hoặc sửa cache `_Map` nguồn.

Ground offset2,5cm, lưới3×3 cho decal nằm đất; cây/lúa/sậy dùng quad đứng theo camera cố định. UV nhập qua DTO field rõ ràng, kiểm miền trước khi bake. Ground source giữ nguyên; runtime khôi phục khi hủy. Preset30/60/120 giữ các chi tiết tĩnh này; sương điều khiển độ hiện theo khu.

Phần bổ sung chỉ nối cuối danh sách;270vị trí cũ giữ tọa độ/cỡ/seed.16vị trí mới ở góc sân,15qua bước loại công trình:8hoa cao34–40cm,3vải38×30cm,4rác48×38cm (khung quad gồm padding). Kết quả268chi tiết/1.862đỉnh,7mesh. `Export/supplement-placements.json` dùng chụp cùng camera trước/sau; builder lưu placedIds để không dùng vị trí bị loại làm bằng chứng.

`node ArtSource/Chapter01/2D/P4/audit-supplements.cjs` kiểm alpha/viền màu và ghép nền sáng/tối. Nguồn hoa/vải có3/63pixel rìa ởalpha1/255, không có viền đỏ; giữ số thô trong source-alpha.json. Gutter của atlas tách các ô; ảnh thật trong Unity/WebGL mới là bằng chứng tích hợp.

Atlas và nguồn PNG giữ local; script, bảng UV/vị trí và material/mesh/config nhỏ được quản lý cùng code. Metadata cache cần đi cùng để bảo toàn tham chiếu. ASTC6×6 cho mobile, DXT1 nền/DXT5 atlas cho web; mipmap/aniso4. Chưa đo chi phí thiết bị.

Ảnh08 ở đây là dải bờ suối trong bộ đã duyệt, chưa phải flipbook bọt sóng động. Âm thanh mới có điểm gắn/cue/pool, clip để trống có chủ ý tới khi có nguồn phù hợp.
