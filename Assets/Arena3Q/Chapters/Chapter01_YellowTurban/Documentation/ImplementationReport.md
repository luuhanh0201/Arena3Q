# Giai đoạn B: báo cáo triển khai

## P3: sương và trạng thái thế giới, 29/10

`ChapterMapWorld` đọc tiến độ đã xác thực; không sinh kết quả trận, sao hoặc thưởng. Khu chưa mở ẩn NPC. Khi trở về sau thắng: chuyển nhóm trước/sau, đưa camera tới khu kế, tan sương, hiện NPC rồi mới hiện marker và lưu `revealedStages`. Tải lại giữ khu đã mở và trạng thái hoàn thành. Nhóm `State_After` chỉ có mô hình tại khu 1 và 4 trong scene bàn giao; những mô hình còn thiếu vẫn thuộc phần art chưa hoàn tất.

Sương dùng `smoke_08.png` Kenney Particle Pack CC0 local, shader chiếu màn hình xuống mặt đất XZ theo cùng vùng gần nhất của navigation. Khu khóa khử bão hòa và tối theo cấu hình; HUD giữ ở trên. Preset 30 dùng hai lớp mẫu mây, 60/120 ba lớp; thời gian chuyển tiếp và thông tin chơi giữ nguyên. Shader đọc opaque color của camera, cần đo chi phí render trên thiết bị sau này.

Unity PlayMode **12/12 qua**, không bỏ qua: `Logs/Chapter01-P3-flow-fixed.xml`; kiểm mở tuần tự bảy khu, không lặp reveal sau tải lại, cùng hồi quy đường đi/quest/trận. Ảnh từng khu tại `NghiemThu/Chapter01/P1_Runtime_20261028/run_20261029_025056_922`. Lượt đầu 11/12 và ảnh sương sai giữ lại để đối chiếu; không tính đạt. EditMode luật tiến độ và DisplayMetrics **7/7 qua** (`Logs/Chapter01-P3-core.xml`, 29/10 10:04).

WebGL P3 đã qua 12 tổ hợp màn hình/preset, thao tác chạm và ba hồ sơ QA đã thắng 1/4/7 ải: `NghiemThu/Chapter01/P3_Web_2026-10-29T03-03-25-545Z/result.json`; không pageerror/console error. Lượt bị ngắt quota lúc 03:02 không tính đạt. Build `b855906df042b06bd36988328711dcc126176a1029ed7c6f8cfddff75fc35355`, 0 lỗi/11 cảnh báo, 557,4 giây. Thư mục web khi build **460,9 MB**, tăng khoảng 0,1 MB so với trước lượt P3; đã gồm trang ảnh P2 51,3 MB. Không lấy số 409,5 MB trước khi thêm ảnh P2 để quy chênh lệch đó cho sương.

Phần còn lại: P4 môi trường/decal/điểm âm thanh; battle và phần thưởng chương chưa được soạn; NPC thật và các mô hình trạng thái còn thiếu; chưa nghiệm thu FPS/ASTC/nhiệt trên điện thoại.

Lượt web cuối có kiểm marker đã bấm được sau fade: **đạt**, `NghiemThu/Chapter01/P3_Web_2026-10-29T03-07-34-178Z/result.json` (25 ảnh, 12 tổ hợp, 3 hồ sơ, 0 lỗi browser). Bộ so sánh: `NghiemThu/Chapter01/P3_Review_20261029/index.html`; tự kiểm của Codex, chưa phải review độc lập của Claude.

## P2 web đã kiểm, 29/10

WebGL thật qua **12/12** tổ hợp Redmi, 16:9, 18:9, 20:9 × preset 30/60/120; safe area, DPI, vùng chạm tối thiểu 56 dp, không cắt chữ. Thao tác touch qua thông tin chương, đi tới NPC, nhận nhiệm vụ, mở bảng ải, chặn Xuất Chiến khi thiếu battle và trở về Sảnh. Không có pageerror/console error. Kết quả: `NghiemThu/Chapter01/P2_Web_2026-10-28T19-39-43-217Z/result.json` (20 ảnh). Ảnh so sánh và mẫu: `NghiemThu/Chapter01/P2_Review_20261029/index.html`.

Đã xem ảnh web: màu nước/mái nhà đúng ở bản này; lỗi màu trong capture batch có SRP Batcher chưa được coi là lỗi player. Log UI dài từng bị WebGL cắt; transport hiện chia đoạn 1000 ký tự, kiểm đủ thứ tự trước ghép JSON. Các lượt cũ thiếu kết quả không tính đạt.

