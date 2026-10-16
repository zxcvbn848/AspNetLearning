# 第一課：讀懂問候 API

這一課先從閱讀開始，不要求手寫。目標是理解一段 C# 程式如何變成可以從瀏覽器呼叫的 API。

## API 功能

在瀏覽器開啟：

```text
http://localhost:8180/hello/Andy
```

伺服器會回傳 HTTP 200 OK 與 JSON：

```json
{
  "message": "Hello, Andy!"
}
```

把網址最後的文字改成其他名稱，回傳內容也會跟著改變：

```text
http://localhost:8180/hello/Amy
```

```json
{
  "message": "Hello, Amy!"
}
```

## 完整程式碼

```csharp
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
```

## 第一行：建立 GET 路由

```csharp
app.MapGet("/hello/{name}", (string name) =>
```

### `app`

`app` 是 `WebApplication` 物件，代表目前執行中的 ASP.NET Core 應用程式。

### `.`

句點表示要使用某個物件所提供的成員。`app.MapGet` 代表使用 `app` 提供的 `MapGet` 方法。

### `MapGet`

`MapGet` 用來設定 HTTP GET 路由。GET 通常用於讀取資料，不應用來新增、修改或刪除資料。

### `(...)`

小括號用來放入呼叫方法時需要的參數。這裡傳入兩個參數：

1. 路由樣式 `"/hello/{name}"`
2. 收到請求後要執行的 Lambda 運算式

### `"/hello/{name}"`

雙引號包住的是 `string` 字串。

- `/hello/` 是固定路徑。
- `{name}` 是可以改變的路由參數。

例如網址是 `/hello/Andy`，`name` 的值就是 `"Andy"`。

### `(string name)`

這是 Lambda 運算式的參數：

- `string` 是型別，表示內容是一段文字。
- `name` 是變數名稱，用來存放網址提供的名稱。

ASP.NET Core 會根據路由中的 `{name}`，自動把值放進同名的 `name` 參數。這個過程稱為模型繫結（Model Binding）。

### `=>`

這是 Lambda 運算子，可以讀成「收到左邊的參數後，執行右邊的內容」。

### `{ ... }`

大括號建立程式區塊。這裡包含收到 HTTP 請求後依序執行的敘述。

## 第二行：建立變數

```csharp
string message = $"Hello, {name}!";
```

### `string`

`string` 是 C# 的文字型別。

### `message`

`message` 是變數名稱。變數可以暫時保存程式執行期間需要的資料。

### `=`

等號是指定運算子，表示把右邊產生的值存入左邊的變數。

### `$"Hello, {name}!"`

這是字串插值：

- `$` 表示字串中可以插入變數。
- `{name}` 會換成 `name` 變數目前的值。
- 如果 `name` 是 `Andy`，結果就是 `Hello, Andy!`。

### `;`

分號表示一個 C# 敘述結束。

## 第三行：回傳成功結果

```csharp
return Results.Ok(new { message });
```

### `return`

`return` 結束目前的 Lambda 運算式，並將後面的結果交還給 ASP.NET Core。

### `Results.Ok(...)`

建立 HTTP 200 OK 回應。200 表示伺服器成功處理請求。

### `new { message }`

`new` 表示建立新物件。這裡沒有指定類別名稱，因此是匿名物件。

`new { message }` 是以下寫法的縮寫：

```csharp
new { message = message }
```

左邊的 `message` 是 JSON 欄位名稱，右邊的 `message` 是前面建立的變數。

ASP.NET Core 會自動把匿名物件序列化成 JSON：

```json
{
  "message": "Hello, Andy!"
}
```

## 最後的 `);`

```csharp
});
```

- `}` 結束 Lambda 程式區塊。
- `)` 結束 `MapGet` 方法呼叫。
- `;` 結束整個 C# 敘述。

## 請求處理流程

```text
瀏覽器請求 GET /hello/Andy
    ↓
ASP.NET Core 找到 /hello/{name} 路由
    ↓
將 Andy 放入 string name
    ↓
建立 message 字串
    ↓
建立匿名物件
    ↓
Results.Ok 建立 HTTP 200 回應
    ↓
ASP.NET Core 將物件轉換成 JSON
```

## 這一課認識的 C# 語法

| 語法 | 用途 |
|---|---|
| `string` | 宣告文字型別 |
| `=` | 將值指定給變數 |
| `$"...{name}..."` | 使用字串插值組合文字 |
| `(string name) =>` | 建立帶有參數的 Lambda 運算式 |
| `{ ... }` | 建立程式區塊 |
| `new { message }` | 建立匿名物件 |
| `return` | 回傳結果並結束目前區塊 |
| `;` | 結束一個敘述 |

## 這一課認識的 ASP.NET Core 功能

| 功能 | 用途 |
|---|---|
| `MapGet` | 設定處理 HTTP GET 的路由 |
| `{name}` | 從網址接收路由參數 |
| Model Binding | 自動把 HTTP 資料轉換並放入 C# 參數 |
| `Results.Ok` | 建立 HTTP 200 OK 回應 |
| JSON 序列化 | 自動將 C# 物件轉換成 JSON |
