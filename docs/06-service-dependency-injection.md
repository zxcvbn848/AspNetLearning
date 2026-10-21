# 第六課：Service 與 Dependency Injection

上一課的 Controller 同時負責 HTTP 回應、資料保存與 CRUD 邏輯。這一課把資料和操作移到 `TodoService`，讓 Controller 專注處理 HTTP。

## 請求流程

```text
Postman
   ↓ HTTP Request
TodosController
   ↓ 呼叫方法
TodoService
   ↓ 操作 List<TodoItem>
TodosController
   ↓ HTTP Response
Postman
```

## 各層責任

| 檔案 | 責任 |
|---|---|
| `Program.cs` | 註冊 Controller 與 Service，組合應用程式 |
| `TodosController.cs` | 接收 HTTP 請求、驗證輸入、決定狀態碼 |
| `TodoService.cs` | 保存 Todo 資料並執行 CRUD |
| `TodoItem.cs` | 定義一筆 Todo 的資料形狀 |
| `CreateTodoRequest.cs` | 定義建立 Todo 的輸入格式 |
| `UpdateTodoRequest.cs` | 定義更新 Todo 的輸入格式 |

## 註冊 Service

`Program.cs` 使用：

```csharp
builder.Services.AddSingleton<TodoService>();
```

`builder.Services` 是 ASP.NET Core 內建的 Dependency Injection Container。這行告訴容器：當某個類別需要 `TodoService` 時，由容器負責提供。

`AddSingleton` 表示整個應用程式執行期間只建立一個 `TodoService`。目前 Todo 儲存在 Service 的 `List`，因此需要讓所有請求共用同一個 Service；否則每次請求都會拿到重新初始化的清單。

## Constructor Injection

Controller 使用建構式宣告它需要 `TodoService`：

```csharp
private readonly TodoService _todoService;

public TodosController(TodoService todoService)
{
    _todoService = todoService;
}
```

這個過程稱為 Constructor Injection：

1. ASP.NET Core 要建立 `TodosController`。
2. 發現建構式需要 `TodoService`。
3. 到 Dependency Injection Container 尋找已註冊的 `TodoService`。
4. 將同一個 Singleton 物件傳進建構式。
5. Controller 把它保存到 `_todoService` 欄位。

`readonly` 表示 `_todoService` 在建構完成後不能改指向其他物件。

## Controller 變簡單

取得清單時，Controller 不再知道資料放在哪裡：

```csharp
IReadOnlyList<TodoItem> todoItems = _todoService.GetAll();
return Ok(todoItems);
```

更新時，Service 負責尋找及修改資料，Controller 根據結果決定 HTTP 狀態碼：

```csharp
TodoItem? todoItem = _todoService.Update(id, request.Title, request.IsCompleted);

if (todoItem is null)
{
    return NotFound(...);
}

return Ok(todoItem);
```

這個分工很重要：`TodoService` 不需要知道 HTTP 404，Controller 也不需要知道如何搜尋 List。

## Singleton、Scoped 與 Transient

| 註冊方法 | 物件生命週期 | 常見用途 |
|---|---|---|
| `AddSingleton` | 整個應用程式共用一個 | 共用且可安全重複使用的服務 |
| `AddScoped` | 每個 HTTP Request 建立一個 | `DbContext` 與使用資料庫的 Service |
| `AddTransient` | 每次取得都建立一個 | 輕量且不保存狀態的服務 |

目前使用 `AddSingleton` 是為了保存記憶體資料。接上 Entity Framework Core 後，Service 會配合 `DbContext` 改成 Scoped。

## 為何目前沒有介面

專案目前只有一個 `TodoService` 實作，也還沒有需要替換它。先直接注入具體類別，可以更清楚地觀察 Dependency Injection 的核心流程。未來真的需要替換實作或隔離測試時，再加入 `ITodoService`。

## 使用 Postman 驗證

網址與 JSON 格式沒有改變。重新匯入 Collection 後，依序執行 GET、POST、PUT、DELETE，應取得和上一課相同的狀態碼與資料。
