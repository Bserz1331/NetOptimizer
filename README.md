# NetOptimizer

English documentation: [README.en.md](README.en.md)

Windows 網路監測、A/B 備援切換與慢速 EWMA 智慧選路工具。

目前開發版本為 v3.0.12：新增繁體中文／English 介面切換，語言偏好會保存到設定檔；並延續 v3.0.11 的網路自動偵測與 A/B 啟動安全：備援只會選擇 `IsReady` 線路、排除 `NotPresent` 下拉項目，沒有第二條就緒線路時會清空備援並提示；非管理員不會啟動 A/B。

## 專案結構

- `src\`：C# 原始碼、資源檔與底層建置腳本。
- `build\`：本機編譯、測試與畫面快照；不提交到 GitHub。
- `dist\`：本機 Release 輸出；不提交到 GitHub，內容附加到 GitHub Release。
- `docs\`：錯誤診斷與操作補充。
- `docs\OPEN-SOURCE-BOUNDARY.md`：公開核心與未來商業模組的發布邊界。
- `archive\`：舊版與原始桌面副本，僅供本機回溯，不提交到 GitHub。
- `README.en.md`：英文功能、建置與安全說明。

GitHub 發布步驟請看 [GitHub 發布準備](docs/GITHUB-PUBLISH.md)；v3.0.11 變更與驗證請看 [Release notes](docs/RELEASE-v3.0.11.md)。

## 建置

在本目錄的 Windows PowerShell 執行：

```powershell
.\build.ps1 -Version 3.0.12 -OutputDirectory .\dist
```

簽章憑證可選；沒有憑證時會保持未簽章並明確顯示 skipped：

```powershell
  .\build.ps1 -Version 3.0.12 `
  -SigningCertificateThumbprint "憑證指紋" `
  -TimestampUrl "https://你的時間戳服務"
```

## 測試

```powershell
.\dist\NetOptimizer-v3.0.12.exe --self-test
.\dist\NetOptimizer-v3.0.12.exe --failover-simulation
.\dist\NetOptimizer-v3.0.12.exe --interface-probe-test
.\dist\NetOptimizer-v3.0.12.exe --diagnostics-test
.\dist\NetOptimizer-v3.0.12.exe --interface-metric-test
.\dist\NetOptimizer-v3.0.12.exe --ui-layout-test
.\dist\NetOptimizer-v3.0.12.exe --gui-startup-test
.\dist\NetOptimizer-v3.0.12.exe --support-snapshot=.\build\support.png
.\dist\NetOptimizer-v3.0.12.exe --soak-test --seconds=60
```

## 語言

啟動程式後，使用右上角的語言下拉選單切換 `繁體中文` 或 `English`。切換會立即套用到主畫面、系統匣選單與支持開發視窗，並保存到 `%LOCALAPPDATA%\NetOptimizer\settings.xml`。網卡名稱、測試目標與診斷資料保持原始內容。

`--interface-probe-test` 只做唯讀的來源 IPv4 TCP 探測，不會改動 metric、route、DNS 或 MTU。

## 截圖中的 InterfaceMetric 錯誤

請先看 [InterfaceMetric 錯誤說明](docs/INTERFACEMETRIC-ERROR.md)。簡單說，舊版讀取 `Get-NetIPInterface` 後只接受固定的 `數字|True/False` 格式；若 PowerShell 輸出格式不同，就會出現「無法解析 InterfaceMetric」。v3.0.1 已修正解析格式；v3.0.2 再修正 `Set-NetIPInterface` 在目前 Windows 上要求 `Disabled/Enabled` 列舉值的問題，並讓 CLIXML 顯示真正錯誤、修正 log 重複時間戳。

A/B 實際改變 IPv4 InterfaceMetric、讀取預設 route、ARP 或 MTU 通常需要以系統管理員身分啟動。程式不會自動提權；v3.0.11 在權限列提供重新啟動與支持開發入口，非管理員勾選 A/B 時會直接阻止開始監測。

完整功能與安全邊界請看 [`src\README.md`](src/README.md)。

## 授權

本 Repository 的公開核心採用 [MIT License](LICENSE)。未來若加入付費授權、雲端服務或客製整合，會依 [開源邊界](docs/OPEN-SOURCE-BOUNDARY.md) 與公開核心分離；不會把私密金鑰放進 EXE 或公開原始碼。
