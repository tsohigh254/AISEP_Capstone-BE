# Payment Ops - Implementation Guide

> **Trạng thái**: Chờ implement
> **Scope**: Wire `/staff/payment-ops` và `/staff/payment-ops/[id]` từ mock sang real API
> **Nguồn sự thật**: Contract BE đã được chốt trong phần "BE Confirmed"

---

## Tổng quan

Trang `/staff/payment-ops` hiện tại là mock 100%. Mục tiêu là chuyển sang dữ liệu thật cho payout flow của Staff.

Flow nghiệp vụ sau khi BE chốt:

- `GET /api/mentorships?isPayoutEligible=true` trả **cả mentorship pending payout và đã release payout**
- `isPayoutEligible` **vẫn giữ nguyên `true`** sau khi release
- FE phải dùng `payoutReleasedAt` để tách tab:

```text
payoutReleasedAt === null  ->  Tab "Chờ giải ngân"
payoutReleasedAt !== null  ->  Tab "Đã giải ngân"
```

---

## BE Confirmed

### Endpoints

| Method | URL | Dùng cho |
|--------|-----|----------|
| `GET` | `/api/mentorships?isPayoutEligible=true` | Lấy danh sách mentorship đủ điều kiện payout |
| `GET` | `/api/mentorships/{id}` | Lấy chi tiết 1 mentorship |
| `POST` | `/api/mentorships/{id}/release-payout` | Staff kích hoạt giải ngân |

### Pagination của list endpoint

- Có phân trang
- Default: `page=1`, `pageSize=20`
- Max `pageSize=100`
- Nếu FE cần KPI tổng trên list page:
  - ưu tiên gọi `page=1&pageSize=100`
  - có thể dùng `paging.totalItems` cho tổng count nếu phù hợp
  - không nên loop nhiều trang chỉ để tính KPI

### Field chuẩn cho amount

- `actualAmount` là số tiền Advisor thực nhận
- `sessionAmount` là giá gốc Startup trả
- `creditedAmount` chỉ có trong response của `release-payout`, không có trong list endpoint

=> UI payout phải dùng `actualAmount`, không dùng `sessionAmount`

### Điều kiện "report đã được duyệt"

- Không dùng `hasReport`
- Không dùng `reportCount`
- Field đúng là `reportReviewStatus == "Passed"` trên từng report

### Trạng thái canonical

- Field canonical là `status`
- Giá trị hoàn thành đúng là `"Completed"` (PascalCase)

### Xử lý `PAYOUT_ALREADY_RELEASED`

- BE không trả payload
- Error message có chứa timestamp lần release đầu
- Nếu FE muốn xử lý **silent success**, cách đúng là:
  1. bắt lỗi `PAYOUT_ALREADY_RELEASED`
  2. không toast error
  3. gọi lại `GET /api/mentorships/{id}`
  4. lấy `payoutReleasedAt` thật từ detail response để update UI

### Bank info

- `GET /api/mentorships/{id}` không trả bank info
- `GET /api/wallets/me` chỉ dành cho Advisor
- Staff hiện **không có endpoint** xem bank info của Advisor

=> Detail page của Staff **không nên hiển thị bank account/bank name**

---

## Current Repo Reality

### Đã có sẵn trong repo

- `GetAdvisorMentorshipById` đã tồn tại ở `services/advisor/advisor.api.ts`
- `IMentorshipRequest` hiện đã có:
  - `sessionAmount?: number | null`
  - `hasReport?: boolean`
  - `reportCount?: number`
  - `completionConfirmedByStartup: boolean`
  - `status`
  - `mentorshipStatus`
- `isPayoutEligible` hiện chỉ có trong `ISessionOversightResult`, chưa có trong `IMentorshipRequest`
- `payoutReleasedAt` chưa có trong `IMentorshipRequest`
- `actualAmount` chưa có trong `IMentorshipRequest`
- 2 page payment ops hiện vẫn là mock hoàn toàn

### Hệ quả cho FE

- Cần update type trước khi wire page
- Cần thêm API function riêng cho payout list và release payout
- Cần bỏ toàn bộ UI mock liên quan đến refund, hold, reject, pin confirm và bank info

