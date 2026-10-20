# 第十四課：Request 統一驗證

這一課將輸入規則放進 Request 類別，讓 ASP.NET Core 在執行 Controller Action 前自動驗證資料並回傳 HTTP 400。

## 驗證流程

```text
Client 傳入 JSON
    ↓
JSON 反序列化成 Request 物件
    ↓
[ApiController] 執行 Data Annotations
    ↓ 驗證失敗
自動回傳 HTTP 400，不執行 Controller Action
    ↓ 驗證成功
Controller → Service → PostgreSQL
```

## Data Annotations

`System.ComponentModel.DataAnnotations` 提供可以標記在 Property 上的驗證 Attribute：

```csharp
[Required(ErrorMessage = "Title 不可為空白。")]
[MaxLength(100, ErrorMessage = "Title 最多只能有 100 個字元。")]
public string Title { get; init; } = string.Empty;
```

- `[Required]`：拒絕 `null`、空字串與只有空白的字串。
- `[MaxLength(100)]`：最多接受 100 個字元。
- `ErrorMessage`：指定 Client 會收到的錯誤文字。
- `init`：Property 只能在建立物件時設定，之後不能修改。

註冊與登入 Request 也使用 `[EmailAddress]`、`[MinLength]` 與 `[MaxLength]`。

## 為什麼 Request 不使用 C# `required`

C# `required` 與 Data Annotations 的 `[Required]` 是不同功能：

```csharp
public required string Title { get; init; }
```

`required` 主要在編譯階段要求 C# 呼叫端設定 Property。ASP.NET Core JSON 缺少此欄位時，反序列化會先失敗，錯誤欄位會是 `$`，Data Annotations 還沒有機會產生自訂中文訊息。

Request 改用：

```csharp
public string Title { get; init; } = string.Empty;
```

缺少 JSON 欄位時會保留空字串，再由 `[Required]` 產生一致的 `Title` 錯誤。

Response 類別仍可使用 C# `required`，因為 Response 是由自己的 C# 程式建立，編譯器可以協助避免漏填欄位。

## `[ApiController]` 自動回傳 400

Controller 已標記：

```csharp
[ApiController]
public sealed class TodosController : ControllerBase
```

因此不需要在每個 Action 重複撰寫：

```csharp
if (string.IsNullOrWhiteSpace(request.Title))
{
    return BadRequest(...);
}
```

驗證失敗的回應格式大致如下：

```json
{
  "status": 400,
  "errors": {
    "Title": [
      "Title 不可為空白。"
    ]
  }
}
```

Vue 的 `apiClient` 已經讀取 `errors` 中的第一個訊息，因此後端驗證文字可以直接顯示在畫面上。

## Postman 測試

- 「第十四課：驗證錯誤 Email」確認無效 Email 回傳 400。
- 「第十四課：驗證空白 Todo」確認空白 Title 回傳 400。
- 原本的註冊、登入與 Todo CRUD 請求則確認合法資料仍可正常使用。

Data Annotations 適合目前單一欄位的規則。需要查詢資料庫或比較多個欄位時，再將規則放入 Service 或採用專門的驗證工具。
