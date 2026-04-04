# Hướng dẫn sử dụng tài khoản Test (Test Accounts)

Tài liệu này cung cấp thông tin về các tài khoản kiểm thử (test accounts) có sẵn trong cơ sở dữ liệu và hướng dẫn cách tạo thêm tài khoản cho các vai trò (roles) khác nhau để phục vụ quá trình test API và Frontend.

---

## 1. Các tài khoản có sẵn (Được tự động tạo - Seeded)

Khi hệ thống khởi chạy ứng dụng lần đầu tiên, hệ thống sẽ tự động tạo một số tài khoản quản trị để bạn có thể sử dụng ngay mà không cần đăng ký.

### 👤 Nhân viên vận hành (Staff / Admin)
Dùng để test các luồng xét duyệt hồ sơ (Approve / Reject KYC) và quản lý hệ thống.
- **Email:** `staff@aisep.local`
- **Mật khẩu:** `12345.nN`
- **Role:** `Staff`

### 🛡️ Quản trị viên (Admin)
Dùng để test các luồng quản trị hệ thống, phân quyền và cấu hình nền tảng.
- **Email:** `admin@aisep.local`
- **Mật khẩu:** `12345.nN`
- **Role:** `Admin`

### 🚀 Khởi nghiệp (Startup)
Dùng để tạo profile doanh nghiệp và gửi yêu cầu KYC.
- **Email:** `startup@aisep.local` (Tài khoản này đã được gỡ khỏi bộ seed tự động để bạn có thể tự đăng ký test từ đầu qua luồng Frontend)
- **Mật khẩu:** `12345.nN`
- **Role:** `Startup`

### 🤝 Cố vấn (Advisor)
Dùng để tạo profile chuyên môn và nhận kết nối.
- **Email:** `advisor@aisep.local`
- **Mật khẩu:** `12345.nN`
- **Role:** `Advisor`

### 💼 Nhà đầu tư (Investor)
Dùng để tạo profile quỹ, tạo watchlist và kết nối với startup.
- **Email:** `investor@aisep.local`
- **Mật khẩu:** `12345.nN`
- **Role:** `Investor`

*(Lưu ý: Bạn có thể đăng nhập các tài khoản này trực tiếp trên hệ thống mà không cần đăng ký).*

---

## 2. Hướng dẫn tạo tài khoản Test cho Người dùng (Tuỳ chọn)

Nếu bạn muốn tạo các tài khoản riêng biệt để sử dụng thì bạn cần tự tạo qua API Đăng ký (`Register`) của hệ thống.

### 🚀 Đăng ký tài khoản (Register)
**Endpoint:** `POST /api/Auth/register`

#### A. Tạo tài khoản Khởi nghiệp (Startup)
```json
{
  "email": "startup_test@gmail.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "userType": "Startup"
}
```

#### B. Tạo tài khoản Cố vấn (Advisor)
```json
{
  "email": "advisor_test@gmail.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "userType": "Advisor"
}
```

#### C. Tạo tài khoản Nhà đầu tư (Investor)
```json
{
  "email": "investor_test@gmail.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "userType": "Investor"
}
```

---

## 3. Cách Đăng nhập và Test

Sau khi đã có tài khoản (có sẵn hoặc vừa tạo), bạn sử dụng API Login để lấy Token xác thực (JWT).

**Endpoint:** `POST /api/Auth/login`

**Request Body mẫu:**
```json
{
  "email": "staff@aisep.local",
  "password": "12345.nN"
}
```

**Mô phỏng luồng Test KYC:**
1. **Login User (ví dụ Startup):** Lấy token và copy.
2. Cắm token vào Header (trên Swagger chọn nút *Authorize* -> nhập `Bearer <token>`).
3. Gọi API tạo profile và Submit KYC (`POST /api/startups/me/submit-for-approval`).
4. **Login Staff:** Lấy token của `staff@aisep.local`.
5. Thay Bearer token mới vào Header.
6. Gọi API duyệt KYC (`POST /api/registration/approve/startups/{staffId}`).

_Chúc bạn test luồng thành công!_
