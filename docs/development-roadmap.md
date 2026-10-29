# 開發清單

這份文件記錄接下來預計加入 `AspNetLearning` 的功能。實作時仍維持一次完成一個小階段，方便閱讀每次 PR 的變化。

## 下一階段：會員系統

- [x] 建立會員資料模型與資料庫 Migration
- [x] 建立註冊 API，驗證 Email、密碼與重複帳號
- [x] 使用安全的密碼雜湊保存密碼，不儲存明文密碼
- [x] 建立登入 API 並簽發 JWT
- [x] 建立取得目前登入會員資料的 API
- [x] 使用授權機制保護需要登入的 API
- [x] 建立 Vue 註冊畫面
- [x] 建立 Vue 登入畫面
- [x] 在 Postman Collection 加入會員 API 與 Token 變數
- [x] 補上會員系統的學習文件與測試步驟
- [x] 建立登出功能，並在 Token 失效時同步清除整個前端登入狀態
- [x] 使用 Data Annotations 統一驗證 API Request
- [x] 使用 Problem Details 與 Middleware 統一處理未預期例外
- [x] 使用 Vue 狀態與條件渲染建立儀表板導覽
- [x] 加入亮暗模式切換並記住選擇
- [x] Refresh Token 輪替、伺服器端登出與前端自動續期

## 待辦：API 文件

- [x] 建立可瀏覽的 API 文件，涵蓋各端點的請求與回應格式、狀態碼、JWT 授權、Refresh Token Cookie，以及操作範例。

## 待辦：WebSocket 即時通訊

- [x] 使用 ASP.NET Core SignalR 通知同一會員的 Vue 分頁重新載入 Todo；示範連線、重連與 JWT 授權。

## 待辦：聊天室

- [x] 建立會員公開聊天室，練習 SignalR 的雙向訊息、連線加入大廳與斷線離開、PostgreSQL 訊息保存及 Vue 即時顯示。

## 待辦：Postman 練習 SignalR

- [ ] 另建 WebSocket Collection，示範 SignalR 連線協商、Hub 握手、JWT 授權與 `SendMessage` 訊息格式；保留現有 HTTP Collection，並附逐步操作說明。

## 待辦：整理應用程式入口

- [x] 將 `Program.cs` 中較長的 JWT 註冊與驗證設定移到獨立檔案，讓入口保留服務註冊、Middleware 順序與路由概覽；保持現有授權行為不變。

## 建議學習順序

```text
會員 Model
    ↓
註冊與密碼雜湊
    ↓
登入與 JWT
    ↓
授權保護 API
    ↓
Vue 登入狀態與會員畫面
    ↓
登出與 401 狀態同步
    ↓
Request 自動驗證
    ↓
全域例外處理
    ↓
Vue 儀表板導覽
    ↓
亮暗模式
    ↓
Refresh Token、Cookie 與工作階段
```

正式開始前會先決定採用 ASP.NET Core Identity，或以較精簡的 JWT 流程教學。兩者不會同時建立，避免重複的會員邏輯。
