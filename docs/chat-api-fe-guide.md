# Chat API — Hướng dẫn tích hợp Frontend

> Backend: ASP.NET Core 8 + SignalR
> Base URL: `http://localhost:5000` (dev) — thay bằng domain thật khi deploy
> Tất cả REST response đều bọc trong envelope chung (xem mục 1)

---

## 1. Response Envelope chung

Mọi REST endpoint đều trả về shape sau:

```jsonc
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": { ... }      // null nếu lỗi
}
```

**TypeScript interface:**

```ts
interface IBackendRes<T> {
  isSuccess: boolean;
  statusCode: number;
  message: string;
  data: T | null;
}
```

### Paged response

Các endpoint có phân trang, `data` có shape:

```jsonc
{
  "items": [ ... ],
  "paging": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 45,
    "totalPages": 3
  }
}
```

```ts
interface IPaged<T> {
  items: T[];
  paging: {
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
  };
}
```

---

## 2. TypeScript Interfaces (aligned với BE DTOs)

```ts
// Một conversation trong danh sách
interface IConversation {
  conversationId: number;
  connectionId:   number | null;  // null nếu là mentorship
  mentorshipId:   number | null;  // null nếu là connection
  status:         "Open" | "Closed";
  title:          string;         // tên người kia (tự build ở BE)

  // Thông tin đối phương — dùng để hiển thị avatar, tên, role
  participantId:       number;
  participantName:     string;
  participantRole:     "Startup" | "Investor" | "Advisor";
  participantAvatarUrl: string | null;

  lastMessagePreview: string | null;  // tối đa 80 ký tự
  unreadCount:        number;
  createdAt:          string;         // ISO 8601
  lastMessageAt:      string | null;  // ISO 8601
}

// Chi tiết conversation (kèm danh sách participants)
interface IConversationDetail {
  conversationId: number;
  connectionId:   number | null;
  mentorshipId:   number | null;
  status:         "Open" | "Closed";
  title:          string;
  participants:   IParticipant[];
  createdAt:      string;
  lastMessageAt:  string | null;
}

interface IParticipant {
  userId:      number;
  displayName: string;
  userType:    "Startup" | "Investor" | "Advisor";
}

// Một tin nhắn (lấy từ REST — GET messages)
interface IMessage {
  messageId:          number;
  conversationId:     number;
  senderUserId:       number;
  senderDisplayName:  string;
  isMine:             boolean;    // BE tự tính dựa vào userId của caller
  content:            string;
  attachmentUrls:     string | null;
  isRead:             boolean;
  sentAt:             string;     // ISO 8601
  readAt:             string | null;
}

// Payload nhận qua SignalR event "ReceiveMessage"
interface IIncomingMessage {
  messageId:      number;
  conversationId: number;
  senderId:       number;
  content:        string;
  attachmentUrl:  string | null;
  createdAt:      string;         // ISO 8601
}

// Body gửi khi tạo conversation
interface ICreateConversationBody {
  connectionId?:  number;   // dùng khi chat trong Connection
  mentorshipId?:  number;   // dùng khi chat trong Mentorship
  // Bắt buộc có một trong hai
}

// Body gửi tin nhắn qua REST (nếu không dùng SignalR)
interface ISendMessageBody {
  conversationId: number;
  content:        string;
  attachmentUrl?: string;
}
```

---

## 3. REST Endpoints

### 3.1 Lấy danh sách conversations

```
GET /api/conversations
Authorization: Bearer <token>

Query params (tuỳ chọn):
  ?status=Open        — lọc theo trạng thái ("Open" | "Closed")
  ?page=1
  ?pageSize=20
```

**Response `data`:** `IPaged<IConversation>`

```jsonc
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": {
    "items": [
      {
        "conversationId": 1,
        "connectionId": 5,
        "mentorshipId": null,
        "status": "Open",
        "title": "Nguyen Van A",
        "participantId": 12,
        "participantName": "Nguyen Van A",
        "participantRole": "Investor",
        "participantAvatarUrl": null,
        "lastMessagePreview": "Chào bạn, tôi muốn...",
        "unreadCount": 3,
        "createdAt": "2026-03-15T08:00:00Z",
        "lastMessageAt": "2026-03-19T10:30:00Z"
      }
    ],
    "paging": { "page": 1, "pageSize": 20, "totalItems": 1, "totalPages": 1 }
  }
}
```

