# Quy tắc bắt buộc cho model 3D, animation và VFX

Áp dụng cho mọi nhân vật; Mặc Ảnh là ca hồi quy đầu tiên. Đọc trước khi sửa rig, weight, action, exporter, importer, material hoặc hiệu ứng. Mỗi lỗi mới phải bổ sung nguyên nhân, cách phòng tránh và bằng chứng kiểm tra vào đây cùng `docs/QuyTac/NhatKySuaLoi.md`.

## Điều kiện bàn giao

- Không bàn giao khi còn kéo giãn, gai vải, xoắn vai, rách silhouette, bước nhảy pose, animation cứng hoặc hiệu ứng có biên cắt ngoài chủ ý.
- Không lấy test hữu hạn/quaternion/scale hoặc số test pass làm bằng chứng model không biến dạng. Phải đo mesh sau skinning và xem ảnh/video thật.
- Kiểm tra **toàn bộ model và toàn bộ clip**, không giới hạn ở vùng người dùng chụp. Bao gồm đầu/mặt, tóc, vương miện, vai, nách, tay áo, khuỷu, bàn tay/kẽ ngón, eo, vạt trước/sau/hai bên, chân, dây/trang sức và quạt.
- Chạy ở tốc độ thường và chậm; kiểm tra cả frame giữa hai key, chuyển clip, lặp clip, preparation, ra chiêu, recovery, fade và trả pool.
- Xem trước, sau, hai bên, góc chéo và góc trên. Lưu báo cáo, ảnh trước/sau và frame lỗi. Nêu rõ phần chưa xác minh; không đánh dấu đạt thay cho phép đo còn thiếu.

## Lỗi đã gặp và phòng tránh

