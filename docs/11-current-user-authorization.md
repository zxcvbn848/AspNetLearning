# 第十一課：目前會員與 JWT 授權

這一階段新增 `GET /auth/me`，它是專案中第一個必須登入才能使用的 API。

## 請求流程

```text
Client 加入 Authorization: Bearer <JWT>
    ↓
JwtBearer 驗證簽章、Issuer、Audience 與到期時間
    ↓
[Authorize] 允許有效身分進入 Controller
    ↓
AuthController 從 sub Claim 取得會員 Id
    ↓
UserService 從 PostgreSQL 讀取會員
```

## `[Authorize]`

```csharp
[Authorize]
[HttpGet("me")]
public async Task<ActionResult<UserResponse>> GetCurrentUser()
```

`[Authorize]` 表示這個 Action 需要通過已註冊的驗證方式。目前預設方式是 JWT Bearer，因此沒有 Token、簽章錯誤或 Token 過期時，ASP.NET Core 會在執行 Action 前直接回傳 HTTP 401。

## Claim 與 sub

登入時，`TokenService` 把會員 Id 放進 JWT 標準的 `sub` Claim。授權成功後，Controller 使用：

```csharp
string? subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
```

`User` 是由 JwtBearer 建立的 `ClaimsPrincipal`，其中的 Claim 已通過簽章驗證。Controller 不會採用 Query String 或 Request Body 傳來的會員 Id，避免使用者冒充其他會員。

## MapInboundClaims

`Program.cs` 設定：

```csharp
options.MapInboundClaims = false;
```

這會保留 JWT 原始 Claim 名稱，因此 Token 中的 `sub` 在 Controller 仍叫作 `sub`，閱讀與除錯比較直接。

## API 回應

有效 Token 回傳 HTTP 200：

```json
{
  "id": 1,
  "email": "andy@example.com",
  "createdAtUtc": "2026-10-19T09:00:00Z"
}
```

以下情況回傳 HTTP 401：

- 缺少 `Authorization` Header。
- Token 格式或簽章錯誤。
- Issuer 或 Audience 不符。
- Token 已過期。
- Token 對應的會員已不存在。

## Vue 畫面

`CurrentUserPanel.vue` 從 `sessionStorage` 取得 Token，並將它放進 Header：

```text
Authorization: Bearer eyJ...
```

登入成功後，`LoginForm` 發出 `loggedIn` 事件，`App.vue` 重新建立目前會員元件，使畫面立即呼叫 `/auth/me`。如果 Token 無效或過期，前端會移除它。

## Postman 順序

1. 第九課：會員註冊
2. 第十課：會員登入
3. 第十一課：取得目前會員

登入請求的 Test Script 會保存 `accessToken`，第三個請求會自動以 Bearer Token 呼叫 API。

這一階段只保護 `/auth/me`。Todo API 會在下一個階段加入會員關聯與授權。
