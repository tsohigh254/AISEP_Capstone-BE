# AISEP — Startup Profile: Backend API Specification

> Tài liệu dành cho **Backend Team** — mô tả chi tiết tất cả API endpoints, cấu trúc request/response, database schema gợi ý, và luồng dữ liệu mà Frontend đang gọi cho tính năng **Startup Profile**.

---

## 1. Tổng quan tính năng

Startup Profile cho phép user (role `Startup`) tạo, chỉnh sửa, và quản lý hồ sơ startup của mình trên nền tảng AISEP, bao gồm:

- **Thông tin cơ bản**: Tên công ty, tagline, mô tả, ngành nghề, giai đoạn, logo, website
- **Tài chính**: Số vốn cần gọi, vốn đã huy động, định giá
- **Đội ngũ**: Danh sách thành viên (CRUD đầy đủ)
- **Trạng thái duyệt**: Gửi duyệt hồ sơ cho admin
- **Hiển thị**: Ẩn/hiện profile với nhà đầu tư (chưa có API — cần implement)

### Luồng chính

```
[Đăng ký] → [Onboarding] → [Tạo hồ sơ] → [Chỉnh sửa] → [Gửi duyệt] → [Admin duyệt] → [Hiển thị cho nhà đầu tư]
```

### Các route FE

| Route | Mô tả | API sử dụng |
|---|---|---|
| `/startup/startup-profile` | Xem hồ sơ (read-only) | `GET /api/startups/me` |
| `/startup/startup-profile/info` | Tạo / chỉnh sửa hồ sơ | `POST /api/startups`, `PUT /api/startups/me`, `POST .../submit-for-approval` |
| `/startup/startup-profile/team` | Quản lý đội ngũ | `GET/POST/PUT/DELETE .../team-members` |
| `/startup/startup-profile/visibility` | Cài đặt hiển thị | Chưa có API (mock) |

---

## 2. Response Wrapper chung

