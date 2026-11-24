namespace AspNetLearning.Models;

/// <summary>公開聊天室的一則持久化訊息。</summary>
public sealed class ChatMessage
{
    /// <summary>訊息編號。</summary>
    public long Id { get; init; }

    /// <summary>發送者的會員編號，不公開 Email。</summary>
    public int UserId { get; init; }

    public User Author { get; init; } = null!;

    /// <summary>有回覆對象時，指向同一張資料表的原訊息。</summary>
    public long? ReplyToMessageId { get; init; }

    public ChatMessage? ReplyToMessage { get; init; }

    /// <summary>訊息文字，最多 500 個字元。</summary>
    public required string Content { get; init; }

    /// <summary>伺服器記錄的 UTC 發送時間。</summary>
    public DateTime CreatedAtUtc { get; init; }
}
