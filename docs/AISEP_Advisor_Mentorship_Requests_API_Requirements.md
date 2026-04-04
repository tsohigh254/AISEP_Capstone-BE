# Advisor Mentorship Requests - API Requirements

This document outlines the required C# Backend API endpoints needed to implement the real-world integration for the Advisor Mentorship Requests page (`http://localhost:3000/advisor/requests`). Currently, the UI is built but temporarily disabled or using mock data because the advisor-facing real APIs are missing or undefined on the Frontend.

## 1. List Requests for Advisor
The page needs a paginated endpoint to get all incoming mentorship requests for the logged-in Advisor.

**Endpoint Draft:** `GET /api/advisors/mentorships` (or `GET /api/mentorships/incoming`)
**Query Params:**
- `status` (string, optional - e.g., "REQUESTED", "ACCEPTED", "SCHEDULED", "COMPLETED", "REJECTED", "CANCELLED")
- `pageIndex` (number, optional)
- `pageSize` (number, optional)

**Expected Response Model (Dto):**
Need a paginated list of objects similar to this:
```typescript
interface IAdvisorMentorshipRequestDto {
  id: string; // Guidance ID
  startupId: string;
  startupName: string;
  startupLogoUrl?: string;
  startupIndustry?: string; // Tên lĩnh vực (VD: EdTech, FinTech...)
  startupStage?: string;    // Tên giai đoạn (VD: Seed, Series A...)
  durationMinutes: number;
  tags?: string[];
  preferredFormat?: "GoogleMeet" | "MicrosoftTeams";
  // The actual selected time if already scheduled
  scheduledTime?: string;
}
```

## 2. Accept Mentorship Request
When the Advisor clicks "Accept", they need an endpoint to transition the status from `REQUESTED` to `ACCEPTED`.

**Endpoint Draft:** `PUT /api/mentorships/{id}/accept`  *(or POST)*
**Request Body:** None strictly required. (Maybe a note).
**Returns:** Status 200 OK.

## 3. Reject/Decline Mentorship Request
When the Advisor clicks "Decline", they can provide a reason. This transitions the status to `REJECTED`.

**Endpoint Draft:** `PUT /api/mentorships/{id}/reject`
**Request Body:**
```json
{
  "reason": "My schedule is completely full this month."
}
```

## 4. Finalize Booking (Schedule Time)
After accepting, the Advisor selects one of the slots the Startup proposed to lock it in.

**Endpoint Draft:** `PUT /api/mentorships/{id}/schedule`
**Request Body:**
```json
{
  "startAt": "2026-03-31T10:00:00Z",
  "endAt": "2026-03-31T11:00:00Z",
  "meetingLink": "https://meet.google.com/abc-xyz-def"
}
```
**Effect:** Moves status to `SCHEDULED` and confirms the meeting.

## 5. View Details
When clicking on a request, the Advisor needs the full context the startup wrote (`challengeDescription`, etc).

**Endpoint Draft:** `GET /api/mentorships/{id}`

---
### Checklist for Backend Developer:
- [x] Confirm/Implement `GET /api/advisors/mentorships` filtering by status. *(Added `startupLogoUrl`)*
- [x] Confirm/Implement `PUT /api/mentorships/{id}/accept` action. *(Changed from POST → PUT, 30/03)*
- [x] Confirm/Implement `PUT /api/mentorships/{id}/reject` action with string reason. *(Changed from POST → PUT, 30/03)*
- [x] Confirm/Implement `PUT /api/mentorships/{id}/schedule` to confirm meeting time & link. *(Payload updated, status maps to `InProgress`)*
- [x] Ensure the DTO returns `ChallengeDescription`. *(Already present in both MentorshipListItemDto and MentorshipDetailDto)*

---
## 6. Frontend - Backend Sync & Resolutions (Mar 29, 2026)

