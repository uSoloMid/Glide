# Glide

Smooth, cursor-centered zoom for any Windows PC — the fluid Precision-Touchpad /
macOS zoom feel, with a regular mouse. GPU-accelerated, low latency, and fully
independent of Windows Magnifier.

Hold **Ctrl** (configurable) and scroll: the desktop glides toward your cursor.
Release the key and it eases back to 100%.

## How it works

- **Capture** — DXGI Desktop Duplication grabs the desktop of the zoomed monitor
  directly on the GPU (no CPU copies).
- **Render** — a Direct3D 11 fullscreen pass samples the captured frame with a
  cursor-anchored source rectangle into a borderless, topmost, click-through
  overlay window. The overlay is excluded from capture
  (`WDA_EXCLUDEFROMCAPTURE`), so there is no hall-of-mirrors feedback.
- **Cursor math** — the pixel under the cursor is always the *real* desktop
  pixel at that position, so clicks land exactly where you point, at any zoom.
- **Input** — low-level keyboard/mouse hooks on a dedicated high-priority
  thread detect modifier + wheel, swallow the event, and drive the zoom
  animator (exponential smoothing for the touchpad feel, easing curves for the
  return animation).

## Features

- Cursor-centered continuous zoom, 100% → 1000%
- Temporary mode (return on release) or persistent mode
- 9 easing curves, configurable animation duration and zoom speed
- Modifier: Ctrl / Alt / Shift / Win / mouse side buttons
- Pan while zoomed with the middle mouse button (content sticks to the cursor)
- Double-tap the modifier to reset
- Multi-monitor: zoom follows the cursor across displays, or zoom all at once
- Per-application exclusion list (games, Photoshop, …)
- Per-Monitor V2 DPI aware — sharp at 100–300% scaling
- Tray app, silent startup with Windows, dark Windows 11-style settings UI
- VSync or capped-FPS rendering

## Build

Requires the .NET 10 SDK on Windows 10 2004+ (build 19041).

```powershell
dotnet build Glide.slnx -c Release
dotnet test  Glide.slnx -c Release   # 46 unit tests
```

Run `src\Glide.UI\bin\Release\net10.0-windows\Glide.exe`. First launch opens
the settings window; `--silent` starts straight to the tray.

## Project layout

| Project | Responsibility |
|---|---|
| `Glide.Common` | Easing curves, primitives, logging |
| `Glide.Settings` | Settings model, validation, JSON persistence (`%APPDATA%\Glide`) |
| `Glide.Input` | Low-level keyboard/mouse hooks, modifier tracking |
| `Glide.Graphics` | D3D11 device, desktop duplication, overlay window, zoom shader |
| `Glide.Engine` | Zoom animator, viewport math, monitors, render sessions |
| `Glide.UI` | WPF settings window, tray icon, startup registration |
| `Glide.Tests` | Unit tests for the pure logic (easing, viewport, animator, settings, exclusions) |

The render engine is fully independent from the UI: `Glide.Engine` +
`Glide.Graphics` can run headless (see the smoke harness pattern in tests).

## Known limitations (v0.1)

- Exclusive-fullscreen games cannot be overlaid — add them to the exclusion
  list so their own Ctrl+wheel keeps working.
- During UAC/secure-desktop prompts the capture is lost; Glide resets to 100%
  and resumes afterwards.
- The hardware cursor is not magnified (content is; the cursor keeps its size).

## Roadmap

Lens mode, per-app profiles, pinch-gesture simulation, zoom presets,
screenshot/OCR/color-picker of the zoomed region, plugin API.
