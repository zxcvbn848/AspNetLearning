namespace AspNetLearning.Contracts;

/// <summary>
/// 表示登入成功後回傳的 JWT 與到期時間。
/// </summary>
public sealed class LoginResponse
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public required UserResponse User { get; init; }
}
