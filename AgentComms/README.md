# AgentComms – kênh giao việc và phối hợp giữa các agent

> Mọi quy tắc làm việc và nhật ký rút kinh nghiệm của dự án (quy tắc người dùng giao, luật 3D, nhật ký vận hành 3D, nhật ký sửa lỗi): **`docs/QuyTac/`** (mục lục `docs/QuyTac/README.md`). File này chỉ là giao thức trao đổi giữa hai agent.

Folder này nằm ở gốc repo, ngoài `Assets/` (Unity không import). Người dùng giao việc cho **từng agent** qua đây; các agent báo cáo và phối hợp với nhau cũng qua đây.

```
AgentComms/
  README.md        quy tắc này (mọi agent đọc trước khi làm)
  STATUS.md        trạng thái hiện tại của từng agent + file đang giữ
  claude/inbox/    việc giao cho Claude (Claude Code)
  claude/outbox/   báo cáo của Claude
  codex/inbox/     việc giao cho Codex
  codex/outbox/    báo cáo của Codex
```

## 1. Giao việc
- Người dùng tạo một file trong `<agent>/inbox/`, tên `YYYYMMDD-HHMM_<ten-ngan>.md` (ví dụ `20261020-1430_sua-weight-lubu.md`). Nội dung tự do; nên ghi mục tiêu, phạm vi file được sửa, điều kiện xong.
- Agent **không sửa, không xoá, không đổi tên** file trong inbox của mình (để người dùng xem lại được).
- Một agent cần agent kia làm gì thì ghi vào inbox của agent kia, tên bắt đầu bằng `tu-<agent>_` (ví dụ `tu-claude_20261020-1500_can-unity-dong.md`). Việc do người dùng giao luôn được ưu tiên hơn.

## 2. Nhận việc và báo cáo
- Một việc được coi là **đã nhận** khi agent tạo báo cáo cùng tên trong outbox của mình: `<agent>/outbox/<tên file việc>.report.md`.
- Dòng đầu của báo cáo là trạng thái, cập nhật trong lúc làm:
  `Trạng thái: đang làm` → `Trạng thái: xong` | `Trạng thái: bị chặn – <lý do>` | `Trạng thái: cần người dùng quyết – <câu hỏi>`
- Báo cáo khi xong ghi: đã làm gì, file đã sửa, test/kiểm tra đã chạy và kết quả, đường dẫn ảnh/log, phần chưa làm hoặc chưa kiểm chứng, commit (nếu có).
- Làm từng việc theo thứ tự tên file (cũ trước). Việc chưa có báo cáo = việc mới.

## 3. Theo dõi khi được yêu cầu
- **Chỉ theo dõi khi người dùng yêu cầu rõ ràng** (chỉ đạo25/10/2026: "bỏ theo dõi đi, chỉ thoe dõi khi nào tôi yêu cầu"). Không tự chờ hoặc theo dõi1giờ sau khi hoàn tất công việc. Chỉ đạo này thay thế quy tắc chờ mặc định trước đây.
- Khi được giao theo dõi, ghi phạm vi và thời hạn theo yêu cầu vào `STATUS.md`; không tự kéo dài thời hạn sau một việc mới.
- Người dùng yêu cầu dừng → dừng ngay, cập nhật trạng thái; không để watcher chạy ngầm.
- Việc đọc inbox khi bắt đầu/đang thực hiện một nhiệm vụ vẫn theo quy trình nhận việc; đó không phải phiên theo dõi sau bàn giao.

## 4. Tránh dẫm chân nhau
- Trước khi sửa, agent ghi vào phần của mình trong `STATUS.md` các **file / folder đang giữ**. Chỉ sửa phần của mình trong `STATUS.md`.
- **Không sửa file / folder mà agent kia đang giữ.** Cần sửa thì ghi yêu cầu vào inbox của agent kia, hoặc hỏi người dùng.
- Unity Editor và batch mode không chạy song song trên cùng project: agent cần chạy Unity batch thì giữ mục `Unity` trong `STATUS.md`; agent kia không mở Unity cho tới khi mục đó được nhả.
- Git: mỗi agent chỉ `git add` đúng file của việc mình làm (không `git add -A`), không commit thay đổi của agent kia, commit xong thì push (quy ước của repo). Không `reset`, `checkout --`, `stash` hay xoá thay đổi chưa commit của người khác.
- Vẫn tuân thủ `CLAUDE.md`, `docs/QuyTac/Model3D_Rules.md` và các tài liệu của repo.

## 5. Git và folder này
Chỉ `README.md`, `STATUS.md` và các `.gitkeep` được commit; file việc và báo cáo trong inbox / outbox bị `.gitignore` bỏ qua (giao tiếp tạm thời, tránh hai agent xung đột khi commit).

## 6. Nghiệm thu
Khi agent làm báo `Trạng thái: xong` cho một phần việc, agent review (từ 2026-10-21 06:40 theo chia việc của người dùng: **Codex làm Mặc Ảnh + Lữ Bố, Claude review phần đó và làm phần chuyển 3D còn lại**; luôn xem ghi chú chia việc / DOI-VAI mới nhất) kiểm tra (test Unity, đo bề mặt, chụp trận thật nhiều góc). **Chỉ khi đạt**, agent review tạo một đợt trong `NghiemThu/<Tướng>/<NN>_<TênĐợt>_<YYYYMMDD>/` với ảnh chụp chi tiết và % so với tài liệu để người dùng tự kiểm (`NghiemThu/README.md`). Chưa đạt thì chỉ có phản hồi trong inbox của agent làm.

## 7. Ảnh trước – sau cho mỗi chỉnh sửa (bắt buộc, từ 2026-10-20)
Mỗi lần sửa một phần, dù nhỏ (weight một vùng, một clip, một VFX, đóng gói atlas, thông số nhập ảnh…), agent làm phải:
- chụp **cùng góc, cùng khung, cùng frame** trước và sau khi sửa, ghép vào một ảnh so sánh (trái = trước, phải = sau; ghi tên vùng / frame lên ảnh nếu có thể);
- lưu vào `NghiemThu/<Tướng>/XemTruoc_<TenViec>_<YYYYMMDD>/` (ảnh bị gitignore, README được commit), kèm số đo nếu có (số cạnh lỗi, kích thước, chênh lệch pixel);
- **mở ảnh đó lên màn hình cho người dùng kiểm tra** ngay khi sửa xong phần đó, rồi mới làm phần tiếp theo. Thay đổi không đổi hình (ví dụ đóng gói lại atlas) vẫn phải có ảnh chứng minh hình không đổi.
