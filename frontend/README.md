# Vue 3 + TypeScript 前端

這個目錄是待辦清單的前端，使用 Vue 3、TypeScript 與 Vite。

## 主要檔案

```text
frontend/
├── src/
│   ├── components/
│   │   └── TodoListItem.vue
│   ├── services/
│   │   └── todoApi.ts
│   ├── types/
│   │   └── todo.ts
│   ├── App.vue
│   ├── main.ts
│   └── style.css
├── index.html
├── package.json
├── tsconfig.json
└── vite.config.ts
```

- `App.vue`：管理畫面狀態與新增、修改、刪除流程。
- `TodoListItem.vue`：顯示單筆待辦事項，並把操作事件通知父元件。
- `todoApi.ts`：集中呼叫 ASP.NET Core Todo API。
- `todo.ts`：宣告前後端共用資料形狀的 TypeScript 型別。
- `vite.config.ts`：將瀏覽器送往 `/todos` 的請求代理至後端容器。

整組專案由根目錄的 Docker Compose 啟動，前端網址是：

```text
http://localhost:8181
```

詳細語法與資料流程請閱讀 `docs/08-vue-typescript-frontend.md`。
