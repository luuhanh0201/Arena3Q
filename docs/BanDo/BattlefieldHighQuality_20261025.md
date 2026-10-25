# Chiến trường 3D Arena3Q, scene v1 ngày 25/10/2026

Đã dựng scene **`Assets/Arena3Q/Scenes/Battlefield_HighQuality.unity`**, mở/sửa trực tiếp và nhấn Play để thử PvE. Trang xem ảnh/video Unity: **http://127.0.0.1:4311/app/map.html**. Đây là bản môi trường bằng bộ asset người dùng đã xem và cho phép dùng qua lệnh “dựng map đi”; chưa coi là nghiệm thu art cuối giống ảnh concept hoặc nghiệm thu FPS mobile.

## Audit và phạm vi thay đổi

- Unity **6000.6.6f1**, URP **17.6.0**, Universal Forward renderer index1. Giữ pipeline dự án; ba URP asset của map là bản sao riêng, chỉ áp dụng trong scene mới khi Play, và phục hồi khi scene bị dỡ.
- Camera chiến đấu hiện có được tái sử dụng. Bản mới dùng góc cao50°, orthographic, zoom8 đến mức toàn cảnh; cấu hình ở `BattlefieldConfig` và camera. Script camera chung chỉ thêm cờ `allowMapOverview`, mặc địnhfalse ở scene cũ.
- Không có package Terrain Tools, Cinemachine hoặc AI Navigation trong manifest. Có Shader Graph đi cùng URP, Input System1.20.0. Không cài/chuyển package hay pipeline để dựng map.
- Địa hình dùng bốn mesh có chung cạnh và normal, phù hợp bản đồ nhỏ; không dùng một ảnh vẽ toàn cảnh trải lên plane. Mỗi mesh nguồn khoảng620–636KB, scene khoảng120KB, section prefab lớn nhất dưới450KB.
- Đã xem và tái sử dụng đúng chín đạo cụ Arena3Q. `BaguaStonePillar` là một cụm riêng, nên đặt tám lần; tế đàn là một model, đặt một lần. `ThreeKingdomsSquare` là lều, `Command_*` là đài trống. Dùng mesh Props3D nhẹ có sẵn, không lấy bản nguồn hàng triệu tam giác cho từng vật thể mobile và không sửa mesh nguồn.
- Asset ngoài: Quaternius Nature/Props Standard và Kenney Particle Pack, CC0. Giấy phép, checksum archive và danh mục trong [báo cáo asset](BattlefieldAssetReview_20261025.md).
- Kiểm SHA256 **40 file nguồn** gồm scene cũ, cấu hình map/view, FBX và texture được bảo vệ: **không file nào đổi**. Các chỉnh sửa có sẵn của công việc khác không được hoàn tác hoặc đưa vào commit này.

## Các nhóm đã triển khai

| Nhóm | Kết quả thực tế |
|---|---|
| 1. Audit, asset, giấy phép | Đã kiểm project, nguồn và ảnh/model; bộ xem185 tài nguyên được giữ trên4311. |
| 2. Scene và cấu hình riêng | Scene mới, hai ScriptableObject chính, section prefab và các công cụ Editor. |
| 3. Địa hình và đường | Vùng chiến đấu80×44, XZ/Y-up; nền có độ cao ở viền, hai tuyến cong, sáu texture blend qua vertex mask. Không sông/cầu. |
| 4. Trung tâm Bát Quái | Tế đàn, tám trụ gốc, tám rune; các cửa đóng/mở và đổi loại theo hệ thống trận pháp hiện hành. |
| 5. Hai doanh trại | Lều, đài trống, tháp, hàng rào, cờ, vũ khí, xe, thùng, bao, bàn, đuốc; cặp bố trí quay180°, cloth xanh/đỏ. |
| 6. Cây/cỏ và LOD | Cây nhiều biến thể, bụi, cỏ, hoa, đá; cây lớn ở ngoài vùng đi bộ, bốn mesh cỏ dùng chung. LOD gần là model gốc, xa là billboard512px dẫn xuất từ đúng model. |
| 7. Ánh sáng và môi trường | Một sun vàng nhẹ, ambient, ACES/color/bloom nhẹ; GPU wind cho cây/cỏ/cờ; particle hữu hạn cho lửa/khói/bụi/lá, có fade đầu/cuối. |
| 8. PvE | Giữ combat, tối đa5 tướng, chọn/ra lệnh/đánh tự động. Hai Stage clone Công/Thủ; spawn, collider/vùng cấm và navigation hiện có đã kiểm. |
| 9. Tối ưu và kiểm tra | GPU instancing, Low/Medium/High; cây chắn tướng mờ dần rồi phục hồi. Sáu bài PlayMode map và hai hồi quy camera cũ đạt. Có số đo Editor thực, chưa có số đo thiết bị. |
| 10. Bàn giao | Scene/config/prefab/shader, hướng dẫn dưới đây, ảnh trước–sau, video và trang xem local. Các phần chưa đạt cuối được nêu riêng, không tính thành kết quả đã kiểm. |

