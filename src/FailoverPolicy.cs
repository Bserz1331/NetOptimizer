using System;

namespace NetOptimizerV2
{
    internal sealed class FailoverPolicy
    {
        public FailoverPolicy(string activeInterface, string standbyInterface)
        {
            ActiveInterface = activeInterface;
            StandbyInterface = standbyInterface;
            LastSwitchUtc = DateTime.UtcNow;
            NextSwitchAttemptUtc = DateTime.MinValue;
            LastSwitchReason = string.Empty;
        }

        public string ActiveInterface { get; private set; }
        public string StandbyInterface { get; private set; }
        public bool InFailover { get; private set; }
        public int BadStreak { get; private set; }
        public DateTime RecoverySinceUtc { get; private set; }
        public DateTime CooldownUntilUtc { get; private set; }
        public DateTime NextSwitchAttemptUtc { get; private set; }
        public DateTime LastSwitchUtc { get; private set; }
        public DateTime BetterSinceUtc { get; private set; }
        public int SwitchCount { get; private set; }
        public string LastSwitchReason { get; private set; }

        public void ObserveActive(bool healthy)
        {
            if (healthy)
            {
                BadStreak = 0;
            }
            else
            {
                BadStreak++;
                RecoverySinceUtc = DateTime.MinValue;
            }
        }

        public bool ShouldFailover(DateTime nowUtc, int badSamples)
        {
            return BadStreak >= badSamples && CanAttemptSwitch(nowUtc);
        }

        public bool ObserveRecovery(bool healthy, DateTime nowUtc, int recoverySeconds)
        {
            if (!healthy)
            {
                RecoverySinceUtc = DateTime.MinValue;
                return false;
            }
            if (RecoverySinceUtc == DateTime.MinValue)
            {
                RecoverySinceUtc = nowUtc;
                return false;
            }
            return nowUtc - RecoverySinceUtc >= TimeSpan.FromSeconds(recoverySeconds) &&
                   CanAttemptSwitch(nowUtc);
        }

        public bool ShouldSmartSwitch(
            DateTime nowUtc,
            double activeScore,
            double standbyScore,
            int marginMs,
            int minDwellSeconds,
            int holdSeconds)
        {
            if (double.IsNaN(activeScore) || double.IsInfinity(activeScore) ||
                double.IsNaN(standbyScore) || double.IsInfinity(standbyScore))
            {
                BetterSinceUtc = DateTime.MinValue;
                return false;
            }
            if (nowUtc - LastSwitchUtc < TimeSpan.FromSeconds(minDwellSeconds) ||
                !CanAttemptSwitch(nowUtc) ||
                standbyScore + marginMs >= activeScore)
            {
                BetterSinceUtc = DateTime.MinValue;
                return false;
            }
            if (BetterSinceUtc == DateTime.MinValue)
            {
                BetterSinceUtc = nowUtc;
                return false;
            }
            return nowUtc - BetterSinceUtc >= TimeSpan.FromSeconds(holdSeconds);
        }

        public bool CanAttemptSwitch(DateTime nowUtc)
        {
            return nowUtc >= CooldownUntilUtc && nowUtc >= NextSwitchAttemptUtc;
        }

        public void MarkSwitchFailure(DateTime nowUtc, int backoffSeconds)
        {
            NextSwitchAttemptUtc = nowUtc.AddSeconds(Math.Max(1, backoffSeconds));
            BetterSinceUtc = DateTime.MinValue;
        }

        public void MarkSwitchSuccess(DateTime nowUtc, int cooldownSeconds, bool failoverMode)
        {
            MarkSwitchSuccess(
                nowUtc,
                cooldownSeconds,
                failoverMode,
                failoverMode ? "故障切換" : "智慧選路");
        }

        public void MarkSwitchSuccess(
            DateTime nowUtc,
            int cooldownSeconds,
            bool failoverMode,
            string reason)
        {
            string previousActive = ActiveInterface;
            ActiveInterface = StandbyInterface;
            StandbyInterface = previousActive;
            InFailover = failoverMode;
            BadStreak = 0;
            RecoverySinceUtc = DateTime.MinValue;
            BetterSinceUtc = DateTime.MinValue;
            LastSwitchUtc = nowUtc;
            CooldownUntilUtc = nowUtc.AddSeconds(Math.Max(0, cooldownSeconds));
            NextSwitchAttemptUtc = DateTime.MinValue;
            SwitchCount++;
            LastSwitchReason = reason ?? string.Empty;
        }

        public void ResetFailureBackoff()
        {
            NextSwitchAttemptUtc = DateTime.MinValue;
        }
    }
}
