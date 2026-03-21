# Hướng dẫn triển khai: Hiển thị hồ sơ Startup & Investor

> Routes mục tiêu:
> - `http://localhost:3000/investor/startups` — Investor tìm kiếm Startup
> - `http://localhost:3000/startup/investors` — Startup tìm kiếm Investor

---

## Trạng thái hiện tại

| Route | Trang tồn tại | API thật | Ghi chú |
|-------|--------------|----------|---------|
| `/investor/startups` | ✅ | ✅ Một phần | `SearchStartups` đã gọi thật, nhưng thiếu filter keyword/industry/stage |
| `/investor/startups/[id]` | ✅ | ⚠️ `GetStartupById` có nhưng page dùng mock | Cần nối API |
| `/startup/investors` | ✅ | ❌ Mock data cứng | `investors[]` là mảng cứng trong file |
| `/startup/investors/[id]` | ✅ | ❌ Mock data | Cần `GetInvestorById` từ BE |

**Vấn đề cốt lõi:** `services/startup/startup.api.ts` **rỗng hoàn toàn** — startup không có hàm nào để gọi danh sách investor hay hồ sơ investor.

---

## Phần 1: Investor xem danh sách Startup — `/investor/startups`

### 1.1 API Backend (.NET) cần có

```http
GET /api/investors/search
  ?page=1
  &pageSize=20
  &keyword=string        (tên công ty hoặc mô tả)
  &industry=string       (lọc theo ngành)
  &stage=string          (Idea / Pre-Seed / Seed / Series A / ...)
  &fundingStage=string   (vòng gọi vốn hiện tại)
  &sortBy=string         (relevance | recent | aiScore | lowRisk)
```

Response:
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "data": {
    "items": [
      {
        "startupID": 1,
        "companyName": "TechVN",
        "oneLiner": "AI-powered logistics platform",
        "stage": "Seed",
        "industry": "AI & Machine Learning",
        "subIndustry": "Logistics",
        "location": "Hồ Chí Minh",
        "country": "Vietnam",
        "logoURL": "https://...",
        "fundingStage": "Seed",
        "profileStatus": "Active",
        "updatedAt": "2025-03-01T..."
      }
    ],
    "paging": {
      "page": 1,
      "pageSize": 20,
      "totalItems": 120,
      "totalPages": 6
    }
  }
}
```

> **Nếu BE chưa có filter params** — tạm thời gọi không params và filter phía FE.

---

### 1.2 Cập nhật `services/investor/investor.api.ts`

Thêm params filter vào `SearchStartups`:

```typescript
export interface ISearchStartupsParams {
  page?: number;
  pageSize?: number;
  keyword?: string;
  industry?: string;
  stage?: string;
  fundingStage?: string;
  sortBy?: string;
}

export const SearchStartups = (params: ISearchStartupsParams = {}) => {
  return axios.get<IBackendRes<IPaginatedRes<IStartupSearchItem>>>(
    `/api/investors/search`,
    { params: { page: 1, pageSize: 20, ...params } }
  );
};
```

---

### 1.3 Cập nhật `app/investor/startups/page.tsx`

**Thay đổi cần làm:**

1. Thêm state cho filter params
2. Truyền filter vào `SearchStartups`
3. Xử lý loading/error state

```typescript
// Thêm state
const [keyword, setKeyword] = useState("");
const [selectedIndustry, setSelectedIndustry] = useState("Tất cả lĩnh vực");
const [selectedStage, setSelectedStage] = useState("Tất cả giai đoạn");
const [sortBy, setSortBy] = useState("relevance");

// Gọi API với filter
const fetchStartups = useCallback(async () => {
  setIsLoading(true);
  try {
    const res = await SearchStartups({
      page: currentPage,
      pageSize: 12,
      keyword: keyword || undefined,
      industry: selectedIndustry === "Tất cả lĩnh vực" ? undefined : selectedIndustry,
      stage: selectedStage === "Tất cả giai đoạn" ? undefined : selectedStage,
      sortBy,
    }) as any as IBackendRes<IPaginatedRes<IStartupSearchItem>>;

    if (res.success && res.data) {
      setStartups(res.data.items);
      setTotalPages(res.data.paging.totalPages);
    }
  } finally {
    setIsLoading(false);
  }
}, [currentPage, keyword, selectedIndustry, selectedStage, sortBy]);

