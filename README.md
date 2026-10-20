# AspNetLearning

這是一個使用 ASP.NET Core 與 Docker 建立的學習專案。開發環境執行於 .NET 10 SDK 容器，因此 macOS 不需要另外安裝 .NET SDK。

## 專案架構

```text
AspNetLearning/
├── .devcontainer/
│   └── devcontainer.json
├── Program.cs
├── AspNetLearning.csproj
├── appsettings.json
├── appsettings.Development.json
├── compose.yaml
├── Contracts/
│   ├── CreateTodoRequest.cs
│   ├── LoginRequest.cs
│   ├── LoginResponse.cs
│   ├── RegisterRequest.cs
│   └── UpdateTodoRequest.cs
├── Controllers/
│   ├── AuthController.cs
│   └── TodosController.cs
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── Endpoints/
│   └── HelloEndpoints.cs
├── Extensions/
│   └── ClaimsPrincipalExtensions.cs
├── frontend/
│   ├── src/
│   │   ├── components/CurrentUserPanel.vue
│   │   ├── components/ErrorHandlingPanel.vue
│   │   ├── components/LoginForm.vue
│   │   ├── components/RegisterForm.vue
│   │   ├── components/TodoListItem.vue
│   │   ├── services/apiClient.ts
│   │   ├── services/authApi.ts
│   │   ├── services/todoApi.ts
│   │   ├── types/todo.ts
│   │   ├── types/user.ts
│   │   ├── App.vue
│   │   ├── main.ts
│   │   └── style.css
│   ├── package.json
│   └── vite.config.ts
├── Models/
│   ├── TodoItem.cs
│   └── User.cs
├── Services/
│   ├── TokenService.cs
│   ├── TodoService.cs
│   └── UserService.cs
├── docs/
│   ├── 01-hello-api.md
│   ├── 02-todo-list.md
│   ├── 03-create-todo.md
│   ├── 04-update-delete-todo.md
│   ├── 05-controller.md
│   ├── 06-service-dependency-injection.md
│   ├── 07-postgresql-ef-core.md
│   ├── 08-vue-typescript-frontend.md
│   ├── 09-member-registration.md
│   ├── 10-member-login-jwt.md
│   ├── 11-current-user-authorization.md
│   ├── 12-user-owned-todos.md
│   ├── 13-logout-auth-state.md
│   ├── 14-request-validation.md
│   ├── 15-global-error-handling.md
│   ├── 16-dashboard-navigation.md
│   └── development-roadmap.md
├── postman/
│   ├── AspNetLearning.postman_collection.json
│   └── environments/
│       ├── AspNetLearning.local.postman_environment.json
│       └── AspNetLearning.production.postman_environment.json
├── Properties/
│   └── launchSettings.json
├── dotnet-tools.json
├── bin/
├── obj/
└── .gitignore
```

### `.devcontainer/devcontainer.json`

讓 Cursor 進入現有的 .NET 10 SDK 容器開發。C# 語言伺服器會在容器內取得 SDK 與 MSBuild，因此 macOS 不需要另外安裝 .NET SDK。

在 Cursor 的 Command Palette 執行 `Dev Containers: Reopen in Container` 即可進入容器。

容器中的網站已由 `dotnet watch` 執行。需要除錯時按下 F5，選擇 `.NET Core Attach (Docker)`，再從程序清單選擇 `AspNetLearning`；這會附加至既有網站，不會重複啟動並占用 `8080`。

### `Program.cs`

ASP.NET Core 應用程式的進入點，負責建立、設定並啟動網站。

```csharp
// 匯入問候 API 所在的命名空間。
using AspNetLearning.Endpoints;

// 建立 WebApplicationBuilder，載入設定並準備註冊服務。
var builder = WebApplication.CreateBuilder(args);

// 根據 builder 中的設定建立 WebApplication。
var app = builder.Build();

// 註冊 GET / 路由；收到請求時回傳 Hello World!。
app.MapGet("/", () => "Hello World!");

// 載入第一階段的問候 API 路由。
app.MapHelloEndpoints();

// 啟動網站並開始接收 HTTP 請求。
app.Run();
```

### `Endpoints/HelloEndpoints.cs`

集中放置第一階段的問候 API。`Program.cs` 只負責啟動應用程式及載入端點，實際的路由與處理邏輯放在此檔案。

### `AspNetLearning.csproj`

C# 專案設定檔，指定使用 ASP.NET Core Web SDK、.NET 版本及編譯選項。

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <!-- 指定專案使用 .NET 10。 -->
    <TargetFramework>net10.0</TargetFramework>

    <!-- 啟用 Null 安全檢查。 -->
    <Nullable>enable</Nullable>

    <!-- 自動加入常用命名空間。 -->
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

### `appsettings.json`

存放所有執行環境共用的應用程式設定，例如 Log 層級與允許的 Host。

