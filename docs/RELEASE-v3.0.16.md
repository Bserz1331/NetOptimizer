# NetOptimizer v3.0.16

## Highlights

- Reworked the quick-start surface into a compact, content-sized dashboard so
  the primary action, network cards, and fixed footer remain visible without a
  large unused lower area.
- Reduced the fixed header row to 56 px with a 2 px bottom margin, matching
  the 34 + 20 px status cluster and removing the remaining vertical gap before
  the quick-start panel.
- Aligned the A/B cards, interface selectors, status pills, signal indicators,
  and action buttons into predictable rows; the backup card is visually
  disabled when no ready backup path exists.
- Moved the full monitoring, repair, A/B, and EWMA controls into a dedicated
  advanced-settings window opened by the gear icon.  Saving commits the
  edited values; Cancel or close restores the previous values.
- Kept execution records in the home screen: the log button expands a fourth
  row below the quick-start actions and temporarily grows the main window,
  then restores the previous height when collapsed.
- Added DPI-safe responsive sizing and explicit status colors: green for
  healthy, yellow for standby or pending, red for a failed probe, and gray for
  untested or unavailable states.
- Corrected the Bluetooth network glyph to use a symmetric standard Bluetooth
  mark instead of the malformed joined path from v3.0.15.
- Refined the Wi-Fi glyph with compact, evenly spaced nested arcs and a closer
  center dot for clearer small-size rendering.
- Made the header advanced-settings and language controls icon-only while
  keeping tooltips, keyboard focus, and accessible names for discoverability.
- Replaced the settings gear with a symmetric eight-tooth outline whose center
  circle is explicitly aligned to the SVG viewport center.
- Matched the language selector's closed surface to the settings button,
  including its rounded border, colors, focus treatment, and centered globe.
- Kept Bluetooth-first classification for Windows Bluetooth PAN adapters,
  which may report `NetworkInterfaceType.Ethernet` while their name or
  description identifies Bluetooth.
- Kept the embedded SVG resource path and the code-native fallback so the
  portable EXE remains self-contained.
- Fixed the administrator-restart race: the elevated child now carries an
  explicit handoff flag, waits up to 30 seconds for the original instance to
  release the single-instance mutex, and reports a distinct timeout instead
  of incorrectly claiming that NetOptimizer is already running.
- Added fail-closed A/B readiness checks before metric reads, immediately
  before initial metric application, and immediately before every switch;
  only an `IsReady` candidate can become the new primary.
- Corrected initial-setup rollback to restore the captured original metric
  state instead of reusing the role-oriented switch rollback.
- Route verification failure now keeps A/B in a not-ready state instead of
  treating an unreadable default route as success.
- Automatic refresh now preserves the failure count when refresh is skipped,
  cancelled, or partially fails; the count is cleared only after a successful
  refresh operation.
- Synchronized the numeric Windows file-resource version with the assembly
  version and made the build script generate the resource version from
  `-Version`.

## Validation

- UI layout, GUI startup, deterministic self-test, stability, failover
  simulation, diagnostics, and interface probe checks passed on the local
  Windows machine.
- Interface metric reads were attempted but remained unavailable in the
  non-administrator test shell; the administrator-only metric path is not
  claimed as fully validated here.
- Traditional Chinese Wi-Fi, Bluetooth, Ethernet, and English UI snapshots
  were generated from the patched EXE for visual inspection.
- The UI layout test opened and closed the advanced-settings window, verified
  the reparented settings groups, and exercised inline log expansion and
  collapse.
- The snapshot path supports `--snapshot-network=bluetooth` for deterministic
  visual regression of the Bluetooth glyph without changing user settings.
- The single-instance self-test covers immediate duplicate rejection and
  successful mutex handoff after the original owner releases it.
- The GitHub Release is published at https://github.com/Bserz1331/NetOptimizer/releases/tag/v3.0.16 with the runtime ZIP, standalone EXE, PDB, source ZIP, and SHA-256 checksum file.