useEffect(() => { fetchStartups(); }, [fetchStartups]);
```

---

### 1.4 `app/investor/startups/[id]/page.tsx` — Nối `GetStartupById`

```typescript
// Thay mock data bằng:
useEffect(() => {
  const load = async () => {
    const res = await GetStartupById(Number(params.id)) as any as IBackendRes<IStartupSearchItem>;
    if (res.success && res.data) setStartup(res.data);
  };
  load();
}, [params.id]);
```

> **Lưu ý:** `IStartupSearchItem` chỉ có thông tin cơ bản. Nếu trang detail cần thêm (AI score, mô tả dài, documents...), BE cần endpoint `GET /api/investors/startups/{id}` trả về object đầy đủ hơn với type `IStartupDetail`.

---

## Phần 2: Startup xem danh sách Investor — `/startup/investors`

### 2.1 API Backend (.NET) cần có

**Danh sách investor (có thể tìm kiếm & lọc):**
```http
GET /api/startups/investors
  ?page=1
  &pageSize=12
  &keyword=string         (tên hoặc mô tả)
  &investorType=string    (Institutional | Individual)
  &stage=string           (Seed / Series A / ...)
  &industry=string        (Fintech / AI / ...)
  &sortBy=string          (matchScore | recent)
```

**Chi tiết một investor:**
```http
GET /api/startups/investors/{id}
```

**Danh sách connection đã gửi:**
```http
GET /api/connections/sent?page=1&pageSize=50
```
> Đã có trong `connection.api.ts` — `GetSentConnections()`

---

### 2.2 Response types cần thêm vào `types/global.ts`

```typescript
// Investor hiển thị trên trang tìm kiếm (từ góc startup)
interface IInvestorSearchItem {
  investorID: number;
  fullName: string;
  firmName: string;
  investorType: "Institutional" | "Individual";
  title: string;
  bio: string;
  profilePhotoURL: string;
  preferredIndustries: string[];
  preferredStages: string[];
  preferredGeographies: string[];
  ticketSizeMin?: number;
  ticketSizeMax?: number;
  portfolioCount?: number;
  acceptingConnections: boolean;
  location: string;
  country: string;
  linkedInURL: string;
  website: string;
  updatedAt: string;
}

// Params tìm kiếm investor
interface ISearchInvestorsParams {
  page?: number;
  pageSize?: number;
  keyword?: string;
  investorType?: string;
  stage?: string;
  industry?: string;
  sortBy?: string;
}
```

---

### 2.3 Tạo `services/startup/startup.api.ts`

File này hiện tại **rỗng hoàn toàn**. Cần viết đầy đủ:

```typescript
import axios from "../interceptor";

// ── Startup xem danh sách Investor ──

export const SearchInvestors = (params: ISearchInvestorsParams = {}) => {
  return axios.get<IBackendRes<IPaginatedRes<IInvestorSearchItem>>>(
    `/api/startups/investors`,
    { params: { page: 1, pageSize: 12, ...params } }
  );
};

export const GetInvestorById = (id: number) => {
  return axios.get<IBackendRes<IInvestorProfile>>(`/api/startups/investors/${id}`);
};

// ── Startup Profile ──

export const GetStartupProfile = () => {
  return axios.get<IBackendRes<IStartupProfile>>(`/api/startups/me`);
};

export const CreateStartupProfile = (data: FormData) => {
  return axios.post<IBackendRes<IStartupProfile>>(`/api/startups`, data, {
    headers: { "Content-Type": "multipart/form-data" },
  });
};