## Hierarchy

```text
Battle Controller                       # giữ composition root/input/HUD cũ
  BattlefieldStartup                    # gắn BattleView riêng trước StartBattle
  BattlefieldLegacyBridge               # adapter trình bày và nâng chân theo mặt tế đàn
Main Camera                             # camera hiện có
Arena3Q_Battlefield
  Environment
    Terrain                             # bốn mesh + MeshCollider
    Roads / Cliffs / Rocks
    Vegetation
      Trees / Bushes / Grass / Flowers
    Decorations
  BattleCenter
    ExistingBaguaAltar
    ExistingOctagonalPillarsAndRunes
  Camps
    BlueCamp / RedCamp
  Lighting
  EnvironmentVFX
  Navigation                            # mốc tổ chức, không giả làm NavMesh đã bake
  SpawnPoints
  AssetReplacementAnchors               # mốc trống cho asset còn thiếu
  [Environment / Quality / GateState / Instancing / Generated]
```

Trong Editor, prefab render bình thường và chỉnh được ngay, không phải chạy game để sinh map. `BattlefieldGenerated` đánh dấu subtree do builder sở hữu. Rebuild chỉ thay subtree này; không xóa scene khác hoặc prefab nguồn. Giữ đồ chỉnh tay ngoài subtree nếu không muốn lần rebuild sau thay thế.

## Chỉnh sửa và dựng lại

1. Mở scene mới. `Assets/Arena3Q/Content/Battlefield/BattlefieldConfig.asset` chỉnh kích thước, seed, chiều rộng đường, bán kính vòng/tế đàn, vị trí trại, mật độ, camera, nắng và các thông số gió. Khi đổi cỡ map, chỉnh `campX`/`ringRadius` cho vừa khoảng chiến đấu.
2. `BattlefieldAssetLibrary.asset` chứa các prefab tham chiếu và sáu albedo. Kéo prefab khác vào entry tương ứng; ID là khóa của recipe, không phải yêu cầu prefab phải giữ tên/đường dẫn nguồn. Cờ `missing` và ghi chú phân biệt asset còn thiếu.
3. Chọn **Arena3Q > Battlefield > 2 Rebuild Environment** sau khi sửa config/thư viện. Rebuild cập nhật cả stage clone/vùng cấm để khớp cảnh. `1 Generate Battlefield` dành cho khởi tạo lại bộ mặc định từ binding đã audit, có thể đặt lại danh sách entry mặc định.
4. `3 Validate Battlefield` kiểm StageConfig, spawn hợp lệ và material. `4 Clear Generated Decorations` xóa riêng nhóm đồ trang trí phát sinh, có Undo. Menu `Graphics` áp Low/Medium/High. Generate/Rebuild yêu cầu dừng Play Mode.
5. Chỉnh trực tiếp section prefab ở `Art/Battlefield/Prefabs/Sections`, hoặc đặt override trong scene. Nếu thay model lớn, cập nhật collider/vùng cấm ở các Stage clone rồi Validate. Rebuild sẽ áp lại recipe/config.

`Tools/BattlefieldAssetBindings.json` ánh xạ model/material từ bộ đã audit để tạo library mặc định. Runtime không tìm prefab bằng đường dẫn và không tải asset ngoài mạng.

## Gameplay và navigation

Logic game gốc dùng mặt phẳngXY, trục lên−Z. Môi trường mới dùngXZ/Y-up. Adapter của scene chỉ xoay cứng hierarchy trình bày và camera trong lượt render, rồi phục hồi ngay; logic, picking, chỉ số tướng, kỹ năng và luật chiến đấu giữ hệ tọa độ cũ. Hai bài hồi quy scene cũ kiểm camera/tilt/picking vẫn đạt.

Navigation dùng `BattlefieldLayout` có sẵn: hình học trụ/cửa động lấy từ Formation clone, hàng rào/lều/tháp/đá lớn lấy từ bounds prefab làm vùng cấm. Các đạo cụ tương ứng có collider3D; thân cây ở ngoài vùng đi bộ, không đặt cây cao giữa sân. Điểm triệu hồi nằm trong sân trống. Cổng Đỗ dùng vật cản logic động như game cũ, không thay bằng một hệ combat/NavMesh thứ hai.

Khi tướng lên tế đàn, adapter raycast đúng MeshCollider của mesh gốc để nâng hình đến mặt đá trong lúc render, không sửa tọa độ gameplay. Khi cây gần camera chắn tướng, hệ instancing giảm độ hiện bằng dither trong0,25giây, giữ18% hình cây rồi phục hồi khi hết che; collider vẫn giữ nguyên. Ảnh `occlusion-before/faded.png` cho thấy trường hợp tướng bị che và được nhìn lại.

