# NetOptimizer v3.0.17

## Highlights

- Hardened A/B failover with `IsReady` checks before metric reads, initial
  application, and every switch.
- Verified interface metric readback and the selected default route after each
  managed change; unreadable or mismatched state remains not ready.
- Made rollback fail closed when an external process changes an interface
  metric, and recorded a role-specific managed metric in new recovery journals.
- Corrected EWMA treatment of unavailable links, and based cooldown/backoff on
  the actual completion or failure time of a switch.
- Strengthened monitor lifecycle ownership so refresh tasks, cancellation
  sources, and synchronization gates are not disposed while work is still
  running.
- Hardened single-instance mutex handoff, settings persistence, update-state
  persistence, and bounded/cancellable GitHub update checks.

## Validation

- Final v3.0.17 build passed the deterministic self-test, failover simulation,
  diagnostics, interface probe, UI layout, GUI startup, stability, and soak
  checks on the local Windows machine.
- GitHub live update checking passed against the latest stable endpoint.
- Interface metric reads require an elevated process on the test machine; the
  administrator-only metric write and physical A/B route-switch path remain
  manual validation items.

## Upgrade notes

- Existing v3.0.16 installations keep their settings and recovery data.
- The new recovery journal field is optional, so older journals remain
  readable through the compatibility guard.
