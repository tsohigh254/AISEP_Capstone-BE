# Real-time Chat — Kế hoạch triển khai chi tiết

> Stack FE: Next.js 16 / React 19 / TypeScript / Tailwind CSS 4 / Axios + JWT Bearer token
> Stack BE: **.NET (ASP.NET Core)**
> Phương thức real-time: **ASP.NET Core SignalR**

---

## 1. Tổng quan kiến trúc

```
Client (Next.js)
  ├── REST API (Axios)          ← lấy danh sách conversations, lịch sử messages
  └── SignalR HubConnection     ← gửi / nhận message real-time
           │
           ▼
      ASP.NET Core Backend
        ├── /api/conversations         REST
        ├── /api/conversations/{id}/messages  REST
        └── /hubs/chat                 SignalR Hub
              ├── invoke "SendMessage"  ← client gửi
              └── on    "ReceiveMessage" ← client nhận
```

---

## 2. Cài đặt dependencies

```bash
npm install @microsoft/signalr
```

Không cần SockJS hay bất kỳ thư viện phụ nào — `@microsoft/signalr` đã tự xử lý fallback (WebSocket → Server-Sent Events → Long Polling).

---

## 3. Kiểu dữ liệu — thêm vào `types/global.ts`

Paste vào bên trong block `declare global { ... }`:

```ts
// ── Messaging ──

interface IConversation {
  conversationId: number;
  participantId: number;          // userId của người còn lại
  participantName: string;
  participantRole: "Investor" | "Startup" | "Advisor" | "Admin";
  participantAvatarUrl?: string;
  lastMessage: string;
  lastMessageAt: string;          // ISO 8601
  unreadCount: number;
  isOnline?: boolean;
}

interface IMessage {
  messageId: number;
  conversationId: number;
  senderId: number;
  content: string;
  attachmentUrl?: string;
  createdAt: string;              // ISO 8601
  readAt?: string | null;
}

interface ISendMessage {
  conversationId: number;
  content: string;
  attachmentUrl?: string;
}

// Payload nhận về qua SignalR "ReceiveMessage"
interface IIncomingMessage {
  messageId: number;
  conversationId: number;
  senderId: number;
  content: string;
  attachmentUrl?: string;
  createdAt: string;
}
```

---

## 4. REST API service — tạo `services/messaging/messaging.api.ts`

```ts
import axios from "../interceptor";

/** Danh sách conversations của user đang đăng nhập */
export const GetConversations = () =>
  axios.get<IBackendRes<IConversation[]>>("/api/conversations");

/** Lịch sử tin nhắn (phân trang, mới nhất trước) */
export const GetMessages = (
  conversationId: number,
  page = 1,
  pageSize = 30
) =>
  axios.get<IBackendRes<IPaginatedRes<IMessage>>>(
    `/api/conversations/${conversationId}/messages`,
    { params: { page, pageSize } }
  );

/** Tạo conversation mới với một user khác */
export const CreateConversation = (participantId: number) =>
  axios.post<IBackendRes<IConversation>>("/api/conversations", {
    participantId,
  });

/** Đánh dấu tất cả tin nhắn trong conversation là đã đọc */
export const MarkConversationRead = (conversationId: number) =>
  axios.put<IBackendRes<null>>(
    `/api/conversations/${conversationId}/read`
  );
```

---

## 5. SignalR hook — tạo `hooks/useChat.ts`

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

    // Khởi tạo HubConnection
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_BACKEND_URL}/hubs/chat`, {
        accessTokenFactory: () => token,
        // Ưu tiên WebSocket, fallback tự động nếu server không hỗ trợ
        transport: signalR.HttpTransportType.WebSockets,
        skipNegotiation: true, // bỏ nếu server không dùng raw WS
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000]) // retry intervals (ms)
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Lắng nghe tin nhắn đến
    connection.on("ReceiveMessage", (msg: IIncomingMessage) => {
      onMessage(msg);
    });

    // Kết nối và join room
    connection
      .start()
      .then(() => {
        if (conversationId != null) {
          // Báo server biết client đang ở conversation nào
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

  /** Gửi tin nhắn qua SignalR */
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
        } satisfies ISendMessage)
        .catch(console.error);
    },
    [conversationId]
  );

  return { sendMessage };
}
```

