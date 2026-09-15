# NetOptimizer UI icons

These SVG files are the standalone design assets for the modern dashboard.
They use a shared 64 x 64 viewBox so they can be previewed or reused by
documentation, packaging, or a future renderer without changing their size.

The desktop build embeds these SVG files into the EXE as managed resources
and renders them through src/UiIconRenderer.cs. The existing code-native
drawing remains as a fallback if an embedded resource is unavailable, so a
portable EXE does not depend on files beside it.

## Network icon mapping

- network-wifi.svg: Wireless80211 or Wi-Fi-like interface names
- network-bluetooth.svg: Bluetooth / 藍牙 interface names or descriptions
- network-ethernet.svg: Ethernet / LAN / 乙太網路 interface types or names
- no selected interface: the neutral network glyph rendered by the application

## Palette

- Accent: #14E0CD
- Network blue: #3CAAFF
- Warning: #FFC72A
- Foreground: #EAF4FA

`network-bluetooth.svg` is an original NetOptimizer geometric redraw. The
PNGTree raster reference is not included in the project or embedded in the
EXE.
