# NetOptimizer v3.0.14

NetOptimizer 是 Windows 上的網路監測、一般刷新與 A/B 備援切換工具。v3 將原本的 NetOptimizer 與 DoubleNet 邏輯整合，保留手動控制，並把長時間執行、失敗復原與可驗證性放在優先位置。

## 已實作功能

- 原生 TCP 探測：每次探測都有 timeout、取消與 `TcpClient.Dispose`，可選擇來源 IPv4，避免依賴 `ping.exe` 與無限等待。
- 一般監測：連續異常才刷新，刷新有 cooldown；DNS、ARP、MTU pulse 可分開停用。
- A/B 故障切換：A 連續異常時，先確認 B 具備 IPv4、gateway 且通過多目標健康檢查，再調整兩張網卡的 IPv4 `InterfaceMetric`。
- 切換後讀回驗證：設定 metric 後會重新讀取，並嘗試確認最低預設 route 使用預期介面。
- Crash-safe recovery journal：變更 metric 前先寫入 `%LOCALAPPDATA%\NetOptimizer\failover-recovery.xml`；程式重新啟動時會先嘗試復原。若目前 metric 已被使用者改成非本程式管理的值，程式不會覆蓋它。
- A 恢復切回：原主線通過可調整的穩定時間後才切回 A。
- 失敗保護：切換失敗有 backoff，每小時有切換次數上限，降低雙線路抖動與反覆改 route 的風險。
- 動作協調：A/B 就緒後可暫停一般自動刷新；「立即刷新」仍可手動執行，避免兩個背景工作同時改動網路。
- 單一實例：重複開啟時不再建立第二個背景監測器。
- 狀態面板：顯示目前模式、主線／備援、兩條線路最近健康狀態、上次切換原因與次數。
- 新手儀表板：移除重複的目前狀態卡與多餘空間，以目前連線、備援網路與健康度集中呈現；保留主要／備援網路選擇與一鍵開始、偵測、復原、查看紀錄。
- 新手模式：預設只顯示一般使用者需要的選項；可一鍵切換進階模式，原有監測、A/B、EWMA 與診斷選項仍完整保留。
- 診斷匯出：可輸出設定、介面、MTU、metric、預設 route、A/B 狀態與最近 log；匯出前會提醒其中包含網路資訊。
- 支持開發視窗：只在使用者按下時開啟，提供 Ko-fi、USDT BEP20／TRC20、複製地址與轉帳網路提醒；不自動開啟、不包含遙測。
- 介面修正：恢復監測設定與異常時動作區塊，補上管理員重新啟動入口、介面資訊提示與 log 右鍵操作。
- 品牌圖示與系統匣：標題區顯示 NetOptimizer 圖示；最小化後隱藏主視窗並保留系統匣操作，雙擊圖示可恢復。
- 開機自動啟動：預設關閉。Program Files 安裝版啟用時會經一次 UAC 建立 `NetOptimizer Elevated Startup` 工作排程器，使用最高權限於登入後執行 `--startup --auto-start` 並開始監測；portable 版則使用目前使用者的 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`，登入後縮到系統匣，不會自動提權。高權限工作不會指向桌面、下載等使用者可寫入路徑。
- 可縮放布局：新手儀表板依內容自動高度，預設視窗高度貼合內容；支持開發與管理員重啟入口集中在權限列，未使用的頁尾列在新手模式收合；進階設定與執行紀錄改用自動布局，內容區可捲動、底部操作列固定可見，A/B 進階參數預設收合，支援調整視窗大小與每螢幕 DPI。
- 自動偵測安全選路：自動選出的 A/B 僅來自 `IsReady` 介面；找不到第二條就緒線路時 B 保持空白並提示使用者；介面下拉清單排除 `NotPresent`。
- 權限閘門：非管理員啟動 A/B 會在監測引擎開始前被阻止，並聚焦「重新以管理員啟動」入口。
- 語言切換：右上角可切換 `繁體中文`／`English`；偏好會保存到設定檔，並套用到主視窗、系統匣與支持開發視窗。
- 更新提示：啟動後背景檢查 GitHub 最新穩定 Release，成功檢查間隔 24 小時；有新版本時顯示標題區與系統匣提示，並可手動查看或忽略版本。不自動下載、執行或覆蓋 EXE。

## 慢速 EWMA 智慧選路

這是可選功能，預設關閉。開啟後，程式會以慢速週期累積每條線路的 EWMA 延遲、loss 與 jitter，使用可解釋的分數比較線路：

`score = EWMA latency + EWMA loss × timeout + EWMA jitter`

只有在候選線路優於目前線路超過 margin、持續 hold 時間、且已超過最小停留時間時才切換；同時受每小時切換上限與 backoff 保護。預設參數為 alpha 0.15、決策週期 30 秒、最小停留 30 秒、hold 10 秒、margin 8 ms、每小時最多 6 次。

它不會取代「A 壞掉才切 B」的故障保護，而是額外提供「兩條都正常時，較慢速地選較佳線路」。若你只要穩定備援，建議保持關閉；若兩條線路都能穩定上網且延遲差異有意義，再開啟並觀察匯出的 diagnostics。

## 重要限制與安全邊界

- A/B metric 變更、ARP 與 MTU 通常需要系統管理員權限。程式不會偷偷提權；無法讀取或寫入時會記錄錯誤並停止 A/B 動作。
- A/B 預設關閉。啟用前請確認 A、B 名稱、測試目標與 metric。一般設定要求主線 metric 小於備援 metric。
- 測試目標預設為 `1.1.1.1:443` 與 `8.8.8.8:443`。若要觀察特定遊戲或服務，應改成與實際路徑相符的 endpoint。
- 「健康」代表指定 TCP 目標中至少半數成功，不等同於所有網站、遊戲伺服器或 VPN 都可用。
- 這個設計避免了常見的未釋放 socket、未觀察的背景 task、重複實例與無限等待路徑，但任何 Windows 網路驅動、第三方 VPN 或外部命令都可能造成環境層級問題；正式發布前仍應做管理員權限與實際斷線演練。

## 建置與測試

在 Windows PowerShell 執行：

```powershell
./build.ps1 -Version 3.0.14 -OutputDirectory ./build
```

若已有程式碼簽章憑證，可在建置時選擇性簽章；沒有憑證時腳本會明確輸出 skipped：

```powershell
./build.ps1 -Version 3.0.14 -OutputDirectory ./build `
  -SigningCertificateThumbprint "憑證指紋" `
  -TimestampUrl "https://你的時間戳服務"