Nếu về sau chuyển sang Unity AI Navigation: chỉ thêm NavMeshSurface cho groundXZ, dùng collider/vùng cấm hiện có, bake sau khi chỉnh model; cần liên kết trạng thái cửa động trước khi thay pathfinding. **Hiện không có NavMeshSurface hoặc NavMeshData đã bake**, vì game đang đi theo hệ navigation hiện hành đã kiểm.

## Đồ họa và số đo

| Preset | Mật độ cỏ | Wind | Shadow / post | LOD bias | Target |
|---|---|---|---|---:|---:|
| Low | 1/4 số cặp patch | giảm | tắt |0,6|30FPS|
| Medium | 1/2 | đầy đủ | chọn lọc,1024 |1|30FPS|
| High | đầy đủ | đầy đủ | sun mềm2048, ACES/bloom nhẹ,MSAA2 |1,8|60FPS|

Mức30FPS chỉ nằm trong scene mới theo brief chiến trường lần này, không đổi chính sách scene cũ. Mục tiêu không phải kết quả đo. Preset không đổi bố cục vật cản hoặc gameplay. Particle tắt emission ở Low, không sinh Animator cho từng bụi cỏ. Texture đạo cụ dẫn xuất dùng albedo tối đa2048, normal1024 và ASTC6×6 cho Android/iPhone; ảnh nguồn giữ nguyên.

Runtime giữ prefab/collider để chỉnh trong Editor, nhưng gom nguồn cùng mesh/material/submesh bằng [Graphics.RenderMeshInstanced của Unity](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html). Danh sách/mảng tái sử dụng, hỗ trợ fallback renderer thường khi máy không có instancing. Bốn biến thể patch cỏ được chia sẻ thay vì tạo một mesh riêng cho mọi patch. Có thể tắt `BattlefieldInstancing` để so cách render thường; LOD/renderer được phục hồi.

Đo **Editor D3D12,1600×900 RenderTexture, toàn cảnh với5 tướng và lính/địch**, bằng ProfilerRecorder và đối chiếu UnityStats:

| Mức | Lượt vẽ | Tam giác render |
|---|---:|---:|
| Low |201|652.414|
| Medium |227|1.070.160|
| High |231|1.360.186|

Trước tối ưu: Low819 lượt vẽ, High1.009 lượt vẽ/1.483.554tam giác. Kết quả dao động theo frame, VFX và gate; không coi một snapshot là p95 hoặc FPS. Bộ đếm Batches trong batch Editor trả0 nên không dùng. Ước lượng runtime memory của53 texture được material tham chiếu trong **Editor** khoảng349MB, toàn bộ Editor allocation khoảng881MB; không suy ra bộ nhớ texture của bản Android từ số này. Cần đo riêng bản build trên GPU thực, đặc biệt mứcHigh còn hơn1triệu tam giác và foliage có alpha overdraw.

## Kiểm tra đã chạy

- Generate cuối mở scene/lưu prefab/capture thành công; không có compile error hoặc shader error trong lượt cuối.
- **7/7 PlayMode map:** đủ5 tướng, đi tuyến trên; Stage validation và kết nối cả hai tuyến; Công/Thủ và wave; tọa độ picking/render phục hồi; ba preset giữ vùng cấm và đối xứng cỏ; gió khác27.822pixel giữa hai pha; particle có hoạt động và fade; chân theo mặt tế đàn; cây mờ/phục hồi; bộ đếm render thực; nút đổi chất lượng/FPS target và đổi Công/Thủ hoạt động.
- **2/2 hồi quy Battle cũ:** camera trong bounds/edge icon và tilt/picking.
- **Web:**10 ảnh decode đủ1600×900, video phát được, không pageerror, không tràn ngang ở412px; POST405 và traversal/file log ngoài whitelist404.
- Nguồn:40/40 SHA256 giữ nguyên. Đã mở ảnh toàn cảnh, ảnh trong trận, Low, cận cảnh, occlusion trước–sau và screenshot trang xem.

