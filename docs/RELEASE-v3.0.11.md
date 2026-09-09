# NetOptimizer v3.0.11

## 這個版本

- 自動偵測的 A/B 只會選擇 `IsReady` 網路介面。
- 沒有第二條就緒線路時，備援 B 保持空白並提示使用者。
- 所有介面下拉選單排除 `NotPresent`。
- 非管理員啟動 A/B 會在監測引擎開始前被阻止，並提供重新以管理員啟動入口。
- 支持開發入口位於權限列，提供 Ko-fi、USDT BEP20／TRC20 與複製地址。

## Release assets

- `NetOptimizer-v3.0.11-win-x64.zip`：一般使用者下載這個可攜版。
- `NetOptimizer-v3.0.11.exe`：單獨執行檔。
- `SHA256SUMS-v3.0.11.txt`：檔案完整性核對。
- `NetOptimizer-v3.0.11-source.zip`：需要完整原始碼壓縮包時提供。

## 已驗證

- 核心自測、故障切換模擬、診斷測試：通過。
- UI layout、GUI startup：通過。
- 目前 Wi-Fi 實際 TCP probe：2/2 成功。
- Portable ZIP 解出後 UI layout test：通過。
- Source ZIP 乾淨編譯：通過。

## 注意

A/B 的 InterfaceMetric、route、ARP 與 MTU 動作通常需要系統管理員權限。程式不會偷偷提權；使用者應透過介面上的管理員重啟入口重新啟動。
