using AspNetLearning.Contracts;

// 宣告此檔案所屬的命名空間，用來組織相關程式碼並避免名稱衝突。
namespace AspNetLearning.Endpoints;

/// <summary>
/// 集中管理第一階段的問候 API 路由。
/// </summary>
/// <remarks>
/// static 表示不需要先建立 HelloEndpoints 物件，就能使用類別中的方法。
/// </remarks>
public static class HelloEndpoints
{
    /// <summary>
    /// 將第一階段的問候 API 註冊到 ASP.NET Core 應用程式。
    /// </summary>
    /// <param name="app">目前正在設定的 ASP.NET Core 應用程式。</param>
    /// <remarks>
    /// this WebApplication app 讓呼叫方式可以寫成 app.MapHelloEndpoints()。
    /// void 表示此方法執行完畢後不回傳資料。
    /// </remarks>
    public static void MapHelloEndpoints(this WebApplication app)
    {
        // 註冊 HTTP GET /hello/{name} 路由。
        // {name} 是路由參數，ASP.NET Core 會把網址中的對應文字放進 name 變數。
        app.MapGet("/hello/{name}", (string name) =>
        {
            // string 表示文字型別。
            // $ 讓字串可以使用 {name} 插入變數值，這種寫法稱為字串插值。
            string message = $"Hello, {name}!";

            // HelloResponse 明確描述回應格式，OpenAPI 才能產生可閱讀的 JSON Schema。
            // Results.Ok(...) 會建立 HTTP 200 OK 回應，表示請求處理成功。
            return Results.Ok(new HelloResponse(message));
        })
        .WithSummary("問候指定名稱")
        .WithDescription("將網址中的 name 放進問候文字，回傳 JSON 物件。")
        .Produces<HelloResponse>(StatusCodes.Status200OK);
    }
}
