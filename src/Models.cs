using System;
using System.Collections.Generic;
using System.Linq;

namespace NetOptimizerV2
{
    public sealed class MonitorSettings
    {
        public string InterfaceName { get; set; }
        public List<string> Targets { get; set; }
        public int Port { get; set; }
        public int ThresholdMs { get; set; }
        public int ProbeTimeoutMs { get; set; }
        public int ConsecutiveFailuresBeforeRefresh { get; set; }
        public int CooldownSeconds { get; set; }
        public int HealthySamplesBeforeSlow { get; set; }
        public int FastIntervalMs { get; set; }
        public int SlowIntervalMs { get; set; }
        public bool EnableRefresh { get; set; }
        public bool FlushDns { get; set; }
        public bool ClearArp { get; set; }
        public bool PulseMtu { get; set; }
        public int MtuPulseValue { get; set; }
        public bool FailoverEnabled { get; set; }
        public string PrimaryInterface { get; set; }
        public string BackupInterface { get; set; }
        public string FailoverTarget { get; set; }
        public List<string> FailoverTargets { get; set; }
        public int FailoverThresholdMs { get; set; }
        public int FailoverPingTimeoutMs { get; set; }
        public int FailoverBadSamples { get; set; }
        public int FailoverRecoverySeconds { get; set; }
        public int FailoverSwitchCooldownSeconds { get; set; }
        public int FailoverSwitchBackoffSeconds { get; set; }
        public int FailoverProbeIntervalMs { get; set; }
        public int FailoverPrimaryMetric { get; set; }
        public int FailoverBackupMetric { get; set; }
        public bool SuppressRefreshDuringFailover { get; set; }
        public bool SmartSelectionEnabled { get; set; }
        public int SmartDecisionIntervalSeconds { get; set; }
        public int SmartMinDwellSeconds { get; set; }
        public int SmartSwitchHoldSeconds { get; set; }
        public int SmartMarginMs { get; set; }
        public int SmartProbeIntervalMs { get; set; }
        public int SmartMaxSwitchesPerHour { get; set; }
        public double SmartEwmaAlpha { get; set; }
        public AppLanguage Language { get; set; }
        public bool? BeginnerMode { get; set; }

        public static MonitorSettings CreateDefault()
        {
            return new MonitorSettings
            {
                InterfaceName = "Wi-Fi",
                Targets = new List<string> { "1.1.1.1", "8.8.8.8" },
                Port = 443,
                ThresholdMs = 90,
                ProbeTimeoutMs = 1500,
                ConsecutiveFailuresBeforeRefresh = 3,
                CooldownSeconds = 60,
                HealthySamplesBeforeSlow = 5,
                FastIntervalMs = 500,
                SlowIntervalMs = 1000,
                EnableRefresh = true,
                FlushDns = true,
                ClearArp = true,
                PulseMtu = true,
                MtuPulseValue = 1471,
                FailoverEnabled = false,
                PrimaryInterface = "Wi-Fi",
                BackupInterface = string.Empty,
                FailoverTarget = "1.1.1.1",
                FailoverTargets = new List<string> { "1.1.1.1", "8.8.8.8" },
                FailoverThresholdMs = 200,
                FailoverPingTimeoutMs = 700,
                FailoverBadSamples = 2,
                FailoverRecoverySeconds = 5,
                FailoverSwitchCooldownSeconds = 5,
                FailoverSwitchBackoffSeconds = 10,
                FailoverProbeIntervalMs = 500,
                FailoverPrimaryMetric = 5,
                FailoverBackupMetric = 50,
                SuppressRefreshDuringFailover = true,
                SmartSelectionEnabled = false,
                SmartDecisionIntervalSeconds = 30,
                SmartMinDwellSeconds = 30,
                SmartSwitchHoldSeconds = 10,
                SmartMarginMs = 8,
                SmartProbeIntervalMs = 1000,
                SmartMaxSwitchesPerHour = 6,
                SmartEwmaAlpha = 0.15,
                Language = AppLanguage.TraditionalChinese,
                BeginnerMode = true
            };
        }