**Axios service:**
```ts
export const GetConversations = (status?: string, page = 1, pageSize = 20) =>
  axios.get<IBackendRes<IPaged<IConversation>>>("/api/conversations", {
    params: { status, page, pageSize }
  });
```

---

### 3.2 Tạo conversation mới

```
POST /api/conversations
Authorization: Bearer <token>
Content-Type: application/json

Body: { "connectionId": 5 }
  hoặc
Body: { "mentorshipId": 3 }
```

**Điều kiện:**
- `connectionId` → Connection phải có `status = "Accepted"`
- `mentorshipId` → Mentorship phải có `status = "Accepted"` hoặc `"InProgress"`
- Đã tồn tại conversation `Open` cho cùng connection/mentorship → trả 409

**Response `data`:** `IConversation` (chỉ có các field cơ bản, không có participant info)

```jsonc
{
  "isSuccess": true,
  "statusCode": 201,
  "message": "Conversation created successfully.",
  "data": {
    "conversationId": 7,
    "connectionId": 5,
    "mentorshipId": null,
    "status": "Open",
    "createdAt": "2026-03-19T10:00:00Z",
    "lastMessageAt": null
  }
}
```

**Axios service:**
```ts
export const CreateConversation = (body: ICreateConversationBody) =>
  axios.post<IBackendRes<IConversation>>("/api/conversations", body);
```

---

### 3.3 Lấy chi tiết conversation

```
GET /api/conversations/:id
Authorization: Bearer <token>
```

**Response `data`:** `IConversationDetail`

```jsonc
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Success",
  "data": {
    "conversationId": 7,
    "connectionId": 5,
    "mentorshipId": null,
    "status": "Open",
    "title": "Nguyen Van A",
    "participants": [
      { "userId": 3,  "displayName": "TechStart Co.",  "userType": "Startup"  },
      { "userId": 12, "displayName": "Nguyen Van A", "userType": "Investor" }
    ],
    "createdAt": "2026-03-19T10:00:00Z",
    "lastMessageAt": "2026-03-19T10:30:00Z"
  }
}
```

```ts
export const GetConversation = (id: number) =>
  axios.get<IBackendRes<IConversationDetail>>(`/api/conversations/${id}`);
```

---

### 3.4 Lấy lịch sử tin nhắn

```
GET /api/conversations/:id/messages
Authorization: Bearer <token>

Query params:
  ?page=1
  ?pageSize=50       — mặc định 50, tối đa 100
```

> ⚠️ BE trả **mới nhất trước** (descending `sentAt`).
> FE cần reverse mảng `items` trước khi render.

**Response `data`:** `IPaged<IMessage>`

```jsonc
{
  "isSuccess": true,
  "statusCode": 200,
  "data": {
    "items": [
      {
        "messageId": 42,
        "conversationId": 7,
        "senderUserId": 12,
        "senderDisplayName": "Nguyen Van A",
        "isMine": false,
        "content": "Xin chào!",
        "attachmentUrls": null,
        "isRead": true,
        "sentAt": "2026-03-19T10:30:00Z",
        "readAt": "2026-03-19T10:31:00Z"
      }
    ],
    "paging": { "page": 1, "pageSize": 50, "totalItems": 1, "totalPages": 1 }
  }
}
```

```ts
export const GetMessages = (conversationId: number, page = 1, pageSize = 50) =>
  axios.get<IBackendRes<IPaged<IMessage>>>(
    `/api/conversations/${conversationId}/messages`,
    { params: { page, pageSize } }
  );
```

---

### 3.5 Đánh dấu đã đọc toàn bộ conversation

```
PUT /api/conversations/:id/read
Authorization: Bearer <token>
```

**Gọi khi:** user mở conversation (sau khi fetch messages).

```jsonc
{ "isSuccess": true, "statusCode": 200, "message": "3 message(s) marked as read.", "data": null }
```