Unity của bản P2 trước khi tiếp tục: `Chapter01-P2-final.xml` 11/11, `Chapter01-P2-frontend.xml` 11/11, `Chapter01-P2-labels.xml` 1/1. Lượt tiếp tục này chạy browser, không ghi các số Unity đó thành lượt test mới. Build hash `121d81fcf036a953b43ced9c20b002323bff1437fd878dee307a9367a3ff382c`.

Ở mốc P2, P3/P4 còn làm tiếp (xem cập nhật P3 phía trên). Đây chưa phải nghiệm thu trọn chương: NPC vẫn tạm, bảy battle/phần thưởng chưa có nội dung, chưa đo FPS hoặc ASTC/nhiệt trên thiết bị. Chọn preset 120 trên web không chứng minh máy chạy 120 FPS.

## P1a: dữ liệu và luật tiến độ

Đã thêm ChapterProgressRules thuần Core; các trạng thái khóa, có nhiệm vụ, đang thực hiện và hoàn thành; dấu hỏi/kiếm theo trạng thái; kiểm mã lượt đánh và ải khi nhận kết quả; sao tốt nhất, nhận rương một lần. ChapterMapConfig, MapUiConfig, MapMotionConfig giữ cấu hình có thể chỉnh. Tiến độ theo nhân vật/máy chủ trong PlayerProfile, có vị trí/hướng/camera trở về.

Unity 6000.6.6f1 EditMode: **5/5 đạt, không bỏ qua**, `Logs/Chapter01-P1-core.xml`, 28/10/2026. Kiểm khóa tuần tự, chặn kết quả sai/trùng, thua/hủy/đánh lại, save lỗi và JSON round-trip theo máy chủ. Đây chưa phải nghiệm thu map hoàn chỉnh.

Chưa có ở mốc này: prefab runtime, NavMesh, chuyển trận, UI, fog, chuyển động và kiểm web. Phần 2D đối màu được lưu riêng ở `ArtSource/Chapter01/2D/Assets/Revision_R2` (e01d0e3); chưa render lại tích hợp.

## P1b: tiếp tục ngày 28/10, chưa nghiệm thu

Trong working tree đã có prefab hệ thống, NavMesh theo 7 mức mở khóa, camera, chạm và đi tới NPC, hội thoại nhận nhiệm vụ, bảng Xuất Chiến, lưu pose, callback sao và chơi lại. UI mới là bản đầu. Giữ bản sửa của Claude: `walkableNames` gồm bridge/plateau/stairs và lấy mặt sàn dưới NPC để tới tế đàn cao.

Lượt tiếp tục này bổ sung:

- Hủy tương tác khi mục tiêu mất/ẩn/đổi trạng thái; đường bị xóa vẫn chạy timeout thay vì treo sự kiện chờ. Kiểm kết quả SetDestination, chỉ số mục tiêu và chặn chạm đất khi đang chuyển scene.
- Hủy lượt đánh bị gián đoạn và ghi thay đổi xuống kho hồ sơ khi khôi phục phiên.
- Validate kích thước navigation, timeout và tên công trình đi được trước bake; không cho tên rỗng làm mọi công trình thành nền đi được.
- Cập nhật kiểm Frontend sang Sảnh -> Chương 1. Giữ bài kiểm tuyến trận trực tiếp/capture cũ bằng cấu hình fixture tạm, phục hồi ngay.
- Thêm 3 bài PlayMode: đường bị xóa/quest đổi giữa đường; hủy lượt dở được lưu; đi qua scene trận thật, thắng rồi chơi lại thua, quay về đúng vị trí/hướng/camera. Bài trận dùng Stage_Attack_01 chỉ trong fixture, không gắn vào nội dung chương phát hành.

### Kiểm tra thực tế của lượt tiếp tục

- **Unity Test Runner chưa chạy:** khởi động batch bị chặn trước khi mở project, `No valid Unity Editor license found`, mã thoát ghi trong log là **198**. Log: `Logs/Chapter01-resume-baseline.log`. Không có XML mới để nhận kết quả.
- **Biên dịch C# độc lập đạt 5 assembly:** Data, Exploration, Exploration.Editor, Exploration.Tests.PlayMode và Tests.PlayMode. Dùng Roslyn/reference/defines của Unity đang cài, ghi output riêng tại `Logs/Chapter01-resume-compile/`, không thay DLL/cache của Editor. Có cảnh báo API FindObjects cũ, không có lỗi compile. Đây không phải chạy PlayMode, import/build player hoặc kiểm hình.
- Kiểm `.meta` cho Scripts/Editor/Tests: không thiếu; `git diff --check` các file tracked vừa sửa không báo lỗi khoảng trắng.
- Claude đã báo 5/5 map và 314/314 EditMode **trước các sửa mới**; không dùng các con số đó làm nghiệm thu bản hiện tại.
- Chưa có ảnh trước/sau mới, chưa build web/APK, chưa đo FPS thiết bị. Git không ghi được `.git/index.lock` (Permission denied), nên chưa commit/push.

