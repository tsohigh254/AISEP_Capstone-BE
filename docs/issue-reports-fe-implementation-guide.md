# Issue Reports — FE Implementation Guide

> Hướng dẫn implement tính năng **Báo cáo sự cố** sau khi BE đã hoàn thành endpoints. Đọc kỹ trước khi code để tránh round-trip lặp lại.

**Liên quan:**
- BE API spec: `/api/issue-reports` (POST, GET list, GET /me, GET /{id}, PATCH /{id}/status)
- Existing mock: [services/issue-report.api.ts](../services/issue-report.api.ts)
- Existing modal UI: [components/shared/issue-report-modal.tsx](../components/shared/issue-report-modal.tsx)
- Existing staff page (mock): [app/staff/issue-reports/page.tsx](../app/staff/issue-reports/page.tsx)

---

## 1. Mục tiêu

Chuyển toàn bộ luồng báo cáo sự cố từ **mock** sang **API thật**:

- User (Startup/Advisor/Investor) submit báo cáo có context (Mentorship/Session/Payment/AdvisorReport/Connection/User) hoặc không context.
- Staff/Admin xem danh sách báo cáo, filter theo status/category/reporter, đổi status + ghi note.
- Reporter xem lại các báo cáo của chính mình.
- Notification realtime qua SignalR cho cả 2 phía (staff khi có report mới, reporter khi staff đổi status).

---

## 2. Trạng thái hiện tại của FE (audit)

| Hạng mục | Hiện tại | Đánh giá |
|---|---|---|
| `services/issue-report.api.ts` | Mock với `setTimeout 1500ms`, hardcode `success: true` | Phải viết lại hoàn toàn |
| `components/shared/issue-report-modal.tsx` | UI hoàn chỉnh, gọi mock API | Sửa: max 5 file, đổi kiểu `entityId`, đổi shape response |
| Caller `entityType` ở 5 chỗ | Sai whitelist BE (`CONSULTING_REQUEST`, `PAYMENT`, ...) | Phải đổi sang `Mentorship`, `Session`, ... |
| `entityId` truyền vào modal | `string` | Phải đổi sang `number \| null` (BE yêu cầu int) |
| `app/staff/issue-reports/page.tsx` | Hardcode `ISSUES_DATA` (mock từ 03/2024) | Viết lại toàn bộ + paging |
| Trang chi tiết staff `/staff/issue-reports/[id]` | Chưa có | Tạo mới |
| Trang reporter "My reports" | Chưa có | Tạo mới (3 role × 2 trang) |
| SignalR routing | Hook `useNotifications` đã có sẵn | Chỉ cần dùng `actionUrl` BE gửi |

---

## 3. Final API Contract (đã confirm với BE)

### Base URL
```
/api/issue-reports
```

### Endpoints

| Method | Path | Auth | Mục đích |
|---|---|---|---|
| `POST` | `/api/issue-reports` | Any logged-in | Tạo báo cáo (multipart/form-data) |
| `GET` | `/api/issue-reports/me` | Any logged-in | Reporter xem report của mình (Summary DTO) |
| `GET` | `/api/issue-reports/{id}` | Reporter own / Staff / Admin | Chi tiết (full DTO + attachments + staffNote) |
| `GET` | `/api/issue-reports` | Staff / Admin | List + filter |
| `PATCH` | `/api/issue-reports/{id}/status` | Staff / Admin | Đổi status + cập nhật staffNote |

### Request: POST (multipart/form-data)

| Field | Type | Required | Note |
|---|---|---|---|
| `issueCategory` | `number` (0–8) | ✅ | Xem mapping ở §4 |
| `description` | `string` | ✅ | FE đã validate min 20 ký tự |
| `relatedEntityType` | `string \| null` | ❌ | Whitelist ở §4 |
| `relatedEntityID` | `number \| null` | ❌ | **Tên field có chữ ID viết hoa** |
| `attachments` | `File[]` | ❌ | Tối đa **5 file**, mỗi file ≤ 10MB |

### Request: PATCH /status

