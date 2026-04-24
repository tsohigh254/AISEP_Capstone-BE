# Hướng dẫn bypass demo — Payout flow

Dùng khi cần demo staff release tiền cho advisor mà không muốn chờ job tự động (30–60 phút).

---

## Điều kiện để `IsPayoutEligible = true`

`IsPayoutEligible` được tính lại tự động mỗi khi có action liên quan. Công thức:

| # | Điều kiện | Field |
|---|---|---|
| 1 | Tất cả active sessions có `session_status = 'Completed'` | `mentorship_sessions.session_status` |
| 2 | Tất cả active sessions có `startup_confirmed_conducted_at != NULL` | `mentorship_sessions.startup_confirmed_conducted_at` |
| 3 | Tất cả reports (non-superseded) có `report_review_status = 1` (Passed) | `mentorship_reports.report_review_status` |
| 4 | Tất cả passed reports có `startup_acknowledged_at != NULL` | `mentorship_reports.startup_acknowledged_at` |
| 5 | Không có session nào có `session_status = 'InDispute'` | `mentorship_sessions.session_status` |

> "Active session" = session có status KHÔNG thuộc `Cancelled`, `ProposedByStartup`, `ProposedByAdvisor`

---

## Enum reference

### `mentorship_sessions.session_status` (string)
| Value | Ý nghĩa |
|---|---|
| `ProposedByStartup` | Slot startup đề xuất, chưa được chọn |
| `ProposedByAdvisor` | Slot advisor đề xuất, chưa được chọn |
| `Scheduled` | Lịch đã chốt, chưa diễn ra |
| `InProgress` | Đang diễn ra |
| `Conducted` | Đã diễn ra, chờ startup confirm |
| `Completed` | Startup đã confirm hoàn thành |
| `InDispute` | Đang tranh chấp |
| `Resolved` | Tranh chấp đã giải quyết |
| `Cancelled` | Đã huỷ |

### `startup_advisor_mentorships.mentorship_status` (integer)
| Value | Ý nghĩa |
|---|---|
| `0` | Requested |
| `1` | Rejected |
| `2` | Accepted |
| `3` | InProgress |
| `4` | **Completed** |
| `5` | InDispute |
| `6` | Resolved |
| `7` | Cancelled |

### `mentorship_reports.report_review_status` (integer)
| Value | Ý nghĩa |
|---|---|
| `0` | PendingReview |
| `1` | **Passed** |
| `2` | Failed |
| `3` | NeedsMoreInfo |
| `4` | Draft |

### `startup_advisor_mentorships.payment_status` (integer)
| Value | Ý nghĩa |
|---|---|
| `0` | Pending |
| `1` | **Completed** (startup đã thanh toán — FE dùng để hiện meeting URL) |
| `2` | Failed |

---

## Script bypass — chạy theo thứ tự

Thay `{MENTORSHIP_ID}` bằng ID thực trước khi chạy.

### Bước 0 — Kiểm tra trạng thái hiện tại

```sql
-- Xem sessions
SELECT session_id, session_status, startup_confirmed_conducted_at, scheduled_start_at
FROM mentorship_sessions
WHERE mentorship_id = {MENTORSHIP_ID}
ORDER BY session_id;

-- Xem reports
SELECT report_id, report_review_status, startup_acknowledged_at, superseded_by_report_id, submitted_at
FROM mentorship_reports
WHERE mentorship_id = {MENTORSHIP_ID};

-- Xem mentorship
SELECT mentorship_id, mentorship_status, is_payout_eligible, payment_status, payout_released_at
FROM startup_advisor_mentorships
WHERE mentorship_id = {MENTORSHIP_ID};
```

---

### Bước 1 — Set sessions thành Completed

Set tất cả active sessions (không phải slot đề xuất/cancelled) thành Completed:

```sql
UPDATE mentorship_sessions
SET session_status = 'Completed',
    startup_confirmed_conducted_at = NOW(),
    updated_at = NOW()
WHERE mentorship_id = {MENTORSHIP_ID}
  AND session_status NOT IN ('Cancelled', 'ProposedByStartup', 'ProposedByAdvisor');
```

