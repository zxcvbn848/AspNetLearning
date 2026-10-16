# 第二課：讀懂 Class 與 List

這一課仍以閱讀與除錯觀察為主，不要求手寫。目標是理解 C# 如何定義資料形狀、建立物件，並用集合保存多筆資料。

## API 功能

使用 Postman 呼叫：

```text
GET http://localhost:8180/todos
```

預期回傳：

```json
[
  {
    "id": 1,
    "title": "閱讀第一階段文件",
    "isCompleted": true
  },
  {
    "id": 2,
    "title": "認識 Class 與 List",
    "isCompleted": false
  }
]
```

## 檔案分工

```text
Models/TodoItem.cs
    定義一筆待辦事項有哪些欄位

Endpoints/TodoEndpoints.cs
    建立範例資料並註冊 GET /todos

Program.cs
    呼叫 app.MapTodoEndpoints() 載入路由
```

## Class：定義資料形狀

```csharp
public sealed class TodoItem
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public bool IsCompleted { get; set; }
}
```

### `class TodoItem`

`class` 用來定義一種物件的資料與行為。`TodoItem` 是類別名稱，可以把它理解為待辦事項的設計圖。

### `public`

表示其他檔案也可以使用 `TodoItem`。

### `sealed`

表示其他類別不能繼承 `TodoItem`。目前沒有繼承需求，明確封閉可讓資料模型更容易理解。

### `int Id`

- `int` 是整數型別。
- `Id` 是屬性名稱。
- `{ get; init; }` 表示可以讀取，而且只能在建立物件時設定。

### `required string Title`

- `required` 表示建立物件時必須提供此屬性。
- `string` 表示文字型別。
- `Title` 是待辦事項內容。

### `bool IsCompleted`

- `bool` 只能是 `true` 或 `false`。
- `{ get; set; }` 表示建立物件後仍可讀取與修改。

## Object：依照 Class 建立資料

```csharp
new TodoItem
{
    Id = 1,
    Title = "閱讀第一階段文件",
    IsCompleted = true
}
```

- `new TodoItem` 表示建立新的 `TodoItem` 物件。
- 大括號內使用物件初始化運算式設定屬性。
- 每一組 `屬性 = 值` 之間使用逗號分隔。

## List：保存多筆相同型別的資料

```csharp
private static readonly List<TodoItem> TodoItems = new()
{
    new TodoItem { ... },
    new TodoItem { ... }
};
```

### `List<TodoItem>`

`List<T>` 是可以保存多筆資料的泛型集合。尖括號中的 `TodoItem` 指定這個 List 只能放入 `TodoItem` 物件。

### `private`

表示 `TodoItems` 只能在 `TodoEndpoints` 類別內使用。

### `static`

表示資料屬於類別本身，不需要先建立 `TodoEndpoints` 物件。

### `readonly`

表示 `TodoItems` 變數建立後，不能改為指向另一個 List；List 內的項目仍然可以新增、修改或刪除。

### `new()`

編譯器已從左邊的 `List<TodoItem>` 知道要建立什麼型別，因此右邊可以省略成 `new()`。

## 回傳集合

```csharp
app.MapGet("/todos", () => Results.Ok(TodoItems));
```

- `MapGet` 註冊 HTTP GET 路由。
- `/todos` 是此 API 的網址。
- Lambda 不需要輸入參數，所以使用空的小括號 `()`。
- `Results.Ok` 建立 HTTP 200 OK 回應。
- ASP.NET Core 自動把 `List<TodoItem>` 序列化成 JSON 陣列。

## 資料流

```text
Postman 呼叫 GET /todos
    ↓
ASP.NET Core 找到 TodoEndpoints 中的路由
    ↓
讀取 List<TodoItem> TodoItems
    ↓
Results.Ok 建立 HTTP 200 回應
    ↓
List 轉換成 JSON 陣列
    ↓
Postman 顯示兩筆待辦事項
```

## 目前限制

資料目前只存在容器記憶體中。重新啟動應用程式後，內容會回到程式碼中定義的兩筆範例資料；後續學到 Entity Framework Core 時再改用資料庫。
