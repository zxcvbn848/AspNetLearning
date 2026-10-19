// 匯入 AppDbContext 所在的命名空間。
using AspNetLearning.Data;

// 匯入問候 API 所在的命名空間，讓這個檔案可以使用 MapHelloEndpoints。
using AspNetLearning.Endpoints;

// 匯入 TodoService 所在的命名空間，讓 Program.cs 可以註冊這個服務。
using AspNetLearning.Services;

// 匯入 User 與 ASP.NET Core 內建的密碼雜湊服務。
using AspNetLearning.Models;
using Microsoft.AspNetCore.Identity;

// 匯入 JWT 驗證、簽章金鑰與文字編碼相關類別。
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// 匯入 Entity Framework Core，讓程式可以使用 UseNpgsql 與 Migrate。
using Microsoft.EntityFrameworkCore;

// 建立 WebApplicationBuilder，負責載入設定、註冊服務及準備 ASP.NET Core 應用程式。
// args 代表啟動程式時傳入的命令列參數。
var builder = WebApplication.CreateBuilder(args);

// 將 Controller 相關服務註冊到 ASP.NET Core 的 Dependency Injection 容器。
// AddControllers 讓應用程式能辨識繼承 ControllerBase 並標有路由 Attribute 的類別。
builder.Services.AddControllers();

// JWT 簽章金鑰至少需要 32 bytes，避免使用過短金鑰降低 HMAC-SHA256 安全性。
string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("找不到 Jwt:Key 設定。");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key 至少需要 32 bytes。");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 保留 JWT 原始 Claim 名稱，讓 Controller 可以直接讀取標準 sub Claim。
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// AddAuthorization 提供下一階段 [Authorize] 保護 API 所需的授權服務。
builder.Services.AddAuthorization();

// 從設定讀取名稱為 DefaultConnection 的 PostgreSQL 連線字串。
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("找不到 ConnectionStrings:DefaultConnection 設定。");

// AppDbContext 預設使用 Scoped，每個 HTTP Request 共用一個資料庫工作階段。
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        // PostgreSQL 短暫斷線時由 EF Core 自動重試，例如資料庫容器重新啟動。
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

// TodoService 與同一個 HTTP Request 的 AppDbContext 共用 Scoped 生命週期。
builder.Services.AddScoped<TodoService>();

// 每個 HTTP Request 建立一個 UserService，並使用 ASP.NET Core 內建 PasswordHasher。
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<TokenService>();

// 根據 builder 中的設定與服務，建立可以處理 HTTP 請求的 WebApplication。
var app = builder.Build();

// shortcut: 本機學習環境啟動時套用 Migration，正式多副本部署改由獨立部署步驟執行。
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// 註冊 HTTP GET 路由。
// "/" 代表網站根路徑，() => "Hello World!" 是收到請求後執行的 Lambda 運算式。
app.MapGet("/", () => "Hello World!");

// 載入第一階段的問候 API 路由。
app.MapHelloEndpoints();

// 驗證 Authorization Header 中的 JWT，再執行端點設定的授權規則。
app.UseAuthentication();
app.UseAuthorization();

// 尋找 Controller 上的 Route、HttpGet、HttpPost、HttpPut 與 HttpDelete 等 Attribute，並建立對應路由。
app.MapControllers();

// 啟動應用程式，開始監聽並處理 HTTP 請求。
app.Run();
