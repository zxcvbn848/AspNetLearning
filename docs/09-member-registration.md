# 第九課：會員註冊與密碼雜湊

這一階段建立第一個會員 API：`POST /auth/register`。Vue 畫面會收集 Email 與密碼，ASP.NET Core 驗證資料後，將會員保存到 PostgreSQL。

## 請求流程

```text
Vue 會員註冊表單
    ↓ POST /auth/register
RegisterRequest 驗證 Email 與密碼
    ↓
AuthController 決定 HTTP 狀態碼
    ↓
UserService 正規化 Email 並產生密碼雜湊
    ↓
AppDbContext 寫入 PostgreSQL users 資料表
```

## User Model

`Models/User.cs` 對應 PostgreSQL 的 `users` 資料表：

| 欄位 | 用途 |
|---|---|
| `Id` | 會員流水號 |
| `Email` | 正規化後的登入帳號 |
| `PasswordHash` | 密碼雜湊，不能回傳給 Client |
| `CreatedAtUtc` | UTC 格式的建立時間 |

Email 有唯一索引。即使兩個註冊請求同時進入，PostgreSQL 仍會阻止重複帳號。

## Request 驗證

`RegisterRequest` 使用 Data Annotations：

```csharp
[Required]
[EmailAddress]
[MaxLength(254)]
public required string Email { get; init; }
```

因為 Controller 標記了 `[ApiController]`，驗證失敗時 ASP.NET Core 會在 Action 執行前自動回傳 HTTP 400。Controller 不需要為每個欄位重複撰寫 `if`。

密碼設定為 8 至 100 個字元。這一階段不要求特殊符號，重點是先理解註冊與雜湊流程。

## PasswordHasher

`IPasswordHasher<User>` 是 ASP.NET Core 提供的密碼雜湊介面：

```csharp
user.PasswordHash = _passwordHasher.HashPassword(user, password);
```

雜湊是單向運算，登入時會使用驗證函式比對，不會把雜湊還原成原始密碼。API Response 使用 `UserResponse`，刻意排除 `PasswordHash`。

## HTTP 回應

| 情況 | 狀態碼 |
|---|---|
| 註冊成功 | `201 Created` |
| Email 或密碼格式錯誤 | `400 Bad Request` |
| Email 已經存在 | `409 Conflict` |

成功範例：

```json
{
  "id": 1,
  "email": "andy@example.com",
  "createdAtUtc": "2026-10-19T09:00:00Z"
}
```

Response 不會包含密碼或密碼雜湊。

## Vue 畫面

`RegisterForm.vue` 使用三個響應式欄位：Email、密碼與確認密碼。確認密碼只存在瀏覽器，用來避免輸入錯誤，不會送到 API。

Vite 將 `/auth` 代理到 ASP.NET Core 容器，因此瀏覽器仍從下列網址操作：

```text
http://localhost:8181
```

登入與 JWT 會放在下一階段，避免一次混合註冊、Token 與授權三種概念。
