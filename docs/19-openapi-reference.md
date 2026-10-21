# 第十九課：用 OpenAPI 閱讀與測試 API

這一課把現有 API 轉成可瀏覽的文件。啟動 Docker Compose 後，打開 [http://localhost:8180/scalar](http://localhost:8180/scalar) 可查看端點、輸入欄位、JSON 回應與狀態碼，並直接送出測試請求。機器可讀的規格在 [http://localhost:8180/openapi/v1.json](http://localhost:8180/openapi/v1.json)。兩者只在 `Development` 環境提供；Production 不會公開這些路由。

## 文件從哪裡來

`Microsoft.AspNetCore.OpenApi` 根據路由、Request 類別、Response 類別及 Attribute 產生規格。專案開啟 `GenerateDocumentationFile`，因此 Controller 和資料模型中的 `///` XML 註解也會變成頁面上的摘要與欄位說明。`Scalar.AspNetCore` 只負責把同一份規格顯示成互動頁面，不另寫一份 API 定義。

例如 `POST /todos` 成功實際回傳 **201**，而 `ActionResult<T>` 本身不會推斷每一條程式分支的狀態碼，所以 Controller 明確標示 `201`、驗證失敗 `400`、未登入 `401`。這些 Attribute 是文件描述，不會取代 Controller 原本的回應邏輯。

| 路由 | 用途 | 主要狀態碼 | 授權 |
| --- | --- | --- | --- |
| `GET /`、`GET /hello/{name}` | 啟動檢查與問候 | 200 | 無 |
| `POST /auth/register` | 註冊 | 201、400、409 | 無 |
| `POST /auth/login` | 登入 | 200、400、401 | 無；成功後設定 Refresh Cookie |
| `POST /auth/refresh` | 更新 JWT 與 Refresh Cookie | 200、401 | HttpOnly Refresh Cookie |
| `POST /auth/logout` | 撤銷目前工作階段 | 204 | 有 Cookie 時撤銷；沒有也回傳 204 |
| `GET /auth/me` | 取得目前會員 | 200、401 | Bearer JWT |
| `GET /todos`、`POST /todos` | 查詢與新增待辦事項 | 200／201、400、401 | Bearer JWT |
| `PUT /todos/{id}`、`DELETE /todos/{id}` | 更新與刪除待辦事項 | 200／204、400、401、404 | Bearer JWT |
| `GET /debug/error` | 刻意觸發全域錯誤處理 | 500 | 僅 Development |

## 在文件頁試一次完整流程

1. 打開 Scalar，先在 `POST /auth/register` 輸入 Email 與至少八個字元的密碼，再送出請求。若已註冊，可直接從登入開始。
2. 在 `POST /auth/login` 使用同一組帳密。回應 JSON 的 `accessToken` 是 15 分鐘 JWT；瀏覽器會另外保存 `HttpOnly` Refresh Cookie。
3. 在文件頁的 Bearer 授權欄貼上 **JWT 字串本身**，再呼叫 `GET /auth/me` 或 `GET /todos`。瀏覽器會將它放入 `Authorization: Bearer <token>`。
4. 呼叫 `POST /auth/refresh`。因為文件頁與 API 同源，瀏覽器會自動附上 Refresh Cookie；回應會給新的 JWT，使用受保護端點前要把授權欄換成新 JWT。
5. 呼叫 `POST /auth/logout` 後再試 `POST /auth/refresh`，應得到 401。已簽發的 JWT 仍在原定到期時間前有效。

例如註冊請求的 JSON Body：

```json
{
  "email": "learner@example.com",
  "password": "Learning123!"
}
```

新增 Todo 的 JSON Body：

```json
{
  "title": "閱讀 API 文件"
}
```

資料格式與驗證規則以頁面上最新產生的 Schema 為準。`400`、`401`、`404`、`409` 等錯誤使用 Problem Details；其 `traceId` 可以協助對照後端 Log。Postman Collection 仍保留，適合保存一組固定測試流程；OpenAPI 頁面適合探索單一端點與欄位。

閱讀程式碼時依序看 `Program.cs` 的 `AddLearningOpenApi()` 與開發環境路由、`OpenApi/ApiDocumentation.cs` 的 Bearer/Cookie 定義、Controller 的 `ProducesResponseType`，最後看 `Contracts` 類別與 XML 註解。這樣可以對照「實際 HTTP 行為」和「文件如何描述行為」。
