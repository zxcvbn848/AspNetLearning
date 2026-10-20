# 第十六課：以狀態切換整理 Vue 儀表板

這一課不新增 API，而是解決所有操作介面持續往下排列、頁面越來越長的問題。

## 調整後的畫面結構

```text
API 整合平台
├── 會員
│   ├── 登入或註冊
│   └── 登入後顯示目前會員
├── 待辦事項
├── 錯誤測試
└── API 狀態
```

桌面版使用左側導覽列，右側只顯示目前選擇的功能。手機版將導覽列改成可以水平捲動的按鈕列。

## 為何暫時不使用 Vue Router

目前所有功能都在同一個學習頁面內，尚未要求每個功能擁有獨立網址。使用一個 `ref` 就能完成切換，沒有必要先增加套件與路由設定。

```ts
type DashboardPage = 'member' | 'todos' | 'errors' | 'status'

const activePage = ref<DashboardPage>('member')
```

- `type DashboardPage = ...` 是 TypeScript 的字串聯集型別，只允許列出的四個值。
- `ref<DashboardPage>()` 表示這個響應式狀態必須符合 `DashboardPage`。
- `'member'` 是畫面第一次開啟時的預設值。

## 用 v-if 顯示單一功能頁

```vue
<section v-if="activePage === 'member'">會員</section>
<section v-else-if="activePage === 'todos'">待辦事項</section>
<section v-else-if="activePage === 'errors'">錯誤測試</section>
<section v-else>API 狀態</section>
```

`v-if` 只建立條件成立的區塊。切換頁面時，上一個區塊會被移除，新的區塊才會建立，因此畫面不會同時顯示所有表單。

這也代表尚未送出的表單內容可能在切換後清除。對目前的 API 練習頁來說，這是可以接受且容易理解的行為。

## 用 v-for 產生導覽按鈕

導覽文字集中在陣列中：

```ts
const navigationItems = [
  { id: 'member', label: '會員', description: '註冊、登入與會員資料' },
  { id: 'todos', label: '待辦事項', description: '管理自己的 Todo' },
]
```

模板使用 `v-for` 建立按鈕：

```vue
<button
  v-for="item in navigationItems"
  :key="item.id"
  @click="selectPage(item.id)"
>
  {{ item.label }}
</button>
```

- `v-for` 逐筆讀取陣列。
- `:key` 讓 Vue 能穩定識別每個按鈕。
- `@click` 是 `v-on:click` 的簡寫。
- `item.id` 會交給 `selectPage()` 更新目前頁面。

## 依登入狀態調整會員畫面

未登入時，畫面只顯示登入或註冊其中一張表單；登入後改成只顯示目前會員資料。

```vue
<template v-if="!hasAccessToken">
  <LoginForm v-if="activeAuthForm === 'login'" />
  <RegisterForm v-else />
</template>

<CurrentUserPanel v-else />
```

`template` 本身不會產生 HTML 元素，適合用來一起控制多個子元素。

## 為何切到 Todo 時重新讀取資料

```ts
function selectPage(page: DashboardPage): void {
  activePage.value = page

  if (page === 'todos') {
    void loadTodos()
  }
}
```

進入 Todo 頁時再次呼叫 API，讓清單接近資料庫的最新狀態。`void` 表示這裡刻意不等待 Promise，畫面先切換，資料完成後再由 Vue 更新清單。

## 何時需要 Vue Router

出現以下需求時再加入即可：

- 希望網址顯示 `/members`、`/todos`。
- 重新整理後仍要停在同一頁。
- 希望使用瀏覽器上一頁與下一頁。
- 希望直接分享某個功能頁的網址。

目前使用狀態切換，可以用更少的程式碼先理解 Vue 的響應式資料與條件渲染。
