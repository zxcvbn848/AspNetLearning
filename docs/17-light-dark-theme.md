# 第十七課：Vue 亮暗模式

這一課在儀表板右上角加入「暗色模式」按鈕。預設維持原本的亮色畫面；按下按鈕後切成暗色，再按一次恢復亮色。選擇會保存在目前瀏覽器，重新整理後仍然有效。

## 先讀 App.vue

```ts
const isDark = ref(window.localStorage.getItem('aspnetlearning-theme') === 'dark')
document.documentElement.dataset.theme = isDark.value ? 'dark' : 'light'
```

- `localStorage.getItem()` 讀取先前保存的字串；沒有保存過時回傳 `null`。
- `=== 'dark'` 將讀取結果轉成 `true` 或 `false`。
- `ref()` 把布林值變成 Vue 的響應式狀態，按鈕文字與 `aria-pressed` 會跟著更新。
- `document.documentElement` 指向網頁最外層的 `<html>` 元素。
- `dataset.theme` 對應 HTML 的 `data-theme` 屬性；三元運算式根據 `isDark.value` 選擇 `dark` 或 `light`。

按鈕呼叫 `toggleTheme()`：

```ts
isDark.value = !isDark.value
const theme = isDark.value ? 'dark' : 'light'
document.documentElement.dataset.theme = theme
window.localStorage.setItem('aspnetlearning-theme', theme)
```

`!` 將布林值反轉。`setItem()` 保存選擇，因此重新整理或關閉瀏覽器後仍能沿用。

## 再讀 style.css

`:root` 定義亮色模式的色彩變數；`:root[data-theme="dark"]` 定義暗色模式的同名變數。現有元件使用 `var(--text)`、`var(--surface)` 等變數，因此不必為每個畫面額外維護一份暗色 CSS。

```css
:root { --text: #172033; }
:root[data-theme="dark"] { --text: #edf2fa; }
```

`color-scheme: dark` 也讓瀏覽器原生控制項，例如捲軸與部分表單元件，使用相符的暗色外觀。

## 自己驗證

開啟 `http://localhost:8181`，按右上角「暗色模式」。確認背景、導覽列、表單與對話框都轉為暗色；再重新整理，確認選擇保留。再次按下按鈕可恢復亮色。

這一版使用手動選擇，首次開啟時預設亮色；尚未跟隨作業系統的外觀設定。