| Lỗi | Nguyên nhân đã biết / điều phải kiểm tra | Quy tắc hồi quy |
| --- | --- | --- |
| Model co xuống 1/10 | Parent inverse còn scale tạm của heat solver | Cập nhật dependency graph; kiểm matrix basis/inverse, scale rig/mesh và chiều cao rest. Mặc Ảnh 1,70 m. |
| Vai/tay áo xoắn | Bone roll/pole gây tự quay quá mức | Kiểm góc xoay và silhouette nhiều góc, cả giữa key; dùng frame xoay ổn định. |
| Kẽ ngón thành gai | Mask màu cắt nhầm vùng đỏ/tối thuộc ngón | Phân vùng theo bề mặt/hình học; kiểm cả ngón và kẽ ngón sau skinning. |
| Vạt áo đi theo tay | Weight lan từ arm/sleeve sang thân áo | Kiểm lượng weight theo bone family và độ giãn cạnh ở mọi clip; không gán lại theo hộp tọa độ có mép cứng. |
| Mảng áo bị kéo thành vệt/răng cưa | Weight lân cận không liên tục; cắt top-4 hoặc làm tròn có thể tạo lại biên | Audit cạnh mesh sau skinning, cả cạnh rất ngắn; đo độ giãn tương đối kèm sai lệch tuyệt đối, lưu vị trí và bone weight của cạnh lỗi. Làm mượt rồi **đo lại sau giới hạn influence và xuất/import**. |
| Blender đúng nhưng Unity vẫn rách áo | Importer hoặc SkinnedMeshRenderer cắt bớt weight so với nguồn | Nguồn/importer/renderer phải thống nhất. Nguồn legacy nhiều weight chỉ là tham chiếu; hai LOD bàn giao phải tối đa4 và rendererBone4, audit sau import. Trả QualitySettings về giá trị cũ khi đóng preview. Unity có thể clamp getter minBoneWeight về0,001 dù lưu0; phải đo mesh đã import, không coi một setter là bằng chứng không cắt weight. |
| Blender che biến dạng mà Unity còn lỗi | Blender Preserve Volume dùng dual quaternion; Unity skin tuyến tính | Preview kiểm chứng cho Unity phải tắt Preserve Volume. Audit dùng cùng thuật toán và số influence của runtime; lưới Portrait/Battle mới bắt buộc tối đa 4. |
| Kiểm rig pass nhưng hình vẫn hỏng | Chỉ kiểm scale, hữu hạn, socket; chưa kiểm bề mặt | Kiểm toàn bộ cạnh/triangles của mesh đã biến dạng; xác nhận ảnh Unity của clip lỗi. |
| Audit FBX báo chiều cao 170 m, ngưỡng giãn sai | BakeMesh(false) đã áp scale100 của FBX, TransformPoint lại nhân100 lần nữa; cạnh và chiều cao khác hệ | Dùng BakeMesh(true) rồi đổi toàn bộ đỉnh sang world metres trước đo cả cạnh lẫn chiều cao. Assert chiều cao rest Mặc Ảnh 1,70 m ±0,05 m; đối chiếu bounds renderer. |
| Ra chiêu giật/cứng | Đổi hướng lớn trong một frame; mỗi chặng easing về vận tốc 0; thân/tay di chuyển đồng loạt | Kiểm vận tốc/gia tốc tay, quạt và thân; có lấy đà, truyền lực, follow-through, chuyển trọng tâm và recovery; giữ đúng thời điểm event. |
| Khựng ở đuôi chiêu | Giữ frame cuối; fade ngưỡng; hạt xuất hiện đồng loạt hoặc còn sống khi pool | Blend về idle; alpha liên tục; particle lifetime kết thúc trước release; test pause/seek/reuse. |
| Lốc giống ống đối xứng, miệng phẳng | Dùng cùng bán kính/chiều cao theo mọi góc; cắt density hoặc mesh tại một mặt phẳng | Biến thiên trục, bề rộng và chiều cao theo góc/thời gian, mép tiêu tán bằng turbulence trong thể tích; cage phải lớn hơn vùng có density. Xem từ trên và bên khi đang chạy. |
| VFX có vòng/ranh giới cứng | Helix tuần hoàn, alpha mask cứng, quad/cage lọt ra hình | Tránh lặp đối xứng hoàn hảo; kiểm mép/đầu ribbon và bounds tại mọi góc, tier và pha fade. Hình học trận pháp có thể đều theo chủ ý; gió/khói phải tự nhiên. |
| Nhầm đường chân trời của sân test thành mép cắt VFX | Màu nền khác màu sân tại far clip | Tắt lần lượt volume, ribbon, smoke để xác định đúng lớp gây mép. Nền kiểm tra không được tạo đường ngang đánh lừa đánh giá silhouette. |
| Khói lộ hình chữ nhật | Texture noise dùng làm billboard còn alpha tại biên UV | Thêm envelope mềm tại cả bốn mép billboard; kiểm từng lớp particle ở góc gần và khi xoay camera. |
| Tay áo dài bị hất lên như thanh cứng | Toàn chuỗi tay áo kế thừa đầy đủ xoay humerus | Kiểm đường đi của gấu tay áo khi nâng tay. Có bù pose để áo rủ theo thân/trọng lực và theo cánh tay vừa đủ; không chỉ làm mượt weight để che lỗi chuyển động của chain. |
| Blender trả exit0 dù script lỗi | Python exception hoặc lỗi save render không luôn trả mã tiến trình khác0 | Kiểm marker hoàn tất và trường passed của báo cáo; output render dùng đường dẫn tuyệt đối trong workspace. |
| Mesh VFX NaN | Pow của sin hơi âm tại endpoint | Clamp cơ số trước pow; kiểm mọi vertex hữu hạn ở mọi tier. |
| Model sai hướng sau export | Bỏ transform đổi trục tại root FBX | Giữ transform đơn vị/trục; kiểm rest và action sau import, không xoay bù chồng hai lần. |
| Quạt bám grip nhưng vẫn xuyên tóc/tay áo | Chỉ kiểm khoảng cách origin/socket và độ giãn cạnh; bỏ qua va xuyên giữa các mesh. Clamp cổ tay có thể làm lông quạt ngả vào tóc; tay áo rủ chưa đúng có thể che cán/tua | Kiểm tam giác quạt đối với bề mặt đã skin trên mọi frame/half-frame. Kiểm riêng cả6phần: lông, khung, ngọc, cán, chuỗi trang trí và tua. Chỉ loại tiếp xúc với vùng lòng bàn tay/ngón; không loại toàn bộ cán hoặc bỏ qua cổ tay áo. Giữ điểm cầm, có follow-through, render closeup lại. Phải kiểm mesh/action đã import Unity; phép dựng lại skin cần đối chiếu BakeMesh trước khi dùng. |
| Sửa key trước làm nhiễm pose của key sau | Vừa đọc vừa ghi cùng action đang nội suy, hoặc cộng lại độ cong tua khi bake lại | Chụp đủ pose cũ trước khi ghi lại channel; giữ các channel không thuộc sửa đổi. Biến đổi rest có property phiên bản/giá trị đã áp và chạy lại không cộng dồn. Kiểm frame giữa key trên bản đã lưu, không chỉ pose procedural trong bộ nhớ. |
| Xuất bind pose còn tư thế của clip trước | Reset Euler trong khi bone dùng quaternion | Xóa action rồi reset toàn bộ matrix_basis, cập nhật view layer trước xuất geometry; kiểm bind/rest sau round-trip. |
| Event sai thời điểm | Importer dùng normalized time; AnimationClip dùng giây | Đối chiếu event của clip **đã import** với manifest theo frame/30, gồm combo/ultimate. |
| Màu sai / PNG tối / vật liệu hồng | Nhầm linear/sRGB hoặc shader không hỗ trợ renderer | Giữ map nguồn, đối chiếu color space, render target và shader pass; kiểm cả renderer preview và trận đấu. |
| FBX mới thành mảng trắng trong trận3D | Đổi tên material khi bake; remap cũ không còn khớp, mesh nhận Sprite-Lit mặc định. Texture reimport có thể làm mất importer đang giữ | Nạp lại ModelImporter sau khi import hết texture; gán remap theo tên material trong export report; assert sharedMaterials của mesh thực trỏ đúng asset PBR trước lưu prefab. Xem ảnh trong trận sau import. |
| Làm sạch seam xong lại giãn/ép dù weight trước đó đạt | Sửa tọa độ rest nhưng giữ hệ số nội suy theo tọa độ cũ | Tính lại trường weight trên tọa độ rest cuối, normalize/limit rồi chạy đủ frame và half-frame lại. |
| Vàng/vương miện thành đen sau bake | Diffuse colour bake loại phần albedo kim loại | Nếu cần bake màu, nối nguyên đầu vào Base Color nguồn qua Emission. So sánh riêng vàng, da, vải sáng/tối; normal bake không chứng minh màu đúng. |
| LOD giữ màu nhưng normal lóe bất thường | UV nguồn có quá nhiều mảnh sau giảm lưới, tangent frame không phù hợp | Giữ nguyên UV0/texture/màu theo yêu cầu. Có thể thêm UV1 riêng cho normal bake; shader phải dùng đúng UV và tangent frame của UV1. Hash UV0 trước/sau; không thay UV0 rồi gọi là giữ nguyên. |
| Normal bake làm mặt/vải lốm đốm, hoa văn trông vụn | Ảnh normal ghi vào UV1 nhưng tangent bake lấy active render UV0 | Đặt NormalBakeUV thành active render trong lúc bake, rồi trả UV0 về active render; Normal Map node và shader dùng cùng UV1. So sánh có/tắt normal cùng góc trước khi xuất sprite. Đối chiếu hướng dẫn Normal Map của Blender: https://docs.blender.org/manual/en/latest/render/shader_nodes/vector/normal_map.html |
| Bóng phân thân không tan hoặc bị ép dẹt | Wrapper 3D trả sớm trước fade sprite; áp lại phép chiếu vào sprite đã render | Chỉ vẽ một snapshot đúng pose; reset chiếu của snapshot 2D, fade theo battle clock cho cả sprite và ảnh render 3D. Thu hồi texture/mesh snapshot khi trả pool. |
| Android thiếu CopyDepth | Build stripper chỉ thấy Renderer2D | Đóng gói đúng resources/pipeline; kiểm log trên thiết bị; khôi phục pipeline dự án sau build trong finally. |
| Particle chạy khi pause; seek lệch | Nhiều clock khác nhau | Mesh, shader, particle, projectile và pool cùng clock; kiểm seek tiến/lùi và thuê lại. |
| Va chạm/ultimate mất hình trong phép chạy nhiều bước | Pool cộng thời gian cả trước khi effect được sinh | Mỗi instance lưu thời điểm spawn/tick riêng; không dùng một delta chung cho effect mới. Capture phải xác nhận cả sau release, không chỉ có charge. |
| FPS thấp nhưng báo animation mượt | Chỉ xem easing, không đo frame time | Tách chất lượng chuyển động và hiệu năng; chỉ ghi FPS của đúng bản build, thiết bị và workload đã đo. |
| VFX vẫn trôi khi pause dù pool đã dừng | Shader lấy `_Time.y` thay đồng hồ instance | Truyền thời gian effect/battle qua property block; ảnh pause phải không đổi, kiểm tốc độ/seek tiến-lùi/reuse. |
| Bóng ảnh sprite sạch nhưng bóng realtime còn vòng trắng | Hai đường snapshot không tắt cùng lớp mesh prefab | Dùng chung bước chuẩn bị snapshot; kiểm sprite và captured3D, rồi tái thuê để lớp mesh thông thường vẫn bật đúng. |
| Snapshot đúng bounds/index nhưng thủng thành tam giác rời | Material bóng sprite lấy alpha atlas qua UV của model3D, hai hệ UV không khớp | Material model3D riêng không dùng atlas sprite làm mask. Kiểm silhouette pixel qua sáu góc và ảnh trong trận ngoài số đo geometry; fade mọi phần cứng và skin theo cùng clock. |
| Flipbook giật giữa các frame nguồn | Chọn floor(frame) mà không hòa frame kế | Theo yêu cầu21/10, shader hòa hai frame VFX theo phần lẻ thời gian effect; kiểm atlas khác trang/rect, loop cuối→đầu, nonloop giữ frame cuối, pause và pool. Không áp hòa này lên sprite nhân vật. |
| Android chạy30FPS dù hiệu ứng đã sửa | Không đặt targetFrameRate khi vào game; PC có thể bị vSync khóa | Target lấy từ ScriptableObject, tối thiểu60 cho Frontend/Battle/standalone scene; kiểm khôi phục sau preview/benchmark. Đo FPS/p95/khung tụt/nhiệt trên đúng APK với5tướng+lính+địch+VFX; đặt cap60 không phải chứng minh đạt60. |

