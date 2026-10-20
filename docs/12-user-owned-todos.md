# 第十二課：會員專屬待辦事項

這一課將 Todo API 改成會員功能。每位會員只能讀取、建立、修改及刪除自己的待辦事項。

## 請求流程

```text
Vue 或 Postman
    ↓ Authorization: Bearer <JWT>
JWT 驗證簽章、期限及會員是否仍存在
    ↓
TodosController 從 sub Claim 取得 userId
    ↓
TodoService 使用 userId 查詢或修改資料
    ↓
PostgreSQL todos.UserId → users.Id
```

## `[Authorize]` 保護整個 Controller

`TodosController` 類別加上 `[Authorize]`，所以 `GET`、`POST`、`PUT`、`DELETE` 都必須帶有效 JWT。沒有 Token、Token 無效或 Token 所屬帳號已不存在時，ASP.NET Core 會先回傳 HTTP 401，不會進入 Action。

```csharp
[ApiController]
[Authorize]
[Route("todos")]
public sealed class TodosController : ControllerBase
```

## 從 JWT 取得會員 Id

登入時，伺服器將會員 Id 寫入標準 `sub` Claim。`TryGetUserId` Extension Method 集中處理讀取及整數轉換，Controller 不需要重複解析 Claim。

```csharp
if (!User.TryGetUserId(out int userId))
{
    return Unauthorized(new { message = "Token 缺少有效的會員 Id。" });
}
```

`out int userId` 表示方法除了回傳成功或失敗的 `bool`，也會把解析結果寫入 `userId`。

## Service 必須同時比對 Todo Id 與會員 Id

只用 Todo Id 查詢會讓會員猜測編號後修改別人的資料。更新和刪除都同時比對兩個欄位：

```csharp
TodoItem? todoItem = await _dbContext.TodoItems
    .SingleOrDefaultAsync(todo => todo.Id == id && todo.UserId == userId);
```

找不到自己的資料時統一回傳 HTTP 404。這不會透露該 Id 是否屬於其他會員。

## 為什麼 `UserId` 暫時允許 `null`

資料庫在這一課以前已有 Todo 資料，但它們沒有會員資訊。Migration 新增 nullable `UserId`，讓既有資料保留；Service 只查詢目前會員的 `UserId`，所以舊資料不會顯示給任何會員。

新建立的 Todo 一律寫入登入會員 Id。資料庫 Foreign Key 保證該會員存在，會員被刪除時也會連帶刪除其 Todo。

## Vue 如何帶入 Token

登入成功後，前端將 JWT 保存於 `sessionStorage`。Todo API 呼叫會加入：

```http
Authorization: Bearer <accessToken>
```

`sessionStorage` 在分頁關閉後清除，適合目前的學習專案。正式系統仍需要評估 Cookie、XSS 防護及 Refresh Token。

## Postman 測試順序

1. 執行「第九課：會員註冊」。
2. 執行「第十課：會員登入」，Test Script 會保存 `accessToken`。
3. 執行「第三課：建立待辦事項」，Test Script 會保存新的 `todoId`。
4. 執行取得、更新及刪除待辦事項。

用第二個會員登入後，取得清單不會出現第一個會員的資料；使用第一個會員的 `todoId` 更新或刪除會得到 HTTP 404。
