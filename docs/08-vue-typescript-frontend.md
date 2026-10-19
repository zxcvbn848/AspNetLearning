# 第八課：Vue 3 + TypeScript 前端

這一階段在同一個 Git 專案加入 `frontend/`，由 Vue 顯示待辦清單，並呼叫既有的 ASP.NET Core API。

## 執行流程

```text
瀏覽器 http://localhost:8181
    ↓
Vue 元件處理畫面與使用者操作
    ↓
todoApi.ts 使用 fetch 呼叫 /todos
    ↓
Vite Proxy 轉送到 http://app:8080
    ↓
ASP.NET Core Controller → Service → EF Core → PostgreSQL
```

瀏覽器只認得 `localhost:8181`。`app` 是 Docker Compose 內部的服務名稱，因此由 Vite 在容器網路中代替瀏覽器轉送請求。前端與 API 對瀏覽器而言是同一個來源，也就不需要先加入 CORS 設定。

## TypeScript 型別

`src/types/todo.ts` 描述 API 回傳資料的形狀：

```ts
export interface TodoItem {
  id: number
  title: string
  isCompleted: boolean
}
```

- `interface` 宣告物件應包含哪些欄位。
- `number`、`string`、`boolean` 分別代表數字、文字與真假值。
- TypeScript 只在開發與建置時檢查型別，瀏覽器執行的是編譯後的 JavaScript。

## API 模組

`src/services/todoApi.ts` 集中管理四種 HTTP 操作：

| 函式 | HTTP 請求 | 用途 |
|---|---|---|
| `getTodoItems()` | `GET /todos` | 取得清單 |
| `createTodoItem()` | `POST /todos` | 新增資料 |
| `updateTodoItem()` | `PUT /todos/{id}` | 修改資料 |
| `deleteTodoItem()` | `DELETE /todos/{id}` | 刪除資料 |

`fetch()` 回傳 `Promise`，因此函式使用 `async` 與 `await` 等待 HTTP 回應。共用的 `request()` 會檢查 `response.ok`，並將 API 錯誤轉成畫面可以顯示的 `Error`。

## App.vue

`<script setup lang="ts">` 表示元件使用 Vue 的 Composition API 與 TypeScript。

```ts
const todos = ref<TodoItem[]>([])
```

- `ref()` 建立響應式資料。
- `<TodoItem[]>` 指定內容是 Todo 陣列。
- 讀寫 JavaScript 內的值時使用 `todos.value`；模板中 Vue 會自動解開 `.value`。

```html
<input v-model="newTitle">
```

`v-model` 讓輸入框與 `newTitle` 雙向同步。使用者輸入時資料會更新，程式修改資料時輸入框也會更新。

```html
<TodoListItem
  v-for="todo in todos"
  :key="todo.id"
  :todo="todo"
  @toggle="toggleTodo"
/>
```

- `v-for` 根據陣列重複建立元件。
- `:key` 讓 Vue 穩定識別每一筆資料。
- `:todo` 把資料傳給子元件。
- `@toggle` 接收子元件發出的事件。

## TodoListItem.vue

```ts
defineProps<{ todo: TodoItem }>()
```

`defineProps` 宣告父元件必須傳入一筆 `TodoItem`。

```ts
defineEmits<{
  toggle: [todo: TodoItem]
}>()
```

`defineEmits` 宣告子元件可以發出的事件及參數型別。子元件只負責顯示與通知操作，實際 API 流程仍由 `App.vue` 管理。

## Vite Proxy

`vite.config.ts` 中的設定：

```ts
proxy: {
  '/todos': {
    target: 'http://app:8080',
    changeOrigin: true,
  },
}
```

所有以 `/todos` 開頭的請求都會轉送至 ASP.NET Core 容器。這只適用目前的 Docker 開發環境；正式部署時可由反向代理提供相同路徑。

## 啟動與驗證

在專案根目錄啟動所有服務：

```sh
docker compose up -d
```

開啟前端：

```text
http://localhost:8181
```

需要單獨檢查 TypeScript 與正式建置時執行：

```sh
docker compose exec frontend npm run build
```