---

## Step 1 - Update Types

**File**: `types/startup-mentorship.ts`

### 1a. Thêm field vào `IMentorshipRequest`

```typescript
export interface IMentorshipRequest {
  // ...existing fields
  sessionAmount?: number | null;
  actualAmount?: number | null;
  paymentStatus?: string | null;
  paidAt?: string | null;
  isPayoutEligible?: boolean;
  payoutReleasedAt?: string | null;
  // ...existing fields
}
```

### 1b. Thêm type cho release payout response

```typescript
export interface IReleasePayoutResult {
  mentorshipID: number;
  creditedAmount: number;
  payoutReleasedAt: string;
  isPayoutEligible: boolean;
  releasedByStaffID: number;
}
```

### 1c. Nếu detail response trả review status trong từng report

Guide này assume BE trả `reportReviewStatus` trên từng report để FE check điều kiện pass.

Nếu TypeScript chưa có field đó, thêm vào report type đang được `IMentorshipRequest.reports` sử dụng:

```typescript
reportReviewStatus?: ReportReviewStatus | string | null;
```

Lưu ý: trong file hiện tại có type report bị định nghĩa lặp; khi implement cần thêm đúng vào interface đang được detail response dùng thực tế.

---

## Step 2 - Add API Functions

**File**: `services/staff/consulting-oversight.api.ts`

Thêm import:

```typescript
import type { IMentorshipRequest, IReleasePayoutResult } from "@/types/startup-mentorship";
```

Thêm API functions:

```typescript
export interface IPayoutEligibleMentorshipParams {
  page?: number;
  pageSize?: number;
}

export const GetPayoutEligibleMentorships = (
  params: IPayoutEligibleMentorshipParams = { page: 1, pageSize: 100 }
) => {
  return axios.get<IBackendRes<IPagingData<IMentorshipRequest>>>(
    "/api/mentorships",
    {
      params: {
        isPayoutEligible: true,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 100,
      },
    }
  );
};

export const ReleasePayout = (mentorshipId: number | string) => {
  return axios.post<IBackendRes<IReleasePayoutResult>>(
    `/api/mentorships/${mentorshipId}/release-payout`
  );
};
```

---

## Step 3 - List Page

**File**: `app/staff/payment-ops/page.tsx`

### 3a. Xóa toàn bộ mock

- Xóa `PAYMENTS_DATA`
- Xóa `STATUS_CFG`
- Xóa `ELIGIBILITY_CFG`
- Xóa toàn bộ UI/filter dành cho refund
- Xóa hoặc thay subtitle đang nói cả payout và refund
- Xóa widget `Liquidity` nếu chưa có API thật

### 3b. State cần có

```typescript
const [data, setData] = useState<IMentorshipRequest[]>([]);
const [loading, setLoading] = useState(true);
const [activeTab, setActiveTab] = useState<"PENDING" | "RELEASED">("PENDING");
const [search, setSearch] = useState("");
const [paging, setPaging] = useState<{ totalItems?: number; page?: number; pageSize?: number }>({});
```

### 3c. Fetch list

```typescript
useEffect(() => {
  const fetch = async () => {
    setLoading(true);
    try {
      const res = await GetPayoutEligibleMentorships({ page: 1, pageSize: 100 });
      const payload = res.data?.data;
      const items = payload?.items ?? [];

      setData(items);
      setPaging({
        totalItems: payload?.totalItems,
        page: payload?.page,
        pageSize: payload?.pageSize,
      });
    } catch (err) {
      console.error("Failed to fetch payout list", err);
    } finally {
      setLoading(false);
    }
  };

  fetch();
}, []);
```

### 3d. Split tabs bằng `payoutReleasedAt`

```typescript
const pendingItems = useMemo(
  () => data.filter((item) => !item.payoutReleasedAt),
  [data]
);

const releasedItems = useMemo(
  () => data.filter((item) => !!item.payoutReleasedAt),
  [data]
);
```

### 3e. Search logic