```ts
export const MarkConversationRead = (conversationId: number) =>
  axios.put<IBackendRes<null>>(`/api/conversations/${conversationId}/read`);
```

---

### 3.6 Đóng conversation

```
POST /api/conversations/:id/close
Authorization: Bearer <token>
```

```ts
export const CloseConversation = (id: number) =>
  axios.post<IBackendRes<IConversation>>(`/api/conversations/${id}/close`);
```

---

### 3.7 Gửi tin nhắn qua REST (fallback)

> Dùng khi không có SignalR hoặc cần gửi từ server-side context.
> **Khi dùng SignalR thì FE không cần gọi endpoint này** — Hub đã tự lưu DB.

```
POST /api/messages
Authorization: Bearer <token>
Content-Type: application/json

Body: { "conversationId": 7, "content": "Hello", "attachmentUrl": null }
```

```ts
export const SendMessage = (body: ISendMessageBody) =>
  axios.post<IBackendRes<IMessage>>("/api/messages", body);
```

---

## 4. SignalR Hub

**Endpoint:** `ws://localhost:5000/hubs/chat`
**Auth:** token gửi qua query string `?access_token=<JWT>` (WebSocket không hỗ trợ header)

### 4.1 Kết nối

```ts
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${process.env.NEXT_PUBLIC_BACKEND_URL}/hubs/chat`, {
    accessTokenFactory: () => localStorage.getItem("accessToken") ?? "",
    // KHÔNG dùng skipNegotiation — để SignalR tự negotiate
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000])
  .configureLogging(signalR.LogLevel.Warning)
  .build();

await connection.start();
```

---

### 4.2 Methods FE invoke lên server

| Method | Tham số | Mô tả |
|--------|---------|-------|
| `JoinConversation` | `conversationId: number` | Đăng ký nhận tin nhắn của conversation |
| `LeaveConversation` | `conversationId: number` | Huỷ đăng ký |
| `SendMessage` | `{ conversationId, content, attachmentUrl }` | Gửi tin — BE tự lưu DB + broadcast |

```ts
// Join khi mở conversation
await connection.invoke("JoinConversation", conversationId);

// Leave khi đóng / unmount
await connection.invoke("LeaveConversation", conversationId);

// Gửi tin nhắn
await connection.invoke("SendMessage", {
  conversationId: 7,
  content: "Xin chào!",
  attachmentUrl: null,
} satisfies ISendMessageBody);
```

---

### 4.3 Events FE lắng nghe từ server

#### `ReceiveMessage` — có tin nhắn mới

Payload: `IIncomingMessage`

```ts
connection.on("ReceiveMessage", (msg: IIncomingMessage) => {
  if (msg.conversationId === currentConversationId) {
    // Thêm vào cuối danh sách tin nhắn
    setMessages(prev => [...prev, {
      messageId:         msg.messageId,
      conversationId:    msg.conversationId,
      senderUserId:      msg.senderId,
      senderDisplayName: "",          // không có trong payload này
      isMine:            msg.senderId === myUserId,
      content:           msg.content,
      attachmentUrls:    msg.attachmentUrl ?? null,
      isRead:            false,
      sentAt:            msg.createdAt,
      readAt:            null,
    }]);
  } else {
    // Cập nhật badge unread cho conversation khác
    setConversations(prev =>
      prev.map(c => c.conversationId === msg.conversationId
        ? { ...c, unreadCount: c.unreadCount + 1, lastMessagePreview: msg.content }
        : c
      )
    );
  }
});
```

#### `Error` — hub trả lỗi

```ts
connection.on("Error", (message: string) => {
  console.error("[SignalR Hub Error]", message);
  // Ví dụ: "Cannot send messages in a closed conversation."
});
```

---

## 5. File `messaging.api.ts` hoàn chỉnh

```ts
import axios from "@/services/interceptor";

// ── Conversations ──────────────────────────────────────────

export const GetConversations = (status?: string, page = 1, pageSize = 20) =>
  axios.get<IBackendRes<IPaged<IConversation>>>("/api/conversations", {
    params: { status, page, pageSize },
  });

export const CreateConversation = (body: ICreateConversationBody) =>
  axios.post<IBackendRes<IConversation>>("/api/conversations", body);

