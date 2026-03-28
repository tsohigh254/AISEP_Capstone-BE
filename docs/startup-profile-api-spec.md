# API Specification: Startup Profile View

Tài liệu này mô tả cấu trúc payload (JSON response) trả về từ Backend cần thiết để phục vụ cho trang giao diện **Startup Profile** (`/startup/startup-profile`). Thông tin này đảm bảo thay thế hoàn toàn cho dữ liệu Mock cứng trên giao diện.

## 1. Get Startup Profile

Lấy toàn bộ thông tin hiển thị trên màn hình hồ sơ của Startup hiện tại. Thông tin này được tổ chức thành các tab khác nhau trên UI (Tổng quan, Kinh doanh, Gọi vốn, Đội ngũ, Liên hệ).

- **Endpoint:** `GET /api/startups/me`
- **Authentication:** Yêu cầu (Bearer Token của role `STARTUP`)

### Response Format (JSON)

UI hiện đại mong muốn nhận được một object duy nhất (hoặc nằm trong thuộc tính `data` của API Response chuẩn) chứa tất cả các trường sau:

```json
{
  "success": true,
  "data": {
    // 1. Thông tin chung (Top Section & Tab Tổng quan)
    "companyName": "TechAlpha Co.",
    "oneLiner": "Giải pháp AI toàn diện cho doanh nghiệp SMEs tại Đông Nam Á",
    "description": "Chúng tôi cung cấp các giải pháp trí tuệ nhân tạo giúp tự động hóa quy trình vận hành và tối ưu hóa chi phí...",
    "logoURL": "https://example.com/logo.png", // Trả về text rỗng ("") nếu chưa có
    
    // 2. Phân loại & Quy mô
    "industry": "SaaS",
    "subIndustry": "AI / ML",
    "stage": "MVP",
    "marketScope": "B2B",
    "productStatus": "Launched",
    "foundedYear": 2023,
    "teamSize": 12,
    
    // 3. Vị trí
    "location": "TP. Hồ Chí Minh",
    "country": "Việt Nam",

    // 4. Tab "Kinh doanh" (Vấn đề & Giải pháp)
    "problemStatement": "Các doanh nghiệp SME tại Đông Nam Á đang lãng phí hàng triệu giờ nhân công...",
    "solutionSummary": "Nền tảng AI plug-and-play giúp SMEs tự động hóa quy trình mà không cần đội ngũ kỹ thuật...",
    
    // 5. Tab "Gọi vốn" & Nhu cầu
    "currentNeeds": ["Funding", "Partnership", "Market Access"], // Mảng danh sách các nhu cầu hiện tại
    "fundingStage": "Seed",
    "targetFunding": 500000, 
    "raisedAmount": 120000, 

    // 6. Tab "Đội ngũ & Xác thực"
    "validationStatus": "In Progress", // Các trạng thái: "Validated", "In Progress", "Unverified"
    "metricSummary": "MRR $8K · 450 MAU · tăng trưởng 18%/tháng · churn 4%", // Hoặc Backend có thể lưu thành mảng objects và ghép chuỗi sau
    
    // 7. Tab "Liên hệ" & External Links
    "website": "https://techalpha.ai",
    "linkedInURL": "https://linkedin.com/company/techalpha",
    "contactEmail": "contact@techalpha.ai",
    "contactPhone": "+84 909 123 456",

    // 8. Trạng thái hệ thống nội bộ của ứng dụng
    "visibilityStatus": "Visible", // Chỉ định Startup có hiển thị với Investor hay không ("Visible" | "Hidden")
    "profileCompleteness": 78 // Tính toán % độ hoàn thiện hồ sơ. Điểm màu sắc UX sẽ thay đổi dựa vào số này: >= 80% xanh, >= 50% vàng, < 50% đỏ
  }
}
```

## 2. Các hành động (Actions) yêu cầu API bổ sung
Mặc dù UI trên phần lớn là để View, nhưng có một vài thành phần có thể người dùng sẽ tương tác (cụ thể là các nút chuyển hướng hoặc toggle), back-end nên tham khảo các endpoint sau nếu chưa có:

### 2.1. Cập nhật trạng thái hiển thị (Visibility)
Khi Startup bấm vào nút "Đang hiển thị với nhà đầu tư / Đang ẩn", hệ thống sẽ đổi trạng thái (trang này được đặt tại route `/startup/startup-profile/visibility`).
- **Endpoint:** `PUT /api/startups/me/visibility` hoặc `POST /api/startups/me/visibility/enable` & `disable` (đã có trong file `startup.api.ts`)

### 2.2. Tracking Completeness Bar
- Backend cần tự động tính toán field `profileCompleteness` dựa trên số lượng các trường bắt buộc đã được điền thay vì Frontend phải ngồi check thủ công.

---
**Lưu ý cho Team Backend:**  
Trường hợp một số thông tin nằm rải rác ở các hệ thống bảng khác nhau (Company profile, Fundings, Metrics), UI hiện tại đang gom **tất cả vào một lệnh gọi API duy nhất** để tối ưu tốc độ render. Bạn cân nhắc viết một Query tổng hợp (hoặc Endpoint aggregation) trả về đúng format này.
