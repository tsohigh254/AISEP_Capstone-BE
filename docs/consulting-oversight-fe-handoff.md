# Consulting Oversight — FE Integration Guide

> **Ngày:** 2026-04-15 | **Branch BE:** `feature/mentorship-cancel` | **Status:** BE done, chờ FE  
> **Spec chi tiết:** `docs/consulting-oversight-implementation.md` (v2.1)

---

## ⚡ Breaking Changes (xử lý trước)

### 1. `sessionId` bắt buộc khi tạo Report

```
POST /api/mentorships/{id}/reports
```

**Trước đây:** `sessionId` optional → **Bây giờ:** required.  
FE phải truyền `sessionId` trong body. Nếu thiếu → `400 SESSION_ID_REQUIRED`.

```json
{
  "sessionId": 12,        // ← BẮT BUỘC
  "reportSummary": "...",
  "detailedFindings": "...",
  "recommendations": "..."
}
```

### 2. Response DTO thay đổi ở các API cũ

**`ReportDto`** (trong GET mentorship detail, GET report) — thêm 3 fields:
```json
{
  "...existing fields...",
  "reviewStatus": "PendingReview",   // NEW: PendingReview | Passed | Failed | NeedsMoreInfo
  "staffReviewNote": null,           // NEW: null cho Startup, có giá trị cho Advisor/Staff
  "reviewedAt": null                 // NEW
}
```

**`SessionDto`** — thêm 5 fields:
```json
{
  "...existing fields...",
  "startupConfirmedConductedAt": null,  // NEW
  "disputeReason": null,                // NEW
  "resolutionNote": null,               // NEW
  "markedByStaffID": null,              // NEW
  "markedAt": null                      // NEW
}
```

**`GET /api/mentorships/{id}` (Detail)** — Startup chỉ thấy reports đã `Passed`.

### 3. `PUT /api/mentorships/{id}/complete` vẫn tồn tại nhưng luôn trả 400

```json
{
  "success": false,
  "code": "COMPLETION_BY_ADVISOR_DISABLED",
  "message": "Mentorship completion is now handled by Operations Staff..."
}
```

FE nên ẩn nút "Complete" cho Advisor. Mentorship completion giờ tự động aggregate từ sessions.

### 4. Session có 3 trạng thái mới

| Status | Ý nghĩa |
|--------|---------|
| `Conducted` | Startup đã xác nhận session diễn ra |
| `InDispute` | Staff mở tranh chấp |
| `Resolved` | Staff giải quyết tranh chấp (nhưng ≠ Completed) |

---

## 🆕 6 API Endpoints mới

### API 1: Staff — Danh sách reports chờ review

```
GET /api/mentorships/oversight/reports
Auth: StaffOrAdmin
```

