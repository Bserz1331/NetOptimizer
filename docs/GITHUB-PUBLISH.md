# GitHub 發布準備

本專案已將原始碼、文件、建置腳本與 GitHub Actions 放在版本控制範圍；`build\`、`dist\`、`src\dist\` 與 `archive\` 會被 `.gitignore` 排除，避免把本機測試輸出、歷史二進位檔或原始桌面副本上傳。

## 建議發布方式

1. 建立 repository，建議名稱為 `NetOptimizer`。
2. 推送原始碼與文件到 `main`。
3. 建立 tag `v3.0.11`。
4. 將 `dist\NetOptimizer-v3.0.11-win-x64.zip` 與 `dist\SHA256SUMS-v3.0.11.txt` 附加到 GitHub Release；需要時再附上 EXE、PDB 與 source ZIP。
5. 確認 GitHub Actions 的 Windows build 通過後，再公開 Release。

## 發布前必須決定

- repository 要公開或私人。
- 是否採用 MIT、Apache-2.0 或其他授權；目前尚未放入 `LICENSE`，公開原始碼不等於授權他人使用或再發布。
- 是否將 PDB 與 source ZIP 一起提供給使用者。

## 不應提交的資料

- `%LOCALAPPDATA%\NetOptimizer\settings.xml`
- `startup-error.log`
- 個人網路診斷輸出
- `build\` 測試快照與暫存輸出
- `archive\original\` 與歷史二進位檔
