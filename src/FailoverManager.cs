using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetOptimizerV2
{
    internal sealed class FailoverManager
    {
        private readonly SemaphoreSlim networkActionGate;
        private readonly Action<string, bool> log;
        private readonly Action<FailoverStatus> statusChanged;

        public FailoverManager(
            SemaphoreSlim networkActionGate,
            Action<string, bool> log,
            Action<FailoverStatus> statusChanged)
        {
            this.networkActionGate = networkActionGate;
            this.log = log;
            this.statusChanged = statusChanged;
        }

        public static async Task<RecoveryReport> RecoverPendingAsync(CancellationToken token)
        {
            RecoveryReport report = new RecoveryReport();
            string warning;
            MetricRecoveryJournal journal = RecoveryStore.Load(out warning);
            if (!string.IsNullOrWhiteSpace(warning))
            {
                report.Messages.Add(warning);
            }
            if (journal == null)
            {
                return report;
            }

            report.FoundJournal = true;
            bool allRestored = true;
            foreach (MetricRecoveryEntry entry in journal.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.InterfaceName))
                {
                    allRestored = false;
                    report.Messages.Add("A/B recovery journal 有無效介面項目，保留 journal 供人工處理。");
                    continue;
                }

                InterfaceMetricState current = await NetworkInfo.ReadInterfaceMetricAsync(
                    entry.InterfaceName, token).ConfigureAwait(false);
                if (!MetricStateIsUsable(current))
                {
                    allRestored = false;
                    report.Messages.Add("無法讀取「" + entry.InterfaceName + "」的目前 metric：" +
                                        (current == null ? "沒有結果" : current.Error));
                    continue;
                }

                if (IsOriginalState(current, entry))
                {
                    report.Messages.Add("「" + entry.InterfaceName + "」已是原始 metric。");
                    continue;
                }
                if (!IsManagedState(current, entry))
                {
                    allRestored = false;
                    report.Messages.Add("「" + entry.InterfaceName + "」的 metric 與本程式預期不符，未覆蓋使用者變更。");
                    continue;
                }

                CommandResult restore = await NetworkInfo.SetInterfaceMetricAsync(
                    entry.InterfaceName,
                    entry.OriginalMetric,
                    entry.OriginalAutomaticMetric,
                    CancellationToken.None).ConfigureAwait(false);
                if (restore == null || !restore.Succeeded)
                {
                    allRestored = false;
                    report.Messages.Add("復原「" + entry.InterfaceName + "」失敗：" + FirstError(restore));
                    continue;
                }

                InterfaceMetricState after = await NetworkInfo.ReadInterfaceMetricAsync(
                    entry.InterfaceName, CancellationToken.None).ConfigureAwait(false);
                if (!IsOriginalState(after, entry))
                {
                    allRestored = false;
                    report.Messages.Add("「" + entry.InterfaceName + "」復原後驗證不一致，保留 journal。");
                }
                else
                {
                    report.Messages.Add("已復原「" + entry.InterfaceName + "」的原始 metric。");
                }
            }

            report.Restored = allRestored;
            if (allRestored)
            {
                try
                {
                    RecoveryStore.Clear();
                    report.Cleared = true;
                    report.Messages.Add("A/B recovery journal 已清除。");
                }
                catch (Exception ex)
                {
                    report.Messages.Add("復原成功，但無法清除 recovery journal：" + ex.Message);
                }
            }
            return report;
        }

        public async Task RunAsync(MonitorSettings settings, CancellationToken token)
        {
            string primaryName = (settings.PrimaryInterface ?? string.Empty).Trim();
            string backupName = (settings.BackupInterface ?? string.Empty).Trim();
            if (primaryName.Length == 0 || backupName.Length == 0)
            {
                Write("A/B 切換未啟動：請同時指定主線 A 與備援 B。", true);
                return;
            }
            if (string.Equals(primaryName, backupName, StringComparison.OrdinalIgnoreCase))
            {
                Write("A/B 切換未啟動：A 與 B 不能是同一張網卡。", true);
                return;
            }

            List<string> targets = GetTargets(settings);
            if (targets.Count == 0)
            {
                Write("A/B 切換未啟動：沒有指定故障切換測試目標。", true);
                return;
            }

            InterfaceSnapshot primarySnapshot = NetworkInfo.GetInterfaceSnapshot(primaryName);
            InterfaceSnapshot backupSnapshot = NetworkInfo.GetInterfaceSnapshot(backupName);
            if (primarySnapshot == null || backupSnapshot == null)
            {
                Write("A/B 切換未啟動：找不到指定的 A 或 B 網卡。", true);
                return;
            }

            InterfaceMetricState primaryState =
                await NetworkInfo.ReadInterfaceMetricAsync(primaryName, token).ConfigureAwait(false);
            InterfaceMetricState backupState =
                await NetworkInfo.ReadInterfaceMetricAsync(backupName, token).ConfigureAwait(false);
            if (!MetricStateIsUsable(primaryState))
            {
                Write("A/B 切換未啟動：無法讀取 A 的 InterfaceMetric（" +
                      (primaryState == null ? "沒有結果" : primaryState.Error) + "）。", true);
                return;
            }
            if (!MetricStateIsUsable(backupState))
            {
                Write("A/B 切換未啟動：無法讀取 B 的 InterfaceMetric（" +
                      (backupState == null ? "沒有結果" : backupState.Error) + "）。", true);
                return;
            }

            MetricRecoveryJournal journal = new MetricRecoveryJournal
            {
                SessionId = Guid.NewGuid().ToString("N"),
                CreatedUtc = DateTime.UtcNow,
                Entries = new List<MetricRecoveryEntry>
                {
                    new MetricRecoveryEntry
                    {
                        InterfaceName = primaryName,
                        OriginalMetric = primaryState.Metric,
                        OriginalAutomaticMetric = primaryState.AutomaticMetric,
                        ManagedPrimaryMetric = settings.FailoverPrimaryMetric,
                        ManagedBackupMetric = settings.FailoverBackupMetric
                    },
                    new MetricRecoveryEntry
                    {
                        InterfaceName = backupName,
                        OriginalMetric = backupState.Metric,
                        OriginalAutomaticMetric = backupState.AutomaticMetric,
                        ManagedPrimaryMetric = settings.FailoverPrimaryMetric,
                        ManagedBackupMetric = settings.FailoverBackupMetric
                    }
                }
            };

            try
            {
                RecoveryStore.Save(journal);
            }
            catch (Exception ex)
            {
                Write("A/B 切換未啟動：無法建立 crash-safe recovery journal（" + ex.Message + "）。", true);
                return;
            }

            bool mayHaveChanged = true;
            bool ready = false;
            try
            {
                Write("A/B 切換準備：A=" + primaryName + "，B=" + backupName +
                      "，目標=" + string.Join(", ", targets) + "。", false);
                ready = await ApplyPairAsync(
                    primaryName,
                    backupName,
                    settings,
                    token).ConfigureAwait(false);
                if (ready)
                {
                    PublishStatus(new FailoverStatus
                    {
                        Ready = true,
                        ActiveInterface = primaryName,
                        StandbyInterface = backupName,
                        SmartSelection = settings.SmartSelectionEnabled,
                        Detail = "A/B metric 已套用，開始介面健康監測。",
                        LastSwitchUtc = DateTime.MinValue
                    });
                    await MonitorStateAsync(settings, primaryName, backupName, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                if (!token.IsCancellationRequested)
                {
                    Write("A/B 切換意外取消。", true);
                }
            }
            catch (Exception ex)
            {
                Write("A/B 切換停止：" + ex.Message, true);
            }

            if (mayHaveChanged)
            {
                bool restored = await RestoreOriginalAsync(primaryState, backupState).ConfigureAwait(false);
                if (restored)
                {
                    try
                    {
                        RecoveryStore.Clear();
                    }
                    catch (Exception ex)
                    {
                        Write("A/B 已復原，但無法清除 recovery journal：" + ex.Message, true);
                    }
                }
                else
                {
                    Write("A/B 原始 metric 尚未全部確認復原；下次啟動會再次嘗試。", true);
                }
            }
            PublishStatus(new FailoverStatus
            {
                Ready = false,
                SmartSelection = settings.SmartSelectionEnabled,
                Detail = ready ? "A/B 監測已停止，已完成復原嘗試。" : "A/B 監測未進入工作狀態。"
            });
        }

        private async Task MonitorStateAsync(
            MonitorSettings settings,
            string originalPrimary,
            string originalBackup,
            CancellationToken token)
        {
            FailoverPolicy policy = new FailoverPolicy(originalPrimary, originalBackup);
            Dictionary<string, EwmaLinkStats> statistics =
                new Dictionary<string, EwmaLinkStats>(StringComparer.OrdinalIgnoreCase);
            List<DateTime> switchHistory = new List<DateTime>();
            LinkHealth lastActive = null;
            LinkHealth lastStandby = null;
            DateTime nextSmartProbeUtc = DateTime.MinValue;
            DateTime nextSmartDecisionUtc = DateTime.UtcNow.AddSeconds(settings.SmartDecisionIntervalSeconds);
            bool badReported = false;
            bool recoveryReported = false;
            bool standbyReported = false;

            while (!token.IsCancellationRequested)
            {
                DateTime now = DateTime.UtcNow;
                lastActive = await MeasureLinkAsync(policy.ActiveInterface, settings, token)
                    .ConfigureAwait(false);
                if (lastActive.Cancelled) { break; }

                AddStatistics(statistics, policy.ActiveInterface, lastActive, settings);

                if (policy.InFailover)
                {
                    lastStandby = await MeasureLinkAsync(policy.StandbyInterface, settings, token)
                        .ConfigureAwait(false);
                    if (lastStandby.Cancelled) { break; }
                    AddStatistics(statistics, policy.StandbyInterface, lastStandby, settings);

                    bool recoveryReady = policy.ObserveRecovery(
                        lastStandby.Healthy,
                        now,
                        settings.FailoverRecoverySeconds);
                    if (recoveryReady)
                    {
                        if (SwitchBudgetAvailable(switchHistory, settings))
                        {
                            bool switched = await SwitchPairAsync(
                                policy.StandbyInterface,
                                policy.ActiveInterface,
                                settings,
                                token).ConfigureAwait(false);
                            if (switched)
                            {
                                policy.MarkSwitchSuccess(
                                    now,
                                    settings.FailoverSwitchCooldownSeconds,
                                    false,
                                    "主線恢復切回");
                                switchHistory.Add(now);
                                recoveryReported = false;
                                badReported = false;
                                Write("A 已穩定 " + settings.FailoverRecoverySeconds +
                                      " 秒，已切回 A/B 優先線路：" + policy.ActiveInterface + "。", false);
                            }
                            else
                            {
                                policy.MarkSwitchFailure(now, settings.FailoverSwitchBackoffSeconds);
                            }
                        }
                        else
                        {
                            Write("A 已恢復，但已達每小時切換上限，暫停自動切換。", true);
                            policy.MarkSwitchFailure(now, settings.FailoverSwitchBackoffSeconds);
                        }
                    }
                    else if (lastStandby.Healthy && !recoveryReported)
                    {
                        Write("原主線已恢復，開始累積穩定時間。", false);
                        recoveryReported = true;
                    }
                    else if (!lastStandby.Healthy)
                    {
                        recoveryReported = false;
                    }

                    PublishStatus(BuildStatus(policy, settings, lastActive, lastStandby));
                }
                else
                {
                    policy.ObserveActive(lastActive.Healthy);
                    if (!lastActive.Healthy)
                    {
                        if (!badReported && policy.BadStreak >= settings.FailoverBadSamples)
                        {
                            Write("A 異常：" + lastActive.Display + "，準備檢查 B。", true);
                            badReported = true;
                        }

                        if (policy.ShouldFailover(now, settings.FailoverBadSamples))
                        {
                            lastStandby = await MeasureLinkAsync(policy.StandbyInterface, settings, token)
                                .ConfigureAwait(false);
                            if (lastStandby.Cancelled) { break; }
                            AddStatistics(statistics, policy.StandbyInterface, lastStandby, settings);

                            if (!lastStandby.Healthy)
                            {
                                if (!standbyReported)
                                {
                                    Write("A 異常，但 B 尚未通過健康檢查（" + lastStandby.Display + "）。", true);
                                    standbyReported = true;
                                }
                                policy.MarkSwitchFailure(now, settings.FailoverSwitchBackoffSeconds);
                            }
                            else if (!SwitchBudgetAvailable(switchHistory, settings))
                            {
                                Write("A 異常，但已達每小時切換上限，維持現有路由。", true);
                                policy.MarkSwitchFailure(now, settings.FailoverSwitchBackoffSeconds);
                            }
                            else
                            {
                                bool switched = await SwitchPairAsync(
                                    policy.StandbyInterface,
                                    policy.ActiveInterface,
                                    settings,
                                    token).ConfigureAwait(false);
                                if (switched)
                                {
                                        policy.MarkSwitchSuccess(
                                            now,
                                            settings.FailoverSwitchCooldownSeconds,
                                            true,
                                            "故障切換");
                                    switchHistory.Add(now);
                                    badReported = false;
                                    standbyReported = false;
                                    recoveryReported = false;
                                    Write("已切換到 B=" + policy.ActiveInterface +
                                          "；持續監測 A=" + policy.StandbyInterface +
                                          "，A 穩定後才切回。", false);
                                }
                                else
                                {
                                    policy.MarkSwitchFailure(now, settings.FailoverSwitchBackoffSeconds);
                                }
                            }
                        }
                    }
                    else
                    {
                        badReported = false;
                        standbyReported = false;
                        if (settings.SmartSelectionEnabled)
                        {
                            if (now >= nextSmartProbeUtc)
                            {
                                lastStandby = await MeasureLinkAsync(policy.StandbyInterface, settings, token)
                                    .ConfigureAwait(false);
                                if (lastStandby.Cancelled) { break; }
                                AddStatistics(statistics, policy.StandbyInterface, lastStandby, settings);
                                nextSmartProbeUtc = now.AddMilliseconds(settings.SmartProbeIntervalMs);
                            }

                            if (now >= nextSmartDecisionUtc)
                            {
                                nextSmartDecisionUtc = now.AddSeconds(settings.SmartDecisionIntervalSeconds);
                                EwmaLinkStats activeStats;
                                EwmaLinkStats standbyStats;
                                if (lastStandby != null && lastStandby.Healthy &&
                                    statistics.TryGetValue(policy.ActiveInterface, out activeStats) &&
                                    statistics.TryGetValue(policy.StandbyInterface, out standbyStats) &&
                                    standbyStats.HasSample && activeStats.HasSample &&
                                    SwitchBudgetAvailable(switchHistory, settings) &&
                                    policy.ShouldSmartSwitch(
                                        now,
                                        activeStats.Score(settings.FailoverPingTimeoutMs),
                                        standbyStats.Score(settings.FailoverPingTimeoutMs),
                                        settings.SmartMarginMs,
                                        settings.SmartMinDwellSeconds,
                                        settings.SmartSwitchHoldSeconds))
                                {
                                    bool switched = await SwitchPairAsync(
                                        policy.StandbyInterface,
                                        policy.ActiveInterface,
                                        settings,
                                        token).ConfigureAwait(false);
                                    if (switched)
                                    {
                                        policy.MarkSwitchSuccess(
                                            now,
                                            settings.FailoverSwitchCooldownSeconds,
                                            false,
                                            "智慧選路");
                                        switchHistory.Add(now);
                                        nextSmartProbeUtc = DateTime.MinValue;
                                        nextSmartDecisionUtc = now.AddSeconds(settings.SmartDecisionIntervalSeconds);
                                        Write("智慧選路：" + policy.ActiveInterface +
                                              " 的健康分數優於原線路，已切換。", false);
                                    }
                                    else
                                    {
                                        policy.MarkSwitchFailure(now, settings.FailoverSwitchBackoffSeconds);
                                    }
                                }
                            }
                        }
                    }

                    PublishStatus(BuildStatus(policy, settings, lastActive, lastStandby));
                }

                await Task.Delay(settings.FailoverProbeIntervalMs, token).ConfigureAwait(false);
            }
        }

        private async Task<LinkHealth> MeasureLinkAsync(
            string interfaceName,
            MonitorSettings settings,
            CancellationToken token)
        {
            InterfaceSnapshot snapshot = NetworkInfo.GetInterfaceSnapshot(interfaceName);
            if (snapshot == null)
            {
                return LinkHealth.Unhealthy(interfaceName, "找不到網卡");
            }
            if (snapshot.Status != System.Net.NetworkInformation.OperationalStatus.Up)
            {
                return LinkHealth.Unhealthy(interfaceName, "介面狀態為 " + snapshot.Status);
            }
            if (string.IsNullOrWhiteSpace(snapshot.IPv4))
            {
                return LinkHealth.Unhealthy(interfaceName, "沒有可用 IPv4");
            }
            if (string.IsNullOrWhiteSpace(snapshot.Gateway))
            {
                return LinkHealth.Unhealthy(interfaceName, "沒有 IPv4 預設閘道");
            }

            List<string> targets = GetTargets(settings);
            List<int> successfulLatencies = new List<int>();
            int healthyCount = 0;
            string firstError = string.Empty;
            foreach (string target in targets)
            {
                ProbeResult result = await NetworkProbe.TcpAsync(
                    snapshot.IPv4,
                    target,
                    settings.Port,
                    settings.FailoverPingTimeoutMs,
                    token).ConfigureAwait(false);
                if (result.State == ProbeState.Cancelled)
                {
                    return LinkHealth.CancelledResult(interfaceName);
                }
                if (result.State == ProbeState.Success)
                {
                    successfulLatencies.Add(result.LatencyMs);
                    if (result.IsHealthy(settings.FailoverThresholdMs))
                    {
                        healthyCount++;
                    }
                }
                else if (firstError.Length == 0)
                {
                    firstError = string.IsNullOrWhiteSpace(result.Error) ? result.State.ToString() : result.Error;
                }
            }

            int total = Math.Max(1, targets.Count);
            double loss = 1.0 - (double)successfulLatencies.Count / total;
            double average = successfulLatencies.Count == 0
                ? settings.FailoverPingTimeoutMs
                : successfulLatencies.Average();
            double jitter = 0;
            if (successfulLatencies.Count > 1)
            {
                for (int i = 1; i < successfulLatencies.Count; i++)
                {
                    jitter += Math.Abs(successfulLatencies[i] - successfulLatencies[i - 1]);
                }
                jitter /= successfulLatencies.Count - 1;
            }
            bool healthy = healthyCount >= Math.Max(1, (targets.Count + 1) / 2);
            return new LinkHealth
            {
                InterfaceName = interfaceName,
                SourceIp = snapshot.IPv4,
                Gateway = snapshot.Gateway,
                Healthy = healthy,
                Cancelled = false,
                TotalTargets = targets.Count,
                HealthyTargets = healthyCount,
                SuccessfulTargets = successfulLatencies.Count,
                AverageMs = average,
                JitterMs = jitter,
                LossRate = loss,
                Error = healthy ? string.Empty : firstError
            };
        }

        private async Task<bool> ApplyPairAsync(
            string primary,
            string backup,
            MonitorSettings settings,
            CancellationToken token)
        {
            bool entered = false;
            try
            {
                entered = await networkActionGate.WaitAsync(0, token).ConfigureAwait(false);
                if (!entered)
                {
                    Write("A/B 初始化略過：另一個網路動作正在執行。", true);
                    return false;
                }

                CommandResult primaryResult = await NetworkInfo.SetInterfaceMetricAsync(
                    primary, settings.FailoverPrimaryMetric, false, token).ConfigureAwait(false);
                if (!await ReportAndVerifyMetricAsync(
                    "初始化 A metric=" + settings.FailoverPrimaryMetric,
                    primary,
                    settings.FailoverPrimaryMetric,
                    primaryResult,
                    token).ConfigureAwait(false))
                {
                    await RollbackManagedPairAsync(primary, backup, settings).ConfigureAwait(false);
                    return false;
                }

                CommandResult backupResult = await NetworkInfo.SetInterfaceMetricAsync(
                    backup, settings.FailoverBackupMetric, false, token).ConfigureAwait(false);
                if (!await ReportAndVerifyMetricAsync(
                    "初始化 B metric=" + settings.FailoverBackupMetric,
                    backup,
                    settings.FailoverBackupMetric,
                    backupResult,
                    token).ConfigureAwait(false))
                {
                    await RollbackManagedPairAsync(primary, backup, settings).ConfigureAwait(false);
                    return false;
                }

                bool routeOk = await ReportRouteVerificationAsync(primary, token).ConfigureAwait(false);
                if (!routeOk)
                {
                    await RollbackManagedPairAsync(primary, backup, settings).ConfigureAwait(false);
                }
                return routeOk;
            }
            finally
            {
                if (entered)
                {
                    networkActionGate.Release();
                }
            }
        }

        private async Task<bool> SwitchPairAsync(
            string newPrimary,
            string newBackup,
            MonitorSettings settings,
            CancellationToken token)
        {
            bool entered = false;
            try
            {
                entered = await networkActionGate.WaitAsync(0, token).ConfigureAwait(false);
                if (!entered)
                {
                    Write("A/B 切換略過：另一個網路動作正在執行。", true);
                    return false;
                }

                CommandResult primary = await NetworkInfo.SetInterfaceMetricAsync(
                    newPrimary, settings.FailoverPrimaryMetric, false, token).ConfigureAwait(false);
                if (!await ReportAndVerifyMetricAsync(
                    "切換主線 " + newPrimary + " metric=" + settings.FailoverPrimaryMetric,
                    newPrimary,
                    settings.FailoverPrimaryMetric,
                    primary,
                    token).ConfigureAwait(false))
                {
                    await RollbackManagedPairAsync(newPrimary, newBackup, settings).ConfigureAwait(false);
                    return false;
                }

                CommandResult backup = await NetworkInfo.SetInterfaceMetricAsync(
                    newBackup, settings.FailoverBackupMetric, false, token).ConfigureAwait(false);
                if (!await ReportAndVerifyMetricAsync(
                    "切換備援 " + newBackup + " metric=" + settings.FailoverBackupMetric,
                    newBackup,
                    settings.FailoverBackupMetric,
                    backup,
                    token).ConfigureAwait(false))
                {
                    Write("備援 metric 設定失敗，保留目前 metric 並等待退避重試。", true);
                    await RollbackManagedPairAsync(newPrimary, newBackup, settings).ConfigureAwait(false);
                    return false;
                }

                bool routeOk = await ReportRouteVerificationAsync(newPrimary, token).ConfigureAwait(false);
                if (!routeOk)
                {
                    await RollbackManagedPairAsync(newPrimary, newBackup, settings).ConfigureAwait(false);
                }
                return routeOk;
            }
            finally
            {
                if (entered)
                {
                    networkActionGate.Release();
                }
            }
        }

        private async Task<bool> ReportAndVerifyMetricAsync(
            string action,
            string interfaceName,
            int expectedMetric,
            CommandResult result,
            CancellationToken token)
        {
            if (result == null || !result.Succeeded)
            {
                Write(action + "：失敗（" + FirstError(result) + "）。", true);
                return false;
            }
            bool verified = await NetworkInfo.VerifyInterfaceMetricAsync(
                interfaceName, expectedMetric, token).ConfigureAwait(false);
            if (!verified)
            {
                Write(action + "：命令成功，但讀回 metric 不一致。", true);
                return false;
            }
            Write(action + "：成功並已驗證。", false);
            return true;
        }

        private async Task RollbackManagedPairAsync(
            string oldStandby,
            string oldActive,
            MonitorSettings settings)
        {
            try
            {
                CommandResult standby = await NetworkInfo.SetInterfaceMetricAsync(
                    oldStandby,
                    settings.FailoverBackupMetric,
                    false,
                    CancellationToken.None).ConfigureAwait(false);
                CommandResult active = await NetworkInfo.SetInterfaceMetricAsync(
                    oldActive,
                    settings.FailoverPrimaryMetric,
                    false,
                    CancellationToken.None).ConfigureAwait(false);
                if (standby == null || !standby.Succeeded || active == null || !active.Succeeded)
                {
                    Write("A/B 部分切換失敗，rollback metric 也未完全成功。", true);
                }
                else
                {
                    Write("A/B 部分切換已 rollback。", false);
                }
            }
            catch (Exception ex)
            {
                Write("A/B rollback 發生錯誤：" + ex.Message, true);
            }
        }

        private async Task<bool> ReportRouteVerificationAsync(
            string expectedInterface,
            CancellationToken token)
        {
            DefaultRouteState route = await NetworkInfo.ReadPreferredDefaultRouteAsync(token)
                .ConfigureAwait(false);
            if (route == null || !string.IsNullOrWhiteSpace(route.Error))
            {
                Write("路由驗證無法完成：" + (route == null ? "沒有結果" : route.Error) +
                      "；metric 已驗證，但請留意其他 VPN 或自訂 route。", true);
                return true;
            }
            if (!string.Equals(route.InterfaceName, expectedInterface, StringComparison.OrdinalIgnoreCase))
            {
                Write("路由驗證失敗：目前最低預設 route 是「" + route.InterfaceName +
                      "」，不是預期的「" + expectedInterface + "」。", true);
                return false;
            }
            Write("路由驗證成功：目前預設 route 使用「" + expectedInterface +
                    "」（總 metric 約 " + (route.RouteMetric + route.InterfaceMetric) + "）。", false);
            return true;
        }

        private async Task<bool> RestoreOriginalAsync(
            InterfaceMetricState primary,
            InterfaceMetricState backup)
        {
            bool entered = false;
            try
            {
                entered = await networkActionGate.WaitAsync(
                    TimeSpan.FromSeconds(8), CancellationToken.None).ConfigureAwait(false);
                if (!entered)
                {
                    Write("無法在期限內取得 network action lock，延後復原 A/B metric。", true);
                    return false;
                }
                bool primaryOk = await RestoreMetricAsync(primary).ConfigureAwait(false);
                bool backupOk = await RestoreMetricAsync(backup).ConfigureAwait(false);
                if (primaryOk && backupOk)
                {
                    Write("A/B metric 已復原並通過讀回驗證。", false);
                }
                return primaryOk && backupOk;
            }
            finally
            {
                if (entered)
                {
                    networkActionGate.Release();
                }
            }
        }

        private async Task<bool> RestoreMetricAsync(InterfaceMetricState original)
        {
            if (original == null || string.IsNullOrWhiteSpace(original.InterfaceName))
            {
                return false;
            }
            CommandResult result = await NetworkInfo.SetInterfaceMetricAsync(
                original.InterfaceName,
                original.Metric,
                original.AutomaticMetric,
                CancellationToken.None).ConfigureAwait(false);
            if (result == null || !result.Succeeded)
            {
                Write("復原 " + original.InterfaceName + "：失敗（" + FirstError(result) + "）。", true);
                return false;
            }
            InterfaceMetricState after = await NetworkInfo.ReadInterfaceMetricAsync(
                original.InterfaceName, CancellationToken.None).ConfigureAwait(false);
            bool verified = IsOriginalState(after, original);
            Write("復原 " + original.InterfaceName + "：" +
                  (verified ? "成功。" : "讀回驗證不一致。"), !verified);
            return verified;
        }

        private FailoverStatus BuildStatus(
            FailoverPolicy policy,
            MonitorSettings settings,
            LinkHealth active,
            LinkHealth standby)
        {
            return new FailoverStatus
            {
                Ready = true,
                InFailover = policy.InFailover,
                SmartSelection = settings.SmartSelectionEnabled,
                ActiveInterface = policy.ActiveInterface,
                StandbyInterface = policy.StandbyInterface,
                ActiveHealth = active == null ? "尚未測試" : active.Display,
                StandbyHealth = standby == null ? "尚未測試" : standby.Display,
                Detail = policy.InFailover ? "故障切換中，等待原主線穩定恢復。" : "主線優先監測中。",
                LastSwitchUtc = policy.LastSwitchUtc,
                LastSwitchReason = policy.LastSwitchReason,
                SwitchCount = policy.SwitchCount
            };
        }

        private static void AddStatistics(
            IDictionary<string, EwmaLinkStats> statistics,
            string interfaceName,
            LinkHealth health,
            MonitorSettings settings)
        {
            EwmaLinkStats stats;
            if (!statistics.TryGetValue(interfaceName, out stats))
            {
                stats = new EwmaLinkStats();
                statistics[interfaceName] = stats;
            }
            stats.Add(health, settings.SmartEwmaAlpha, settings.FailoverPingTimeoutMs);
        }

        private static bool SwitchBudgetAvailable(
            List<DateTime> switchHistory,
            MonitorSettings settings)
        {
            DateTime cutoff = DateTime.UtcNow.AddHours(-1);
            switchHistory.RemoveAll(delegate(DateTime time) { return time < cutoff; });
            return switchHistory.Count < settings.SmartMaxSwitchesPerHour;
        }

        private static List<string> GetTargets(MonitorSettings settings)
        {
            IEnumerable<string> source = settings.FailoverTargets;
            if (source == null || !source.Any())
            {
                source = new[] { settings.FailoverTarget };
            }
            return source
                .Where(delegate(string target) { return !string.IsNullOrWhiteSpace(target); })
                .Select(delegate(string target) { return target.Trim(); })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();
        }

        private static bool MetricStateIsUsable(InterfaceMetricState state)
        {
            return state != null && string.IsNullOrWhiteSpace(state.Error) && state.Metric > 0;
        }

        private static bool IsOriginalState(InterfaceMetricState current, MetricRecoveryEntry original)
        {
            if (current == null || !string.IsNullOrWhiteSpace(current.Error)) { return false; }
            if (original.OriginalAutomaticMetric)
            {
                return current.AutomaticMetric;
            }
            return !current.AutomaticMetric && current.Metric == original.OriginalMetric;
        }

        private static bool IsOriginalState(InterfaceMetricState current, InterfaceMetricState original)
        {
            if (current == null || original == null || !string.IsNullOrWhiteSpace(current.Error)) { return false; }
            if (original.AutomaticMetric)
            {
                return current.AutomaticMetric;
            }
            return !current.AutomaticMetric && current.Metric == original.Metric;
        }

        private static bool IsManagedState(InterfaceMetricState current, MetricRecoveryEntry entry)
        {
            return current != null && !current.AutomaticMetric &&
                   (current.Metric == entry.ManagedPrimaryMetric ||
                    current.Metric == entry.ManagedBackupMetric);
        }

        private static string FirstError(CommandResult result)
        {
            if (result == null) { return "沒有收到結果"; }
            if (result.Cancelled) { return "已取消"; }
            if (result.TimedOut) { return "timeout"; }
            string text = CommandRunner.GetUsefulError(result.StandardError);
            if (text.Length > 0) { return text; }
            text = CommandRunner.GetUsefulError(result.StandardOutput);
            if (text.Length > 0) { return text; }
            return "exit code " + result.ExitCode;
        }

        private void PublishStatus(FailoverStatus status)
        {
            if (statusChanged == null) { return; }
            try { statusChanged(status); } catch { }
        }

        private void Write(string message, bool warning)
        {
            if (log != null)
            {
                try { log(message, warning); }
                catch { }
            }
        }

        private sealed class LinkHealth
        {
            public string InterfaceName { get; set; }
            public string SourceIp { get; set; }
            public string Gateway { get; set; }
            public bool Healthy { get; set; }
            public bool Cancelled { get; set; }
            public int TotalTargets { get; set; }
            public int HealthyTargets { get; set; }
            public int SuccessfulTargets { get; set; }
            public double AverageMs { get; set; }
            public double JitterMs { get; set; }
            public double LossRate { get; set; }
            public string Error { get; set; }

            public string Display
            {
                get
                {
                    if (Cancelled) { return "已取消"; }
                    if (TotalTargets == 0)
                    {
                        return string.IsNullOrWhiteSpace(Error) ? "未測試" : Error;
                    }
                    string prefix = Healthy ? "健康" : "異常";
                    return prefix + " " + Math.Round(AverageMs, 1) + " ms / loss " +
                           Math.Round(LossRate * 100, 1) + "% / jitter " + Math.Round(JitterMs, 1) +
                           " ms（" + HealthyTargets + "/" + TotalTargets + "）" +
                           (string.IsNullOrWhiteSpace(Error) ? string.Empty : "：" + Error);
                }
            }

            public static LinkHealth Unhealthy(string interfaceName, string error)
            {
                return new LinkHealth
                {
                    InterfaceName = interfaceName,
                    Healthy = false,
                    TotalTargets = 0,
                    Error = error
                };
            }

            public static LinkHealth CancelledResult(string interfaceName)
            {
                return new LinkHealth { InterfaceName = interfaceName, Cancelled = true };
            }
        }

        private sealed class EwmaLinkStats
        {
            public bool HasSample { get; private set; }
            private double ewmaLatency;
            private double ewmaLoss;
            private double ewmaJitter;

            public void Add(LinkHealth health, double alpha, int timeoutMs)
            {
                if (health == null || health.Cancelled) { return; }
                double latency = health.SuccessfulTargets == 0 ? timeoutMs : health.AverageMs;
                if (!HasSample)
                {
                    ewmaLatency = latency;
                    ewmaLoss = health.LossRate;
                    ewmaJitter = health.JitterMs;
                    HasSample = true;
                    return;
                }
                ewmaLatency = alpha * latency + (1 - alpha) * ewmaLatency;
                ewmaLoss = alpha * health.LossRate + (1 - alpha) * ewmaLoss;
                ewmaJitter = alpha * health.JitterMs + (1 - alpha) * ewmaJitter;
            }

            public double Score(int timeoutMs)
            {
                return ewmaLatency + ewmaLoss * timeoutMs + ewmaJitter;
            }
        }
    }
}
