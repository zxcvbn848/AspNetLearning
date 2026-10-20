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
```

正式開始前會先決定採用 ASP.NET Core Identity，或以較精簡的 JWT 流程教學。兩者不會同時建立，避免重複的會員邏輯。
