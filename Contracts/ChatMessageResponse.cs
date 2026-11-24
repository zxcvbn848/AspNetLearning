namespace AspNetLearning.Contracts;

/// <summary>聊天室對外傳送的訊息；不包含會員 Email。</summary>
public sealed record ChatMessageResponse(
    long Id, int UserId, string? Nickname, string Content, DateTime CreatedAtUtc, ChatReplyPreview? ReplyTo);

/// <summary>只回傳原訊息的作者與文字，避免遞迴展開引用。</summary>
public sealed record ChatReplyPreview(long Id, int UserId, string? Nickname, string Content);