```json
{ "status": 2, "staffNote": "Đã xử lý xong" }
```
- `status`: number (0–3)
- `staffNote`: optional. Nếu không gửi, BE giữ nguyên note cũ (không xóa).

### Response shape (consistent với codebase)

Tất cả endpoints đều trả về `IBackendRes<T>`:
```ts
{ success: boolean; data: T; message?: string }
```

#### POST response (`IssueReportSummaryDto`)
```ts
{
  issueReportID: number;
  category: string;        // BE trả về tên enum dạng PascalCase: "PaymentIssue"
  status: string;          // "New" | "UnderReview" | "Resolved" | "Dismissed"
  description: string;
  relatedEntityType: string | null;
  relatedEntityID: number | null;
  submittedAt: string;     // ISO
  updatedAt: string | null;
}
```

#### GET /{id} response (`IssueReportDetailDto`)
Như Summary + bổ sung:
```ts
{
  // ...all summary fields
  reporterUserID: number;
  reporterName: string;
  staffNote: string | null;
  assignedToStaffID: number | null;
  attachments: Array<{
    attachmentID: number;
    fileUrl: string;       // Cloudinary URL
    fileName: string;
    fileSize: number;
    mimeType: string;
  }>;
}
```

#### GET /me và GET (staff list) response
```ts
IPaginatedRes<IssueReportSummaryDto>  // /me - không kèm attachments/staffNote
IPaginatedRes<IssueReportDetailDto>   // staff list - đầy đủ
```

### Error codes
- `400 INVALID_RELATED_ENTITY_TYPE` — `relatedEntityType` ngoài whitelist
- `403 FORBIDDEN` — reporter cố xem report của người khác
- `404 ISSUE_REPORT_NOT_FOUND`

---

## 4. Enum & whitelist mapping (CRITICAL — không được sai)

### IssueCategory (FE → BE number → BE response string)

| FE constant (giữ nguyên cho UI) | BE number (request) | BE string (response) |
|---|---|---|
| `PAYMENT_ISSUE` | `0` | `PaymentIssue` |
| `CONSULTING_ISSUE` | `1` | `ConsultingIssue` |
| `MESSAGING_ISSUE` | `2` | `MessagingIssue` |
| `OFFER_OR_CONNECTION_ISSUE` | `3` | `OfferOrConnectionIssue` |
| `VERIFICATION_ISSUE` | `4` | `VerificationIssue` |
| `DOCUMENT_ISSUE` | `5` | `DocumentIssue` |
| `HARASSMENT_OR_MISCONDUCT` | `6` | `HarassmentOrMisconduct` |
| `TECHNICAL_PROBLEM` | `7` | `TechnicalProblem` |
| `OTHER` | `8` | `Other` |

**Implementation**: tạo 2 const map trong `services/issue-report.api.ts`:
```ts
export const CATEGORY_TO_NUMBER: Record<IssueCategory, number> = {
  PAYMENT_ISSUE: 0, CONSULTING_ISSUE: 1, /* ... */
};
export const CATEGORY_FROM_BE: Record<string, IssueCategory> = {
  PaymentIssue: "PAYMENT_ISSUE", ConsultingIssue: "CONSULTING_ISSUE", /* ... */
};
```

### IssueReportStatus

| FE constant | BE number | BE response string | Màu UI gợi ý |
|---|---|---|---|
| `NEW` | `0` | `New` | blue |
| `UNDER_REVIEW` | `1` | `UnderReview` | amber |
| `RESOLVED` | `2` | `Resolved` | emerald |
| `DISMISSED` | `3` | `Dismissed` | slate |

**Lưu ý**: BE **không enforce transition** — staff có thể chuyển bất kỳ status nào sang status nào. UI cứ show đủ 4 option.

### relatedEntityType whitelist

| Value | Dùng ở (FE caller) |
|---|---|
| `Mentorship` | `app/startup/mentorship-requests/[id]/page.tsx`, `app/advisor/requests/[id]/page.tsx` |
| `Session` | `app/advisor/schedule/page.tsx` |
| `Payment` | `app/startup/payments/page.tsx` (xem §9 — bị block do payment page mock) |
| `AdvisorReport` | `app/advisor/reports/page.tsx` |
| `Connection` | (chưa có caller — để dành) |
| `User` | (chưa có caller — để dành) |
| `null` | Header bell (4 file `*-header.tsx`) — báo lỗi chung không gắn entity |

