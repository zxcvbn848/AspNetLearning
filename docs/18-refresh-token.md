# 第十八課：Refresh Token 與工作階段

登入現在會同時產生兩種 Token。`TokenService` 簽發有效 60 分鐘的 JWT Access Token，前端保存在目前分頁的 `sessionStorage`，並放入受保護 API 的 `Authorization: Bearer` Header。`RefreshTokenService` 另外產生 32 bytes 的隨機 Refresh Token，透過 `HttpOnly`、`SameSite=Strict`、`Path=/auth` Cookie 交給瀏覽器；正式環境的 Cookie 另設 `Secure`，只透過 HTTPS 傳送。JavaScript 讀不到這枚 Cookie。

資料庫的 `refresh_tokens` 資料表只保存 Refresh Token 的 SHA-256 雜湊、會員編號、到期時間與撤銷時間。即使讀到資料庫，也不能直接拿雜湊當 Token 使用。每次登入建立一個 `FamilyId`，用來區分不同裝置或瀏覽器的工作階段。

```text
POST /auth/login
  → 回傳 JWT；Set-Cookie 寫入 Refresh Token
受保護 API 回傳 401
  → POST /auth/refresh 使用 Cookie
  → 作廢舊 Refresh Token、建立新 Token 與 JWT
  → 使用新 JWT 重試原本的 API 一次
POST /auth/logout
  → 撤銷目前 FamilyId 並刪除 Cookie
```

`POST /auth/refresh` 不需要 JWT，但必須帶有效 Cookie。成功時回傳與登入相同格式的 `LoginResponse`；沒有 Cookie、過期或重複使用舊 Token 時回傳 401。舊 Token 被重複使用時，伺服器會撤銷同一個工作階段的其他 Refresh Token。`POST /auth/logout` 成功回傳 204，即使 Cookie 已不存在也一樣。它不會登出其他裝置。

前端的 `apiClient.ts` 讓同時失效的請求共用一次更新流程，防止兩個請求同時輪替同一枚 Cookie。若更新失敗才清除登入狀態；登出時先等待正在進行的更新，再請伺服器撤銷最新 Cookie。延遲回來的舊帳號 API 回應不會覆蓋新帳號資料。

## 用 Postman 驗證

1. 執行會員註冊與登入。登入回應的 `Set-Cookie` 會由 Postman Cookie Jar 保存。
2. 執行「第十八課：更新 Access Token」，確認 200 與新的 JWT，Collection Variable `accessToken` 會更新。
3. 執行「第十八課：登出並撤銷 Refresh Token」，確認 204；再執行更新請求應得到 401。

若單獨用 `curl`，必須使用 Cookie Jar；只複製 JWT 不足以呼叫更新端點。重新整理前端頁面後，`sessionStorage` 仍有 JWT 時可以繼續使用，JWT 過期時會嘗試自動更新。關閉分頁後 `sessionStorage` 清空，目前不會用 Cookie 自動重新登入。

已啟動 Docker Compose 時，可用 `node tests/auth-smoke.mjs` 重跑登入、輪替、重放、跨會員隔離及登出的整合測試；腳本會刪除自己建立的測試帳號。

現有 JWT 是無狀態的：登出撤銷的是 Refresh Token，已簽發的 Access Token 在原定 60 分鐘到期前仍有效。正式上線若需要「登出立刻撤銷所有 JWT」，要另加伺服器端撤銷檢查；正式部署也必須使用 HTTPS、私密 JWT 金鑰及安全的資料庫密碼。
