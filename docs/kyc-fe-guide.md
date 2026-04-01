# Hướng dẫn tích hợp luồng Xác thực KYC (Profile Approval) cho Frontend

Tài liệu này mô tả chi tiết luồng nghiệp vụ và các API cần gọi để thực hiện chức năng **Đăng ký hồ sơ (KYC)** cho người dùng (Startup, Advisor, Investor) và **Xét duyệt hồ sơ** dành cho Ban quản trị (Staff/Admin).

---

## 1. Trạng thái hồ sơ (Profile Status)
Frontend cần dựa vào trường `profileStatus` (hoặc chuỗi Enum tương ứng) được trả về từ API `/me` (lấy thông tin cá nhân) để hiển thị UI phù hợp.

Các trạng thái của `ProfileStatus`:
- `0` (hoặc `"Draft"`): Bản nháp. Người dùng đang cập nhật thông tin thành profile, chưa nộp. UI nên hiện nút **"Gửi yêu cầu xác duyệt"** (Submit for Approval).
- `1` (hoặc `"Pending"`): Cập nhật đang chờ duyệt. UI nên khoá form, chỉ hiện trạng thái **"Đang chờ duyệt"**.
- `2` (hoặc `"Approved"`): Đã duyệt. UI mở khóa toàn bộ tính năng của hệ thống.
- `3` (hoặc `"Rejected"`): Bị từ chối. UI hiện thông báo bị từ chối (kèm lý do nếu có) và cho phép người dùng sửa thông tin, sau đó nộp lại.

---

## 2. Luồng dành cho Người dùng (Users: Startup, Advisor, Investor)

**Điều kiện:**
Người dùng cần gọi API cập nhật hồ sơ (Update Profile) liên tục cho đến khi điền đủ các trường bắt buộc (ví dụ: upload Business Certificate đối với Startup). Sau khi hoàn tất, FE mới gọi API Submit.

### 2.1 API Nộp hồ sơ xét duyệt (Submit KYC)

- **Gọi khi:** Người dùng bấm nút "Gửi xác duyệt" trên UI.
- **Header:** Cần truyền `Authorization: Bearer {token}`.

**Startup:**
```http
POST /api/startups/submit-approval
```
*(Ghi chú: API này tự động lấy `userId` từ token)*

**Advisor:**
```http
POST /api/advisors/me/kyc/submit
```

**Investor:**
```http
POST /api/investors/me/kyc/submit
```

**Kết quả thành công (200 OK):**
Hệ thống sẽ chuyển trạng thái của User sang `Pending` và FE có thể redirect sang trang báo thành công hoặc render UI dạng trạng thái chờ duyệt.

---

## 3. Luồng dành cho Ban quản trị (Staff / Admin)

**Điều kiện:** Frontend Staff cần fetch danh sách hồ sơ có trạng thái `Pending` thông qua các API Get List pending registrations `GET /api/registration/pending/...` để hiển thị trên bảng Dashboard.

### 3.1 Phê duyệt hồ sơ (Approve)

- **Gọi khi:** Staff kiểm tra chi tiết thông tin, đối chiếu hình ảnh / giấy tờ hợp lệ và nhấn nút "Duyệt".
- **Path Param:** `{staffId}` là id của Staff đang thực hiện thao tác (Staff/Admin hiện tại).

**Startup:**
```http
POST /api/registration/approve/startups/{staffId}
```
*Body:*
```json
{
  "startupId": 12,
  "score": 8 // Điểm đánh giá (nếu có logic verify rank)
}
```

**Advisor:**
```http
POST /api/registration/approve/advisors/{staffId}
```
*Body:*
```json
{
  "advisorId": 5
}
```

**Investor:**
```http
POST /api/registration/approve/investors/{staffId}
```
*Body:*
```json
{
  "investorId": 8
}
```

### 3.2 Từ chối hồ sơ (Reject)

- **Gọi khi:** Staff phát hiện giấy tờ mờ, thiếu thông tin, hoặc không hợp pháp. Staff nhấn "Từ chối" và nhập lý do.
- **Path Param:** `{staffId}` là id của Staff.

**Startup:**
```http
POST /api/registration/reject/startups/{staffId}
```

**Advisor:**
```http
POST /api/registration/reject/advisors/{staffId}
```

**Investor:**
```http
POST /api/registration/reject/investors/{staffId}
```

*Request Body (Chung cho các role khi bị Reject):*
```json
{
  "id": 12, // ID của Startup, Advisor hoặc Investor bị từ chối
  "reason": "Ảnh chứng minh thư bị mờ, vui lòng chụp lại."
}
```

---

## 4. Xử lý Lỗi thường gặp (Error Handling)

Khi gọi API Submit hoặc Approve/Reject, FE cần bắt các mã lỗi (nằm trong khối `error` của Response envelope) như:
- `ALREADY_PENDING`: "Hồ sơ của bạn đã được gửi trước đó và đang chờ duyệt."
- `ALREADY_APPROVED`: "Hồ sơ này đã được duyệt rồi."
- `STARTUP_PROFILE_NOT_FOUND` / `ADVISOR_PROFILE_NOT_FOUND`: "Không tìm thấy hồ sơ cá nhân, yêu cầu user tạo hồ sơ nháp trước."

Mọi phản hồi từ backend sẽ theo cấu trúc chuẩn Envelope:
```json
{
  "success": false,
  "error": {
    "code": "ALREADY_PENDING",
    "message": "Profile is already pending approval."
  }
}
```