> Nếu có slot dư (2 `ProposedByStartup` nhưng chỉ 1 được accept), slot còn lại phải là `Cancelled`. Nếu chưa, chạy thêm:
> ```sql
> UPDATE mentorship_sessions
> SET session_status = 'Cancelled', updated_at = NOW()
> WHERE mentorship_id = {MENTORSHIP_ID}
>   AND session_status IN ('ProposedByStartup', 'ProposedByAdvisor');
> ```

---

### Bước 2 — Xử lý report

**Nếu chưa có report nào** (bước 0 trả về 0 row):

```sql
INSERT INTO mentorship_reports (
    mentorship_id,
    session_id,
    created_by_advisor_id,
    report_summary,
    report_review_status,
    is_mandatory,
    submitted_at,
    reviewed_at,
    startup_acknowledged_at,
    created_at
)
SELECT
    {MENTORSHIP_ID},
    (SELECT session_id FROM mentorship_sessions
     WHERE mentorship_id = {MENTORSHIP_ID}
       AND session_status = 'Completed'
     LIMIT 1),
    (SELECT advisor_id FROM startup_advisor_mentorships
     WHERE mentorship_id = {MENTORSHIP_ID}),
    'Demo report',
    1,
    true,
    NOW(), NOW(), NOW(), NOW();
```

**Nếu đã có report** (bước 0 trả về ≥ 1 row):

```sql
UPDATE mentorship_reports
SET report_review_status = 1,
    startup_acknowledged_at = NOW(),
    reviewed_at = NOW(),
    updated_at = NOW()
WHERE mentorship_id = {MENTORSHIP_ID}
  AND superseded_by_report_id IS NULL;
```

---

### Bước 3 — Set mentorship Completed + payout eligible

```sql
UPDATE startup_advisor_mentorships
SET mentorship_status = 4,
    is_payout_eligible = true,
    payment_status = 1,
    updated_at = NOW()
WHERE mentorship_id = {MENTORSHIP_ID};
```

---

### Bước 4 — Verify lại trước khi demo

```sql
SELECT
    m.mentorship_id,
    m.mentorship_status,
    m.is_payout_eligible,
    m.payment_status,
    m.payout_released_at,
    COUNT(CASE WHEN s.session_status = 'Completed' THEN 1 END) AS completed_sessions,
    COUNT(CASE WHEN s.startup_confirmed_conducted_at IS NOT NULL THEN 1 END) AS confirmed_sessions,
    COUNT(CASE WHEN r.report_review_status = 1 AND r.superseded_by_report_id IS NULL THEN 1 END) AS passed_reports,
    COUNT(CASE WHEN r.startup_acknowledged_at IS NOT NULL AND r.superseded_by_report_id IS NULL THEN 1 END) AS acked_reports
FROM startup_advisor_mentorships m
LEFT JOIN mentorship_sessions s ON s.mentorship_id = m.mentorship_id
    AND s.session_status NOT IN ('Cancelled', 'ProposedByStartup', 'ProposedByAdvisor')
LEFT JOIN mentorship_reports r ON r.mentorship_id = m.mentorship_id
WHERE m.mentorship_id = {MENTORSHIP_ID}
GROUP BY m.mentorship_id, m.mentorship_status, m.is_payout_eligible, m.payment_status, m.payout_released_at;
```

Kết quả mong đợi:
- `is_payout_eligible = true`
- `payment_status = 1`
- `completed_sessions = confirmed_sessions`
- `passed_reports = acked_reports` và > 0

---

### Bước 5 — Demo release payout

Sau khi verify xong, staff gọi API:

```
POST /api/mentorships/{MENTORSHIP_ID}/payout/release
Authorization: Bearer {staff_token}
```

Không cần restart server, không cần chờ job.

---

## Shortcut — Bypass từ trạng thái "chờ họp" (session Scheduled, chưa thanh toán)

Dùng khi session đang `Scheduled` (lịch đã chốt nhưng chưa diễn ra), startup chưa thanh toán. Cần set đủ tất cả điều kiện kể cả `payment_status`:

