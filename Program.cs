// 匯入問候 API 所在的命名空間，讓這個檔案可以使用 MapHelloEndpoints。
using AspNetLearning.Endpoints;

// 建立 WebApplicationBuilder，負責載入設定、註冊服務及準備 ASP.NET Core 應用程式。
// args 代表啟動程式時傳入的命令列參數。
var builder = WebApplication.CreateBuilder(args);

// 根據 builder 中的設定與服務，建立可以處理 HTTP 請求的 WebApplication。
var app = builder.Build();

// 註冊 HTTP GET 路由。
// "/" 代表網站根路徑，() => "Hello World!" 是收到請求後執行的 Lambda 運算式。
app.MapGet("/", () => "Hello World!");

// 載入第一階段的問候 API 路由。
app.MapHelloEndpoints();

// 啟動應用程式，開始監聽並處理 HTTP 請求。
app.Run();