**1. Vấn đề "requestedSlots" (Mảng thời gian đề xuất)**
*   **Backend Status:** Đã cập nhật xong (Mar 29). Đã tạo Migration thêm bảng mới `MentorshipRequestedSlots` qua Entity Framework. API Create/Get List/Get Detail đã được map để trả trực tiếp mảng Object.
*   **Quyết định từ Frontend:** Đã chốt theo phương án Ưu tiên 1 (Lưu Object Time Slot cấu trúc DB). Frontend sẽ gửi lên/nhận về payload gốc `{ preferredSlots: [...] }` theo chuẩn REST Object.

**2. Vấn đề "Tags"**
*   **Backend Status:** Không có array Tags đi theo Mentorship Request.
*   **Quyết định từ Frontend:** Đồng ý bỏ qua. Frontend sẽ không hiển thị Tags ở UI Inbox của Advisor nữa, thay vào đó sẽ hiển thị một phần của `ChallengeDescription` trang danh sách để Advisor dễ hình dung yêu cầu.

Once these endpoints are available and tested on Swagger, the Frontend can fully map the Advisor's inbox.

---
## 7. Refactoring Target (Technical Debt Cleanup) - Mar 29

**🛠️ VIỆC CẦN LÀM CHO BACKEND (BE Team)**
Để tuân thủ hoàn toàn theo Core Global System của Frontend đã vẽ ra từ lúc khởi tạo dự án, xin Backend hãy điều chỉnh lại JSON Wrapper trả về của các **API có chứa phân trang (Pagination)**.

*   **Tình trạng hiện tại (Chưa chuẩn)**
    `GET /api/mentorships` đang bọc mảng ngoài cùng bằng keyword `data`, và các thông số phân trang thì nằm rải rác:
    ```json
    {
      "message": "Success",
      "isSuccess": true,
      "data": {
        "page": 1,
        "pageSize": 100,
        "total": 1,
        "data": [ { ... } ]    <--- Keyword bị lặp, không theo chuẩn
      }
    }
    ```

*   **Đích đến mong muốn (Chuẩn IPagingData)**
    ```json
    {
      "message": "Success",
      "isSuccess": true,
      "data": {
        "items": [ { ... } ],  <--- Đổi chữ "data" thành "items"
        "paging": {            <--- Gom các biến page vào một object tên là "paging"
            "page": 1,
            "pageSize": 100,
            "totalItems": 1
        }
      }
    }
    ```
*(P/S: Việc thay đổi này là cực kỳ cần thiết để ứng dụng Frontend có thể tái sử dụng một hàm Generic Type duy nhất `IBackendRes<IPagingData<T>>`. Nếu không, FE sẽ phải viết code giải nén thủ công (destructuring array) tốn kém và nguy hiểm cho từng API một!)*

---
**🛠️ VIỆC CẦN LÀM CHO FRONTEND (FE Team đang tự xử lý)**
Trong lúc đợi Backend cập nhật DTO phân trang trên, Frontend sẽ tự động Refactor (Tái cấu trúc) các phần mã nguồn nội bộ sau:
1.  **Dọn dẹp Map Function:** Xoá hàm `.map()` thủ công dài 50 dòng trong ruột giao diện `app/advisor/requests/page.tsx`, di dời logic chuyển đổi (Transformation) đó vào một file Mapper tĩnh tái sử dụng được (Data Mapper Pattern).
2.  **Đồng bộ Enum Trạng thái:** Sẽ xoá sổ Enum rác `"REQUESTED"` mà FE cũ thiết lập, ép mảng giao diện chạy theo Base Core Enum là `"PENDING"` của C# Backend ném ra để xoá bỏ hoàn toàn rào cản Mapping Status.
3.  **Strict Typing:** Khai báo kiểu trả về tường minh `Promise<IBackendRes<IPagingData<IMentorshipRequest>>>` cho Axios request `GetAdvisorMentorships` thay vì đang ép ngược thành `any` để lướt rào.

---

## 8. Missing Fields for Advisor Inbox UI (Mới Cập Nhật - Cần BE Bổ Sung Gấp)

