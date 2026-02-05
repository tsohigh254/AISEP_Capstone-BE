# AISEP_Capstone-BE
# AISEP Backend (ASP.NET Core Web API) — README for AI Coding

> Mục tiêu file này: để AI đọc và có thể **code đầy đủ hệ thống API** theo scope dự án AISEP (Startup–Investor–Advisor–Staff–Admin), gồm Auth/RBAC, Startup, Documents + Blockchain proof, AI scoring, Mentorship, Connections, Chat, Notifications, Moderation, Audit, Staff/Admin.

---

## 0) Tổng quan
AISEP = AI-powered Startup Ecosystem Platform:
- **Startup**: tạo hồ sơ, upload PitchDeck/BusinessPlan, bảo vệ IP (blockchain proof), AI đánh giá tiềm năng.
- **Investor**: tìm kiếm startup, watchlist, AI trends + recommendations, kết nối/offer.
- **Advisor**: profile + expertise + availability, nhận mentorship request, sessions/reports/feedback.
- **Staff/Ops**: duyệt KYC, duyệt startup/doc/reports, xử lý flags.
- **Admin**: quản lý user, RBAC, config AI/Blockchain, audit logs, workflows, incidents.

---

## 1) Tech Stack (khuyến nghị)
- .NET 8, ASP.NET Core Web API
- EF Core + SQL Server (hoặc PostgreSQL)
- JWT Bearer Authentication + Refresh Token (DB)
- Swagger/OpenAPI
- FluentValidation (validate DTO)
- Serilog (logging)
- Background Jobs: Hangfire/Quartz (cho AI evaluate async + polling blockchain tx)
- Storage: Local Disk (dev) / S3/Azure Blob (prod)
- Optional: Redis (cache, rate limit)

---

## 2) Quy ước API chung
### Base URL
- `/api` (có thể đổi thành `/api/v1`)

