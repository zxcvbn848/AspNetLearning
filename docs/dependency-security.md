# 依賴與容器安全檢查紀錄

檢查日期：2026-11-04（Asia/Taipei）。範圍為本機 ARM64 專案依賴與實際 Docker 映像。下列數量是套件／漏洞配對數，可能包含同一 CVE 的不同套件，不等於不同漏洞或可利用入口的數量。

## 第一階段修補與驗證（歷史紀錄）


Compose 透過 `docker/` 的三份 Dockerfile 建立本機映像；開發容器沿用相同設定。後端更新 Ubuntu 的 Brotli 與 PCRE2，資料庫更新 Alpine 的 Brotli 與 libcurl，前端與 CI 統一使用 npm 11.21.1。前端以 `npm ci` 依既有鎖定檔安裝。

2026-11-04 的 ARM64 測試映像掃描：Node 系統套件為 0，npm 內附依賴仍有 8 筆；.NET 10.0.14 與 PowerShell 依賴為 0，Ubuntu 仍有 24 筆且未列修補版本；PostgreSQL 系統套件為 0，gosu 的 Go 依賴仍有 61 筆，實際可利用性待確認。這些結果不是完整安全保證，CI 的 AMD64 映像也需另行掃描。

首次啟動或修改 Dockerfile 後使用 `docker compose up -d --build`。更新上游映像與系統套件時先執行 `docker compose build --pull --no-cache`，再複掃並驗證。重建服務會短暫中斷本機連線；保留既有資料 Volume，不使用 `docker compose down -v` 清理開發環境。後端 NuGet 尚無鎖定檔，因此未來還原的間接依賴版本仍可能變動。

本次修補映像已完成：三份映像建置、Compose 設定檢查、後端建置（0 警告／0 錯誤）、前端建置與 2 項單元測試、npm 與 NuGet 直接／間接依賴複掃，以及獨立測試資料庫上的 OpenAPI、JWT／Refresh Token、會員隔離、待辦與聊天室 SignalR 測試。以上均通過。測試環境已清除。使用者已重建並套用三個本機修補映像；確認三個服務均持續執行、資料庫健康，前後端 HTTP 均回應 200，原資料 Volume 保留。未提交、推送或部署至線上，GitHub Actions 尚未實際執行。

## 目前剩餘項目與處理順序

1. npm 工具：8 筆（高 3、中 5），來自 npm 11.21.1 內附的 glob、ip-address、minimatch、undici。已在隔離映像實測最新版 npm 12.2.1，仍為相同 8 筆、相同受影響版本，因此維持 11.21.1，不為相同漏洞數承擔重大升級。等待 npm 上游整合修補；不直接修改專案鎖定檔或手動覆寫 npm 內部依賴。
2. PostgreSQL 的 gosu：原先 61 筆已透過重建工具排除，候選複掃為 0。詳見下方第二次修補紀錄，原始項目保留供追溯，並非仍有 61 個已確認可利用的漏洞。
3. .NET 映像的 Ubuntu 套件：24 筆（中 19、低 5），本次報告未列修補版本。這只表示該次掃描未提供修補版本，並非斷言所有上游都沒有修補；已重新查詢 Ubuntu 24.04 官方來源並模擬升級，結果為 0 個可更新套件。維持目前版本並追蹤公告；不切換作業系統主版本或添加非官方來源。

使用者已確認僅處理仍有漏洞的項目。未升級 npm 12、Node.js／.NET／PostgreSQL 主版本。npm 與 Ubuntu 剩餘項目仍屬版本匹配，未完成逐項可利用性分析，也沒有已遭入侵的證據。

## 第二次修補：重建 gosu（2026-11-04）

- 確認官方發行版 gosu 1.19 已是目前版本，內含 Go 1.24.6。依上游安全政策執行 govulncheck v1.1.4 的 binary／symbol 分析，資料庫為 vuln.go.dev、更新時間 2026-11-02T22:01:43Z。
- 分析在二進位中找到 CVE-2026-61801（GO-2026-6686）涉及的 GetExecUserPath、GetExecUser、ParseGroupFilter 函式。觸發仍需能控制被解析的使用者／群組檔案；這不是確認能從網站遠端利用。其餘大部分 Go 版本項目未找到受影響函式，不直接視為可利用，也不以此宣稱全部是假陽性。
- 固定 gosu 1.19 原始碼 commit `6456aaa0f3c854d199d0f037f068eb97515b7513`，使用 Go 1.27.3，將 `github.com/moby/sys/user` 更新至 0.4.1、`golang.org/x/sys` 更新至 0.44.0 後重建。此為本專案重建版本，不是聲稱上游發布了新的 gosu 版本。
- Dockerfile 使用多階段建置，Go 編譯器與 Git 不進入最後的 PostgreSQL 映像。保留上游 PostgreSQL 18 的啟動流程與資料路徑；建置時驗證 nobody 及 postgres 的權限切換。
- 上游 gosu 專案 `go test ./...` 回報沒有測試檔；真正執行的驗證為權限切換自檢、獨立資料庫初始化、API／JWT／Refresh Token／會員隔離／待辦及聊天室 SignalR 測試，以及重啟後測試資料保留。以上通過。
- 正式映像 `sha256:538bd3aa63900bd69706640261c0ab457ad2bd4a5b4fa2f712bf3a0061173993` 的 Trivy 複掃：Alpine 系統套件 0、gosu 0；不代表未知漏洞已排除。已套用至本機服務，資料庫健康且 SQL 連線通過，沿用 `aspnetlearning_postgres_data`，前後端 HTTP 均為 200。獨立測試環境已清除。

