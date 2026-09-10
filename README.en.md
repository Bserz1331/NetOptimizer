# NetOptimizer

NetOptimizer is a Windows network monitor with A/B failover and slow EWMA-based smart routing. It combines the practical refresh behavior of the original NetOptimizer with the dual-link failover logic from DoubleNet, while keeping timeouts, rollback, and long-running stability explicit.

The `main` branch currently contains the v3.0.12 development changes, including a switchable Traditional Chinese / English interface. The existing [v3.0.11 release](https://github.com/Bserz1331/NetOptimizer/releases/tag/v3.0.11) remains available while the new build is being verified.

## Features

- Timeout-aware TCP probes with cancellation and source IPv4 binding.
- Consecutive-failure detection with cooldown-controlled DNS, ARP, and MTU refresh actions.
- A/B failover that changes IPv4 interface metrics only after the backup passes health checks.
- Recovery journal and rollback verification after a crash or interrupted metric change.
- Automatic return to A after the primary link remains stable for the configured recovery period.
- Slow EWMA smart routing as an optional policy; it does not replace failure failover.
- Safe interface discovery: automatic selection uses only interfaces with IPv4 and a default gateway (`IsReady`); `NotPresent` interfaces are excluded from the lists.
- Non-administrator A/B startup is blocked with an explicit “Restart as administrator” action.
- Beginner mode for the common workflow and an advanced mode for monitoring, failover, EWMA, and diagnostics settings.
- System-tray operation, bounded activity logs, diagnostic export, and a support dialog.
- Traditional Chinese / English UI selection in the upper-right corner. The preference is stored in `%LOCALAPPDATA%\NetOptimizer\settings.xml` and applies to the main window, tray menu, and support dialog.

## How A/B failover works

The primary interface A is monitored against the configured TCP targets. When A reaches the consecutive-failure threshold, the program checks that B is present, has IPv4 and a gateway, and passes the failover health policy before changing interface metrics. If no second ready interface exists, B remains empty and no route change is attempted.

When both links are healthy, optional EWMA smart routing slowly compares latency, loss, and jitter:

`score = EWMA latency + EWMA loss × timeout + EWMA jitter`

The policy requires a configurable margin, hold time, minimum dwell time, backoff, and hourly switch budget. Keep it disabled when stability is more important than choosing between two already-healthy links.

## Permissions and safety

The program does not elevate itself silently. A/B metric changes, ARP operations, and MTU operations commonly require an elevated process. DNS refresh may work without elevation depending on the Windows environment. The tool records command results, verifies metric reads after changes, and keeps a recovery journal before changing managed metrics.

“Healthy” means that the configured TCP targets satisfy the selected policy. It does not guarantee that every website, game server, VPN, or application route is healthy.

## Build

From Windows PowerShell:

```powershell
.\build.ps1 -Version 3.0.12 -OutputDirectory .\dist
```

The build uses the .NET Framework compiler and Windows SDK resources available on the machine. Code signing is optional; without a certificate the build reports that signing was skipped.

## Tests

```powershell
.\dist\NetOptimizer-v3.0.12.exe --self-test
.\dist\NetOptimizer-v3.0.12.exe --failover-simulation
.\dist\NetOptimizer-v3.0.12.exe --interface-probe-test
.\dist\NetOptimizer-v3.0.12.exe --diagnostics-test
.\dist\NetOptimizer-v3.0.12.exe --interface-metric-test
.\dist\NetOptimizer-v3.0.12.exe --ui-layout-test
.\dist\NetOptimizer-v3.0.12.exe --gui-startup-test
.\dist\NetOptimizer-v3.0.12.exe --support-snapshot=.\build\support.png
.\dist\NetOptimizer-v3.0.12.exe --soak-test --seconds=60
```

`--interface-probe-test` is read-only: it probes using each available interface's source IPv4 and does not change routes, metrics, DNS, or MTU. Real A/B route switching should be tested as an administrator in an environment where a brief connection transition is acceptable.

## Repository layout

- `src\` contains the C# source, icon assets, and the source build script.
- `docs\` contains diagnostic, release, and open-source boundary notes.
- `build\`, `dist\`, and `archive\` are local build/recovery directories and are not part of the public source tree.

## Support

The application includes an optional support dialog with Ko-fi and USDT network choices. It opens only when requested and does not include telemetry or background network reporting.

## License

The public core is released under the [MIT License](LICENSE). Future paid, hosted, or customer-specific modules may remain separate from this repository as described in [OPEN-SOURCE-BOUNDARY.md](docs/OPEN-SOURCE-BOUNDARY.md). No private keys or service secrets are included in the executable or source tree.
