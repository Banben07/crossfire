# CrossfireCrosshair

Windows desktop crosshair overlay for FPS games that do not offer custom crosshair settings.

## Features

- Transparent top-most crosshair overlay (click-through, no focus steal)
- Profile system (add, duplicate, delete, reset defaults)
- Presets inspired by common `CS` and `VALORANT` style setups
- Real-time preview and live update
- Minimize to system tray with tray menu (restore/toggle/exit)
- Optional auto-start with Windows (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`)
- Share-code import/export for crosshair profiles
- Extensive customization:
  - line length, line thickness, gap
  - center dot and dot size
  - T-style
  - outline and outline thickness
  - opacity
  - screen offsets
  - dynamic spread value
  - optional temporary spread while holding `WASD`, arrow keys, or `Space`
  - hex color + color picker
- Global hotkeys:
  - toggle overlay
  - cycle profile
- JSON settings persistence at:
  - `%APPDATA%\CrossfireCrosshair\settings.json`

## Build

Requirements:

- Windows 10/11
- .NET 8 SDK

Build and run:

```powershell
dotnet build
dotnet run
```

## Share Code

- Export: click `Copy current code` in the app.
- Import: click `Import from clipboard` or `Paste code...`.
- Imported profiles are sanitized and clamped to safe value ranges.

## Publish (Single-file EXE)

Build a release single-file executable:

```powershell
dotnet publish CrossfireCrosshair.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishTrimmed=false `
  -o artifacts/win-x64
```

Or use the provided script:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1
```

## GitHub Actions CI/CD

Workflow file: `.github/workflows/windows-build.yml`

It does:

- Restore + build on `push`, `pull_request`, and manual trigger.
- Publish `win-x64` single-file release package.
- Upload `.zip` + `.sha256` as build artifacts.
- Create GitHub Release automatically when tag matches `v*`.
- Optionally sign the executable when signing secrets are configured.

### Optional Code-signing Secrets

Set these repository secrets to enable signing in CI:

- `WINDOWS_PFX_BASE64`: base64-encoded `.pfx` certificate content
- `WINDOWS_PFX_PASSWORD`: certificate password

## Security and Compliance Notes

- This app only draws a desktop overlay and listens for global hotkeys.
- It does **not** inject into game processes.
- It does **not** read game memory.
- It does **not** install kernel drivers.
- It does **not** require administrator privileges.

No tool can guarantee anti-cheat acceptance in every game. Always follow the game's terms and anti-cheat policy.

## Reducing False Positives

- Build from source yourself.
- Keep dependencies minimal.
- Avoid obfuscators/packers.
- Sign release binaries with a trusted code-signing certificate.
- Publish checksums and release notes for each build.
