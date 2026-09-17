# NetOptimizer

<p align="center">
  <img src="src/assets/NetOptimizer-logo.png" alt="NetOptimizer logo" width="96">
</p>

<p align="center">A Windows network monitor with dual-link failover and optional slow smart routing.</p>

<p align="center">
  <a href="https://github.com/Bserz1331/NetOptimizer/releases/latest">Download v3.0.17</a> ·
  <a href="README.md">繁體中文</a> ·
  <a href="LICENSE">MIT License</a>
</p>

NetOptimizer periodically checks configured TCP targets. When the primary link A becomes unhealthy, it checks that backup link B is ready and healthy before changing the route, then returns to A after A has been stable again. The goal is to reduce manual recovery and repeated route switching during interruptions.

It is not an ISP speed booster. It does not merge two links into one larger connection, and its results still depend on Windows networking, routers, VPNs, drivers, and the quality of each link.

## Download and first start

1. Open the latest version on [GitHub Releases](https://github.com/Bserz1331/NetOptimizer/releases/latest).
2. Download NetOptimizer-v3.0.17-win-x64.zip from Assets.
3. Extract it and run NetOptimizer-v3.0.17.exe; no installer is required.
4. Select the primary link A. If a second ready link exists, select it as backup B.
5. Confirm the settings and click Start protection.

Wi-Fi, Bluetooth Network Connection, and Ethernet can all be used as A or B. The program shows the names and icons reported by Windows. If no second ready link exists, B stays empty and automatic backup switching is disabled by default.

The v3.0.17 EXE is not Authenticode-signed, so Windows may show a source warning. Download only from the official Release and use the SHA256 checksum included on that page when you need to verify the file.

## Reading the main screen

| Area | Meaning |
| --- | --- |
| Primary network A | The link Windows should prefer right now. |
| Backup network B | The second link that can take over when A fails; it should be ready before use. |
| Latest latency / health | The latest probe result; it does not represent every website or service. |
| Automatic failover | Decides whether to switch to B after consecutive failures on A. |
| Automatic repair | Optionally runs DNS, ARP, or MTU refresh actions after an anomaly. |
| Gear icon | Opens the separate Advanced settings window. |
| Activity log | Expands below the main screen so you can inspect actions and errors. |

## How automatic failover works

1. The program probes the configured TCP targets from A's source IPv4 address.
2. When A reaches the consecutive-failure threshold, it rechecks that B has IPv4, a default gateway, and a passing health check.
3. A switch is considered successful only after the InterfaceMetric (Windows interface priority) write succeeds, the value reads back consistently, and the expected default route is observed.
4. Once A recovers and stays stable for the configured period, the program can switch back to A.
5. If B is missing, not IsReady (has IPv4 and a default gateway), unhealthy, or inaccessible because of permissions, the program stops the switch instead of reporting a false success.

The network lists exclude NotPresent (currently unavailable) interfaces. This prevents an unavailable interface from being selected automatically, but it does not guarantee Internet access; health is still determined by the TCP targets you configure.

## Slow EWMA smart routing

This is optional and disabled by default. When A and B are both healthy, the program slowly accumulates latency, loss, and jitter measurements. It considers a switch only when the difference remains beyond the configured margin and hold time.

EWMA does not replace failure protection. Keep it off when stability matters more than choosing between two already-healthy links. Enable it from Advanced settings only when both links are stable and their latency difference is meaningful.

## Permissions and limitations

- Ordinary TCP monitoring usually works as a standard user.
- Changing IPv4 InterfaceMetric (Windows interface priority), refreshing ARP, and changing MTU commonly require administrator rights. The program does not elevate silently; it blocks A/B startup when permission is missing and provides a Restart as admin action.
- A route change can cause a brief connection transition. For the first real test, keep the recovery data and use an environment where a short interruption is acceptable.
- Healthy means that the configured TCP targets satisfy the selected policy. It does not guarantee that every website, game server, VPN, or application will work.
- The program changes Windows interface priority; it does not combine bandwidth for one connection and does not replace a VPN, router, or network driver.

## Language, startup, and updates

- Use the language button in the upper-right corner to switch between Traditional Chinese and English.
- On first start, Windows zh-* cultures default to Traditional Chinese and other cultures default to English. Existing settings and a manual choice are preserved.
- Start with Windows is disabled by default. An installation under Program Files can create a highest-privilege scheduled task after one UAC approval; a portable copy uses the current user's startup entry and does not elevate automatically.
- The program checks the latest stable GitHub Release in the background. Successful checks are spaced 24 hours apart. It only shows an update notice; it never downloads, executes, or replaces the EXE automatically.
- Minimizing keeps the program in the system tray; double-click the tray icon to restore the window.

## Common questions

### Why is backup B empty?

Make sure the second network is connected and has IPv4 plus a default gateway. Click Detect networks; only IsReady interfaces (with IPv4 and a default gateway) appear in the available choices. When there is no second ready interface, the program keeps B empty and disables automatic backup switching.

### Why is A/B startup blocked?

Click Restart as admin, then select A and B again. Ordinary monitoring does not always require elevation, but changing interface metrics, ARP, or MTU usually does.

### Why does it say that the program is already running?

The program intentionally uses one process instance. A second launch asks the existing instance to show its window instead of creating another monitor. If the window does not appear, check the system tray or Task Manager for NetOptimizer.

### What should I do about an InterfaceMetric error?

First confirm that NetOptimizer is running as administrator, then read [InterfaceMetric troubleshooting](docs/INTERFACEMETRIC-ERROR.md). If the cause is still unclear, export diagnostics from the app. Diagnostics may contain interface names, IPv4 addresses, gateways, metrics, and the default route, so redact anything you do not want to share.

## Privacy and local data

- There is no telemetry or background reporting.
- Update checks connect only to the GitHub Release API; a failed check does not stop monitoring.
- Settings and update-check state are stored under %LOCALAPPDATA%\NetOptimizer\.
- Diagnostics are created only when you request an export and may contain local network information.
- The support dialog opens only when you click the support button; it does not make automatic payments or send background reports.

## Support development

The app's Support button opens Ko-fi and USDT support options. You can also visit [ko-fi.com/minz_space_cat](https://ko-fi.com/minz_space_cat) directly. Before sending cryptocurrency, verify the network and test with a small amount first.

## For developers

<details>
<summary>Build, test, and project documentation</summary>

### Build

Run this from Windows PowerShell:

~~~powershell
.\build.ps1 -Version 3.0.17 -OutputDirectory .\dist
~~~

The project builds with the Windows x64 .NET Framework C# compiler and Windows SDK resources. Code signing is optional; without a certificate, the build explicitly reports that signing was skipped.

### Basic validation

~~~powershell
.\dist\NetOptimizer-v3.0.17.exe --self-test
.\dist\NetOptimizer-v3.0.17.exe --failover-simulation
.\dist\NetOptimizer-v3.0.17.exe --interface-probe-test
.\dist\NetOptimizer-v3.0.17.exe --interface-metric-test
.\dist\NetOptimizer-v3.0.17.exe --ui-layout-test
.\dist\NetOptimizer-v3.0.17.exe --gui-startup-test
.\dist\NetOptimizer-v3.0.17.exe --update-check-test
.\dist\NetOptimizer-v3.0.17.exe --soak-test --seconds=60
~~~

interface-probe-test is read-only and does not modify routes, metrics, DNS, or MTU. interface-metric-test only reads metrics and commonly requires administrator rights. Real A/B route switching should still be tested manually in an environment where a brief connection change is acceptable.

### Documentation

- [Advanced source and feature notes](src/README.md)
- [InterfaceMetric troubleshooting](docs/INTERFACEMETRIC-ERROR.md)
- [Open-source boundary](docs/OPEN-SOURCE-BOUNDARY.md)
- [Release v3.0.17 notes](docs/RELEASE-v3.0.17.md)
- [GitHub release process](docs/GITHUB-PUBLISH.md)

</details>

## License

The public core is released under the [MIT License](LICENSE). Future paid, hosted, or customer-specific integrations may remain separate from the public core as described in [Open-source boundary](docs/OPEN-SOURCE-BOUNDARY.md).
