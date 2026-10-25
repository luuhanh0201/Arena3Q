# Bộ asset chiến trường trước khi dựng map

**Cập nhật:** người dùng đã ra lệnh “dựng map đi” sau khi xem bộ asset. Bộ này được phép dùng để triển khai scene riêng `Battlefield_HighQuality`; nội dung bên dưới ghi lại phạm vi tại thời điểm gửi duyệt. Tiến độ và nghiệm thu map ghi trong `BattlefieldHighQuality_20261025.md`.

Người dùng cho phép bổ sung asset miễn phí dùng thương mại, sau đó yêu cầu **xem asset trước khi bắt đầu tạo map**. Trạng thái hiện tại: **PENDING_APPROVAL**. Chưa dựng scene, địa hình, bố cục hay prefab môi trường mới. Không thay đổi Battle.unity, camera, navigation, gameplay hoặc model Arena3Q gốc.

Trang duyệt local: **http://127.0.0.1:4311**. Bấm từng món để xoay/zoom 3D. Danh sách mặc định có **80 đề xuất**; chọn “Toàn bộ thư viện” xem **185 tài nguyên**. Với 9 model Arena3Q, chọn “Nguồn gốc” hoặc “Bản nhẹ hiện có”. Những bản nguồn 54–126MiB chỉ dùng để kiểm hình, chưa phải ngân sách mobile được duyệt. Ánh sáng/material trình duyệt dùng cho duyệt hình; chưa phải cấu hình shader URP cuối cùng.

| Nhóm | Số lượng | Phạm vi hiện tại |
|---|---:|---|
| Arena3Q hiện có | 9 model | Tế đàn, trụ, phiến cửa, cờ, 2 đài trống chỉ huy, lều, tháp canh, hàng rào. Có 18 GLB dẫn xuất nguồn/nhẹ, không sửa FBX. |
| Quaternius Nature Standard | 68 model | Cây tán tròn, thông, cây xoắn, cây khô, đá, cỏ, bụi, hoa, sỏi và lát đường. |
| Quaternius Props Standard | 94 model | 27 món được đề xuất cho trại: thùng, bao, giá vũ khí, xe, đèn, đuốc… Các món còn lại chỉ là dự phòng. |
| Mẫu mặt đất mới | 6 PNG | Đất cát, đường nén, sỏi, đá lát, cỏ khô, cỏ xanh; mới là albedo chờ duyệt, chưa gắn terrain/normal/roughness. |
| Kenney Particle Pack | 8 texture được chọn | Nguồn lửa, khói và bụi; chưa cấu hình thành particle môi trường. |

Tổng **171 model** xem được, **14 texture**. Tên hiển thị dựa trên model thật: `ThreeKingdomsSquare` là **lều** và `ThreeKingdomsCommand_*` là **đài trống**, không coi một model là toàn bộ doanh trại.

## Những gì còn thiếu hoặc cần quyết định

- Tre, đào nở hoa và phong lá đỏ đúng loài chưa có. Có cây tán tròn/xoắn làm lựa chọn thay thế, nhưng không tự nhận chúng là đúng loài.
- Chưa có bộ cổng trại/tường đổ phương Đông riêng. Có thể ghép prefab mới từ hàng rào, tháp và đá đã có sau khi được duyệt. Không sửa mesh gốc.
- Đá Medium dùng được để bố trí viền/vách thấp; chưa có cliff kit riêng hoặc đèn đá cổ đúng mẫu.
- Shader gió, prefab URP, va chạm và LOD của các gói trả phí **không có** trong bản Standard miễn phí. Chưa triển khai các tính năng này. Cây/đá/đạo cụ còn cần đồng bộ vật liệu sau duyệt.
- Không lấy texture VFX làm bằng chứng đã có lửa/khói chuyển động. Tương tự, texture lặp chỉ là mẫu màu/chất liệu; đường nối và độ lặp cần duyệt hình trước khi dùng.

Danh mục đầy đủ và số thứ tự nằm trong `NghiemThu/Battlefield/XemTruoc_Assets_20261025/manifest.json`. Phần “Phạm vi và hạng mục cần chốt” trên trang luôn hiển thị giới hạn, không ẩn dưới trạng thái hoàn thành.

## Nguồn và giấy phép