```sql
-- 1. Hoàn thành session + startup confirmed
UPDATE mentorship_sessions
SET session_status = 'Completed',
    startup_confirmed_conducted_at = NOW(),
    updated_at = NOW()
WHERE mentorship_id = {MENTORSHIP_ID}
  AND session_status NOT IN ('Cancelled', 'ProposedByStartup', 'ProposedByAdvisor');

-- 2a. Nếu chưa có report: insert report mẫu
INSERT INTO mentorship_reports (
    mentorship_id, session_id, created_by_advisor_id,
    report_summary, report_review_status, is_mandatory,
    submitted_at, reviewed_at, startup_acknowledged_at, created_at
)
SELECT
    {MENTORSHIP_ID},
    (SELECT session_id FROM mentorship_sessions
     WHERE mentorship_id = {MENTORSHIP_ID} AND session_status = 'Completed' LIMIT 1),
    (SELECT advisor_id FROM startup_advisor_mentorships WHERE mentorship_id = {MENTORSHIP_ID}),
    'Demo report', 1, true, NOW(), NOW(), NOW(), NOW();

-- 2b. Nếu đã có report: set Passed + acknowledged
UPDATE mentorship_reports
SET report_review_status = 1,
    startup_acknowledged_at = NOW(),
    reviewed_at = NOW(),
    updated_at = NOW()
WHERE mentorship_id = {MENTORSHIP_ID}
  AND superseded_by_report_id IS NULL;

-- 3. Set mentorship Completed + payout eligible + payment Completed
UPDATE startup_advisor_mentorships
SET mentorship_status = 4,
    is_payout_eligible = true,
    payment_status = 1,
    updated_at = NOW()
WHERE mentorship_id = {MENTORSHIP_ID};
```

Sau đó gọi ngay: `POST /api/mentorships/{MENTORSHIP_ID}/payout/release`

---

## Shortcut — Bypass từ trạng thái "đang họp / đã thanh toán"

Dùng khi mentorship đã ở `InProgress`, session đang `Scheduled`/`InProgress`, startup đã thanh toán (`payment_status = 1`). Chỉ cần 3 query:

```sql
-- 1. Hoàn thành session
UPDATE mentorship_sessions
SET session_status = 'Completed',
    startup_confirmed_conducted_at = NOW(),
    updated_at = NOW()
WHERE mentorship_id = {MENTORSHIP_ID}
  AND session_status NOT IN ('Cancelled', 'ProposedByStartup', 'ProposedByAdvisor');

-- 2a. Nếu chưa có report: insert report mẫu
INSERT INTO mentorship_reports (
    mentorship_id, session_id, created_by_advisor_id,
    report_summary, report_review_status, is_mandatory,
    submitted_at, reviewed_at, startup_acknowledged_at, created_at
)
SELECT
    {MENTORSHIP_ID},
    (SELECT session_id FROM mentorship_sessions
     WHERE mentorship_id = {MENTORSHIP_ID} AND session_status = 'Completed' LIMIT 1),
    (SELECT advisor_id FROM startup_advisor_mentorships WHERE mentorship_id = {MENTORSHIP_ID}),
    'Demo report', 1, true, NOW(), NOW(), NOW(), NOW();

-- 2b. Nếu đã có report: set Passed + acknowledged
UPDATE mentorship_reports
SET report_review_status = 1,
    startup_acknowledged_at = NOW(),
    reviewed_at = NOW(),
    updated_at = NOW()
WHERE mentorship_id = {MENTORSHIP_ID}
  AND superseded_by_report_id IS NULL;

-- 3. Set mentorship Completed + payout eligible (giữ nguyên payment_status vì đã = 1)
UPDATE startup_advisor_mentorships
SET mentorship_status = 4,
    is_payout_eligible = true,
    updated_at = NOW()
WHERE mentorship_id = {MENTORSHIP_ID};
```

Sau đó gọi ngay: `POST /api/mentorships/{MENTORSHIP_ID}/payout/release`

---

## Lưu ý

- `payout_released_at` có idempotency guard — nếu đã release rồi thì API trả lỗi `PAYOUT_ALREADY_RELEASED`. Muốn reset để demo lại thì set `payout_released_at = NULL` và trừ lại balance trong `advisor_wallets`.
- Advisor phải có wallet (`advisor_wallets` table) thì API mới chạy được. Nếu chưa, advisor cần tạo wallet trước qua app.
