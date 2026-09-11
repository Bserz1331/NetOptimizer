using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace NetOptimizerV2
{
    public enum AppLanguage
    {
        TraditionalChinese = 0,
        English = 1
    }

    internal static class Localization
    {
        private static readonly Dictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "狀態：", "Status: " },
                { "狀態：未啟動", "Status: Not started" },
                { "未啟動", "Not started" },
                { "最近探測：", "Last probe: " },
                { "最近探測：尚未測試", "Last probe: Not tested" },
                { "尚未測試", "Not tested" },
                { "進階設定 ▸", "Advanced settings ▸" },
                { "進階設定 ▾", "Advanced settings ▾" },
                { "顯示進階設定 ▸", "Advanced settings ▸" },
                { "切換新手模式", "Beginner mode" },
                { "監測設定", "Monitoring" },
                { "網卡", "Network adapter" },
                { "測試目標", "Test targets" },
                { "連接埠", "Port" },
                { "高延遲", "High latency" },
                { "逾時", "Timeout" },
                { "連續異常", "Consecutive failures" },
                { "刷新冷卻", "Refresh cooldown" },
                { "次", "times" },
                { "秒", "sec" },
                { "異常時動作", "Actions on failure" },
                { "自動刷新", "Automatic refresh" },
                { "清除 DNS 快取", "Clear DNS cache" },
                { "清除 ARP 快取", "Clear ARP cache" },
                { "MTU 測試", "MTU pulse" },
                { "A/B 自動切換與智慧選路", "A/B failover & smart routing" },
                { "啟用 A/B 自動切換", "Enable A/B failover" },
                { "故障目標", "Failover targets" },
                { "主線 A", "Primary A" },
                { "備援 B", "Backup B" },
                { "故障門檻", "Failure threshold" },
                { "故障逾時", "Failure timeout" },
                { "EWMA 智慧選路", "EWMA smart routing" },
                { "A/B：未啟用", "A/B: Disabled" },
                { "健康度：尚未測試", "Health: Not tested" },
                { "顯示或隱藏 A/B 與 EWMA 調校參數。", "Show or hide A/B and EWMA tuning parameters." },
                { "連續失敗", "Consecutive failures" },
                { "恢復穩定", "Recovery stability" },
                { "切換冷卻", "Switch cooldown" },
                { "失敗退避", "Failure backoff" },
                { "A 優先值", "A metric" },
                { "B 優先值", "B metric" },
                { "決策間隔", "Decision interval" },
                { "最小停留", "Minimum dwell" },
                { "差距", "Margin" },
                { "選路停留", "Selection hold" },
                { "故障時停用一般刷新", "Pause normal refresh during failover" },
                { "目前使用中的介面、備援介面與切換模式。", "Active interface, backup interface, and switching mode." },
                { "完整健康度會在滑鼠停留時顯示。", "Hover to see the full health details." },
                { "系統管理員；ARP／MTU／A-B metric 動作可正常嘗試。", "Administrator; ARP, MTU, and A/B metric actions can be attempted." },
                { "一般使用者；DNS 通常可執行，ARP／MTU／A-B metric 可能需要系統管理員。", "Standard user; DNS usually works, while ARP, MTU, and A/B metric actions may require administrator privileges." },
                { "☕ 支持開發", "☕ Support" },
                { "重新以管理員啟動", "Restart as admin" },
                { "執行紀錄", "Activity log" },
                { "尚未開始監測", "Monitoring has not started" },
                { "開始監測", "Start monitoring" },
                { "停止", "Stop" },
                { "停止監測", "Stop monitoring" },
                { "立即刷新", "Refresh now" },
                { "儲存設定", "Save settings" },
                { "匯出診斷", "Export diagnostics" },
                { "顯示主視窗", "Show main window" },
                { "以系統管理員重新啟動", "Restart as admin" },
                { "支持開發", "Support development" },
                { "結束", "Exit" },
                { "快速開始", "Quick start" },
                { "目前連線", "Current connection" },
                { "尚未選擇", "Not selected" },
                { "備援網路", "Backup network" },
                { "未啟用", "Disabled" },
                { "網路中斷時自動切換備援", "Switch to backup when disconnected" },
                { "遇到問題時自動修復", "Repair when problems occur" },
                { "開始自動保護", "Start protection" },
                { "停止自動保護", "Stop protection" },
                { "重新偵測網路", "Detect networks" },
                { "復原上一筆變更", "Undo last change" },
                { "查看執行紀錄 ▸", "View log ▸" },
                { "隱藏執行紀錄 ▴", "Hide log ▴" },
                { "自動捲到最新", "Follow latest log" },
                { "複製全部紀錄", "Copy all log" },
                { "清除紀錄", "Clear log" },
                { "目前使用：", "Using: " },
                { "已選擇：", "Selected: " },
                { "待命：", "Standby: " },
                { "請選擇備援", "Choose a backup" },
                { "最近：", "Last: " },
                { "等待監測", "Waiting for monitoring" },
                { "健康度：", "Health: " },
                { " · 目前使用中", " · Active" },
                { " · 待命", " · Standby" },
                { "健康度：尚未對應", "Health: Not mapped" },
                { "先選擇備援網路", "Choose a backup network first" },
                { "A/B：未啟用或尚未就緒", "A/B: Disabled or not ready" },
                { "故障切換中", "Failing over" },
                { "智慧選路", "Smart routing" },
                { "主線優先", "Primary preferred" },
                { "目前 ", "Active " },
                { "／備援 ", " / Backup " },
                { " · 切換 ", " · Switches " },
                { "A/B 健康度：", "A/B health: " },
                { "權限：管理員", "Permission: Administrator" },
                { "權限：一般使用者", "Permission: Standard user" },
                { "開機自動啟動", "Start with Windows" },
                { "檢查更新", "Check for updates" },
                { "檢查更新中…", "Checking for updates…" },
                { "有新版本", "Update available" },
                { "查看更新", "View update" },
                { "忽略此版本", "Ignore this version" },
                { "更新", "Update" },
                { "發現 ", "Found " },
                { "，點擊查看更新。", ". Click to view the update." },
                { "檢查更新失敗", "Update check failed" },
                { "更新檢查失敗：", "Update check failed: " },
                { "請稍後再試。", "Please try again later." },
                { "目前已是最新版本。", "You are using the latest version." },
                { "無法開啟更新頁面：", "Unable to open the update page: " },
                { "已忽略版本：", "Ignored version: " },
                { "更新狀態儲存失敗：", "Failed to save update state: " },
                { "更新狀態檔是空的，已改用預設值。", "The update state file was empty; defaults were used." },
                { "讀取更新狀態失敗，已改用預設值：", "Failed to read update state; defaults were used: " },
                { "已啟用開機自動啟動。", "Start with Windows enabled." },
                { "已停用開機自動啟動。", "Start with Windows disabled." },
                { "開機自動啟動設定失敗：", "Failed to update Start with Windows: " },
                { "安裝版登入後會以系統管理員啟動並自動開始監測。", "Installed versions start with administrator privileges and begin monitoring after Windows sign-in." },
                { "可攜版登入後啟動程式並縮到系統匣。", "Portable versions start after Windows sign-in and minimize to the system tray." },
                { "正在要求系統管理員權限以設定開機自動保護。", "Requesting administrator permission to configure elevated startup." },
                { "使用者取消系統管理員權限，未變更開機自動啟動。", "Administrator permission was canceled; Start with Windows was not changed." },
                { "已啟用安裝版高權限自動保護。", "Elevated installed-version protection enabled." },
                { "完整結果：", "Full result: " },
                { "無", "None" },
                { "是", "Yes" },
                { "否", "No" },
                { "進階設定與執行紀錄。", "Advanced settings and activity log." },
                { "顯示完整監測、刷新、A/B 與 EWMA 設定。", "Show full monitoring, refresh, A/B, and EWMA settings." },
                { "回到簡化畫面，只保留一般使用者需要的選項。", "Return to the simplified view for common tasks." },
                { "NetOptimizer 網路監測與備援工具", "NetOptimizer network monitor and failover tool" },
                { "監測器目前狀態與下一次背景探測時間。", "Monitor status and the next background probe." },
                { "最近一次 TCP 探測結果。", "Most recent TCP probe result." },
                { "檢查 GitHub 是否有較新的穩定版本。", "Check GitHub for a newer stable version." },
                { "重新啟動並要求系統管理員權限；不會自動提權。", "Restart and request administrator privileges; elevation is never silent." },
                { "開啟支持開發選項：Ko-fi 與加密貨幣地址。", "Open support options: Ko-fi and cryptocurrency addresses." },
                { "可輸入 IP 或網域，使用逗號分隔。", "Enter IP addresses or hostnames separated by commas." },
                { "連續異常時執行已勾選的刷新動作。", "Run the selected refresh actions after consecutive failures." },
                { "清除 DNS 快取。", "Clear the DNS cache." },
                { "清除 ARP 快取。", "Clear the ARP cache." },
                { "暫時套用 1471 MTU，再復原原值。", "Temporarily apply MTU 1471, then restore the original value." },
                { "右鍵可複製或清除紀錄，也可暫停自動捲動。", "Right-click to copy or clear the log, or pause auto-follow." },
                { "可直接輸入 Windows 網路介面名稱。", "You can enter a Windows network adapter name." },
                { "尚未取得這張介面的 IPv4 與 gateway。", "IPv4 and gateway details are not available for this adapter." },
                { "A/B 自動切換需要系統管理員權限，已阻止啟動。", "A/B failover requires administrator privileges, so startup was blocked." },
                { "請按「重新以管理員啟動」後再開始監測。", "Click “Restart as admin” before starting monitoring." },
                { "需要管理員權限", "Administrator permission required" },
                { "匯出 NetOptimizer 診斷報告", "Export NetOptimizer diagnostics" },
                { "文字報告 (*.txt)|*.txt|所有檔案 (*.*)|*.*", "Text report (*.txt)|*.txt|All files (*.*)|*.*" },
                { "診斷報告已匯出。報告包含網卡、IP、route 與近期 log，請確認內容後再分享。", "Diagnostics exported. The report includes adapters, IPs, routes, and recent logs; review it before sharing." },
                { "探測異常：", "Probe issue: " },
                { "失敗", "Failed" },
                { "監測中", "Monitoring" },
                { "已停止", "Stopped" },
                { "下次約 ", "next in about " },
                { "監測仍在背景執行。", "Monitoring is still running in the background." },
                { "程式已縮到系統匣。", "The app was minimized to the system tray." },
                { "登入 Windows 後啟動並縮到系統匣。", "Starts after Windows sign-in and minimizes to the system tray." },
                { "選擇介面語言。", "Select the interface language." },
                { "自願支持", "Voluntary support" },
                { "透過 Ko-fi 支持", "Support via Ko-fi" },
                { "支持後續維護與改善", "Support ongoing maintenance and improvements" },
                { "開啟 ↗", "Open ↗" },
                { "轉帳前請確認網路：BEP20／TRC20。建議先小額測試。", "Verify the network before sending: BEP20 / TRC20. Test a small amount first." },
                { "複製地址", "Copy" },
                { "已複製 ✓", "Copied ✓" },
                { "無法複製地址，請稍後再試。", "Unable to copy the address. Please try again later." },
                { "無法開啟瀏覽器。", "Unable to open the browser." },
                { "設定檔是空的，已改用預設值。", "The settings file was empty; defaults were used." },
                { "讀取設定檔失敗，已改用預設值：", "Failed to read the settings file; defaults were used: " },
                { "A/B recovery 檢查失敗：", "A/B recovery check failed: " },
                { "已複製紀錄", "Log copied" },
                { "無法複製紀錄。", "Unable to copy the log." },
                { "確定要清除目前執行紀錄嗎？清除後仍可重新匯出之後的新紀錄。", "Clear the current activity log? New entries can still be exported later." },
                { "使用者取消系統管理員重新啟動。", "Administrator restart was cancelled by the user." },
                { "無法以系統管理員重新啟動：", "Unable to restart as admin: " },
                { "自動偵測未找到任何就緒網路；主要與備援已留空。", "Automatic detection found no ready networks; primary and backup were cleared." },
                { "請先連線 Wi‑Fi 或藍牙網路，再按「重新偵測網路」。", "Connect Wi-Fi or Bluetooth first, then click “Detect networks”." },
                { "已找到主要網路「", "Primary network found: “" },
                { "」，但沒有第二條就緒線路。", "”, but no second ready link was found." },
                { "備援已留空；請再連線另一條具 IPv4 與 gateway 的網路。", "Backup was left empty; connect another network with IPv4 and a gateway." },
                { "已重新偵測網路：主要「", "Networks detected again: primary “" },
                { "」；備援未找到，已留空。", "”; no backup found, so it was left empty." },
                { "」；備援「", "”; backup “" },
                { "」。", "”." },
                { "自動偵測網路失敗：", "Automatic network detection failed: " },
                { "自動偵測：沒有找到具 IPv4 與 gateway 的就緒網路。", "Automatic detection found no ready network with IPv4 and a gateway." },
                { "沒有偵測到就緒備援，自動切換已關閉。", "No ready backup was detected; automatic failover was disabled." },
                { "無法完成網路偵測。", "Unable to finish network detection." },
                { "請先停止自動保護，再執行復原。", "Stop protection before restoring the previous change." },
                { "目前沒有待復原的 A/B 網路變更。", "There is no pending A/B network change to restore." },
                { "上一筆 A/B 網路變更已完成復原。", "The previous A/B network change was restored." },
                { "目前無法完整復原上一筆變更。請以系統管理員身分重試，或查看執行紀錄。", "The previous change could not be fully restored. Retry as admin or check the activity log." },
                { "復原：", "Restore: " },
                { "手動復原失敗：", "Manual restore failed: " },
                { "復原失敗。", "Restore failed." },
                { "設定已儲存：", "Settings saved: " },
                { "儲存設定失敗：", "Failed to save settings: " },
                { "語言設定儲存失敗：", "Failed to save the language preference: " },
                { "診斷報告已匯出：", "Diagnostics exported: " },
                { "匯出診斷失敗：", "Failed to export diagnostics: " },
                { "A/B 啟動已阻止：目前不是系統管理員。", "A/B startup blocked: the current user is not an administrator." },
                { "請先選擇網卡。 ", "Select a network adapter first. " },
                { "請至少填寫一個測試目標。 ", "Enter at least one test target. " },
                { "啟用 A/B 切換時，請同時指定主線 A 與備援 B。 ", "Specify both primary A and backup B when A/B failover is enabled. " },
                { "主線 A 與備援 B 不能是同一張網卡。 ", "Primary A and backup B cannot be the same adapter. " },
                { "啟用 A/B 切換時，請至少填寫一個故障切換測試目標。 ", "Enter at least one failover test target when A/B failover is enabled. " },
                { "A metric 必須小於 B metric，才能讓 A/B 優先順序明確。 ", "A metric must be lower than B metric so the A/B priority is unambiguous. " },
                { "IPv4：", "IPv4: " },
                { "Gateway：", "Gateway: " },
                { "就緒：", "Ready: " },
                { "以系統管理員身分重試", "Retry as admin" }
            };

        public static AppLanguage DetectWindowsDefault()
        {
            if (IsChineseCulture(CultureInfo.CurrentUICulture) ||
                IsChineseCulture(CultureInfo.CurrentCulture))
            {
                return AppLanguage.TraditionalChinese;
            }

            return AppLanguage.English;
        }

        internal static AppLanguage DetectDefaultForCultureName(string cultureName)
        {
            return !string.IsNullOrWhiteSpace(cultureName) &&
                   cultureName.Trim().StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? AppLanguage.TraditionalChinese
                : AppLanguage.English;
        }

        internal static void RunSelfTest()
        {
            if (DetectDefaultForCultureName("zh-TW") != AppLanguage.TraditionalChinese ||
                DetectDefaultForCultureName("zh-CN") != AppLanguage.TraditionalChinese ||
                DetectDefaultForCultureName("zh-HK") != AppLanguage.TraditionalChinese ||
                DetectDefaultForCultureName("en-US") != AppLanguage.English ||
                DetectDefaultForCultureName("ja-JP") != AppLanguage.English ||
                DetectDefaultForCultureName(string.Empty) != AppLanguage.English)
            {
                throw new InvalidOperationException("Windows 語系預設判斷驗證失敗。");
            }

            Console.WriteLine(
                "NetOptimizer language default test: PASS (" +
                CultureInfo.CurrentUICulture.Name + " -> " + DetectWindowsDefault() + ")");
        }

        private static bool IsChineseCulture(CultureInfo culture)
        {
            return culture != null &&
                   DetectDefaultForCultureName(culture.Name) == AppLanguage.TraditionalChinese;
        }

        public static AppLanguage Normalize(AppLanguage language)
        {
            return Enum.IsDefined(typeof(AppLanguage), language)
                ? language
                : AppLanguage.TraditionalChinese;
        }

        public static string LanguageName(AppLanguage language)
        {
            return Normalize(language) == AppLanguage.English ? "English" : "繁體中文";
        }

        public static string Get(AppLanguage language, string source)
        {
            if (source == null || Normalize(language) != AppLanguage.English)
            {
                return source;
            }

            string translated;
            return English.TryGetValue(source, out translated) ? translated : source;
        }

        public static void Mark(Control control, string source)
        {
            if (control != null) { control.Tag = source; }
        }

        public static void Mark(ToolStripItem item, string source)
        {
            if (item != null) { item.Tag = source; }
        }

        public static void Apply(Control root, AppLanguage language)
        {
            if (root == null) { return; }
            string source = root.Tag as string;
            if (source != null) { root.Text = Get(language, source); }

            ToolStrip strip = root as ToolStrip;
            if (strip != null) { ApplyItems(strip, language); }
            foreach (Control child in root.Controls)
            {
                Apply(child, language);
            }
        }

        public static void ApplyItems(ToolStrip strip, AppLanguage language)
        {
            if (strip == null) { return; }
            foreach (ToolStripItem item in strip.Items)
            {
                ApplyItem(item, language);
            }
        }

        private static void ApplyItem(ToolStripItem item, AppLanguage language)
        {
            if (item == null) { return; }
            string source = item.Tag as string;
            if (source != null) { item.Text = Get(language, source); }
            ToolStripDropDownItem dropDown = item as ToolStripDropDownItem;
            if (dropDown != null)
            {
                foreach (ToolStripItem child in dropDown.DropDownItems)
                {
                    ApplyItem(child, language);
                }
            }
        }
    }
}