Hiện tại giao diện Thẻ Yêu Cầu (Request Card) của Advisor đang bị khuyết thông tin (Hiển thị chữ "Chưa xác định" và "Khởi nghiệp") vì trong cục response của GET /api/mentorships trả về DTO chưa map thông tin này từ Profile của Startup.

**Yêu cầu Backend update DTO:**
Vui lòng .Include(x => x.Startup).ThenInclude(x => x.Profile) (hoặc tương đương) để bổ sung **2 field mới** vào JSON của từng thẻ request:

1. \startupIndustry\ (string): Lĩnh vực của Startup (ví dụ: "EdTech", "FinTech", "HealthTech", v.v.). Nếu startup thuộc nhiều lĩnh vực, có thể nối chuỗi cách nhau bằng dấu phẩy.
2. \startupStage\ (string): Giai đoạn hiện tại của Startup (ví dụ: "Seed", "Series A", "Idea Stage", v.v.).

*Lý do:* Advisor cần nhìn lướt qua xem Startup này làm về lĩnh vực gì để biết có phù hợp với chuyên môn của mình hay không, trước khi bấm vào xem chi tiết.


## 9. API Cho Luồng Đề Xuất Thời Gian Mới (Propose Time Slots)

Trong màn hình **Chi tiết Yêu cầu Tư vấn**, khi Advisor click vào nút "Đề xuất thời gian khác" (Propose Time), hệ thống Frontend (FE) đã tích hợp API gọi xuống Backend (BE) để gửi danh sách các khung giờ mới được đề xuất.

**Endpoint Mới (Cần BE Mở):**
```http
PUT /api/mentorships/{id}/propose-slots
```

**Mục Đích & Logic Tại BE:**
1. Cập nhật trạng thái của yêu cầu này (Mentorship State) thành `ACCEPTED` (nếu đang ở trạng thái Cần xem xét/PENDING) vì hành động đề xuất lịch ngầm hiểu là hệ thống đã chấp nhận.
2. Lưu các mốc thời gian này vào DB dưới dạng Slot mới. Cần đánh dấu cờ dữ liệu là `proposedBy = "ADVISOR"` (do Advisor đề xuất).
3. Đóng hoặc vô hiệu hoá các slot cũ do Startup đưa ra lúc đầu.

**FE Payload Trình Lên (Request Body):**
```json
{
  "requestedSlots": [
    {
      "startAt": "2026-03-29T10:00:00", 
      "endAt": "2026-03-29T11:00:00",
      "timezone": "Asia/Ho_Chi_Minh",
      "note": "Khung giờ này tôi rảnh hoàn toàn."
    },
    {
      "startAt": "2026-03-30T10:00:00", 
      "endAt": "2026-03-30T11:00:00",
      "timezone": "Asia/Ho_Chi_Minh"
    }
  ]
}
```

---

## 10. API Hiển Thị Báo Cáo Tư Vấn (Get Session Report)

Khi quy trình tư vấn kết thúc (Trạng thái `COMPLETED` hoặc `FINALIZED`), màn hình chi tiết FE sẽ hiển thị Tab "Báo cáo tư vấn". Hiện tại giao diện này đã được nối luồng, chỉ chờ BE mở endpoint trả JSON Báo cáo.

**Endpoint Mới (Cần BE Mở):**
```http
GET /api/mentorships/{id}/report
```

**Mục Đích:** Trả về đối tượng báo cáo đánh giá (Mentorship Report) chứa các kết luận và đánh giá của buổi tư vấn. 

**FE Model Mong Đợi (Response Dto):**
```json
{
  "isSuccess": true,
  "data": {
    "reportId": "rep-0210",
    "mentorshipId": "3",
    "content": "Startup trình bày tốt, mô hình SaaS tiềm năng. Bước tiếp theo nên tập trung vào Acquisition Cost...",
    "createdAt": "2026-03-29T14:00:00Z",
    "advisor": {
       "fullName": "Nguyễn Văn Chuyên Gia",
       "avatarUrl": "..."
    }
  },
  "message": "Success"
}
```