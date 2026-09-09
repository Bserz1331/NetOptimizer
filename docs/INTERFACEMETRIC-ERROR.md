# InterfaceMetric 錯誤說明

截圖中的訊息：

`A/B 切換未啟動：無法讀取 A 的 InterfaceMetric（無法解析 InterfaceMetric）。`

代表程式呼叫 `Get-NetIPInterface` 後，命令本身沒有回報失敗，但回傳文字沒有符合舊版 C# 的固定正規表示式。舊版只接受：

`數字|True` 或 `數字|False`

因此這個訊息不等於「一定沒有管理員權限」。若是權限問題，通常會在前一步得到 `拒絕存取`；若是輸出格式、欄位為空或 Windows／PowerShell 版本差異，則可能落到「無法解析」。

## v3.0.2 的修正

- PowerShell 改輸出帶有 `NETOPTIMIZER_METRIC|` 標記的格式。
- 數字使用 invariant culture，避免地區設定影響解析。
- C# 同時支援新格式與舊的 `數字|True/False` 格式。
- `Set-NetIPInterface` 改用這台 Windows 實際要求的 `Disabled/Enabled` 列舉值，不再傳入 `$false/$true`。
- CLIXML 錯誤會先解析出真正的 PowerShell 錯誤，不再只顯示 `#< CLIXML`。
- 啟動與測試模式都有例外邊界，不再把未處理 CLR 例外直接交給 Windows 對話框。
- A/B log 不再重複加兩次時間戳。

## 截圖中的另一組 A/B 錯誤

若 log 顯示：

`A/B 初始化：A=5 故障（#< CLIXML）`

接著出現切換或復原失敗，這不是單憑 `#< CLIXML` 就能判定為 Wi-Fi 或藍牙網路中斷。`#< CLIXML` 是 Windows PowerShell 將錯誤串流序列化後的標記，舊版只取第一行，因而把真正原因遮住。

針對這次案例，實際重現舊版命令後確認有兩個需要分開看的條件：

- 舊版把 `-AutomaticMetric` 寫成 Boolean `$false/$true`；目前這台 Windows 的參數型別是列舉，只接受 `Disabled/Enabled`。v3.0.2 已改正。
- 真正寫入或讀取介面 metric 通常仍需要系統管理員權限。非管理員測試時會得到 `PermissionDenied` / `HRESULT 0x80041003`；v3.0.2 會把它顯示成明確的權限提示，而不是 `#< CLIXML`。

所以實際使用 v3.0.11 時，請用「以系統管理員身分執行」啟動，再重新測試 A=Wi-Fi、B=藍牙網路連線。若仍失敗，請匯出診斷檔，才能再區分權限、介面狀態、gateway 或 route metric 的問題。

## 使用前檢查

1. 以「系統管理員身分」啟動 `NetOptimizer-v3.0.11.exe`，或使用介面上的「以管理員重新啟動」。
2. A 選 `Wi-Fi`，B 選 `藍牙網路連線`，確認兩者都有 IPv4 與 gateway。
3. 先使用 `--interface-probe-test` 驗證兩張線路的來源綁定 TCP 探測。
4. 再勾選 A/B 自動切換；第一次實測請保留目前 metric，讓 recovery journal 能在停止時還原。

若仍發生錯誤，請從介面按「匯出診斷」，或提供：

`%LOCALAPPDATA%\NetOptimizer\startup-error.log`

診斷檔可能包含介面名稱、IPv4、gateway、metric 與預設 route；分享前請先確認是否要遮蔽這些資訊。