```

可執行檔支援下列檢查模式：

```powershell
./NetOptimizer-v3.0.14.exe --self-test
./NetOptimizer-v3.0.14.exe --failover-simulation
./NetOptimizer-v3.0.14.exe --interface-probe-test
./NetOptimizer-v3.0.14.exe --interface-metric-test
./NetOptimizer-v3.0.14.exe --ui-layout-test
./NetOptimizer-v3.0.14.exe --gui-startup-test
./NetOptimizer-v3.0.14.exe --update-check-test
./NetOptimizer-v3.0.14.exe --update-check-live-test
./NetOptimizer-v3.0.14.exe --support-snapshot=./build/support.png
./NetOptimizer-v3.0.14.exe --stability-test
./NetOptimizer-v3.0.14.exe --soak-test --seconds=60
```

`--interface-probe-test` 只讀取目前可用介面並以各介面的來源 IPv4 做 TCP probe，不會改 metric、MTU、DNS 或 route。真正的 A/B route 切換必須在管理員權限下、並於可接受短暫連線變化的環境進行。

`--interface-metric-test` 只讀取目前介面的 InterfaceMetric；它不會寫入 metric，適合先確認管理員權限與 PowerShell 回傳內容。

設定檔位置：`%LOCALAPPDATA%\NetOptimizer\settings.xml`。

更新檢查狀態位置：`%LOCALAPPDATA%\NetOptimizer\update-state.xml`。更新檢查使用 GitHub Release API；逾時或網路錯誤只記錄手動檢查結果，不會阻止一般監測或 A/B 功能。
