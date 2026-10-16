// 匯入建立待辦事項的 Request 與 TodoItem 所在的命名空間。
using AspNetLearning.Contracts;
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

        // POST /todos 會讀取 Request Body 中的 JSON，並建立新的待辦事項。
        app.MapPost("/todos", (CreateTodoRequest request) =>
        {
            // IsNullOrWhiteSpace 會檢查 null、空字串及只含空白的文字。
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                // HTTP 400 Bad Request 表示 Client 傳入的資料不符合要求。
                return Results.BadRequest(new { message = "Title 不可為空白。" });
            }

            // 如果清單沒有資料，第一個 Id 使用 1；否則使用目前最大 Id 加 1。
            int nextId = TodoItems.Count == 0
                ? 1
                : TodoItems.Max(todoItem => todoItem.Id) + 1;

            // 根據 Request Body 建立新的 TodoItem 物件。
            TodoItem todoItem = new()
            {
                Id = nextId,
                Title = request.Title.Trim(),
                IsCompleted = false
            };

            // 將新物件加入記憶體中的 List。
            // shortcut: List 不支援多使用者同時寫入，接上資料庫時由資料庫產生 Id。
            TodoItems.Add(todoItem);

            // HTTP 201 Created 表示成功建立資料，並提供新資源的位置與內容。
            return Results.Created($"/todos/{todoItem.Id}", todoItem);
        });
    }
}