> **Lưu ý `skipNegotiation: true`:**
> Chỉ dùng khi BE cấu hình `MapHub` với raw WebSocket (không qua negotiate endpoint).
> Nếu BE dùng negotiate mặc định (phổ biến hơn), **xóa dòng đó** và xóa cả dòng `transport`.

---

## 6. Cập nhật `components/messaging/messaging-content.tsx`

### 6.1 Import mới thêm vào

```ts
import { useEffect, useRef, useState } from "react";
import { useChat } from "@/hooks/useChat";
import {
  GetConversations,
  GetMessages,
  MarkConversationRead,
} from "@/services/messaging/messaging.api";
```

### 6.2 Xác định userId của "tôi"

```ts
const [myUserId, setMyUserId] = useState<number | null>(null);

useEffect(() => {
  const token = localStorage.getItem("accessToken");
  if (!token) return;
  try {
    // Decode JWT payload (phần giữa dấu chấm)
    const payload = JSON.parse(atob(token.split(".")[1]));
    // .NET identity thường dùng claim "nameid" hoặc "sub"
    const id = payload["nameid"] ?? payload["sub"] ?? payload["userId"];
    setMyUserId(Number(id));
  } catch {
    // ignore
  }
}, []);
```

### 6.3 State và fetch data

Xóa `const conversations: Conversation[]` và `const sampleMessages: Message[]`.
Thay bằng:

```ts
const [conversations, setConversations] = useState<IConversation[]>([]);
const [messages, setMessages] = useState<IMessage[]>([]);
const [loadingConvs, setLoadingConvs] = useState(true);
const [loadingMsgs, setLoadingMsgs] = useState(false);
const bottomRef = useRef<HTMLDivElement>(null);

// Fetch conversations khi mount
useEffect(() => {
  GetConversations().then((res) => {
    if (res.success && res.data) setConversations(res.data);
    setLoadingConvs(false);
  });
}, []);

// Fetch lịch sử messages khi chọn conversation
useEffect(() => {
  if (selectedId == null) return;
  setLoadingMsgs(true);
  GetMessages(selectedId).then((res) => {
    if (res.success && res.data) {
      // API trả mới nhất trước → reverse để hiển thị đúng thứ tự
      setMessages([...res.data.items].reverse());
    }
    setLoadingMsgs(false);
  });
  MarkConversationRead(selectedId);
}, [selectedId]);

// Auto-scroll xuống cuối khi có tin nhắn mới
useEffect(() => {
  bottomRef.current?.scrollIntoView({ behavior: "smooth" });
}, [messages]);
```

### 6.4 Tích hợp SignalR hook

```ts
const { sendMessage } = useChat({
  conversationId: selectedId,
  onMessage: (incoming) => {
    // Chỉ append nếu đang mở đúng conversation
    if (incoming.conversationId !== selectedId) {
      // Cập nhật badge unread cho conversation khác
      setConversations((prev) =>
        prev.map((c) =>
          c.conversationId === incoming.conversationId
            ? { ...c, unreadCount: c.unreadCount + 1, lastMessage: incoming.content }
            : c
        )
      );
      return;
    }
    setMessages((prev) => [
      ...prev,
      {
        messageId: incoming.messageId,
        conversationId: incoming.conversationId,
        senderId: incoming.senderId,
        content: incoming.content,
        attachmentUrl: incoming.attachmentUrl,
        createdAt: incoming.createdAt,
        readAt: null,
      },
    ]);
  },
});
```

### 6.5 Gửi tin nhắn (optimistic update)

