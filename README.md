# AspNetLearning

這是一個使用 ASP.NET Core 與 Docker 建立的學習專案。開發環境執行於 .NET 10 SDK 容器，因此 macOS 不需要另外安裝 .NET SDK。

## 專案架構

```text
AspNetLearning/
├── Program.cs
├── AspNetLearning.csproj
├── appsettings.json
├── appsettings.Development.json
├── compose.yaml
├── docs/
│   └── 01-hello-api.md
├── postman/
│   └── AspNetLearning.postman_collection.json
├── Properties/
│   └── launchSettings.json
├── bin/
├── obj/
└── .gitignore
```

### `Program.cs`

ASP.NET Core 應用程式的進入點，負責建立、設定並啟動網站。

```csharp
// 建立 WebApplicationBuilder，載入設定並準備註冊服務。
var builder = WebApplication.CreateBuilder(args);

// 根據 builder 中的設定建立 WebApplication。
var app = builder.Build();

// 註冊 GET / 路由；收到請求時回傳 Hello World!。
app.MapGet("/", () => "Hello World!");

// 啟動網站並開始接收 HTTP 請求。
app.Run();
```

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

## HTTP 請求流程

```text
Chrome 開啟 http://localhost:8180
    ↓
Docker 將 Mac 的 8180 轉送至容器的 8080
    ↓
ASP.NET Core 收到 GET /
    ↓
Program.cs 中的 MapGet("/") 處理請求
    ↓
Lambda 運算式回傳 Hello World!
    ↓
Chrome 顯示 Hello World!
```

## 啟動專案

在專案根目錄執行：

```sh
docker compose up -d
```

開啟網站：

```text
http://localhost:8180
```

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

## Postman Collection

將 `postman/AspNetLearning.postman_collection.json` 匯入 Postman，即可呼叫目前的首頁與問候 API。

Collection 預設使用：

```text
baseUrl = http://localhost:8180
name = Andy
```
