# 第二十課：用 SignalR 即時更新待辦事項

登入後，在兩個瀏覽器分頁開啟 `http://localhost:8181` 的「待辦事項」。其中一頁新增、修改、完成或刪除 Todo，另一頁的清單會自動更新。「即時連線」文字會顯示已連線、重新連線中或未連線。這個功能不需要 Postman Collection，因為 SignalR 使用持續連線，而不是一般的單次 HTTP API 呼叫。

## 從 HTTP 到即時通知

原本 Vue 只有按下按鈕時呼叫 `GET /todos`。現在 `frontend/src/services/todoRealtime.ts` 連到 `/hubs/todos`；連線成功後，後端可主動送出 `TodosChanged` 事件。Vue 收到事件，仍使用原本的 `GET /todos` 取得資料。事件只表示「資料變了」，不把完整清單塞進訊息。

後端的 `TodoHub` 標有 `[Authorize]`，連線必須帶 JWT。`OnConnectedAsync` 從已驗證的會員身分讀出 Id，將連線加入 `todos:<會員 Id>` 群組。`TodosController` 只有在資料庫新增、更新或刪除成功後，才透過 `IHubContext<TodoHub>` 通知該會員的群組。因此其他會員不會收到通知，原有 Todo API 的授權仍是讀取資料的最後一道檢查。

瀏覽器的 WebSocket 握手無法自行設定 `Authorization` Header；SignalR JavaScript Client 在必要時把 JWT 放進 `access_token` 查詢參數。`Program.cs` 只允許 `/hubs/todos` 路徑讀取這個參數，其他 API 仍使用 Header。正式環境應使用 HTTPS，並避免把含 `access_token` 的完整 URL 寫入存取紀錄。

## 斷線、重連與 Token

`withAutomaticReconnect()` 會在意外斷線後嘗試重連。重連成功時再呼叫一次 `GET /todos`，補上斷線期間可能漏掉的變更。15 分鐘 JWT 快到期時，`getRealtimeAccessToken()` 沿用前端既有的 Refresh Token 流程；伺服器也會在身分驗證到期時結束舊連線，讓客戶端重新驗證。若登入狀態失效，畫面會清除連線與會員資料。

先讀 `Hubs/TodoHub.cs` 了解 Hub 與 Group，再看 `Program.cs` 的 JWT 和路由設定；接著看 `Controllers/TodosController.cs` 何時推送事件，最後看 Vue 如何連線、接收事件及重新讀取清單。聊天室已記在開發清單，會是後續獨立階段，屆時才加入雙向訊息和訊息保存。
