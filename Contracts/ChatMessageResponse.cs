namespace AspNetLearning.Contracts;

/// <summary>聊天室對外傳送的訊息；不包含會員 Email。</summary>
public sealed record ChatMessageResponse(
    long Id, int UserId, string? Nickname, string Content, DateTime CreatedAtUtc, ChatReplyPreview? ReplyTo);

/// <summary>引用保留原訊息 Id，原訊息已刪除時作者和文字均為 null。</summary>
public sealed record ChatReplyPreview(long Id, int? UserId, string? Nickname, string? Content);
