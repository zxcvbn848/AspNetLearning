# 第十課：會員登入與 JWT

這一階段新增 `POST /auth/login`。會員輸入 Email 與密碼後，ASP.NET Core 會驗證密碼雜湊，成功時簽發一枚有效 15 分鐘的 JWT Access Token。

## 登入流程

```text
Vue 登入表單
    ↓ POST /auth/login
LoginRequest 驗證必填與 Email 格式
    ↓
UserService 尋找會員並驗證 PasswordHash
    ↓
TokenService 簽發 JWT
    ↓
Vue 將 Token 保存到目前分頁的 sessionStorage
```

## 驗證密碼

登入不能重新雜湊輸入密碼後直接比較，因為安全的密碼雜湊每次都會包含不同 Salt。程式改用：

```csharp
_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
```

結果可能是：

- `Failed`：密碼錯誤。
- `Success`：密碼正確。
- `SuccessRehashNeeded`：密碼正確，但建議使用較新的參數重新雜湊。

Email 不存在與密碼錯誤都只回傳 `Email 或密碼不正確`，避免 API 洩漏已註冊的帳號。

## JWT 結構

JWT 由三段以句點分隔的文字組成：

```text
Header.Payload.Signature
```

- Header：演算法與 Token 類型。
- Payload：會員 Id、Email、到期時間等 Claim。
- Signature：伺服器使用金鑰產生的簽章，用來檢查內容是否被修改。

JWT 的 Payload 只是 Base64Url 編碼，並沒有加密，因此不能放密碼或其他秘密資料。

## 簽章與驗證設定

本機開發金鑰由 `compose.yaml` 的 `Jwt__Key` 提供。雙底線代表設定階層 `Jwt:Key`。

正式環境必須使用部署平台提供的強隨機金鑰，不可沿用提交在專案裡的本機預設值。

`Program.cs` 註冊 `AddJwtAuthentication()`；實際檢查寫在 `Authentication/JwtAuthenticationExtensions.cs`：

- Issuer 是否正確。
- Audience 是否正確。
- HMAC-SHA256 簽章是否正確。
- Token 是否已到期。
- 金鑰是否至少 32 bytes。

## API 回應

登入成功回傳 HTTP 200：

```json
{
  "accessToken": "eyJ...",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-10-19T11:00:00Z",
  "user": {
    "id": 1,
    "email": "andy@example.com",
    "createdAtUtc": "2026-10-19T09:00:00Z"
  }
}
```

帳號或密碼錯誤時回傳 HTTP 401。

## Vue 與 Postman

Vue 登入成功後會把 Token 放在 `sessionStorage`。關閉該分頁後資料會清除；下一階段會把 Token 放入 `Authorization: Bearer ...` Header 呼叫受保護 API。

Postman 的正確執行順序是：

1. 執行「第九課：會員註冊」，產生隨機 Email。
2. 執行「第十課：會員登入」。
3. Test Script 會把 JWT 保存到 `accessToken` Collection Variable。

這一階段只確認 JWT 能正確簽發與驗證，Todo API 尚未要求登入。
