using AspNetLearning.Data;
using AspNetLearning.Contracts;
using AspNetLearning.Extensions;
using AspNetLearning.Models;
using AspNetLearning.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AspNetLearning.Hubs;

/// <summary>登入會員共用的公開聊天室。</summary>
[Authorize]
public sealed class ChatHub(AppDbContext dbContext, ChatSendLimiter sendLimiter) : Hub
{
    private const string Lobby = "lobby";

    /// <summary>連線後加入公開大廳；斷線時 SignalR 會自動移除群組成員。</summary>
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, Lobby);
        await base.OnConnectedAsync();
    }

    /// <summary>讀取最近 50 則訊息，供初次進入與確認補收終點。</summary>
    public Task<List<ChatMessageResponse>> GetRecentMessages() => GetMessagesAsync(null);

    /// <summary>以目前最舊訊息的 Id 為游標，讀取更早的最多 50 則訊息。</summary>
    public Task<List<ChatMessageResponse>> GetOlderMessages(long beforeId)
    {
        if (beforeId <= 0) throw new HubException("訊息游標必須大於 0。");
        return GetMessagesAsync(beforeId);
    }

    /// <summary>依 Id 由小到大補收，單次最多 50 則。</summary>
    public Task<List<ChatMessageResponse>> GetNewerMessages(long afterId, long throughId)
    {
        if (afterId < 0 || throughId <= afterId) throw new HubException("訊息游標範圍不正確。");

        return VisibleMessages()
            .Where(message => message.Id > afterId && message.Id <= throughId)
            .OrderBy(message => message.Id)
            .Take(50)
            .Select(ToResponse)
            .ToListAsync();
    }

    private async Task<List<ChatMessageResponse>> GetMessagesAsync(long? beforeId)
    {
        // null 代表最新一頁；否則只查 Id 更小的訊息，避免新訊息使分頁位移。
        List<ChatMessageResponse> recent = await VisibleMessages()
            .Where(message => beforeId == null || message.Id < beforeId)
            .OrderByDescending(message => message.Id)
            .Take(50)
            .Select(ToResponse)
            .ToListAsync();
        recent.Reverse();
        return recent;
    }

    // 主訊息清單排除刪除紀錄，引用仍能從同一筆紀錄辨識刪除狀態。
    private IQueryable<ChatMessage> VisibleMessages() => dbContext.ChatMessages.AsNoTracking()
        .Where(message => !message.IsDeleted);

    // 查歷史、重連補收與即時廣播共用這份格式，引用不必另外向前端查詢。
    private static readonly Expression<Func<ChatMessage, ChatMessageResponse>> ToResponse =
        message => new ChatMessageResponse(
            message.Id, message.UserId, message.Author.Nickname, message.Content, message.CreatedAtUtc,
            message.ReplyToMessage == null ? null : new ChatReplyPreview(
                message.ReplyToMessage.Id,
                message.ReplyToMessage.IsDeleted ? null : message.ReplyToMessage.UserId,
                message.ReplyToMessage.IsDeleted ? null : message.ReplyToMessage.Author.Nickname,
                message.ReplyToMessage.IsDeleted ? null : message.ReplyToMessage.Content));

    /// <summary>傳送一般聊天訊息。</summary>
    public Task SendMessage(string content) => SaveAndBroadcastAsync(content, null);

    /// <summary>回覆聊天室內已存在的訊息。</summary>
    public Task SendReply(string content, long replyToMessageId) => SaveAndBroadcastAsync(content, replyToMessageId);

    /// <summary>刪除自己的訊息，再通知仍在線的聊天室。</summary>
    public async Task DeleteMessage(long messageId)
    {
        if (Context.User is null || !Context.User.TryGetUserId(out int userId))
        {
            throw new HubException("請先登入再刪除訊息。");
        }

        // 同一條 UPDATE 檢查擁有權、隱藏訊息並清空原文，保留引用所需的 Id。
        int deleted = await dbContext.ChatMessages
            .Where(message => message.Id == messageId && message.UserId == userId && !message.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.IsDeleted, true)
                .SetProperty(message => message.Content, "")
                .SetProperty(message => message.ReplyToMessageId, (long?)null),
                Context.ConnectionAborted);
        if (deleted == 0)
        {
            throw new HubException("找不到可刪除的訊息。");
        }

        await Clients.Group(Lobby).SendAsync("MessageDeleted", messageId);
    }

    /// <summary>重連時確認已載入的訊息或引用原文是否還存在。</summary>
    public async Task<long[]> GetMissingMessageIds(long[]? messageIds)
    {
        if (messageIds is null || messageIds.Length is < 1 or > 50 || messageIds.Any(id => id <= 0))
        {
            throw new HubException("請提供 1 到 50 個有效的訊息 Id。");
        }

        // 只查這一批的 Id，再用差集找出畫面需要移除的訊息。
        List<long> existingIds = await VisibleMessages()
            .Where(message => messageIds.Contains(message.Id))
            .Select(message => message.Id)
            .ToListAsync(Context.ConnectionAborted);
        return messageIds.Except(existingIds).ToArray();
    }

    private async Task SaveAndBroadcastAsync(string content, long? replyToMessageId)
    {
        string text = content?.Trim() ?? "";
        if (text.Length is < 1 or > 500)
        {
            throw new HubException("訊息必須介於 1 到 500 個字元。");
        }

        if (Context.User is null || !Context.User.TryGetUserId(out int userId))
        {
            throw new HubException("請先登入再傳送訊息。");
        }

        if (replyToMessageId is long targetId &&
            (targetId <= 0 || !await VisibleMessages().AnyAsync(message => message.Id == targetId)))
        {
            throw new HubException("找不到要回覆的訊息。");
        }

        if (!sendLimiter.TryAcquire(userId))
        {
            throw new HubException("CHAT_RATE_LIMIT");
        }

        var message = new ChatMessage
        {
            UserId = userId,
            ReplyToMessageId = replyToMessageId,
            Content = text,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync();
        ChatMessageResponse? response = await VisibleMessages()
            .Where(saved => saved.Id == message.Id).Select(ToResponse).SingleOrDefaultAsync();
        if (response is not null)
        {
            await Clients.Group(Lobby).SendAsync("MessageReceived", response);
        }
    }
}
