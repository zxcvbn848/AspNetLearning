# 第二十四課：聊天室歷史訊息分頁

登入後開啟「聊天室」，先顯示最新 50 則；資料滿一頁時出現「載入更早訊息」。每按一次最多補 50 則，保留目前閱讀位置。若回傳少於 50 則，就顯示已載入所有歷史訊息；剛好滿 50 則時，可能需再查一次空頁才能確認結尾。

## 用訊息 Id 當游標

游標（cursor）是下一次查詢的定位值。假設已顯示訊息 101～150，最舊的 Id 是 101，下一次就查 `Id < 101`，由大到小取 50 則，再反轉為 51～100。Id 可以有間隔，不需要連號。

```csharp
// beforeId 為 null 時查最新一頁；有值時只查更早的訊息。
where beforeId == null || message.Id < beforeId
// 從符合條件的訊息中優先取較新的。
orderby message.Id descending
```

`Take(50)` 限制資料庫回傳筆數；`ToListAsync()` 執行查詢；`Reverse()` 在記憶體中把這一頁調成由舊到新。這是 LINQ 經 EF Core 轉為 SQL 的實際用途。Laravel Query Builder 的近似查法是 `where('id', '<', $beforeId)->orderByDesc('id')->limit(50)->get()`。

相較於用 `Skip` 跳過固定筆數，游標以前一頁的 Id 定位；期間即使有人新增訊息，也不會把這個游標之前的頁面往後推。訊息若被刪除，資料仍可能改變，游標不代表資料庫快照。

## 程式碼閱讀順序

1. [ChatHub](../Hubs/ChatHub.cs)：保留原本無參數的 `GetRecentMessages()`；新增 `GetOlderMessages(beforeId)`，兩者共用私有查詢方法。小於等於 0 的游標會被拒絕。
2. [ChatPanel.vue](../frontend/src/components/ChatPanel.vue)：`loadOlder()` 使用目前最舊訊息的 Id 呼叫 Hub，`hasMore` 決定按鈕是否顯示。查詢失敗保留現有訊息，可以重試。
3. `addMessages()` 依 Id 合併去重，不再只保留最後 50 則。加入舊訊息前後比較清單高度，調整捲動位置；閱讀舊訊息時，新廣播不會強制捲到底。

這是 Hub 方法，測試沿用 SignalR Client；沒有新增一般 HTTP API 或 Postman HTTP 請求。`frontend/tests/chat-smoke.mjs` 會驗證跨頁查詢、排序、結尾空頁、無效游標，以及查詢期間新增訊息的情況。

目前載入的訊息保留在此 Vue 元件的記憶體中，切換頁面會釋放；長時間大量載入時，之後再考慮虛擬清單或限制保留頁數。重連仍只補讀最新 50 則，長時間離線可能需要重新載入歷史才能補齊。
