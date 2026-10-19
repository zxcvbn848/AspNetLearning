using System.ComponentModel.DataAnnotations;

namespace AspNetLearning.Contracts;

/// <summary>
/// 表示會員登入時，Client 必須傳入的 JSON 資料。
/// </summary>
public sealed class LoginRequest
{
    [Required(ErrorMessage = "Email 為必填欄位。")]
    [EmailAddress(ErrorMessage = "Email 格式不正確。")]
    [MaxLength(254, ErrorMessage = "Email 最多只能有 254 個字元。")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "密碼為必填欄位。")]
    [MaxLength(100, ErrorMessage = "密碼最多只能有 100 個字元。")]
    public required string Password { get; init; }
}
