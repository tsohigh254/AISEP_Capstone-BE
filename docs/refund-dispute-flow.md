# Refund and Dispute Flow

## Mục tiêu
Giải thích rõ cho cả FE và BE cách xử lý khi một session tư vấn bị staff mở tranh chấp, và khi nào startup được refund tiền.

## Tổng quan
- Khi startup báo cáo vấn đề và staff mở tranh chấp, **chưa refund ngay**.
- Mở tranh chấp chỉ là bước xác nhận có sự cố, cần phải kiểm tra, đối chiếu, và staff đưa ra quyết định cuối cùng.
- Nếu staff quyết định refund, tiền sẽ về ví startup.
- Startup rút tiền ví này giống như cách advisor rút tiền từ ví của họ.
- Nếu quá 24 tiếng startup không báo cáo gì, thì coi như session đã hoàn thành và **không refund**.

## Các bên liên quan
- **Startup**: người dùng trả tiền cho mentorship/session.
- **Advisor**: người nhận tiền khi session hoàn thành và không có tranh chấp.
- **Staff**: người mở tranh chấp, kiểm tra và quyết định có refund hay không.
- **Hệ thống**: quản lý trạng thái session, mentorship, payment, wallet.

## Luồng xử lý

1. **Session hoàn thành bình thường**
   - Advisor nộp report.
   - System auto approve report hoặc staff duyệt report.
   - Startup xác nhận session.
   - Nếu không có tranh chấp xảy ra, advisor sẽ được release payout sau 24h.

2. **Startup báo cáo và staff mở tranh chấp**
   - Staff bấm `Mark Dispute` trên session.
   - Session chuyển trạng thái `InDispute`.
   - Đây chỉ là bước ghi nhận có tranh chấp, không có refund tiền tại đây.
   - Mentor / report / session vẫn giữ tiền ở trạng thái tạm.

3. **Dispute được điều tra và xử lý**
   - Staff kiểm tra chi tiết, đối chiếu dữ liệu.
   - Có hai kết quả chính:
     - `Giải quyết nghiêng về advisor`: session trở lại `Completed` hoặc `Resolved`, advisor vẫn được hưởng tiền nếu phù hợp.
     - `Giải quyết nghiêng về startup`: hệ thống thực hiện refund vào ví startup.

4. **Refund tiền cho startup**
   - Refund phải trả đúng số tiền mà advisor đã nhận được nếu session hoàn thành.
   - Tiền refund không phải là tiền giả, mà là số tiền thực tế được tính cho session đó.
   - Số tiền này sẽ vào ví của startup.
   - Startup sẽ tự nhập số tài khoản ngân hàng và rút ra như với tiền trong ví advisor.

5. **Không refund nếu quá 24h không báo cáo**
   - Nếu startup không tạo báo cáo hoặc không mở tranh chấp trong 24h, session coi như đã hoàn thành.
   - Khi đó, tiền không được refund và sẽ mặc định là của advisor.
   - Rule này tồn tại để tránh tranh chấp muộn và bảo vệ advisor.

## Câu hỏi quan trọng

### Có cần hành động riêng để xác nhận refund?
- **Có.**
- Mở tranh chấp là bước ghi nhận vấn đề.
- Sau khi xử lý, staff cần có hành động rõ ràng để kết luận:
  - `Resolve dispute` và giữ tiền cho advisor, hoặc
  - `Resolve dispute` và `Refund startup`.
- Tức là FE/BE nên thiết kế một bước quyết định cuối cùng, không nên tự động refund ngay khi mở dispute.

## Định nghĩa trạng thái

- `Scheduled`, `InProgress`, `Conducted`, `Completed`: trạng thái bình thường của session.
- `InDispute`: session đang bị staff xem xét tranh chấp.
- `Resolved`: tranh chấp đã giải quyết, có thể không hoàn thành.

## Ý nghĩa cho FE

- Khi hiển thị session tranh chấp:
  - Hiện rõ trạng thái `Tranh chấp`.
  - Không cho phép hiển thị refund ngay.
  - Hiển thị nút hoặc form staff để chuyển sang `Giải quyết`.
- Sau khi staff quyết định refund:
  - Hiển thị số tiền refund vào ví startup.
  - Startup sẽ thấy số dư ví và nút rút tiền giống như bình thường.
- Nếu không refund:
  - Hiển thị trạng thái `Resolved` hoặc `Completed` tùy quyết định.

## Ý nghĩa cho BE

- Khi staff mở dispute:
  - Gọi endpoint staff mark dispute.
  - Chỉ đổi trạng thái session và giữ lại số tiền tạm.
- Khi staff resolve dispute:
  - Có logic để xác định outcome.
  - Nếu outcome là refund, tạo entry credit vào ví startup.
  - Nếu outcome không refund, vẫn cập nhật trạng thái và có thể tiếp tục payout advisor.
- Phải lưu lại thông tin hành động staff: `DisputeReason`, `ResolutionNote`, `MarkedByStaffID`, `MarkedAt`.

## Yêu cầu rõ ràng

- Staff mở tranh chấp không refund ngay.
- Refund chỉ được thực hiện sau bước `resolve dispute`.
- Số tiền refund là số tiền advisor đáng ra được nhận.
- Refund vào ví startup.
- Startup rút tiền từ ví như bình thường.
- Rule 24h không báo cáo = không refund.

## Ghi chú

- Đây là luồng nghiệp vụ chung, cả FE và BE cần rõ:
  - `Mark dispute` = mở tranh chấp.
  - `Resolve dispute` = bước quyết định cuối cùng.
  - `Refund startup` = hành động riêng, không đi kèm ngay với `Mark dispute`.

