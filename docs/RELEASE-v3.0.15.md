# NetOptimizer v3.0.15

## Highlights

- Added Windows-language-aware first-run defaults: any `zh-*` culture defaults to Traditional Chinese; other cultures default to English.
- Existing settings and manual language choices are preserved.
- Fixed beginner-mode failover safety: if no second ready backup interface (`IsReady`) is detected, automatic backup switching is disabled by default.
- The same guard applies at startup when a previously configured backup is not currently ready; EWMA smart routing is disabled with it.
- Added localized warning/prompt and UI regression coverage for no-ready-backup states.
- Continued background GitHub update checks, system-tray operation, and bounded network actions.

## Safety notes

- A/B metric, ARP, and MTU actions still require administrator permission.
- A/B is not started without a confirmed ready backup.
- The app does not silently elevate or self-update.
- Update checks never download, execute, or replace the EXE.

## Validation

- Local build completed with the .NET Framework compiler; code signing was skipped because no certificate was supplied.
- Deterministic self-test, failover simulation, Wi-Fi interface probe (2/2), diagnostics, UI layout, GUI startup, update-check, and 5-second soak checks passed.
- GitHub Actions workflow includes the update-check self-test.
