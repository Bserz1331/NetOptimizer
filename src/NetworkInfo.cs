using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NetOptimizerV2
{
    internal sealed class InterfaceMetricState
    {
        public string InterfaceName { get; set; }
        public int Metric { get; set; }
        public bool AutomaticMetric { get; set; }
        public string Error { get; set; }
    }

    internal sealed class InterfaceSnapshot
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public NetworkInterfaceType Type { get; set; }
        public OperationalStatus Status { get; set; }
        public string IPv4 { get; set; }
        public string Gateway { get; set; }

        public bool IsReady
        {
            get
            {
                return Status == OperationalStatus.Up &&
                       !string.IsNullOrWhiteSpace(IPv4) &&
                       !string.IsNullOrWhiteSpace(Gateway);
            }
        }
    }

    internal sealed class DefaultRouteState
    {
        public string InterfaceName { get; set; }
        public int InterfaceIndex { get; set; }
        public int RouteMetric { get; set; }
        public int InterfaceMetric { get; set; }
        public string NextHop { get; set; }
        public string Error { get; set; }
    }

    internal static class NetworkInfo
    {
        public static List<string> GetInterfaceNames()
        {
            List<NetworkInterface> all = new List<NetworkInterface>();
            try
            {
                all.AddRange(NetworkInterface.GetAllNetworkInterfaces());
            }
            catch
            {
                return new List<string>();
            }

            List<string> active = all
                .Where(IsSelectableInterface)
                .OrderByDescending(delegate(NetworkInterface item)
                {
                    return item.OperationalStatus == OperationalStatus.Up;
                })
                .ThenBy(delegate(NetworkInterface item) { return item.Name; }, StringComparer.OrdinalIgnoreCase)
                .Select(delegate(NetworkInterface item) { return item.Name; })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (active.Count > 0)
            {
                return active;
            }

            return all
                .Where(IsSelectableInterface)
                .Select(delegate(NetworkInterface item) { return item.Name; })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(delegate(string name) { return name; }, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool IsAdministrator()
        {
            try
            {
                using (System.Security.Principal.WindowsIdentity identity =
                    System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    System.Security.Principal.WindowsPrincipal principal =
                        new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        public static int TryGetMtu(string interfaceName)
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
            {
                return 0;
            }

            try
            {
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (!string.Equals(item.Name, interfaceName, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(item.Description, interfaceName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    IPv4InterfaceProperties ipv4 = item.GetIPProperties().GetIPv4Properties();
                    return ipv4 == null ? 0 : ipv4.Mtu;
                }
            }
            catch
            {
                return 0;
            }
            return 0;
        }

        public static string TryGetIPv4(string interfaceName)
        {
            InterfaceSnapshot snapshot = GetInterfaceSnapshot(interfaceName);
            return snapshot == null || snapshot.Status != OperationalStatus.Up ? null : snapshot.IPv4;
        }

        public static InterfaceSnapshot GetInterfaceSnapshot(string interfaceName)
        {
            if (string.IsNullOrWhiteSpace(interfaceName)) { return null; }
            try
            {
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (!string.Equals(item.Name, interfaceName, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(item.Description, interfaceName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    IPAddress ipv4 = item.GetIPProperties().UnicastAddresses
                        .Select(delegate(UnicastIPAddressInformation address) { return address.Address; })
                        .Where(IsUsableIPv4)
                        .FirstOrDefault();
                    IPAddress gateway = item.GetIPProperties().GatewayAddresses
                        .Select(delegate(GatewayIPAddressInformation address) { return address.Address; })
                        .Where(IsUsableIPv4)
                        .FirstOrDefault();
                    return new InterfaceSnapshot
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Type = item.NetworkInterfaceType,
                        Status = item.OperationalStatus,
                        IPv4 = ipv4 == null ? null : ipv4.ToString(),
                        Gateway = gateway == null ? null : gateway.ToString()
                    };
                }
            }
            catch { }
            return null;
        }

        public static List<InterfaceSnapshot> GetInterfaceSnapshots()
        {
            List<InterfaceSnapshot> result = new List<InterfaceSnapshot>();
            try
            {
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (!IsUsableInterface(item)) { continue; }
                    IPAddress ipv4 = item.GetIPProperties().UnicastAddresses
                        .Select(delegate(UnicastIPAddressInformation address) { return address.Address; })
                        .Where(IsUsableIPv4)
                        .FirstOrDefault();
                    IPAddress gateway = item.GetIPProperties().GatewayAddresses
                        .Select(delegate(GatewayIPAddressInformation address) { return address.Address; })
                        .Where(IsUsableIPv4)
                        .FirstOrDefault();
                    result.Add(new InterfaceSnapshot
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Type = item.NetworkInterfaceType,
                        Status = item.OperationalStatus,
                        IPv4 = ipv4 == null ? null : ipv4.ToString(),
                        Gateway = gateway == null ? null : gateway.ToString()
                    });
                }
            }
            catch { }
            return result
                .OrderByDescending(delegate(InterfaceSnapshot item) { return item.IsReady; })
                .ThenBy(delegate(InterfaceSnapshot item) { return item.Name; }, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static async Task<InterfaceMetricState> ReadInterfaceMetricAsync(
            string interfaceName,
            CancellationToken token)
        {
            string alias = PowerShellLiteral(interfaceName);
            string script = "$i = Get-NetIPInterface -AddressFamily IPv4 -InterfaceAlias " + alias +
                            " -ErrorAction Stop | Select-Object -First 1; " +
                            "if ($null -eq $i) { throw 'interface not found' }; " +
                            "$metric = [int]$i.InterfaceMetric; " +
                            "$automatic = if ([bool]$i.AutomaticMetric) { '1' } else { '0' }; " +
                            "Write-Output ('NETOPTIMIZER_METRIC|' + " +
                            "$metric.ToString([Globalization.CultureInfo]::InvariantCulture) + '|' + $automatic)";
            CommandResult result = await RunWindowsPowerShellAsync(script, 5000, token).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return new InterfaceMetricState
                {
                    InterfaceName = interfaceName,
                    Error = FirstError(result)
                };
            }

            int metric;
            bool automaticMetric;
            if (!TryParseInterfaceMetricOutput(
                result.StandardOutput,
                out metric,
                out automaticMetric))
            {
                return new InterfaceMetricState
                {
                    InterfaceName = interfaceName,
                    Error = "無法解析 InterfaceMetric。"
                };
            }

            return new InterfaceMetricState
            {
                InterfaceName = interfaceName,
                Metric = metric,
                AutomaticMetric = automaticMetric
            };
        }

        internal static bool TryParseInterfaceMetricOutput(
            string output,
            out int metric,
            out bool automaticMetric)
        {
            metric = 0;
            automaticMetric = false;
            string text = output ?? string.Empty;
            Match match = Regex.Match(
                text,
                @"(?m)^\s*NETOPTIMIZER_METRIC\|(\d+)\|(0|1)\s*$",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                match = Regex.Match(
                    text,
                    @"(?m)^\s*(\d+)\|(True|False|0|1)\s*$",
                    RegexOptions.IgnoreCase);
            }
            if (!match.Success ||
                !int.TryParse(
                    match.Groups[1].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out metric))
            {
                metric = 0;
                automaticMetric = false;
                return false;
            }

            string flag = match.Groups[2].Value;
            automaticMetric = string.Equals(flag, "1", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(flag, "True", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        public static Task<CommandResult> SetInterfaceMetricAsync(
            string interfaceName,
            int metric,
            bool automaticMetric,
            CancellationToken token)
        {
            string alias = PowerShellLiteral(interfaceName);
            string script;
            if (automaticMetric)
            {
                script = "Set-NetIPInterface -AddressFamily IPv4 -InterfaceAlias " + alias +
                         " -AutomaticMetric Enabled -ErrorAction Stop";
            }
            else
            {
                script = "Set-NetIPInterface -AddressFamily IPv4 -InterfaceAlias " + alias +
                         " -AutomaticMetric Disabled -InterfaceMetric " + metric + " -ErrorAction Stop";
            }
            return RunWindowsPowerShellAsync(script, 5000, token);
        }

        public static async Task<DefaultRouteState> ReadPreferredDefaultRouteAsync(
            CancellationToken token)
        {
            string script = "$ifs = @(Get-NetIPInterface -AddressFamily IPv4 -ErrorAction Stop); " +
                     "$routes = @(Get-NetRoute -AddressFamily IPv4 -DestinationPrefix '0.0.0.0/0' -ErrorAction Stop); " +
                     "$rows = foreach ($r in $routes) { " +
                     "$i = $ifs | Where-Object { $_.ifIndex -eq $r.ifIndex } | Select-Object -First 1; " +
                     "if ($null -ne $i) { [pscustomobject]@{ Alias=$i.InterfaceAlias; Index=$r.ifIndex; Route=$r.RouteMetric; IfMetric=$i.InterfaceMetric; Hop=$r.NextHop } } }; " +
                     "$best = $rows | Sort-Object @{Expression={ [int]$_.Route + [int]$_.IfMetric }} | Select-Object -First 1; " +
                     "if ($null -ne $best) { Write-Output ($best.Alias.ToString() + '|' + $best.Index.ToString() + '|' + " +
                     "$best.Route.ToString() + '|' + $best.IfMetric.ToString() + '|' + $best.Hop.ToString()) } else { throw 'default route not found' }";
            CommandResult result = await RunWindowsPowerShellAsync(script, 5000, token).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return new DefaultRouteState { Error = FirstError(result) };
            }
            string[] lines = (result.StandardOutput ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                return new DefaultRouteState { Error = "無法取得預設 route。" };
            }
            string[] parts = lines[lines.Length - 1].Trim().Split('|');
            if (parts.Length < 5)
            {
                return new DefaultRouteState { Error = "無法解析預設 route。" };
            }
            int index;
            int routeMetric;
            int interfaceMetric;
            if (!int.TryParse(parts[1], out index) ||
                !int.TryParse(parts[2], out routeMetric) ||
                !int.TryParse(parts[3], out interfaceMetric))
            {
                return new DefaultRouteState { Error = "預設 route metric 不是有效數字。" };
            }
            return new DefaultRouteState
            {
                InterfaceName = parts[0],
                InterfaceIndex = index,
                RouteMetric = routeMetric,
                InterfaceMetric = interfaceMetric,
                NextHop = parts[4]
            };
        }

        public static async Task<bool> VerifyInterfaceMetricAsync(
            string interfaceName,
            int expectedMetric,
            CancellationToken token)
        {
            InterfaceMetricState state = await ReadInterfaceMetricAsync(interfaceName, token)
                .ConfigureAwait(false);
            return state != null && string.IsNullOrWhiteSpace(state.Error) &&
                   !state.AutomaticMetric && state.Metric == expectedMetric;
        }

        private static async Task<CommandResult> RunWindowsPowerShellAsync(
            string script,
            int timeoutMs,
            CancellationToken token)
        {
            string powershell = System.IO.Path.Combine(
                Environment.SystemDirectory, "WindowsPowerShell\\v1.0\\powershell.exe");
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            return await CommandRunner.RunAsync(
                powershell,
                new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand", encoded },
                timeoutMs,
                token).ConfigureAwait(false);
        }

        private static string PowerShellLiteral(string value)
        {
            return "'" + (value ?? string.Empty).Replace("'", "''") + "'";
        }

        private static string FirstError(CommandResult result)
        {
            if (result == null) { return "沒有收到結果。"; }
            if (result.Cancelled) { return "已取消。"; }
            if (result.TimedOut) { return "timeout。"; }
            string error = CommandRunner.GetUsefulError(result.StandardError);
            if (error.Length > 0) { return error; }
            error = CommandRunner.GetUsefulError(result.StandardOutput);
            if (error.Length > 0) { return error; }
            return "exit code " + result.ExitCode;
        }

        private static bool IsUsableInterface(NetworkInterface item)
        {
            if (item == null || item.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                item.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                item.NetworkInterfaceType == NetworkInterfaceType.Unknown ||
                item.NetworkInterfaceType == NetworkInterfaceType.Ppp)
            {
                return false;
            }
            string identity = ((item.Name ?? string.Empty) + " " +
                               (item.Description ?? string.Empty)).ToLowerInvariant();
            string[] excluded =
            {
                "wi-fi direct", "wfp", "qos packet scheduler", "native wifi filter",
                "virtual wifi filter", "wan miniport", "teredo", "hyper-v",
                "loopback", "vpn"
            };
            return !excluded.Any(delegate(string token) { return identity.Contains(token); });
        }

        private static bool IsSelectableInterface(NetworkInterface item)
        {
            return IsUsableInterface(item) && item.OperationalStatus != OperationalStatus.NotPresent;
        }

        private static bool IsUsableIPv4(IPAddress address)
        {
            if (address == null || address.AddressFamily != AddressFamily.InterNetwork ||
                IPAddress.IsLoopback(address))
            {
                return false;
            }
            byte[] bytes = address.GetAddressBytes();
            return !(bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254);
        }
    }
}
