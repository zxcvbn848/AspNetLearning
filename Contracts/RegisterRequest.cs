using System.ComponentModel.DataAnnotations;

namespace AspNetLearning.Contracts;

/// <summary>
/// 表示註冊會員時，Client 必須傳入的 JSON 資料。
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>會員登入使用的 Email。</summary>
    [Required(ErrorMessage = "Email 為必填欄位。")]
    [EmailAddress(ErrorMessage = "Email 格式不正確。")]
    [MaxLength(254, ErrorMessage = "Email 最多只能有 254 個字元。")]
    public required string Email { get; init; }

    /// <summary>會員的原始密碼，只用來產生雜湊，不會寫入資料庫。</summary>
    [Required(ErrorMessage = "密碼為必填欄位。")]
    [MinLength(8, ErrorMessage = "密碼至少需要 8 個字元。")]
    [MaxLength(100, ErrorMessage = "密碼最多只能有 100 個字元。")]
    public required string Password { get; init; }
}
