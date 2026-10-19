# 第七課：Entity Framework Core 與 PostgreSQL

這一課將 Todo 從記憶體 `List` 搬到 PostgreSQL。資料寫入 Docker Volume，因此網站容器重啟後仍然存在。

## 請求流程

```text
Postman
   ↓ HTTP Request
TodosController
   ↓ await TodoService
TodoService
   ↓ Entity Framework Core
AppDbContext
   ↓ Npgsql Provider / SQL
PostgreSQL
```

## Docker Compose 的 db 服務

`compose.yaml` 新增 PostgreSQL：

```yaml
db:
  image: postgres:18-alpine
```

網站使用 Compose 服務名稱 `db` 連線，不使用 `localhost`。因為對 `app` 容器來說，`localhost` 代表 `app` 自己。

PostgreSQL 18 的資料 Volume 掛載在：

```yaml
- postgres_data:/var/lib/postgresql
```

PostgreSQL 18 官方映像改用 major version 子目錄，因此不能沿用舊版的 `/var/lib/postgresql/data` 掛載方式。

## 連線字串

Compose 使用環境變數提供設定：

```yaml
ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=aspnetlearning;Username=aspnetlearning;Password=aspnetlearning
```

雙底線 `__` 對應 ASP.NET Core 設定中的冒號，因此程式使用：

```csharp
builder.Configuration.GetConnectionString("DefaultConnection")
```

本機預設密碼只供學習環境使用，資料庫連接埠只綁定 Mac 的 `127.0.0.1`，不會開放給區域網路。正式環境必須透過部署平台的 Secret 設定真正密碼。

## 使用 DataGrip 連線

Compose 將 PostgreSQL 綁定在 Mac 本機的 `9181`，而且只接受這台電腦的連線：

```text
Host: localhost
Port: 9181
Database: aspnetlearning
User: aspnetlearning
Password: aspnetlearning
```

JDBC URL 是 `jdbc:postgresql://localhost:9181/aspnetlearning`。DataGrip 的 SSL 設為 Disable，即可查看 `todo_items` 與 `__EFMigrationsHistory`。

## Entity Framework Core 套件

| 套件 | 用途 |
|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 讓 EF Core 連接 PostgreSQL |
| `Microsoft.EntityFrameworkCore.Design` | 建立及管理 Migration |
| `dotnet-ef` | 執行 Migration 指令的本機工具 |

`dotnet-tools.json` 固定 `dotnet-ef` 版本，其他開發者執行 `dotnet tool restore` 就能取得同一版本。

## AppDbContext

`Data/AppDbContext.cs` 繼承 `DbContext`：

```csharp
public sealed class AppDbContext : DbContext
```

`DbSet<TodoItem>` 代表可以查詢及修改 Todo 資料表：

```csharp
public DbSet<TodoItem> TodoItems => Set<TodoItem>();
```

`OnModelCreating` 設定：

- 資料表名稱是 `todo_items`。
- `Id` 是 Primary Key，新增時由 PostgreSQL 自動產生。
- `Title` 必填且最多 100 個字元。
- Migration 初次建立資料表時加入兩筆學習資料。

## 註冊生命週期

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

builder.Services.AddScoped<TodoService>();
```

`AddDbContext` 預設是 Scoped。每個 HTTP Request 會得到一個 `AppDbContext`，同一個 Request 內的 `TodoService` 使用相同物件。

DbContext 不適合 Singleton，因為它會追蹤資料變更，也不是設計給多個請求同時使用。

`EnableRetryOnFailure()` 讓 EF Core 在 PostgreSQL 短暫斷線時自動重試，例如資料庫容器重新啟動。它只能處理短暫錯誤，錯誤密碼或不存在的資料庫仍會正常失敗。

## 非同步資料庫操作

Service 方法改成 `async` 並回傳 `Task<T>`：

```csharp
public async Task<List<TodoItem>> GetAllAsync()
```

查詢使用：

```csharp
await _dbContext.TodoItems
    .AsNoTracking()
    .OrderBy(todoItem => todoItem.Id)
    .ToListAsync();
```

- `AsNoTracking()`：只讀資料，不需要 EF Core 追蹤變更。
- `OrderBy(...)`：讓 PostgreSQL 依照 Id 排序。
- `ToListAsync()`：非同步執行 SQL 並建立 List。
- `await`：等待資料庫時將 Thread 還給伺服器處理其他請求。

新增、更新與刪除最後都要呼叫：

```csharp
await _dbContext.SaveChangesAsync();
```

沒有呼叫 `SaveChangesAsync()`，EF Core 只會追蹤記憶體中的狀態，不會把 SQL 寫入資料庫。

## Migration

Migration 是資料庫結構的版本紀錄。這次的 `InitialCreate` 包含：

- 建立 `todo_items` 資料表。
- 建立 Primary Key。
- 設定 Title 長度。
- 寫入兩筆 Seed Data。

專案啟動時呼叫：

```csharp
dbContext.Database.Migrate();
```

它只會套用尚未執行的 Migration。這適合目前的單一學習容器；正式環境通常在部署流程中獨立執行 Migration。

## 查看資料庫中的 Todo

在專案根目錄執行：

```bash
docker compose exec db psql -U aspnetlearning -d aspnetlearning -c 'SELECT * FROM todo_items ORDER BY "Id";'
```

## 驗證資料持久化

先透過 Postman 建立一筆 Todo，再只重啟網站：

```bash
docker compose restart app
```

重新呼叫 `GET /todos`，剛新增的資料仍存在，表示資料已保存在 PostgreSQL Volume。

`docker compose down` 只刪除容器，會保留 Volume。只有使用 `docker compose down --volumes` 才會連資料一起刪除。
