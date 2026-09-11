# NetOptimizer v3.0.14

## Highlights

- Added a background update check for the latest stable GitHub Release.
- Shows a compact update link in the header and matching system-tray actions when a newer version is available.
- Added manual check, open release page, and ignore-this-version actions without automatic download, execution, or replacement.
- Stores update-check metadata separately in `%LOCALAPPDATA%\NetOptimizer\update-state.xml` and keeps update failures isolated from monitoring.
- Added cancellation, timeout, TLS 1.2 compatibility, trusted-release URL validation, and deterministic update-check tests.
- Updated the Windows resource, manifest, build scripts, documentation, and Actions workflow to the `3.0.14` release version.

## Safety notes

- The application does not silently elevate or self-update.
- Update links are restricted to the official `github.com/Bserz1331/NetOptimizer/releases/` path.
- A failed update check does not stop monitoring, failover, or refresh actions.
- The release binaries are portable and do not require an installer.

## Validation

- Local build completed with the Windows .NET Framework compiler; code signing was skipped because no certificate was supplied.
- Deterministic self-test, failover simulation, interface probe, diagnostics, UI layout, GUI startup, update-check, and 5-second soak checks passed locally.
- GitHub Actions runs the same deterministic checks, including the update-check self-test, for the published source.
