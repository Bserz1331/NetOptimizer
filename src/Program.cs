using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("NetOptimizer")]
[assembly: AssemblyDescription("A bounded, timeout-aware network monitoring, failover, and refresh utility.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Space Cat")]
[assembly: AssemblyProduct("NetOptimizer")]
[assembly: AssemblyCopyright("Copyright © Space Cat")]
[assembly: AssemblyVersion("3.0.12.0")]
[assembly: AssemblyFileVersion("3.0.12.0")]

namespace NetOptimizerV2
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args != null && args.Any(delegate(string arg)
            {
                return string.Equals(arg, "--self-test", StringComparison.OrdinalIgnoreCase);
            }))
            {
                SelfTest.Run();
                return;
            }
            if (args != null && args.Any(delegate(string arg)
            {
                return string.Equals(arg, "--stability-test", StringComparison.OrdinalIgnoreCase);
            }))
            {
                SelfTest.RunStability();
                return;
            }
            if (args != null && args.Any(delegate(string arg)
            {
                return string.Equals(arg, "--soak-test", StringComparison.OrdinalIgnoreCase);
            }))
            {
                SelfTest.RunSoak(ParseSeconds(args, 120));
                return;
            }
            if (args != null && args.Any(delegate(string arg)
            {
                return string.Equals(arg, "--failover-simulation", StringComparison.OrdinalIgnoreCase);
            }))
            {
                try
                {
                    FailoverSimulation.Run();
                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("NetOptimizer failover simulation: FAIL");
                    Console.Error.WriteLine(ex.Message);
                    Environment.ExitCode = 1;
                }
                return;
            }
            if (args != null && args.Any(delegate(string arg)
            {
                return string.Equals(arg, "--interface-probe-test", StringComparison.OrdinalIgnoreCase);
            }))
            {
                SelfTest.RunInterfaceProbes();
                return;
            }
            if (args != null && args.Any(delegate(string arg)
            {
                return string.Equals(arg, "--diagnostics-test", StringComparison.OrdinalIgnoreCase);
            }))
            {
                SelfTest.RunDiagnostics();
                return;
            }
            if (args != null && args.Any(delegate(string arg)
            {
                return string.Equals(arg, "--interface-metric-test", StringComparison.OrdinalIgnoreCase);
            }))
            {
                SelfTest.RunInterfaceMetrics();
                return;
            }
            if (args != null && args.Any(delegate(string arg)
            {
                return string.Equals(arg, "--ui-layout-test", StringComparison.OrdinalIgnoreCase);
            }))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    MainForm.RunUiLayoutSelfTest();
                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("NetOptimizer UI layout test: FAIL");
                    Console.Error.WriteLine(ex.Message);
                    Environment.ExitCode = 1;
                }
                return;
            }
            if (args != null && args.Any(delegate(string arg)
            {
                return string.Equals(arg, "--gui-startup-test", StringComparison.OrdinalIgnoreCase);
            }))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    MainForm.RunGuiStartupSelfTest();
                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("NetOptimizer GUI startup test: FAIL");
                    Console.Error.WriteLine(ex.Message);
                    Environment.ExitCode = 1;
                }
                return;
            }
            string uiSnapshotPath = ParseArgumentValue(args, "--ui-snapshot=");
            if (!string.IsNullOrWhiteSpace(uiSnapshotPath))
            {
                string snapshotLanguage = ParseArgumentValue(args, "--snapshot-language=");
                AppLanguage language = string.Equals(snapshotLanguage, "en", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(snapshotLanguage, "english", StringComparison.OrdinalIgnoreCase)
                    ? AppLanguage.English
                    : AppLanguage.TraditionalChinese;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    MainForm.SaveUiSnapshot(uiSnapshotPath, language);
                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("NetOptimizer UI snapshot: FAIL");
                    Console.Error.WriteLine(ex.Message);
                    Environment.ExitCode = 1;
                }
                return;
            }
            string supportSnapshotPath = ParseArgumentValue(args, "--support-snapshot=");
            if (!string.IsNullOrWhiteSpace(supportSnapshotPath))
            {
                string snapshotLanguage = ParseArgumentValue(args, "--snapshot-language=");
                AppLanguage language = string.Equals(snapshotLanguage, "en", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(snapshotLanguage, "english", StringComparison.OrdinalIgnoreCase)
                    ? AppLanguage.English
                    : AppLanguage.TraditionalChinese;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    SupportDialog.SaveUiSnapshot(supportSnapshotPath, language);
                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("NetOptimizer support snapshot: FAIL");
                    Console.Error.WriteLine(ex.Message);
                    Environment.ExitCode = 1;
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                bool acquired;
                using (SingleInstanceLock singleInstance = SingleInstanceLock.TryAcquire(out acquired))
                {
                    if (!acquired)
                    {
                        MessageBox.Show("NetOptimizer 已經在執行中。請從系統匣開啟現有視窗。",
                                        "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    Application.Run(new MainForm());
                }
            }
            catch (Exception ex)
            {
                string errorPath = Path.Combine(
                    SettingsStore.SettingsDirectory,
                    "startup-error.log");
                try
                {
                    Directory.CreateDirectory(SettingsStore.SettingsDirectory);
                    File.AppendAllText(
                        errorPath,
                        DateTime.Now.ToString("o") + Environment.NewLine +
                        ex + Environment.NewLine + Environment.NewLine,
                        System.Text.Encoding.UTF8);
                }
                catch { }

                MessageBox.Show(
                    "NetOptimizer 啟動失敗，已避免未處理的 CLR 錯誤。\n\n" +
                    "錯誤記錄：" + errorPath + "\n\n" + ex.Message,
                    "NetOptimizer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.ExitCode = 1;
            }
        }

        private static int ParseSeconds(string[] args, int fallback)
        {
            if (args != null)
            {
                foreach (string arg in args)
                {
                    const string prefix = "--seconds=";
                    if (arg != null && arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        int value;
                        if (int.TryParse(arg.Substring(prefix.Length), out value))
                        {
                            return Math.Max(1, Math.Min(86400, value));
                        }
                    }
                }
            }
            return fallback;
        }

        private static string ParseArgumentValue(string[] args, string prefix)
        {
            if (args == null)
            {
                return null;
            }
            foreach (string arg in args)
            {
                if (arg != null && arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(prefix.Length).Trim('"');
                }
            }
            return null;
        }
    }

    internal static class SelfTest
    {
        public static void Run()
        {
            try
            {
                MonitorSettings settings = MonitorSettings.CreateDefault();
                settings.Normalize();
                if (settings.Targets.Count != 2 || settings.FastIntervalMs != 500 ||
                    settings.SlowIntervalMs != 1000 || settings.FailoverEnabled ||
                    settings.FailoverThresholdMs != 200 || settings.FailoverBadSamples != 2 ||
                    settings.FailoverRecoverySeconds != 5 || settings.FailoverPrimaryMetric != 5 ||
                    settings.FailoverBackupMetric != 50 || settings.FailoverTargets.Count != 2 ||
                    settings.SmartSelectionEnabled || settings.SmartEwmaAlpha < 0.149 ||
                    settings.SmartEwmaAlpha > 0.151 ||
                    settings.Language != AppLanguage.TraditionalChinese)
                {
                    throw new InvalidOperationException("預設設定驗證失敗。");
                }

                string warning;
                MonitorSettings loaded = SettingsStore.Load(out warning);
                loaded.Normalize();
                if (loaded.Targets.Count == 0 || loaded.Port < 1 || loaded.Port > 65535)
                {
                    throw new InvalidOperationException("設定讀取驗證失敗。");
                }

                CommandResult command = CommandRunner.RunAsync(
                    "cmd.exe", new[] { "/c", "exit", "0" }, 2000, CancellationToken.None)
                    .GetAwaiter().GetResult();
                if (!command.Succeeded)
                {
                    throw new InvalidOperationException("外部命令 runner 驗證失敗。");
                }

                int parsedMetric;
                bool parsedAutomatic;
                if (!NetworkInfo.TryParseInterfaceMetricOutput(
                        "NETOPTIMIZER_METRIC|5|0",
                        out parsedMetric,
                        out parsedAutomatic) ||
                    parsedMetric != 5 || parsedAutomatic ||
                    !NetworkInfo.TryParseInterfaceMetricOutput(
                        "50|False",
                        out parsedMetric,
                        out parsedAutomatic) ||
                    parsedMetric != 50 || parsedAutomatic)
                {
                    throw new InvalidOperationException("InterfaceMetric parser 驗證失敗。");
                }
                string usefulError = CommandRunner.GetUsefulError(
                    "#< CLIXML\r\n<Objs Version=\"1.1.0.1\" " +
                    "xmlns=\"http://schemas.microsoft.com/powershell/2004/04\"><S S=\"Error\">" +
                    "Set-NetIPInterface : test_x000D__x000A_</S><S S=\"Error\">Access denied</S></Objs>");
                if (usefulError.IndexOf("Set-NetIPInterface", StringComparison.OrdinalIgnoreCase) < 0 ||
                    usefulError.IndexOf("Access denied", StringComparison.OrdinalIgnoreCase) < 0 ||
                    usefulError.IndexOf("<S", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw new InvalidOperationException("PowerShell CLIXML error parser 驗證失敗。");
                }
                string permissionError = CommandRunner.GetUsefulError(
                    "#< CLIXML\r\n<Objs><S S=\"Error\">CategoryInfo : PermissionDenied</S></Objs>");
                if (permissionError.IndexOf("權限不足", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    throw new InvalidOperationException("PowerShell permission error normalization 驗證失敗。");
                }

                TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
                try
                {
                    listener.Start();
                    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                    Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
                    ProbeResult boundProbe = NetworkProbe.TcpAsync(
                        "127.0.0.1", "127.0.0.1", port, 1000, CancellationToken.None)
                        .GetAwaiter().GetResult();
                    if (boundProbe.State != ProbeState.Success)
                    {
                        throw new InvalidOperationException("來源 IP 綁定 TCP probe 驗證失敗。");
                    }
                    TcpClient accepted = acceptTask.GetAwaiter().GetResult();
                    accepted.Dispose();
                }
                finally
                {
                    listener.Stop();
                }

                FailoverSimulation.Run();

                MonitorSettings localSmoke = MonitorSettings.CreateDefault();
                localSmoke.InterfaceName = "Loopback";
                localSmoke.Targets = new System.Collections.Generic.List<string> { "127.0.0.1" };
                localSmoke.Port = 9;
                localSmoke.ProbeTimeoutMs = 200;
                localSmoke.FastIntervalMs = 100;
                localSmoke.SlowIntervalMs = 100;
                localSmoke.EnableRefresh = false;
                localSmoke.FlushDns = false;
                localSmoke.ClearArp = false;
                localSmoke.PulseMtu = false;
                localSmoke.Normalize();
                using (MonitorEngine engine = new MonitorEngine())
                {
                    engine.Start(localSmoke);
                    Thread.Sleep(350);
                    engine.Stop();
                }

                int interfaceCount = NetworkInfo.GetInterfaceNames().Count;
                Console.WriteLine("NetOptimizer self-test: PASS");
                Console.WriteLine("Settings path: " + SettingsStore.SettingsPath);
                Console.WriteLine("Network interfaces visible: " + interfaceCount);
                if (!string.IsNullOrWhiteSpace(warning))
                {
                    Console.WriteLine("Note: " + warning);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("NetOptimizer self-test: FAIL");
                Console.Error.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
        }

        public static void RunStability()
        {
            try
            {
                MonitorSettings settings = MonitorSettings.CreateDefault();
                settings.InterfaceName = "Loopback";
                settings.Targets = new System.Collections.Generic.List<string> { "127.0.0.1" };
                settings.Port = 9;
                settings.ProbeTimeoutMs = 150;
                settings.FastIntervalMs = 100;
                settings.SlowIntervalMs = 100;
                settings.EnableRefresh = false;
                settings.FlushDns = false;
                settings.ClearArp = false;
                settings.PulseMtu = false;
                settings.Normalize();

                using (MonitorEngine engine = new MonitorEngine())
                {
                    engine.Start(settings);
                    DateTime end = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < end)
                    {
                        Thread.Sleep(100);
                    }
                    engine.Stop();
                }
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
        }

        public static void RunSoak(int seconds)
        {
            try
            {
                MonitorSettings settings = MonitorSettings.CreateDefault();
                settings.InterfaceName = "Loopback";
                settings.Targets = new System.Collections.Generic.List<string> { "127.0.0.1" };
                settings.Port = 9;
                settings.ProbeTimeoutMs = 150;
                settings.FastIntervalMs = 100;
                settings.SlowIntervalMs = 100;
                settings.EnableRefresh = false;
                settings.FlushDns = false;
                settings.ClearArp = false;
                settings.PulseMtu = false;
                settings.Normalize();

                using (MonitorEngine engine = new MonitorEngine())
                {
                    engine.Start(settings);
                    DateTime end = DateTime.UtcNow.AddSeconds(Math.Max(1, seconds));
                    while (DateTime.UtcNow < end)
                    {
                        Thread.Sleep(250);
                    }
                    engine.Stop();
                }
                Console.WriteLine("NetOptimizer soak test: PASS (" + seconds + " seconds)");
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("NetOptimizer soak test: FAIL");
                Console.Error.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
        }

        public static void RunInterfaceProbes()
        {
            try
            {
                MonitorSettings settings = MonitorSettings.CreateDefault();
                settings.Normalize();
                System.Collections.Generic.List<InterfaceSnapshot> snapshots =
                    NetworkInfo.GetInterfaceSnapshots()
                        .Where(delegate(InterfaceSnapshot snapshot) { return snapshot.IsReady; })
                        .ToList();
                if (snapshots.Count == 0)
                {
                    throw new InvalidOperationException("沒有找到同時具備 IPv4 與 gateway 的可用介面。");
                }

                int attempted = 0;
                int successful = 0;
                Console.WriteLine("NetOptimizer interface probe test");
                foreach (InterfaceSnapshot snapshot in snapshots)
                {
                    Console.WriteLine("[" + snapshot.Name + "] IPv4=" + snapshot.IPv4 +
                                      " Gateway=" + snapshot.Gateway);
                    foreach (string target in settings.FailoverTargets)
                    {
                        ProbeResult result = NetworkProbe.TcpAsync(
                            snapshot.IPv4,
                            target,
                            settings.Port,
                            settings.FailoverPingTimeoutMs,
                            CancellationToken.None).GetAwaiter().GetResult();
                        attempted++;
                        bool ok = result != null && result.State == ProbeState.Success;
                        if (ok) { successful++; }
                        Console.WriteLine("  " + target + ": " +
                                          (result == null ? "no result" : result.State.ToString()) +
                                          " " + (result == null ? 0 : result.LatencyMs) + " ms" +
                                          (result == null || string.IsNullOrWhiteSpace(result.Error)
                                              ? string.Empty
                                              : " (" + result.Error + ")"));
                    }
                }

                Console.WriteLine("Successful probes: " + successful + "/" + attempted);
                if (successful == 0)
                {
                    throw new InvalidOperationException("所有介面的來源綁定 TCP probe 都失敗。");
                }
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("NetOptimizer interface probe test: FAIL");
                Console.Error.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
        }

        public static void RunDiagnostics()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "NetOptimizer-diagnostics-test-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                MonitorSettings settings = MonitorSettings.CreateDefault();
                settings.Normalize();
                FailoverStatus status = new FailoverStatus
                {
                    Ready = false,
                    ActiveInterface = settings.PrimaryInterface,
                    StandbyInterface = settings.BackupInterface,
                    Detail = "diagnostics test"
                };
                System.Collections.Generic.List<string> logs =
                    new System.Collections.Generic.List<string> { "diagnostics test log" };

                DiagnosticsExporter.ExportAsync(
                    path,
                    settings,
                    logs,
                    status,
                    CancellationToken.None).GetAwaiter().GetResult();
                DiagnosticsExporter.ExportAsync(
                    path,
                    settings,
                    logs,
                    status,
                    CancellationToken.None).GetAwaiter().GetResult();

                string report = File.ReadAllText(path);
                if (report.IndexOf("[Settings]", StringComparison.Ordinal) < 0 ||
                    report.IndexOf("[Interfaces]", StringComparison.Ordinal) < 0 ||
                    report.IndexOf("[FailoverStatus]", StringComparison.Ordinal) < 0 ||
                    report.IndexOf("diagnostics test log", StringComparison.Ordinal) < 0)
                {
                    throw new InvalidOperationException("diagnostics report sections are incomplete");
                }
                Console.WriteLine("NetOptimizer diagnostics test: PASS");
                Console.WriteLine("Report bytes: " + new FileInfo(path).Length);
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("NetOptimizer diagnostics test: FAIL");
                Console.Error.WriteLine("EX_TYPE=" + ex.GetType().FullName);
                Console.Error.WriteLine("EX_HRESULT=0x" + ex.HResult.ToString("X8"));
                Console.Error.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
            finally
            {
                try
                {
                    if (File.Exists(path)) { File.Delete(path); }
                }
                catch { }
            }
        }

        public static void RunInterfaceMetrics()
        {
            try
            {
                System.Collections.Generic.List<InterfaceSnapshot> snapshots =
                    NetworkInfo.GetInterfaceSnapshots()
                        .Where(delegate(InterfaceSnapshot snapshot) { return snapshot.IsReady; })
                        .ToList();
                if (snapshots.Count == 0)
                {
                    throw new InvalidOperationException("沒有找到同時具備 IPv4 與 gateway 的可用介面。");
                }

                Console.WriteLine("NetOptimizer InterfaceMetric read test");
                int failures = 0;
                foreach (InterfaceSnapshot snapshot in snapshots)
                {
                    InterfaceMetricState state = NetworkInfo.ReadInterfaceMetricAsync(
                        snapshot.Name,
                        CancellationToken.None).GetAwaiter().GetResult();
                    if (state == null || !string.IsNullOrWhiteSpace(state.Error))
                    {
                        failures++;
                        Console.WriteLine("[" + snapshot.Name + "] ERROR=" +
                                          (state == null ? "no result" : state.Error));
                    }
                    else
                    {
                        Console.WriteLine("[" + snapshot.Name + "] Metric=" + state.Metric +
                                          " AutomaticMetric=" + state.AutomaticMetric);
                    }
                }
                Console.WriteLine("Successful metric reads: " + (snapshots.Count - failures) + "/" + snapshots.Count);
                Environment.ExitCode = failures == 0 ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("NetOptimizer InterfaceMetric read test: FAIL");
                Console.Error.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
        }
    }
}