---

## 5. Routing & Navigation

### Reporter routes (BE đã handle role detection)

BE gửi `actionUrl` đã có role prefix dựa trên `reporter.UserType`:
- Startup: `/startup/issue-reports/{id}`
- Advisor: `/advisor/issue-reports/{id}`
- Investor: `/investor/issue-reports/{id}`

→ FE phải tạo 3 cặp route (list + detail) cho 3 role. UI nội dung **giống nhau 100%** chỉ khác Shell wrapper.

**Strategy**: tạo 2 shared component, mỗi role page chỉ là wrapper mỏng:
```
components/shared/
  ├── issue-report-list-page.tsx    (shared, nhận `shell` prop hoặc render children)
  └── issue-report-detail-page.tsx  (shared)

app/startup/issue-reports/page.tsx       → <StartupShell><IssueReportListPage /></StartupShell>
app/startup/issue-reports/[id]/page.tsx  → <StartupShell><IssueReportDetailPage id={id} /></StartupShell>
app/advisor/issue-reports/page.tsx       → <AdvisorShell>...</AdvisorShell>
app/advisor/issue-reports/[id]/page.tsx
app/investor/issue-reports/page.tsx      → <InvestorShell>...</InvestorShell>
app/investor/issue-reports/[id]/page.tsx
```

### Staff routes
- `app/staff/issue-reports/page.tsx` — viết lại từ mock thành API thật
- `app/staff/issue-reports/[id]/page.tsx` — tạo mới (chi tiết + đổi status)

---

## 6. Phase 1 — Submit flow chạy thật (Priority: P0)

**Mục tiêu**: User bấm submit → API POST chạy thật → file upload lên Cloudinary → toast success với data thật.

### File 1: rewrite [services/issue-report.api.ts](../services/issue-report.api.ts)

