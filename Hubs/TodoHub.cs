using AspNetLearning.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AspNetLearning.Hubs;

/// <summary>已登入會員的待辦事項即時通知連線。</summary>
[Authorize]
public sealed class TodoHub : Hub
{
    /// <summary>同一會員的分頁加入同一群組，其他會員收不到通知。</summary>
    public override async Task OnConnectedAsync()
    {
        if (!Context.User!.TryGetUserId(out int userId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
        await base.OnConnectedAsync();
    }

    /// <summary>產生會員專屬的 SignalR 群組名稱。</summary>
    public static string GroupName(int userId) => $"todos:{userId}";
}