Lệnh chạy lại từ gốc project, đóng Unity Editor khác trước khi chạy:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe' -batchmode -quit -projectPath D:/UnityProjects/Arena3Q -executeMethod Arena3Q.Editor.BattlefieldBuilder.Generate -logFile Logs/BattlefieldGenerate.log
& 'C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe' -batchmode -projectPath D:/UnityProjects/Arena3Q -runTests -testPlatform PlayMode -testFilter Arena3Q.Tests.PlayMode.BattlefieldHighQualityTests -testResults Logs/BattlefieldTests.xml -logFile Logs/BattlefieldTests.log
node docs/BanDo/Tools/asset-review/server.cjs
node docs/BanDo/Tools/asset-review/verify-map.cjs
```

## Phần chưa đạt cuối và cách bổ sung

| Phần | Hiện trạng / bước tiếp |
|---|---|
| Model đúng loài/kiến trúc | Chưa có tre, đào nở hoa, phong đúng loài, đèn đá và tường đổ phương Đông. Các entry `missing` có mốc trống ở `AssetReplacementAnchors` hai bên rìa. Kéo model có giấy phép vào mốc hoặc thay prefab entry rồi cập nhật recipe; không thay bằng Cube/Sphere và nhận là art cuối. |
| Cliff kit/cổng trại | Dùng các model đá, tháp/hàng rào đã duyệt; chưa có kit cliff/cổng phương Đông riêng. Thay section `Cliffs`/camp bằng prefab mới khi có, giữ lối vào. |
| Art polish theo concept | Bộ miễn phí và đạo cụ gốc khác độ chi tiết; đã đồng bộ kích thước, matte/cloth và ánh sáng, nhưng vẫn cần duyệt art/mật độ/texture lặp. Không tuyên bố đạt chất lượng thương mại cuối giống ảnh tham chiếu. |
| Baked lighting | Đang dùng một sun realtime và ambient, không có lightmap bake cuối. Có thể bake sau khi chốt layout/model; chưa có bằng chứng lightmap. Fog/SSAO/DOF không bật. |
| Tướng/lính | Tái sử dụng prototype; một số đơn vị vẫn có hình tạm. Việc dựng model/animation mới cho chúng không nằm trong thay đổi map này. |
| Thiết bị và triển khai | APK test đã build và kiểm chữ ký, mở map mới trực tiếp. Chưa chạy/đo FPS/CPU/GPU/nhiệt trên máy thật và chưa build iOS. Scene mặc định của project được giữ. |

## APK kiểm tra trên điện thoại

Lệnh **Arena3Q > Battlefield > Build Android Test APK**, hoặc `-executeMethod Arena3Q.Editor.BattlefieldAndroidBuild.Build`, xuất `Builds/Android/Arena3Q-Battlefield-20261025.apk`. APK Development mở scene mới đầu tiên, vẫn chứa Frontend/Battle cũ; không đổi danh sách scene mặc định của project hoặc bundle ID. Trên mobile mặc định mứcVừa/30FPS target, có nút **Đồ họa** để chuyển Thấp/Vừa/Cao và **Chuyển ải Công/Thủ** để kiểm cả hai mode. Lựa chọn chất lượng lưu riêng trong PlayerPrefs; dữ liệu tài khoản không đổi.

**Build thành công:**382.807.684byte (**365,1MiB**),0lỗi,10cảnh báo UAC0009 từ directive DEVELOPMENT_BUILD ở BattleInputController đang có;736,8giây. AndroidARM64, Android8/API26 trở lên, targetAPI36. `apksigner verify` đạt chữ kýv2, cùng certificate với APK cũ nên hỗ trợ cài đè. SHA256: `6aa9abe43eedb39539a11ef54db76116d92877cb6aa6e609e444b3a0b95816d4`. Mã runtime của bản build: commit`18584c1`.

Tải qua trang xem hoặc [link APK local](http://127.0.0.1:4311/test-apk/Arena3Q-Battlefield-20261025.apk). Đây là địa chỉ trên máy đang chạy server; có thể tải ở máy tính rồi chuyển APK sang điện thoại. Chỉ gửi fileAPK, không gửi thư mục `BackUpThisFolder_ButDontShipItWithYourGame` chứa symbols. `BuildReport.totalSize` gồm các output khác, không phải dung lượng APK. Bản này chứa cả icon kỹ năng đã duyệt và tài nguyên game hiện có, không phải365MiB chỉ riêng map. Chưa tự cài/chạy bản này trên điện thoại; người dùng sẽ test theo yêu cầu.

## File local cần sao lưu

Theo quy tắc repo chỉ commit code/asset nhỏ, không LFS: sáu PNG đất gốc và các bản texture `Owned_*` để local; `.meta`, material, cấu hình, mesh nhỏ, prefab và scene được commit. Các mesh `Terrain.asset`/`GrassPatch_*` là bản trung gian cũ local, không còn được scene mới tham chiếu.

Cần giữ đầy đủ `Assets/ThirdParty/{Quaternius,Kenney}`, chín nguồn Arena3Q/Props3D và `.meta`, `Art/Battlefield/Textures`, cùng `NghiemThu/Battlefield/XemTruoc_Assets_20261025` và `XemTruoc_Map_20261025` khi chuyển máy. **Repo Git riêng lẻ không chứa toàn bộ model/texture lớn**. Builder dùng nguồn local để dựng lại, không tự tải hoặc mua asset.
