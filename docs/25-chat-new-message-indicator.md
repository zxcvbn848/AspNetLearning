# 第二十五課：新訊息提示與回到最新

登入後進入聊天室，往上閱讀歷史訊息時，清單下方會出現「回到最新」。此時另一個分頁傳送訊息，畫面保留閱讀位置，並顯示「有 N 則新訊息」。按下按鈕會捲到底部並清除提示；手動捲到底也會清除。原本停在底部時仍自動跟隨新訊息，不累計未讀。

## 主要程式碼

- [ChatPanel.vue](../frontend/src/components/ChatPanel.vue) 的 `addMessages()` 在更新清單前讀取捲動位置。即時廣播與重連補收只計入清單上尚未出現的 Id，即使先收到 102、再收到 101。首次載入與往前分頁不增加未讀。
- [useChatReadingState.ts](../frontend/src/composables/useChatReadingState.ts) 是 Vue composable：用函式集中管理反應式狀態，角色類似前端的可重用狀態邏輯，與後端 DI Service 的生命週期不同。
- `ref` 保存「是否在底部」與未讀 Id 集合；`computed` 從集合大小計算畫面數量。`Set` 對相同 Id 只保留一次，可聯想 PHP 以 Id 為 key 的陣列，或 Go 的 `map[int]struct{}`。
- `scrollHeight - scrollTop - clientHeight` 是距離清單底部的高度差，小於 40px 視為已到最新。`@scroll` 在使用者捲動時更新狀態；`returnToLatest()` 將 `scrollTop` 設為 `scrollHeight`，瀏覽器會限制在實際可捲動的最大值。

這裡的「新訊息」是目前元件中尚未捲到底查看的提示，不是存進 DB 的跨裝置已讀回條。切換功能頁面後元件卸載，計數會重設。

## 手動驗證

1. 開兩個聊天室分頁，左邊往上捲，右邊傳兩則訊息；左邊保持原閱讀位置並顯示兩則新訊息。
2. 左邊按「回到最新」，確認捲到底、提示消失且看得到新訊息。
3. 左邊再往上捲並收到新訊息，這次手動捲到底，也應清除提示。
4. 載入更早訊息時不增加新訊息數量；停在最新訊息時收到廣播仍自動跟隨。

`frontend/tests/chatReadingState.test.mjs` 涵蓋未讀去重、底部清除及停在底部時不累計的狀態行為。
