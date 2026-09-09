using System;
using System.Collections.Generic;

namespace NetOptimizerV2
{
    internal static class FailoverSimulation
    {
        public static void Run()
        {
            DateTime origin = DateTime.UtcNow;
            FailoverPolicy policy = new FailoverPolicy("Wi-Fi", "藍牙網路連線");

            policy.ObserveActive(false);
            policy.ObserveActive(false);
            Assert(policy.ShouldFailover(origin.AddSeconds(1), 2), "SIM_1_FAILOVER_TRIGGER");
            policy.MarkSwitchSuccess(origin.AddSeconds(1), 5, true);
            Assert(policy.InFailover, "SIM_2_FAILOVER_STATE");
            Assert(policy.ActiveInterface == "藍牙網路連線", "SIM_3_FAILOVER_ACTIVE");

            Assert(!policy.ObserveRecovery(true, origin.AddSeconds(2), 5), "SIM_4_RECOVERY_PREMATURE");
            Assert(policy.ObserveRecovery(true, origin.AddSeconds(8), 5), "SIM_5_RECOVERY_HOLD");
            policy.MarkSwitchSuccess(origin.AddSeconds(8), 5, false);
            Assert(!policy.InFailover && policy.ActiveInterface == "Wi-Fi", "SIM_6_RECOVERY_STATE");

            policy.MarkSwitchFailure(origin.AddSeconds(9), 10);
            Assert(!policy.CanAttemptSwitch(origin.AddSeconds(10)), "SIM_7_BACKOFF_ACTIVE");
            Assert(policy.CanAttemptSwitch(origin.AddSeconds(20)), "SIM_8_BACKOFF_EXPIRED");

            FailoverPolicy smart = new FailoverPolicy("Wi-Fi", "藍牙網路連線");
            DateTime firstDecision = origin.AddSeconds(31);
            Assert(!smart.ShouldSmartSwitch(firstDecision, 100, 70, 8, 30, 10),
                   "SIM_9_SMART_HOLD_START");
            Assert(smart.ShouldSmartSwitch(firstDecision.AddSeconds(10), 100, 70, 8, 30, 10),
                   "SIM_10_SMART_HOLD_DONE");

            List<string> results = new List<string>
            {
                "故障切換：A failure -> B：PASS",
                "恢復切回：A stable -> A：PASS",
                "切換退避：failed switch backoff：PASS",
                "智慧選路：EWMA score hold：PASS"
            };
            foreach (string result in results)
            {
                Console.WriteLine(result);
            }
            Console.WriteLine("NetOptimizer failover simulation: PASS");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) { throw new InvalidOperationException(message); }
        }
    }
}
