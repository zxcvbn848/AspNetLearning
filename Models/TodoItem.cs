// 宣告此檔案所屬的命名空間，集中管理專案的資料模型。
namespace AspNetLearning.Models;

/// <summary>
/// 代表一筆待辦事項資料。
/// </summary>
public sealed class TodoItem
{
    /// <summary>
    /// 待辦事項的唯一編號。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 待辦事項的文字內容。
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// 待辦事項是否已完成。
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 擁有這筆待辦事項的會員 Id；舊資料保留 null，不會顯示給任何會員。
    /// </summary>
    public int? UserId { get; set; }
}
