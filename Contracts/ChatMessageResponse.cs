namespace AspNetLearning.Contracts;

/// <summary>聊天室對外傳送的訊息；不包含會員 Email。</summary>
public sealed record ChatMessageResponse(long Id, int UserId, string? Nickname, string Content, DateTime CreatedAtUtc);
