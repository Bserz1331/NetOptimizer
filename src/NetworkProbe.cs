using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetOptimizerV2
{
    internal static class NetworkProbe
    {
        public static async Task<ProbeResult> TcpAsync(
            string sourceIp,
            string target,
            int port,
            int timeoutMs,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return new ProbeResult
                {
                    Target = target,
                    Port = port,
                    State = ProbeState.Failed,
                    Error = "沒有指定目標。"
                };
            }

            TcpClient client = null;
            Task connectTask = null;
            long start = Stopwatch.GetTimestamp();
            try
            {
                if (string.IsNullOrWhiteSpace(sourceIp))
                {
                    client = new TcpClient();
                }
                else
                {
                    IPAddress sourceAddress;
                    if (!IPAddress.TryParse(sourceIp, out sourceAddress) ||
                        sourceAddress.AddressFamily != AddressFamily.InterNetwork)
                    {
                        return new ProbeResult
                        {
                            Target = target,
                            Port = port,
                            State = ProbeState.Failed,
                            Error = "來源 IPv4 無效。"
                        };
                    }
                    client = new TcpClient(new IPEndPoint(sourceAddress, 0));
                }

                connectTask = client.ConnectAsync(target, port);
                using (CancellationTokenSource timeoutCancellation =
                       CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    Task timeoutTask = Task.Delay(timeoutMs, timeoutCancellation.Token);
                    Task completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                    if (completed != connectTask)
                    {
                        ObserveFault(connectTask);
                        try { client.Close(); } catch { }
                        return new ProbeResult
                        {
                            Target = target,
                            Port = port,
                            State = token.IsCancellationRequested ? ProbeState.Cancelled : ProbeState.Timeout,
                            LatencyMs = ElapsedMilliseconds(start),
                            Error = token.IsCancellationRequested ? "已取消" : "連線 timeout"
                        };
                    }

                    timeoutCancellation.Cancel();
                }

                await connectTask.ConfigureAwait(false);
                return new ProbeResult
                {
                    Target = target,
                    Port = port,
                    State = ProbeState.Success,
                    LatencyMs = ElapsedMilliseconds(start)
                };
            }
            catch (OperationCanceledException)
            {
                if (connectTask != null) { ObserveFault(connectTask); }
                if (client != null) { try { client.Close(); } catch { } }
                return new ProbeResult
                {
                    Target = target,
                    Port = port,
                    State = ProbeState.Cancelled,
                    LatencyMs = ElapsedMilliseconds(start),
                    Error = "已取消"
                };
            }
            catch (Exception ex)
            {
                return new ProbeResult
                {
                    Target = target,
                    Port = port,
                    State = ProbeState.Failed,
                    LatencyMs = ElapsedMilliseconds(start),
                    Error = ex.Message
                };
            }
            finally
            {
                if (client != null) { client.Dispose(); }
            }
        }

        private static int ElapsedMilliseconds(long start)
        {
            double elapsed = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            if (elapsed < 0) { return 0; }
            if (elapsed > int.MaxValue) { return int.MaxValue; }
            return (int)Math.Round(elapsed);
        }

        private static void ObserveFault(Task task)
        {
            if (task == null) { return; }
            task.ContinueWith(delegate(Task completed)
            {
                Exception ignored = completed.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