## Checklist thực thi

1. Sao lưu nguồn trước sửa. Hash tọa độ rest, topology, UV, texture/material; xác nhận quyền sở hữu action. Giữ nguyên hình dáng và chi tiết nguồn khi sửa skinning.
2. Đọc **weight đang lưu thực tế**, không tin cache cũ. Kiểm tổng weight=1, không vertex mất weight, số influence đúng cấu hình runtime, không weight ngoài bone hợp lệ.
3. Audit mesh đã skin toàn bộ frame của 11 clip Mặc Ảnh; thêm mẫu nửa frame quanh đổi hướng. Báo cạnh giãn bất thường theo vùng, bone family và frame. Không bỏ qua lỗi lớn bằng đổi threshold.
4. Sửa nguyên nhân và chạy lại audit trên kết quả cuối sau normalize/limit/export. Kiểm diện tích/độ ép, silhouette, va xuyên và quạt bám tay bằng ảnh thật.
5. Giữ event gameplay đúng frame. Kiểm animation trong lúc ra chiêu, kể cả combo, không chỉ lúc tan biến. Quan sát truyền lực từ chân/hông/thân tới tay và quạt.
6. Xem cả 7 chiêu, 3 tier VFX, camera trước/sau/bên/trên; kiểm bất đối xứng tự nhiên, mép mềm, chiều sâu và pha kết thúc.
7. Import Unity, compile sạch, chạy test EditMode/PlayMode phù hợp, capture/video thật. Kiểm nguồn prefab/controller/FBX khớp bản sửa. Sprite trong trận nếu được tạo từ nguồn đã sửa cũng phải tái xuất và kiểm lại.
8. Ghi rõ kết quả đã chạy, đường dẫn artifact, lỗi còn lại và giới hạn. Chỉ hoàn tất bàn giao khi mọi lỗi thuộc yêu cầu đã sửa và kiểm chứng.
9. Với flipbook, lưu chuỗi frame/video60Hz ngoài ảnh tĩnh; so trước/sau quanh mốc đổi frame, pause/seek/loop/khác trang atlas. Với hiệu năng, lưu hashAPK, thiết bị, workload, thời gian đo, FPS trung bình/p95 và nhiệt; không thay kết quả thiết bị bằng Editor hoặc targetFrameRate.
