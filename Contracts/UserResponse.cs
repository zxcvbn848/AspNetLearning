using AspNetLearning.Models;

namespace AspNetLearning.Contracts;

/// <summary>
/// 表示可以安全回傳給 Client 的會員資料，不包含 PasswordHash。
/// </summary>
public sealed class UserResponse
{
    public int Id { get; init; }
    public required string Email { get; init; }
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>將資料庫 User 轉成不含密碼雜湊的 API Response。</summary>
    public static UserResponse FromUser(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        CreatedAtUtc = user.CreatedAtUtc
    };
}
