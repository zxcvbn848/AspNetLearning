// 匯入 Todo API 接收的 Request 類別。
using AspNetLearning.Contracts;

// 匯入待辦事項的資料模型。
using AspNetLearning.Models;

// 匯入 ControllerBase、HTTP 路由 Attribute 與 ActionResult 等 ASP.NET Core MVC 類別。
using Microsoft.AspNetCore.Mvc;

// 宣告此檔案所屬的命名空間，用來集中管理 Controller。
namespace AspNetLearning.Controllers;

/// <summary>
/// 接收待辦事項的 HTTP 請求，並決定要回傳的 HTTP 狀態碼與資料。
/// </summary>
// ApiController 會啟用 Web API Controller 的行為，例如從 Request Body 綁定複雜型別。
[ApiController]
// Route 指定這個 Controller 內所有 Action 共用的網址前綴。
[Route("todos")]
public sealed class TodosController : ControllerBase
{
    // readonly 表示 TodoItems 不能改指向其他 List，但 List 內的資料仍可新增、修改與刪除。
    // static 讓每次 HTTP 請求建立的 Controller 物件共用同一份練習資料。
    // shortcut: 目前使用記憶體資料且不支援多使用者同時寫入，學到 Service 與資料庫時移出 Controller。
    private static readonly List<TodoItem> TodoItems = new()
    {
        // new TodoItem 會建立一筆待辦事項；大括號內是物件初始化設定。
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
    /// 取得所有待辦事項。
    /// </summary>
    /// <returns>HTTP 200 與待辦事項陣列。</returns>
    // HttpGet 表示這個 Action 處理 GET /todos。
    [HttpGet]
    public ActionResult<List<TodoItem>> GetAll()
    {
        // Ok 會建立 HTTP 200 回應，並將 TodoItems 自動序列化為 JSON。
        return Ok(TodoItems);
    }

    /// <summary>
    /// 建立一筆待辦事項。
    /// </summary>
    /// <param name="request">Client 傳入的 JSON Request Body。</param>
    /// <returns>成功時回傳 HTTP 201 與新資料；Title 空白時回傳 HTTP 400。</returns>
    // HttpPost 表示這個 Action 處理 POST /todos。
    [HttpPost]
    public ActionResult<TodoItem> Create(CreateTodoRequest request)
    {
        // IsNullOrWhiteSpace 會檢查 null、空字串及只含空白的文字。
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            // BadRequest 會建立 HTTP 400 回應。
            return BadRequest(new { message = "Title 不可為空白。" });
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

        // Add 將新物件加入記憶體中的 List。
        TodoItems.Add(todoItem);

        // Created 會建立 HTTP 201 回應，並在 Location Header 提供新資源網址。
        return Created($"/todos/{todoItem.Id}", todoItem);
    }

    /// <summary>
    /// 完整更新指定待辦事項。
    /// </summary>
    /// <param name="id">網址中的待辦事項 Id。</param>
    /// <param name="request">Client 傳入的 JSON Request Body。</param>
    /// <returns>成功時回傳 HTTP 200；資料錯誤時回傳 400；找不到時回傳 404。</returns>
    // {id:int} 表示網址中的 id 必須是整數。
    [HttpPut("{id:int}")]
    public ActionResult<TodoItem> Update(int id, UpdateTodoRequest request)
    {
        // FirstOrDefault 尋找第一筆 Id 相符的資料；找不到時回傳 null。
        TodoItem? todoItem = TodoItems.FirstOrDefault(item => item.Id == id);

        // is null 用來判斷變數目前沒有指向任何物件。
        if (todoItem is null)
        {
            // NotFound 會建立 HTTP 404 回應。
            return NotFound(new { message = $"找不到 Id 為 {id} 的待辦事項。" });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Title 不可為空白。" });
        }

        // 修改找到的 TodoItem；Trim 會移除文字前後的空白。
        todoItem.Title = request.Title.Trim();
        todoItem.IsCompleted = request.IsCompleted;

        // Ok 會建立 HTTP 200 回應，並回傳更新後的資料。
        return Ok(todoItem);
    }

    /// <summary>
    /// 刪除指定待辦事項。
    /// </summary>
    /// <param name="id">網址中的待辦事項 Id。</param>
    /// <returns>成功時回傳 HTTP 204；找不到時回傳 HTTP 404。</returns>
    // HttpDelete 表示這個 Action 處理 DELETE /todos/{id}。
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        TodoItem? todoItem = TodoItems.FirstOrDefault(item => item.Id == id);

        if (todoItem is null)
        {
            return NotFound(new { message = $"找不到 Id 為 {id} 的待辦事項。" });
        }

        // Remove 會從 List 中移除找到的物件。
        TodoItems.Remove(todoItem);

        // NoContent 會建立沒有 Response Body 的 HTTP 204 回應。
        return NoContent();
    }
}