| Query Param | Type | Default | Mô tả |
|-------------|------|---------|--------|
| `reviewStatus` | string | `PendingReview` | `PendingReview` / `Passed` / `Failed` / `NeedsMoreInfo` / `all` |
| `advisorId` | int? | — | Lọc theo advisor |
| `startupId` | int? | — | Lọc theo startup |
| `from` | DateTime? | — | ISO 8601 |
| `to` | DateTime? | — | ISO 8601 |
| `page` | int | 1 | |
| `pageSize` | int | 20 | Max 100 |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "reportID": 1,
        "mentorshipID": 5,
        "sessionID": 12,
        "advisorID": 3,
        "advisorName": "Dr. Nguyen Van A",
        "startupID": 7,
        "startupName": "TechVN",
        "reportSummary": "Great progress on GTM strategy...",
        "detailedFindings": "...",
        "recommendations": "...",
        "attachmentsURL": "https://...",
        "submittedAt": "2026-04-12T10:00:00Z",
        "reviewStatus": "PendingReview",
        "reviewedByStaffID": null,
        "staffReviewNote": null,
        "reviewedAt": null,
        "supersededByReportID": null,
        "isLatestForSession": true,
        "sessionStatus": "Conducted",
        "startupConfirmedConductedAt": "2026-04-11T09:00:00Z",
        "mentorshipStatus": "InProgress",
        "challengeDescription": "Go-to-market strategy for B2B SaaS"
      }
    ],
    "paging": { "page": 1, "pageSize": 20, "totalItems": 5 }
  }
}
```

---

### API 2: Staff — Review report

```
PUT /api/mentorships/reports/{reportId}/review
Auth: StaffOrAdmin
```

**Request:**
```json
{
  "reviewStatus": "Passed",       // "Passed" | "Failed" | "NeedsMoreInfo"
  "note": "Report đầy đủ."       // optional, max 2000 chars
}
```

**Response 200:**
```json
{
  "success": true,
  "data": {
    "reportID": 1,
    "mentorshipID": 5,
    "reviewStatus": "Passed",
    "staffReviewNote": "Report đầy đủ.",
    "reviewedByStaffID": 99,
    "reviewedAt": "2026-04-14T08:30:00Z"
  },
  "message": "Report reviewed successfully."
}
```

---

### API 3: Startup — Xác nhận đã tư vấn

```
POST /api/mentorships/{mentorshipId}/sessions/{sessionId}/confirm-conducted
Auth: StartupOnly
```

**Request:** Body trống (`{}` hoặc không gửi body)

**Preconditions:** Session = `Scheduled` hoặc `InProgress`

**Response 200:**
```json
{
  "success": true,
  "data": {
    "sessionID": 12,
    "sessionStatus": "Conducted",
    "startupConfirmedConductedAt": "2026-04-14T08:00:00Z",
    "...other SessionDto fields..."
  },
  "message": "Session confirmed as conducted."
}
```

---

### API 4: Staff — Mark session completed

```
PUT /api/mentorships/{mentorshipId}/sessions/{sessionId}/mark-completed
Auth: StaffOrAdmin
```

**Request:**
```json
{
  "note": "Session OK, reports passed."   // optional
}
```

**Preconditions:**
- Session phải `Conducted`
- Tất cả reports phải `Passed`
- Có ít nhất 1 report

**Response 200:**
```json
{
  "success": true,
  "data": {
    "sessionID": 12,
    "sessionStatus": "Completed",
    "mentorshipID": 5,
    "mentorshipStatus": "InProgress",
    "isPayoutEligible": false,
    "markedByStaffID": 99,
    "markedAt": "2026-04-14T09:00:00Z",
    "disputeReason": null,
    "resolutionNote": null
  },
  "message": "Session marked as completed."
}
```

---

### API 5: Staff — Mark session dispute

```
PUT /api/mentorships/{mentorshipId}/sessions/{sessionId}/mark-dispute
Auth: StaffOrAdmin
```

**Request:**
```json
{
  "reason": "Startup reported advisor did not show up."   // required, max 2000
}
```

**Response 200:**
```json
{
  "success": true,
  "data": {
    "sessionID": 12,
    "sessionStatus": "InDispute",
    "disputeReason": "Startup reported advisor did not show up.",
    "mentorshipID": 5,
    "mentorshipStatus": "InDispute",
    "isPayoutEligible": false,
    "markedByStaffID": 99,
    "markedAt": "...",
    "resolutionNote": null
  },
  "message": "Session marked as in dispute."
}
```

---

### API 6: Staff — Resolve dispute

```
PUT /api/mentorships/{mentorshipId}/sessions/{sessionId}/mark-resolved
Auth: StaffOrAdmin
```

**Request:**
```json
{
  "resolution": "Confirmed advisor was present via meeting logs.",   // required, max 2000
  "restoreCompleted": true    // true → Completed, false → Resolved (blocks payout)
}
```

**Response 200:**
```json
{
  "success": true,
  "data": {
    "sessionID": 12,
    "sessionStatus": "Completed",
    "resolutionNote": "Confirmed advisor was present via meeting logs.",
    "mentorshipID": 5,
    "mentorshipStatus": "InProgress",
    "isPayoutEligible": false,
    "markedByStaffID": 99,
    "markedAt": "...",
    "disputeReason": "Startup reported advisor did not show up."
  },
  "message": "Dispute resolved."
}
```

---

## 🔴 Error Codes FE cần handle

| Code | HTTP | Khi nào | FE Action |
|------|------|---------|-----------|
| `REPORT_NOT_AVAILABLE` | **403** | Startup xem report chưa Passed | Hiện placeholder "Báo cáo chưa sẵn sàng" (403 = tồn tại nhưng chưa được phép xem, khác 400 validation) |
| `INVALID_REVIEW_STATUS` | 400 | Staff gửi status sai | Hiện validation error |
| `REPORTS_NOT_ALL_PASSED` | 400 | Staff mark completed khi reports chưa Passed hết | Toast warning |
| `NO_REPORT` | 400 | Staff mark completed khi chưa có report | Toast warning |
| `SESSION_NOT_CONDUCTED` | 400 | Staff mark completed khi startup chưa confirm | Toast: "Startup chưa xác nhận" |
| `INVALID_STATUS_TRANSITION` | 400 | Transition không hợp lệ | Toast error |
| `SESSION_NOT_FOUND` | **404** | Session không tồn tại hoặc không thuộc mentorship | Redirect về list page |
| `COMPLETION_BY_ADVISOR_DISABLED` | 400 | Advisor gọi complete | Ẩn nút, hiện thông báo |
| `SESSION_ID_REQUIRED` | 400 | Create report thiếu sessionId | Validation error trên form |

---

## 📋 FE Tasks Checklist

| # | Task | Role affected | Priority |
|---|------|--------------|----------|
| FE-1 | Page `/staff/consulting-oversight` — report queue với tabs/filter | Staff | High |
| FE-2 | Page `/staff/consulting-oversight/reports/{id}` — review detail | Staff | High |
| FE-3 | Staff Session Actions panel (mark completed/dispute/resolved) | Staff | High |
| FE-4 | Nút "Xác nhận đã tư vấn" cho Startup khi session Scheduled/InProgress | Startup | High |
| FE-5 | Report status badge (PendingReview/Passed/Failed/NeedsMoreInfo) | Advisor | Medium |
| FE-6 | Handle 403 report not available → placeholder cho Startup | Startup | High |
| FE-7 | Sidebar thêm "Consulting Oversight" cho Staff | Staff | Medium |
| FE-8 | `staffReviewNote` chỉ hiện cho Advisor/Staff, ẩn cho Startup | All | Medium |
| FE-9 | Error handling cho staff actions (xem bảng trên) | Staff | Medium |
| FE-10 | Badge "Đã thay thế" cho superseded reports | Advisor/Staff | Low |
| FE-11 | Hiện `disputeReason` + `resolutionNote` trên Session Detail | All | Medium |
| FE-12 | Ẩn nút Complete cho Advisor, hiện aggregate status | Advisor | Medium |
| FE-13 | **Breaking:** Form create report phải gửi `sessionId` | Advisor | **Critical** |

---

## 🔄 Luồng nghiệp vụ tóm tắt

```
Advisor tạo session → Startup confirm slot → Session = Scheduled
        ↓
    Session diễn ra
        ↓
Startup click "Xác nhận đã tư vấn" → Session = Conducted
        ↓
Advisor nộp Report (bắt buộc có sessionId) → Report = PendingReview
        ↓
Staff review report → Passed / Failed / NeedsMoreInfo
    ├─ NeedsMoreInfo → Advisor nộp report mới (auto-supersede cũ) → quay lại review
    ├─ Failed → Report bị reject. Advisor KHÔNG thể supersede (phải tạo report mới thủ công cho session)
    │          ⚠️ BE chỉ auto-supersede NeedsMoreInfo, KHÔNG auto-supersede Failed
    └─ Passed → Startup có thể xem report
        ↓
Staff Mark Session Completed (cần: Conducted + all reports Passed)
        ↓
Khi TẤT CẢ sessions Completed → Mentorship auto = Completed, IsPayoutEligible = true
```

**Dispute flow:**
```
Staff Mark Dispute (từ bất kỳ status nào trừ Cancelled) → Session = InDispute
        ↓
Staff Resolve → restoreCompleted=true → Completed (cho payout)
             → restoreCompleted=false → Resolved (blocks payout)
```
