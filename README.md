# ShrinkDarkerDisplay

[English](README.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

A compact Windows 11 screen dimmer built with C#, WinUI 3 and .NET 10 NativeAOT.

![screenshot](https://s41.ax1x.com/2026/10/10/pnUdTiV.png)


## Features

- Independent dimming for each monitor, from 10% to 100%.
- Tray panel with light, dark and system themes.
- Simplified Chinese, Traditional Chinese, English and Japanese UI.
- Capture-excluded overlays and an emergency reset shortcut: **Ctrl+Alt+Shift+F12**.

## Install

Download `ShrinkDarkerDisplay-Setup-x64.exe` from [Releases](https://github.com/cneicy/ShrinkDarkerDisplay/releases). Requires Windows 11 x64; no separate .NET installation is needed.

The installer uses a self-signed certificate and requires administrator permission to add local trust for UIAccess. Windows may show a SmartScreen warning. Only install a package from a source you trust; managed devices may block it. The installer currently uses Simplified Chinese.

Click the tray icon to open the panel. Toggle **Dim** to pause or resume; exiting restores normal brightness. Settings are stored in `%LocalAppData%\ShrinkDarkerDisplay` and retained after uninstall.

**Known limitations:** IME candidate windows may flicker. HDR and mixed-monitor setups are not fully verified. The pointer and UAC secure desktop are not dimmed. Screenshot compatibility depends on the capture method.

## Build

Requires the .NET 10 SDK, Windows SDK and Visual C++ x64 build tools. Installer builds also require Inno Setup 6.7+.

```powershell
.\scripts\Build.ps1
.\scripts\Build-Installer.ps1
```

The installer is written to `artifacts/installer/`. Translations are JSON files in `src/ShrinkDarkerDisplay.WinUI/Localization/`.

See [development and release instructions](docs/DEVELOPMENT.md) for project structure, signing and GitHub Actions.

## License

[MIT](LICENSE). Third-party dependencies retain their own licenses.
