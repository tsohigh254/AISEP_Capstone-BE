# AISEP Capstone Backend - Hướng dẫn chạy và triển khai

## Mục lục

- [Tổng quan](#tổng-quan)
- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Cài đặt môi trường phát triển](#cài-đặt-môi-trường-phát-triển)
- [Cấu hình dự án](#cấu-hình-dự-án)
- [Chạy dự án](#chạy-dự-án)
- [Database & Migrations](#database--migrations)
- [Cấu trúc dự án](#cấu-trúc-dự-án)
- [API Endpoints](#api-endpoints)
- [Xác thực & Phân quyền](#xác-thực--phân-quyền)
- [Tính năng Real-time (SignalR)](#tính-năng-real-time-signalr)
- [Tích hợp bên thứ ba](#tích-hợp-bên-thứ-ba)
- [Triển khai Production](#triển-khai-production)
- [Xử lý sự cố](#xử-lý-sự-cố)

---

## Tổng quan

**AISEP** (AI-powered Startup Ecosystem Platform) là một nền tảng hệ sinh thái khởi nghiệp được hỗ trợ bởi AI, bao gồm các tính năng chính:

- Quản lý hồ sơ Startup, Investor, Advisor
- Hệ thống mentorship (cố vấn) giữa Advisor và Startup
- Kết nối Startup - Investor
- Chat real-time qua SignalR
- Upload và quản lý tài liệu (Cloudinary)
- Xác thực tài liệu trên blockchain (Ethereum Sepolia)
- Hệ thống kiểm duyệt nội dung (Moderation)
- Quản trị viên (Staff/Admin)

**Tech Stack:**

| Thành phần       | Công nghệ                          |
| ---------------- | ----------------------------------- |
| Framework        | .NET 8.0 (ASP.NET Core Web API)    |
| Database         | PostgreSQL                          |
| ORM              | Entity Framework Core 8.0           |
| Authentication   | JWT Bearer Token                    |
| Real-time        | SignalR                             |
| Validation       | FluentValidation                    |
| Object Mapping   | AutoMapper                          |
| File Storage     | Cloudinary                          |
| Blockchain       | Ethereum (Nethereum) / Stub         |
| Email            | MailKit (SMTP Gmail)                |
| Logging          | Serilog (Console + File)            |
| API Docs         | Swagger / OpenAPI                   |
| CQRS Pattern     | MediatR                             |

---

## Yêu cầu hệ thống

### Bắt buộc

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (>= 8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (>= 14.0)
- [Git](https://git-scm.com/downloads)

### Khuyến nghị

- [Visual Studio 2022](https://visualstudio.microsoft.com/) hoặc [VS Code](https://code.visualstudio.com/) với C# Dev Kit extension
- [pgAdmin](https://www.pgadmin.org/) hoặc [DBeaver](https://dbeaver.io/) để quản lý database
- [Postman](https://www.postman.com/) để test API

### Kiểm tra cài đặt

```bash
dotnet --version    # >= 8.0.x
psql --version      # >= 14.x
git --version
```

---

## Cài đặt môi trường phát triển

### 1. Clone repository

```bash
git clone https://github.com/tsohigh254/AISEP_Capstone-BE.git
cd AISEP_Capstone-BE
```

### 2. Restore packages

```bash
dotnet restore
```

### 3. Tạo database PostgreSQL

Mở terminal PostgreSQL hoặc pgAdmin và tạo database:

```sql
CREATE DATABASE "AISEP";
```

### 4. Cấu hình file `.env`

Copy file `.env.example` thành `.env` trong thư mục `src/AISEP.WebAPI/`:

```bash
cp src/AISEP.WebAPI/.env.example src/AISEP.WebAPI/.env
```

Mở file `.env` và điền các giá trị cần thiết (xem phần [Cấu hình dự án](#cấu-hình-dự-án) bên dưới).

### 5. Chạy database migration

```bash
dotnet ef database update --project src/AISEP.Infrastructure --startup-project src/AISEP.WebAPI
```

> **Lưu ý:** Nếu chưa cài `dotnet-ef`, chạy lệnh sau trước:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

### 6. Chạy ứng dụng

```bash
dotnet run --project src/AISEP.WebAPI
```

Ứng dụng sẽ chạy tại:
- HTTP: `http://localhost:5294`
- HTTPS: `https://localhost:7001`
- Swagger UI: `http://localhost:5294/swagger`

---

## Cấu hình dự án

### Biến môi trường (file `.env`)

Tạo file `src/AISEP.WebAPI/.env` với nội dung sau:

```env
# ===== DATABASE =====
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=AISEP;Username=postgres;Password=your_password

# ===== JWT =====
Jwt__SecretKey=your-secret-key-at-least-32-characters-long

# ===== EMAIL (Gmail SMTP) =====
Email__SmtpUser=your-email@gmail.com
Email__SmtpPassword=your-gmail-app-password
Email__FromEmail=your-email@gmail.com

# ===== CLOUDINARY =====
CloudinaryOptions__CloudName=your-cloud-name
CloudinaryOptions__ApiKey=your-api-key
CloudinaryOptions__ApiSecret=your-api-secret

# ===== BLOCKCHAIN (Tùy chọn) =====
Blockchain__Provider=Stub
# Nếu dùng Ethereum thật, đổi Provider=Ethereum và điền:
# Blockchain__RpcUrl=https://sepolia.infura.io/v3/your-project-id
# Blockchain__ContractAddress=0x_your_contract_address
# Blockchain__PrivateKey=your-wallet-private-key
```

### Hướng dẫn lấy thông tin cấu hình

#### Gmail App Password

1. Đăng nhập Google Account > **Security** > **2-Step Verification** (bật nếu chưa bật)
2. Vào **App passwords** > Chọn app "Mail" > Generate
3. Copy password 16 ký tự vào `Email__SmtpPassword`

#### Cloudinary

1. Đăng ký tại [cloudinary.com](https://cloudinary.com/)
2. Vào **Dashboard** > Copy `Cloud Name`, `API Key`, `API Secret`

#### Blockchain (Tùy chọn)

- Mặc định `Blockchain__Provider=Stub` sẽ sử dụng mock service, không cần cấu hình thêm
- Nếu muốn dùng Ethereum Sepolia thật:
  1. Tạo project trên [Infura](https://infura.io/) hoặc [Alchemy](https://www.alchemy.com/)
  2. Lấy Sepolia RPC URL
  3. Deploy smart contract và lấy contract address
  4. Lấy private key từ ví MetaMask (cần có Sepolia ETH)

### File cấu hình chính

| File                          | Mô tả                                      |
| ----------------------------- | ------------------------------------------- |
| `appsettings.json`            | Cấu hình chung (không chứa secrets)         |
| `appsettings.Development.json`| Cấu hình ghi đè cho môi trường Development  |
| `.env`                        | Biến môi trường chứa secrets (KHÔNG commit)  |
| `Properties/launchSettings.json` | Cấu hình launch profiles                 |

---

## Chạy dự án

### Chạy bằng CLI

```bash
# Development mode (mặc định)
dotnet run --project src/AISEP.WebAPI

# Chỉ định environment
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/AISEP.WebAPI

# Chạy với watch mode (tự restart khi thay đổi code)
dotnet watch run --project src/AISEP.WebAPI
```

### Chạy bằng Visual Studio

1. Mở file `AISEP.sln`
2. Đặt **AISEP.WebAPI** làm Startup Project
3. Chọn profile `http` hoặc `https`
4. Nhấn **F5** (Debug) hoặc **Ctrl+F5** (Run without debug)

### Chạy bằng VS Code

1. Mở thư mục dự án trong VS Code
2. Cài extension **C# Dev Kit**
3. Mở terminal và chạy `dotnet run --project src/AISEP.WebAPI`
4. Hoặc dùng **Run and Debug** (Ctrl+Shift+D) > chọn `.NET Core Launch`

### Kiểm tra ứng dụng đã chạy

- Mở trình duyệt: `http://localhost:5294/swagger` để xem Swagger UI
- Hoặc test bằng curl:
  ```bash
  curl http://localhost:5294/api/auth/login
  ```

---

## Database & Migrations

### Tạo migration mới

Khi thay đổi Entity hoặc DbContext:

```bash
dotnet ef migrations add <TenMigration> \
  --project src/AISEP.Infrastructure \
  --startup-project src/AISEP.WebAPI
```

Ví dụ:

```bash
dotnet ef migrations add AddNewFeature \
  --project src/AISEP.Infrastructure \
  --startup-project src/AISEP.WebAPI
```

### Áp dụng migration

```bash
dotnet ef database update \
  --project src/AISEP.Infrastructure \
  --startup-project src/AISEP.WebAPI
```

### Rollback migration

```bash
# Rollback về migration cụ thể
dotnet ef database update <TenMigration> \
  --project src/AISEP.Infrastructure \
  --startup-project src/AISEP.WebAPI

# Xóa migration cuối cùng (chưa áp dụng)
dotnet ef migrations remove \
  --project src/AISEP.Infrastructure \
  --startup-project src/AISEP.WebAPI
```

### Seed Data

Ứng dụng tự động seed dữ liệu khi khởi động:

- **Roles:** Startup, Investor, Advisor, Staff, Admin
- **Permissions:** ~16 quyền cho các chức năng (Users, Startups, Documents, Mentorships, Connections, Moderation, Admin)
- **Industries:** Danh sách các ngành công nghệ

---

## Cấu trúc dự án

```
AISEP_Capstone-BE/
├── AISEP.sln                           # Solution file
├── src/
│   ├── AISEP.Domain/                   # Domain Layer
│   │   ├── Entities/                   # Entity classes (User, Startup, Advisor, ...)
│   │   ├── Enums/                      # Enum definitions
│   │   └── Interfaces/                 # Domain interfaces
│   │
│   ├── AISEP.Application/             # Application Layer
│   │   ├── Configuration/             # Config classes (JwtSettings, BlockchainSettings, ...)
│   │   ├── DTOs/                      # Data Transfer Objects
│   │   ├── Interfaces/               # Service interfaces
│   │   ├── Mapping/                  # AutoMapper profiles
│   │   └── QueryParams/             # Query parameter classes
│   │
│   ├── AISEP.Infrastructure/          # Infrastructure Layer
│   │   ├── Data/                      # DbContext, Seeder
│   │   ├── Migrations/               # EF Core Migrations
│   │   ├── Services/                 # Service implementations
│   │   └── Settings/                 # Infrastructure settings
│   │
│   └── AISEP.WebAPI/                  # Presentation Layer
│       ├── Controllers/              # API Controllers
│       ├── Hubs/                     # SignalR Hubs (ChatHub)
│       ├── Middlewares/              # Custom middlewares
│       ├── Extensions/              # Extension methods
│       ├── Validators/              # FluentValidation validators
│       ├── Program.cs               # Entry point
│       ├── appsettings.json         # Configuration
│       └── .env.example             # Environment variables template
└── docs/                             # Documentation
```

---

## API Endpoints

### Base URL: `http://localhost:5294/api`

### Authentication

| Method | Endpoint                  | Mô tả                    | Auth |
| ------ | ------------------------- | ------------------------- | ---- |
| POST   | `/auth/register`          | Đăng ký tài khoản         | No   |
| POST   | `/auth/login`             | Đăng nhập                 | No   |
| POST   | `/auth/refresh-token`     | Làm mới access token      | No   |
| POST   | `/auth/forgot-password`   | Yêu cầu reset mật khẩu   | No   |
| POST   | `/auth/reset-password`    | Đặt lại mật khẩu          | No   |

### Users

| Method | Endpoint          | Mô tả                | Auth       |
| ------ | ----------------- | --------------------- | ---------- |
| GET    | `/users`          | Danh sách users       | Staff/Admin|
| GET    | `/users/{id}`     | Chi tiết user         | JWT        |
| PUT    | `/users/{id}`     | Cập nhật user         | JWT        |

### Startups

| Method | Endpoint              | Mô tả                        | Auth       |
| ------ | --------------------- | ----------------------------- | ---------- |
| GET    | `/startups`           | Danh sách startups            | JWT        |
| GET    | `/startups/{id}`      | Chi tiết startup              | JWT        |
| POST   | `/startups`           | Tạo hồ sơ startup            | Startup    |
| PUT    | `/startups/{id}`      | Cập nhật startup              | Startup    |

### Advisors

| Method | Endpoint              | Mô tả                        | Auth       |
| ------ | --------------------- | ----------------------------- | ---------- |
| GET    | `/advisors`           | Danh sách advisors            | JWT        |
| GET    | `/advisors/{id}`      | Chi tiết advisor              | JWT        |
| POST   | `/advisors`           | Tạo hồ sơ advisor            | Advisor    |
| PUT    | `/advisors/{id}`      | Cập nhật advisor              | Advisor    |

### Investors

| Method | Endpoint              | Mô tả                        | Auth       |
| ------ | --------------------- | ----------------------------- | ---------- |
| GET    | `/investors`          | Danh sách investors           | JWT        |
| GET    | `/investors/{id}`     | Chi tiết investor             | JWT        |
| POST   | `/investors`          | Tạo hồ sơ investor           | Investor   |
| PUT    | `/investors/{id}`     | Cập nhật investor             | Investor   |

### Mentorships

| Method | Endpoint                        | Mô tả                            | Auth       |
| ------ | ------------------------------- | --------------------------------- | ---------- |
| GET    | `/mentorships`                  | Danh sách mentorships             | JWT        |
| POST   | `/mentorships`                  | Tạo yêu cầu mentorship           | Startup    |
| PUT    | `/mentorships/{id}/accept`      | Chấp nhận mentorship              | Advisor    |
| PUT    | `/mentorships/{id}/reject`      | Từ chối mentorship                | Advisor    |

### Documents

| Method | Endpoint                        | Mô tả                            | Auth       |
| ------ | ------------------------------- | --------------------------------- | ---------- |
| GET    | `/documents`                    | Danh sách tài liệu               | JWT        |
| POST   | `/documents`                    | Upload tài liệu                  | JWT        |
| POST   | `/documents/{id}/blockchain`    | Tạo blockchain proof              | JWT        |

### Connections (Startup - Investor)

| Method | Endpoint                        | Mô tả                            | Auth       |
| ------ | ------------------------------- | --------------------------------- | ---------- |
| GET    | `/connections`                  | Danh sách connections             | JWT        |
| POST   | `/connections`                  | Tạo connection request            | JWT        |

### Chat

| Method | Endpoint                        | Mô tả                            | Auth       |
| ------ | ------------------------------- | --------------------------------- | ---------- |
| GET    | `/conversations`                | Danh sách conversations           | JWT        |
| POST   | `/conversations`                | Tạo conversation mới              | JWT        |
| GET    | `/messages`                     | Danh sách messages                | JWT        |

> **Xem đầy đủ tại Swagger UI:** `http://localhost:5294/swagger`

---

## Xác thực & Phân quyền

### JWT Authentication

Ứng dụng sử dụng JWT Bearer Token:

1. Đăng nhập qua `POST /api/auth/login` để nhận `accessToken` và `refreshToken`
2. Gửi request với header: `Authorization: Bearer <accessToken>`
3. Khi token hết hạn (60 phút), dùng `POST /api/auth/refresh-token` để lấy token mới

### Authorization Policies

| Policy        | Điều kiện                          |
| ------------- | ---------------------------------- |
| StartupOnly   | `userType = "Startup"`             |
| InvestorOnly  | `userType = "Investor"`            |
| AdvisorOnly   | `userType = "Advisor"`             |
| StaffOrAdmin  | `userType ∈ ["Staff", "Admin"]`    |
| AdminOnly     | `userType = "Admin"`               |

### Roles mặc định

| Role     | Mô tả                                    |
| -------- | ----------------------------------------- |
| Startup  | Người dùng đại diện cho startup           |
| Investor | Nhà đầu tư                               |
| Advisor  | Cố vấn / Mentor                          |
| Staff    | Nhân viên hệ thống                       |
| Admin    | Quản trị viên toàn quyền                 |

---

## Tính năng Real-time (SignalR)

### ChatHub

- **Endpoint:** `http://localhost:5294/hubs/chat`
- **Authentication:** Truyền JWT token qua query string `?access_token=<token>`

### Kết nối từ client (JavaScript)

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5294/hubs/chat", {
    accessTokenFactory: () => "your-jwt-token",
  })
  .withAutomaticReconnect()
  .build();

// Lắng nghe tin nhắn mới
connection.on("ReceiveMessage", (message) => {
  console.log("New message:", message);
});

// Kết nối
await connection.start();

// Tham gia conversation
await connection.invoke("JoinConversation", conversationId);

// Gửi tin nhắn
await connection.invoke("SendMessage", {
  conversationId: "guid",
  content: "Hello!",
});

// Rời conversation
await connection.invoke("LeaveConversation", conversationId);
```

---

## Tích hợp bên thứ ba

### Cloudinary (Lưu trữ file)

- Dùng để upload tài liệu, hình ảnh (Pitch Deck, Business Plan, Avatar, ...)
- Giới hạn upload: **20 MB** mỗi file
- Cấu hình trong `.env`: `CloudinaryOptions__CloudName`, `CloudinaryOptions__ApiKey`, `CloudinaryOptions__ApiSecret`

### Blockchain (Ethereum Sepolia)

- Dùng để tạo bằng chứng blockchain cho tài liệu (document proof)
- Hai chế độ:
  - `Stub` (mặc định): Mock service, không cần setup blockchain
  - `Ethereum`: Kết nối thật tới Sepolia testnet
- Chain ID: `11155111` (Sepolia)

### Email (Gmail SMTP)

- Gửi email xác thực tài khoản, reset mật khẩu, thông báo
- SMTP: `smtp.gmail.com:587` (TLS)
- Cần sử dụng **App Password** (không dùng mật khẩu Gmail trực tiếp)

---

## Triển khai Production

### 1. Build ứng dụng

```bash
dotnet publish src/AISEP.WebAPI -c Release -o ./publish
```

### 2. Cấu hình Production

Tạo file `.env` trên server hoặc đặt biến môi trường hệ thống:

```bash
# Bắt buộc thay đổi cho production
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000

# Database
ConnectionStrings__DefaultConnection=Host=<db-host>;Port=5432;Database=AISEP;Username=<user>;Password=<strong-password>;SSL Mode=Require;

# JWT - Sử dụng key mạnh, dài (>= 64 ký tự)
Jwt__SecretKey=<production-secret-key-64-chars-minimum>

# Email
Email__SmtpUser=<production-email>
Email__SmtpPassword=<app-password>
Email__FromEmail=<production-email>
Email__BaseUrl=https://your-domain.com

# Cloudinary
CloudinaryOptions__CloudName=<name>
CloudinaryOptions__ApiKey=<key>
CloudinaryOptions__ApiSecret=<secret>

# CORS - Đổi sang domain production
Cors__AllowedOrigins__0=https://your-frontend-domain.com

# Blockchain
Blockchain__Provider=Ethereum
Blockchain__RpcUrl=<production-rpc-url>
Blockchain__ContractAddress=<contract-address>
Blockchain__PrivateKey=<wallet-private-key>
```

### 3. Chạy ứng dụng trên server

```bash
cd publish
dotnet AISEP.WebAPI.dll
```

### 4. Triển khai với Reverse Proxy (Nginx)

Cấu hình Nginx làm reverse proxy:

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # SignalR WebSocket support
    location /hubs/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### 5. Chạy như System Service (Linux)

Tạo file `/etc/systemd/system/aisep.service`:

```ini
[Unit]
Description=AISEP Capstone Backend
After=network.target postgresql.service

[Service]
WorkingDirectory=/var/www/aisep/publish
ExecStart=/usr/bin/dotnet /var/www/aisep/publish/AISEP.WebAPI.dll
Restart=always
RestartSec=10
SyslogIdentifier=aisep
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:5000
EnvironmentFile=/var/www/aisep/.env

[Install]
WantedBy=multi-user.target
```

Kích hoạt service:

```bash
sudo systemctl enable aisep
sudo systemctl start aisep
sudo systemctl status aisep
```

### 6. Triển khai với Docker (Tùy chọn)

Tạo `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AISEP.WebAPI/AISEP.WebAPI.csproj", "AISEP.WebAPI/"]
COPY ["src/AISEP.Application/AISEP.Application.csproj", "AISEP.Application/"]
COPY ["src/AISEP.Domain/AISEP.Domain.csproj", "AISEP.Domain/"]
COPY ["src/AISEP.Infrastructure/AISEP.Infrastructure.csproj", "AISEP.Infrastructure/"]
RUN dotnet restore "AISEP.WebAPI/AISEP.WebAPI.csproj"
COPY src/ .
RUN dotnet publish "AISEP.WebAPI/AISEP.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "AISEP.WebAPI.dll"]
```

Tạo `docker-compose.yml`:

```yaml
version: "3.8"

services:
  db:
    image: postgres:16
    environment:
      POSTGRES_DB: AISEP
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: your_password
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

  api:
    build: .
    ports:
      - "5000:5000"
    depends_on:
      - db
    env_file:
      - ./src/AISEP.WebAPI/.env
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=AISEP;Username=postgres;Password=your_password

volumes:
  pgdata:
```

Chạy với Docker Compose:

```bash
docker-compose up -d
```

---

## Xử lý sự cố

### Lỗi kết nối Database

```
Npgsql.NpgsqlException: Failed to connect to localhost:5432
```

**Giải pháp:**
1. Kiểm tra PostgreSQL đang chạy: `pg_isready`
2. Kiểm tra connection string trong `.env`
3. Kiểm tra user/password PostgreSQL
4. Đảm bảo database `AISEP` đã được tạo

### Lỗi Migration

```
The migration '...' has already been applied to the database
```

**Giải pháp:**
```bash
# Xem danh sách migration
dotnet ef migrations list --project src/AISEP.Infrastructure --startup-project src/AISEP.WebAPI

# Nếu cần reset, xóa database và chạy lại
dotnet ef database drop --project src/AISEP.Infrastructure --startup-project src/AISEP.WebAPI
dotnet ef database update --project src/AISEP.Infrastructure --startup-project src/AISEP.WebAPI
```

### Lỗi JWT

```
401 Unauthorized
```

**Giải pháp:**
1. Kiểm tra `Jwt__SecretKey` trong `.env` (tối thiểu 32 ký tự)
2. Kiểm tra token chưa hết hạn
3. Kiểm tra header: `Authorization: Bearer <token>` (có khoảng trắng sau "Bearer")

### Lỗi CORS

```
Access-Control-Allow-Origin header is missing
```

**Giải pháp:**
1. Kiểm tra `Cors:AllowedOrigins` trong `appsettings.json` hoặc biến môi trường
2. Đảm bảo URL frontend khớp chính xác (bao gồm port)
3. Mặc định cho phép: `http://localhost:3000`

### Lỗi Cloudinary Upload

```
Account not found
```

**Giải pháp:**
1. Kiểm tra `CloudinaryOptions__CloudName`, `ApiKey`, `ApiSecret` trong `.env`
2. Đăng nhập Cloudinary dashboard để xác nhận thông tin

### Lỗi Email

```
Authentication failed - SmtpCommandException
```

**Giải pháp:**
1. Bật **2-Step Verification** trên Google Account
2. Tạo **App Password** (không dùng mật khẩu Google trực tiếp)
3. Kiểm tra `Email__SmtpUser` và `Email__SmtpPassword` trong `.env`

### Xem log ứng dụng

Log được lưu tại thư mục `logs/` (rolling daily):

```bash
# Xem log hôm nay
cat logs/aisep-20260330.log

# Theo dõi log real-time
tail -f logs/aisep-*.log
```

---

## Liên hệ & Hỗ trợ

- **Repository:** [AISEP_Capstone-BE](https://github.com/tsohigh254/AISEP_Capstone-BE)
- **Swagger API Docs:** `http://localhost:5294/swagger` (khi chạy local)
