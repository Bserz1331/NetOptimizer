# NetOptimizer

[Traditional Chinese documentation](README.md)

NetOptimizer is a portable Windows x64 network monitor with A/B failover and optional slow EWMA-based smart routing. It combines practical network refresh actions with dual-link failover, explicit timeouts, rollback checks, and long-running stability safeguards.

## Download

Download the latest stable build from [NetOptimizer v3.0.13](https://github.com/Bserz1331/NetOptimizer/releases/tag/v3.0.13).

For most users, download `NetOptimizer-v3.0.13-win-x64.zip`, extract it, and run `NetOptimizer-v3.0.13.exe`. No installer is required. The standalone EXE and SHA256 checksum file are also available in the release assets.

## Quick start

1. Start the program.
2. Choose `繁體中文` or `English` from the language selector in the upper-right corner.
3. Select the primary network. If a second ready network is available, select it as the backup.
4. Optionally enable `Start with Windows` in the permission bar.
5. Click `Start protection`.

Use `Beginner mode` for the common workflow. Advanced monitoring, A/B, EWMA, and diagnostic settings remain available when needed.

## Features

- Timeout-aware TCP probes with cancellation and source IPv4 binding.
- Consecutive-failure detection with cooldown-controlled DNS, ARP, and MTU refresh actions.
- A/B failover that changes IPv4 interface metrics only after the backup passes health checks.
- Recovery journal and rollback verification after a crash or interrupted metric change.
- Automatic return to A after the primary link remains stable for the configured recovery period.
- Optional slow EWMA smart routing that compares latency, loss, and jitter; it does not replace failure failover.
- Safe interface discovery: automatic selection uses only interfaces with IPv4 and a default gateway (`IsReady`); `NotPresent` interfaces are excluded from the lists.
- Non-administrator A/B startup is blocked with an explicit “Restart as administrator” action.
- Beginner mode for the common workflow and an advanced mode for monitoring, failover, EWMA, and diagnostics settings.
- System-tray operation, bounded activity logs, diagnostic export, and a support dialog.
- Optional `Start with Windows` registration, disabled by default. When the executable is installed under `C:\Program Files` or `C:\Program Files (x86)`, enabling it requests UAC once and creates a highest-privilege Task Scheduler logon task; after sign-in it starts monitoring and minimizes to the system tray. Portable copies outside Program Files use the current user's `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, start minimized, and do not elevate automatically. Do not configure an elevated task for a user-writable folder.
- Traditional Chinese / English UI selection in the upper-right corner. The preference is stored in `%LOCALAPPDATA%\NetOptimizer\settings.xml` and applies to the main window, tray menu, and support dialog.

## How A/B failover works

The primary interface A is monitored against the configured TCP targets. When A reaches the consecutive-failure threshold, the program checks that B is present, has IPv4 and a gateway, and passes the failover health policy before changing interface metrics. If no second ready interface exists, B remains empty and no route change is attempted.

When both links are healthy, optional EWMA smart routing slowly compares latency, loss, and jitter. The policy uses a configurable margin, hold time, minimum dwell time, backoff, and hourly switch budget. It is disabled by default; keep it disabled when stability is more important than choosing between two already-healthy links.

## Permissions and safety

The program does not elevate itself silently. A/B metric changes, ARP operations, and MTU operations commonly require an elevated process. DNS refresh may work without elevation depending on the Windows environment. The tool records command results, verifies metric reads after changes, and keeps a recovery journal before changing managed metrics.

Start with Windows has two deliberate modes. A protected Program Files installation uses the Task Scheduler task `NetOptimizer Elevated Startup` with highest privileges, runs `--startup --auto-start`, and therefore begins monitoring after logon. A portable copy uses the per-user Run key and only starts minimized. The elevated task is created or removed only after an explicit UAC approval, and failed changes are rolled back where possible.

“Healthy” means that the configured TCP targets satisfy the selected policy. It does not guarantee that every website, game server, VPN, or application route is healthy.

## Build

From Windows PowerShell:

```powershell
.\build.ps1 -Version 3.0.13 -OutputDirectory .\dist
```

The build uses the .NET Framework compiler and Windows SDK resources available on the machine. Code signing is optional; without a certificate the build reports that signing was skipped.

## Tests

Maintainers can run the deterministic checks below from the repository root:

<details>
<summary>Show validation commands</summary>

```powershell
.\dist\NetOptimizer-v3.0.13.exe --self-test
.\dist\NetOptimizer-v3.0.13.exe --failover-simulation
.\dist\NetOptimizer-v3.0.13.exe --interface-probe-test
.\dist\NetOptimizer-v3.0.13.exe --diagnostics-test
.\dist\NetOptimizer-v3.0.13.exe --ui-layout-test
.\dist\NetOptimizer-v3.0.13.exe --gui-startup-test
.\dist\NetOptimizer-v3.0.13.exe --soak-test --seconds=60
```

`--interface-probe-test` is read-only: it probes using each available interface's source IPv4 and does not change routes, metrics, DNS, or MTU. The metric test, when used, must run as administrator and is expected to be refused in a standard-user shell. Real A/B route switching should be tested as an administrator in an environment where a brief connection transition is acceptable.

</details>

## More documentation

- [InterfaceMetric permission troubleshooting](docs/INTERFACEMETRIC-ERROR.md)
- [Open-source boundary](docs/OPEN-SOURCE-BOUNDARY.md)
- [Release preparation notes](docs/GITHUB-PUBLISH.md)

## Support

The application includes an optional support dialog with Ko-fi and USDT network choices. It opens only when requested and does not include telemetry or background network reporting.

## License

The public core is released under the [MIT License](LICENSE). Future paid, hosted, or customer-specific modules may remain separate from this repository as described in [OPEN-SOURCE-BOUNDARY.md](docs/OPEN-SOURCE-BOUNDARY.md). No private keys or service secrets are included in the executable or source tree.