```ts
import axios from "./interceptor";

/* ─── Enums (FE side, dùng cho UI) ──────────────────────── */
export type IssueCategory =
  | "PAYMENT_ISSUE" | "CONSULTING_ISSUE" | "MESSAGING_ISSUE"
  | "OFFER_OR_CONNECTION_ISSUE" | "VERIFICATION_ISSUE" | "DOCUMENT_ISSUE"
  | "HARASSMENT_OR_MISCONDUCT" | "TECHNICAL_PROBLEM" | "OTHER";

export type IssueReportStatus = "NEW" | "UNDER_REVIEW" | "RESOLVED" | "DISMISSED";

export type RelatedEntityType =
  | "Mentorship" | "Session" | "Payment"
  | "AdvisorReport" | "Connection" | "User";

/* ─── Mappings ──────────────────────────────────────────── */
export const CATEGORY_TO_NUMBER: Record<IssueCategory, number> = {
  PAYMENT_ISSUE: 0, CONSULTING_ISSUE: 1, MESSAGING_ISSUE: 2,
  OFFER_OR_CONNECTION_ISSUE: 3, VERIFICATION_ISSUE: 4, DOCUMENT_ISSUE: 5,
  HARASSMENT_OR_MISCONDUCT: 6, TECHNICAL_PROBLEM: 7, OTHER: 8,
};

export const CATEGORY_FROM_BE: Record<string, IssueCategory> = {
  PaymentIssue: "PAYMENT_ISSUE", ConsultingIssue: "CONSULTING_ISSUE",
  MessagingIssue: "MESSAGING_ISSUE", OfferOrConnectionIssue: "OFFER_OR_CONNECTION_ISSUE",
  VerificationIssue: "VERIFICATION_ISSUE", DocumentIssue: "DOCUMENT_ISSUE",
  HarassmentOrMisconduct: "HARASSMENT_OR_MISCONDUCT",
  TechnicalProblem: "TECHNICAL_PROBLEM", Other: "OTHER",
};

export const STATUS_TO_NUMBER: Record<IssueReportStatus, number> = {
  NEW: 0, UNDER_REVIEW: 1, RESOLVED: 2, DISMISSED: 3,
};

export const STATUS_FROM_BE: Record<string, IssueReportStatus> = {
  New: "NEW", UnderReview: "UNDER_REVIEW",
  Resolved: "RESOLVED", Dismissed: "DISMISSED",
};

/* ─── DTOs ──────────────────────────────────────────────── */
export interface IssueReportAttachment {
  attachmentID: number;
  fileUrl: string;
  fileName: string;
  fileSize: number;
  mimeType: string;
}

export interface IssueReportSummaryDto {
  issueReportID: number;
  category: string;        // BE trả PascalCase, FE dùng CATEGORY_FROM_BE để map
  status: string;          // BE trả PascalCase, FE dùng STATUS_FROM_BE
  description: string;
  relatedEntityType: string | null;
  relatedEntityID: number | null;
  submittedAt: string;
  updatedAt: string | null;
}

export interface IssueReportDetailDto extends IssueReportSummaryDto {
  reporterUserID: number;
  reporterName: string;
  staffNote: string | null;
  assignedToStaffID: number | null;
  attachments: IssueReportAttachment[];
}

export interface SubmitInput {
  issueCategory: IssueCategory;
  description: string;
  relatedEntityType?: RelatedEntityType | null;
  relatedEntityID?: number | null;
  attachments?: File[];
}

/* ─── API calls ─────────────────────────────────────────── */

export const SubmitIssueReport = (input: SubmitInput) => {
  const fd = new FormData();
  fd.append("issueCategory", String(CATEGORY_TO_NUMBER[input.issueCategory]));
  fd.append("description", input.description);
  if (input.relatedEntityType) fd.append("relatedEntityType", input.relatedEntityType);
  if (input.relatedEntityID != null) fd.append("relatedEntityID", String(input.relatedEntityID));
  (input.attachments ?? []).slice(0, 5).forEach(file => fd.append("attachments", file));

  return axios.post<IBackendRes<IssueReportSummaryDto>>(`/api/issue-reports`, fd, {
    headers: { "Content-Type": "multipart/form-data" },
  });
};

export const GetMyIssueReports = (params?: {
  page?: number; pageSize?: number;
  status?: IssueReportStatus; category?: IssueCategory;
}) => {
  return axios.get<IBackendRes<IPaginatedRes<IssueReportSummaryDto>>>(
    `/api/issue-reports/me`,
    {
      params: {
        page: params?.page ?? 1,
        pageSize: params?.pageSize ?? 20,
        ...(params?.status && { status: STATUS_TO_NUMBER[params.status] }),
        ...(params?.category && { category: CATEGORY_TO_NUMBER[params.category] }),
      },
    }
  );
};

export const GetIssueReportById = (id: number) =>
  axios.get<IBackendRes<IssueReportDetailDto>>(`/api/issue-reports/${id}`);

export const GetIssueReportsList = (params?: {
  page?: number; pageSize?: number;
  status?: IssueReportStatus; category?: IssueCategory;
  reporterUserId?: number;
}) => {
  return axios.get<IBackendRes<IPaginatedRes<IssueReportDetailDto>>>(
    `/api/issue-reports`,
    {
      params: {
        page: params?.page ?? 1,
        pageSize: params?.pageSize ?? 20,
        ...(params?.status && { status: STATUS_TO_NUMBER[params.status] }),
        ...(params?.category && { category: CATEGORY_TO_NUMBER[params.category] }),
        ...(params?.reporterUserId && { reporterUserId: params.reporterUserId }),
      },
    }
  );
};

export const UpdateIssueReportStatus = (
  id: number,
  body: { status: IssueReportStatus; staffNote?: string }
) => {
  return axios.patch<IBackendRes<IssueReportDetailDto>>(
    `/api/issue-reports/${id}/status`,
    {
      status: STATUS_TO_NUMBER[body.status],
      ...(body.staffNote !== undefined && { staffNote: body.staffNote }),
    }
  );
};
```

### File 2: update [components/shared/issue-report-modal.tsx](../components/shared/issue-report-modal.tsx)

