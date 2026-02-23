using AISEP.Application.DTOs.Chat;
using AISEP.Application.DTOs.Common;

namespace AISEP.Application.Interfaces;

public interface IChatService
{
    // ─── Conversations ─────────────────────────────────────────
    Task<ApiResponse<PagedResponse<ConversationListItemDto>>> GetMyConversationsAsync(
        int userId, string? status, int page, int pageSize);

    Task<ApiResponse<ConversationDto>> CreateConversationAsync(
        int userId, CreateConversationRequest request);

    Task<ApiResponse<ConversationDetailDto>> GetConversationAsync(
        int userId, int conversationId);

    Task<ApiResponse<ConversationDto>> CloseConversationAsync(
        int userId, int conversationId);

    // ─── Messages ──────────────────────────────────────────────
    Task<ApiResponse<PagedResponse<MessageDto>>> GetMessagesAsync(
        int userId, int conversationId, int page, int pageSize);

    Task<ApiResponse<MessageDto>> SendMessageAsync(
        int userId, SendMessageRequest request);

    Task<ApiResponse<string>> MarkReadAsync(
        int userId, int messageId);

    Task<ApiResponse<string>> MarkReadAllAsync(
        int userId, MarkReadAllRequest request);
}
