# NetOptimizer

<p align="center">
  <img src="src/assets/NetOptimizer-logo.png" alt="NetOptimizer 標誌" width="96">
</p>

<p align="center">Windows 網路監測、雙線路備援與可選的慢速智慧選路工具。</p>

<p align="center">
  <a href="https://github.com/Bserz1331/NetOptimizer/releases/latest">下載最新版 v3.0.17</a> ·
  <a href="README.en.md">English</a> ·
  <a href="LICENSE">MIT License</a>
</p>

NetOptimizer 會定期檢查指定的 TCP 目標，發現主要網路 A 異常時，在確認備援網路 B 可用後切換路由，並在 A 穩定後切回。它的重點是降低斷線時的人工處理與反覆切換風險。

它不是 ISP 加速器，也不會把兩條網路合併成一條更大的頻寬；實際效果仍取決於 Windows 網路設定、路由器、VPN、驅動程式與線路品質。

## 下載與開始使用

1. 到 [GitHub Releases](https://github.com/Bserz1331/NetOptimizer/releases/latest) 開啟最新版。
2. 在 Assets 下載 NetOptimizer-v3.0.17-win-x64.zip。
3. 解壓縮後直接執行 NetOptimizer-v3.0.17.exe，不需要安裝程式。
4. 在主畫面選擇主要網路 A；如果有第二條就緒線路，再選擇備援網路 B。
5. 確認設定後按下「開始自動保護」。

Wi-Fi、藍牙網路連線與乙太網路都可以作為 A 或 B；程式會依 Windows 實際偵測到的介面顯示對應名稱與圖示。沒有第二條就緒線路時，B 會保持空白，自動切換備援也會預設關閉。

目前 v3.0.17 的 EXE 沒有附 Authenticode 數位簽章，Windows 可能顯示來源警告。請只從官方 Release 下載，並可使用同頁的 SHA256 checksum 驗證檔案。

## 主畫面怎麼看

| 區域 | 意義 |
| --- | --- |
| 主要網路 A | 目前優先使用的線路。 |
| 備援網路 B | A 異常時可接手的第二條線路；只有就緒介面才適合使用。 |
| 最新延遲／健康度 | 最近一次探測結果，不代表所有網站或服務都正常。 |
| 自動切換備援 | A 連續異常時，依安全檢查決定是否切換到 B。 |
| 自動修復 | 異常時選擇性執行 DNS、ARP 或 MTU refresh。 |
| 齒輪圖示 | 開啟獨立的進階設定視窗。 |
| 執行紀錄 | 在主畫面下方展開紀錄，查看實際動作與錯誤。 |

## 自動切換怎麼運作

1. 程式從 A 的來源 IPv4 探測指定 TCP 目標。
2. A 連續失敗達到門檻時，先重新確認 B 具有 IPv4、預設 gateway，且健康檢查通過。
3. 只有在 InterfaceMetric（Windows 的介面優先順序）寫入成功、讀回一致，並確認預設路由符合預期後，才視為切換成功。
4. A 恢復並保持穩定一段時間後，才會切回 A。
5. 如果 B 不存在、不是 IsReady（有 IPv4 與預設 gateway）、健康檢查失敗或權限不足，程式會停止這次切換，不會把失敗當成成功。

下拉選單會排除 NotPresent（目前不可用）的介面。這代表未就緒的介面不會被自動選成備援，但不代表 Windows 一定能連上網際網路；健康判定仍以你設定的 TCP 目標為準。

## 慢速 EWMA 智慧選路

這是可選功能，預設關閉。當 A 與 B 都健康時，它會用較慢的週期累積延遲、封包遺失與 jitter，只有差距持續超過門檻並符合停留時間時，才考慮選擇較佳線路。

它不會取代「A 故障才切換 B」的保護邏輯，也不適合需要頻繁換路由的情境。重視穩定性時請保持關閉；兩條線路都穩定且延遲差異確實有意義時，再從進階設定開啟。

## 重要權限與限制

- 一般 TCP 監測通常可在一般使用者權限執行。
- A/B 的 IPv4 InterfaceMetric（Windows 的介面優先順序）變更、ARP 操作與 MTU 操作通常需要系統管理員權限。程式不會偷偷提權；權限不足時會阻止 A/B 啟動，並提供「重新以管理員啟動」入口。
- A/B 變更路由時可能出現短暫連線轉換；第一次實測請保留復原資料，並在可接受短暫斷線的環境測試。
- 「健康」只表示你設定的 TCP 目標符合目前策略，不保證每個網站、遊戲伺服器、VPN 或應用程式都能使用。
- 程式只會調整 Windows 的介面優先順序，不會同時分配單一連線的流量，也不會取代 VPN、路由器或網卡驅動程式。

## 語言、開機與更新

- 右上角語言按鈕可切換「繁體中文」與 English。
- 第一次啟動時，Windows 的 zh-* 語系預設繁體中文，其他語系預設 English；既有設定與手動選擇不會被覆蓋。
- 「開機自動啟動」預設關閉。安裝在 Program Files 的版本可在一次 UAC 同意後建立高權限工作排程；portable 版本則使用目前使用者的啟動項目，不會自動提權。
- 程式會在背景檢查 GitHub 最新穩定 Release。檢查成功間隔為 24 小時；只提示新版本，不會自動下載、執行或覆蓋 EXE。
- 最小化後可留在系統匣；雙擊系統匣圖示可恢復視窗。

## 常見問題

### 備援 B 為什麼是空白？

請確認第二條網路已連線，並且有 IPv4 與預設 gateway。按「重新偵測網路」後，只有 IsReady（有 IPv4 與預設 gateway）的介面會出現在可用選項；沒有第二條就緒線路時，程式會保留空白並關閉自動備援切換。

### 為什麼 A/B 不能開始？

請按「重新以管理員啟動」，再重新選擇 A、B。一般監測不一定需要管理員權限，但實際改變介面 metric、ARP 或 MTU 通常需要。

### 為什麼說程式已經在執行？

程式採單一執行個體設計。重複啟動時，新的啟動器會嘗試把既有視窗帶回前景，不會建立第二個監測器；若視窗沒有出現，請先查看系統匣或工作管理員中的 NetOptimizer。

### 看到 InterfaceMetric 錯誤怎麼辦？

先確認已使用管理員權限啟動，再查看 [InterfaceMetric 錯誤說明](docs/INTERFACEMETRIC-ERROR.md)。仍無法判斷時，從介面匯出診斷；診斷資料可能包含介面名稱、IPv4、gateway、metric 與預設 route，分享前請先遮蔽不想公開的資訊。

## 隱私與資料

- 不包含遙測或背景回報功能。
- 更新檢查只連線到 GitHub Release API；失敗不會阻止一般監測。
- 設定與更新檢查狀態保存在 %LOCALAPPDATA%\NetOptimizer\。
- 診斷匯出只在使用者主動操作時產生，且可能包含本機網路資訊。
- 支持開發視窗只在使用者按下按鈕後開啟，不會自動付款或在背景回報。

## 支持開發

程式內的「支持開發」按鈕會開啟 Ko-fi 與 USDT 支持選項。Ko-fi 也可以直接前往 [ko-fi.com/minz_space_cat](https://ko-fi.com/minz_space_cat)；加密貨幣轉帳前請確認使用的是正確網路，並先小額測試。

## 給開發者

<details>
<summary>建置、測試與文件</summary>

### 建置

在 Windows PowerShell 執行：

~~~powershell
.\build.ps1 -Version 3.0.17 -OutputDirectory .\dist
~~~

程式使用 Windows x64 的 .NET Framework C# 編譯器與 Windows SDK 資源建置。程式碼簽章是選用功能；沒有憑證時，建置腳本會明確顯示 skipped。

### 基本驗證

~~~powershell
.\dist\NetOptimizer-v3.0.17.exe --self-test
.\dist\NetOptimizer-v3.0.17.exe --failover-simulation
.\dist\NetOptimizer-v3.0.17.exe --interface-probe-test
.\dist\NetOptimizer-v3.0.17.exe --interface-metric-test
.\dist\NetOptimizer-v3.0.17.exe --ui-layout-test
.\dist\NetOptimizer-v3.0.17.exe --gui-startup-test
.\dist\NetOptimizer-v3.0.17.exe --update-check-test
.\dist\NetOptimizer-v3.0.17.exe --soak-test --seconds=60
~~~

interface-probe-test 只做唯讀探測，不會修改 route、metric、DNS 或 MTU。interface-metric-test 只讀取 metric，通常需要管理員權限；真正的 A/B 路由切換仍應在可接受短暫連線變化的測試環境中手動驗證。

### 文件

- [進階原始碼與功能說明](src/README.md)
- [InterfaceMetric 診斷](docs/INTERFACEMETRIC-ERROR.md)
- [開源邊界](docs/OPEN-SOURCE-BOUNDARY.md)
- [Release v3.0.17 notes](docs/RELEASE-v3.0.17.md)
- [GitHub 發布流程](docs/GITHUB-PUBLISH.md)

</details>

## 授權

公開核心採用 [MIT License](LICENSE)。未來的付費授權、雲端服務或客製整合，會依 [開源邊界](docs/OPEN-SOURCE-BOUNDARY.md) 與公開核心分離。
