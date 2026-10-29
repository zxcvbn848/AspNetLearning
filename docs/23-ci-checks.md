# 第二十三課：GitHub Actions CI

`.github/workflows/ci.yml` 會在指向 `dev` 或 `main` 的 Pull Request，以及這兩個分支收到推送時執行。CI 是一台暫時的 GitHub 執行機器：它取出程式碼、建置、測試，結束後移除測試容器與資料庫資料。

只修改 Markdown 文件或 `docs/` 內檔案的 PR 會略過 CI；只要同一個 PR 同時修改程式碼或工作流程，仍執行完整檢查。這項規則也適用於合併後推送到 `dev`、`main` 的事件。

## 檢查順序

1. `npm ci` 依 `frontend/package-lock.json` 安裝固定版本；`npm run build` 同時檢查 TypeScript 與 Vue 建置；`npm test` 執行前端單元測試。
2. `docker compose up -d app` 啟動 PostgreSQL 與 ASP.NET Core。CI 等待首頁可連線後，再執行 `dotnet build --no-restore`。
3. 現有的 smoke tests 依序檢查 OpenAPI 文件、註冊與 JWT/Refresh Token、Todo 的 SignalR 即時通知。聊天室測試檔存在時也會執行，讓聊天室 PR 合併後自動納入檢查。
4. 失敗時列出容器 Log；最後用 `docker compose down -v` 清掉 CI 的測試資料庫。這不會操作你的本機資料庫。

在 PR 的 **Checks** 頁籤可查看 `build-and-test`。全部通過代表這些自動檢查通過，不代表已涵蓋每種瀏覽器操作。純文件 PR 因路徑過濾不會啟動此工作流程，因此看不到這次 PR 的 CI 執行紀錄。

目前先不要將 `build-and-test` 設為分支保護的必要檢查：GitHub 對被路徑過濾略過的工作流程，可能會一直等待必要檢查回報，導致純文件 PR 無法合併。若之後要強制 CI 通過才能合併，應先改成每支 PR 都執行一個短檢查，只有程式碼變更才跑完整建置與測試。參考 [GitHub：略過但被要求的檢查](https://docs.github.com/en/pull-requests/how-tos/merge-and-close-pull-requests/troubleshooting-required-status-checks#handling-skipped-but-required-checks)。
