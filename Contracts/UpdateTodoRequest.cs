namespace AspNetLearning.Contracts;

/// <summary>
/// 表示更新待辦事項時，Client 必須傳入的 JSON 資料。
/// </summary>
public sealed class UpdateTodoRequest
{
    /// <summary>更新後的文字內容。</summary>
    public required string Title { get; init; }

    /// <summary>更新後的完成狀態。</summary>
    public bool IsCompleted { get; init; }
}
