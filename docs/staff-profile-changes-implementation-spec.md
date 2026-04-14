# Staff – Thay đổi Hồ sơ Nhạy cảm (UC-139)
**Route:** `/staff/profile-changes`  
**Trạng thái hiện tại:** UI mock data — chưa kết nối API

---

## 1. Nghiệp vụ tổng quan

Khi một user đã **qua KYC và được duyệt** (trạng thái KYC = `Approved` theo từng role) cố thay đổi một **restricted field** trong profile của họ, hệ thống **không apply ngay** mà tạo một **pending change request** chờ Operation Staff xét duyệt.

Với các field thông thường (không phải restricted): vẫn apply thẳng khi user bấm Save, không cần qua queue.

> **Định nghĩa "đã qua KYC và được duyệt":** BE xác định theo trạng thái KYC của từng role — cụ thể là `workflowStatus = APPROVED` trong bảng KYC tương ứng (StartupKYC / AdvisorKYC / InvestorKYC), không phải `profileStatus` của bảng Profile.

---

## 2. Điều kiện kích hoạt (Trigger)

| Điều kiện | Hành vi |
|---|---|
| User có KYC `Approved` **+** sửa **non-restricted field** | Apply ngay, không tạo request |
| User có KYC `Approved` **+** sửa **restricted field** | Tạo pending change request, value cũ vẫn giữ cho đến khi approved |
| User chưa có KYC `Approved` | Không bao giờ tạo request — apply thẳng |

> **Lưu ý:** Điều kiện kiên quyết là KYC `Approved` — flow này **chỉ áp dụng cho verified profiles**. Cần BA xác nhận `workflowStatus = APPROVED` có phải là điều kiện đúng không.

---

## 3. Danh sách Restricted Fields (v1 — cần BA xác nhận)

| Role | Restricted fields |
|---|---|
| **Startup** (có pháp nhân) | Tên pháp nhân, Mã số doanh nghiệp, Người đại diện pháp luật |
| **Startup** (chưa có pháp nhân) | Tên project/startup, Tên Founder/Representative |
| **Advisor** | Số định danh (CCCD/Passport), Số tài khoản ngân hàng |
| **Investor** | Số CCCD/Passport |

---

## 4. Impact Level (Mức ảnh hưởng)

FE hiện có 3 mức, cần BE trả về theo enum:

| Enum | Label hiển thị | Ý nghĩa đề xuất |
|---|---|---|
| `CRITICAL` | Cực cao | Liên quan đến định danh (CCCD, Passport) |
| `HIGH` | Cao | Liên quan đến pháp nhân (tên công ty, MST) |
| `MEDIUM` | Trung bình | Thông tin tài chính (tài khoản ngân hàng) |

> Có thể do BE tự tính theo field changed, hoặc config sẵn trong whitelist restricted fields.

---

## 5. Trạng thái của Change Request

```
NEW → (staff bấm "Nhận xử lý") → UNDER_REVIEW → RESOLVED
                                              → REJECTED
```

| Status | Ý nghĩa |
|---|---|
| `NEW` | Vừa được tạo, chưa staff nào nhận |
| `UNDER_REVIEW` | Một staff đã nhận xử lý, đang review |
| `RESOLVED` | Staff đã approve, change đã được apply |
| `REJECTED` | Staff từ chối, value cũ được giữ nguyên |

**Chốt về `UNDER_REVIEW` trigger — dùng action "Nhận xử lý":**
- Staff mở detail page chỉ để **xem** — GET không tự đổi status
- Staff phải bấm nút **"Nhận xử lý"** → gọi `POST .../start-review` → status chuyển `UNDER_REVIEW`
- **Chỉ 1 staff được nhận tại một thời điểm:** nếu đã có staff nhận (`UNDER_REVIEW`), staff khác vẫn mở xem được nhưng nút "Nhận xử lý" bị disabled — FE hiển thị "Đang được xử lý bởi [reviewStartedBy]"
- Lý do chọn hướng này: GET không nên có side effect; dễ audit trail; tránh race condition multi-staff

