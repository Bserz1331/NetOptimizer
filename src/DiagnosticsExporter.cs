using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetOptimizerV2
{
    internal static class DiagnosticsExporter
    {
        public static async Task ExportAsync(
            string path,
            MonitorSettings settings,
            IList<string> logLines,
            FailoverStatus failoverStatus,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentException("path"); }
            if (settings == null) { throw new ArgumentNullException("settings"); }

            StringBuilder report = new StringBuilder(12000);
            report.AppendLine("NetOptimizer diagnostics");
            report.AppendLine("GeneratedLocal=" + DateTime.Now.ToString("o"));
            report.AppendLine("GeneratedUtc=" + DateTime.UtcNow.ToString("o"));
            report.AppendLine("Version=" + typeof(DiagnosticsExporter).Assembly.GetName().Version);
            report.AppendLine("OS=" + Environment.OSVersion.VersionString);
            report.AppendLine("64BitOS=" + Environment.Is64BitOperatingSystem);
            report.AppendLine("64BitProcess=" + Environment.Is64BitProcess);
            report.AppendLine("Administrator=" + NetworkInfo.IsAdministrator());
            report.AppendLine();

            report.AppendLine("[Settings]");
            report.AppendLine("InterfaceName=" + Safe(settings.InterfaceName));
            report.AppendLine("Targets=" + Safe(string.Join(", ", settings.Targets ?? new List<string>())));
            report.AppendLine("Port=" + settings.Port);
            report.AppendLine("ThresholdMs=" + settings.ThresholdMs);
            report.AppendLine("ProbeTimeoutMs=" + settings.ProbeTimeoutMs);
            report.AppendLine("EnableRefresh=" + settings.EnableRefresh);
            report.AppendLine("FlushDns=" + settings.FlushDns);
            report.AppendLine("ClearArp=" + settings.ClearArp);
            report.AppendLine("PulseMtu=" + settings.PulseMtu);
            report.AppendLine("FailoverEnabled=" + settings.FailoverEnabled);
            report.AppendLine("PrimaryInterface=" + Safe(settings.PrimaryInterface));
            report.AppendLine("BackupInterface=" + Safe(settings.BackupInterface));
            report.AppendLine("FailoverTargets=" + Safe(string.Join(", ", settings.FailoverTargets ?? new List<string>())));
            report.AppendLine("FailoverThresholdMs=" + settings.FailoverThresholdMs);
            report.AppendLine("FailoverPingTimeoutMs=" + settings.FailoverPingTimeoutMs);
            report.AppendLine("FailoverBadSamples=" + settings.FailoverBadSamples);
            report.AppendLine("FailoverRecoverySeconds=" + settings.FailoverRecoverySeconds);
            report.AppendLine("FailoverSwitchCooldownSeconds=" + settings.FailoverSwitchCooldownSeconds);
            report.AppendLine("FailoverSwitchBackoffSeconds=" + settings.FailoverSwitchBackoffSeconds);
            report.AppendLine("FailoverPrimaryMetric=" + settings.FailoverPrimaryMetric);
            report.AppendLine("FailoverBackupMetric=" + settings.FailoverBackupMetric);
            report.AppendLine("SuppressRefreshDuringFailover=" + settings.SuppressRefreshDuringFailover);
            report.AppendLine("SmartSelectionEnabled=" + settings.SmartSelectionEnabled);
            report.AppendLine("SmartDecisionIntervalSeconds=" + settings.SmartDecisionIntervalSeconds);
            report.AppendLine("SmartMinDwellSeconds=" + settings.SmartMinDwellSeconds);
            report.AppendLine("SmartSwitchHoldSeconds=" + settings.SmartSwitchHoldSeconds);
            report.AppendLine("SmartMarginMs=" + settings.SmartMarginMs);
            report.AppendLine("SmartProbeIntervalMs=" + settings.SmartProbeIntervalMs);
            report.AppendLine("SmartMaxSwitchesPerHour=" + settings.SmartMaxSwitchesPerHour);
            report.AppendLine("SmartEwmaAlpha=" + settings.SmartEwmaAlpha.ToString("0.###"));
            report.AppendLine();

            report.AppendLine("[Interfaces]");
            List<InterfaceSnapshot> snapshots = NetworkInfo.GetInterfaceSnapshots();
            foreach (InterfaceSnapshot snapshot in snapshots)
            {
                report.AppendLine("Name=" + Safe(snapshot.Name) +
                                  " | Description=" + Safe(snapshot.Description) +
                                  " | Type=" + snapshot.Type +
                                  " | Status=" + snapshot.Status +
                                  " | IPv4=" + Safe(snapshot.IPv4) +
                                  " | Gateway=" + Safe(snapshot.Gateway) +
                                  " | Ready=" + snapshot.IsReady +
                                  " | MTU=" + NetworkInfo.TryGetMtu(snapshot.Name));
            }
            report.AppendLine();

            report.AppendLine("[SelectedMetrics]");
            HashSet<string> selectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in new[] { settings.InterfaceName, settings.PrimaryInterface, settings.BackupInterface })
            {
                if (string.IsNullOrWhiteSpace(name) || !selectedNames.Add(name)) { continue; }
                InterfaceMetricState metric = await NetworkInfo.ReadInterfaceMetricAsync(name, token)
                    .ConfigureAwait(false);
                report.AppendLine("Name=" + Safe(name) +
                                  " | Metric=" + (metric == null ? "" : metric.Metric.ToString()) +
                                  " | AutomaticMetric=" + (metric == null ? "" : metric.AutomaticMetric.ToString()) +
                                  " | Error=" + Safe(metric == null ? "沒有結果" : metric.Error));
            }
            DefaultRouteState route = await NetworkInfo.ReadPreferredDefaultRouteAsync(token)
                .ConfigureAwait(false);
            report.AppendLine("PreferredDefaultRoute=" + (route == null ? "" : Safe(route.InterfaceName)) +
                              " | RouteMetric=" + (route == null ? "" : route.RouteMetric.ToString()) +
                              " | InterfaceMetric=" + (route == null ? "" : route.InterfaceMetric.ToString()) +
                              " | NextHop=" + (route == null ? "" : Safe(route.NextHop)) +
                              " | Error=" + Safe(route == null ? "沒有結果" : route.Error));
            report.AppendLine();

            report.AppendLine("[FailoverStatus]");
            if (failoverStatus == null)
            {
                report.AppendLine("Status=未收到");
            }
            else
            {
                report.AppendLine("Ready=" + failoverStatus.Ready);
                report.AppendLine("InFailover=" + failoverStatus.InFailover);
                report.AppendLine("SmartSelection=" + failoverStatus.SmartSelection);
                report.AppendLine("ActiveInterface=" + Safe(failoverStatus.ActiveInterface));
                report.AppendLine("StandbyInterface=" + Safe(failoverStatus.StandbyInterface));
                report.AppendLine("ActiveHealth=" + Safe(failoverStatus.ActiveHealth));
                report.AppendLine("StandbyHealth=" + Safe(failoverStatus.StandbyHealth));
                report.AppendLine("Detail=" + Safe(failoverStatus.Detail));
                report.AppendLine("LastSwitchReason=" + Safe(failoverStatus.LastSwitchReason));
                report.AppendLine("LastSwitchUtc=" + failoverStatus.LastSwitchUtc.ToString("o"));
                report.AppendLine("SwitchCount=" + failoverStatus.SwitchCount);
            }
            report.AppendLine();

            report.AppendLine("[RecentLog]");
            if (logLines != null)
            {
                foreach (string line in logLines.Take(240))
                {
                    report.AppendLine(line ?? string.Empty);
                }
            }

            string tempPath = path + ".tmp";
            try
            {
                File.WriteAllText(tempPath, report.ToString(), new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(tempPath, path, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(tempPath, path, true);
                        File.Delete(tempPath);
                    }
                    catch (IOException)
                    {
                        File.Copy(tempPath, path, true);
                        File.Delete(tempPath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        File.Copy(tempPath, path, true);
                        File.Delete(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }

        private static string Safe(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
        }
    }
}
