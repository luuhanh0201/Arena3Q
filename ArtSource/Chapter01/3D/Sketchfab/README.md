# Chuẩn hóa Sketchfab Chương 1

Nguồn người dùng tải nằm trong `../Sources/Sketchfab/SFxx` (local, không commit). Cấu hình `sf_normalize.json` là đề xuất tỉ lệ để xem/duyệt, chưa tự gắn vào map. Chạy Blender4.5.3 đi kèm dự án:

```powershell
& Library/LuBuTools/blender-4.5.3-windows-x64/blender.exe -b --factory-startup --python-exit-code 1 --python ArtSource/Chapter01/3D/Sketchfab/sf_normalize.py
& Library/LuBuTools/blender-4.5.3-windows-x64/blender.exe -b --factory-startup --python-exit-code 1 --python ArtSource/Chapter01/3D/Sketchfab/sf_review.py
& Library/LuBuTools/blender-4.5.3-windows-x64/blender.exe -b --factory-startup --python-exit-code 1 --python ArtSource/Chapter01/3D/Sketchfab/sf_preview.py -- ArtSource/Chapter01/3D/Sources/Sketchfab ArtSource/Chapter01/3D/Renders/Sketchfab/Source
```

Hai script đầu nhận `-- SF04,SF08` nếu chỉ chạy lại vài nguồn. Kết quả gồm47mảnh FBX (22nguồn), texture sao chép/thu nhỏ tối đa1024, `manifest.json`, `pieces.tsv`, license trong `Assets/ThirdParty/Chapter01/Sketchfab`. Tất cả file nguồn/binary chỉ ở máy. QA blend chứa Before/After, ảnh tại `../Renders/Sketchfab`.

- Mỗi nguồn dùng một hệ số đổi đơn vị; mảnh cùng bộ giữ tỉ lệ tương đối. Gốc giữa đáy, đèn treo SF23/SF24 giữa đỉnh.
- SF15 dùng parent node glTF gom thân/cành/lá thay vì AABB tán chạm nhau; xuất9cây cao,4cụm,3thân. Các biến thể ngoài giới hạn ghi trong `omittedObjects`, không xóa nguồn.
- SF04 bỏ đúng10mặt nền cỏ ở vùng đáy và mép footprint; ảnh Before còn nền để đối chiếu.
- SF08/SF21 giảm mặt theo cấu hình; xóa mặt suy biến bằng phép đo float64, không gộp UV hoặc đổi normal nguồn.
- Map metallic/roughness của glTF chuyển sang metallic R / smoothness A cho Unity; normal là non-color; albedo/emission sRGB. Các vật liệu transmission/alpha blend được đơn giản hóa thành opaque/cutout và cần duyệt hình.
- Nguồn không có rig/animation. Bản này chưa tạo LOD cho SF13, chưa chứng minh ngân sách FPS/mobile. Các nhà SF13 còn10–29k tam giác mỗi mảnh.
- SF13 Building01/02/03 có cờ `REFERENCE_ONLY_NEEDS_LOD` trong `pieces.tsv`: chỉ tham khảo; phải giảm mặt hoặc có LOD và kiểm lại trước khi dùng trên map.
- SF06 mái xanh đậm từ nguồn, không phải lỗi import. Nếu chọn làm mái Đông Hán: chuyển xám trước rồi nhuộm theo bảng màu, không nhân tint trực tiếp lên màu xanh gốc.
- `metallic_override` chỉnh riêng SF05 `lambert1*` và SF24 `Material.001` thành phi kim (tường/gỗ/giấy); SF21 đồng giữ nguyên. Gộp map phải nhân metallicFactor/roughnessFactor từ glTF; `packedAudit` đọc lạiPNG kiểm sai số lượng tử≤1,1/255.

Trong Unity: **Arena3Q > Chapter01 > Import and Render Sketchfab**, hoặc batch `Arena3Q.Editor.Chapter01.Chapter01ColorReference.RenderSketchfabChecks`. Importer tạo URP material/prefab local, xác minh kích thước, pivot, UV, số tam giác và đỉnh hữu hạn sau FBX. Nó chụp ứng viên cạnh mẫu nhóm trong game bằng cùng cỡ khung. Không sửa scene map hoặc dữ liệu gameplay.

Batch `Arena3Q.Editor.Chapter01.Chapter01SketchfabDetailReview.BuildAndRender` chạy cùng kiểm tra trên, rồi chụp riêng 16 mảnh cao dưới 3 m cạnh thùng gỗ của game ở đúng tỉ lệ mét. Ảnh và bảng đo ở `../Renders/Sketchfab/UnityDetails`.

Sau khi có đầy đủ ảnh Blender/Unity, chạy `node ArtSource/Chapter01/3D/Sketchfab/sf_delivery.cjs`: kiểm22hash glTF, kích thước texture trên đĩa, mesh hữu hạn/không mặt suy biến, đủ ảnh; tạo gallery `NghiemThu/Chapter01/Sketchfab_20261029` và ghi công từng nguồn tại `../Catalog/SKETCHFAB_CREDITS.md`.

**Lệch mục đích:** SF20 là đảo ruộng bậc thang, chưa thay được ruộng phẳng Ải2; SF26 là hành lang có mái, chưa thay cầu qua suối. Tất cả vẫn là ứng viên, chưa được người dùng duyệt để thay asset trên map.
