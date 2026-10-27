Ghi chú 27/10: APK thử cũ đã nghỉ khi sảnh được tích hợp Frontend. Giữ báo cáo này làm lịch sử; không dùng APK này để nghiệm thu vòng sửa mới. Xem IntegratedLobby_20261027.md.

# APK thử sảnh2D và game Android

Yêu cầu27/10: người dùng chọn APK mở **sảnh2D mới và nút vào game**. Builder riêng `Arena3Q.Lobby2D.Editor.LobbyAndroidBuild.Build` xuất `Builds/Android/Arena3Q-Lobby2D-20261027.apk`, giữ các APK cũ.

Mở app vào `LobbyAndroidTest`: Lữ Bố chạy rig liên tục, có nút Đứng/Chạy/Tấn công và FPS trung bình của cửa sổ ngắn. Năm nút thời điểm đổi sky/tint/light; UI có hiệu ứng độc lập. Xuất Chiến mở Frontend hiện tại, sau đăng nhập/chọn máy chủ tiếp tục campaign/chiến trường mới nhất. Những menu chưa có logic báo chưa mở trong bản thử. Muốn xem lại sảnh thử sau khi vào game, đóng và mở lại app. Không đổi profile, thông số tướng, rule tutorial hoặc phần thưởng.

APK dùng cùng package/signing hiện tại để cài cập nhật bằng `adb install -r`; không gỡ app hoặc xóa dữ liệu. Android ARM64, minimumSDK theo project. Cần kiểm chữ ký cùng bản đã cài trước khi cài đè. FPS hiện trên màn hình sảnh chỉ mô tả workload sảnh trong thời gian quan sát, không thay phép đo trung bình/p95 của chiến trường đầy đủ.

Đầu vào: `LobbyArtCatalog`, `LuBuCutoutRig`, prefab/scene đã bàn giao và `CurrentGameRelease.ScenePaths`. BuildReport phải chứng minh có rig mới cùng Frontend và hai stage campaign đúng; không chỉ thêm scene vào danh sách rồi kết luận đã nối luồng. Tùy chọn appBundle được khôi phục sau build. Không sửa manifest phát hành chung.

Kết quả build/chữ ký/thiết bị ghi trong `NghiemThu/UI/Lobby2D_Android_20261027/README.md` và các JSON cùng thư mục. APK và ảnh/log lớn chỉ ở máy theo quy tắc repo.
