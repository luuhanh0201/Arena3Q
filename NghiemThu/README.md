# NghiemThu – hồ sơ nghiệm thu để người dùng tự kiểm

Claude tạo một **đợt nghiệm thu** khi Codex báo xong một phần việc **và** review của Claude (test Unity + đo bề mặt + chụp ảnh thật) đạt. Mỗi đợt là một folder riêng, không sửa đợt cũ, để so sánh trước / sau.

```
NghiemThu/
  <Tướng>/<NN>_<TênĐợt>_<YYYYMMDD>/
    README.md          kết luận, % so với tài liệu, danh sách cần nhìn, lỗi còn lại
    1_TrongTran/       ảnh chụp trận thật: mỗi chiêu 4 pha (chuẩn bị → tung → đỉnh → tan) + bảng tổng hợp
    2_VFX_Preview/     ảnh hiệu ứng ở scene preview (nhiều góc) + concept để so
    3_Model_.../       model nhiều góc (trước, sau, 2 bên, trên, 3/4) ở các frame khó nhất, có ảnh tô màu đỉnh lỗi
    4_SoDo/            số đo (JSON): audit bề mặt, test, hiệu năng
```

- `00_HienTrang_...` = **hiện trạng trước khi sửa, CHƯA ĐẠT** – để đối chiếu các lỗi đã báo.
- Đợt `01`, `02`, … = các lần Codex bàn giao đã qua review của Claude.
- Ảnh và JSON không đưa vào git (nặng); chỉ các README được commit.

| Tướng | Đợt | Kết luận | Điểm so với tài liệu |
|---|---|---|---|
| Mặc Ảnh | `MacAnh/00_HienTrang_20261020` | CHƯA ĐẠT | 47 % |
| Lữ Bố | `LuBu/00_HienTrang_20261020` | CHƯA ĐẠT | ~50 % |