### Response envelope (thống nhất)
**Success**
```json
{
  "success": true,
  "data": { },
  "message": null
}
Error

{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input",
    "details": [{ "field": "email", "message": "Email is required" }]
  }
}
Pagination / Sorting (cho list endpoints)
Query chuẩn:

page (default 1)

pageSize (default 20, max 100)

sortBy (vd: createdAt)

sortDir (asc|desc)

q keyword (nếu cần)

Response list:

{
  "items": [],
  "paging": { "page": 1, "pageSize": 20, "totalItems": 0, "totalPages": 0 }
}
HTTP status conventions
200/201 success

400 validation

401 unauthenticated

403 forbidden

404 not found

409 conflict (duplicate, concurrency)

422 business rule violation

500 server

3) Security & RBAC
Roles
Startup, Investor, Advisor, Staff, Admin

Authorization
Dùng [Authorize] + policy-based (khuyến nghị):

Policies.StartupOnly

Policies.InvestorOnly

Policies.AdvisorOnly

Policies.StaffOrAdmin

Policies.AdminOnly

JWT claims đề xuất
sub = UserID

email

roles = [..]

userType

Refresh token
Lưu DB: RefreshTokens (nếu ERD chưa có thì tạo table)

Token, UserId, ExpiresAt, RevokedAt, CreatedAt, Ip, UserAgent

4) Database & Entity mapping (tối thiểu)
AI cần tạo Entities + DbContext + Fluent API mappings theo ERD (tên bảng có thể theo PascalCase).
Các nhóm bảng chính:

Auth/RBAC: Users, Roles, Permissions, UserRoles, RolePermissions

Startup: Startups, TeamMembers

Docs: Documents, DocumentBlockchainProofs

AI: StartupPotentialScores, ScoreSubMetrics, ScoreImprovementRecommendations, ScoringModelConfigurations

Investor: Investors, InvestorPreferences, InvestorIndustryFocus, InvestorStageFocus, PortfolioCompanies, InvestorWatchlists

Advisor: Advisors, AdvisorAvailability, AdvisorExpertise, AdvisorIndustryFocus, AdvisorAchievements, AdvisorTestimonials

Mentorship: StartupAdvisorMentorships, MentorshipSessions, MentorshipReports, MentorshipFeedbacks

Investor connection: StartupInvestorConnections, InformationRequests

Chat: Conversations, Messages

System: Notifications, AuditLogs, FlaggedContent, ModerationActions, ProfileViews

Nếu thiếu bảng cho KYC / SystemSettings / Incidents / RefreshTokens thì tạo thêm (migration) vì API cần.

5) Kiến trúc code (bắt buộc để AI code nhanh & sạch)
Folder structure
src/
  Api/
    Controllers/
    Middlewares/
    Filters/
    Program.cs
  Application/
    DTOs/
    Validators/
    Interfaces/
    Services/
    Policies/
  Domain/
    Entities/
    Enums/
  Infrastructure/
    Persistence/ (DbContext, Migrations)
    Repositories/
    External/
      AiProvider/
      Blockchain/
      Storage/
    Logging/
tests/
  Api.Tests/
  Application.Tests/
Pattern
Controller chỉ nhận request + auth + gọi service

Business logic đặt ở Application/Services

DB access qua Repositories (hoặc trực tiếp DbContext nhưng phải tách rõ)

DTO tách Request/Response, không expose Entity trực tiếp

Mọi service method async + CancellationToken

6) External integrations (AI + Blockchain + Storage)
StorageService
SaveAsync(IFormFile file) -> StoredFileInfo { Url, Path, Size, ContentType }

DeleteAsync(path)

Dev: lưu local /uploads/{userId}/{startupId}/...

Prod: S3/Azure Blob

Document hash
Tính SHA-256 của file bytes

Lưu hash vào Documents.FileHash hoặc DocumentBlockchainProofs.FileHash (khuyến nghị lưu cả 2)

BlockchainService
SubmitHashAsync(fileHash, metadata) -> { txHash }

GetTxStatusAsync(txHash) -> Pending|Confirmed|Failed

VerifyHashAsync(fileHash) -> bool (đọc từ contract / event log)

Background job polling tx status và update DocumentBlockchainProofs.ProofStatus

AIService
EvaluateDocumentAsync(documentId) -> ScoreResult

Output phải lưu vào:

StartupPotentialScores (overall)

ScoreSubMetrics (sub-scores)

ScoreImprovementRecommendations (recommendations)

Nên chạy async (job) vì lâu:

POST /ai/evaluate/{documentId} => trả jobId hoặc analysisStatus

Có endpoint check status nếu cần

7) API Spec (FINAL) — endpoints theo module & role
7.1 AuthController
POST /api/auth/register (Anonymous)

POST /api/auth/login (Anonymous)

POST /api/auth/refresh-token (Anonymous/Authenticated)

POST /api/auth/logout (Authenticated)

GET /api/auth/verify-email (Anonymous)

POST /api/auth/resend-verification (Anonymous)

POST /api/auth/forgot-password (Anonymous)

POST /api/auth/reset-password (Anonymous)

7.2 UsersController
GET /api/users/me (Authenticated)

PUT /api/users/me (Authenticated)

PUT /api/users/me/change-password (Authenticated)

Staff/Admin

GET /api/users (Staff/Admin)

GET /api/users/{userId} (Staff/Admin)

PATCH /api/users/{userId} (Staff/Admin)

POST /api/users/{userId}/lock (Admin)

POST /api/users/{userId}/unlock (Admin)

7.3 RBAC (Admin)
GET /api/roles

POST /api/roles

PUT /api/roles/{roleId}

DELETE /api/roles/{roleId}

GET /api/permissions

GET /api/roles/{roleId}/permissions

PUT /api/roles/{roleId}/permissions

GET /api/users/{userId}/roles

POST /api/users/{userId}/roles

DELETE /api/users/{userId}/roles/{roleId}

7.4 NotificationsController
GET /api/notifications

GET /api/notifications/{id}

PUT /api/notifications/{id}/read

PUT /api/notifications/read-all

7.5 MasterDataController
GET /api/master/industries

GET /api/master/stages

GET /api/master/roles

7.6 StartupsController
POST /api/startups (Startup)

GET /api/startups/me (Startup)

PUT /api/startups/me (Startup)

POST /api/startups/me/submit-for-approval (Startup)

GET /api/startups/{startupId} (Startup/Investor/Advisor)

GET /api/startups (Investor/Advisor/Public tùy policy)

Team members

GET /api/startups/me/team-members (Startup)

POST /api/startups/me/team-members (Startup)

PUT /api/startups/me/team-members/{teamMemberId} (Startup)

DELETE /api/startups/me/team-members/{teamMemberId} (Startup)

7.7 DocumentsController
POST /api/documents (Startup) — multipart upload

GET /api/documents (Startup)

GET /api/documents/{documentId} (Startup)

GET /api/documents/{documentId}/download (Startup)

PUT /api/documents/{documentId}/metadata (Startup)

DELETE /api/documents/{documentId} (Startup) — soft delete

Staff

GET /api/staff/documents/pending (Staff/Admin)

POST /api/staff/documents/{documentId}/approve (Staff/Admin)

POST /api/staff/documents/{documentId}/reject (Staff/Admin)

7.8 Blockchain proof
POST /api/documents/{documentId}/hash (Startup)

POST /api/documents/{documentId}/submit-chain (Startup)

GET /api/documents/{documentId}/verify-chain (Startup/Staff)

GET /api/documents/{documentId}/chain/tx-status (Startup/Staff)

POST /api/staff/documents/{documentId}/verify-hash (Staff/Admin)

7.9 AIController
POST /api/ai/evaluate/{documentId} (Startup)

GET /api/ai/scores/latest (Startup)

GET /api/ai/history (Startup)

GET /api/ai/reports/{startupId} (Startup owner / Staff / Investor nếu được share)

GET /api/ai/trends (Investor)

GET /api/investors/recommendations (Investor)

Admin configs

GET /api/admin/scoring-model-configs (Admin)

POST /api/admin/scoring-model-configs (Admin)

PUT /api/admin/scoring-model-configs/{configId} (Admin)

POST /api/admin/scoring-model-configs/{configId}/activate (Admin)

7.10 InvestorsController
POST /api/investors (Investor)

GET /api/investors/me (Investor)

PUT /api/investors/me (Investor)

Preferences

GET /api/investors/me/preferences (Investor)

PUT /api/investors/me/preferences (Investor)

GET /api/investors/me/industry-focus (Investor)

POST /api/investors/me/industry-focus (Investor)

DELETE /api/investors/me/industry-focus/{id} (Investor)

GET /api/investors/me/stage-focus (Investor)

POST /api/investors/me/stage-focus (Investor)

DELETE /api/investors/me/stage-focus/{id} (Investor)

KYC

POST /api/investors/me/kyc (Investor)

GET /api/investors/me/kyc/status (Investor)

Watchlist

GET /api/investors/me/watchlist (Investor)

POST /api/investors/me/watchlist (Investor)

DELETE /api/investors/me/watchlist/{startupId} (Investor)

GET /api/investors/me/watchlist/history (Investor)

Portfolio

GET /api/investors/me/portfolio (Investor)

POST /api/investors/me/portfolio (Investor)

PUT /api/investors/me/portfolio/{portfolioId} (Investor)

DELETE /api/investors/me/portfolio/{portfolioId} (Investor)

Compare

POST /api/investors/compare (Investor)

7.11 AdvisorsController
POST /api/advisors (Advisor)

GET /api/advisors/me (Advisor)

PUT /api/advisors/me (Advisor)

GET /api/advisors/search (Startup)

Expertise & availability

GET /api/advisors/me/expertise (Advisor)

PUT /api/advisors/me/expertise (Advisor)

GET /api/advisors/me/availability (Advisor)

PUT /api/advisors/me/availability (Advisor)

Advisor KYC

POST /api/advisors/me/kyc (Advisor)

GET /api/advisors/me/kyc/status (Advisor)

POST /api/advisors/me/kyc/resubmit (Advisor)

7.12 MentorshipsController (Consulting)
Requests

POST /api/mentorships (Startup)

GET /api/mentorships (Startup/Advisor)

GET /api/mentorships/{id} (Startup/Advisor)

POST /api/mentorships/{id}/accept (Advisor)

POST /api/mentorships/{id}/reject (Advisor)

POST /api/mentorships/{id}/complete (Startup/Advisor)

POST /api/mentorships/{id}/confirm-completion (Startup)

Sessions

GET /api/mentorships/{id}/sessions (Startup/Advisor)

POST /api/mentorships/{id}/sessions (Advisor)

PUT /api/sessions/{sessionId} (Advisor)

POST /api/sessions/{sessionId}/reschedule (Startup/Advisor)

POST /api/sessions/{sessionId}/cancel (Startup/Advisor)

GET /api/advisors/me/schedule (Advisor)

Reports

GET /api/mentorships/{id}/reports (Startup/Advisor)

POST /api/mentorships/{id}/reports (Advisor)

GET /api/reports/{reportId} (Startup/Advisor)

PUT /api/reports/{reportId} (Advisor)

POST /api/reports/{reportId}/submit (Advisor)

POST /api/reports/{reportId}/attachments (Advisor)

GET /api/reports/history (Advisor)

POST /api/staff/reports/{reportId}/validate (Staff/Admin)

Feedback

POST /api/mentorships/{id}/feedback (Startup)

GET /api/advisors/{advisorId}/feedbacks (Advisor/Admin)

7.13 ConnectionsController (Offers) + InfoRequests
Connections

POST /api/connections (Investor)

GET /api/connections (Startup/Investor)

GET /api/connections/{id} (Startup/Investor)

PUT /api/connections/{id} (Investor)

POST /api/connections/{id}/accept (Startup)

POST /api/connections/{id}/reject (Startup)

POST /api/connections/{id}/withdraw (Investor)

POST /api/connections/{id}/close (Startup/Investor)

Information requests

POST /api/connections/{id}/info-requests (Investor)

GET /api/connections/{id}/info-requests (Startup/Investor)

POST /api/info-requests/{requestId}/fulfill (Startup)

Spam/Abuse flags

POST /api/connections/{id}/flag (Startup/Investor)

GET /api/staff/connections/flags (Staff/Admin)

PUT /api/staff/connections/flags/{flagId}/resolve (Staff/Admin)

7.14 Chat: Conversations & Messages
POST /api/conversations (Authenticated)

GET /api/conversations (Authenticated)

GET /api/conversations/{id} (Authenticated)

GET /api/conversations/{id}/messages (Authenticated)

POST /api/conversations/{id}/messages (Authenticated)

PUT /api/messages/{messageId}/read (Authenticated)

PUT /api/conversations/{id}/read-all (Authenticated)

7.15 Moderation (Flags + Actions)
POST /api/flags (Authenticated)

GET /api/staff/flags (Staff/Admin)

GET /api/staff/flags/{flagId} (Staff/Admin)

POST /api/staff/moderation-actions (Staff/Admin)

GET /api/staff/moderation-actions (Staff/Admin)

7.16 Audit logs (Admin)
GET /api/admin/audit-logs

GET /api/admin/audit-logs/{id}

7.17 Staff approvals
GET /api/staff/approvals/users (Staff/Admin)

POST /api/staff/approvals/users/{userId}/approve (Staff/Admin)

POST /api/staff/approvals/users/{userId}/reject (Staff/Admin)

GET /api/staff/startups/pending (Staff/Admin)

POST /api/staff/startups/{startupId}/approve (Staff/Admin)

POST /api/staff/startups/{startupId}/reject (Staff/Admin)

GET /api/staff/logs (Staff/Admin) — optional

7.18 Admin system
GET /api/admin/users (Admin)

PUT /api/admin/users/{id}/lock (Admin)

PUT /api/admin/users/{id}/unlock (Admin)

GET /api/admin/roles/matrix (Admin)

PUT /api/admin/roles/matrix (Admin)

PUT /api/admin/config/ai (Admin)

PUT /api/admin/config/blockchain (Admin)

PUT /api/admin/workflows (Admin)

GET /api/admin/system-health (Admin) — optional

GET /api/admin/violation-reports (Admin)

PUT /api/admin/violation-reports/{id}/resolve (Admin)

POST /api/admin/incidents (Admin) — optional

POST /api/admin/incidents/{id}/rollback (Admin) — optional

8) DTO Requirements (AI phải tạo đầy đủ)
Mỗi endpoint cần:

Request DTO (vd: RegisterRequest, CreateStartupRequest, UploadDocumentRequest…)

Response DTO (vd: AuthResponse, StartupDto, DocumentDto, ScoreDto…)

Validation rules (FluentValidation)

Mapping (AutoMapper hoặc manual)

9) Audit logging (bắt buộc)
Mọi action quan trọng phải ghi AuditLogs:

Auth events (login success/fail), change password

CRUD Startup, Documents, Mentorship, Connections

Staff/Admin approvals & moderation actions
Format đề xuất: EntityType, EntityId, Action, PerformedBy, PerformedAt, Metadata(JSON).

10) Definition of Done (DoD) cho AI code
Khi code xong 1 module:

Controller + Route + Swagger tag

Service interface + implementation

Repository (nếu dùng) + EF queries đúng quyền

DTO + Validation

Authorization theo role/policy

Error handling + response envelope

Unit tests tối thiểu cho business rules chính

Migration nếu thêm bảng/field

11) Local Setup (template)
appsettings.Development.json
ConnectionStrings:Default

Jwt:Issuer, Jwt:Audience, Jwt:Key, Jwt:AccessTokenMinutes, Jwt:RefreshTokenDays

Storage:Provider = Local|S3|Azure

Storage:LocalPath = ./uploads

Ai:Provider, Ai:ApiKey, Ai:Model

Blockchain:RpcUrl, Blockchain:ChainId, Blockchain:ContractAddress, Blockchain:PrivateKey (hoặc signer service)

Run
dotnet ef database update

dotnet run --project src/Api

12) Notes quan trọng
File upload: giới hạn size + whitelist filetype (pdf/pptx/docx).

Document permissions: chỉ owner Startup (và Staff/Admin) xem file raw; Investor xem khi Startup share qua connection/info-request (nếu bạn bật).

AI evaluation: nên lưu “status” (Pending/Running/Done/Failed) cho từng document hoặc job.

Blockchain tx status: polling job update proof status.

Không được expose nội dung nhạy cảm qua list APIs (mask email/phone nếu public).

13) Output expectations (cho AI làm việc)
Khi bắt đầu code, AI nên:

Generate solution .sln + projects theo structure

Implement Auth + Users + RBAC trước

Implement Startups + Documents + Storage + Hash

Implement Blockchain proof + tx polling

Implement AI scoring pipeline

Implement Investor/Advisor/Mentorship/Connections

Implement Chat + Notifications

Implement Staff/Admin + Moderation + Audit logs

Add Swagger docs + sample requests