export const GetConversation = (id: number) =>
  axios.get<IBackendRes<IConversationDetail>>(`/api/conversations/${id}`);

export const GetMessages = (conversationId: number, page = 1, pageSize = 50) =>
  axios.get<IBackendRes<IPaged<IMessage>>>(
    `/api/conversations/${conversationId}/messages`,
    { params: { page, pageSize } }
  );

export const MarkConversationRead = (conversationId: number) =>
  axios.put<IBackendRes<null>>(`/api/conversations/${conversationId}/read`);

export const CloseConversation = (id: number) =>
  axios.post<IBackendRes<IConversation>>(`/api/conversations/${id}/close`);
```

---

## 6. Hook `useChat.ts` hoàn chỉnh

```ts
"use client";

import { useEffect, useRef, useCallback } from "react";
import * as signalR from "@microsoft/signalr";

interface UseChatOptions {
  conversationId: number | null;
  onMessage: (msg: IIncomingMessage) => void;
}

export function useChat({ conversationId, onMessage }: UseChatOptions) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("accessToken");
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_BACKEND_URL}/hubs/chat`, {
        accessTokenFactory: () => token,
        // Không dùng skipNegotiation + transport override
        // để SignalR tự negotiate (WebSocket → SSE → Long Polling)
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on("ReceiveMessage", onMessage);

    connection.on("Error", (msg: string) => {
      console.error("[ChatHub]", msg);
    });

    connection
      .start()
      .then(() => {
        if (conversationId != null) {
          connection.invoke("JoinConversation", conversationId).catch(console.error);
        }
      })
      .catch(console.error);

    connectionRef.current = connection;

    return () => {
      if (conversationId != null) {
        connection
          .invoke("LeaveConversation", conversationId)
          .catch(() => {})
          .finally(() => connection.stop());
      } else {
        connection.stop();
      }
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [conversationId]);

  /** Gửi tin nhắn qua SignalR. BE tự lưu DB + broadcast. */
  const sendMessage = useCallback(
    (content: string, attachmentUrl?: string) => {
      const conn = connectionRef.current;
      if (!conn || conn.state !== signalR.HubConnectionState.Connected) return;
      if (conversationId == null) return;

      conn
        .invoke("SendMessage", {
          conversationId,
          content,
          attachmentUrl: attachmentUrl ?? null,
        } satisfies ISendMessageBody)
        .catch(console.error);
    },
    [conversationId]
  );

  return { sendMessage };
}
```

---

## 7. Luồng dữ liệu tổng thể

```
Mở trang chat
  └─► GET /api/conversations              ← danh sách sidebar

Chọn một conversation
  ├─► GET /api/conversations/:id/messages ← lịch sử (reverse items trước khi render)
  ├─► PUT /api/conversations/:id/read     ← reset badge unread
  └─► SignalR: JoinConversation(:id)      ← subscribe real-time

User gõ và nhấn Send
  └─► SignalR: invoke "SendMessage"
        ├─► BE lưu vào DB
        └─► BE broadcast "ReceiveMessage" tới mọi client trong group
              ├─► Tab của người gửi:   append vào messages[]
              └─► Tab của người nhận:  append vào messages[]

Rời conversation (unmount / đổi tab)
  └─► SignalR: LeaveConversation(:id)
```

---

## 8. Error codes thường gặp

| Code | HTTP | Ý nghĩa |
|------|------|---------|
| `CONVERSATION_NOT_FOUND` | 404 | conversationId không tồn tại |
| `ACCESS_DENIED` | 403 | User không phải participant của conversation này |
| `CONVERSATION_ALREADY_EXISTS` | 409 | Đã có conversation Open cho connection/mentorship này |
| `INVALID_STATUS_TRANSITION` | 409 | Conversation đã Closed, không gửi được tin |
| `CONNECTION_NOT_FOUND` | 404 | connectionId không tồn tại khi tạo conversation |
| `MENTORSHIP_NOT_FOUND` | 404 | mentorshipId không tồn tại khi tạo conversation |

---

## 9. Biến môi trường FE

```env
# .env.local
NEXT_PUBLIC_BACKEND_URL=http://localhost:5000
```