export const UpdateStartupProfile = (data: FormData) => {
  return axios.put<IBackendRes<IStartupProfile>>(`/api/startups/me`, data, {
    headers: { "Content-Type": "multipart/form-data" },
  });
};
```

---

### 2.4 Cập nhật `app/startup/investors/page.tsx` — Xóa mock data

**Trước:** `const investors = [{ id: 1, name: "VinaCapital..." }, ...]` — mảng cứng trong file.

**Sau:**

```typescript
import { SearchInvestors } from "@/services/startup/startup.api";
import { GetSentConnections } from "@/services/connection/connection.api";

// State
const [investors, setInvestors] = useState<IInvestorSearchItem[]>([]);
const [sentConnections, setSentConnections] = useState<IConnectionItem[]>([]);
const [isLoading, setIsLoading] = useState(false);
const [totalPages, setTotalPages] = useState(1);

// Load investors
const fetchInvestors = useCallback(async () => {
  setIsLoading(true);
  try {
    const res = await SearchInvestors({
      page: currentPage,
      pageSize: 12,
      keyword: keyword || undefined,
      investorType: selectedType === "Tất cả" ? undefined : selectedType,
      stage: selectedStage === "Tất cả giai đoạn" ? undefined : selectedStage,
    }) as any as IBackendRes<IPaginatedRes<IInvestorSearchItem>>;

    if (res.success && res.data) {
      setInvestors(res.data.items);
      setTotalPages(res.data.paging.totalPages);
    }
  } finally {
    setIsLoading(false);
  }
}, [currentPage, keyword, selectedType, selectedStage]);

// Load sent connections (cho tab "Yêu cầu đã gửi" và kiểm tra trạng thái)
const fetchSentConnections = useCallback(async () => {
  const res = await GetSentConnections(1, 100) as any as IBackendRes<IPaginatedRes<IConnectionItem>>;
  if (res.success && res.data) {
    setSentConnections(res.data.items);
  }
}, []);

useEffect(() => {
  fetchInvestors();
  fetchSentConnections();
}, [fetchInvestors, fetchSentConnections]);

// Kiểm tra trạng thái connection của 1 investor
const getConnectionStatus = (investorId: number): IConnectionItem | undefined => {
  return sentConnections.find(c => c.investorID === investorId);
};
```

---

### 2.5 Cập nhật `app/startup/investors/[id]/page.tsx` — Nối `GetInvestorById`

```typescript
import { GetInvestorById } from "@/services/startup/startup.api";
import { GetConnectionByInvestorId } from "@/services/connection/connection.api";

const [investor, setInvestor] = useState<IInvestorProfile | null>(null);
const [connection, setConnection] = useState<IConnectionItem | null>(null);
const [isLoading, setIsLoading] = useState(true);

useEffect(() => {
  const load = async () => {
    setIsLoading(true);
    try {
      const [investorRes, conn] = await Promise.all([
        GetInvestorById(Number(params.id)) as any as Promise<IBackendRes<IInvestorProfile>>,
        GetConnectionByInvestorId(Number(params.id)),
      ]);
      if (investorRes.success && investorRes.data) setInvestor(investorRes.data);
      setConnection(conn);
    } finally {
      setIsLoading(false);
    }
  };
  load();
}, [params.id]);
```

---

## Phần 3: Nút kết nối và flow Chat

### 3.1 Logic nút hành động trên trang `/startup/investors/[id]`

Dựa vào `connection.connectionStatus`:

```typescript
const renderActionButton = () => {
  if (!connection) {
    return (
      <Button onClick={() => setIsModalOpen(true)}>
        Gửi lời mời kết nối
      </Button>
    );
  }

  switch (connection.connectionStatus) {
    case "Pending":
      return (
        <Button variant="outline" onClick={() => handleWithdraw(connection.connectionID)}>
          Đã gửi lời mời — Rút lại
        </Button>
      );
    case "Accepted":
      return (
        <Button onClick={() => router.push(`/startup/messaging?connectionId=${connection.connectionID}`)}>
          <MessageSquare className="w-4 h-4 mr-2" />
          Nhắn tin
        </Button>
      );
    case "Rejected":
      return (
        <Button variant="outline" disabled>
          Đã từ chối
        </Button>
      );
    default:
      return null;
  }
};
```

### 3.2 Flow sau khi gửi connection request thành công

```
Startup → click "Gửi lời mời" → InvestorConnectionModal mở
  → điền message → submit → CreateConnection(data) → API trả về IConnectionItem
  → onSuccess(connectionItem) callback
  → cập nhật local state: setConnection(connectionItem)
  → nút đổi sang "Đã gửi lời mời — Rút lại"
