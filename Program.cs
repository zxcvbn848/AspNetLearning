// 建立 WebApplicationBuilder，負責載入設定、註冊服務及準備 ASP.NET Core 應用程式。
// args 代表啟動程式時傳入的命令列參數。
var builder = WebApplication.CreateBuilder(args);

// 根據 builder 中的設定與服務，建立可以處理 HTTP 請求的 WebApplication。
var app = builder.Build();

// 註冊 HTTP GET 路由。
// "/" 代表網站根路徑，() => "Hello World!" 是收到請求後執行的 Lambda 運算式。
app.MapGet("/", () => "Hello World!");

// 註冊 HTTP GET /hello/{name} 路由。
// {name} 是路由參數，ASP.NET Core 會把網址中的對應文字放進 name 變數。
app.MapGet("/hello/{name}", (string name) =>
{
    // string 表示文字型別。
    // $ 讓字串可以使用 {name} 插入變數值，這種寫法稱為字串插值。
    string message = $"Hello, {name}!";

    // new { message } 建立匿名物件，ASP.NET Core 會自動將它轉換為 JSON。
    // Results.Ok(...) 會建立 HTTP 200 OK 回應，表示請求處理成功。
    return Results.Ok(new { message });
});

// 啟動應用程式，開始監聽並處理 HTTP 請求。
app.Run();
