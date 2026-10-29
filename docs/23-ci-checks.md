# 第二十三課：GitHub Actions CI

`.github/workflows/ci.yml` 會在指向 `dev` 或 `main` 的 Pull Request，以及這兩個分支收到推送時執行。CI 是一台暫時的 GitHub 執行機器：它取出程式碼、建置、測試，結束後移除測試容器與資料庫資料。

只修改 Markdown 文件或 `docs/` 內檔案的 PR 會略過 CI；只要同一個 PR 同時修改程式碼或工作流程，仍執行完整檢查。這項規則也適用於合併後推送到 `dev`、`main` 的事件。

## 檢查順序

1. `npm ci` 依 `frontend/package-lock.json` 安裝固定版本；`npm run build` 同時檢查 TypeScript 與 Vue 建置；`npm test` 執行前端單元測試。
2. `docker compose up -d app` 啟動 PostgreSQL 與 ASP.NET Core。CI 等待首頁可連線後，再執行 `dotnet build --no-restore`。
3. 現有的 smoke tests 依序檢查 OpenAPI 文件、註冊與 JWT/Refresh Token、Todo 的 SignalR 即時通知。聊天室測試檔存在時也會執行，讓聊天室 PR 合併後自動納入檢查。
4. 失敗時列出容器 Log；最後用 `docker compose down -v` 清掉 CI 的測試資料庫。這不會操作你的本機資料庫。

在 PR 的 **Checks** 頁籤可查看 `build-and-test`。全部通過代表這些自動檢查通過，不代表已涵蓋每種瀏覽器操作。分支保護若要禁止紅燈 PR 合併，還需在 GitHub 將 `build-and-test` 設為必要檢查。