```typescript
const displayedItems = useMemo(() => {
  const base = activeTab === "PENDING" ? pendingItems : releasedItems;
  const q = search.trim().toLowerCase();

  if (!q) return base;

  return base.filter((item) =>
    item.advisorName?.toLowerCase().includes(q) ||
    item.startupName?.toLowerCase().includes(q) ||
    String(item.mentorshipID).includes(q)
  );
}, [activeTab, pendingItems, releasedItems, search]);
```

### 3f. KPI cards

Chỉ giữ KPI nào tính đúng từ dataset hiện có:

```typescript
const stats = useMemo(() => ({
  pendingCount: pendingItems.length,
  releasedCount: releasedItems.length,
  pendingAmount: pendingItems.reduce((sum, item) => sum + (item.actualAmount ?? 0), 0),
  releasedAmount: releasedItems.reduce((sum, item) => sum + (item.actualAmount ?? 0), 0),
  totalEligibleCount: paging.totalItems ?? data.length,
}), [pendingItems, releasedItems, paging.totalItems, data.length]);
```

Lưu ý:

- Dùng `actualAmount`
- Không dùng `sessionAmount`
- Nếu API chỉ trả 100 item đầu thì KPI amount vẫn chỉ đúng trong phạm vi dataset đã fetch; guide này assume list đủ nhỏ để `pageSize=100` cover được thực tế

### 3g. Tabs thay cho dropdown filters

Chỉ cần 2 tab:

- `Chờ giải ngân`
- `Đã giải ngân`

Không còn các filter:

- loại giao dịch
- eligibility mock
- status mock

### 3h. Table mapping

| Cột | Field |
|---|---|
| Mentorship ID | `item.mentorshipID` |
| Advisor | `item.advisorName` |
| Startup | `item.startupName` |
| Số tiền | `item.actualAmount` |
| Trạng thái | `item.payoutReleasedAt` |
| Thao tác | `/staff/payment-ops/${item.mentorshipID}` |

Badge trạng thái:

```tsx
{item.payoutReleasedAt ? (
  <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-[11px] font-semibold border bg-emerald-50 text-emerald-700 border-emerald-200">
    <CheckCircle2 className="w-3 h-3" />
    Đã giải ngân
  </span>
) : (
  <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-[11px] font-semibold border bg-blue-50 text-blue-700 border-blue-200">
    <Clock className="w-3 h-3" />
    Chờ giải ngân
  </span>
)}
```

### 3i. Loading và empty state

- Skeleton nên khớp số cột thật của table
- Empty state text tách riêng theo tab

---

## Step 4 - Detail Page

**File**: `app/staff/payment-ops/[id]/page.tsx`

### 4a. Xóa toàn bộ mock

- Xóa object `payment`
- Xóa `STATUS_CFG`
- Xóa `ELIGIBILITY_CFG`
- Xóa `handleAction`
- Xóa button `Tạm giữ (Hold) Payout`
- Xóa button `Từ chối thanh toán`
- Xóa widget `Mã pin xác nhận`
- Xóa toàn bộ card bank info vì Staff không có endpoint xem bank info

### 4b. State và fetch

```typescript
const { id } = use(params);
const [mentorship, setMentorship] = useState<IMentorshipRequest | null>(null);
const [loading, setLoading] = useState(true);
const [releasing, setReleasing] = useState(false);
```

```typescript
useEffect(() => {
  const fetch = async () => {
    setLoading(true);
    try {
      const res = await GetAdvisorMentorshipById(id);
      if (res.data?.data) setMentorship(res.data.data);
    } catch (err) {
      console.error("Failed to load mentorship", err);
    } finally {
      setLoading(false);
    }
  };

  fetch();
}, [id]);
```

### 4c. Điều kiện checklist

```typescript
const conditions = mentorship ? [
  {
    label: "Buổi tư vấn đã hoàn thành",
    ok: mentorship.status === "Completed",
  },
  {
    label: "Có ít nhất 1 báo cáo đã được duyệt",
    ok: (mentorship.reports ?? []).some(
      (report) => report.reportReviewStatus === "Passed"
    ),
  },
  {
    label: "Đủ điều kiện giải ngân",
    ok: mentorship.isPayoutEligible === true,
  },
  {
    label: "Startup đã xác nhận hoàn thành",
    ok: mentorship.completionConfirmedByStartup === true,
  },
] : [];
```

