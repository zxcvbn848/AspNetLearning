# 第三課：使用 POST 建立待辦事項

這一課仍以閱讀與呼叫 API 為主。目標是理解 JSON Request Body 如何變成 C# 物件，以及程式如何驗證並加入新資料。

## API 功能

```text
POST http://localhost:8180/todos
```

Request Body：

```json
{
  "title": "閱讀 POST API 文件"
}
```

成功時回傳 HTTP 201 Created：

```json
{
  "id": 3,
  "title": "閱讀 POST API 文件",
  "isCompleted": false
}
```

## 檔案分工

```text
Contracts/CreateTodoRequest.cs
    定義 Client 建立待辦事項時可以傳入哪些欄位

Models/TodoItem.cs
    定義完整待辦事項的資料形狀

Endpoints/TodoEndpoints.cs
    驗證輸入、建立物件並加入 List
```

`CreateTodoRequest` 沒有 `Id` 與 `IsCompleted`，因為這兩個值由伺服器決定，Client 不應自行指定。

## Request Body 轉成 C# 物件

```csharp
app.MapPost("/todos", (CreateTodoRequest request) =>
```

- `MapPost` 註冊 HTTP POST 路由。
- ASP.NET Core 讀取 JSON Request Body。
- JSON 的 `title` 會對應到 C# 的 `Title` 屬性。
- 轉換後的物件放入 `request` 參數。

這個過程仍屬於 Model Binding。

## 輸入驗證

```csharp
if (string.IsNullOrWhiteSpace(request.Title))
{
    return Results.BadRequest(new { message = "Title 不可為空白。" });
}
```

### `if`

`if` 會判斷小括號中的條件：

- 條件是 `true`：執行大括號中的內容。
- 條件是 `false`：跳過大括號，繼續執行後面的程式。

### `string.IsNullOrWhiteSpace`

以下內容都會被判定為無效：

```text
null
""
"   "
```

### `Results.BadRequest`

建立 HTTP 400 Bad Request，表示 Client 傳入的資料不符合 API 要求。

## 產生下一個 Id

```csharp
int nextId = TodoItems.Count == 0
    ? 1
    : TodoItems.Max(todoItem => todoItem.Id) + 1;
```

### `==`

比較左右兩邊是否相等。`TodoItems.Count == 0` 用來判斷清單是否沒有資料。

### `? :`

這是條件運算子：

```text
條件 ? 條件成立時的值 : 條件不成立時的值
```

如果清單為空，`nextId` 是 1；否則取出目前最大 Id 再加 1。

### `Max`

`Max` 是 LINQ 方法。`todoItem => todoItem.Id` 指定要比較每個物件的 `Id` 屬性。

## 建立並加入物件

```csharp
TodoItem todoItem = new()
{
    Id = nextId,
    Title = request.Title.Trim(),
    IsCompleted = false
};

TodoItems.Add(todoItem);
```

- `Trim()` 移除文字前後的空白。
- 新待辦事項預設為未完成，所以 `IsCompleted` 是 `false`。
- `List.Add` 將新物件加入集合尾端。

## 回傳 HTTP 201

```csharp
return Results.Created($"/todos/{todoItem.Id}", todoItem);
```

- `Created` 產生 HTTP 201 Created。
- 第一個參數是新資源的位置，例如 `/todos/3`。
- 第二個參數是要轉換成 JSON 的新物件。

## 資料流

```text
Postman 傳送 JSON
    ↓
ASP.NET Core 建立 CreateTodoRequest
    ↓
檢查 Title
    ↓
產生下一個 Id
    ↓
建立 TodoItem
    ↓
List.Add
    ↓
回傳 HTTP 201 與 JSON
```

## 目前限制

目前的 `List<TodoItem>` 適合單人學習，不適合多個請求同時寫入。資料庫階段會讓 PostgreSQL 安全地保存資料並產生 Id。
