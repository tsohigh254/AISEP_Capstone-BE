# Consulting Oversight — Implementation Spec (v2.1)

> **Ngày tạo:** 2026-04-14 | **Cập nhật:** 2026-04-14 (BA chốt nghiệp vụ v1)  
> **Mục đích:** Staff/Admin giám sát report advisor, quản lý dispute ở **session-level**, gate payout + visibility cho startup.  
> **Thay đổi chính v2:** Session-level instead of mentorship-level; Startup confirm conducted; Chỉ Staff marks completed.  
> **v2.1:** Chốt 7 điểm nghiệp vụ — single confirm, SessionID required, route consistency, supersede logic, Resolved intentional, payment deferred.

---

## Mục lục

0. [Chốt nghiệp vụ v1 (BA decisions)](#0-chốt-nghiệp-vụ-v1-ba-decisions)
1. [Tổng quan luồng nghiệp vụ](#1-tổng-quan-luồng-nghiệp-vụ)
2. [Trạng thái cần track](#2-trạng-thái-cần-track)
3. [Thay đổi DB — Entity & Migration](#3-thay-đổi-db--entity--migration)
4. [API Endpoints mới](#4-api-endpoints-mới)
5. [API Contracts chi tiết](#5-api-contracts-chi-tiết)
6. [BE Implementation — Service layer](#6-be-implementation--service-layer)
7. [Thay đổi logic hiện tại](#7-thay-đổi-logic-hiện-tại)
8. [FE Implementation Guide](#8-fe-implementation-guide)
9. [Business Rules](#9-business-rules)
10. [Message Codes](#10-message-codes)
11. [Checklist triển khai](#11-checklist-triển-khai)
12. [Notification Events](#12-notification-events)
13. [FE Error Handling Spec](#13-fe-error-handling-spec)

---

## 0. Chốt nghiệp vụ v1 (BA decisions)

> Ngày chốt: 2026-04-14. Áp dụng cho toàn bộ sprint này.

| # | Vấn đề | Quyết định | Ghi chú |
|---|--------|-----------|--------|
| 1 | Dual confirm hay single confirm? | **Single confirm.** Chỉ Startup confirm conducted là đủ. | `AdvisorConfirmedConductedAt`, `ConductedConfirmedAt` giữ trong DB — reserved for future enhancement, v1 chưa dùng. |
| 2 | Report có được phép không gắn Session? | **KHÔNG.** `SessionID` bắt buộc cho report mới. | Legacy reports (`SessionID = null`) không tham gia payout gate / session completion gate. `CreateReportAsync` validator phải require SessionID. Breaking change cho FE. |
| 3 | Thêm Conducted ảnh hưởng code cũ? | **Có.** Phải audit tất cả status guard. | Mọi chỗ check `Scheduled`/`InProgress`/`Completed` phải rà xem có cần thêm `Conducted`. Task bắt buộc. |
| 4 | Payment trigger sau IsPayoutEligible = true? | **Deferred.** Sprint này chỉ set flag. | Không làm payment release, payout execution, disbursement service. `IsPayoutEligible = true` = đủ điều kiện nghiệp vụ, release thật là phase sau. |
| 5 | SupersededByReportID ai set? | **System-managed** trong `CreateReportAsync`. | Khi advisor tạo report mới cho cùng session → tìm report cũ `NeedsMoreInfo` + `SupersededByReportID == null` → save new → set old.SupersededByReportID = newID. Cần 2 SaveChanges trong transaction. |
| 6 | Resolved mà mentorship không Completed? | **Chủ đích, không phải bug.** | `Resolved ≠ Completed`. Session Resolved không tính completed → mentorship không aggregate Completed → `IsPayoutEligible = false`. Muốn payout → staff phải `restoreCompleted = true`. |
| 7 | Route inconsistency | **Thống nhất dưới `/api/mentorships/...`** | Bỏ `/api/mentorship-sessions/...`. Routes 4-6 đổi thành `/api/mentorships/{mentorshipId}/sessions/{sessionId}/mark-*`. Service validate session thuộc mentorship (chống IDOR). |

---

## 1. Tổng quan luồng nghiệp vụ

### Luồng thuận (happy path)

```
┌─────────────────────────────────────────────────────────────────────┐
│                     TRƯỚC KHI STAFF VÀO                            │
│                                                                     │
│  Startup tạo mentorship request                                     │
│         ↓                                                           │
│  Advisor accept → tạo session → session diễn ra                    │
│         ↓                                                           │
│  ⭐ Startup confirm conducted (bước mới — xác nhận đã tư vấn)     │
│         ↓                                                           │
│  Advisor nộp report (POST /api/mentorships/{id}/reports)           │
│         ↓                                                           │
│  Report.ReportReviewStatus = PendingReview (auto)                  │
│                                                                     │
├─────────────────────────────────────────────────────────────────────┤
│                     STAFF VÀO TỪ ĐÂY                              │
│                                                                     │
│  Staff mở Consulting Oversight Dashboard                            │
│         ↓                                                           │
│  Xem queue reports PendingReview                                    │
│         ↓                                                           │
│  Mở report detail — xem summary, findings, recommendations,        │
│  attachments, session info, startup/advisor info                    │
│         ↓                                                           │
│  Staff chọn outcome:                                                │
│    ✅ Passed → startup được xem report, mở đường payout            │
│    ❌ Failed → report KHÔNG được xem, không payout                  │
│    🔄 NeedsMoreInfo → advisor cần bổ sung                          │
│         ↓                                                           │
│  (Nếu report Passed + startup đã confirm conducted)                │
│  Staff bấm "Mark Session Completed" (SESSION-LEVEL)                │
│         ↓                                                           │
│  SessionStatus = Completed                                          │
│  Mentorship auto-aggregate: nếu TẤT CẢ sessions completed         │
│  → MentorshipStatus = Completed, IsPayoutEligible = true           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Luồng dispute

```
┌─────────────────────────────────────────────────────────────────────┐
│  Issue/complaint phát sinh cho 1 SESSION cụ thể                    │
│         ↓                                                           │
│  Staff bấm "Mark Session InDispute"                                │
│         ↓                                                           │
│  SessionStatus = InDispute                                          │
│  Mentorship auto-aggregate → MentorshipStatus = InDispute           │
│  IsPayoutEligible = false                                           │
│         ↓                                                           │
│  Staff điều tra, ghi resolution note                               │
│         ↓                                                           │
│  Staff bấm "Mark Session Resolved"                                 │
│         ↓                                                           │
│  SessionStatus = Resolved hoặc Completed (tùy outcome)             │
│  Mentorship auto-aggregate → recalculate status                    │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. Trạng thái cần track

### 2.1 Session Status (mở rộng — cốt lõi v2)

Hiện có: `ProposedByStartup`, `ProposedByAdvisor`, `Scheduled`, `InProgress`, `Completed`, `Cancelled`

**Thêm mới:**

```
SessionStatusValues mới:
  "Conducted"   — Startup đã confirm buổi tư vấn diễn ra
  "InDispute"   — Staff mark dispute trên session này
  "Resolved"    — Staff resolve dispute (không restore completed)
```

**Session lifecycle đầy đủ:**

```
ProposedByStartup/ProposedByAdvisor → Scheduled → InProgress → Conducted → Completed
                                                             ↘ InDispute → Resolved
                                         ↘ Cancelled
```

> **"Conducted"** là trạng thái mới: startup xác nhận session đã diễn ra thực tế. Đây là prerequisite để staff mark completed.

### 2.2 Mentorship Status — trở thành AGGREGATE

Mentorship không còn được set trực tiếp bởi staff. Thay vào đó, hệ thống **tự động tính** từ sessions:

| Điều kiện | MentorshipStatus tự động |
|---|---|
| Có ít nhất 1 session `InDispute` | `InDispute` |
| TẤT CẢ sessions scheduled+ đều `Completed` | `Completed` |
| Mix sessions (vẫn InProgress/Conducted) | Giữ `InProgress` |
| TẤT CẢ sessions `Cancelled` | Giữ `Cancelled` |

> Rule tính trong helper method `RecalculateMentorshipStatus()`, gọi sau mỗi session status change.

### 2.3 Report Review Status (giữ nguyên từ v1)

```csharp
public enum ReportReviewStatus : short
{
    PendingReview = 0,
    Passed        = 1,
    Failed        = 2,
    NeedsMoreInfo = 3
}
```

### 2.4 Payout Eligibility

> **BA chốt #4:** Sprint này chỉ set flag `IsPayoutEligible`. Không làm payout execution.

| Condition | IsPayoutEligible |
|---|---|
| MentorshipStatus (aggregate) = Completed | ✅ Kiểm tra tiếp |
| + Startup đã confirm conducted cho TẤT CẢ sessions | ✅ Kiểm tra tiếp |
| + TẤT CẢ reports hiện hành đều `Passed` | ✅ Kiểm tra tiếp |
| + KHÔNG có session nào `InDispute` | ✅ → `true` |
| Bất kỳ điều kiện nào KHÔNG đáp ứng | `false` |

---

## 3. Thay đổi DB — Entity & Migration

### 3.1 Thay đổi Entity `MentorshipReport`

**File:** `src/AISEP.Domain/Entities/MentorshipReport.cs`

```diff
  public bool IsMandatory { get; set; }
- public bool ReviewedByStaff { get; set; }
+ public ReportReviewStatus ReportReviewStatus { get; set; } = ReportReviewStatus.PendingReview;
+ public int? ReviewedByStaffID { get; set; }
+ public string? StaffReviewNote { get; set; }
+ public DateTime? ReviewedAt { get; set; }
+ public int? SupersededByReportID { get; set; }
  public DateTime CreatedAt { get; set; }
```

### 3.2 Thay đổi Entity `MentorshipSession`

**File:** `src/AISEP.Domain/Entities/MentorshipSession.cs`

```diff
  public string? StartupNotes { get; set; }
+ public string? DisputeReason { get; set; }      // Staff ghi khi mark dispute
+ public string? ResolutionNote { get; set; }      // Staff ghi khi resolve
+ public int? MarkedByStaffID { get; set; }        // Staff nào mark completed/dispute/resolved
+ public DateTime? MarkedAt { get; set; }          // Thời điểm staff mark
  public DateTime CreatedAt { get; set; }
```

> **BA feedback #1 + #2:** Dispute/resolution lưu ở SESSION level, không phải mentorship.

> **BA chốt #1:** `AdvisorConfirmedConductedAt` + `ConductedConfirmedAt` đã có sẵn trong entity — **giữ nguyên, v1 chưa dùng**. Thêm comment `// Reserved for future dual-confirm enhancement`.

### 3.3 Thay đổi Entity `StartupAdvisorMentorship`

**File:** `src/AISEP.Domain/Entities/StartupAdvisorMentorship.cs`

```diff
  public bool CompletionConfirmedByAdvisor { get; set; }
+ public bool IsPayoutEligible { get; set; }

  // ===== PAYMENT FIELDS =====
```

> **Bỏ:** `DisputeReason` + `ResolutionNote` từ mentorship (đã chuyển xuống session).

### 3.4 Thêm Enum `ReportReviewStatus`

**File:** `src/AISEP.Domain/Enums/Enums.cs`

```csharp
// ───────────────────── Report Review (Staff Oversight) ──────────────

public enum ReportReviewStatus : short
{
    PendingReview = 0,
    Passed        = 1,
    Failed        = 2,
    NeedsMoreInfo = 3
}
```

### 3.5 Thêm Session Status Values

**File:** `src/AISEP.Application/Const/SessionStatusValues.cs`

```diff
  public const string Cancelled = "Cancelled";

+ /// <summary>Startup đã xác nhận buổi tư vấn diễn ra. Prerequisite cho staff mark completed.</summary>
+ public const string Conducted = "Conducted";

+ /// <summary>Staff đánh dấu session đang tranh chấp.</summary>
+ public const string InDispute = "InDispute";

+ /// <summary>Staff đã giải quyết tranh chấp (không restore completed).</summary>
+ public const string Resolved = "Resolved";

  public static readonly IReadOnlySet<string> All = new HashSet<string>
  {
      ProposedByStartup,
      ProposedByAdvisor,
      Scheduled,
      InProgress,
      Completed,
-     Cancelled
+     Cancelled,
+     Conducted,
+     InDispute,
+     Resolved
  };
```

### 3.6 DbContext Registration

**File:** `src/AISEP.Infrastructure/Data/ApplicationDbContext.cs`

```csharp
modelBuilder.Entity<MentorshipReport>()
    .Property(e => e.ReportReviewStatus)
    .HasConversion<short>()
    .HasDefaultValue(ReportReviewStatus.PendingReview);
```

### 3.7 Migration Command

```bash
cd src/AISEP.Infrastructure
dotnet ef migrations add AddConsultingOversight \
    --startup-project ../AISEP.WebAPI \
    -- --environment Development
dotnet ef database update --startup-project ../AISEP.WebAPI
```

**Lưu ý:** Migration phải convert `ReviewedByStaff = true → ReportReviewStatus.Passed` cho data cũ (nếu có).

---

## 4. API Endpoints mới

### 4.1 Tổng quan

| # | Method | Route | Auth | Mô tả |
|---|--------|-------|------|--------|
| 1 | `GET` | `/api/mentorships/oversight/reports` | StaffOrAdmin | Queue danh sách reports cần review |
| 2 | `PUT` | `/api/mentorships/reports/{reportId}/review` | StaffOrAdmin | Staff review report |
| 3 | `POST` | `/api/mentorships/{mentorshipId}/sessions/{sessionId}/confirm-conducted` | StartupOnly | Startup xác nhận session đã diễn ra |
| 4 | `PUT` | `/api/mentorships/{mentorshipId}/sessions/{sessionId}/mark-completed` | StaffOrAdmin | Staff mark session Completed |
| 5 | `PUT` | `/api/mentorships/{mentorshipId}/sessions/{sessionId}/mark-dispute` | StaffOrAdmin | Staff mark session InDispute |
| 6 | `PUT` | `/api/mentorships/{mentorshipId}/sessions/{sessionId}/mark-resolved` | StaffOrAdmin | Staff mark session Resolved |

> **BA chốt #7:** Tất cả routes thống nhất dưới `/api/mentorships/...`. Bỏ `/api/mentorship-sessions/...`.  
> **BA chốt:** Service validate `session.MentorshipID == mentorshipId` (chống IDOR).  
> Report review (1-2) giữ trong `MentorshipsController`. Session actions (3-6) cũng trong `MentorshipsController` (mở rộng).

---

## 5. API Contracts chi tiết

### 5.1 GET /api/mentorships/oversight/reports

**Query Params:**

| Param | Type | Required | Default | Mô tả |
|-------|------|----------|---------|--------|
| `reviewStatus` | string | No | `PendingReview` | `PendingReview` / `Passed` / `Failed` / `NeedsMoreInfo` / `all` |
| `advisorId` | int | No | — | Lọc theo advisor |
| `startupId` | int | No | — | Lọc theo startup |
| `from` | DateTime | No | — | Lọc report nộp từ ngày (ISO 8601) |
| `to` | DateTime | No | — | Lọc report nộp đến ngày (ISO 8601) |
| `page` | int | No | 1 | |
| `pageSize` | int | No | 20 | Max 100 |

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

### 5.2 PUT /api/mentorships/reports/{reportId}/review

**Request Body:**

```json
{
  "reviewStatus": "Passed",
  "note": "Report đầy đủ, approve."
}
```

**Validation:**
- `reviewStatus` PHẢI là `Passed`, `Failed`, hoặc `NeedsMoreInfo`
- `note` max 2000 chars

**Response 200:**

```json
{
  "success": true,
  "data": {
    "reportID": 1,
    "mentorshipID": 5,
    "reviewStatus": "Passed",
    "staffReviewNote": "Report đầy đủ, approve.",
    "reviewedByStaffID": 99,
    "reviewedAt": "2026-04-14T08:30:00Z"
  },
  "message": "Report reviewed successfully."
}
```

### 5.3 POST /api/mentorships/{mentorshipId}/sessions/{sessionId}/confirm-conducted

> **BA feedback #2:** Startup xác nhận session đã diễn ra thực tế.

**Request Body:** (trống — action thuần)

**Preconditions:**
- Caller phải là Startup owner của mentorship
- SessionStatus PHẢI là `Scheduled` hoặc `InProgress`
- Session phải thuộc mentorship này

**Response 200:**

```json
{
  "success": true,
  "data": {
    "sessionID": 12,
    "sessionStatus": "Conducted",
    "startupConfirmedConductedAt": "2026-04-14T08:00:00Z"
  },
  "message": "Session confirmed as conducted."
}
```

### 5.4 PUT /api/mentorships/{mentorshipId}/sessions/{sessionId}/mark-completed

**Request Body:**

```json
{
  "note": "Session OK, reports passed."
}
```

**Preconditions:**
- SessionStatus PHẢI là `Conducted` (startup đã confirm)
- TẤT CẢ reports hiện hành (SupersededByReportID == null) gắn với session này phải `Passed`
- Phải có ít nhất 1 report cho session này

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
    "markedAt": "2026-04-14T09:00:00Z"
  },
  "message": "Session marked as completed. (MSG144)"
}
```

> `mentorshipStatus` + `isPayoutEligible` được tính lại tự động bởi `RecalculateMentorshipStatus()`.

### 5.5 PUT /api/mentorships/{mentorshipId}/sessions/{sessionId}/mark-dispute

**Request Body:**

```json
{
  "reason": "Startup reported advisor did not show up."
}
```

**Validation:** `reason` required, max 2000 chars.

**Preconditions:**
- SessionStatus PHẢI là `Scheduled`, `InProgress`, `Conducted`, `Completed`, hoặc `Resolved` (re-open)

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
    "isPayoutEligible": false
  },
  "message": "Session marked as in dispute. (MSG145)"
}
```

### 5.6 PUT /api/mentorships/{mentorshipId}/sessions/{sessionId}/mark-resolved

**Request Body:**

```json
{
  "resolution": "Confirmed advisor was present via meeting logs.",
  "restoreCompleted": true
}
```

**Validation:** `resolution` required, max 2000 chars.

**Preconditions:**
- SessionStatus PHẢI là `InDispute`

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
    "isPayoutEligible": false
  },
  "message": "Dispute resolved. (MSG146)"
}
```

> Nếu `restoreCompleted = true` → SessionStatus = `Completed`  
> Nếu `restoreCompleted = false` → SessionStatus = `Resolved` (session kẹt, nhưng staff có thể re-open via mark-dispute)

---

## 6. BE Implementation — Service layer

### 6.1 Thêm methods vào `IMentorshipService`

```csharp
// Staff oversight — Report review
Task<ApiResponse<PagedResponse<ReportOversightDto>>> GetReportsForOversightAsync(
    string? reviewStatus, int? advisorId, int? startupId,
    DateTime? from, DateTime? to, int page, int pageSize);

Task<ApiResponse<ReportReviewResultDto>> ReviewReportAsync(
    int staffUserId, int reportId, ReviewReportRequest request);

// Startup — Confirm conducted
Task<ApiResponse<SessionDto>> ConfirmConductedAsync(
    int userId, int mentorshipId, int sessionId);

// Staff oversight — Session actions (mentorshipId for IDOR check)
Task<ApiResponse<SessionOversightResultDto>> MarkSessionCompletedAsync(
    int staffUserId, int mentorshipId, int sessionId, string? note);

Task<ApiResponse<SessionOversightResultDto>> MarkSessionDisputeAsync(
    int staffUserId, int mentorshipId, int sessionId, string reason);

Task<ApiResponse<SessionOversightResultDto>> MarkSessionResolvedAsync(
    int staffUserId, int mentorshipId, int sessionId, ResolveDisputeRequest request);
```

### 6.2 DTO mới cần tạo

**File:** `src/AISEP.Application/DTOs/Mentorship/MentorshipDTOs.cs` (append)

```csharp
// ============================= STAFF OVERSIGHT =============================

/// <summary>Staff review report request.</summary>
public class ReviewReportRequest
{
    public string ReviewStatus { get; set; } = null!;  // "Passed" | "Failed" | "NeedsMoreInfo"
    public string? Note { get; set; }
}

/// <summary>Resolve dispute request (session-level).</summary>
public class ResolveDisputeRequest
{
    public string Resolution { get; set; } = null!;
    public bool RestoreCompleted { get; set; }
}

/// <summary>Staff mark session completed request.</summary>
public class StaffSessionNoteRequest
{
    public string? Note { get; set; }
}

/// <summary>Staff mark session dispute request.</summary>
public class StaffMarkDisputeRequest
{
    public string Reason { get; set; } = null!;
}

/// <summary>Report item in staff oversight queue.</summary>
public class ReportOversightDto
{
    public int ReportID { get; set; }
    public int MentorshipID { get; set; }
    public int? SessionID { get; set; }
    public int? AdvisorID { get; set; }
    public string? AdvisorName { get; set; }
    public int? StartupID { get; set; }
    public string? StartupName { get; set; }
    public string? ReportSummary { get; set; }
    public string? DetailedFindings { get; set; }
    public string? Recommendations { get; set; }
    public string? AttachmentsURL { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public int? ReviewedByStaffID { get; set; }
    public string? StaffReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? SupersededByReportID { get; set; }
    public bool IsLatestForSession { get; set; }
    public string? SessionStatus { get; set; }
    public DateTime? StartupConfirmedConductedAt { get; set; }
    public string? MentorshipStatus { get; set; }
    public string? ChallengeDescription { get; set; }
}

/// <summary>Result after staff reviews report.</summary>
public class ReportReviewResultDto
{
    public int ReportID { get; set; }
    public int MentorshipID { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public string? StaffReviewNote { get; set; }
    public int? ReviewedByStaffID { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>Result after staff marks session completed/dispute/resolved.</summary>
public class SessionOversightResultDto
{
    public int SessionID { get; set; }
    public string SessionStatus { get; set; } = string.Empty;
    public string? DisputeReason { get; set; }
    public string? ResolutionNote { get; set; }
    public int MentorshipID { get; set; }
    public string MentorshipStatus { get; set; } = string.Empty;
    public bool IsPayoutEligible { get; set; }
    public int? MarkedByStaffID { get; set; }
    public DateTime? MarkedAt { get; set; }
}
```

### 6.3 Service Implementation (pseudo-code)

```csharp
// ================================================================
// GET REPORTS FOR OVERSIGHT (Staff)
// ================================================================
public async Task<...> GetReportsForOversightAsync(
    string? reviewStatus, int? advisorId, int? startupId,
    DateTime? from, DateTime? to, int page, int pageSize)
{
    var query = _db.MentorshipReports
        .Include(r => r.Mentorship).ThenInclude(m => m.Startup)
        .Include(r => r.Mentorship).ThenInclude(m => m.Advisor)
        .Include(r => r.Session)
        .Where(r => r.SupersededByReportID == null)  // chỉ report hiện hành
        .AsNoTracking();

    if (string.IsNullOrEmpty(reviewStatus) || reviewStatus == "PendingReview")
        query = query.Where(r => r.ReportReviewStatus == ReportReviewStatus.PendingReview);
    else if (reviewStatus != "all" && Enum.TryParse<ReportReviewStatus>(reviewStatus, out var s))
        query = query.Where(r => r.ReportReviewStatus == s);

    if (advisorId.HasValue)
        query = query.Where(r => r.CreatedByAdvisorID == advisorId.Value);
    if (startupId.HasValue)
        query = query.Where(r => r.Mentorship.StartupID == startupId.Value);
    if (from.HasValue)
        query = query.Where(r => r.SubmittedAt >= from.Value);
    if (to.HasValue)
        query = query.Where(r => r.SubmittedAt <= to.Value);

    query = query.OrderByDescending(r => r.SubmittedAt);
    // Paged, project to ReportOversightDto
}

// ================================================================
// REVIEW REPORT (Staff)
// ================================================================
public async Task<...> ReviewReportAsync(int staffUserId, int reportId, ReviewReportRequest request)
{
    var report = await _db.MentorshipReports
        .Include(r => r.Mentorship).ThenInclude(m => m.Reports)
        .Include(r => r.Mentorship).ThenInclude(m => m.Sessions)
        .FirstOrDefaultAsync(r => r.ReportID == reportId);
    if (report == null) → 404

    if (!Enum.TryParse<ReportReviewStatus>(request.ReviewStatus, out var newStatus)
        || newStatus == ReportReviewStatus.PendingReview)
        → 400 INVALID_REVIEW_STATUS

    report.ReportReviewStatus = newStatus;
    report.ReviewedByStaffID = staffUserId;
    report.StaffReviewNote = request.Note;
    report.ReviewedAt = DateTime.UtcNow;

    // Auto-recalculate payout eligibility
    RecalculatePayoutEligibility(report.Mentorship);

    await _db.SaveChangesAsync();
    await _audit.LogAsync("REVIEW_REPORT", "MentorshipReport", reportId, $"Status={newStatus}");
    // → Notifications (Section 12)
}

// ================================================================
// CONFIRM CONDUCTED (Startup)
// ================================================================
public async Task<...> ConfirmConductedAsync(int userId, int mentorshipId, int sessionId)
{
    var startup = ... // verify startup owner
    var session = await _db.MentorshipSessions
        .FirstOrDefaultAsync(s => s.SessionID == sessionId && s.MentorshipID == mentorshipId);

    if (session == null) → 404
    if (session.SessionStatus != SessionStatusValues.Scheduled
        && session.SessionStatus != SessionStatusValues.InProgress)
        → 400 INVALID_STATUS_TRANSITION "Session must be Scheduled or InProgress"

    session.SessionStatus = SessionStatusValues.Conducted;
    session.StartupConfirmedConductedAt = DateTime.UtcNow;
    session.UpdatedAt = DateTime.UtcNow;

    await _db.SaveChangesAsync();
    await _audit.LogAsync("CONFIRM_CONDUCTED", "MentorshipSession", sessionId, ...);
    // → Notify advisor: startup confirmed session conducted
}

// ================================================================
// MARK SESSION COMPLETED (Staff only — BA feedback #3)
// ================================================================
public async Task<...> MarkSessionCompletedAsync(int staffUserId, int mentorshipId, int sessionId, string? note)
{
    var session = await _db.MentorshipSessions
        .Include(s => s.Reports)
        .Include(s => s.Mentorship).ThenInclude(m => m.Sessions)
        .Include(s => s.Mentorship).ThenInclude(m => m.Reports)
        .FirstOrDefaultAsync(s => s.SessionID == sessionId);

    if (session == null || session.MentorshipID != mentorshipId) → 404 SESSION_NOT_FOUND

    // Precondition: Startup phải đã confirm
    if (session.SessionStatus != SessionStatusValues.Conducted)
        → 400 SESSION_NOT_CONDUCTED
          "Startup must confirm conducted before staff can mark completed."

    // Precondition: Reports đều Passed
    var currentReports = session.Reports
        .Where(r => r.SupersededByReportID == null);
    if (!currentReports.Any())
        → 400 NO_REPORT "Session must have at least one report."
    if (currentReports.Any(r => r.ReportReviewStatus != ReportReviewStatus.Passed))
        → 400 REPORTS_NOT_ALL_PASSED

    session.SessionStatus = SessionStatusValues.Completed;
    session.MarkedByStaffID = staffUserId;
    session.MarkedAt = DateTime.UtcNow;
    session.UpdatedAt = DateTime.UtcNow;

    // Auto-aggregate mentorship status + payout
    RecalculateMentorshipStatus(session.Mentorship);
    RecalculatePayoutEligibility(session.Mentorship);

    await _db.SaveChangesAsync();
    await _audit.LogAsync("STAFF_MARK_SESSION_COMPLETED", "MentorshipSession", sessionId, note);
}

// ================================================================
// MARK SESSION DISPUTE (Staff only)
// ================================================================
public async Task<...> MarkSessionDisputeAsync(int staffUserId, int mentorshipId, int sessionId, string reason)
{
    var session = await _db.MentorshipSessions
        .Include(s => s.Mentorship).ThenInclude(m => m.Sessions)
        .Include(s => s.Mentorship).ThenInclude(m => m.Reports)
        .FirstOrDefaultAsync(s => s.SessionID == sessionId);

    if (session == null || session.MentorshipID != mentorshipId) → 404 SESSION_NOT_FOUND

    var allowedStatuses = new[] {
        SessionStatusValues.Scheduled, SessionStatusValues.InProgress,
        SessionStatusValues.Conducted, SessionStatusValues.Completed,
        SessionStatusValues.Resolved  // BR-07: re-open
    };
    if (!allowedStatuses.Contains(session.SessionStatus))
        → 400 INVALID_STATUS_TRANSITION

    session.SessionStatus = SessionStatusValues.InDispute;
    session.DisputeReason = reason;
    session.ResolutionNote = null;  // Clear previous
    session.MarkedByStaffID = staffUserId;
    session.MarkedAt = DateTime.UtcNow;
    session.UpdatedAt = DateTime.UtcNow;

    RecalculateMentorshipStatus(session.Mentorship);
    RecalculatePayoutEligibility(session.Mentorship);

    await _db.SaveChangesAsync();
    await _audit.LogAsync("STAFF_MARK_SESSION_DISPUTE", "MentorshipSession", sessionId, reason);
}

// ================================================================
// MARK SESSION RESOLVED (Staff only)
// ================================================================
public async Task<...> MarkSessionResolvedAsync(int staffUserId, int mentorshipId, int sessionId, ResolveDisputeRequest request)
{
    var session = await _db.MentorshipSessions
        .Include(s => s.Mentorship).ThenInclude(m => m.Sessions)
        .Include(s => s.Mentorship).ThenInclude(m => m.Reports)
        .FirstOrDefaultAsync(s => s.SessionID == sessionId);

    if (session == null || session.MentorshipID != mentorshipId) → 404 SESSION_NOT_FOUND
    if (session.SessionStatus != SessionStatusValues.InDispute) → 400

    session.ResolutionNote = request.Resolution;
    session.MarkedByStaffID = staffUserId;
    session.MarkedAt = DateTime.UtcNow;
    session.UpdatedAt = DateTime.UtcNow;

    if (request.RestoreCompleted)
        session.SessionStatus = SessionStatusValues.Completed;
    else
        session.SessionStatus = SessionStatusValues.Resolved;

    RecalculateMentorshipStatus(session.Mentorship);
    RecalculatePayoutEligibility(session.Mentorship);

    await _db.SaveChangesAsync();
    await _audit.LogAsync("STAFF_RESOLVE_SESSION", "MentorshipSession", sessionId, request.Resolution);
}

// ================================================================
// HELPER: Recalculate Mentorship Status from Sessions
// ================================================================
private void RecalculateMentorshipStatus(StartupAdvisorMentorship mentorship)
{
    var sessions = mentorship.Sessions
        .Where(s => s.SessionStatus != SessionStatusValues.Cancelled
                  && s.SessionStatus != SessionStatusValues.ProposedByStartup
                  && s.SessionStatus != SessionStatusValues.ProposedByAdvisor)
        .ToList();

    if (!sessions.Any()) return;  // không đổi nếu chưa có session active

    // Ưu tiên: InDispute > InProgress > Completed
    if (sessions.Any(s => s.SessionStatus == SessionStatusValues.InDispute))
    {
        mentorship.MentorshipStatus = MentorshipStatus.InDispute;
    }
    else if (sessions.All(s => s.SessionStatus == SessionStatusValues.Completed))
    {
        mentorship.MentorshipStatus = MentorshipStatus.Completed;
        mentorship.CompletedAt ??= DateTime.UtcNow;
    }
    else
    {
        // Mix: some Scheduled/InProgress/Conducted/Resolved → keep InProgress
        // BA chốt #6: Resolved ≠ Completed — chủ đích, không phải bug
        // Mentorship có session Resolved sẽ KHÔNG aggregate thành Completed
        if (mentorship.MentorshipStatus == MentorshipStatus.InDispute
            || mentorship.MentorshipStatus == MentorshipStatus.Completed)
        {
            mentorship.MentorshipStatus = MentorshipStatus.InProgress;
        }
    }
    mentorship.UpdatedAt = DateTime.UtcNow;
}

// ================================================================
// HELPER: Recalculate Payout Eligibility
// ================================================================
private void RecalculatePayoutEligibility(StartupAdvisorMentorship mentorship)
{
    var activeSessions = mentorship.Sessions
        .Where(s => s.SessionStatus != SessionStatusValues.Cancelled
                  && s.SessionStatus != SessionStatusValues.ProposedByStartup
                  && s.SessionStatus != SessionStatusValues.ProposedByAdvisor)
        .ToList();

    var allSessionsCompleted = activeSessions.Any()
        && activeSessions.All(s => s.SessionStatus == SessionStatusValues.Completed);

    var allStartupConfirmed = activeSessions.All(s => s.StartupConfirmedConductedAt != null);

    var currentReports = mentorship.Reports
        .Where(r => r.SupersededByReportID == null);
    var allReportsPassed = currentReports.Any()
        && currentReports.All(r => r.ReportReviewStatus == ReportReviewStatus.Passed);

    var noDispute = !activeSessions.Any(s => s.SessionStatus == SessionStatusValues.InDispute);

    mentorship.IsPayoutEligible =
        allSessionsCompleted && allStartupConfirmed && allReportsPassed && noDispute;
}
```

---

## 7. Thay đổi logic hiện tại

### 7.1 Report Visibility Gate ⚠️ BREAKING CHANGE

**File:** `MentorshipService.cs` → `GetReportAsync()`

> Dùng **403 Forbidden** — resource tồn tại nhưng startup chưa có quyền xem.

```diff
  if (!await IsParticipantOrStaff(userId, userType, report.Mentorship))
      return ApiResponse<ReportDto>.ErrorResponse("MENTORSHIP_NOT_OWNED", ...);

+ // Visibility gate: Startup chỉ xem report khi đã Passed
+ if (userType == "Startup" && report.ReportReviewStatus != ReportReviewStatus.Passed)
+     return ApiResponse<ReportDto>.ErrorResponse("REPORT_NOT_AVAILABLE",
+         "No consulting report available yet.", 403);  // MSG117

  return ApiResponse<ReportDto>.SuccessResponse(MapReportDto(report));
```

### 7.2 Report trong MentorshipDetailDto — filter cho Startup

Khi map `Reports` trong `MapToDetailDto`, nếu caller là Startup, chỉ include reports có `ReportReviewStatus == Passed`.

### 7.3 ReportDto — thêm review fields

```diff
  public class ReportDto
  {
      ...
      public DateTime CreatedAt { get; set; }
+     public string ReviewStatus { get; set; } = string.Empty;
+     public string? StaffReviewNote { get; set; }
+     public DateTime? ReviewedAt { get; set; }
  }
```

> Startup KHÔNG thấy `StaffReviewNote` → giữ null cho Startup, populate cho Advisor/Staff/Admin.

### 7.4 CreateReportAsync — SessionID required + default status + supersede

> **BA chốt #2:** `SessionID` từ giờ là **required** cho reports mới. Legacy reports (`SessionID = null`) không tham gia gate.

```diff
  var report = new MentorshipReport
  {
      ...
+     SessionID = request.SessionId,  // REQUIRED (validator enforced)
      SubmittedAt = DateTime.UtcNow,
+     ReportReviewStatus = ReportReviewStatus.PendingReview,
      CreatedAt = DateTime.UtcNow
  };

+ // BA chốt #5: Supersede chain — system-managed
+ var existingReport = await _db.MentorshipReports
+     .Where(r => r.SessionID == request.SessionId
+              && r.MentorshipID == mentorshipId
+              && r.ReportReviewStatus == ReportReviewStatus.NeedsMoreInfo
+              && r.SupersededByReportID == null)
+     .OrderByDescending(r => r.CreatedAt)
+     .FirstOrDefaultAsync();
+
+ _db.MentorshipReports.Add(report);
+ await _db.SaveChangesAsync();  // Save 1: get report.ReportID
+
+ if (existingReport != null)
+ {
+     existingReport.SupersededByReportID = report.ReportID;
+     await _db.SaveChangesAsync();  // Save 2: link supersede
+ }
```

> **Lưu ý:** 2 lần `SaveChanges` trong cùng request. Nếu dùng retry execution strategy thì wrap trong `CreateExecutionStrategy().Execute()` transaction.

### 7.5 Advisor CompleteAsync — BỎ hoặc KHÓA ⚠️

> **BA feedback #3:** Chỉ Staff được mark completed. Advisor KHÔNG tự complete.

**Có 2 xử lý:**

- **Option A (recommended):** Bỏ hẳn endpoint `PUT /api/mentorships/{id}/complete` (Advisor). Completion chỉ qua Staff mark session completed → aggregate.
- **Option B (transition):** Giữ endpoint nhưng trả error: `"COMPLETION_BY_ADVISOR_DISABLED"` — "Mentorship completion is now handled by Operations Staff."

**Đề xuất:** Option A. Advisor chỉ cần:
1. Nộp report (`POST /api/mentorships/{id}/reports`)
2. Bổ sung report khi NeedsMoreInfo
3. Bấm confirm completion = advisor confirm done (giữ `CompletionConfirmedByAdvisor` flag)

### 7.6 SessionDto — thêm oversight fields

```diff
  public class SessionDto
  {
      ...
      public DateTime? UpdatedAt { get; set; }
+     public DateTime? StartupConfirmedConductedAt { get; set; }
+     public string? DisputeReason { get; set; }
+     public string? ResolutionNote { get; set; }
+     public int? MarkedByStaffID { get; set; }
+     public DateTime? MarkedAt { get; set; }
  }
```

---

## 8. FE Implementation Guide

### 8.1 Màn hình Staff — Consulting Oversight Dashboard

**Route:** `/staff/consulting-oversight`

**Layout:**

```
┌──────────────────────────────────────────────────────────────────┐
│  Consulting Oversight                                             │
├──────────────────────────────────────────────────────────────────┤
│  Tabs: [Chờ duyệt (5)] [Đã duyệt] [Cần bổ sung] [Tất cả]     │
├──────────────────────────────────────────────────────────────────┤
│  Filter: [ Advisor ▼ ] [ Startup ▼ ] [ Từ ngày ] [ Đến ngày ]  │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │ 📄 Report #1                         ⏱ 2 ngày trước      │    │
│  │ Mentorship: TechVN ↔ Dr. Nguyen Van A                    │    │
│  │ Session: #12 — "Go-to-market strategy"                    │    │
│  │ Session Status: ✅ Conducted                              │    │
│  │ Summary: "Great progress on GTM..."                       │    │
│  │ Review Status: 🟡 Chờ thẩm định                          │    │
│  │                                    [Xem chi tiết →]       │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                   │
│  Pagination: ← 1 2 3 →                                          │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

### 8.2 Màn hình Staff — Report Review Detail

**Route:** `/staff/consulting-oversight/reports/{reportId}`

```
┌──────────────────────────────────────────────────────────────────┐
│  ← Quay lại                          Report #1                   │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─ Thông tin Session ───────────────────────────────────────┐   │
│  │ Startup: TechVN          Advisor: Dr. Nguyen Van A        │   │
│  │ Challenge: Go-to-market strategy for B2B SaaS             │   │
│  │ Session: #12 (2026-04-10)  Status: Conducted              │   │
│  │ Startup Confirmed: ✅ 2026-04-11                          │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                   │
│  ┌─ Nội dung Report ─────────────────────────────────────────┐   │
│  │ Summary: "Great progress on GTM strategy..."               │   │
│  │ Detailed Findings: "1. Market analysis shows..."           │   │
│  │ Recommendations: "Focus on enterprise segment..."          │   │
│  │ Attachments: [📎 Download]                                │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                   │
│  ┌─ Kết quả Review ──────────────────────────────────────────┐   │
│  │  Ghi chú (tùy chọn):                                     │   │
│  │  ┌──────────────────────────────────────────────────┐     │   │
│  │  │                                                    │     │   │
│  │  └──────────────────────────────────────────────────┘     │   │
│  │  [✅ Duyệt (Passed)]  [🔄 Yêu cầu bổ sung]  [❌ Từ chối] │   │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

### 8.3 Màn hình Staff — Session Oversight Actions

**Xuất hiện trong:** Session Detail page (trong mentorship detail) — thêm panel cho Staff

```
┌─ Staff Session Actions ──────────────────────────────────────────┐
│                                                                   │
│  Session #12 Status: Conducted                                    │
│  Startup Confirmed: ✅ 2026-04-11 09:00                          │
│  Reports: 1/1 Passed ✅                                          │
│                                                                   │
│  [✅ Mark Completed]   [⚠️ Mark InDispute]                       │
│                                                                   │
│  (Nếu đang InDispute:)                                           │
│  Dispute Reason: "Startup reported advisor did not show up."      │
│  Resolution:                                                      │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                                                              │  │
│  └────────────────────────────────────────────────────────────┘  │
│  ☑ Restore to Completed (cho phép payout)                        │
│  [✅ Resolve Dispute]                                             │
│                                                                   │
├──────────────────────────────────────────────────────────────────┤
│  Mentorship Aggregate:                                            │
│  Status: InProgress (2/3 sessions completed)                     │
│  Payout Eligible: ❌ Chưa (1 session chưa completed)            │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

### 8.4 Startup — Confirm Conducted Button

**Xuất hiện trong:** Session detail khi SessionStatus = `Scheduled` hoặc `InProgress`

```
┌─ Session #12 ────────────────────────────────────────────────────┐
│  Trạng thái: Đã lên lịch                                        │
│  Thời gian: 2026-04-10 10:00 — 11:00                            │
│  Meeting URL: https://meet.google.com/abc                        │
│                                                                   │
│  [✅ Xác nhận đã tư vấn]                                        │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

**API Call:**

```typescript
POST /api/mentorships/{mentorshipId}/sessions/{sessionId}/confirm-conducted
```

> **BA chốt #2 — Breaking change:** Khi create report, FE phải truyền `sessionId` (bắt buộc từ v2.1).

### 8.5 Advisor — Report Status Badge

| ReportReviewStatus | Badge | Mô tả |
|---|---|---|
| `PendingReview` | 🟡 Chờ thẩm định | Staff chưa review |
| `Passed` | ✅ Đã duyệt | Startup có thể xem |
| `Failed` | ❌ Không đạt | Cần tạo report mới |
| `NeedsMoreInfo` | 🔄 Cần bổ sung | Advisor sửa/bổ sung |

### 8.6 Startup — Report Visibility

- Gọi `GET /api/mentorships/{id}` → `reports` array chỉ chứa reports đã `Passed`
- Gọi `GET /api/mentorships/reports/{reportId}` cho report chưa Passed → **403** "No consulting report available yet"
- FE hiển thị placeholder: "Báo cáo tư vấn chưa sẵn sàng"

---

## 9. Business Rules

### BR-01: Report review mandatory

> Report advisor nộp PHẢI được Staff review (Passed) trước khi startup xem được.

### BR-02: Payout gate

> Payment release chỉ khi:
> - TẤT CẢ sessions `Completed` (aggregate MentorshipStatus = Completed)
> - Startup đã confirm conducted cho TẤT CẢ sessions
> - TẤT CẢ reports hiện hành đều `Passed`
> - KHÔNG có session nào `InDispute`

### BR-03: Dispute blocks payout

> Khi BẤT KỲ session nào bị `InDispute`, `IsPayoutEligible` = `false` cho toàn mentorship.

### BR-04: Session status transition rules (staff)

| From | To | Allowed? | Condition |
|------|-----|----------|-----------|
| Conducted | Completed | ✅ | Reports all Passed |
| Scheduled/InProgress/Conducted/Completed | InDispute | ✅ | Staff có reason |
| InDispute | Completed | ✅ | restoreCompleted=true |
| InDispute | Resolved | ✅ | restoreCompleted=false |
| Resolved | InDispute | ✅ | Re-open (BR-07) |
| Cancelled | * | ❌ | |

### BR-05: Report re-submission (supersede chain)

> **BA chốt #5:** `SupersededByReportID` là **system-managed field**, không cho client tự truyền.
>
> Khi advisor tạo report mới cho cùng session:
> 1. Tìm report hiện hành gần nhất của session đó có status `NeedsMoreInfo` + `SupersededByReportID == null`
> 2. Save report mới trước (để lấy ID)
> 3. Set report cũ: `SupersededByReportID = newReportID`
> 4. Save lần 2 (trong cùng request, cần transaction nếu dùng retry)
> - Oversight queue chỉ hiện report hiện hành (`SupersededByReportID == null`)

### BR-06: Startup confirm conducted là prerequisite

> **BA feedback #2:** Startup PHẢI confirm session đã diễn ra trước khi:
> - Staff có thể mark session `Completed`
> - Report visibility chính thức
> - Payout eligibility

### BR-07: Resolved KHÔNG phải terminal state (session-level)

> Staff có thể re-open dispute từ `Resolved` → `InDispute` nếu có evidence mới.

### BR-08: Mentorship status là aggregate (KHÔNG set trực tiếp)

> **BA feedback #1:** Staff KHÔNG mark mentorship completed/dispute/resolved trực tiếp. Thay vào đó:
> - Staff mark từng SESSION
> - Hệ thống auto-aggregate mentorship status từ sessions
> - `RecalculateMentorshipStatus()` chạy sau mỗi session change

### BR-09: Chỉ Staff marks session completed

> **BA feedback #3:** Advisor KHÔNG tự mark completed. Advisor chỉ:
> - Nộp report
> - Bổ sung report khi NeedsMoreInfo
> - Confirm completion bằng flag `CompletionConfirmedByAdvisor`
>
> Staff là người duy nhất kiểm tra + mark session Completed.

### BR-10: SessionID bắt buộc cho report mới

> **BA chốt #2:** Report mới PHẢI có `SessionID`. Không chấp nhận report mentorship-level không gắn session.
> - Validator: `SessionID` required
> - Legacy reports (`SessionID = null`): không tham gia payout gate, không tham gia session completion gate
> - **Breaking change** cho FE: FE phải truyền `sessionId` khi create report

### BR-11: Resolved không được tính là Completed

> **BA chốt #6:** `Resolved ≠ Completed`. Session Resolved không tính completed:
> - Mentorship có session Resolved không aggregate thành Completed
> - `IsPayoutEligible = false` nếu có session Resolved
> - Muốn payout: staff phải resolve với `restoreCompleted = true`
> - Đây là **chủ đích**, không phải bug.

### BR-12: Payment release deferred

> **BA chốt #4:** Sprint này chỉ làm `IsPayoutEligible` flag.
> - `IsPayoutEligible = true` = đủ điều kiện nghiệp vụ
> - Không làm payout execution / disbursement service
> - Phase sau sẽ xử lý release tiền thật

---

## 10. Message Codes

| Code | HTTP | Message |
|------|------|---------|
| `REPORT_NOT_AVAILABLE` | 403 | No consulting report available yet. (MSG117) |
| `INVALID_REVIEW_STATUS` | 400 | Review status must be Passed, Failed, or NeedsMoreInfo. |
| `REPORTS_NOT_ALL_PASSED` | 400 | Cannot mark completed — not all reports have passed review. |
| `NO_REPORT` | 400 | Session must have at least one report. |
| `INVALID_STATUS_TRANSITION` | 400 | Session state transition is invalid. (MSG147) |
| `SESSION_NOT_FOUND` | 404 | Session not found. |
| `SESSION_NOT_CONDUCTED` | 400 | Startup must confirm conducted before staff can mark completed. |
| `MENTORSHIP_NOT_FOUND` | 404 | Mentorship not found. |
| `REPORT_NOT_FOUND` | 404 | Report not found. |
| `REPORT_REVIEWED` | 200 | Report reviewed successfully. |
| `SESSION_MARKED_COMPLETED` | 200 | Session marked as completed. (MSG144) |
| `SESSION_MARKED_DISPUTE` | 200 | Session marked as in dispute. (MSG145) |
| `SESSION_MARKED_RESOLVED` | 200 | Dispute resolved. (MSG146) |
| `SESSION_CONFIRMED_CONDUCTED` | 200 | Session confirmed as conducted. |

---

## 11. Checklist triển khai

### BE Tasks

- [ ] **DB-1:** Thêm enum `ReportReviewStatus` vào `Enums.cs`
- [ ] **DB-2:** Sửa `MentorshipReport` entity — đổi `ReviewedByStaff` → 5 fields mới
- [ ] **DB-3:** Thêm `DisputeReason`, `ResolutionNote`, `MarkedByStaffID`, `MarkedAt` vào `MentorshipSession`
- [ ] **DB-4:** Thêm `IsPayoutEligible` vào `StartupAdvisorMentorship`
- [ ] **DB-5:** Thêm `Conducted`, `InDispute`, `Resolved` vào `SessionStatusValues`
- [ ] **DB-6:** Register enum conversion trong `ApplicationDbContext`
- [ ] **DB-7:** Tạo + chạy migration
- [ ] **DTO-1:** Thêm DTOs oversight (ReportOversightDto, ReviewReportRequest, etc.)
- [ ] **DTO-2:** Thêm `ReviewStatus`, `StaffReviewNote`, `ReviewedAt` vào `ReportDto`
- [ ] **DTO-3:** Thêm oversight fields vào `SessionDto`
- [ ] **SVC-1:** Implement `GetReportsForOversightAsync`
- [ ] **SVC-2:** Implement `ReviewReportAsync`
- [ ] **SVC-3:** Implement `ConfirmConductedAsync` (Startup)
- [ ] **SVC-4:** Implement `MarkSessionCompletedAsync` (Staff)
- [ ] **SVC-5:** Implement `MarkSessionDisputeAsync` (Staff)
- [ ] **SVC-6:** Implement `MarkSessionResolvedAsync` (Staff)
- [ ] **SVC-7:** Implement `RecalculateMentorshipStatus()` helper
- [ ] **SVC-8:** Implement `RecalculatePayoutEligibility()` helper
- [ ] **SVC-9:** Sửa `GetReportAsync` — thêm visibility gate 403 cho Startup
- [ ] **SVC-10:** Sửa `GetDetailAsync` / `MapToDetailDto` — filter reports cho Startup
- [ ] **SVC-11:** Sửa `CreateReportAsync` — SessionID required + set default `PendingReview` + handle supersede chain (2 saves)
- [ ] **SVC-12:** Sửa `MapReportDto` — include review fields
- [ ] **SVC-13:** Bỏ/khóa `CompleteAsync` (Advisor không tự complete — BR-09)
- [ ] **SVC-14:** Audit tất cả status guards — thêm `Conducted` vào các chỗ check Scheduled/InProgress/Completed (BA chốt #3)
- [ ] **CTRL-1:** Thêm 6 endpoints mới vào `MentorshipsController` (routes thống nhất `/api/mentorships/...`)
- [ ] **VALID-1:** Thêm validators cho `ReviewReportRequest`, `StaffMarkDisputeRequest`, `ResolveDisputeRequest`
- [ ] **VALID-2:** Sửa validator `CreateReportRequest` — `SessionID` required (BA chốt #2, breaking change)
- [ ] **IF-1:** Cập nhật `IMentorshipService` interface (thêm mentorshipId param cho session actions)
- [ ] **BUILD:** Verify build passes (0 errors, 0 warnings)

### FE Tasks

- [ ] **FE-1:** Tạo page `/staff/consulting-oversight` — report queue
- [ ] **FE-2:** Tạo page `/staff/consulting-oversight/reports/{id}` — report review detail
- [ ] **FE-3:** Thêm Staff Session Actions panel vào Session Detail
- [ ] **FE-4:** Thêm "Xác nhận đã tư vấn" button cho Startup trên Session Detail
- [ ] **FE-5:** Thêm report status badge cho Advisor view
- [ ] **FE-6:** Xử lý 403 `REPORT_NOT_AVAILABLE` cho Startup → placeholder
- [ ] **FE-7:** Cập nhật sidebar navigation (thêm Consulting Oversight)
- [ ] **FE-8:** Handle `staffReviewNote` — chỉ hiện cho Advisor/Staff, ẩn cho Startup
- [ ] **FE-9:** Error handling cho tất cả staff actions (xem Section 13)
- [ ] **FE-10:** Hiển thị badge "Đã thay thế" cho superseded reports
- [ ] **FE-11:** Hiển thị `disputeReason` + `resolutionNote` trên Session Detail
- [ ] **FE-12:** Hiển thị mentorship aggregate status (tính từ sessions)

---

## 12. Notification Events

| Trigger | Recipient | Nội dung | Type |
|---------|-----------|----------|------|
| Startup confirm conducted | **Advisor** | "Startup đã xác nhận session #{sessionId} đã diễn ra." | `SESSION_CONDUCTED` |
| Staff marks report **Passed** | **Startup** | "Báo cáo tư vấn cho session #{sessionId} đã sẵn sàng xem." | `REPORT_APPROVED` |
| Staff marks report **Passed** | **Advisor** | "Báo cáo #{reportId} đã được duyệt." | `REPORT_APPROVED` |
| Staff marks report **NeedsMoreInfo** | **Advisor** | "Báo cáo #{reportId} cần bổ sung: {staffReviewNote}" | `REPORT_NEEDS_INFO` |
| Staff marks report **Failed** | **Advisor** | "Báo cáo #{reportId} không đạt: {staffReviewNote}" | `REPORT_REJECTED` |
| Staff marks session **Completed** | **Startup + Advisor** | "Session #{sessionId} đã hoàn thành." | `SESSION_COMPLETED` |
| Staff marks session **InDispute** | **Startup + Advisor** | "Session #{sessionId} đang được xem xét tranh chấp." | `SESSION_DISPUTE` |
| Staff marks session **Resolved** | **Startup + Advisor** | "Tranh chấp session #{sessionId} đã được giải quyết." | `SESSION_RESOLVED` |
| Auto: IsPayoutEligible → true | **Advisor** | "Payout cho mentorship #{id} đã đủ điều kiện." | `PAYOUT_ELIGIBLE` |

---

## 13. FE Error Handling Spec

| Error Code | HTTP | FE Action | UI Element |
|---|---|---|---|
| `REPORTS_NOT_ALL_PASSED` | 400 | Toast warning | "Không thể hoàn thành — còn báo cáo chưa duyệt." |
| `NO_REPORT` | 400 | Toast warning | "Session chưa có báo cáo nào." |
| `SESSION_NOT_CONDUCTED` | 400 | Toast warning | "Startup chưa xác nhận đã tư vấn." |
| `INVALID_STATUS_TRANSITION` | 400 | Toast error | "Trạng thái hiện tại không cho phép thao tác này." |
| `INVALID_REVIEW_STATUS` | 400 | Toast error | "Kết quả review không hợp lệ." |
| `SESSION_NOT_FOUND` | 404 | Redirect | "Session không tồn tại." |
| `REPORT_NOT_FOUND` | 404 | Redirect | "Báo cáo không tồn tại." |
| `MENTORSHIP_NOT_FOUND` | 404 | Redirect | "Mentorship không tồn tại." |
| `REPORT_NOT_AVAILABLE` | 403 | Placeholder card | "Báo cáo tư vấn chưa sẵn sàng." |
| Network / 500 | 500 | Toast error | "Đã xảy ra lỗi hệ thống. Vui lòng thử lại." |

**Pattern chung:**
- **400:** Toast warning/error, KHÔNG redirect, giữ form state
- **403:** Placeholder thân thiện, KHÔNG toast (expected behavior)
- **404:** Toast + redirect về list page
- **500:** Toast error + suggest retry
- **Optimistic UI:** Disable button + spinner → chờ response → toast success/error
