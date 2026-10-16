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
    public required string Title { get; init; }
}
