# AISEP Backend

**AISEP** (AI-powered Startup Ecosystem Platform) — .NET 8 Web API kết nối Startup, Investor, Advisor, Staff, Admin. Hỗ trợ quản lý tài liệu với blockchain proof-of-integrity, AI scoring cho startup, mentorship workflow, investor matching, chat, moderation và RBAC.

## Tech Stack

- **.NET 8** + ASP.NET Core Web API
- **PostgreSQL** (Npgsql) + EF Core
- **JWT Bearer** auth + refresh token
- **FluentValidation**, **Serilog**, **Swagger**
- **Resend** (email), **Cloudinary** (image), local disk (file)
- **xUnit** + FluentAssertions + Moq + EF Core InMemory (tests)

Architecture: Clean layered — `Domain ← Application ← Infrastructure ← WebAPI`.

## Quick Start

```bash
# 1. Copy env template và điền giá trị thật
cp src/AISEP.WebAPI/.env.example src/AISEP.WebAPI/.env

# 2. Apply migrations
dotnet ef database update \
  --project src/AISEP.Infrastructure \
  --startup-project src/AISEP.WebAPI

# 3. Chạy API
dotnet run --project src/AISEP.WebAPI

# 4. Mở Swagger UI (Development only)
# http://localhost:5000/swagger
```

## Commands

```bash
# Build toàn solution
dotnet build AISEP.sln

# Run tests
dotnet test

# Thêm migration mới
dotnet ef migrations add <Name> \
  --project src/AISEP.Infrastructure \
  --startup-project src/AISEP.WebAPI
```

## Cấu trúc

```
src/
├── AISEP.Domain/          # Entities + enums, không dependency
├── AISEP.Application/     # DTOs, service interfaces, validators config
├── AISEP.Infrastructure/  # EF Core DbContext, service implementations, external integrations
└── AISEP.WebAPI/          # Controllers, middleware, DI, Program.cs

tests/AISEP.Tests/         # Unit tests
contracts/                 # Blockchain smart contracts (Solidity)
docs/                      # Implementation guides + FE handoff docs
```

## Documentation

- **`CLAUDE.md`** — Project instructions cho Claude Code
- **`docs/`** — Implementation guides, FE handoff, integration docs
- **`Function_V0.pdf`** (repo root) — API specification đầy đủ
- **`Report3_SRS.docx`** (repo root) — Software Requirement Specification

## Environment Variables

Xem `src/AISEP.WebAPI/.env.example` để biết các biến cần cấu hình:
- `ConnectionStrings__DefaultConnection` (PostgreSQL)
- `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience`
- `Email__ResendApiKey`, `Email__FromEmail`
- `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`
- `Blockchain__*` (optional)

## Deployment

Production deploy qua Docker Compose — xem `AISEP-Capstone-Infra` repo cho config chính thức (`docker-compose.yml`, `nginx-docker.conf`).
