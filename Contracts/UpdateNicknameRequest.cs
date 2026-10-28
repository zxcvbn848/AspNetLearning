using System.ComponentModel.DataAnnotations;

namespace AspNetLearning.Contracts;

/// <summary>會員修改公開暱稱時提交的資料。</summary>
public sealed class UpdateNicknameRequest
{
    [Required(ErrorMessage = "暱稱為必填欄位。")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "暱稱長度須介於 2 到 30 個字元。")]
    public string Nickname { get; init; } = string.Empty;
}
