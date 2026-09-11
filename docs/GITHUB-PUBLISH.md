# GitHub 發布準備

本專案已將原始碼、文件、建置腳本與 GitHub Actions 放在版本控制範圍；`build\`、`dist\`、`src\dist\` 與 `archive\` 會被 `.gitignore` 排除，避免把本機測試輸出、歷史二進位檔或原始桌面副本上傳。

## 建議發布方式

1. 建立 repository，建議名稱為 `NetOptimizer`。
2. 推送原始碼與文件到 `main`。
3. 建立 tag `v3.0.13`。
4. 將 `dist\NetOptimizer-v3.0.13-win-x64.zip` 與 `dist\SHA256SUMS-v3.0.13.txt` 附加到 GitHub Release；需要時再附上 EXE、PDB 與 source ZIP。
5. 確認 GitHub Actions 的 Windows build 通過後，再公開 Release。

## 目前發布決定

- repository 已採公開，名稱為 `NetOptimizer`。
- 公開核心採用根目錄 `LICENSE` 的 MIT License。
- 未來付費授權、雲端服務與商業整合依 [開源邊界](OPEN-SOURCE-BOUNDARY.md) 分離，不放入公開核心。
- 是否將 PDB 與 source ZIP 一起提供給使用者。

## 不應提交的資料

- `%LOCALAPPDATA%\NetOptimizer\settings.xml`
- `startup-error.log`
- 個人網路診斷輸出
- `build\` 測試快照與暫存輸出
- `archive\original\` 與歷史二進位檔