Lưu ý:

- Không dùng `hasReport` để đại diện cho report pass
- Không dùng `reportCount` để suy ra report pass
- Field canonical để check completed là `status === "Completed"`

### 4d. Xử lý release payout

```typescript
const fetchMentorship = async () => {
  const res = await GetAdvisorMentorshipById(id);
  if (res.data?.data) setMentorship(res.data.data);
};

const handleReleasePayout = async () => {
  if (!mentorship) return;

  setReleasing(true);
  try {
    const res = await ReleasePayout(mentorship.mentorshipID);

    if (res.data?.data) {
      setMentorship((prev) =>
        prev
          ? {
              ...prev,
              payoutReleasedAt: res.data.data.payoutReleasedAt,
            }
          : prev
      );
    }
  } catch (err: any) {
    const errorCode =
      err?.response?.data?.error?.code ||
      err?.response?.data?.message ||
      "";

    if (
      errorCode === "PAYOUT_ALREADY_RELEASED" ||
      String(errorCode).includes("ALREADY_RELEASED")
    ) {
      await fetchMentorship();
    } else {
      console.error("Failed to release payout", err);
      // TODO: show error toast
    }
  } finally {
    setReleasing(false);
  }
};
```

Nguyên tắc:

- `PAYOUT_ALREADY_RELEASED` là silent success
- Không tự fake timestamp ở FE
- Refetch detail để lấy `payoutReleasedAt` thật

### 4e. Guard nút giải ngân

```tsx
{mentorship?.payoutReleasedAt ? (
  <div className="w-full flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-emerald-500/10 text-emerald-600 text-[13px] font-semibold border border-emerald-200">
    <CheckCircle2 className="w-4 h-4" />
    Đã giải ngân
    <span className="text-[11px] text-emerald-500 ml-1">
      {new Date(mentorship.payoutReleasedAt).toLocaleDateString("vi-VN")}
    </span>
  </div>
) : (
  <button
    onClick={handleReleasePayout}
    disabled={releasing || mentorship?.isPayoutEligible !== true}
    className="w-full flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-emerald-500 text-white text-[13px] font-semibold hover:bg-emerald-600 transition-all active:scale-[0.98] disabled:opacity-50"
  >
    {releasing ? <Loader2 className="w-4 h-4 animate-spin" /> : <CheckCircle2 className="w-4 h-4" />}
    Xác nhận & Giải ngân
  </button>
)}
```

---

## UI Decisions Chốt Theo Contract Mới

- List page là payout-only
- Không còn refund UX trong payment ops
- Không hiển thị bank account của advisor ở staff detail
- Không hiển thị hold/reject action vì BE không có endpoint
- Không hiển thị pin confirm vì chỉ là mock UI

---

## Thứ tự implement

1. Update types
2. Thêm API functions
3. Wire list page
4. Dọn lại KPI/header để bỏ toàn bộ wording và widget mock liên quan refund/liquidity
5. Wire detail page
6. Test case:
   - mentorship pending payout
   - mentorship đã release
   - click release thành công
   - click release khi BE trả `PAYOUT_ALREADY_RELEASED`

---

## Những gì đã được chốt, không cần hỏi lại BE

- Split tab bằng `payoutReleasedAt`
- `isPayoutEligible` không đổi sau release
- Amount chuẩn là `actualAmount`
- Report pass dùng `reportReviewStatus == "Passed"`
- Completed check dùng `status == "Completed"`
- Staff không có quyền xem bank info của advisor

---

## Điểm cần lưu ý khi implement

- `GetAdvisorMentorshipById` hiện đang nằm ở `services/advisor/advisor.api.ts`; staff có thể dùng chung endpoint đó
- List endpoint có paging, nên đừng assume `res.data.data` là array thuần
- Nếu type report hiện chưa có `reportReviewStatus`, phải bổ sung trước khi dùng ở detail page
- Nếu sau này số item payout eligible vượt 100, KPI amount ở list page sẽ cần BE hỗ trợ aggregate hoặc endpoint summary riêng