Tất cả API response **PHẢI** sử dụng cấu trúc wrapper sau. FE parse theo cấu trúc này:

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": { ... },
  "error": null
}
```

```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Validation failed: companyName is required",
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": [
      { "field": "companyName", "message": "Company name is required" }
    ]
  }
}
```

**TypeScript interface (FE):**

```typescript
interface IBackendRes<T> {
    success: boolean;       // FE alias (interceptor tự map từ isSuccess)
    isSuccess: boolean;     // BE .NET trả trực tiếp
    statusCode: number;
    data?: T | null;
    message: string;
    error: IError | null;
}
```

> **Quan trọng**: FE check `res.isSuccess && res.data` để xác nhận thành công. Nếu `data: null` dù `isSuccess: true` → FE coi như không có dữ liệu.

---

## 3. API Endpoints chi tiết

### 3.1 GET /api/startups/me

Lấy hồ sơ startup của user đang đăng nhập.

| | |
|---|---|
| **Method** | `GET` |
| **Auth** | Bearer Token (required) |
| **Request Body** | Không có |
| **Khi nào được gọi** | Mỗi khi user vào bất kỳ trang nào trong `/startup/startup-profile/*` (context auto-fetch on mount) |

**Response thành công (200):**

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": {
    "startupID": 1,
    "userID": 42,
    "companyName": "AI Tech Startup",
    "oneLiner": "AI-powered solution for SMBs",
    "description": "Mô tả chi tiết về startup...",
    "industryID": 3,
    "industryName": "AI & Technology",
    "stage": "Seed",
    "foundedDate": "2024-01-15T00:00:00Z",
    "website": "https://example.com",
    "logoURL": "https://storage.example.com/logos/startup-1.png",
    "fundingAmountSought": 500000,
    "currentFundingRaised": 150000,
    "valuation": 2000000,
    "profileStatus": "Pending",
    "approvedAt": null,
    "createdAt": "2024-06-01T10:30:00Z",
    "updatedAt": "2024-06-15T14:20:00Z",
    "teamMembers": [
      {
        "teamMemberID": 1,
        "fullName": "Nguyễn Văn A",
        "role": "Toàn thời gian",
        "title": "CEO & Co-founder",
        "linkedInURL": "https://linkedin.com/in/nguyenvana",
        "bio": "10 năm kinh nghiệm trong lĩnh vực AI...",
        "photoURL": "https://storage.example.com/photos/member-1.jpg",
        "isFounder": "true",
        "yearsOfExperience": "10",
        "createdAt": "2024-06-01T10:30:00Z"
      }
    ]
  }
}
```

**Response khi chưa có profile (404):**

```json
{
  "isSuccess": false,
  "statusCode": 404,
  "message": "Startup profile not found",
  "data": null,
  "error": null
}
```

> **Lưu ý**: FE xử lý 404 như trạng thái bình thường (chưa tạo hồ sơ). KHÔNG nên trả 500 cho trường hợp này.

> **Bug đã gặp**: GET /me trả 500 khi profile tồn tại nhưng `logoURL` là null → backend cần handle null fields (đặc biệt `logoURL`, `approvedAt`, `foundedDate`).

---

### 3.2 POST /api/startups

Tạo mới hồ sơ startup. Mỗi user chỉ được tạo 1 profile.

| | |
|---|---|
| **Method** | `POST` |
| **Auth** | Bearer Token (required) |
| **Content-Type** | `multipart/form-data` |
| **Constraint** | 1 startup profile per user |

**Request FormData fields:**

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `companyName` | string | ✅ | Tên công ty |
| `oneLiner` | string | ✅ | Tagline / slogan (max 120 ký tự) |
| `description` | string | ❌ | Mô tả chi tiết |
| `industryID` | number | ❌ | ID ngành nghề (FK → Industries table) |
| `stage` | number (enum) | ✅ | Giai đoạn startup (xem enum bên dưới) |
| `foundedDate` | ISO string | ❌ | Ngày thành lập, format: `"2024-01-15T00:00:00.000Z"` |
| `website` | string | ❌ | URL website |
| `logoUrl` | File | ❌ | File ảnh logo (PNG/JPG/WEBP, max 5MB) |
| `fundingAmountSought` | number | ❌ | Số vốn cần gọi (USD) |
| `currentFundingRaised` | number | ❌ | Vốn đã huy động (USD) |
| `valuation` | number | ❌ | Định giá hiện tại (USD) |

**StartupStage Enum:**

| Value | Label (FE) |
|---|---|
| `0` | Idea |
| `1` | Pre-Seed |
| `2` | Seed |
| `3` | Series A |
| `4` | Series B |
| `5` | Series C / C+ |
| `6` | Growth |

**Response thành công (200/201):**

```json
{
  "isSuccess": true,
  "statusCode": 201,
  "message": "Startup profile created successfully",
  "data": "created-startup-id-or-message"
}
```

**Response khi user đã có profile (400):**

```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "You already have a startup profile. Each user can only create one startup.",
  "data": null
}
```

> **Lưu ý quan trọng**: Nếu logo upload thất bại (storage error), backend KHÔNG nên tạo profile record. Phải **rollback** toàn bộ transaction. FE đã gặp trường hợp: POST trả 400 "Failed to upload logo" nhưng profile record đã được tạo trong DB → GET /me sau đó bị 500 vì logoURL null.

---

### 3.3 PUT /api/startups/me

Cập nhật hồ sơ startup hiện có.

| | |
|---|---|
| **Method** | `PUT` |
| **Auth** | Bearer Token (required) |
| **Content-Type** | `multipart/form-data` |

**Request FormData fields:**

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `companyName` | string | ❌ | Tên công ty |
| `oneLiner` | string | ✅ | Tagline (FE luôn gửi) |
| `description` | string | ❌ | Mô tả |
| `industryID` | number | ❌ | ID ngành nghề |
| `stage` | number (enum) | ✅ | Giai đoạn (FE luôn gửi) |
| `foundedDate` | ISO string | ❌ | Ngày thành lập |
| `website` | string | ❌ | URL website |
| `logoUrl` | File \| `"null"` | ❌ | File mới HOẶC string `"null"` để xóa logo hiện tại |
| `fundingAmountSought` | number | ❌ | Vốn cần gọi |
| `currentFundingRaised` | number | ❌ | Vốn đã huy động |
| `valuation` | number | ❌ | Định giá |

> **Xử lý đặc biệt `logoUrl`:**
> - Nếu gửi File → upload file mới, thay thế logo cũ
> - Nếu gửi string `"null"` → xóa logo hiện tại (set `logoURL = null` trong DB)
> - Nếu không gửi field này → giữ nguyên logo hiện tại

**Response thành công (200):**

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Startup profile updated successfully",
  "data": "updated"
}
```

---

### 3.4 POST /api/startups/me/submit-for-approval

Gửi hồ sơ để admin duyệt.

| | |
|---|---|
| **Method** | `POST` |
| **Auth** | Bearer Token (required) |
| **Request Body** | Không có (empty body OK) |

> **Lưu ý**: FE gọi `saveProfile()` TRƯỚC khi gọi endpoint này. Nghĩa là PUT /me luôn được gọi trước POST /submit-for-approval.

**Response thành công (200):**

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Profile submitted for approval",
  "data": null
}
```

**Response thất bại (400) — ví dụ: thiếu thông tin bắt buộc:**

```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Profile must have company name and tagline before submission",
  "data": null
}
```

**Business Logic gợi ý:**
- Chuyển `profileStatus` từ `"Draft"` → `"Pending"`
- Kiểm tra các field bắt buộc đã có (companyName, oneLiner, stage)
- Không cho submit lại nếu đang ở trạng thái `"Pending"`

---

### 3.5 GET /api/startups/me/team-members

Lấy danh sách thành viên đội ngũ.

| | |
|---|---|
| **Method** | `GET` |
| **Auth** | Bearer Token (required) |

**Response thành công (200):**

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": [
    {
      "teamMemberID": 1,
      "fullName": "Nguyễn Văn A",
      "role": "Toàn thời gian",
      "title": "CEO & Co-founder",
      "linkedInURL": "https://linkedin.com/in/nguyenvana",
      "bio": "10 năm kinh nghiệm trong lĩnh vực AI...",
      "photoURL": "https://storage.example.com/photos/member-1.jpg",
      "isFounder": "true",
      "yearsOfExperience": "10",
      "createdAt": "2024-06-01T10:30:00Z"
    }
  ]
}
```

> **Lưu ý**: FE đang expect `isFounder` và `yearsOfExperience` dưới dạng **string** (không phải boolean/number). Đây có thể là bug hoặc do cách C# serialize. Nên thống nhất: trả boolean cho `isFounder` và number cho `yearsOfExperience`.

---

### 3.6 POST /api/startups/me/team-members

Thêm thành viên mới.

| | |
|---|---|
| **Method** | `POST` |
| **Auth** | Bearer Token (required) |
| **Content-Type** | `multipart/form-data` |

**Request FormData fields:**

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `fullName` | string | ✅ | Họ và tên |
| `role` | string | ✅ | Vai trò: `"Toàn thời gian"`, `"Bán thời gian"`, `"Cố vấn"`, `"Thực tập"` |
| `title` | string | ✅ | Chức vụ (e.g., "CEO & Co-founder") |
| `linkedInURL` | string | ❌ | URL LinkedIn |
| `bio` | string | ✅ | Tiểu sử ngắn |
| `photoURL` | File | ✅ | Ảnh đại diện (image file) |
| `isFounder` | boolean | ✅ | `"true"` hoặc `"false"` (gửi dưới dạng string trong FormData) |
| `yearsOfExperience` | number | ✅ | Số năm kinh nghiệm (gửi dưới dạng string trong FormData) |

**Response thành công (200/201):**

```json
{
  "isSuccess": true,
  "statusCode": 201,
  "message": "Team member added successfully",
  "data": null
}
```

---

### 3.7 PUT /api/startups/me/team-members/{teamMemberId}

Cập nhật thông tin thành viên.

| | |
|---|---|
| **Method** | `PUT` |
| **Auth** | Bearer Token (required) |
| **Path Param** | `teamMemberId` (number) |
| **Content-Type** | `multipart/form-data` |

**Request FormData fields:** Giống POST nhưng tất cả đều optional. Chỉ gửi field cần thay đổi.

> Nếu không gửi `photoURL` → giữ ảnh hiện tại.

---

### 3.8 DELETE /api/startups/me/team-members/{memberId}

Xóa thành viên.

| | |
|---|---|
| **Method** | `DELETE` |
| **Auth** | Bearer Token (required) |
| **Path Param** | `memberId` (number) |

**Response thành công (200):**

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Team member deleted successfully",
  "data": null
}
```

---

## 4. Database Schema gợi ý

### 4.1 Startups Table

```sql
CREATE TABLE Startups (
    StartupID       INT PRIMARY KEY IDENTITY(1,1),
    UserID          INT NOT NULL UNIQUE,         -- FK → Users.UserID (1 user = 1 startup)
    CompanyName     NVARCHAR(200) NOT NULL,
    OneLiner        NVARCHAR(120) NOT NULL,
    Description     NVARCHAR(MAX) NULL,
    IndustryID      INT NULL,                    -- FK → Industries.IndustryID
    Stage           INT NOT NULL DEFAULT 0,      -- Enum: 0=Idea, 1=PreSeed, 2=Seed, 3=SeriesA, 4=SeriesB, 5=SeriesC, 6=Growth
    FoundedDate     DATETIME2 NULL,
    Website         NVARCHAR(500) NULL,
    LogoURL         NVARCHAR(1000) NULL,         -- URL to uploaded logo in storage
    FundingAmountSought    DECIMAL(18,2) NULL,
    CurrentFundingRaised   DECIMAL(18,2) NULL,
    Valuation              DECIMAL(18,2) NULL,
    ProfileStatus   NVARCHAR(50) NOT NULL DEFAULT 'Draft',  -- Draft | Pending | Approved | Rejected
    ApprovedAt      DATETIME2 NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_Startups_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Startups_Industries FOREIGN KEY (IndustryID) REFERENCES Industries(IndustryID),
    CONSTRAINT UQ_Startups_UserID UNIQUE (UserID)  -- 1 user chỉ có 1 startup
);
```

### 4.2 TeamMembers Table

```sql
CREATE TABLE TeamMembers (
    TeamMemberID        INT PRIMARY KEY IDENTITY(1,1),
    StartupID           INT NOT NULL,            -- FK → Startups.StartupID
    FullName            NVARCHAR(200) NOT NULL,
    Role                NVARCHAR(100) NOT NULL,  -- "Toàn thời gian", "Bán thời gian", "Cố vấn", "Thực tập"
    Title               NVARCHAR(200) NOT NULL,  -- "CEO & Co-founder", "CTO", etc.
    LinkedInURL         NVARCHAR(500) NULL,
    Bio                 NVARCHAR(1000) NOT NULL,
    PhotoURL            NVARCHAR(1000) NULL,     -- URL to uploaded photo
    IsFounder           BIT NOT NULL DEFAULT 0,
    YearsOfExperience   INT NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_TeamMembers_Startups FOREIGN KEY (StartupID) REFERENCES Startups(StartupID) ON DELETE CASCADE
);
```

### 4.3 Industries Table (Lookup)

```sql
CREATE TABLE Industries (
    IndustryID      INT PRIMARY KEY IDENTITY(1,1),
    IndustryName    NVARCHAR(100) NOT NULL       -- "AI & Technology", "Fintech", "Healthcare", "E-commerce", "EdTech", "ClimateTech"
);
```

### 4.4 ProfileStatus Enum Values

| Value | Mô tả |
|---|---|
| `Draft` | Mới tạo, chưa gửi duyệt |
| `Pending` | Đã gửi duyệt, chờ admin |
| `Approved` | Đã được duyệt |
| `Rejected` | Bị từ chối (có thể sửa và gửi lại) |

---

## 5. Authentication & Authorization

- Tất cả endpoint đều yêu cầu **Bearer Token** trong header `Authorization`
- Token được lấy từ `localStorage.accessToken` (FE gắn tự động qua interceptor)
- User phải có role `Startup` mới được truy cập các endpoint `/api/startups/me/*`
- Endpoint `/api/startups/me` sử dụng token để xác định `UserID` → lookup `Startups.UserID`

**Interceptor behavior (FE):**

| HTTP Status | Hành vi FE |
|---|---|
| `200-299` | Parse `response.data`, map `isSuccess` → `success` |
| `401` (non-auth endpoint) | Tự động gọi `POST /api/auth/refresh-token` → retry request |
| `401` (auth endpoint, e.g. login) | Trả lỗi trực tiếp, KHÔNG refresh |
| `404` | Nếu response có `isSuccess` field → trả resolved value (không throw) |
| `400/500` | Nếu response có `isSuccess` field → trả resolved value, FE đọc `message` |

---

## 6. File Upload Specifications

### Logo
- **Field name trong FormData**: `logoUrl`
- **Accepted formats**: PNG, JPG, JPEG, WEBP
- **Max size**: 5MB
- **Recommended dimensions**: 400×400px
- **Storage**: Upload lên cloud storage (S3/Azure Blob/Cloudinary), trả về public URL

### Team Member Photo
- **Field name trong FormData**: `photoURL`
- **Accepted formats**: Tất cả image formats
- **Storage**: Tương tự logo

> **Lưu ý**: FormData field names FE gửi là **camelCase** (`companyName`, `logoUrl`, `photoURL`). C# ASP.NET Core `[FromForm]` model binding mặc định **case-insensitive**, nhưng cần đảm bảo model property names match.

---

## 7. Known Issues & Cần backend fix

| # | Issue | Mô tả | Đề xuất fix |
|---|---|---|---|
| 1 | GET /me trả 500 khi logoURL null | Profile được tạo thành công nhưng logo upload fail → record có logoURL=null → GET /me crash (null reference) | Handle null fields trong serialization, không throw khi logoURL null |
| 2 | POST tạo profile nhưng không rollback khi logo fail | POST /api/startups trả 400 "Failed to upload logo" nhưng record đã được insert vào DB | Wrap trong transaction: nếu logo upload fail → rollback insert |
| 3 | isFounder/yearsOfExperience type mismatch | FE interface `ITeamMember` define `isFounder: string`, `yearsOfExperience: string` thay vì boolean/number | Backend nên trả đúng type: `isFounder: boolean`, `yearsOfExperience: number`. FE sẽ được update tương ứng |
| 4 | Visibility API chưa implement | FE có trang visibility nhưng đang dùng mock state | Cần thêm field `visibility` trong Startups table và endpoint PUT để update |

---

## 8. API chưa có — cần implement thêm

### 8.1 GET /api/industries (Danh sách ngành nghề)

FE hiện đang hardcode danh sách ngành nghề. Cần API để lấy danh sách động.

```json
{
  "isSuccess": true,
  "data": [
    { "industryID": 1, "industryName": "AI & Technology" },
    { "industryID": 2, "industryName": "Fintech" },
    { "industryID": 3, "industryName": "Healthcare" }
  ]
}
```

### 8.2 PUT /api/startups/me/visibility (Cập nhật trạng thái hiển thị)

```json
// Request
{ "visibility": "Visible" }  // "Visible" | "Hidden"

// Response
{
  "isSuccess": true,
  "message": "Visibility updated"
}
```

---

## 9. Tóm tắt tất cả endpoints

| # | Method | Path | Mô tả | Status |
|---|---|---|---|---|
| 1 | `GET` | `/api/startups/me` | Lấy profile startup | ✅ Có |
| 2 | `POST` | `/api/startups` | Tạo profile mới | ✅ Có |
| 3 | `PUT` | `/api/startups/me` | Cập nhật profile | ✅ Có |
| 4 | `POST` | `/api/startups/me/submit-for-approval` | Gửi duyệt | ✅ Có |
| 5 | `GET` | `/api/startups/me/team-members` | Lấy danh sách team | ✅ Có |
| 6 | `POST` | `/api/startups/me/team-members` | Thêm thành viên | ✅ Có |
| 7 | `PUT` | `/api/startups/me/team-members/{id}` | Sửa thành viên | ✅ Có |
| 8 | `DELETE` | `/api/startups/me/team-members/{id}` | Xóa thành viên | ✅ Có |
| 9 | `GET` | `/api/industries` | Danh sách ngành | ❌ Cần thêm |
| 10 | `PUT` | `/api/startups/me/visibility` | Cập nhật hiển thị | ❌ Cần thêm |

---

*Tài liệu được tạo dựa trên codebase FE tại thời điểm 2026-03-26.*
