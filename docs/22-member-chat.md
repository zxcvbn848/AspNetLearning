# 第二十二課：會員公開聊天室

底層連線與訊息推送流程請看 [WebSocket 與 SignalR 流程圖](websocket-signalr-flow.md)。

這一課用既有的 SignalR 與 PostgreSQL 建立一個公開聊天室。登入後在前端選擇「聊天室」，開兩個分頁或用兩個會員帳號，就能看到訊息即時出現在對方畫面。每則訊息會保存到資料庫；重新進入聊天室時，畫面讀取最近 50 則。為避免公開會員的註冊資料，畫面只顯示會員編號。

## 一次送訊息會經過什麼

1. `frontend/src/components/ChatPanel.vue` 建立 `/hubs/chat` 連線，`accessTokenFactory` 提供 JWT。後端 `ChatHub` 的 `[Authorize]` 會拒絕未登入連線。
2. 連線成功後，Hub 的 `OnConnectedAsync` 把連線加入 `lobby` 群組。離線時 SignalR 自動移除群組成員。前端呼叫 `GetRecentMessages` 讀取資料庫中最近 50 則。
3. 使用者送出文字時，前端用 `connection.invoke('SendMessage', text)` 呼叫 C# 的 `SendMessage` 方法。這就是 Client → Server 的方向，與前一課只接收通知不同。
4. Hub 先修剪前後空白，拒絕空白或超過 500 字的訊息；會員 Id 只從已驗證的 JWT 取得，不接受 Client 自報。`SaveChangesAsync()` 等待 PostgreSQL 完成儲存。
5. 儲存成功後，`Clients.Group("lobby").SendAsync("MessageReceived", message)` 把訊息送給所有已連線會員。前端的 `hub.on('MessageReceived', ...)` 接收並顯示。這是 Server → Client 的方向。

`GetRecentMessages` 用 `OrderByDescending` 先取最新 50 則，再在回傳前反轉，讓畫面由舊到新排列。前端以訊息 Id 去重：重連後補讀紀錄時，同一則訊息可能同時來自即時廣播與歷史查詢。

## 資料與連線生命週期

`Models/ChatMessage.cs` 描述訊息資料，`Data/AppDbContext.cs` 對應 PostgreSQL 的 `chat_messages` 資料表；`AddChatMessages` Migration 建表。進入聊天室時 Vue 元件掛載並連線，切換到其他功能頁時元件卸載並斷線。意外斷線時 `withAutomaticReconnect()` 嘗試重連，成功後重讀最近訊息補上離線期間遺漏的內容。

SignalR Hub 方法不是一般 HTTP Controller，因此不會出現在 Scalar/OpenAPI 的路由清單，也不適合沿用此專案的 Postman HTTP Collection。直接從 Vue 畫面操作最容易看出雙向通訊；整合測試 `frontend/tests/chat-smoke.mjs` 會檢查未登入拒絕、輸入驗證、兩個會員收到廣播，以及重新連線後的歷史紀錄。

這是學習版的單一公開大廳。訊息保留在資料庫，尚無分頁瀏覽、刪除、檢舉或防洗版機制；若要對外正式開放，應先補上這些管理與濫用防護能力。