**Sửa interface `IssueReportContext`**:
```ts
export interface IssueReportContext {
  entityType: RelatedEntityType;        // dùng type đã export từ service
  entityId: number;                      // BẮT BUỘC number
  entityTitle: string;
  otherPartyName?: string;
}
```

**Sửa `addFiles`** để enforce max 5:
```ts
const MAX_FILES = 5;
const addFiles = (incoming: FileList | File[]) => {
  const items: AttachedFile[] = Array.from(incoming).map(file => ({
    file, error: validateFile(file)
  }));
  setAttachments(prev => {
    const combined = [...prev, ...items];
    if (combined.length > MAX_FILES) {
      toast.warning(`Chỉ chấp nhận tối đa ${MAX_FILES} file. Các file thừa đã bị bỏ.`);
    }
    return combined.slice(0, MAX_FILES);
  });
};
```

**Sửa `handleSubmit`** để khớp response shape mới:
```ts
const handleSubmit = async () => {
  if (!isValid) return;
  setIsSubmitting(true); setSubmitError("");
  try {
    const res = await SubmitIssueReport({
      issueCategory: category as IssueCategory,
      description,
      attachments: validFiles.map(a => a.file),
      relatedEntityType: context?.entityType ?? null,
      relatedEntityID: context?.entityId ?? null,
    });
    if (res.data?.success) {
      setStep("success");
    } else {
      setSubmitError(res.data?.message || "Gửi báo cáo thất bại.");
      setStep("error");
    }
  } catch (err: any) {
    const msg = err?.response?.data?.message || "Lỗi kết nối. Vui lòng thử lại.";
    setSubmitError(msg);
    setStep("error");
  } finally {
    setIsSubmitting(false);
  }
};
```

**Cập nhật ID hiển thị** ở context badge (line 233): `{context.entityId}` giờ là number, format lại nếu cần (`#${context.entityId}`).

### File 3–7: sửa 5 caller

