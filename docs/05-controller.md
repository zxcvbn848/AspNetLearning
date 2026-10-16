# 第五課：將 Todo API 搬入 Controller

這一課不改變 API 網址與功能，而是將 Todo CRUD 從 Minimal API 搬到 `TodosController`，認識 ASP.NET Core 常見的 Controller 寫法。

## 請求流程

```text
Postman
   ↓ HTTP Request
Program.cs 的 MapControllers()
   ↓ 依照 Route 與 Http Attribute 找到 Action
TodosController
   ↓ 執行 CRUD
HTTP Response
```

## 註冊 Controller 服務

`Program.cs` 在建立應用程式前呼叫：

```csharp
builder.Services.AddControllers();
```

`builder.Services` 是 ASP.NET Core 的 Dependency Injection 容器。`AddControllers()` 將 Controller 所需服務註冊進容器。

建立應用程式後呼叫：

```csharp
app.MapControllers();
```

它會讀取 Controller 上的路由 Attribute，建立 HTTP 路由。

## Controller 類別

```csharp
[ApiController]
[Route("todos")]
public sealed class TodosController : ControllerBase
```

| 語法 | 用途 |
|---|---|
| `[ApiController]` | 啟用 Web API Controller 行為 |
| `[Route("todos")]` | 設定共用網址前綴 `/todos` |
| `ControllerBase` | 提供 `Ok()`、`BadRequest()`、`NotFound()` 等方法 |
| `sealed` | 表示這個類別不能再被其他類別繼承 |

## Action 與 HTTP 方法

Controller 中負責處理一種請求的方法稱為 Action：

```csharp
[HttpGet]
public ActionResult<List<TodoItem>> GetAll()
```

`[HttpGet]` 搭配 Controller 的 `[Route("todos")]`，組合成 `GET /todos`。

```csharp
[HttpPut("{id:int}")]
public ActionResult<TodoItem> Update(int id, UpdateTodoRequest request)
```

這會對應 `PUT /todos/{id}`。ASP.NET Core 將網址中的數字放進 `id`，並將 JSON Request Body 轉成 `UpdateTodoRequest`。

## ActionResult

```csharp
public ActionResult<TodoItem> Create(...)
```

`ActionResult<TodoItem>` 表示 Action 可以回傳兩類結果：

- 成功時回傳 `TodoItem` 資料。
- 失敗時回傳 `BadRequest(...)`、`NotFound(...)` 等 HTTP 結果。

刪除成功沒有資料要回傳，因此使用 `IActionResult`：

```csharp
public IActionResult Delete(int id)
```

## 與 Minimal API 比較

| Minimal API | Controller |
|---|---|
| `app.MapGet("/todos", ...)` | `[HttpGet]` |
| Lambda 處理請求 | Action 方法處理請求 |
| `Results.Ok(...)` | `Ok(...)` |
| 路由集中在 Endpoint 方法 | 路由 Attribute 放在 Controller 與 Action 上 |

兩種寫法都屬於 ASP.NET Core。Controller 在 API 數量增加後，較容易依照功能分檔閱讀。

## 本課刻意保留的限制

`List<TodoItem>` 暫時仍放在 Controller，讓這一課只專注理解 Controller。下一課會把資料與操作邏輯移到 Service，再透過 Dependency Injection 提供給 Controller。

## 使用 Postman 驗證

API 網址與格式沒有改變。重新匯入 Collection 後，依序呼叫既有的 GET、POST、PUT、DELETE 請求，便可確認 Controller 與上一課有相同行為。
