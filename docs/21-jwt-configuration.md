# 第二十一課：把 JWT 設定從 Program.cs 拆出來

這一課只調整程式碼的擺放位置，不改變登入、授權或 Token 內容。`Program.cs` 現在用一行 `builder.Services.AddJwtAuthentication(builder.Configuration)` 註冊 JWT 驗證。詳細設定集中在 `Authentication/JwtAuthenticationExtensions.cs`，方便先從入口看懂應用程式如何組裝，再深入讀驗證規則。

## 讀懂這個 Extension Method

`AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)` 的第一個參數有 `this`，所以 `builder.Services` 可以用方法語法呼叫它。`IServiceCollection` 是服務註冊清單；`IConfiguration` 提供 `Jwt:Key`、`Jwt:Issuer` 和 `Jwt:Audience`。方法先檢查簽章金鑰至少 32 bytes，再註冊 JwtBearer 驗證器。金鑰不足仍會在啟動時報錯，不會等到第一個請求才發現。

`TokenValidationParameters` 檢查 Issuer、Audience、簽章和到期時間；`ClockSkew = TimeSpan.Zero` 讓到期時間不額外寬限。`MapInboundClaims = false` 保留原本的 `sub` Claim 名稱。這些規則沒有因為移動檔案而改變。

`JwtBearerEvents` 處理兩件額外的事：`OnMessageReceived` 只允許 SignalR Hub `/hubs/todos` 與 `/hubs/chat` 從 `access_token` 查詢參數取得 JWT；`OnTokenValidated` 在簽章等檢查通過後，確認 Token 中的會員 Id 有效、資料庫中仍有這位會員。一般 API 仍使用 `Authorization: Bearer ...` Header。

## 與其他檔案的分工

`Authentication/JwtAuthenticationExtensions.cs` **驗證收到的 Token**；`Services/TokenService.cs` **登入成功後簽發 Token**；`Program.cs` **註冊服務並安排 Middleware 順序**。`AddAuthorization()` 與 `UseAuthentication()`、`UseAuthorization()` 仍留在入口，因此可以清楚看到服務如何組裝，以及每個請求何時經過身分驗證和授權。

讀程式碼時，先找 `Program.cs` 的 `AddJwtAuthentication()`，再進到 Extension Method 看金鑰檢查、驗證參數和兩個事件，最後對照 `Services/TokenService.cs` 的簽發設定。這能幫你區分「產生 JWT」和「驗證 JWT」是兩個方向。
