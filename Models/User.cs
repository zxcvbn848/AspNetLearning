namespace AspNetLearning.Models;

/// <summary>
/// 代表 PostgreSQL 中的一位會員。
/// </summary>
public sealed class User
{
    /// <summary>會員在資料庫中的流水號。</summary>
    public int Id { get; set; }

    /// <summary>已正規化為小寫並移除前後空白的 Email。</summary>
    public required string Email { get; set; }

    /// <summary>公開顯示的暱稱；舊會員可稍後設定。</summary>
    public string? Nickname { get; set; }

    /// <summary>密碼雜湊；資料庫永遠不保存明文密碼。</summary>
    public required string PasswordHash { get; set; }

    /// <summary>會員建立時間，統一使用 UTC 保存。</summary>
    public DateTime CreatedAtUtc { get; set; }
}