**Guard conditions cho Approve/Reject:**
- Chỉ cho phép khi status là `NEW` hoặc `UNDER_REVIEW`
- Nếu status đã là `RESOLVED` hoặc `REJECTED` → BE trả `409 Conflict`, FE hiện "Yêu cầu này đã được xử lý"

---

## 6. Staff Actions

Theo SRS UC-139 và thống nhất với BA, staff chỉ có **2 action**:

1. **Approve (Duyệt)** — hệ thống apply giá trị mới, cập nhật profile
2. **Reject (Từ chối)** — giữ nguyên giá trị cũ, kèm lý do gửi về user

> **Bỏ "Yêu cầu bổ sung chứng cứ"** khỏi scope v1 — action này kéo theo status mới, notification mới, comment history, resubmit flow. Quá phức tạp cho scope hiện tại.  
> UI hiện tại đang có nút này — **cần xoá hoặc disable**.

---

## 7. Side effect khi Approve

> ⚠️ **Đây là đề xuất nghiệp vụ — chưa được BA xác nhận chính thức.** BE **không tự động implement** các side effect dưới đây cho đến khi BA chốt.

| Field được đổi | Side effect đề xuất |
|---|---|
| Số CCCD/Passport | Thu hồi nhãn VERIFIED, user phải làm lại KYC |
| Mã số doanh nghiệp / Tên pháp nhân | Xem xét revoke verified badge — **cần BA xác nhận** |
| Tài khoản ngân hàng (Advisor) | Không ảnh hưởng KYC status, chỉ ảnh hưởng payment routing |

---

## 8. API cần BE implement

### 8.1 Danh sách change requests
```
GET /api/staff/profile-changes
Query params:
  - status: NEW | UNDER_REVIEW | RESOLVED | REJECTED (optional)
  - role: Startup | Advisor | Investor (optional)
  - impactLevel: CRITICAL | HIGH | MEDIUM (optional)
  - page, pageSize
```

Response mỗi item:
```json
{
  "id": "PR-2001",
  "entityId": 123,
  "entityName": "Startup FinTech X",
  "entityType": "Startup",
  "fieldsChanged": ["Tên pháp nhân", "Người đại diện pháp luật"],
  "impactLevel": "HIGH",
  "status": "NEW",
  "createdAt": "2024-03-24T08:30:00Z"
}
```

### 8.2 Chi tiết 1 request
```
GET /api/staff/profile-changes/{id}
```

Response:
```json
{
  "id": "PR-2001",
  "entityId": 123,
  "entityName": "Startup FinTech X",
  "entityType": "Startup",
  "impactLevel": "HIGH",
  "status": "UNDER_REVIEW",
  "submitReason": "Lý do user cung cấp khi gửi request",
  "rejectReason": null,
  "staffNote": null,
  "reviewStartedBy": "Nguyễn Văn Staff",
  "reviewStartedAt": "2024-03-24T09:00:00Z",
  "diffs": [
    {
      "fieldKey": "legalRepresentative",
      "fieldLabel": "Người đại diện pháp luật",
      "before": "Nguyễn Văn A",
      "after": "Trần Thị B"
    },
    {
      "fieldKey": "businessCode",
      "fieldLabel": "Mã số doanh nghiệp",
      "before": "0123456789",
      "after": "0987654321"
    }
  ],
  "evidenceFiles": [
    { "fileName": "GiayDKKD_New.pdf", "url": "https://..." }
  ],
  "createdAt": "2024-03-24T08:30:00Z"
}
```

> **Rule chốt về 1 request vs nhiều field diffs:**  
> 1 lần Save = 1 change request duy nhất, chứa `diffs[]`. BE approve/reject toàn bộ — không approve từng field riêng lẻ. FE list page hiển thị field đầu tiên + `(+N)`.

> **3 trường reason tách biệt:**
> - `submitReason` — user nhập khi gửi request
> - `rejectReason` — staff nhập khi từ chối (null nếu chưa reject)
> - `staffNote` — ghi chú nội bộ staff khi approve (null nếu chưa approve)

> **Evidence files (v1):** optional — BE không enforce bắt buộc. Sau khi BA chốt per-field requirement thì mới thêm validation.

