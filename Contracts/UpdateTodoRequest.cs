// 匯入 ASP.NET Core Model Validation 使用的驗證 Attribute。
using System.ComponentModel.DataAnnotations;

namespace AspNetLearning.Contracts;

/// <summary>
/// 表示更新待辦事項時，Client 必須傳入的 JSON 資料。
/// </summary>
public sealed class UpdateTodoRequest
{
    /// <summary>更新後的文字內容。</summary>
    // 建立與更新使用相同的 Title 規則，Controller 不需要重複檢查。
    [Required(ErrorMessage = "Title 不可為空白。")]
    [MaxLength(100, ErrorMessage = "Title 最多只能有 100 個字元。")]
    // Request 不使用 C# required，避免反序列化錯誤早於 Model Validation。
    public string Title { get; init; } = string.Empty;

    /// <summary>更新後的完成狀態。</summary>
    public bool IsCompleted { get; init; }
}
