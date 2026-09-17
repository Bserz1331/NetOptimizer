using System;
using System.Collections.Generic;

namespace NetOptimizerV2
{
    internal static class FailoverSimulation
    {
        public static void Run()
        {
            DateTime origin = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            FailoverPolicy policy = new FailoverPolicy("Wi-Fi", "藍牙網路連線", origin);

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

            FailoverPolicy smart = new FailoverPolicy("Wi-Fi", "藍牙網路連線", origin);
            DateTime firstDecision = origin.AddSeconds(31);
            Assert(!smart.ShouldSmartSwitch(firstDecision, 100, 70, 8, 30, 10),
                   "SIM_9_SMART_HOLD_START");
            Assert(smart.ShouldSmartSwitch(firstDecision.AddSeconds(10), 100, 70, 8, 30, 10),
                   "SIM_10_SMART_HOLD_DONE");

            FailoverPolicy timing = new FailoverPolicy("A", "B", origin);
            DateTime completedSwitch = origin.AddSeconds(100);
            timing.MarkSwitchSuccess(completedSwitch, 5, true);
            Assert(timing.LastSwitchUtc == completedSwitch &&
                   !timing.CanAttemptSwitch(completedSwitch.AddSeconds(4)) &&
                   timing.CanAttemptSwitch(completedSwitch.AddSeconds(5)),
                   "SIM_11_COOLDOWN_STARTS_AT_COMPLETION");

            InterfaceMetricState original = new InterfaceMetricState
            {
                InterfaceName = "A",
                Metric = 25,
                AutomaticMetric = false
            };
            InterfaceMetricState originalAutomatic = new InterfaceMetricState
            {
                InterfaceName = "B",
                Metric = 25,
                AutomaticMetric = true
            };
            InterfaceMetricState managed = new InterfaceMetricState
            {
                InterfaceName = "A",
                Metric = 5,
                AutomaticMetric = false
            };
            InterfaceMetricState external = new InterfaceMetricState
            {
                InterfaceName = "A",
                Metric = 77,
                AutomaticMetric = false
            };
            InterfaceMetricState wrongRoleManaged = new InterfaceMetricState
            {
                InterfaceName = "A",
                Metric = 50,
                AutomaticMetric = false
            };
            InterfaceMetricState automaticCurrent = new InterfaceMetricState
            {
                InterfaceName = "B",
                Metric = 99,
                AutomaticMetric = true
            };
            Assert(FailoverManager.CanRestoreOriginalState(original, original, 5, 50) &&
                   FailoverManager.CanRestoreOriginalState(managed, original, 5) &&
                   FailoverManager.CanRestoreOriginalState(managed, original, 5, 50) &&
                   FailoverManager.CanRestoreOriginalState(
                       automaticCurrent, originalAutomatic, 5, 50) &&
                   !FailoverManager.CanRestoreOriginalState(external, original, 5, 50) &&
                   !FailoverManager.CanRestoreOriginalState(wrongRoleManaged, original, 5),
                   "SIM_12_RESTORE_GUARDS_EXTERNAL_METRIC");

            FailoverManager.EwmaLinkStats unavailableStats =
                new FailoverManager.EwmaLinkStats();
            unavailableStats.Add(
                FailoverManager.LinkHealth.Unhealthy("B", "介面未就緒"),
                0.15,
                700);
            Assert(unavailableStats.HasSample && unavailableStats.Score(700) >= 1400,
                   "SIM_13_UNREADY_EWMA_IS_FULL_LOSS");

            InterfaceSnapshot readyA = new InterfaceSnapshot
            {
                Name = "A",
                Status = System.Net.NetworkInformation.OperationalStatus.Up,
                IPv4 = "192.0.2.10",
                Gateway = "192.0.2.1"
            };
            InterfaceSnapshot missingGateway = new InterfaceSnapshot
            {
                Name = "B",
                Status = System.Net.NetworkInformation.OperationalStatus.Up,
                IPv4 = "198.51.100.10"
            };
            Assert(!FailoverManager.IsReadyPair(readyA, missingGateway),
                   "SIM_14_NO_READY_BACKUP_FAIL_CLOSED");

            DefaultRouteState route = new DefaultRouteState
            {
                InterfaceName = " A ",
                InterfaceMetric = 5,
                RouteMetric = 10
            };
            Assert(FailoverManager.IsExpectedRoute(route, "A", 5) &&
                   !FailoverManager.IsExpectedRoute(route, "A", 50),
                   "SIM_15_ROUTE_METRIC_MUST_MATCH");

            List<string> results = new List<string>
            {
                "故障切換：A failure -> B：PASS",
                "恢復切回：A stable -> A：PASS",
                "切換退避：failed switch backoff：PASS",
                "智慧選路：EWMA score hold：PASS",
                "冷卻時序：cooldown starts after completed switch：PASS",
                "復原保護：external metric is not overwritten：PASS",
                "EWMA：unready link counts as full loss：PASS",
                "就緒防護：missing backup gateway fails closed：PASS",
                "路由驗證：selected interface metric must match：PASS"
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
