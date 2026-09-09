using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetOptimizerV2
{
    internal sealed class MonitorEngine : IDisposable
    {
        private readonly object sync = new object();
        private readonly SemaphoreSlim refreshGate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource lifetimeCancellation = new CancellationTokenSource();
        private CancellationTokenSource cancellation;
        private Task loopTask;
        private Task refreshTask;
        private int failoverReady;
        private bool disposed;

        public event EventHandler<EngineLogEventArgs> LogRaised;
        public event EventHandler<ProbeEventArgs> ProbeCompleted;
        public event EventHandler<FailoverStatusEventArgs> FailoverStatusChanged;

        public bool IsRunning
        {
            get
            {
                lock (sync)
                {
                    return loopTask != null && !loopTask.IsCompleted;
                }
            }
        }

        public void Start(MonitorSettings source)
        {
            if (source == null) { throw new ArgumentNullException("source"); }
            MonitorSettings settings = source.Clone();
            settings.Normalize();

            lock (sync)
            {
                ThrowIfDisposed();
                if (loopTask != null && !loopTask.IsCompleted)
                {
                    return;
                }

                if (cancellation != null)
                {
                    cancellation.Dispose();
                }
                cancellation = new CancellationTokenSource();
                CancellationToken token = cancellation.Token;
                loopTask = Task.Run(async delegate
                {
                    await LoopAsync(settings, token).ConfigureAwait(false);
                });
            }

            RaiseLog("監測已啟動。", false);
        }

        public void Stop()
        {
            Task task;
            CancellationTokenSource source;
            lock (sync)
            {
                task = loopTask;
                source = cancellation;
                if (source != null)
                {
                    source.Cancel();
                }
            }

            if (task != null && !task.IsCompleted)
            {
                try { task.Wait(TimeSpan.FromSeconds(15)); }
                catch (AggregateException) { }
                catch (ObjectDisposedException) { }
            }

            lock (sync)
            {
                if (loopTask == null || loopTask.IsCompleted)
                {
                    loopTask = null;
                    if (cancellation != null)
                    {
                        cancellation.Dispose();
                        cancellation = null;
                    }
                }
            }
        }

        public void RefreshNow(MonitorSettings source)
        {
            if (source == null) { throw new ArgumentNullException("source"); }
            MonitorSettings settings = source.Clone();
            settings.Normalize();
            CancellationToken token;
            lock (sync)
            {
                ThrowIfDisposed();
                token = cancellation == null ? lifetimeCancellation.Token : cancellation.Token;
            }

            Task task = Task.Run(async delegate
            {
                await RefreshWithGateAsync(settings, token, true).ConfigureAwait(false);
            });
            lock (sync)
            {
                refreshTask = task;
            }
            task.ContinueWith(delegate(Task completed)
            {
                lock (sync)
                {
                    if (refreshTask == completed)
                    {
                        refreshTask = null;
                    }
                }
            }, TaskScheduler.Default);
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
            }
        }

        private async Task TcpLoopCoreAsync(MonitorSettings settings, CancellationToken token)
        {
            int currentIndex = 0;
            int goodCount = 0;
            int badStreak = 0;
            int intervalMs = settings.FastIntervalMs;
            DateTime lastRefreshUtc = DateTime.MinValue;
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
                            else if (DateTime.UtcNow - lastRefreshUtc >=
                                     TimeSpan.FromSeconds(settings.CooldownSeconds))
                            {
                                await RefreshWithGateAsync(settings, token, false).ConfigureAwait(false);
                                lastRefreshUtc = DateTime.UtcNow;
                                badStreak = 0;
                                cooldownLogged = false;
                            }
                            else if (!cooldownLogged)
                            {
                                TimeSpan elapsed = DateTime.UtcNow - lastRefreshUtc;
                                double remaining = Math.Max(0, settings.CooldownSeconds - elapsed.TotalSeconds);
                                RaiseLog("偵測到異常，但仍在刷新冷卻時間內，剩餘約 " +
                                         Math.Ceiling(remaining) + " 秒。", true);
                                cooldownLogged = true;
                            }
                        }
                    }

                    RaiseProbe(result, badStreak, intervalMs);
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

        private async Task RefreshWithGateAsync(
            MonitorSettings settings,
            CancellationToken token,
            bool manual)
        {
            if (!manual && settings.FailoverEnabled && settings.SuppressRefreshDuringFailover &&
                Thread.VolatileRead(ref failoverReady) == 1)
            {
                RaiseLog("A/B 監測已就緒，暫停一般自動刷新，避免與故障切換互相干擾。", true);
                return;
            }

            bool entered = false;
            try
            {
                entered = await refreshGate.WaitAsync(0, token).ConfigureAwait(false);
                if (!entered)
                {
                    RaiseLog("已有刷新動作執行中，略過重複請求。", true);
                    return;
                }

                RaiseLog(manual ? "開始手動刷新。" : "達到異常門檻，開始刷新。", false);
                await SoftRefreshAsync(settings, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                RaiseLog("刷新動作已取消。", true);
            }
            catch (Exception ex)
            {
                RaiseLog("刷新動作發生錯誤：" + ex.Message, true);
            }
            finally
            {
                if (entered)
                {
                    refreshGate.Release();
                }
            }
        }

        private async Task SoftRefreshAsync(MonitorSettings settings, CancellationToken token)
        {
            if (!NetworkInfo.IsAdministrator())
            {
                RaiseLog("目前不是系統管理員；ARP 與 MTU 動作可能被 Windows 拒絕。", true);
            }

            if (settings.FlushDns)
            {
                CommandResult result = await CommandRunner.RunAsync(
                    "ipconfig.exe", new[] { "/flushdns" }, 5000, token).ConfigureAwait(false);
                ReportCommand("DNS cache", result);
            }

            if (settings.ClearArp)
            {
                CommandResult result = await CommandRunner.RunAsync(
                    "arp.exe", new[] { "-d", "*" }, 5000, token).ConfigureAwait(false);
                ReportCommand("ARP cache", result);
            }

            if (!settings.PulseMtu)
            {
                RaiseLog("MTU 刷新已停用。", false);
                return;
            }

            int originalMtu = NetworkInfo.TryGetMtu(settings.InterfaceName);
            if (originalMtu <= 0)
            {
                RaiseLog("找不到網卡「" + settings.InterfaceName + "」的 IPv4 MTU，略過 MTU 刷新。", true);
                return;
            }
            if (originalMtu <= settings.MtuPulseValue)
            {
                RaiseLog("目前 MTU 為 " + originalMtu + "，不會把它提高到 " +
                         settings.MtuPulseValue + "；略過 MTU 刷新。", false);
                return;
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
            ReportCommand("MTU pulse -> " + settings.MtuPulseValue, pulse);
            if (!pulse.Succeeded)
            {
                return;
            }

            try
            {
                await Task.Delay(150, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                RaiseLog("MTU pulse 等待被取消，仍會先嘗試復原原始值。", true);
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
            ReportCommand("MTU restore -> " + originalMtu, restore);
        }

        private void ReportCommand(string name, CommandResult result)
        {
            if (result == null)
            {
                RaiseLog(name + "：沒有收到結果。", true);
                return;
            }
            if (result.Succeeded)
            {
                RaiseLog(name + "：成功。", false);
                return;
            }
            if (result.Cancelled)
            {
                RaiseLog(name + "：已取消。", true);
                return;
            }
            if (result.TimedOut)
            {
                RaiseLog(name + "：timeout，程序已終止。", true);
                return;
            }

            string detail = CommandRunner.GetUsefulError(result.StandardError);
            if (detail.Length == 0) { detail = CommandRunner.GetUsefulError(result.StandardOutput); }
            if (detail.Length == 0) { detail = "exit code " + result.ExitCode; }
            RaiseLog(name + "：失敗（" + detail + "）。", true);
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
            lock (sync)
            {
                if (disposed) { return; }
                disposed = true;
                lifetimeCancellation.Cancel();
            }
            Stop();
            Task pendingRefresh;
            lock (sync)
            {
                pendingRefresh = refreshTask;
            }
            if (pendingRefresh != null && !pendingRefresh.IsCompleted)
            {
                try { pendingRefresh.Wait(TimeSpan.FromSeconds(15)); }
                catch (AggregateException) { }
                catch (ObjectDisposedException) { }
            }
            refreshGate.Dispose();
            lifetimeCancellation.Dispose();
        }
    }
}
