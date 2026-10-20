# Mặc Ảnh — đồ họa 3D và độ mượt (2026-10-19)

Trạng thái: đã triển khai, **chưa nghiệm thu độ giống concept và hiệu năng Android**.

## Nguồn và hiển thị

Scene cũ MacAnhPreview dùng sprite pose độ phân giải nhỏ; tăng MSAA/Render Scale không khôi phục được chi tiết. Cyclone cũ là prototype nét xoắn phẳng với shader Universal2D.

Scene mới: `Assets/Arena3Q/Scenes/MacAnhGraphicsPreview.unity`. Prefab: `Assets/Arena3Q/Art/Heroes/MacAnh/Graphics3D/MacAnh_Character3D.prefab`. Xuất từ [muu-si.blend](../../../muu-si.blend), không tạo lại nhân vật, không lưu lại Blender. [SourceExport.json](SourceExport.json) ghi SHA nguồn và texture.

Body nguồn 1.476.550 vertex / 2.953.808 tam giác, 87 bone, chiều cao rest đến vương miện **1,70 m**. Quạt 6 mesh / 7.268 tam giác. Unity có thêm seam split; không đổi topology, UV, rig, tỷ lệ hoặc map gốc. Geometry và 11 action FBX tách riêng.

Material Unity mới dùng albedo, normal, ORM nguồn (G roughness / B metallic), giữ giá trị màu linear nguồn. Android albedo tối đa4096, map khác2048, ASTC6×6, mipmaps/trilinear/aniso4; không sharpen hoặc vẽ lại map. Preview có UniversalRenderer riêng, Render Scale1, MSAA4, HDR/depth, bloom nhẹ, nền/ánh sáng trung tính, camera toàn thân/xoay360°. Game vẫn dùng Renderer2D và sprite nhân vật; chưa chuyển cả trận sang skinned mesh.

## Các hiệu ứng đã làm

Skill02 mới `Graphics3D/FX_MacAnh_Cyclone3D.prefab` có trận pháp mặt đất; thân phễu ray march trong không gian3D với texture turbulence64³; dải gió mesh UV flow; linh lực nhiều cao độ; phù văn vàng; hạt/lông vũ/khói; activation burst và shockwave; thu nhỏ/dissolve/tản hạt. Low/Medium/High có geometry/particle budget riêng, volume8/12/18 sample. Particle Shader Graph dùng trong preview; material Universal2D riêng hỗ trợ trận đấu.

Texture gió organic mới chỉ dùng cho VFX, tạo bằng built-in image_gen. [Prompt/chế độ/đường dẫn](TexturePrompt.txt). Không sửa ảnh concept hoặc texture nhân vật.

Attack01/02, Skill01/03, Ultimate, DodgeAttack có prefab3D riêng cùng preparation/impact/afterimage: lưỡi gió, lông vũ, trận đồ/cột sáng, khiên lục giác, trận pháp trên không, phản kích. Đây là triển khai mới, chưa coi toàn bộ chiêu đã đạt concept.

Animation Events từ action nguồn: Skill02 prepare0,5s/release1s; Attack02 ba nhịp; DodgeAttack ba afterimage. Đã sửa nhầm đơn vị: importer event time chuẩn hóa0–1, AnimationClip event time là giây. Events preview không gây damage gameplay.

8 wrapper mới gắn vào profile/binding game; giữ9 prefab VFX cũ và afterimage theo pose. Không đổi cân bằng, SkillDefinition, hoặc cặp tự dùng Huyền Cơ Trận + Phong Vũ Khốn Trận.

## Sửa đoạn cuối bị khựng

- Pha cuối lốc1giây, scale/opacity easing liên tục, giảm tốc quay và UV về0.
- Hạt tan biến phát dần0,6giây, lifetime kết thúc trước khi trả pool; bỏ burst lớn vào cuối.
- Phù văn alpha fade thay ngưỡng noise làm nét bật/tắt.
- Preview blend về Idle0,28giây sau recovery, thay vì giữ cứng frame cuối; không sửa animation nguồn.
- Mesh/shader/particles cùng explicit clock, đồng bộ pause/tốc độ/seek/pool. Seek replay từng bước1/60s.
- Benchmark Android chỉ chạy khi bấm nút hoặc intent `macanh_benchmark=true`; mở icon không tự đổi tier làm gián đoạn xem chiêu.
- Tắt skinned motion vector không cần thiết. Easing không giải quyết được thiếu thời gian GPU; chưa tuyên bố loại bỏ giật do FPS thấp.

## Kiểm chứng

**7/7 EditMode**, job `f438a72350704e909d6aa374607db470`: nguồn mesh/tỷ lệ, geometry hữu hạn, tiers, material/pass, events và nội dung cũ.

**12/12 PlayMode**, job `bcc3b34e81494fc9b2029195d2ecb7c4`: gameplay Mặc Ảnh, pause/seek/pool, afterimage. Test mới kiểm tra fade gần0, không còn hạt ở3,19s trước thu hồi3,2s. Console không error/warning ở lần render sau sửa. Test không chứng nhận nghệ thuật/FPS.

Ảnh Unity thật1920×1080: [mặt trước](Captures/Skill02_Final_Front_1920x1080.png), [bên hông](Captures/Skill02_Final_Side_1920x1080.png), [mặt sau](Captures/Skill02_Final_Back_1920x1080.png), [đang tan2,7s](Captures/Skill02_Final_Tail_1920x1080.png). Ảnh First/Organic/Dense là thử nghiệm cũ.

