// 匯入問候 API 所在的命名空間，讓這個檔案可以使用 MapHelloEndpoints。
using AspNetLearning.Endpoints;

// 匯入 TodoService 所在的命名空間，讓 Program.cs 可以註冊這個服務。
using AspNetLearning.Services;

// 建立 WebApplicationBuilder，負責載入設定、註冊服務及準備 ASP.NET Core 應用程式。
// args 代表啟動程式時傳入的命令列參數。
var builder = WebApplication.CreateBuilder(args);

// 將 Controller 相關服務註冊到 ASP.NET Core 的 Dependency Injection 容器。
// AddControllers 讓應用程式能辨識繼承 ControllerBase 並標有路由 Attribute 的類別。
builder.Services.AddControllers();

// 將 TodoService 註冊成 Singleton，整個應用程式執行期間只建立一個物件。
// 目前資料存在 Service 的記憶體 List，因此所有 HTTP 請求必須共用同一個 TodoService。
builder.Services.AddSingleton<TodoService>();

// 根據 builder 中的設定與服務，建立可以處理 HTTP 請求的 WebApplication。
var app = builder.Build();

// 註冊 HTTP GET 路由。
// "/" 代表網站根路徑，() => "Hello World!" 是收到請求後執行的 Lambda 運算式。
app.MapGet("/", () => "Hello World!");

// 載入第一階段的問候 API 路由。
app.MapHelloEndpoints();

// 尋找 Controller 上的 Route、HttpGet、HttpPost、HttpPut 與 HttpDelete 等 Attribute，並建立對應路由。
app.MapControllers();

// 啟動應用程式，開始監聽並處理 HTTP 請求。
app.Run();