來源：[gosu 安全政策](https://github.com/tianon/gosu/blob/master/SECURITY.md)、[使用者／群組解析漏洞公告](https://github.com/moby/sys/security/advisories/GHSA-mjcv-p78q-w5fw)、[npm 12.2.1 發行紀錄](https://github.com/npm/cli/releases/tag/v12.2.1)。

## 掃描清單（npm、Ubuntu 尚餘；gosu 清單為修補前歷史）

以下直接摘錄本次修補映像的 Trivy JSON 報告，保留每個套件／漏洞配對。修補版本為掃描報告列出的候選，並非已確認可直接替換或彼此相容。

### 前端映像：npm 內附依賴

| 套件 | 安裝版本 | 漏洞 | 嚴重度 | 報告列出的修補版本 |
|---|---|---|---|---|
| glob | 13.0.6 | CVE-2026-118773 | MEDIUM | 14.0.2 |
| ip-address | 10.7.2 | CVE-2026-111997 | MEDIUM | 10.7.4 |
| minimatch | 10.2.5 | CVE-2026-118764 | MEDIUM | 11.0.1 |
| undici | 6.29.0 | CVE-2026-109383 | HIGH | 6.29.1, 7.30.1, 8.12.2 |
| undici | 6.29.0 | CVE-2026-109869 | HIGH | 6.29.1, 7.30.1, 8.12.2 |
| undici | 6.29.0 | CVE-2026-87018 | HIGH | 6.29.1, 7.30.1, 8.12.2 |
| undici | 6.29.0 | CVE-2026-109924 | MEDIUM | 6.29.1, 7.30.1, 8.12.2 |
| undici | 6.29.0 | CVE-2026-109932 | MEDIUM | 6.29.1, 7.30.1, 8.12.2 |

### 後端映像：Ubuntu 系統套件

| 套件 | 安裝版本 | 漏洞 | 嚴重度 | 報告列出的修補版本 |
|---|---|---|---|---|
| dash | 0.5.12-6ubuntu5 | CVE-2026-102473 | MEDIUM | 未列出 |
| dash | 0.5.12-6ubuntu5 | CVE-2026-102474 | MEDIUM | 未列出 |
| git | 1:2.43.0-1ubuntu7.4 | CVE-2024-52005 | MEDIUM | 未列出 |
| git-man | 1:2.43.0-1ubuntu7.4 | CVE-2024-52005 | MEDIUM | 未列出 |
| libc-bin | 2.39-0ubuntu8.9 | CVE-2026-18374 | MEDIUM | 未列出 |
| libc-bin | 2.39-0ubuntu8.9 | CVE-2026-89092 | MEDIUM | 未列出 |
| libc6 | 2.39-0ubuntu8.9 | CVE-2026-18374 | MEDIUM | 未列出 |
| libc6 | 2.39-0ubuntu8.9 | CVE-2026-89092 | MEDIUM | 未列出 |
| libexpat1 | 2.6.1-2ubuntu0.6 | CVE-2025-66382 | MEDIUM | 未列出 |
| libgnutls30t64 | 3.8.3-1.1ubuntu3.6 | CVE-2026-67693 | MEDIUM | 未列出 |
| libgnutls30t64 | 3.8.3-1.1ubuntu3.6 | CVE-2026-88647 | MEDIUM | 未列出 |
| libgnutls30t64 | 3.8.3-1.1ubuntu3.6 | CVE-2026-88648 | MEDIUM | 未列出 |
| libgnutls30t64 | 3.8.3-1.1ubuntu3.6 | CVE-2026-95184 | MEDIUM | 未列出 |
| libgnutls30t64 | 3.8.3-1.1ubuntu3.6 | CVE-2026-95210 | MEDIUM | 未列出 |
| libicu74 | 74.2-1ubuntu3.1 | CVE-2025-5222 | LOW | 未列出 |
| libsasl2-2 | 2.1.28+dfsg1-5ubuntu3.1 | CVE-2026-107161 | MEDIUM | 未列出 |
| libsasl2-modules-db | 2.1.28+dfsg1-5ubuntu3.1 | CVE-2026-107161 | MEDIUM | 未列出 |
| libsystemd0 | 255.4-1ubuntu8.18 | CVE-2026-40228 | LOW | 未列出 |
| libudev1 | 255.4-1ubuntu8.18 | CVE-2026-40228 | LOW | 未列出 |
| login | 1:4.13+dfsg1-4ubuntu3.2 | CVE-2024-56433 | LOW | 未列出 |
| passwd | 1:4.13+dfsg1-4ubuntu3.2 | CVE-2024-56433 | LOW | 未列出 |
| tar | 1.35+dfsg-3ubuntu0.4 | CVE-2026-18477 | MEDIUM | 未列出 |
| tar | 1.35+dfsg-3ubuntu0.4 | CVE-2026-18508 | MEDIUM | 未列出 |
| wget | 1.21.4-1ubuntu4.5 | CVE-2021-31879 | MEDIUM | 未列出 |

### 歷史資料庫映像：gosu 內含依賴（第二次修補已排除）

| 套件 | 安裝版本 | 漏洞 | 嚴重度 | 報告列出的修補版本 |
|---|---|---|---|---|
| github.com/moby/sys/user | v0.1.0 | CVE-2026-61801 | MEDIUM | 0.4.1 |
| golang.org/x/sys | v0.1.0 | CVE-2026-39824 | UNKNOWN | 0.44.0 |
| stdlib | v1.24.6 | CVE-2025-68121 | CRITICAL | 1.24.13, 1.25.7, 1.26.0-rc.3 |
| stdlib | v1.24.6 | CVE-2025-61726 | HIGH | 1.24.12, 1.25.6 |
| stdlib | v1.24.6 | CVE-2025-61729 | HIGH | 1.24.11, 1.25.5 |
| stdlib | v1.24.6 | CVE-2026-111943 | HIGH | 1.26.10, 1.27.3 |
| stdlib | v1.24.6 | CVE-2026-25679 | HIGH | 1.25.8, 1.26.1 |
| stdlib | v1.24.6 | CVE-2026-27145 | HIGH | 1.25.11, 1.26.4 |
| stdlib | v1.24.6 | CVE-2026-32280 | HIGH | 1.25.9, 1.26.2 |
| stdlib | v1.24.6 | CVE-2026-32281 | HIGH | 1.25.9, 1.26.2 |
| stdlib | v1.24.6 | CVE-2026-32283 | HIGH | 1.25.9, 1.26.2 |
| stdlib | v1.24.6 | CVE-2026-33811 | HIGH | 1.25.10, 1.26.3 |
| stdlib | v1.24.6 | CVE-2026-33814 | HIGH | 1.25.10, 1.26.3 |
| stdlib | v1.24.6 | CVE-2026-33818 | HIGH | 1.25.13, 1.26.6, 1.27.0-rc.3 |
| stdlib | v1.24.6 | CVE-2026-39820 | HIGH | 1.25.10, 1.26.3 |
| stdlib | v1.24.6 | CVE-2026-39821 | HIGH | 1.25.13, 1.26.6, 1.27.0-rc.3 |
| stdlib | v1.24.6 | CVE-2026-39822 | HIGH | 1.25.12, 1.26.5, 1.27.0-rc.2 |
| stdlib | v1.24.6 | CVE-2026-39836 | HIGH | 1.25.10, 1.26.3 |
| stdlib | v1.24.6 | CVE-2026-42499 | HIGH | 1.25.10, 1.26.3 |
| stdlib | v1.24.6 | CVE-2026-42504 | HIGH | 1.25.11, 1.26.4 |
| stdlib | v1.24.6 | CVE-2026-56853 | HIGH | 1.25.13, 1.26.6, 1.27.0-rc.3 |
| stdlib | v1.24.6 | CVE-2026-56858 | HIGH | 1.25.13, 1.26.6, 1.27.0-rc.3 |
| stdlib | v1.24.6 | CVE-2026-56859 | HIGH | 1.25.13, 1.26.6, 1.27.0-rc.3 |
| stdlib | v1.24.6 | CVE-2026-56860 | HIGH | 1.25.13, 1.26.6, 1.27.0-rc.3 |
| stdlib | v1.24.6 | CVE-2026-56862 | HIGH | 1.25.13, 1.26.6, 1.27.0-rc.3 |
| stdlib | v1.24.6 | CVE-2026-78667 | HIGH | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2026-78669 | HIGH | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2026-97031 | HIGH | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2025-47912 | MEDIUM | 1.24.8, 1.25.2 |
| stdlib | v1.24.6 | CVE-2025-58183 | MEDIUM | 1.24.8, 1.25.2 |
| stdlib | v1.24.6 | CVE-2025-58185 | MEDIUM | 1.24.8, 1.25.2 |
| stdlib | v1.24.6 | CVE-2025-58187 | MEDIUM | 1.24.9, 1.25.3 |
| stdlib | v1.24.6 | CVE-2025-58188 | MEDIUM | 1.24.8, 1.25.2 |
| stdlib | v1.24.6 | CVE-2025-58189 | MEDIUM | 1.24.8, 1.25.2 |
| stdlib | v1.24.6 | CVE-2025-61723 | MEDIUM | 1.24.8, 1.25.2 |
| stdlib | v1.24.6 | CVE-2025-61724 | MEDIUM | 1.24.8, 1.25.2 |
| stdlib | v1.24.6 | CVE-2025-61725 | MEDIUM | 1.24.8, 1.25.2 |
| stdlib | v1.24.6 | CVE-2025-61727 | MEDIUM | 1.24.11, 1.25.5 |
| stdlib | v1.24.6 | CVE-2025-61728 | MEDIUM | 1.24.12, 1.25.6 |
| stdlib | v1.24.6 | CVE-2025-61730 | MEDIUM | 1.24.12, 1.25.6 |
| stdlib | v1.24.6 | CVE-2026-27142 | MEDIUM | 1.25.8, 1.26.1 |
| stdlib | v1.24.6 | CVE-2026-32282 | MEDIUM | 1.25.9, 1.26.2 |
| stdlib | v1.24.6 | CVE-2026-32288 | MEDIUM | 1.25.9, 1.26.2 |
| stdlib | v1.24.6 | CVE-2026-32289 | MEDIUM | 1.25.9, 1.26.2 |
| stdlib | v1.24.6 | CVE-2026-39823 | MEDIUM | 1.25.10, 1.26.3 |
| stdlib | v1.24.6 | CVE-2026-39825 | MEDIUM | 1.25.10, 1.26.3 |
| stdlib | v1.24.6 | CVE-2026-39826 | MEDIUM | 1.25.10, 1.26.3 |
| stdlib | v1.24.6 | CVE-2026-42505 | MEDIUM | 1.25.12, 1.26.5, 1.27.0-rc.2 |
| stdlib | v1.24.6 | CVE-2026-42507 | MEDIUM | 1.25.11, 1.26.4 |
| stdlib | v1.24.6 | CVE-2026-56866 | MEDIUM | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2026-78659 | MEDIUM | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2026-78660 | MEDIUM | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2026-78663 | MEDIUM | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2026-94439 | MEDIUM | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2026-94440 | MEDIUM | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2026-94448 | MEDIUM | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2026-97030 | MEDIUM | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2026-97032 | MEDIUM | 1.26.9, 1.27.2 |
| stdlib | v1.24.6 | CVE-2025-58186 | LOW | 1.24.8, 1.25.2 |
| stdlib | v1.24.6 | CVE-2026-27139 | LOW | 1.25.8, 1.26.1 |
| stdlib | v1.24.6 | CVE-2026-56857 | LOW | 1.26.9, 1.27.2 |

## 後續限制與交接

- 應用程式的 npm／NuGet 直接與間接依賴複掃未回報已知漏洞，但不涵蓋未知漏洞或業務邏輯問題。
- CI 的 AMD64 平台尚未複掃，GitHub Actions 尚未實際執行。
- NuGet 鎖定檔尚待使用者確認；未把本機安裝套件清單當成專案依賴。
- 三個本機服務已使用修補映像且可連線，既有資料 Volume 保留；未提交、推送或部署至線上。

## 最新交接狀態

僅針對仍有漏洞的項目處理，沒有升級 Node.js、.NET、PostgreSQL 主版本。第二階段實際變更為 `docker/db.Dockerfile` 及本文件；npm 12.2.1 只建了隔離評估映像，未套用。資料庫目前的 Trivy 報告為 0，仍需持續更新漏洞資料庫追蹤；npm 保留 8 筆，Ubuntu 保留 24 筆。本次未新增忽略規則或隱藏未修補項目。變更透過獨立安全修補分支提出至 dev；提交與 PR 狀態以儲存庫紀錄為準。雲端 CI／AMD64 及未知漏洞仍不在已完成驗證範圍。
