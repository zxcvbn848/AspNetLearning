// 匯入 ASP.NET Core Model Validation 使用的驗證 Attribute。
using System.ComponentModel.DataAnnotations;

// 宣告此檔案所屬的命名空間，集中管理 API 接收與回傳的資料格式。
namespace AspNetLearning.Contracts;

/// <summary>
/// 表示建立待辦事項時，Client 必須傳入的 JSON 資料。
/// </summary>
public sealed class CreateTodoRequest
{
    /// <summary>
    /// 新待辦事項的文字內容。
    /// </summary>
    // Required 拒絕 null、空字串與只有空白的字串。
    [Required(ErrorMessage = "Title 不可為空白。")]
    // MaxLength 限制最多 100 個字元，與資料庫欄位長度保持一致。
    [MaxLength(100, ErrorMessage = "Title 最多只能有 100 個字元。")]
    // 預設空字串讓缺少 JSON 欄位也交由 Required 產生一致的 Title 錯誤。
    public string Title { get; init; } = string.Empty;
}
