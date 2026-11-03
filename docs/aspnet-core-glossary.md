# ASP.NET Core 名詞與概念速查

這份文件以本專案已經使用的概念為主，適合讀程式時查閱。PHP／Laravel、Go 的對照是用途相近的理解橋梁，生命週期、執行方式與 API 不一定相同。

快速跳轉：[平台與語法](#平台與語法)｜[請求與分層](#請求與分層)｜[依賴注入](#依賴注入)｜[資料庫與查詢](#資料庫與查詢)｜[非同步](#非同步)｜[會員與安全](#會員與安全)｜[即時通訊](#即時通訊)｜[開發工具](#開發工具)｜[常見誤會](#常見誤會)

## 平台與語法

| 名詞 | 白話解釋 | 熟悉語言的對照 |
|---|---|---|
| C# | 本專案後端使用的程式語言。 | PHP、Go 都是語言。 |
| .NET | 執行環境、標準函式庫與開發工具的整體平台。 | 可分別聯想 PHP runtime 或 Go runtime／工具鏈，涵蓋範圍不同。 |
| ASP.NET Core | 在 .NET 上建立 Web API、網站與即時服務的框架。 | Web 框架角色接近 Laravel、Gin；功能範圍不同。 |
| SDK／Runtime | SDK 用來建置與開發；Runtime 用來執行。SDK 包含開發所需的執行環境。 | Go 工具鏈負責建置；Go 的執行支援通常包含在產生的執行檔中。 |
| NuGet／`.csproj` | NuGet 管理 .NET 套件；`.csproj` 宣告目標框架、套件與建置設定。 | Composer／`composer.json`、Go modules／`go.mod`，設定範圍不同。 |
| Namespace／`using` | Namespace 組織型別；一般 `using` 宣告讓你免寫完整型別名稱。 | PHP namespace／use、Go package／import，載入與可見性規則不同。 |
| Class／Object | Class 定義資料與行為；Object 是依定義建立的實例。 | PHP class；Go 常用 struct 搭配方法組織資料與行為。 |
| Property | 物件對外提供的屬性，例如 `user.Nickname`。 | PHP property、Go struct field；C# property 可在存取時執行邏輯。 |
| `get`／`set`／`init` | 讀取／修改／僅允許在初始化階段設定屬性。 | Go field 沒有內建同等的存取器語法。 |
| `required` | C# 建立物件時要求指定該成員；JSON 反序列化也可能據此要求欄位存在。 | 不等同 Laravel 的完整 `required` 輸入驗證，仍須驗證空白與長度。 |
| `var` | 編譯器從右側推斷靜態型別，不代表型別可任意變換。 | Go `:=` 類似；不同於 PHP 變數可持有不同型別。 |
| `string?`／`?.`／`??` | 可為 null 的參考型別註記／非 null 才存取／null 時提供替代值。 | PHP `?string`、`?->`、`??`；Go 常用指標、零值或明確的狀態表示缺值。 |
| Lambda／`=>` | 簡短的函式寫法，例如 `user => user.Id == 2`。 | PHP `fn ($user) => ...`、Go 匿名函式。 |
| Generic／泛型 | 用型別參數重用程式，例如 `List<User>` 是會員清單。 | Go 的泛型；PHPDoc 泛型註記與 C# 的編譯器型別檢查不同。 |
| `sealed`／`static`／`readonly` | 禁止繼承類別／成員屬於型別本身／欄位初始化後不可重新賦值。 | PHP `final`／`static` 有相近角色；`readonly` 不表示參考物件內部不能改變。 |
| Record／DTO | Record 是 C# 的資料型別寫法；DTO 是用來傳遞資料的設計角色。 | Go response struct、PHP DTO；DTO 也可以用一般 class 定義。 |
| Extension method | 替既有型別提供像實例方法的呼叫方式，例如 `app.MapHelloEndpoints()`。 | PHP／Go 沒有完全相同的標準語法；可聯想接收物件參數的輔助函式。 |

本專案範例：[User](../Models/User.cs)、[ChatMessageResponse](../Contracts/ChatMessageResponse.cs)、[HelloEndpoints](../Endpoints/HelloEndpoints.cs)。

## 請求與分層

| 名詞 | 白話解釋 | PHP／Laravel、Go 對照 |
|---|---|---|
| Request／Response | Client 傳入的請求／Server 回傳的結果，包含 Header、狀態碼與可能的 Body。 | Laravel Request／Response；Go `http.Request`／`http.ResponseWriter`。 |
| Route／Endpoint | Route 描述 URL 與 HTTP 方法如何配對；Endpoint 是選中後執行的處理入口。 | Laravel route；Gin 路由與 handler。 |
| Controller／Action | Controller 接收 HTTP 請求；Action 是其中處理特定請求的方法。 | Laravel Controller method；Go 常用 handler function／method。 |
| Minimal API | 直接用 `MapGet` 等方法定義處理入口的 ASP.NET Core 寫法。 | 類似用 Laravel closure route 或 Gin handler 直接處理。 |
| Service | 放業務與資料操作流程的類別，避免 Controller 包辦所有事情。 | Laravel 自訂 Service、Go service struct；屬於分層慣例。 |
| Repository | 集中資料存取操作的設計角色。本專案目前由 Service 直接使用 EF Core，沒有另建此層。 | PHP／Go 都能採用，但使用框架不代表必須加入。 |
| Entity／Contract | Entity 描述持久化資料；Contract 定義請求或回應格式。 | Eloquent Model 與 Form Request／API Resource 的職責有部分對應；Go 常分 DB struct 和 DTO。 |
| Model binding／Validation | 將路由、Query、JSON 等輸入轉成參數或物件／確認內容符合規則。 | Laravel Request 驗證；Gin binding 與 validator。 |
| Attribute／Data Annotations | `[Authorize]`、`[Required]` 等是附加在型別或成員上的中繼資料，由框架解讀。 | PHP attributes 有近似語法；Go struct tags 用途部分相近。 |
| Middleware | 在請求處理管線中執行的步驟，可檢查、短路請求，也可處理後續回應。 | Laravel Middleware、Gin middleware；順序會影響行為。 |
| Problem Details／Trace Id | 結構化錯誤格式／用來對照同一次請求 Log 的識別值。 | Laravel／Go 可建立相同用途的錯誤回應與 request ID。 |

HTTP 狀態碼速記：`200` 成功、`201` 已建立、`204` 成功且沒有 Body、`400` 輸入有問題、`401` 缺少有效認證、`403` 已認證但權限不足、`404` 找不到資源、`409` 狀態衝突、`500` 伺服器未預期錯誤。本專案對其他會員的 Todo 回 `404`，避免揭露該資源是否存在。

典型流程：`HTTP Request → Middleware → Controller → Service → DbContext → PostgreSQL → HTTP Response`。這是本專案的閱讀路線；Minimal API 與 Hub 有各自的入口。

本專案範例：[TodosController](../Controllers/TodosController.cs)、[TodoService](../Services/TodoService.cs)、[CreateTodoRequest](../Contracts/CreateTodoRequest.cs)。

## 依賴注入

**DI（Dependency Injection）就是把物件需要的服務交給它，而不是讓它自己建立所有依賴。** `TodoService(AppDbContext dbContext)` 的參數由 .NET 容器提供。Laravel Service Container 也支援建構子注入；Go 常以建構函式明確傳入依賴。

| 生命週期 | 意思 | 本專案閱讀提醒 |
|---|---|---|
| Transient | 每次解析時建立實例。 | 不代表每個請求只建立一次。 |
| Scoped | 同一個 scope 共用實例。 | HTTP 通常以一次請求為 scope；不要推論所有背景工作或 Hub 都等同 HTTP 請求。 |
| Singleton | 同一個容器共用一個實例。 | 可能被多個請求同時使用，須注意共享狀態。 |

`AddScoped<TodoService>()` 是註冊規則；建構子參數是取得依賴的位置。`DbContext` 不是可供多個執行緒同時操作的共享資料庫連線池，不能用同一實例平行查詢。

本專案範例：[Program.cs](../Program.cs)、[JWT 註冊](../Authentication/JwtAuthenticationExtensions.cs)。

## 資料庫與查詢

| 名詞 | 白話解釋 | PHP／Laravel、Go 對照 |
|---|---|---|
| ORM | 將程式物件與關聯式資料庫資料對應，協助查詢和保存。 | EF Core、Eloquent、Go 社群的 GORM 都是 ORM。Go 標準庫 `database/sql` 不是 ORM。 |
| EF Core | 本專案的 ORM，產生 SQL、對應資料並追蹤變更。 | 與 Eloquent 角色相近；Eloquent 模型可自行 `save()`，本專案由 DbContext 保存變更。 |
| DbContext／DbSet | 一次資料操作工作階段／某類 Entity 的查詢與操作入口。 | 不直接等同 Go `sql.DB`：`sql.DB` 管理連線池，適合共享。 |
| LINQ | C# 整合的查詢能力，可篩選、排序、投影、JOIN 等。 | 查記憶體時近似 Laravel Collection、JS `filter/map`；透過 EF Core 查 DB 時用途近似 Query Builder。 |
| `IEnumerable<T>`／`IQueryable<T>` | 可列舉資料的抽象／可讓查詢提供者解讀查詢運算式的抽象。 | EF Core 的 IQueryable 可轉 SQL；不是所有 IQueryable 都代表資料庫。 |
| 延遲執行 | 組好查詢時通常還沒真正讀資料；列舉或呼叫執行方法時才執行。 | 可聯想 Laravel Query Builder 在 `get()` 前持續組查詢；一般 Collection 不宜直接套用此規則。 |
| Projection／`Select` | 選出要的欄位或轉成另一種資料結構。 | SQL SELECT、Collection `map/pluck`、Go 迴圈組 response struct，依查詢位置而不同。 |
| Change tracking | DbContext 記住 Entity 狀態，以判斷要新增、修改或刪除什麼。 | Eloquent 有變更判斷，但保存單位與行為不同。 |
| `AsNoTracking()` | 查詢出的 Entity 不加入這次 DbContext 的變更追蹤。 | 不代表鎖定、快取或永久唯讀；適合不需修改 Entity 的查詢。 |
| `Add()`／`SaveChangesAsync()` | 將 Entity 標記為待新增／將追蹤到的變更送至 DB。 | `Add()` 本身不代表已 INSERT；Eloquent `save()` 或 Go Exec 的執行時機不能直接套用。 |
| Migration | 把資料表結構變更記錄成可套用的版本。 | Laravel migration；Go 需自行選擇 migration 工具。不是資料庫備份。 |
| PK／FK／Index | 唯一識別資料／限制資料關聯／幫助查詢定位資料的結構。 | 各語言都受同一套資料庫規則約束；Unique Index 還能阻止重複值。 |

LINQ 方法速記：`Where` 篩選、`Select` 選取／轉換、`OrderByDescending` 降冪排序、`Take` 限制筆數、`ToListAsync` 非同步執行並取得清單、`SingleOrDefaultAsync` 要求最多一筆（零筆回預設值，多筆拋例外）。

```csharp
// 在資料庫查詢階段篩選，ToListAsync 才執行查詢並建立結果清單。
var todos = await dbContext.TodoItems
    .AsNoTracking()
    .Where(todo => todo.UserId == userId)
    .ToListAsync();
```

本專案範例：[AppDbContext](../Data/AppDbContext.cs)、[TodoService](../Services/TodoService.cs)、[聊天室 JOIN](../Hubs/ChatHub.cs)。聊天室直接投影為不包含 Entity 的 DTO，因此本身沒有 Entity 需要追蹤；此處的 `AsNoTracking()` 主要表達只讀意圖。

## 非同步

| 名詞 | 白話解釋 | 跨語言對照與限制 |
|---|---|---|
| Process／Thread | 作業系統程序／程序中的執行緒。 | .NET、PHP、Go 都有程序；Go 的 goroutine 是 runtime 排程的工作單位，不等於一條 OS thread。 |
| `Task`／`Task<T>` | 代表非同步操作的完成狀態／也包含最後結果 T。 | 可類比 JS Promise；不代表一定建立 thread，也不等於 Go goroutine。 |
| `async`／`await` | 允許方法非同步等待／等待結果後繼續。 | 真正非同步 DB I/O 尚未完成時，可讓 thread 處理其他工作；沒有釋放 process，也不保證換 thread。 |
| 非同步／平行 | 非同步讓等待期間能讓出執行資源；平行是多個工作同時執行。 | 加 `async` 不會自動讓所有程式平行，也不會讓 CPU 計算自動變快。 |

PHP 傳統 PHP-FPM 的一般同步 DB 呼叫通常會讓該 worker 等待；PHP 的其他 runtime／非同步函式庫另有機制。Go 的 DB 呼叫外觀可以是同步的，等待時 goroutine 通常可被停放，讓 runtime 排程其他 goroutine；實際仍取決於驅動與操作。這些機制不能只靠有沒有 `await` 判斷。

## 會員與安全

| 名詞 | 白話解釋 | 對照與本專案提醒 |
|---|---|---|
| Authentication／Authorization | 確認你是誰／確認你能做什麼。 | Laravel Auth／Policy、Go 認證 middleware／權限判斷有相近角色。 |
| JWT／Access Token | JWT 是 Token 格式；本專案用簽章 JWT 作為短效 API 存取憑證。 | JWT 簽章不代表內容加密，不能把密碼放進去。 |
| Claim／ClaimsPrincipal | 登入身分中的一項資訊／目前使用者的身分集合。 | 本專案從 `sub` 取得會員 Id，不能相信 Client 自報的作者 Id。 |
| Bearer／`[Authorize]` | 用持有的 Token 存取／要求入口滿足認證與可能的授權規則。 | `[Authorize]` 不會自動保證某筆 Todo 屬於目前會員，仍須查 UserId。 |
| Refresh Token | 用於換發 Access Token 的工作階段憑證。 | 本專案用 HttpOnly Cookie 傳遞、DB 保存雜湊，更新時輪替。不是 Laravel CSRF Token。 |
| Cookie／HttpOnly | 瀏覽器保存並依規則附帶的資料／禁止頁面 JS 直接讀取 Cookie。 | HttpOnly 不等於完整防範所有 XSS 或 CSRF；Cookie 還有 Secure、SameSite 等規則。 |
| Hash／Encryption | 雜湊用於比對，通常不可逆／加密可用金鑰解密。 | 密碼使用 PasswordHasher；JWT 簽章、密碼雜湊與加密是不同用途。 |

本專案範例：[AuthController](../Controllers/AuthController.cs)、[TokenService](../Services/TokenService.cs)、[RefreshTokenService](../Services/RefreshTokenService.cs)。

## 即時通訊

| 名詞 | 白話解釋 | 對照與本專案提醒 |
|---|---|---|
| WebSocket | 持續開啟的雙向傳輸通道。 | PHP／Go 也能使用；傳輸通道本身不會自動保存資料或更新 Vue。 |
| SignalR／Hub | 管理即時連線與訊息的 ASP.NET Core 函式庫／供 Client 與 Server 互相呼叫方法的入口。 | Laravel Broadcasting／Echo 的用途部分相近，但協定與功能不相同。Go 可用 WebSocket 函式庫自行組織。 |
| Group／ConnectionId | 伺服器管理的一群連線／一條連線的識別碼。 | 同一會員能有多條連線；Group 名稱本身不提供權限檢查。 |
| `OnConnectedAsync()` | SignalR 建立連線後呼叫的生命週期方法。 | TodoHub 加入會員群組；ChatHub 加入公開 lobby。 |
| `invoke`／`on`／`SendAsync` | Client 呼叫 Hub／Client 註冊接收事件／Server 傳送事件。 | SignalR 在 WebSocket 等傳輸之上有自己的訊息格式，不是 Socket.IO 協定。 |
| Reconnect | 中斷後重新建立連線。 | 本專案聊天室重連後補讀歷史；自動重連本身不保證補回所有離線訊息。 |

圖解見 [WebSocket 與 SignalR 流程圖](websocket-signalr-flow.md)，程式見 [ChatHub](../Hubs/ChatHub.cs)。

## 開發工具

| 名詞 | 快速記法 |
|---|---|
| Configuration／Environment | 設定值與執行環境；`Development` 是環境名稱。ASP.NET Core 環境變數 `Jwt__Key` 對應設定路徑 `Jwt:Key`。 |
| Docker image／Container／Volume | 建立容器的範本／執行中的隔離環境／獨立保存資料的儲存空間。Volume 不是備份。 |
| Docker Compose／Dev Container | 管理 app、db、frontend 等服務／讓編輯器進入容器開發。 |
| OpenAPI／Scalar／Postman | API 規格／瀏覽與互動操作規格的 UI／組織並發送測試請求的工具。 |
| Unit test／Integration test／Smoke test | 測一小段邏輯／測多個元件的互動／快速確認重要流程可運作；分類可重疊。 |
| CI／PR | 自動建置與測試／供檢閱與合併變更的請求。CI 通過只涵蓋現有檢查。 |

## 常見誤會

- LINQ 是查詢能力；EF Core 才是本專案的 ORM。
- Entity 對應持久化資料；DTO 決定公開格式。使用 DTO 有助避免回傳密碼雜湊等內部欄位。
- `required`、`[Required]` 與 DB 的 `NOT NULL` 分別作用在不同階段，不能互相取代所有驗證。
- `AsNoTracking()` 不會禁止你修改物件；它改變 EF Core 是否追蹤 Entity。
- `await` 不會釋放 process；Task 也不等於 thread。
- 接受有效 JWT 與驗證資源擁有權是兩個步驟。

## 官方延伸閱讀

- [Microsoft：LINQ](https://learn.microsoft.com/en-us/dotnet/csharp/linq/)、[EF Core 變更追蹤](https://learn.microsoft.com/en-us/ef/core/querying/tracking)、[ASP.NET Core DI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0)
- [Laravel Collections](https://laravel.com/docs/12.x/collections)、[Eloquent ORM](https://laravel.com/framework/docs/eloquent)、[Service Container](https://laravel.com/framework/docs/container)
- [Go 資料庫查詢](https://go.dev/doc/database/querying)、[Go 資料庫連線管理](https://go.dev/doc/database/manage-connections)