一般設定可以放在這裡；密碼、Token 及正式環境的資料庫密碼不應提交到 GitHub。

### `appsettings.Development.json`

存放開發環境專用設定。當 `ASPNETCORE_ENVIRONMENT` 是 `Development` 時，此檔案會覆蓋 `appsettings.json` 中的同名設定。

### `compose.yaml`

定義 Docker 開發環境，包括 .NET SDK 映像、原始碼掛載、啟動命令及連接埠。

```yaml
services:
  app:
    # 使用微軟官方 .NET 10 SDK 映像。
    image: mcr.microsoft.com/dotnet/sdk:10.0

    # 將容器的工作目錄設定為 /app。
    working_dir: /app

    # 監看程式碼變更並自動重新載入。
    command: dotnet watch run --non-interactive --no-launch-profile

    # 將 Mac 的 8180 對應至容器的 8080。
    ports:
      - "8180:8080"

    # 將目前專案目錄掛載到容器的 /app。
    volumes:
      - .:/app

    environment:
      # 使用 ASP.NET Core 開發環境設定。
      ASPNETCORE_ENVIRONMENT: Development

      # 監聽容器內所有網路介面的 8080。
      ASPNETCORE_URLS: http://0.0.0.0:8080

      # 使用輪詢偵測 macOS 掛載目錄中的檔案變更。
      DOTNET_USE_POLLING_FILE_WATCHER: "1"
```

### `Properties/launchSettings.json`

記錄本機 `dotnet run` 與編輯器除錯時使用的啟動設定，例如網址、連接埠及環境變數。

Docker 啟動命令使用 `--no-launch-profile`，因此容器不會載入此檔案，避免本機設定覆蓋容器的監聽網址。

### `bin/`

存放編譯完成的程式，例如 `AspNetLearning.dll`。此目錄由 .NET 自動產生，不需要手動修改或提交到 GitHub。

### `obj/`

存放編譯過程中的暫存檔、套件還原資訊及中間產物。此目錄由 .NET 自動產生，不需要手動修改或提交到 GitHub。

### `.gitignore`

告訴 Git 忽略不需要提交的檔案與目錄，例如 `bin/`、`obj/` 及編輯器暫存檔。

## 前端與 API 請求流程

```text
Chrome 開啟 http://localhost:8181
    ↓
Vue 3 + TypeScript 顯示待辦清單
    ↓
Vite 將 /todos 轉送至 app:8080
    ↓
ASP.NET Core Controller → Service → EF Core
    ↓
PostgreSQL 讀寫資料
    ↓
JSON 回到 Vue，Vue 更新畫面
```

## 啟動專案

在專案根目錄執行：

```sh
docker compose up -d
```

開啟 Vue 前端：

```text
http://localhost:8181
```

後端 API 仍可單獨從以下網址存取：

```text
http://localhost:8180
```

第八階段的 Vue 與 TypeScript 說明請閱讀 `docs/08-vue-typescript-frontend.md`。

後續功能規劃請閱讀 `docs/development-roadmap.md`；下一階段預計建立會員系統。

## 查看執行紀錄

```sh
docker compose logs -f app
```

## 停止專案

```sh
docker compose down
```

## Git 分支流程

```text
feat/* → dev → main
```

- 功能與文件修改從 `dev` 建立 `feat/*` 分支。
- 功能分支透過 Pull Request 合併至 `dev`。
- 測試完成後，再透過 Pull Request 將 `dev` 合併至 `main`。

## 學習課程

1. [第一課：讀懂問候 API](docs/01-hello-api.md)
2. [第二課：讀懂 Class 與 List](docs/02-todo-list.md)
3. [第三課：使用 POST 建立待辦事項](docs/03-create-todo.md)
4. [第四課：使用 PUT 與 DELETE](docs/04-update-delete-todo.md)
5. [第五課：將 Todo API 搬入 Controller](docs/05-controller.md)
6. [第六課：Service 與 Dependency Injection](docs/06-service-dependency-injection.md)
7. [第七課：Entity Framework Core 與 PostgreSQL](docs/07-postgresql-ef-core.md)

## Postman Collection

將以下檔案匯入 Postman：

```text
postman/AspNetLearning.postman_collection.json
postman/environments/AspNetLearning.local.postman_environment.json
postman/environments/AspNetLearning.production.postman_environment.json
```

開發時選擇 `AspNetLearning - Local`；正式上線後，將正式 API 網址填入 `AspNetLearning - Production` 的 `baseUrl`，再切換環境即可使用同一組 Request。

Local 環境預設使用：

```text
baseUrl = http://localhost:8180
name = Andy
```

Production 範本的 `baseUrl` 刻意留空，避免尚未部署時誤送請求。密碼、Token 與其他機密資料不可提交到 GitHub，應在 Postman 中設定為敏感值。
