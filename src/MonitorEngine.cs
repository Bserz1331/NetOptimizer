using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetOptimizerV2
{
    internal sealed class MonitorEngine : IDisposable
    {
        private sealed class RunState
        {
            public readonly CancellationTokenSource Cancellation =
                new CancellationTokenSource();
            public readonly List<Task> RefreshTasks = new List<Task>();
            public Task LoopTask;
        }

        private readonly object sync = new object();
        private readonly SemaphoreSlim refreshGate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource lifetimeCancellation = new CancellationTokenSource();
        private readonly List<RunState> runs = new List<RunState>();
        private readonly List<Task> unboundRefreshTasks = new List<Task>();
        private RunState activeRun;
        private int failoverReady;
        private bool disposed;
        private bool resourcesDisposed;

        public event EventHandler<EngineLogEventArgs> LogRaised;
        public event EventHandler<ProbeEventArgs> ProbeCompleted;
        public event EventHandler<FailoverStatusEventArgs> FailoverStatusChanged;

        public bool IsRunning
        {
            get
            {
                lock (sync)
                {
                    return activeRun != null && activeRun.LoopTask != null &&
                           !activeRun.LoopTask.IsCompleted;
                }
            }
        }

        public void Start(MonitorSettings source)
        {
            if (source == null) { throw new ArgumentNullException("source"); }
            MonitorSettings settings = source.Clone();
            settings.Normalize();

            RunState run = null;
            lock (sync)
            {
                ThrowIfDisposed();
                if (activeRun != null && activeRun.LoopTask != null &&
                    !activeRun.LoopTask.IsCompleted)
                {
                    return;
                }

                if (activeRun != null)
                {
                    CompleteRunNoLock(activeRun);
                }
                run = new RunState();
                runs.Add(run);
                activeRun = run;
                try
                {
                    run.LoopTask = Task.Run(async delegate
                    {
                        await LoopAsync(settings, run.Cancellation.Token).ConfigureAwait(false);
                    });
                    run.LoopTask.ContinueWith(delegate(Task completed)
                    {
                        CompleteRun(run);
                    }, TaskScheduler.Default);
                }
                catch
                {
                    activeRun = null;
                    runs.Remove(run);
                    run.Cancellation.Dispose();
                    TryDisposeResourcesNoLock();
                    throw;
                }
            }

            RaiseFailoverStatus(new FailoverStatus
            {
                Ready = false,
                SmartSelection = settings.SmartSelectionEnabled,
                Detail = "A/B 尚未就緒。"
            });
            RaiseLog("監測已啟動。", false);
        }

        public void Stop()
        {
            RunState run;
            lock (sync)
            {
                run = activeRun;
                if (run != null)
                {
                    CancelSafely(run.Cancellation);
                }
            }

            if (run != null)
            {
                WaitForTasks(new[] { run.LoopTask }, TimeSpan.FromSeconds(15));
                CompleteRun(run);
            }
        }

        public void RefreshNow(MonitorSettings source)
        {
            if (source == null) { throw new ArgumentNullException("source"); }
            MonitorSettings settings = source.Clone();
            settings.Normalize();
            RunState run;
            Task task;
            lock (sync)
            {
                ThrowIfDisposed();
                run = activeRun != null && activeRun.LoopTask != null &&
                      !activeRun.LoopTask.IsCompleted ? activeRun : null;
                CancellationToken token = run == null
                    ? lifetimeCancellation.Token
                    : run.Cancellation.Token;
                task = Task.Run(async delegate
                {
                    await RefreshWithGateAsync(settings, token, true).ConfigureAwait(false);
                });
                if (run == null)
                {
                    unboundRefreshTasks.Add(task);
                }
                else
                {
                    run.RefreshTasks.Add(task);
                }
            }
            task.ContinueWith(delegate(Task completed)
            {
                CompleteRefresh(run, completed);
            }, TaskScheduler.Default);
        }

        private void CompleteRun(RunState run)
        {
            lock (sync)
            {
                CompleteRunNoLock(run);
            }
        }

        private void CompleteRunNoLock(RunState run)
        {
            if (run == null || run.LoopTask == null || !run.LoopTask.IsCompleted)
            {
                return;
            }

            if (ReferenceEquals(activeRun, run))
            {
                activeRun = null;
            }
            RemoveCompletedTasksNoLock(run.RefreshTasks);
            if (run.RefreshTasks.Count == 0 && runs.Remove(run))
            {
                DisposeCancellation(run.Cancellation);
            }
            TryDisposeResourcesNoLock();
        }

        private void CompleteRefresh(RunState run, Task completed)
        {
            lock (sync)
            {
                if (run == null)
                {
                    unboundRefreshTasks.Remove(completed);
                }
                else
                {
                    run.RefreshTasks.Remove(completed);
                    CompleteRunNoLock(run);
                }
                TryDisposeResourcesNoLock();
            }
        }

        private Task[] GetOutstandingTasksNoLock()
        {
            List<Task> tasks = new List<Task>();
            foreach (RunState run in runs)
            {
                if (run.LoopTask != null) { tasks.Add(run.LoopTask); }
                tasks.AddRange(run.RefreshTasks);
            }
            tasks.AddRange(unboundRefreshTasks);
            return tasks.ToArray();
        }

        private void PruneCompletedTasksNoLock()
        {
            for (int i = runs.Count - 1; i >= 0; i--)
            {
                RunState run = runs[i];
                RemoveCompletedTasksNoLock(run.RefreshTasks);
                if (run.LoopTask != null && run.LoopTask.IsCompleted &&
                    run.RefreshTasks.Count == 0)
                {
                    if (ReferenceEquals(activeRun, run))
                    {
                        activeRun = null;
                    }
                    runs.RemoveAt(i);
                    DisposeCancellation(run.Cancellation);
                }
            }
            RemoveCompletedTasksNoLock(unboundRefreshTasks);
        }

        private static void RemoveCompletedTasksNoLock(List<Task> tasks)
        {
            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                if (tasks[i] == null || tasks[i].IsCompleted)
                {
                    tasks.RemoveAt(i);
                }
            }
        }

        private void TryDisposeResourcesNoLock()
        {
            if (!disposed || resourcesDisposed || activeRun != null || runs.Count != 0 ||
                unboundRefreshTasks.Count != 0)
            {
                return;
            }

            resourcesDisposed = true;
            try { refreshGate.Dispose(); } catch (ObjectDisposedException) { }
            try { lifetimeCancellation.Dispose(); } catch (ObjectDisposedException) { }
        }

        private static void CancelSafely(CancellationTokenSource source)
        {
            if (source == null) { return; }
            try { source.Cancel(); }
            catch (AggregateException) { }
            catch (ObjectDisposedException) { }
        }

        private static void DisposeCancellation(CancellationTokenSource source)
        {
            if (source == null) { return; }
            try { source.Dispose(); }
            catch (ObjectDisposedException) { }
        }

        private static void WaitForTasks(Task[] tasks, TimeSpan timeout)
        {
            if (tasks == null || tasks.Length == 0) { return; }
            DateTime deadline = DateTime.UtcNow.Add(timeout);
            int currentTaskId = Task.CurrentId.HasValue ? Task.CurrentId.Value : -1;
            foreach (Task task in tasks)
            {
                if (task == null || task.IsCompleted || task.Id == currentTaskId)
                {
                    continue;
                }

                TimeSpan remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) { break; }
                try { task.Wait(remaining); }
                catch (AggregateException) { }
                catch (ObjectDisposedException) { }
            }
        }

        private async Task LoopAsync(MonitorSettings settings, CancellationToken token)
        {
            Interlocked.Exchange(ref failoverReady, 0);
            try
            {
                Task tcpTask = TcpLoopCoreAsync(settings, token);
                Task failoverTask = settings.FailoverEnabled
                    ? new FailoverManager(refreshGate, RaiseLog, RaiseFailoverStatus).RunAsync(settings, token)
                    : Task.CompletedTask;
                await Task.WhenAll(tcpTask, failoverTask).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (!token.IsCancellationRequested)
                {
                    RaiseLog("監測被意外取消。", true);
                }
            }
            catch (Exception ex)
            {
                RaiseLog("監測迴圈停止：" + ex.Message, true);
            }
            finally
            {
                RaiseLog("監測已停止。", false);
                RaiseFailoverStatus(new FailoverStatus
                {
                    Ready = false,
                    SmartSelection = settings.SmartSelectionEnabled,
                    Detail = "A/B 監測已停止。"
                });
            }
        }

        private async Task TcpLoopCoreAsync(MonitorSettings settings, CancellationToken token)
        {
            int currentIndex = 0;
            int goodCount = 0;
            int badStreak = 0;
            int intervalMs = settings.FastIntervalMs;
            DateTime lastRefreshAttemptUtc = DateTime.MinValue;
            bool cooldownLogged = false;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    string target = settings.Targets[currentIndex % settings.Targets.Count];
                    currentIndex = (currentIndex + 1) % settings.Targets.Count;
                    ProbeResult result = await ProbeAsync(target, settings.Port, settings.ProbeTimeoutMs, token)
                        .ConfigureAwait(false);

                    if (result.State == ProbeState.Cancelled)
                    {
                        break;
                    }

                    bool healthy = result.IsHealthy(settings.ThresholdMs);
                    if (healthy)
                    {
                        badStreak = 0;
                        cooldownLogged = false;
                        goodCount++;
                        if (goodCount >= settings.HealthySamplesBeforeSlow)
                        {
                            goodCount = 0;
                            intervalMs = settings.SlowIntervalMs;
                        }
                    }
                    else
                    {
                        goodCount = 0;
                        intervalMs = settings.FastIntervalMs;
                        badStreak++;

                        if (badStreak >= settings.ConsecutiveFailuresBeforeRefresh)
                        {
                            if (!settings.EnableRefresh)
                            {
                                if (!cooldownLogged)
                                {
                                    RaiseLog("偵測到異常，但刷新動作已停用。", true);
                                    cooldownLogged = true;
                                }
                            }
                            else if (DateTime.UtcNow - lastRefreshAttemptUtc >=
                                     TimeSpan.FromSeconds(settings.CooldownSeconds))
                            {
                                lastRefreshAttemptUtc = DateTime.UtcNow;
                                bool refreshed = await RefreshWithGateAsync(
                                    settings, token, false).ConfigureAwait(false);
                                if (refreshed)
                                {
                                    badStreak = 0;
                                    cooldownLogged = false;
                                }
                                else if (!cooldownLogged)
                                {
                                    RaiseLog("刷新未完成，保留異常計數，稍後再試。", true);
                                    cooldownLogged = true;
                                }
                            }
                            else if (!cooldownLogged)
                            {
                                TimeSpan elapsed = DateTime.UtcNow - lastRefreshAttemptUtc;
                                double remaining = Math.Max(0, settings.CooldownSeconds - elapsed.TotalSeconds);
                                RaiseLog("偵測到異常，但仍在刷新冷卻時間內，剩餘約 " +
                                         Math.Ceiling(remaining) + " 秒。", true);
                                cooldownLogged = true;
                            }
                        }
                    }

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    RaiseProbe(result, badStreak, intervalMs);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    await Task.Delay(intervalMs, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                if (!token.IsCancellationRequested)
                {
                    RaiseLog("監測被意外取消。", true);
                }
            }
            catch (Exception ex)
            {
                RaiseLog("監測迴圈停止：" + ex.Message, true);
            }
        }

        private async Task<ProbeResult> ProbeAsync(
            string target,
            int port,
            int timeoutMs,
            CancellationToken token)
        {
            return await NetworkProbe.TcpAsync(
                null, target, port, timeoutMs, token).ConfigureAwait(false);
        }

        private async Task<bool> RefreshWithGateAsync(
            MonitorSettings settings,
            CancellationToken token,
            bool manual)
        {
            if (!manual && settings.FailoverEnabled && settings.SuppressRefreshDuringFailover &&
                Thread.VolatileRead(ref failoverReady) == 1)
            {
                RaiseLog("A/B 監測已就緒，暫停一般自動刷新，避免與故障切換互相干擾。", true);
                return false;
            }

            bool entered = false;
            try
            {
                entered = await refreshGate.WaitAsync(0, token).ConfigureAwait(false);
                if (!entered)
                {
                    RaiseLog("已有刷新動作執行中，略過重複請求。", true);
                    return false;
                }

                RaiseLog(manual ? "開始手動刷新。" : "達到異常門檻，開始刷新。", false);
                return await SoftRefreshAsync(settings, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                RaiseLog("刷新動作已取消。", true);
                return false;
            }
            catch (Exception ex)
            {
                RaiseLog("刷新動作發生錯誤：" + ex.Message, true);
                return false;
            }
            finally
            {
                if (entered)
                {
                    refreshGate.Release();
                }
            }
        }

        private async Task<bool> SoftRefreshAsync(MonitorSettings settings, CancellationToken token)
        {
            bool success = true;
            if (!NetworkInfo.IsAdministrator())
            {
                RaiseLog("目前不是系統管理員；ARP 與 MTU 動作可能被 Windows 拒絕。", true);
            }

            if (settings.FlushDns)
            {
                CommandResult result = await CommandRunner.RunAsync(
                    "ipconfig.exe", new[] { "/flushdns" }, 5000, token).ConfigureAwait(false);
                bool commandOk = ReportCommand("DNS cache", result);
                success = commandOk && success;
            }

            if (settings.ClearArp)
            {
                CommandResult result = await CommandRunner.RunAsync(
                    "arp.exe", new[] { "-d", "*" }, 5000, token).ConfigureAwait(false);
                bool commandOk = ReportCommand("ARP cache", result);
                success = commandOk && success;
            }

            if (!settings.PulseMtu)
            {
                RaiseLog("MTU 刷新已停用。", false);
                return success;
            }

            int originalMtu = NetworkInfo.TryGetMtu(settings.InterfaceName);
            if (originalMtu <= 0)
            {
                RaiseLog("找不到網卡「" + settings.InterfaceName + "」的 IPv4 MTU，略過 MTU 刷新。", true);
                return false;
            }
            if (originalMtu <= settings.MtuPulseValue)
            {
                RaiseLog("目前 MTU 為 " + originalMtu + "，不會把它提高到 " +
                         settings.MtuPulseValue + "；略過 MTU 刷新。", false);
                return success;
            }

            CommandResult pulse = await CommandRunner.RunAsync(
                "netsh.exe",
                new[]
                {
                    "interface", "ipv4", "set", "subinterface", settings.InterfaceName,
                    "mtu=" + settings.MtuPulseValue, "store=active"
                },
                5000,
                token).ConfigureAwait(false);
            bool pulseOk = ReportCommand("MTU pulse -> " + settings.MtuPulseValue, pulse);
            success = pulseOk && success;
            if (!pulseOk)
            {
                return false;
            }

            try
            {
                await Task.Delay(150, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                RaiseLog("MTU pulse 等待被取消，仍會先嘗試復原原始值。", true);
                success = false;
            }

            CommandResult restore = await CommandRunner.RunAsync(
                "netsh.exe",
                new[]
                {
                    "interface", "ipv4", "set", "subinterface", settings.InterfaceName,
                    "mtu=" + originalMtu, "store=active"
                },
                5000,
                CancellationToken.None).ConfigureAwait(false);
            bool restoreOk = ReportCommand("MTU restore -> " + originalMtu, restore);
            return restoreOk && success;
        }

        private bool ReportCommand(string name, CommandResult result)
        {
            if (result == null)
            {
                RaiseLog(name + "：沒有收到結果。", true);
                return false;
            }
            if (result.Succeeded)
            {
                RaiseLog(name + "：成功。", false);
                return true;
            }
            if (result.Cancelled)
            {
                RaiseLog(name + "：已取消。", true);
                return false;
            }
            if (result.TimedOut)
            {
                RaiseLog(name + "：timeout，程序已終止。", true);
                return false;
            }

            string detail = CommandRunner.GetUsefulError(result.StandardError);
            if (detail.Length == 0) { detail = CommandRunner.GetUsefulError(result.StandardOutput); }
            if (detail.Length == 0) { detail = "exit code " + result.ExitCode; }
            RaiseLog(name + "：失敗（" + detail + "）。", true);
            return false;
        }

        private void RaiseLog(string message, bool warning)
        {
            EventHandler<EngineLogEventArgs> handler = LogRaised;
            if (handler == null) { return; }
            try { handler(this, new EngineLogEventArgs(DateTime.Now.ToString("HH:mm:ss") + " " + message, warning)); }
            catch { }
        }

        private void RaiseProbe(ProbeResult result, int badStreak, int intervalMs)
        {
            EventHandler<ProbeEventArgs> handler = ProbeCompleted;
            if (handler == null) { return; }
            try { handler(this, new ProbeEventArgs(result, badStreak, intervalMs)); }
            catch { }
        }

        private void RaiseFailoverStatus(FailoverStatus status)
        {
            Interlocked.Exchange(ref failoverReady, status != null && status.Ready ? 1 : 0);
            EventHandler<FailoverStatusEventArgs> handler = FailoverStatusChanged;
            if (handler == null) { return; }
            try { handler(this, new FailoverStatusEventArgs(status)); }
            catch { }
        }

        private void ThrowIfDisposed()
        {
            if (disposed) { throw new ObjectDisposedException(GetType().Name); }
        }

        public void Dispose()
        {
            Task[] outstanding;
            lock (sync)
            {
                if (disposed) { return; }
                disposed = true;
                LogRaised = null;
                ProbeCompleted = null;
                FailoverStatusChanged = null;
                CancelSafely(lifetimeCancellation);
                foreach (RunState run in runs)
                {
                    CancelSafely(run.Cancellation);
                }
                outstanding = GetOutstandingTasksNoLock();
            }
            WaitForTasks(outstanding, TimeSpan.FromSeconds(15));
            lock (sync)
            {
                PruneCompletedTasksNoLock();
                TryDisposeResourcesNoLock();
            }
        }
    }
}
