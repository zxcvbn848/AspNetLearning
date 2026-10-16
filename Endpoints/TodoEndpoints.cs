// 匯入 TodoItem 所在的命名空間。
using AspNetLearning.Models;

// 宣告此檔案所屬的命名空間。
namespace AspNetLearning.Endpoints;

/// <summary>
/// 集中管理第二階段的待辦事項 API 路由。
/// </summary>
public static class TodoEndpoints
{
    // readonly 表示 TodoItems 變數不能改指向其他 List，但 List 內的資料仍可變動。
    // shortcut: 目前使用記憶體資料，學到 Entity Framework Core 時改成資料庫。
    private static readonly List<TodoItem> TodoItems = new()
    {
        // new TodoItem 建立一個物件；大括號內為物件的屬性設定值。
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
    /// 將待辦事項 API 註冊到 ASP.NET Core 應用程式。
    /// </summary>
    /// <param name="app">目前正在設定的 ASP.NET Core 應用程式。</param>
    public static void MapTodoEndpoints(this WebApplication app)
    {
        // GET /todos 會回傳 TodoItems 中的所有待辦事項。
        app.MapGet("/todos", () => Results.Ok(TodoItems));
    }
}
