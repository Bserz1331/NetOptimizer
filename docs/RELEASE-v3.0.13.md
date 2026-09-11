# NetOptimizer v3.0.13

## Highlights

- Added two-mode Start with Windows behavior:
  - Program Files installations use the `NetOptimizer Elevated Startup` Task Scheduler task with highest privileges and start monitoring after sign-in.
  - Portable copies use the current-user Run key and start minimized without silent elevation.
- Added explicit UAC setup and removal flow with rollback attempts when registration changes fail.
- Added startup-mode and protected-install-path fields to diagnostics.
- Added localized startup messages and a GUI regression check for hidden startup behavior.

## Safety notes

- The application remains `asInvoker`; it does not silently elevate itself.
- An elevated startup task is allowed only when the executable is under `C:\Program Files` or `C:\Program Files (x86)`.
- No elevated task is created for a desktop, download, or other user-writable portable path.
- A/B metric, ARP, and MTU operations still require the normal administrator permission boundary.

## Validation

- Deterministic self-test, failover simulation, interface probe, diagnostics, UI layout, GUI startup, stability, and 5-second soak checks passed locally.
- Portable Start with Windows was toggled in the real WinForms UI; the Run value was created and then removed successfully.
- The local verification shell was a standard user, so a real Program Files/UAC task creation was not performed during packaging.