## Android và giới hạn

Redmi Note8Pro, Android11, MT6785/Mali-G76MC4, RAM5635MB, Unity2340×1080/Vulkan. ADB đã cấp quyền và thực thi shell; một lần host server treo đã khôi phục bằng restart ADB, không đổi cài đặt điện thoại.

APK đầu **141,8 MB** (148.675.685byte), app riêng `com.arena3q.macanhpreview`, không ghi đè game chính. Chưa có các sửa cuối. [Android_Benchmark_Initial.json](Android_Benchmark_Initial.json): Low13,30 / Medium13,02 / High14,92FPS; p95 89,28/94,10/91,09ms, workload1model+1lốc, không có quân. Graphics memory~754MB, PSS~1210MB, có low-memory callback. Lookup Play Asset Delivery optional class không tìm thấy nhưng app vẫn chạy.

APK cập nhật và phép đo8 trường hợp đang thực hiện: 3tier, chỉ body, chỉ VFX, bỏ shadow, bỏ depth, chỉ sân. Kết quả bổ sung bên dưới. [PC Editor initial](PC_Editor_Benchmark_Initial.json) bị can thiệp công cụ, không dùng so sánh tier hoặc suy ra Android.

Chưa tạo LOD/retopology hoặc giảm mesh nguồn. Chưa nghiệm thu30/60FPS, chưa đo trận nhiều quân hoặc nhiều thiết bị. So với concept còn cần đánh giá độ rối tự nhiên của gió, mật độ lông vũ và ngôn ngữ phù văn. Không gọi đây là chất lượng đã đạt hoàn toàn.

### Bổ sung lỗi chỉ xuất hiện trên Android

APK CPU cập nhật:144,4MB (151.428.652byte), build406,49s. Ảnh thiết bị phát hiện lỗi `Missing Copy Depth` lặp từng frame; không dùng phép đo này để chứng nhận hiệu năng. Nguyên nhân: URP build stripper chỉ thấy Renderer2D ở các quality level Android, dù runtime chuyển sang preview UniversalRenderer. Đã sửa builder để tạm dùng preview pipeline trong default và mọi quality khi đóng gói rồi khôi phục toàn bộ cấu hình game trong finally. Không chữa bằng cách tắt depth hoặc ẩn log.

APK GPU trung gian:144,9MB (151.977.527byte), build103,37s; chưa cài vì đang bổ sung sửa resource depth. GPU skinning chuyển phép tính sang compute shader, giữ mesh/rig. Benchmark body-off mới tắt cả GameObject để không tính biến dạng của nhân vật ẩn.

Đã đối chiếu chữ ký Java từ android.jar: Intent.removeExtra trả void; sửa JNI sang Call không có kiểu trả về. Android chặn ADB input tap theo chính sách MIUI; không thay cài đặt bảo mật, dùng intent benchmark của chính app.

### Chốt kiểm tra hiện tại

APK đã cài:144,4MB (151.428.558byte), GPU skinning, đã sửa giữ UniversalRendererResources trong build. Bài đo8 workload chưa hoàn tất: lúc khởi chạy máy tắt màn hình/khóa (`screenState=OFF`, `mInputRestricted=true`) khiến app nhận PAUSE/STOP trước khi vào cảnh. Đã đề nghị người dùng mở khóa. Chưa xác nhận runtime depth sạch hoặc FPS GPU; không được lấy số FPS cũ thay cho bản này.

PlayMode chạy lại sau thay đổi benchmark/JNI: **12/12**, job `d322d95458ee4f4c8ba665bc4a02487f`. Nguồn Blender kiểm SHA lại: **9e24aaaa481572a1bbc1739873792c774aa7004bb06d74dd6da6fff62bdb29c5**, khớp manifest.

Builder khôi phục pipeline bằng đường dẫn asset nạp lại, tránh tham chiếu Unity Object bị unload khi build. Đã khôi phục renderer2D của game ở cả6 quality và default null; không reset các thiết lập khác. Build còn cảnh báo pow tiềm năng trên GLES/Vulkan và Diagnostics DebugSymbols; render Editor không có lỗi runtime. Chưa tuyên bố log build hoàn toàn sạch.

### Bàn giao bản sửa đuôi chiêu

Bản cuối `Builds/MacAnhPreview/MacAnhPreview.apk`: **151.428.558byte =144,4MB**, build thành công20,69s, GPU skinning. Đã cài lại thành công và gửi intent benchmark; ứng dụng còn chờ máy mở khóa. Unity mở scene preview và phát Skill02 bình thường. Cấu hình game sau build được kiểm chứng khôi phục đúng6 pipeline UniversalRP, default null, product Arena3Q và CPU skinning của dự án chính.

Shader ArcaneFlow đã clamp mẫu cloud và Fresnel trước pow, tránh cơ số âm do sai số GPU; không thay bảng màu. Chưa đo FPS bản cuối và chưa xác nhận runtime depth trên điện thoại đang khóa. Những số13–15FPS phía trên chỉ thuộc bản đầu. Tại thời điểm bàn giao, phần còn thiếu để nghiệm thu hiệu năng là mở khóa thiết bị và hoàn tất phép đo8 workload.