| File | Đổi `entityType` | Đổi `entityId` |
|---|---|---|
| [app/startup/mentorship-requests/[id]/page.tsx:1444](../app/startup/mentorship-requests/[id]/page.tsx#L1444) | `"CONSULTING_REQUEST"` → `"Mentorship"` | `String(request.mentorshipID)` → `request.mentorshipID` |
| [app/advisor/requests/[id]/page.tsx:1717](../app/advisor/requests/[id]/page.tsx#L1717) | `"CONSULTING_REQUEST"` → `"Mentorship"` | `String(request.mentorshipID)` → `request.mentorshipID` |
| [app/advisor/schedule/page.tsx:94](../app/advisor/schedule/page.tsx#L94) | `"CONSULTING_SESSION"` → `"Session"` | parse về number; nếu `sessionID` đã là number thì bỏ `String()` |
| [app/advisor/reports/page.tsx:178](../app/advisor/reports/page.tsx#L178) | `"CONSULTING_REPORT"` → `"AdvisorReport"` | `report.id` → đảm bảo là number, parse nếu cần |
| [app/startup/payments/page.tsx:142](../app/startup/payments/page.tsx#L142) | **XÓA toàn bộ context** (set `undefined`), xem §9 | — |

### Acceptance Phase 1
- [ ] User submit báo cáo từ header bell → POST chạy → toast success
- [ ] User submit có context (mentorship detail) → BE nhận đúng `relatedEntityType="Mentorship"` + `relatedEntityID=<int>`
- [ ] Upload 6 file → chỉ 5 file được gửi, có warning
- [ ] Network tab: request là `multipart/form-data`, không phải JSON
- [ ] Network tab: `issueCategory` gửi là `0`–`8` (number string), không phải `"PAYMENT_ISSUE"`

---

## 7. Phase 2 — Staff dashboard (Priority: P1)

### File 1: rewrite [app/staff/issue-reports/page.tsx](../app/staff/issue-reports/page.tsx)

Yêu cầu:
- Gọi `GetIssueReportsList({ page, status, category })` thay cho `ISSUES_DATA` mock
- Filter UI: dropdown 1 status + dropdown 1 category (BE không hỗ trợ multi-value)
- Search bar: tạm bỏ, hoặc client-side filter trên page đang load (BE chưa có search)
- Paging: dùng `paging.totalItems` từ response để show "Trang X / Y"
- Mỗi row click → `router.push('/staff/issue-reports/${id}')`
- Map `category` (BE PascalCase) → label tiếng Việt qua `CATEGORY_FROM_BE` + `CATEGORIES` const trong modal
- Map `status` (BE PascalCase) → badge màu

### File 2: tạo `app/staff/issue-reports/[id]/page.tsx`

Yêu cầu:
- Gọi `GetIssueReportById(id)` lúc mount
- Hiển thị: reporter name, category, status (current), description, attachments (link click mở Cloudinary URL trong tab mới), staffNote, submittedAt/updatedAt
- 4 button đổi status (`NEW` / `UNDER_REVIEW` / `RESOLVED` / `DISMISSED`) — disable button của status hiện tại
- Textarea `staffNote` (optional, dùng lại note cũ làm placeholder)
- Confirm dialog trước khi đổi status (đặc biệt `DISMISSED`)
- Sau PATCH thành công, refresh data và toast success
- Handle 403 → redirect về list
- Handle 404 → show empty state

### Acceptance Phase 2
- [ ] Staff thấy report user vừa submit ở Phase 1 (không còn mock data IS-5001)
- [ ] Filter status `New` → chỉ thấy report mới
- [ ] Click row → vào detail, thấy đủ attachment + thông tin reporter
- [ ] Đổi status `New → UnderReview` + ghi note → reload thấy note mới
- [ ] Đổi status không gửi `staffNote` → note cũ vẫn còn (BE giữ nguyên)

---

## 8. Phase 3 — Reporter "My reports" + SignalR (Priority: P2)

### Shared components

Tạo `components/shared/issue-report-list-page.tsx`:
- Props: `roleBaseUrl: string` (vd `/startup/issue-reports`) để build link sang detail
- Gọi `GetMyIssueReports({ page, status, category })`
- UI list giống staff list nhưng bỏ cột reporter (vì là mình)
- Empty state: "Bạn chưa gửi báo cáo nào"

Tạo `components/shared/issue-report-detail-page.tsx`:
- Props: `id: number`
- Gọi `GetIssueReportById(id)` — BE cho phép reporter xem report của chính mình
- Hiển thị: category, status (read-only badge), description, attachments, staffNote (nếu có), timeline submittedAt/updatedAt
- **Read-only** — reporter không sửa được gì
- Handle 403 → "Bạn không có quyền xem báo cáo này"

### Tạo 6 wrapper page

```
app/startup/issue-reports/page.tsx
app/startup/issue-reports/[id]/page.tsx
app/advisor/issue-reports/page.tsx
app/advisor/issue-reports/[id]/page.tsx
app/investor/issue-reports/page.tsx
app/investor/issue-reports/[id]/page.tsx
```

Mỗi page chỉ ~10 dòng, ví dụ:
```tsx
"use client";
import { StartupShell } from "@/components/startup/startup-shell";
import { IssueReportListPage } from "@/components/shared/issue-report-list-page";

export default function Page() {
  return (
    <StartupShell>
      <IssueReportListPage roleBaseUrl="/startup/issue-reports" />
    </StartupShell>
  );
}
```

### SignalR notification

Hook `useNotifications` đã có sẵn, tự động fetch notification mới qua SignalR. Khi user click bell → click vào item → điều hướng theo `actionUrl`.

BE đã confirm payload notification có `actionUrl` đúng role prefix → **không cần code thêm gì cho routing**.

**Polish UX (optional)**: trong `getNotificationIcon` ở [app/startup/notifications/page.tsx:24](../app/startup/notifications/page.tsx#L24) (và 2 file tương tự cho advisor/investor nếu có), thêm case:
```ts
// Trước default
if (item.relatedEntityType === "IssueReport") {
  return <ShieldAlert className="w-4 h-4 text-amber-500" />;
}
```
*(Cần check `relatedEntityType` được expose từ `INotificationItem` type chưa — nếu chưa thì add field vào type def)*

### Acceptance Phase 3
- [ ] Startup user vào `/startup/issue-reports` thấy list report của mình
- [ ] Click bell sau khi staff đổi status → điều hướng đúng `/startup/issue-reports/{id}`
- [ ] Notification icon cho IssueReport hiển thị `ShieldAlert` thay vì icon SYSTEM mặc định
- [ ] Investor/Advisor cũng hoạt động tương tự

### Add link vào sidebar

Thêm menu "Báo cáo của tôi" vào sidebar 3 role, trỏ về `/{role}/issue-reports`. Vị trí gợi ý: gần menu "Cài đặt" hoặc "Thông báo".

---

## 9. Known limitations & Out-of-scope

### Payment page chưa tích hợp API thật
[app/startup/payments/page.tsx](../app/startup/payments/page.tsx) đang dùng `MOCK_PAYMENTS` với `id = "p1"`, không có `PaymentID` numeric.

→ **Tạm thời ở Phase 1, khi mở modal từ trang payments, KHÔNG truyền context** (set `setIssueContext(undefined)` thay vì truyền `entityType: "Payment"`). User vẫn báo cáo được, chỉ là không liên kết tới payment cụ thể.

→ Khi nào có ticket riêng tích hợp API list Payment thật từ BE (response có `paymentID: number`), mới bật lại context với `entityType: "Payment"` + `entityID: payment.paymentID`.

### Workflow status transition
BE không enforce → FE cho phép đổi tự do. Nếu sau này business muốn ràng buộc (vd không cho chuyển từ `Resolved` về `New`), cần discuss lại.

### Multi-value filter
BE chỉ hỗ trợ filter 1 giá trị/lần cho `status` và `category`. Nếu UX cần multi-select sau này, ping BE để mở rộng.

### Reporter không xóa/sửa được report
Spec BE không có endpoint DELETE/PUT cho report. Đây là intentional (audit trail). Đừng add nút "Xóa" ở reporter detail page.

---

## 10. Validation checklist trước khi merge

### Code quality
- [ ] Không còn `// TODO: replace with real API call` trong issue-report.api.ts
- [ ] Không còn `setTimeout` mock trong service
- [ ] Toàn bộ enum đi qua `CATEGORY_TO_NUMBER` / `STATUS_TO_NUMBER` (không hardcode số `0`, `1`...)
- [ ] Type-check pass: `npx tsc --noEmit`

### Functional smoke test
- [ ] Submit report không context (header bell) ở 3 role → BE nhận, staff thấy
- [ ] Submit report có context (mentorship detail) → `relatedEntityID` đúng PK
- [ ] Upload 5 file 5MB mỗi file → success
- [ ] Upload file 11MB → reject phía FE
- [ ] Upload file .exe → reject phía FE
- [ ] Staff đổi status không note → BE giữ note cũ
- [ ] Reporter mở report của user khác → 403 handled

### Edge cases
- [ ] BE trả `{ success: false, message: "..." }` → modal show error step với message
- [ ] Network down → modal show "Lỗi kết nối. Vui lòng thử lại."
- [ ] Submit double-click → button disabled khi `isSubmitting`

---

## 11. Suggested commit breakdown

```
1. feat(issue-report): rewrite service with real API + enum mappings
2. feat(issue-report): update modal to send number entityId + max 5 files
3. fix(issue-report): map entityType to BE whitelist in 4 callers
4. fix(issue-report): drop context from payments page (mock data)
5. feat(staff): rewrite issue-reports list with real API
6. feat(staff): add issue-report detail page with status update
7. feat(issue-report): shared list/detail components + 6 reporter routes
8. feat(notification): IssueReport icon + sidebar links for 3 roles
```

---

## 12. Câu hỏi escalate cho BE (nếu phát sinh khi code)

1. Format error response chi tiết khi gửi sai enum (vd `issueCategory: "abc"`)? FE cần biết để show message tiếng Việt phù hợp.
2. Có rate limit khi submit không? Nếu có, FE nên throttle button.
3. SignalR event name là gì? (Hiện đoán dùng chung event với notification system, không cần subscribe riêng. Cần confirm.)
