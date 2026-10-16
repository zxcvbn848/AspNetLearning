// 匯入待辦事項的資料模型。
using AspNetLearning.Models;

// 宣告此檔案所屬的命名空間，用來集中管理應用程式邏輯。
namespace AspNetLearning.Services;

/// <summary>
/// 管理待辦事項資料，並提供查詢、新增、更新與刪除操作。
/// </summary>
public sealed class TodoService
{
    // readonly 表示 _todoItems 不可改指向其他 List，但 List 內的資料仍可變動。
    // shortcut: List 不支援多使用者同時寫入，接上資料庫時改由 AppDbContext 保存資料。
    private readonly List<TodoItem> _todoItems = new()
    {
        // new TodoItem 建立一筆物件；大括號內設定物件的初始屬性值。
        new TodoItem
        {
            Id = 1,
            Title = "閱讀第一階段文件",
            IsCompleted = true
        },
        new TodoItem
        {
            Id = 2,
            Title = "認識 Class 與 List",
            IsCompleted = false
        }
    };

    /// <summary>
    /// 取得目前所有待辦事項。
    /// </summary>
    /// <returns>唯讀介面的待辦事項清單。</returns>
    public IReadOnlyList<TodoItem> GetAll()
    {
        // List 可以當成 IReadOnlyList 回傳，呼叫端只能透過介面讀取內容。
        return _todoItems;
    }

    /// <summary>
    /// 建立並保存一筆待辦事項。
    /// </summary>
    /// <param name="title">待辦事項的文字內容。</param>
    /// <returns>建立完成的待辦事項。</returns>
    public TodoItem Create(string title)
    {
        // 如果清單沒有資料，第一個 Id 使用 1；否則使用目前最大 Id 加 1。
        int nextId = _todoItems.Count == 0
            ? 1
            : _todoItems.Max(todoItem => todoItem.Id) + 1;

        TodoItem todoItem = new()
        {
            Id = nextId,
            Title = title.Trim(),
            IsCompleted = false
        };

        // Add 將新物件加入記憶體中的 List。
        _todoItems.Add(todoItem);

        return todoItem;
    }

    /// <summary>
    /// 更新指定待辦事項。
    /// </summary>
    /// <param name="id">要更新的待辦事項 Id。</param>
    /// <param name="title">更新後的文字內容。</param>
    /// <param name="isCompleted">更新後的完成狀態。</param>
    /// <returns>更新後的待辦事項；找不到時回傳 null。</returns>
    public TodoItem? Update(int id, string title, bool isCompleted)
    {
        // FirstOrDefault 尋找第一筆 Id 相符的資料；找不到時回傳 null。
        TodoItem? todoItem = _todoItems.FirstOrDefault(item => item.Id == id);

        if (todoItem is null)
        {
            return null;
        }

        // Trim 會移除文字前後的空白。
        todoItem.Title = title.Trim();
        todoItem.IsCompleted = isCompleted;

        return todoItem;
    }

    /// <summary>
    /// 刪除指定待辦事項。
    /// </summary>
    /// <param name="id">要刪除的待辦事項 Id。</param>
    /// <returns>刪除成功回傳 true；找不到資料回傳 false。</returns>
    public bool Delete(int id)
    {
        TodoItem? todoItem = _todoItems.FirstOrDefault(item => item.Id == id);

        if (todoItem is null)
        {
            return false;
        }

        // Remove 會從 List 中移除找到的物件。
        _todoItems.Remove(todoItem);

        return true;
    }
}
