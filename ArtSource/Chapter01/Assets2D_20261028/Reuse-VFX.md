# Phần hình dùng lại cho hiệu ứng

Đã đọc giấy phép local `Assets/ThirdParty/Kenney/ParticlePack/License.txt`: Kenney Particle Pack 1.1, CC0. Có thể dùng trong dự án thương mại. Không cần mua hoặc tải pack khác cho bộ xem trước này. Không lấy nguyên ảnh từ các nguồn thương mại trong XLSX đưa vào imagegen.

Nguồn PNG: `Assets/ThirdParty/Kenney/ParticlePack/PNG (Transparent)/`. Các hình dưới đây được copy nguyên bản vào `Reuse/` để xem riêng cùng bộ vẽ; không chỉnh màu, không tạo prefab hay thay hiệu ứng trong game.

| Mục workbook | Phần dùng lại | Hình nguồn | Trạng thái |
|---|---|---|---|
| 20, 37, 50, 51 | Lửa cho nhà, đài lửa, lửa trại, đuốc | fire_01.png, flame_01.png | Có sprite; chưa có chuyển động hoặc ghép lên model |
| 20, 37, 50; bổ sung khói/tro | Khói và tro | smoke_01.png, smoke_07.png, dirt_01.png | Có sprite; chưa tinh chỉnh fade/gió |
| 36 | Hạt năng lượng phụ của ba trụ | magic_01.png, spark_01.png | Có sprite tham khảo; chưa chọn/tinh chỉnh VFX runtime |
| Bổ sung mây khóa | Mảng sương/mây từ sprite khói | smoke_08.png | Có nguồn, chưa có mask vùng khóa hoặc reveal |
| Bổ sung bóng blob | Mặt nạ tròn mềm | circle_05.png | Có nguồn để tint tối; chưa xác nhận hình bóng ghép dưới nhân vật |
| Bổ sung thác xa | Hạt/mảng trắng từ nguồn khói/nước | smoke_01.png + texture 05_water.png | Chỉ có thành phần; chưa phải flipbook thác đã hoàn thiện |

Mây/bụi/lửa khi triển khai phải hiện và tan dần theo rule, không bật/tắt đột ngột. Không coi các sprite tĩnh này là flipbook animation. Giảm mật độ khói trang trí ở 30 FPS nhưng không giảm vùng cảnh báo, dấu nhiệm vụ hoặc dấu năng lượng cần cho gameplay. Việc chọn hình này không thay quy trình người dùng xem hiệu ứng trước khi tích hợp.

Không suy diễn giá/giấy phép hiện hành từ workbook: ở lượt này không tải hay xác nhận các trang nguồn ngoài; giấy phép dùng thực tế là file CC0 đã có trong project, copy kèm `Reuse/Kenney_CC0_LICENSE.txt`.