### 8.3 Nhận xử lý
```
POST /api/staff/profile-changes/{id}/start-review
Body: (none)
```
Effect: set `status = UNDER_REVIEW`, ghi `reviewStartedBy` (lấy từ token), `reviewStartedAt = now`.  
Guard: nếu status đã là `UNDER_REVIEW`, `RESOLVED`, hoặc `REJECTED` → trả `409 Conflict`.

### 8.4 Approve
```
POST /api/staff/profile-changes/{id}/approve
Body: { "staffNote": "Ghi chú nội bộ (optional)" }
```
Effect: apply tất cả `diffs[].after` vào profile, set `status = RESOLVED`, notify user.  
Guard: chỉ cho phép khi status là `NEW` hoặc `UNDER_REVIEW`.

### 8.5 Reject
```
POST /api/staff/profile-changes/{id}/reject
Body: { "rejectReason": "Lý do từ chối (bắt buộc)" }
```
Effect: giữ nguyên toàn bộ giá trị cũ, set `status = REJECTED`, notify user kèm `rejectReason`.  
Guard: chỉ cho phép khi status là `NEW` hoặc `UNDER_REVIEW`.

---

## 9. Việc FE cần làm sau khi BE xong

**Staff list page:**
- [ ] Thay mock data bằng `GET /api/staff/profile-changes`
- [ ] `fieldsChanged[]`: hiển thị tên field đầu tiên + `(+N)` nếu có nhiều hơn 1

**Staff detail page:**
- [ ] Thay mock data bằng `GET /api/staff/profile-changes/{id}`
- [ ] Hiển thị nút **"Nhận xử lý"** khi status = `NEW` → `POST .../start-review`
- [ ] Nếu status = `UNDER_REVIEW` và `reviewStartedBy` ≠ staff hiện tại → disable nút, hiện "Đang được xử lý bởi [reviewStartedBy]"
- [ ] Nếu status = `RESOLVED` hoặc `REJECTED` → ẩn toàn bộ action buttons, chỉ hiện read-only view
- [ ] Wire nút **Duyệt** → `POST .../approve` (`staffNote` optional)
- [ ] Wire nút **Từ chối** → `POST .../reject` (`rejectReason` **bắt buộc**, validate trước khi submit)
- [ ] Hiển thị `submitReason` (lý do user) và `staffNote`/`rejectReason` (phản hồi staff) ở 2 block riêng
- [ ] **Xoá nút "Yêu cầu bổ sung chứng cứ"** khỏi detail page

**User profile page (Startup/Advisor/Investor):**
- [ ] Khi BE trả `409 Conflict` do đang có pending request → hiện toast: *"Bạn đang có một yêu cầu thay đổi hồ sơ nhạy cảm chờ xét duyệt. Vui lòng chờ kết quả trước khi gửi thay đổi mới."*
- [ ] Nếu user Save cùng lúc non-restricted + restricted fields khi đang có pending:
  - Non-restricted: lưu bình thường
  - Restricted: không lưu → FE thông báo split: *"Đã lưu [X trường], nhưng [Y trường] chưa thể cập nhật vì đang có yêu cầu chờ duyệt."*

---

## 10. Rule block concurrent pending requests

**Mỗi profile chỉ có tối đa 1 pending change request tại một thời điểm.**

| Tình huống | Hành vi |
|---|---|
| Chưa có request pending | Tạo request mới bình thường |
| Đang có request `NEW` hoặc `UNDER_REVIEW` + user sửa restricted field | Block: không tạo request mới, restricted fields không lưu, trả `409` |
| Đang có request pending + user sửa cả restricted lẫn non-restricted | Non-restricted apply ngay; restricted bị block → FE thông báo split |
| Request đã `RESOLVED` hoặc `REJECTED` | User tạo request mới bình thường |

---

## 11. Điểm còn pending — cần BA xác nhận

1. **Danh sách restricted fields** — section 3 là đề xuất v1, chưa được BA chốt
2. **Side effect approve field pháp nhân** — section 7 là đề xuất, BE không implement cho đến khi BA xác nhận
3. **Evidence bắt buộc theo field nào** — v1 optional toàn bộ, enforce sau khi BA confirm
4. **Reject reason bắt buộc hay optional** — FE đang validate bắt buộc, cần BA xác nhận
