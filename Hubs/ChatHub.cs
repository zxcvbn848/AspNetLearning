using AspNetLearning.Data;
using AspNetLearning.Contracts;
using AspNetLearning.Extensions;
using AspNetLearning.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AspNetLearning.Hubs;

/// <summary>登入會員共用的公開聊天室。</summary>
[Authorize]
public sealed class ChatHub(AppDbContext dbContext) : Hub
{
    private const string Lobby = "lobby";

    /// <summary>連線後加入公開大廳；斷線時 SignalR 會自動移除群組成員。</summary>
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, Lobby);
        await base.OnConnectedAsync();
    }

    /// <summary>讀取最近 50 則訊息，供初次進入與重連後補齊畫面。</summary>
    public Task<List<ChatMessageResponse>> GetRecentMessages() => GetMessagesAsync(null);

    /// <summary>以目前最舊訊息的 Id 為游標，讀取更早的最多 50 則訊息。</summary>
    public Task<List<ChatMessageResponse>> GetOlderMessages(long beforeId)
    {
        if (beforeId <= 0) throw new HubException("訊息游標必須大於 0。");
        return GetMessagesAsync(beforeId);
    }

    private async Task<List<ChatMessageResponse>> GetMessagesAsync(long? beforeId)
    {
        // null 代表最新一頁；否則只查 Id 更小的訊息，避免新訊息使分頁位移。
        List<ChatMessageResponse> recent = await (from message in dbContext.ChatMessages.AsNoTracking()
            where beforeId == null || message.Id < beforeId
            join user in dbContext.Users.AsNoTracking() on message.UserId equals user.Id
            orderby message.Id descending
            select new ChatMessageResponse(message.Id, message.UserId, user.Nickname, message.Content, message.CreatedAtUtc))
            .Take(50)
            .ToListAsync();
        recent.Reverse();
        return recent;
    }

    /// <summary>驗證、儲存並廣播一則聊天訊息。</summary>
    public async Task SendMessage(string content)
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

        var message = new ChatMessage
        {
            UserId = userId,
            Content = text,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync();
        string? nickname = await dbContext.Users.Where(user => user.Id == userId)
            .Select(user => user.Nickname).SingleAsync();
        await Clients.Group(Lobby).SendAsync("MessageReceived",
            new ChatMessageResponse(message.Id, userId, nickname, text, message.CreatedAtUtc));
    }
}
