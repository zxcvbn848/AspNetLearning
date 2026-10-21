namespace AspNetLearning.Models;

/// <summary>資料庫只保存 Refresh Token 的雜湊；同一個 FamilyId 代表一次登入工作階段。</summary>
public sealed class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string TokenHash { get; set; }
    public Guid FamilyId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public User User { get; set; } = null!;
}