Đã kiểm trang tác giả và giấy phép **đi kèm file tải thực tế**, ngày25/10/2026:

| Gói | Nguồn tác giả | Giấy phép | SHA256 archive |
|---|---|---|---|
| Stylized Nature MegaKit Standard | [Quaternius](https://quaternius.com/packs/stylizednaturemegakit.html), [download miễn phí](https://quaternius.itch.io/stylized-nature-megakit) | CC0-1.0, `License_Standard.txt`; bản miễn phí 68/116 model | `298f6732b872e4cf7b30e6e7abf9641c7f6dc6b326df37ac089533ed7e3d58c9` |
| Fantasy Props MegaKit Standard | [Quaternius](https://quaternius.com/packs/fantasypropsmegakit), [download miễn phí](https://quaternius.itch.io/fantasy-props-megakit) | CC0-1.0, `License_Standard.txt`; tải Standard, không lấy Pro/Source trả phí | `8b6f7e806d222e585478f0e1bdc6b271bbc7bc6f84dd6af8ca703a7c64f0cb1e` |
| Particle Pack | [Kenney](https://kenney.nl/assets/particle-pack) | CC0, `License.txt`, cho phép dùng thương mại | `b631d4b07f7002549fdcf155f01141ad482f79f3440e4e301eed49ce5f1d8958` |

Không dùng shader/Unity project của bản trả phí. Asset người dùng được giữ ở nguồn cũ, không gán CC0 cho tài nguyên Arena3Q. Hovl hiện có chưa đưa vào bộ môi trường mới vì những prefab tìm thấy là hiệu ứng kỹ năng, không thay thế bằng chứng giấy phép cho các gói tải lần này.

6 mẫu đất được tạo bằng **imagegen built-in**, một lần gọi cho từng texture, không dùng CLI/API key. Prompt đầy đủ ở `Tools/asset-review/ground-texture-prompts.json`. PNG gốc được chép nguyên vẹn vào `NghiemThu/Battlefield/XemTruoc_Assets_20261025/textures/`; chưa nhập vào terrain.

## Chạy lại trang duyệt

Tại thư mục gốc, cần dependencies trong `Admin/node_modules` và đủ asset local:

```powershell
node docs/BanDo/Tools/asset-review/prepare.cjs
node docs/BanDo/Tools/asset-review/server.cjs
# Chụp thumbnail bằng model thật + bằng chứng trình duyệt:
node docs/BanDo/Tools/asset-review/capture.cjs
# Kiểm source model, ảnh, tìm/lọc, mobile và server chỉ đọc:
node docs/BanDo/Tools/asset-review/verify.cjs
```

`prepare` giữ trạng thái PENDING_APPROVAL, từ chối ghi đè nếu bộ đã được duyệt. Server chỉ GET/HEAD trên loopback4311, chặn ghi/path traversal và symlink ra ngoài mount; không có endpoint duyệt giả hoặc tạo map. Ba nhóm tài nguyên tải nằm trong `Assets/ThirdParty/{Quaternius,Kenney}`; bị Git bỏ qua theo quy tắc project. Archive ở `Library/BattlefieldDownloads`.

9 model gốc đã có từ trước. Để xuất lại derivative cho web, chạy Blender4.5.3 với `--python docs/BanDo/Tools/export_owned_asset_review.py`. Plan đường dẫn/kích thước `Tools/asset-review/owned-export-plan.json` lấy từ `PropSprites.asset`; cập nhật plan nếu nguồn được di chuyển. Script không lưu nguồn, kiểm SHA trước/sau; dùng đúng model gốc và bản Props3D có sẵn, không tạo thêm model hay map.

Mã viewer/công cụ và tài liệu được commit; GLB/PNG/archive/source lớn **chỉ giữ local**, không Git LFS. Cần sao lưu các thư mục asset và preview khi chuyển máy. Bằng chứng kiểm ở README của thư mục `NghiemThu/Battlefield/XemTruoc_Assets_20261025`.

## Bước tiếp theo sau duyệt

Chỉ sau khi người dùng duyệt bộ đề xuất cụ thể và cách xử lý phần thiếu mới bắt đầu scene nâng cấp riêng, rồi kiểm từng phase. Chưa chọn layout thay thế, chưa đổi quy ước trục hoặc target FPS của game hiện hành trong bước này.