        public MonitorSettings Clone()
        {
            MonitorSettings copy = new MonitorSettings
            {
                InterfaceName = InterfaceName,
                Port = Port,
                ThresholdMs = ThresholdMs,
                ProbeTimeoutMs = ProbeTimeoutMs,
                ConsecutiveFailuresBeforeRefresh = ConsecutiveFailuresBeforeRefresh,
                CooldownSeconds = CooldownSeconds,
                HealthySamplesBeforeSlow = HealthySamplesBeforeSlow,
                FastIntervalMs = FastIntervalMs,
                SlowIntervalMs = SlowIntervalMs,
                EnableRefresh = EnableRefresh,
                FlushDns = FlushDns,
                ClearArp = ClearArp,
                PulseMtu = PulseMtu,
                MtuPulseValue = MtuPulseValue,
                FailoverEnabled = FailoverEnabled,
                PrimaryInterface = PrimaryInterface,
                BackupInterface = BackupInterface,
                FailoverTarget = FailoverTarget,
                FailoverTargets = new List<string>(),
                FailoverThresholdMs = FailoverThresholdMs,
                FailoverPingTimeoutMs = FailoverPingTimeoutMs,
                FailoverBadSamples = FailoverBadSamples,
                FailoverRecoverySeconds = FailoverRecoverySeconds,
                FailoverSwitchCooldownSeconds = FailoverSwitchCooldownSeconds,
                FailoverSwitchBackoffSeconds = FailoverSwitchBackoffSeconds,
                FailoverProbeIntervalMs = FailoverProbeIntervalMs,
                FailoverPrimaryMetric = FailoverPrimaryMetric,
                FailoverBackupMetric = FailoverBackupMetric,
                SuppressRefreshDuringFailover = SuppressRefreshDuringFailover,
                SmartSelectionEnabled = SmartSelectionEnabled,
                SmartDecisionIntervalSeconds = SmartDecisionIntervalSeconds,
                SmartMinDwellSeconds = SmartMinDwellSeconds,
                SmartSwitchHoldSeconds = SmartSwitchHoldSeconds,
                SmartMarginMs = SmartMarginMs,
                SmartProbeIntervalMs = SmartProbeIntervalMs,
                SmartMaxSwitchesPerHour = SmartMaxSwitchesPerHour,
                SmartEwmaAlpha = SmartEwmaAlpha,
                Language = Language,
                BeginnerMode = BeginnerMode,
                Targets = new List<string>()
            };

            if (Targets != null)
            {
                copy.Targets.AddRange(Targets);
            }
            if (FailoverTargets != null)
            {
                copy.FailoverTargets.AddRange(FailoverTargets);
            }
            return copy;
        }

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(InterfaceName))
            {
                InterfaceName = "Wi-Fi";
            }
            if (string.IsNullOrWhiteSpace(PrimaryInterface))
            {
                PrimaryInterface = InterfaceName;
            }
            if (FailoverTarget == null)
            {
                FailoverTarget = string.Empty;
            }
            if (FailoverTargets == null)
            {
                FailoverTargets = new List<string>();
            }
            if (!BeginnerMode.HasValue)
            {
                BeginnerMode = true;
            }
            Language = Localization.Normalize(Language);
            if (Targets == null)
            {
                Targets = new List<string>();
            }
            for (int i = Targets.Count - 1; i >= 0; i--)
            {
                Targets[i] = (Targets[i] ?? string.Empty).Trim();
                if (Targets[i].Length == 0)
                {
                    Targets.RemoveAt(i);
                }
            }
            if (Targets.Count == 0)
            {
                Targets.Add("1.1.1.1");
                Targets.Add("8.8.8.8");
            }
            if (string.IsNullOrWhiteSpace(FailoverTarget))
            {
                FailoverTarget = Targets[0];
            }
            for (int i = FailoverTargets.Count - 1; i >= 0; i--)
            {
                FailoverTargets[i] = (FailoverTargets[i] ?? string.Empty).Trim();
                if (FailoverTargets[i].Length == 0)
                {
                    FailoverTargets.RemoveAt(i);
                }
            }
            if (FailoverTargets.Count == 0 && !string.IsNullOrWhiteSpace(FailoverTarget))
            {
                FailoverTargets.Add(FailoverTarget.Trim());
            }
            if (FailoverTargets.Count == 0)
            {
                FailoverTargets.Add(Targets[0]);
            }
            if (!FailoverTargets.Any(delegate(string target)
            {
                return string.Equals(target, FailoverTarget, StringComparison.OrdinalIgnoreCase);
            }))
            {
                FailoverTargets.Insert(0, FailoverTarget.Trim());
            }
            FailoverTarget = FailoverTargets[0];
            Port = Clamp(Port, 1, 65535, 443);
            ThresholdMs = Clamp(ThresholdMs, 1, 60000, 90);
            ProbeTimeoutMs = Clamp(ProbeTimeoutMs, 100, 60000, 1500);
            ConsecutiveFailuresBeforeRefresh = Clamp(ConsecutiveFailuresBeforeRefresh, 1, 20, 3);
            CooldownSeconds = Clamp(CooldownSeconds, 0, 86400, 60);
            HealthySamplesBeforeSlow = Clamp(HealthySamplesBeforeSlow, 1, 100, 5);
            FastIntervalMs = Clamp(FastIntervalMs, 100, 60000, 500);
            int slowFallback = Math.Max(FastIntervalMs, 1000);
            SlowIntervalMs = Clamp(SlowIntervalMs, FastIntervalMs, 60000, slowFallback);
            MtuPulseValue = Clamp(MtuPulseValue, 576, 9000, 1471);
            FailoverThresholdMs = Clamp(FailoverThresholdMs, 1, 60000, 200);
            FailoverPingTimeoutMs = Clamp(FailoverPingTimeoutMs, 100, 60000, 700);
            FailoverBadSamples = Clamp(FailoverBadSamples, 1, 20, 2);
            FailoverRecoverySeconds = Clamp(FailoverRecoverySeconds, 1, 3600, 5);
            FailoverSwitchCooldownSeconds = Clamp(FailoverSwitchCooldownSeconds, 0, 3600, 5);
            FailoverSwitchBackoffSeconds = Clamp(FailoverSwitchBackoffSeconds, 1, 3600, 10);
            FailoverProbeIntervalMs = Clamp(FailoverProbeIntervalMs, 250, 60000, 500);
            FailoverPrimaryMetric = Clamp(FailoverPrimaryMetric, 1, 9999, 5);
            FailoverBackupMetric = Clamp(FailoverBackupMetric, 1, 9999, 50);
            SmartDecisionIntervalSeconds = Clamp(SmartDecisionIntervalSeconds, 5, 3600, 30);
            SmartMinDwellSeconds = Clamp(SmartMinDwellSeconds, 5, 86400, 30);
            SmartSwitchHoldSeconds = Clamp(SmartSwitchHoldSeconds, 1, 3600, 10);
            SmartMarginMs = Clamp(SmartMarginMs, 1, 60000, 8);
            SmartProbeIntervalMs = Clamp(SmartProbeIntervalMs, 500, 60000, 1000);
            SmartMaxSwitchesPerHour = Clamp(SmartMaxSwitchesPerHour, 1, 60, 6);
            if (double.IsNaN(SmartEwmaAlpha) || double.IsInfinity(SmartEwmaAlpha) ||
                SmartEwmaAlpha < 0.05 || SmartEwmaAlpha > 0.95)
            {
                SmartEwmaAlpha = 0.15;
            }
        }

        private static int Clamp(int value, int min, int max, int fallback)
        {
            if (value < min || value > max)
            {
                return fallback;
            }
            return value;
        }
    }

    public enum ProbeState
    {
        Success,
        Timeout,
        Failed,
        Cancelled
    }

    public sealed class ProbeResult
    {
        public string Target { get; set; }
        public int Port { get; set; }
        public ProbeState State { get; set; }
        public int LatencyMs { get; set; }
        public string Error { get; set; }

        public bool IsHealthy(int thresholdMs)
        {
            return State == ProbeState.Success && LatencyMs <= thresholdMs;
        }
    }

    public sealed class EngineLogEventArgs : EventArgs
    {
        public EngineLogEventArgs(string message, bool warning)
        {
            Message = message;
            Warning = warning;
        }

        public string Message { get; private set; }
        public bool Warning { get; private set; }
    }

    public sealed class ProbeEventArgs : EventArgs
    {
        public ProbeEventArgs(ProbeResult result, int badStreak, int intervalMs)
        {
            Result = result;
            BadStreak = badStreak;
            IntervalMs = intervalMs;
        }

        public ProbeResult Result { get; private set; }
        public int BadStreak { get; private set; }
        public int IntervalMs { get; private set; }
    }

    public sealed class CommandResult
    {
        public string FileName { get; set; }
        public int ExitCode { get; set; }
        public bool TimedOut { get; set; }
        public bool Cancelled { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }

        public bool Succeeded
        {
            get { return !TimedOut && !Cancelled && ExitCode == 0; }
        }
    }

    public sealed class FailoverStatus
    {
        public bool Ready { get; set; }
        public bool InFailover { get; set; }
        public bool SmartSelection { get; set; }
        public string ActiveInterface { get; set; }
        public string StandbyInterface { get; set; }
        public string ActiveHealth { get; set; }
        public string StandbyHealth { get; set; }
        public string Detail { get; set; }
        public string LastSwitchReason { get; set; }
        public DateTime LastSwitchUtc { get; set; }
        public int SwitchCount { get; set; }

        public FailoverStatus Clone()
        {
            return new FailoverStatus
            {
                Ready = Ready,
                InFailover = InFailover,
                SmartSelection = SmartSelection,
                ActiveInterface = ActiveInterface,
                StandbyInterface = StandbyInterface,
                ActiveHealth = ActiveHealth,
                StandbyHealth = StandbyHealth,
                Detail = Detail,
                LastSwitchReason = LastSwitchReason,
                LastSwitchUtc = LastSwitchUtc,
                SwitchCount = SwitchCount
            };
        }
    }

    public sealed class FailoverStatusEventArgs : EventArgs
    {
        public FailoverStatusEventArgs(FailoverStatus status)
        {
            Status = status == null ? new FailoverStatus() : status.Clone();
        }

        public FailoverStatus Status { get; private set; }
    }
}