```ts
const handleSend = () => {
  const text = messageInput.trim();
  if (!text || selectedId == null || myUserId == null) return;

  // Hiển thị ngay lập tức, không cần chờ server echo
  setMessages((prev) => [
    ...prev,
    {
      messageId: Date.now(),   // id tạm thời
      conversationId: selectedId,
      senderId: myUserId,
      content: text,
      createdAt: new Date().toISOString(),
      readAt: null,
    },
  ]);
  setMessageInput("");

  // Gửi qua SignalR
  sendMessage(text);
};
```

Bind vào JSX:

```tsx
// Input
onKeyDown={(e) => { if (e.key === "Enter") handleSend(); }}

// Nút Send
<button onClick={handleSend} ...>
  <Send className="w-5 h-5" />
</button>
```

### 6.6 Thêm ref scroll và skeleton loading

Thêm `<div ref={bottomRef} />` ở cuối danh sách messages.

```tsx
{/* Messages list */}
<div className="flex-1 overflow-y-auto p-6 space-y-4">
  {loadingMsgs ? (
    <div className="text-center text-slate-400 text-sm">Đang tải...</div>
  ) : (
    messages.map((msg) => (
      <div key={msg.messageId} className={`flex ${msg.senderId === myUserId ? "justify-end" : "justify-start"}`}>
        {/* ... bubble ... */}
      </div>
    ))
  )}
  <div ref={bottomRef} />
</div>
```

---

## 7. Biến môi trường — `.env.local`

```env
NEXT_PUBLIC_BACKEND_URL=http://localhost:5000
```

SignalR sẽ kết nối tới `http://localhost:5000/hubs/chat`.

---

## 8. Yêu cầu phía Backend (.NET)

### 8.1 REST Endpoints

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/api/conversations` | Conversations của user đang login |
| POST | `/api/conversations` | Tạo mới `{ participantId: number }` |
| GET | `/api/conversations/{id}/messages` | Lịch sử, query: `page`, `pageSize` |
| PUT | `/api/conversations/{id}/read` | Đánh dấu đã đọc |

### 8.2 SignalR Hub

```csharp
// ChatHub.cs
public class ChatHub : Hub
{
    public async Task JoinConversation(int conversationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");

    public async Task LeaveConversation(int conversationId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv_{conversationId}");

    public async Task SendMessage(SendMessageDto dto)
    {
        // 1. Lưu message vào DB
        var saved = await _messageService.SaveAsync(dto, Context.UserIdentifier);

        // 2. Broadcast cho tất cả client trong group
        await Clients.Group($"conv_{dto.ConversationId}")
            .SendAsync("ReceiveMessage", saved);
    }
}
```

```csharp
// Program.cs
builder.Services.AddSignalR();
// ...
app.MapHub<ChatHub>("/hubs/chat");
```

**Auth Hub:** Dùng JWT Bearer authentication tiêu chuẩn — SignalR .NET tự đọc token từ query string `?access_token=` hoặc header `Authorization: Bearer`.

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });
```

### 8.3 CORS cho SignalR

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFE", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // bắt buộc cho SignalR
});
```

---

## 9. Thứ tự thực hiện (checklist)

- [ ] **1.** `npm install @microsoft/signalr`
- [ ] **2.** Thêm types vào `types/global.ts` (Mục 3)
- [ ] **3.** Tạo `services/messaging/messaging.api.ts` (Mục 4)
- [ ] **4.** Tạo `hooks/useChat.ts` (Mục 5)
- [ ] **5.** Refactor `messaging-content.tsx` — xóa dummy data, thêm API fetch (Mục 6)
- [ ] **6.** Tích hợp `useChat` hook, optimistic send, auto-scroll (Mục 6.4–6.6)
- [ ] **7.** Xác nhận với BE team: Hub endpoint `/hubs/chat`, method names `SendMessage` / `ReceiveMessage` / `JoinConversation`
- [ ] **8.** Xác nhận CORS `AllowCredentials()` đã bật ở BE
- [ ] **9.** Test với 2 tài khoản trên 2 tab trình duyệt
- [ ] **10.** Kiểm tra `skipNegotiation` — nếu kết nối lỗi 404, xóa dòng đó + dòng `transport`
