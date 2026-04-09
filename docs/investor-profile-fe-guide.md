# Investor Profile — Thiết kế UI/UX

> Tài liệu này mô tả layout màn hình, luồng người dùng, logic hiển thị theo trạng thái và loại investor.  
> Dành cho FE implement — không phải API reference.  
> API reference (endpoints, DTOs) xem tại: `kyc-fe-guide.md`

---

## Mục lục

1. [Tổng quan 2 loại Investor](#1-tổng-quan-2-loại-investor)
2. [Trạng thái hồ sơ & logic hiển thị](#2-trạng-thái-hồ-sơ--logic-hiển-thị)
3. [Màn hình: Xem Profile (View)](#3-màn-hình-xem-profile-view)
4. [Màn hình: Onboarding — Tạo profile lần đầu](#4-màn-hình-onboarding--tạo-profile-lần-đầu)
5. [Màn hình: KYC — Gửi xác thực](#5-màn-hình-kyc--gửi-xác-thực)
6. [Màn hình: Chỉnh sửa profile](#6-màn-hình-chỉnh-sửa-profile)
7. [Màn hình: Investment Preferences](#7-màn-hình-investment-preferences)
8. [Luồng tổng thể (Flow Diagram)](#8-luồng-tổng-thể-flow-diagram)

---

## 1. Tổng quan 2 loại Investor

Toàn bộ UI của trang Profile **thích nghi theo loại investor**. Loại được chọn trong bước KYC và **không thể đổi sau khi đã được duyệt** mà không cần resubmit KYC.

| | **Angel Investor** (`INDIVIDUAL_ANGEL`) | **Tổ chức / Quỹ** (`INSTITUTIONAL`) |
|---|---|---|
| Đại diện cho | Cá nhân | Quỹ đầu tư, công ty đầu tư |
| Tên hiển thị nổi bật | Tên người (`fullName`) | Tên quỹ (`firmName`) |
| Thông tin đặc trưng | Bio cá nhân, kinh nghiệm | Tên tổ chức, vai trò đại diện |
| Badge xác thực | ✓ Verified Angel Investor | ✓ Verified Investor Entity |
| Form KYC | Đơn giản hơn | Có thêm: tên quỹ, mã số thuế, vai trò |
| Giấy tờ KYC | CCCD + bằng chứng đầu tư | Giấy phép hoạt động + CCCD đại diện |

> **Lưu ý FE:** Trường `investorType` trong response `/me` hiện bị `null` do bug BE chưa fix. Tạm thời đọc từ `GET /api/investors/me/kyc` → `submissionSummary.investorCategory`.

---

## 2. Trạng thái hồ sơ & logic hiển thị

Investor có thể ở một trong 5 trạng thái. Mỗi trạng thái quyết định toàn bộ giao diện trang profile.

```
profileStatus
├── "Draft"       → Chưa hoàn thiện, chưa KYC
├── "PendingKYC"  → Đã gửi KYC, chờ staff duyệt
├── "Approved"    → KYC được duyệt, full access   ← trạng thái bình thường
└── "Rejected"    → KYC bị từ chối
```

### Banner trạng thái — hiển thị cố định trên đầu trang profile

```
┌─────────────────────────────────────────────────────────────┐
│  Draft:       ⚠️  Hồ sơ chưa hoàn thiện                    │
│               [Hoàn thiện ngay →]                           │
├─────────────────────────────────────────────────────────────┤
│  PendingKYC:  ⏳  Hồ sơ của bạn đang được xét duyệt        │
│               Thường mất 1–3 ngày làm việc                  │
├─────────────────────────────────────────────────────────────┤
│  Rejected:    ❌  Hồ sơ bị từ chối                          │
│               Lý do: "{remarks từ staff}"                   │
│               [Cập nhật & gửi lại →]                        │
└─────────────────────────────────────────────────────────────┘
```

Khi `Approved`: **Không hiện banner**, chỉ hiện badge xác thực trên avatar.

---

## 3. Màn hình: Xem Profile (View)

Đây là màn hình chính khi vào `/investors/me` hoặc người khác xem profile công khai.

### 3.1 Layout tổng thể

```
┌──────────────────────────────────────────────────────┐
│  [BANNER TRẠNG THÁI — nếu không phải Approved]       │
├──────────────────────────────────────────────────────┤
│                                                      │
│   [Avatar 80px]   Nguyen Van A                       │
│   (+ nút edit)    ✓ Verified Angel Investor  ← badge │
│                   Managing Partner                   │
│                   📍 TP.HCM, Việt Nam                │
│                   🔗 LinkedIn   🌐 Website           │
│                                          [Chỉnh sửa] │
├──────────────────────────────────────────────────────┤
│  [Giới thiệu]   [Đầu tư]   [Watchlist]              │
├──────────────────────────────────────────────────────┤
│  (nội dung tab — xem 3.2, 3.3, 3.4)                 │
└──────────────────────────────────────────────────────┘
```

---

### 3.2 Tab "Giới thiệu" — nội dung khác nhau theo loại

**Khi `investorType = "INDIVIDUAL_ANGEL"` (hoặc chưa xác định):**

```
┌─────────────────────────────────┐
│  Về tôi                         │
│  ───────────────────────────── │
│  {bio}                          │
│                                 │
│  Luận điểm đầu tư               │
│  ───────────────────────────── │
│  {investmentThesis}             │
│                                 │
│  Liên hệ  (chỉ hiện sau KYC)   │
│  ───────────────────────────── │
│  ✉ {contactEmail}               │
└─────────────────────────────────┘
```

**Khi `investorType = "INSTITUTIONAL"`:**

```
┌─────────────────────────────────┐
│  [Logo quỹ / avatar]            │
│  ABC Capital Fund               │  ← firmName nổi bật hơn
│  Quỹ đầu tư                     │  ← label loại tổ chức
│                                 │
│  Người đại diện                 │
│  ───────────────────────────── │
│  👤 Nguyen Van A — Fund Manager │  ← fullName + submitterRole
│                                 │
│  Giới thiệu tổ chức             │
│  ───────────────────────────── │
│  {bio}                          │
│  {investmentThesis}             │
│                                 │
│  Liên hệ  (chỉ hiện sau KYC)   │
│  ───────────────────────────── │
│  ✉ {contactEmail}               │
│  🌐 {website}                   │
└─────────────────────────────────┘
```

> **Không bao giờ hiển thị** `businessCode` (mã số thuế) ra ngoài — chỉ nội bộ staff.

---

### 3.3 Tab "Đầu tư" — dùng chung cả 2 loại

```
┌─────────────────────────────────┐
│  Quy mô đầu tư                  │
│  $50,000 – $500,000             │
│                                 │
│  Giai đoạn quan tâm             │
│  [Seed]  [Series A]             │
│                                 │
│  Lĩnh vực quan tâm              │
│  [Fintech]  [HealthTech]  [AI]  │
│                                 │
│  Thị trường                     │
│  Vietnam, Singapore             │
│                                 │
│                  [Cập nhật →]   │
└─────────────────────────────────┘
```

Nếu chưa điền preferences: hiện placeholder "Chưa có thông tin đầu tư" + nút "Cập nhật ngay".

---

### 3.4 Tab "Watchlist" — dùng chung

```
┌─────────────────────────────────┐
│  [Logo]  TechViet AI            │
│           Fintech · Seed        │
│           ⭐ Ưu tiên cao         │  ← priority: High
│           Thêm vào: 12/03/2026  │
│                         [Xoá]   │
├─────────────────────────────────┤
│  [Logo]  GreenFarm              │
│           AgriTech · Series A   │
│           ● Bình thường         │  ← priority: Medium
│  ...                            │
└─────────────────────────────────┘
```

Watchlist **chỉ investor thấy** — không hiển thị công khai ra ngoài.

---

## 4. Màn hình: Onboarding — Tạo profile lần đầu

Hiển thị khi investor đăng nhập lần đầu và chưa có profile (`/me` trả về `data: null`).

```
┌─────────────────────────────────────────┐
│  Chào mừng! Hãy tạo hồ sơ của bạn      │
│                                         │
│  [Nhấn để upload ảnh đại diện]          │
│                                         │
│  Họ và tên *      [_______________]     │
│                                         │
│  Chức danh        [_______________]     │
│                                         │
│  Giới thiệu bản thân                    │
│  [                               ]      │
│  [         textarea               ]     │
│  [                               ]      │
│                                         │
│  Địa điểm         [_______________]     │
│  Quốc gia         [_______________]     │
│  LinkedIn         [_______________]     │
│  Website          [_______________]     │
│                                         │
│                    [Tạo hồ sơ →]        │
└─────────────────────────────────────────┘
```

**Sau khi tạo xong:** Chuyển đến trang profile (trạng thái `Draft`), hiện banner nhắc hoàn thiện KYC.

> Bước này **chưa hỏi loại investor** — loại được chọn trong luồng KYC riêng biệt (mục 5).

---

## 5. Màn hình: KYC — Gửi xác thực

Truy cập từ: banner "Hoàn thiện ngay" → `/investors/kyc`

Gồm 3 bước. Có stepper: `① Loại investor → ② Thông tin → ③ Giấy tờ`

---

### Bước 1 — Chọn loại investor

```
┌─────────────────────────────────────────┐
│        ① ──────── ② ──────── ③          │
│                                         │
│  Bạn đầu tư theo tư cách nào?           │
│                                         │
│  ┌──────────────────────────────────┐   │
│  │  👤  Angel Investor   [ Chọn ]   │   │
│  │      Đầu tư với tư cách cá nhân  │   │
│  └──────────────────────────────────┘   │
│                                         │
│  ┌──────────────────────────────────┐   │
│  │  🏢  Tổ chức / Quỹ   [ Chọn ]   │   │
│  │      Đại diện cho quỹ đầu tư     │   │
│  │      hoặc công ty đầu tư         │   │
│  └──────────────────────────────────┘   │
│                                         │
│  ⚠️  Lựa chọn này không thể thay đổi   │
│      sau khi hồ sơ được phê duyệt.      │
│                                         │
│         [← Quay lại]   [Tiếp theo →]   │
└─────────────────────────────────────────┘
```

Nếu đã có draft/submission trước: **lock** lựa chọn, hiện dạng readonly.

---

### Bước 2 — Thông tin chi tiết theo loại

**Form khi chọn Angel Investor:**

```
┌─────────────────────────────────────────┐
│        ① ────────[②]──────── ③          │
│                                         │
│  Thông tin xác thực — Angel Investor    │
│                                         │
│  Họ và tên đầy đủ *   [_____________]  │
│  Email liên hệ *       [_____________]  │
│  Chức danh             [_____________]  │
│  Địa điểm              [_____________]  │
│  LinkedIn              [_____________]  │
│  Website               [_____________]  │
│                                         │
│         [← Bước 1]    [Tiếp theo →]    │
└─────────────────────────────────────────┘
```

**Form khi chọn Tổ chức / Quỹ:**

```
┌─────────────────────────────────────────┐
│        ① ────────[②]──────── ③          │
│                                         │
│  Thông tin xác thực — Tổ chức           │
│                                         │
│  Tên quỹ / tổ chức *  [_____________]  │  ← organizationName
│  Họ tên người đại diện * [___________]  │  ← fullName
│  Vai trò đại diện *   [_____________]  │  ← submitterRole
│    e.g. Fund Manager, General Partner  │
│  Email liên hệ *       [_____________]  │
│  Mã số thuế / ĐKKD     [_____________]  │  ← taxIdOrBusinessCode
│  Địa điểm              [_____________]  │
│  Website               [_____________]  │
│  LinkedIn              [_____________]  │
│                                         │
│         [← Bước 1]    [Tiếp theo →]    │
└─────────────────────────────────────────┘
```

---

### Bước 3 — Upload giấy tờ

**Angel Investor:**

```
┌─────────────────────────────────────────┐
│        ① ──────── ② ────────[③]         │
│                                         │
│  Giấy tờ xác thực                       │
│                                         │
│  CCCD / Hộ chiếu *              (ID_PROOF)
│  ┌──────────────────────────────────┐   │
│  │  📎 Kéo & thả hoặc [Chọn file]  │   │
│  │  JPG, PNG, PDF — tối đa 10MB     │   │
│  └──────────────────────────────────┘   │
│                                         │
│  Bằng chứng đầu tư trước đây           │
│  (sao kê, term sheet, cap table…)       │  (INVESTMENT_PROOF, không bắt buộc)
│  ┌──────────────────────────────────┐   │
│  │  📎 Kéo & thả hoặc [Chọn file]  │   │
│  └──────────────────────────────────┘   │
│                                         │
│  [← Bước 2]  [Lưu nháp]  [Gửi duyệt]  │
└─────────────────────────────────────────┘
```

**Tổ chức / Quỹ:**

```
┌─────────────────────────────────────────┐
│        ① ──────── ② ────────[③]         │
│                                         │
│  Giấy tờ xác thực                       │
│                                         │
│  Giấy phép hoạt động quỹ / ĐKKD *      │  (INVESTMENT_PROOF)
│  ┌──────────────────────────────────┐   │
│  │  📎 Kéo & thả hoặc [Chọn file]  │   │
│  │  PDF — tối đa 10MB               │   │
│  └──────────────────────────────────┘   │
│                                         │
│  CCCD / Hộ chiếu người đại diện *      │  (ID_PROOF)
│  ┌──────────────────────────────────┐   │
│  │  📎 Kéo & thả hoặc [Chọn file]  │   │
│  └──────────────────────────────────┘   │
│                                         │
│  [← Bước 2]  [Lưu nháp]  [Gửi duyệt]  │
└─────────────────────────────────────────┘
```

**Nút "Lưu nháp":** Cho phép partial, không cần file. Trạng thái giữ `Draft`.  
**Nút "Gửi duyệt":** File bắt buộc (lần đầu). Trạng thái chuyển sang `PendingKYC`.

---

### Màn hình xác nhận sau khi gửi

```
┌─────────────────────────────────────────┐
│                                         │
│               ⏳                        │
│                                         │
│   Hồ sơ đã gửi thành công!             │
│                                         │
│   Chúng tôi sẽ xét duyệt trong         │
│   1–3 ngày làm việc.                   │
│   Bạn sẽ nhận thông báo khi có kết quả. │
│                                         │
│           [Về trang hồ sơ]             │
│                                         │
└─────────────────────────────────────────┘
```

---

### Khi bị yêu cầu bổ sung (`PendingMoreInfo`)

Banner hiện trên đầu trang KYC:

```
┌─────────────────────────────────────────┐
│  ⚠️  Staff yêu cầu bổ sung thông tin   │
│                                         │
│  Ghi chú từ staff:                      │
│  "{remarks}"                            │
│                                         │
│  Vui lòng cập nhật và upload lại        │
│  giấy tờ (file mới bắt buộc).           │
└─────────────────────────────────────────┘
```

Khi `requiresNewEvidence = true`: section upload **không thể bỏ qua**.

---

## 6. Màn hình: Chỉnh sửa profile

Truy cập từ nút **[Chỉnh sửa]** trên trang profile. Chỉ chỉnh sửa thông tin cơ bản — **không** thay đổi được thông tin KYC tại đây.

```
┌─────────────────────────────────────────┐
│  Chỉnh sửa hồ sơ                        │
│                                         │
│  [Thay ảnh đại diện]                    │
│                                         │
│  Họ và tên        [Nguyen Van A    ]     │
│  Chức danh        [Managing Partner]    │
│                                         │
│  ────── Chỉ hiện khi INSTITUTIONAL ──── │
│  Tên quỹ          [ABC Capital     ]    │
│  ──────────────────────────────────     │
│                                         │
│  Giới thiệu       [               ]     │
│  Luận điểm đầu tư [               ]     │
│  Địa điểm         [_______________]     │
│  Quốc gia         [_______________]     │
│  LinkedIn         [_______________]     │
│  Website          [_______________]     │
│                                         │
│  [Huỷ]                [Lưu thay đổi]   │
└─────────────────────────────────────────┘
```

**Không cho chỉnh sửa ở đây** — phải qua KYC resubmit:
`investorType`, `contactEmail`, `organizationName`, `submitterRole`, `businessCode`

---

## 7. Màn hình: Investment Preferences

Truy cập từ tab "Đầu tư" → nút **[Cập nhật]**.

```
┌─────────────────────────────────────────┐
│  Tiêu chí đầu tư                        │
│                                         │
│  Quy mô ticket (USD)                    │
│  Tối thiểu  [$_________]               │
│  Tối đa     [$_________]               │
│                                         │
│  Giai đoạn quan tâm                     │
│  [x] Idea      [x] Pre-Seed             │
│  [x] Seed      [ ] Series A             │
│  [ ] Series B  [ ] Growth               │
│                                         │
│  Lĩnh vực quan tâm                      │
│  [Fintech ×] [HealthTech ×] [+ Thêm]   │
│                                         │
│  Thị trường / địa lý                    │
│  [___________________________________]  │
│  e.g. "Vietnam, Singapore"              │
│                                         │
│  [Huỷ]                [Lưu thay đổi]   │
└─────────────────────────────────────────┘
```

"Lĩnh vực quan tâm" là tag input — add/remove từng item riêng (mỗi item 1 API call riêng).

---

## 8. Luồng tổng thể (Flow Diagram)

### 8.1 Luồng Onboarding lần đầu

```
Đăng nhập với role Investor
          │
          ▼
   Kiểm tra /me
  ┌────────┴────────┐
  │ data: null      │ data: có
  ▼                 ▼
Màn hình tạo    Kiểm tra
profile         profileStatus
  │              (xem 8.3)
  ▼
POST /api/investors
  │
  ▼
Trang profile
(profileStatus = Draft)
  │
  ▼
Banner "Hoàn thiện ngay"
  │
  ▼
Luồng KYC (xem 8.2)
```

---

### 8.2 Luồng KYC

```
/investors/kyc
      │
[Bước 1] Chọn loại: INDIVIDUAL_ANGEL | INSTITUTIONAL
      │
[Bước 2] Điền thông tin (form thích nghi theo loại)
      │
[Bước 3] Upload giấy tờ (loại file khác nhau theo loại)
      │
      ├── [Lưu nháp]  → profileStatus giữ nguyên Draft
      │
      └── [Gửi duyệt]
               │
               ▼
       profileStatus = PendingKYC
               │
          (Staff review
           1–3 ngày)
               │
     ┌─────────┴──────────┐
     ▼                    ▼
  Approved            Rejected / PendingMoreInfo
     │                    │
  Full access          Banner lý do từ chối
  Badge hiện           + nút vào lại form KYC
```

---

### 8.3 Logic render theo `profileStatus`

```
profileStatus
  │
  ├── "Draft"
  │     → Hiện: banner onboarding + thông tin cơ bản
  │     → Ẩn:   badge, KYC fields (contactEmail, org…)
  │     → Cho phép: vào luồng KYC
  │
  ├── "PendingKYC"
  │     → Hiện: banner "Đang xét duyệt"
  │     → Ẩn:   nút edit KYC, badge chính thức
  │     → Lock: không cho resubmit KYC (đang under review)
  │
  ├── "Approved"
  │     → Hiện: badge xác thực, đầy đủ KYC fields, watchlist
  │     → Ẩn:   tất cả banner trạng thái
  │
  └── "Rejected"
        → Hiện: banner lý do + nút "Cập nhật & gửi lại"
        → Cho phép: vào lại luồng KYC, chỉnh sửa & upload lại
```

---

### 8.4 Logic badge xác thực

```
investorType + profileStatus
  │
  ├── INDIVIDUAL_ANGEL + Approved  →  "✓ Verified Angel Investor"   (xanh lá)
  ├── INSTITUTIONAL   + Approved   →  "✓ Verified Investor Entity"  (xanh lá)
  ├── (bất kỳ)        + PendingKYC →  "⏳ Đang xét duyệt"           (vàng)
  ├── (bất kỳ)        + Rejected   →  "✗ Xác thực thất bại"          (đỏ)
  └── (bất kỳ)        + Draft      →  (không hiện badge)
```
