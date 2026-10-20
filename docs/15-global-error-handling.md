# 第十五課：全域例外處理與 Problem Details

這一課使用 ASP.NET Core 內建的全域例外 Middleware，讓未預期例外與沒有內容的錯誤狀態統一回傳 JSON。

## Request Pipeline

ASP.NET Core 依照 `Program.cs` 的註冊順序處理每個 Request：

```text
HTTP Request
    ↓
Exception Handler Middleware
    ↓
Status Code Pages Middleware
    ↓
Authentication
    ↓
Authorization
    ↓
Endpoint / Controller
    ↓
HTTP Response
```

Exception Handler 放在前面，才能捕捉後面 Middleware 與 Endpoint 沒有處理的例外。

## 註冊 Problem Details

```csharp
builder.Services.AddProblemDetails();
```

Problem Details 是 HTTP API 的標準錯誤格式，常見欄位包括：

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "伺服器發生未預期錯誤。",
  "status": 500,
  "detail": "這是開發環境的預期測試例外。",
  "traceId": "0HN...:00000001"
}
```

- `title`：適合顯示給使用者的錯誤摘要。
- `status`：HTTP 狀態碼。
- `detail`：開發環境的診斷訊息。
- `traceId`：對照 Client 錯誤與伺服器 Log。

## 全域例外 Middleware

```csharp
app.UseExceptionHandler();
```

未處理例外會由它攔截、記錄 Log，並透過 Problem Details 回傳 HTTP 500。Controller 不需要為未知例外重複撰寫 `try/catch`。

```csharp
app.UseStatusCodePages();
```

這會替原本沒有 Response Body 的 404 等狀態碼產生 Problem Details。

## 開發與正式環境

```csharp
if (builder.Environment.IsDevelopment())
{
    context.ProblemDetails.Detail = exception.Message;
}
```

開發環境回傳例外訊息方便除錯；正式環境只回傳安全的通用標題，避免洩漏資料庫、檔案路徑或程式內部資訊。

`GET /debug/error` 也只在 Development 環境註冊，正式環境不存在這個路由。

## 前端 `ApiError`

一般 JavaScript `Error` 只有錯誤文字。這一課建立 `ApiError`，額外保存：

```ts
public readonly status: number
public readonly traceId?: string
```

畫面可以根據狀態碼處理錯誤，使用者回報問題時也能提供 Trace Id。

專案啟用了 TypeScript `erasableSyntaxOnly`，因此沒有使用 Constructor Parameter Property，而是在 Constructor 內明確指定欄位：

```ts
this.status = status
this.traceId = traceId
```

## Vite Proxy

瀏覽器呼叫相對路徑 `/debug/error` 時，Vite 必須將它轉送到 Compose 中的 ASP.NET Core：

```ts
'/debug': {
  target: 'http://app:8080',
  changeOrigin: true,
}
```

如果沒有 Proxy，Vite 會把 `/debug/error` 當成前端路由並回傳首頁，API 測試就無法收到預期的 500。

## 測試方式

1. 在 Vue 整合頁按下「測試 HTTP 500」。
2. 確認畫面顯示 HTTP 500、測試訊息與 Trace Id。
3. 在 Postman 執行「第十五課：全域例外處理」。
4. 呼叫不存在的路徑，確認 HTTP 404 也使用 Problem Details。

目前只統一未預期例外與空白狀態碼。已知的業務結果仍由 Controller 回傳 400、401、404 或 409。
