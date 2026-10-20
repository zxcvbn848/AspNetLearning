# 第十三課：登出與登入狀態同步

這一課讓登入表單、目前會員與 Todo 共用一致的登入狀態。手動登出或受保護 API 回傳 HTTP 401 時，畫面都會完整回到未登入狀態。

## 為什麼沒有後端登出 API

目前使用的是無狀態 JWT。伺服器沒有保存每一個 Token，因此登出只需要移除瀏覽器 `sessionStorage` 中的 `accessToken`。

```ts
window.sessionStorage.removeItem('accessToken')
```

Token 在原本的到期時間前仍是有效字串，但瀏覽器已不再保存或傳送它。未來若加入 Refresh Token、Token 黑名單或多裝置工作階段，再建立後端登出 API。

## 統一處理 HTTP 401

`apiClient` 是所有前端 API 的共同入口。受保護請求帶有 `Authorization` Header，而且回傳 401 時，它會發出自訂事件：

```ts
if (response.status === 401 && isProtectedRequest) {
  window.dispatchEvent(new Event(authUnauthorizedEvent))
}
```

只有帶 Token 的請求會發出事件，所以登入時輸錯密碼所得到的 401 不會清除其他畫面狀態。

## App 統一清理畫面

`App.vue` 監聽授權失效事件。手動按下「登出」與 Token 失效都會呼叫 `handleLoggedOut`：

```text
移除 accessToken
    ↓
清空 Todo 與輸入內容
    ↓
關閉編輯對話框
    ↓
重新建立登入與會員元件
    ↓
顯示未登入狀態
```

元件移除前會解除事件監聽，避免開發環境 Hot Reload 後重複執行同一事件。

## 測試方式

1. 登入後確認會員資料與 Todo 正常顯示。
2. 按下「登出」，確認會員資料、Todo 與 Token 一起清除。
3. 在 Postman 執行「第十三課：驗證失效 Token」，確認無效 Token 回傳 HTTP 401。
4. 回到前端重新登入，確認功能可以繼續使用。

目前 JWT 無法在伺服器端主動撤銷。這是無狀態 JWT 的特性；需要立即撤銷能力時，才加入 Refresh Token 或伺服器端工作階段。
