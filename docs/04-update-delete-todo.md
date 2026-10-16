# 第四課：使用 PUT 與 DELETE

這一課完成記憶體 CRUD，加入更新與刪除待辦事項。

## 更新待辦事項

```text
PUT /todos/2
```

```json
{
  "title": "認識 Controller 與 Service",
  "isCompleted": true
}
```

路由中的 `{id:int}` 表示 `id` 必須是整數。ASP.NET Core 會把網址中的數字放入 `int id`。

```csharp
TodoItem? todoItem = TodoItems.FirstOrDefault(item => item.Id == id);
```

- `FirstOrDefault` 尋找第一筆符合條件的資料。
- `item => item.Id == id` 比較每筆資料的 Id。
- `TodoItem?` 的問號表示結果可能是 `null`。
- 找不到資料時回傳 HTTP 404 Not Found。

找到資料後修改可寫入的屬性：

```csharp
todoItem.Title = request.Title.Trim();
todoItem.IsCompleted = request.IsCompleted;
```

因此 `TodoItem.Title` 從 `{ get; init; }` 改成 `{ get; set; }`。`init` 只能在建立物件時設定，`set` 允許建立後修改。

## 刪除待辦事項

```text
DELETE /todos/2
```

找到資料後使用：

```csharp
TodoItems.Remove(todoItem);
```

刪除成功回 HTTP 204 No Content。204 表示操作成功，但 Response Body 沒有內容。

## CRUD 對照

| 操作 | HTTP | 路由 | List 方法 |
|---|---|---|---|
| 讀取 | GET | `/todos` | 直接回傳集合 |
| 建立 | POST | `/todos` | `Add` |
| 更新 | PUT | `/todos/{id}` | 找到物件後修改屬性 |
| 刪除 | DELETE | `/todos/{id}` | `Remove` |

## 回應狀態碼

| 狀態碼 | 意義 |
|---|---|
| 200 | 更新成功 |
| 204 | 刪除成功，沒有回應內容 |
| 400 | 輸入資料不合法 |
| 404 | 找不到指定 Id |

下一階段會把這些 HTTP 處理搬入 Controller，並將 List 與操作邏輯搬入 Service。