```

### 3.3 Flow khi connection được Accept → vào Chat

```
Investor Accept connection (trên trang /investor/connections)
  → connection.connectionStatus = "Accepted"

Startup vào /startup/investors/[id]
  → getConnectionByInvestorId → trả về connection với status "Accepted"
  → nút hiện: "Nhắn tin"
  → click → router.push("/startup/messaging?connectionId=xxx")

Trang /startup/messaging
  → đọc query param connectionId
  → GetConversation(connectionId) hoặc CreateConversation({ connectionId })
  → load SignalR hub → useChat(conversationId)
```

---

## Phần 4: Thứ tự triển khai

```
Bước 1 ─ BE cần có trước
  □ GET /api/startups/investors           (danh sách)
  □ GET /api/startups/investors/{id}      (chi tiết)
  □ GET /api/investors/search có params   (keyword, industry, stage, sortBy)

Bước 2 ─ FE: Types & Service
  □ Thêm IInvestorSearchItem, ISearchInvestorsParams vào types/global.ts
  □ Viết services/startup/startup.api.ts  (SearchInvestors, GetInvestorById)
  □ Cập nhật SearchStartups params trong investor.api.ts

Bước 3 ─ FE: Pages
  □ /startup/investors/page.tsx           — xóa mock, gọi SearchInvestors
  □ /startup/investors/[id]/page.tsx      — gọi GetInvestorById + GetConnectionByInvestorId
  □ /investor/startups/page.tsx           — thêm filter params vào SearchStartups
  □ /investor/startups/[id]/page.tsx      — gọi GetStartupById thay mock

Bước 4 ─ FE: Connection & Chat
  □ Nút hành động dựa theo connectionStatus
  □ InvestorConnectionModal → onSuccess cập nhật state
  □ "Nhắn tin" → redirect sang /startup/messaging?connectionId=x
```

---

## Phần 5: Mapping field API ↔ UI

### Card Investor trên `/startup/investors`

| UI hiển thị | Field từ `IInvestorSearchItem` |
|-------------|-------------------------------|
| Avatar | `profilePhotoURL` |
| Tên | `fullName` |
| Tiêu đề | `title` (e.g. "Managing Partner tại Alpha VC") |
| Tags ngành | `preferredIndustries[0..2]` |
| Ticket size | `ticketSizeMin` – `ticketSizeMax` (format: `$500k–2M`) |
| Trạng thái | Từ `sentConnections.find(c => c.investorID === id)` |

### Card Startup trên `/investor/startups`

| UI hiển thị | Field từ `IStartupSearchItem` |
|-------------|------------------------------|
| Logo | `logoURL` |
| Tên | `companyName` |
| Mô tả ngắn | `oneLiner` |
| Giai đoạn | `stage` |
| Ngành | `industry` |
| Địa điểm | `location` |
| AI Score | Không có trong search item — cần endpoint riêng hoặc BE thêm field |

---

## Lưu ý quan trọng

1. **`GetConnectionByInvestorId`** hiện tại lấy tối đa 100 connections để tìm theo investorId — nếu startup có nhiều connections, BE nên có endpoint `GET /api/connections/sent?investorId={id}` để query chính xác.

2. **AI Score** trên card startup — hiện BE chưa trả về trong `SearchStartups`. Cần confirm với BE team: field `aiScore` hoặc `matchScore` có trong search response không.

3. **`acceptingConnections: false`** trên IInvestorProfile — ẩn nút "Gửi lời mời" nếu investor không nhận kết nối.

4. **Phân quyền** — `SearchInvestors` chỉ được gọi khi user có role `Startup`. Interceptor đã gắn Bearer token, BE cần kiểm tra role và trả 403 nếu sai.
