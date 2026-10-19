// 匯入 Todo API 接收的 Request 類別。
using AspNetLearning.Contracts;

// 匯入從 JWT Claim 讀取會員 Id 的 Extension Method。
using AspNetLearning.Extensions;

// 匯入待辦事項的資料模型。
using AspNetLearning.Models;

// 匯入負責 Todo CRUD 邏輯的 Service。
using AspNetLearning.Services;

// 匯入 Authorize Attribute，讓整個 Controller 都必須通過 JWT 驗證。
using Microsoft.AspNetCore.Authorization;

// 匯入 ControllerBase、HTTP 路由 Attribute 與 ActionResult 等 ASP.NET Core MVC 類別。
using Microsoft.AspNetCore.Mvc;

// 宣告此檔案所屬的命名空間，用來集中管理 Controller。
namespace AspNetLearning.Controllers;

/// <summary>
/// 接收待辦事項的 HTTP 請求，並決定要回傳的 HTTP 狀態碼與資料。
/// </summary>
// ApiController 會啟用 Web API Controller 的行為，例如從 Request Body 綁定複雜型別。
[ApiController]
[Authorize]
// Route 指定這個 Controller 內所有 Action 共用的網址前綴。
[Route("todos")]
public sealed class TodosController : ControllerBase
{
    // readonly 表示建構完成後，_todoService 不可以改指向另一個 TodoService 物件。
    private readonly TodoService _todoService;

    /// <summary>
    /// 建立 TodosController，並接收 Dependency Injection 容器提供的 TodoService。
    /// </summary>
    /// <param name="todoService">負責管理待辦事項資料與 CRUD 邏輯的服務。</param>
    // 這個方法與類別同名且沒有回傳型別，因此是 Constructor（建構式）。
    public TodosController(TodoService todoService)
    {
        // 將傳入的 todoService 保存到欄位，讓其他 Action 可以使用同一個服務。
        _todoService = todoService;
    }

    /// <summary>
    /// 取得所有待辦事項。
    /// </summary>
    /// <returns>HTTP 200 與待辦事項陣列。</returns>
    // HttpGet 表示這個 Action 處理 GET /todos。
    [HttpGet]
    public async Task<ActionResult<List<TodoItem>>> GetAll()
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized(new { message = "Token 缺少有效的會員 Id。" });
        }

        // await 等待資料庫查詢完成時，不會阻塞處理其他請求的 Thread。
        List<TodoItem> todoItems = await _todoService.GetAllAsync(userId);

        // Ok 會建立 HTTP 200 回應，並將 todoItems 自動序列化為 JSON。
        return Ok(todoItems);
    }

    /// <summary>
    /// 建立一筆待辦事項。
    /// </summary>
    /// <param name="request">Client 傳入的 JSON Request Body。</param>
    /// <returns>成功時回傳 HTTP 201 與新資料；Title 空白時回傳 HTTP 400。</returns>
    // HttpPost 表示這個 Action 處理 POST /todos。
    [HttpPost]
    public async Task<ActionResult<TodoItem>> Create(CreateTodoRequest request)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized(new { message = "Token 缺少有效的會員 Id。" });
        }

        // IsNullOrWhiteSpace 會檢查 null、空字串及只含空白的文字。
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            // BadRequest 會建立 HTTP 400 回應。
            return BadRequest(new { message = "Title 不可為空白。" });
        }

        // 在送進資料庫前檢查長度，避免 PostgreSQL 拒絕資料後變成 HTTP 500。
        if (request.Title.Length > 100)
        {
            return BadRequest(new { message = "Title 最多只能有 100 個字元。" });
        }

        // Controller 將已驗證的輸入交給 Service，由 Service 建立並保存 TodoItem。
        TodoItem todoItem = await _todoService.CreateAsync(userId, request.Title);

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
    public async Task<ActionResult<TodoItem>> Update(int id, UpdateTodoRequest request)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized(new { message = "Token 缺少有效的會員 Id。" });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Title 不可為空白。" });
        }

        if (request.Title.Length > 100)
        {
            return BadRequest(new { message = "Title 最多只能有 100 個字元。" });
        }

        // Service 尋找並更新資料；找不到時會回傳 null。
        TodoItem? todoItem = await _todoService.UpdateAsync(userId, id, request.Title, request.IsCompleted);

        // is null 用來判斷 Service 沒有找到指定資料。
        if (todoItem is null)
        {
            // NotFound 會建立 HTTP 404 回應。
            return NotFound(new { message = $"找不到 Id 為 {id} 的待辦事項。" });
        }

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
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.TryGetUserId(out int userId))
        {
            return Unauthorized(new { message = "Token 缺少有效的會員 Id。" });
        }

        // Service 刪除成功會回傳 true，找不到資料則回傳 false。
        bool isDeleted = await _todoService.DeleteAsync(userId, id);

        if (!isDeleted)
        {
            return NotFound(new { message = $"找不到 Id 為 {id} 的待辦事項。" });
        }

        // NoContent 會建立沒有 Response Body 的 HTTP 204 回應。
        return NoContent();
    }
}