### Việc cần tiếp tục

1. Khi Unity có giấy phép hoạt động, chạy lại 8 bài ChapterMapFlowTests cùng FrontendFlowTests/LobbyFlowTests; sửa mọi lỗi thực tế, rồi chạy bộ hồi quy phù hợp. Chụp bằng chứng mới, không ghi đè ảnh cũ.
2. Hoàn thiện P2 trên web theo ảnh mẫu và safe area điện thoại; P3 fog/mở khóa/State_Before-After; P4 chuyển động, decal và điểm âm thanh. Các phần này chưa hoàn thành.
3. Bảy battle và phần thưởng chương vẫn chưa có dữ liệu được soạn: runtime giữ thông báo đang phát triển, không giả chiến thắng hoặc tự cấp thưởng. Scene/prefab không đồng nghĩa chương đã chơi được trọn vẹn.
4. NavMesh ở `Assets/ThirdParty/Chapter01/_Navigation`, model/texture lớn và PNG nghiệm thu chỉ có local; dùng builder với đủ nguồn local khi dựng lại. Không sửa tay scene của Claude.

## Đính chính nguyên nhân cấp phép, 28/10 22:00

Không có bằng chứng giấy phép người dùng bị mất/hết hạn. Điều tra trực tiếp bằng Unity.Licensing.Client 1.18.3 với --showEntitlements và --showAllEntitlements xác nhận:
- Tiến trình công cụ chạy dưới MSI\CodexSandboxOffline, dù USERNAME/LOCALAPPDATA vẫn trỏ về luuha.
- UnityEntitlementLicense.xml của luuha vẫn tồn tại, đọc được; --showAllEntitlements thấy com.unity.editor và com.unity.editor.headless.
- License Client gặp System.Management.ManagementException: Access denied khi truy vấn thông tin card mạng bằng WMI, sau đó báo LicenseInvalidBindings / Machine bindings are not valid. Đây là lỗi xác minh nhận dạng máy trong sandbox, không phải thiếu quyền Editor trong giấy phép.
- Logs/claude-edit-all.log lúc 19:39 nhận entitlement thành công và kết thúc test exit 0. Cảnh báo Access token is unavailable và signature Code 10 cũng có trong lượt thành công, nên riêng các dòng này không chứng minh nguyên nhân.
- Bằng chứng đã lọc, không chứa nội dung giấy phép/token: Logs/Unity-license-sandbox-diagnosis.json. Không sửa/xóa/copy giấy phép, không đổi quyền hệ thống. Phiên hiện tại không cho phép chạy ngoài sandbox; chưa chạy lại test Unity.

Hướng xử lý đúng: chạy Unity trong phiên Windows thông thường của người dùng hoặc một phiên agent được cho phép chạy Unity ngoài sandbox. Không yêu cầu người dùng kích hoạt lại giấy phép chỉ dựa trên lần lỗi này.

## P1b xác minh lại, 28/10 22:37

Cơ chế cấp quyền của trang tin nhắn đã được người dùng cài và khởi động lại: lượt kiểm chạy bằng MSI\luuha, giấy phép Editor/headless xác minh thành công. Với preset trong dự án, việc chạy batch có thể xin nâng quyền qua cơ chế tự duyệt.

Unity PlayMode **19/19 đạt, 0 lỗi, 0 bỏ qua**, gồm 8 ChapterMapFlowTests và các bài FrontendFlowTests/LobbyFlowTests. XML/log: `Logs/Chapter01-resume-flow.xml` / `.log`. Kiểm thật vào map, quest từ xa, khôi phục pose, đường tới mục tiêu cả 7 ải, hủy đường/lượt dở, scene trận thật thắng/chơi lại thua/quay về, Sảnh -> Chương 1 và tuyến trận trực tiếp dự phòng. Không gắn battle/thưởng giả vào 7 ải phát hành.

Lượt trước 7/8 map: fixture gây thua bằng phá nhà ta trong ải Công, trái luật hiện có. Đã sửa fixture mô phỏng tới hết giờ; không thay luật game. Ảnh map mới lưu trong thư mục run timestamp ở `NghiemThu/Chapter01/P1_Runtime_20261028/`, giữ nguyên ảnh trước đó.

Mốc này xác minh P1b; chưa phải nghiệm thu P2 UI web/P3 fog/P4 chuyển động. Giữ nguyên các hạn chế về nội dung battle, nhân vật thật, dữ liệu thưởng và hiệu năng thiết bị.
