# Sửa hình 2D theo review Claude, 28/10/2026

Trang xem [trước/sau](../../../../../NghiemThu/Chapter01/XemTruoc_Sua2D_20261028/review.html) được mở cho người dùng và gửi Claude ngay trong lúc sửa. Mỗi tấm so cùng tỉ lệ trên đất, đường, cỏ; bờ suối có thêm nền nước. Đây là kiểm hình nguồn, chưa phải nghiệm thu cảnh Unity.

## Nguồn và thay đổi

29 PNG gốc được giữ trong `Before/`. `Generated/` giữ bản sửa từ built-in imagegen. Ảnh dùng hiện tại nằm ngay thư mục Assets bên trên. PNG và atlas giữ local, không Git/LFS; prompt, công cụ và số đo được commit. Không sửa workbook hoặc code game.

- Bờ suối chuyển sang mặt phẳng nhìn từ trên xuống, mép mềm; vết bánh xe/vũng nước không còn là ụ đất có thành đứng.
- Vũ khí đổi thành đao thẳng chuôi vòng và khiên sơn kiểu Hán; giảm đỏ khiên.
- Lá trúc, lúa, gỗ, rào, cũi, đuốc giảm chi tiết tả thực. Lúa cuối có nhánh mảnh cong rủ, hạt thành nét màu phẳng; bản thử bông đặc bị loại.
- Vải lều cân màu #C7A130; lều tướng dùng vân mây. Vật nghi lễ đổi từ giấy sang cuộn thẻ tre hẹp.
- Ngói vỡ nhìn vuông góc mặt mái; vết nứt trắng chỉ dùng cho tường trát, không dán xuống đất. Kích thước ngói phải khớp texture mái thật khi ghép.
- Núi giữ màu gốc ở vùng alpha mềm, bỏ lỗi sọc do giãn màu từ thân đặc xuống sương. Vòng nghi lễ đã vuông hóa canvas và kiểm bbox tròn 960×960.

Prompt: [lượt đầu](prompts.json), [dấu đất và ruộng](followup-prompts.json), [góp ý tiếp](reviewer-followup-prompts.json), [lúa cuối](rice-final.prompt.txt), [cờ trơn](flag-final.prompt.txt). Prompt gốc cả bộ ở `../prompts.json`.

## Xuất ảnh và kiểm kỹ thuật

Chạy từ gốc repo:

```powershell
node ArtSource/Chapter01/2D/Assets/Revision_R1/process-assets.cjs
node ArtSource/Chapter01/2D/Assets/Revision_R1/export-atlas.cjs
node ArtSource/Chapter01/2D/Assets/Revision_R1/verify-exports.cjs
```

`process-assets.cjs`: alpha thân >=240 đưa về 255, giữ alpha mép mềm, làm sạch RGB mép và giãn màu thân qua vùng trống. Núi dùng nhánh giữ RGB bán trong suốt hợp lệ để không phá sương. Texture kín cân seam hai trục. `Metrics/` ghi hash, alpha, kích thước, seam và đường dẫn so sánh từng tấm.

58 bản xuất: 29 tấm × 512/1024. 08/12/14/26 giữ 2:1 (512×256, 1024×512). Các sheet khác là ảnh nguồn xem chung, không dùng nguyên tấm làm một sprite. `Export/Sprites/` và `Export/atlas.json` có từng vùng cắt, pivot, rect và atlas để thử nhập. 55 sprite, 6 trang 1024 và 1 trang 512 cho vòng nghi lễ; 256px mỗi ô, không xoay, giãn RGB thêm 16px quanh ô. Chưa chốt texel density/nhóm material cho atlas production.

Kiểm đóng gói đối chiếu alpha và RGB nhìn thấy giữa sprite riêng với rect trong atlas, không chỉ đếm file. Kết quả: `export-check.json`; ảnh tổng: `NghiemThu/Chapter01/XemTruoc_Sua2D_20261028/sprite-contact-sheet.png`. Alpha nguồn đạt 255, kích thước xuất là lũy thừa 2; seam texture kín được đo cả hai trục. Chưa kiểm ASTC/DXT/mipmap trong engine.

## Ghi chú ghép cảnh

- Foliage/đạo cụ cần Alpha Is Transparency khi nhập Unity. Không dùng alpha clipping cứng cho bờ nước, sương, vết đất hoặc decal tan mép.
- Khúc gỗ không chứa blob shadow tiếp xúc; thêm blob riêng theo ánh sáng cảnh, tránh hai bóng. Rào/cây khô chỉ trang trí xa, không thay collision 3D.
- `28_ground-marks-2-1` là chân trái, `-2-2` chân phải: dùng luân phiên, đặt so le ±0,35 bề rộng bàn chân quanh trục đi; khoảng tiến 1,8 chiều dài bàn chân; góc chụm/xòe luân phiên ±5°. Đây là mặc định trình bày để thử, cần đưa vào data khi code. Không dùng hai dấu nằm ngang trên sheet như người đang bước.
- Dấu chân là trang trí đã đi qua, không là chỉ dẫn bắt buộc. Đề xuất 30/60/120: tối đa 6/12/12 dấu nhìn thấy mỗi đường dấu, chỉ giảm tuổi lưu vết ở mức thấp; fade-out 0,5 giây. Không thay tốc độ/đường đi gameplay.
- Lá/cánh hoa `27`: đề xuất tối đa 6/12/12 hạt trang trí nhìn thấy toàn màn hình; fade-in 0,3 giây, fade-out 0,5 giây; vòng đời 4 giây. Không tăng mật độ riêng cho 120; chuyển động theo thời gian, không theo số frame. Đây là thông số đề xuất, chưa có prefab/runtime hoặc số FPS đo.
- Lúa `12`: 30 dùng ít cụm hơn ở vùng trang trí, 60/120 cùng mức chi tiết bông; chọn mip theo kích thước hiển thị thật, không tự giảm nét bông ở 30. Nếu bông chìm vào lá ở camera 40°/45° thì chỉnh biến thể đơn giản hơn sau khi có ảnh cảnh; chưa tạo riêng bản LOD lúa hoặc kiểm răng cưa trên điện thoại.
- Vòng nghi lễ 29 cần emission/tương phản nhận diện như nhau ở cả 30/60/120 khi có ý nghĩa gameplay; chỉ được giảm tia/hạt trang trí xung quanh. PNG chưa chứng minh được điều này trong trận.

## Review và giới hạn

Claude đã xem trước/sau trong cùng workspace và phản hồi các vòng 1340–1410: lỗi chính của bờ suối/vũ khí/vết đất đã đạt; núi hết sọc ~85%; ngói đã hết góc 3/4 sai. Bông lúa và màu vải vòng cuối đều được xác nhận đạt ~82%. Hết các mục lệch nhiều và mục chính vòng 1–3; atlas đã gửi kiểm lượt cuối. Các tỉ lệ này là đánh giá hình của Claude, không phải kết quả trong game.

Còn cần kiểm khi ghép model thật: màu núi và rêu, mép bờ nước trên material nước, viền mảng ngói, độ rộng luống, kích thước/vị trí sprite, ánh sáng, che khuất, compression/mipmap và 30/60/120 FPS. Chưa có Art Bible/render 3D chuẩn; không coi bộ này đã nghiệm thu tích hợp. Impostor cây lá rộng vẫn chờ render model 3D đúng workbook.
