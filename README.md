# KeyFix QR

A lightweight Windows background utility with two superpowers:

1. **Smart Keyboard Layout Fix** — typed text with the wrong keyboard layout (Persian ↔ English)? Select it, press a shortcut, and it's instantly converted using the **exact Microsoft Windows Persian (KBDFA) layout mapping** — no translation, no AI, pure key-position conversion.
2. **Instant Live QR Codes** — select any text, press the QR shortcut, and a floating always-on-top QR window appears right next to your cursor/caret. 100% offline.

---

## Features

| Feature | Details |
|---|---|
| Layout fix | Persian ⇄ English by physical-key mapping (KBDFA.DLL table) |
| Auto direction | Detects dominant script automatically (or force a direction in Settings) |
| Global shortcuts | Work in Chrome, Edge, Firefox, Notepad, Word, VS Code, Telegram, Discord, Slack, … |
| Clipboard-safe | Saves your clipboard before converting and restores it afterwards |
| Live QR | Local generation (QRCoder, UTF-8), floating draggable/resizable topmost window |
| QR positioning | Near the text caret → near mouse → clamped to monitor work-area |
| System tray | Full menu: enable/disable features, settings, start-with-Windows, pause, exit |
| Settings UI | Persian (default, RTL) / English, Light / Dark / Follow-Windows themes |
| Privacy | Everything is processed **locally**; nothing is uploaded, logged, or stored |

---

## Default shortcuts

| Action | Shortcut |
|---|---|
| Convert selected text (auto direction) | `Ctrl + Alt + Space` |
| Toggle / update QR for selected text | `Ctrl + Alt + Q` |
| Close QR overlay | `Esc` or `×` or press `Ctrl + Alt + Q` again |

## How it works

1. You type text but the layout was wrong (e.g. you wanted `hello world` and got `اثممخ صخقمی`).
2. Select the wrong text.
3. Press `Ctrl + Alt + Space`.
4. KeyFix QR copies the selection, converts each character through the real Windows Persian layout table, pastes it back, then restores your original clipboard. A small toast confirms: **«متن اصلاح شد»**.

The QR flow uses the same safe clipboard dance (`Ctrl+C`), generates the PNG locally with QRCoder and shows the overlay near your caret.

### Example mappings (Windows "Persian" layout)

```
q→ض  w→ص  e→ث  r→ق  t→ف  y→غ  u→ع  i→ه  o→خ  p→ح
a→ش  s→س  d→ی  f→ب  g→ل  h→ا  j→ت  k→ن  l→م  ;→ک  '→گ   \→پ
z→ظ  x→ط  c→ز  v→ر  b→ذ  n→د  m→ئ  ,→و
Shift: ?→؟  T→،  R→﷼  H→آ  Z→ة ...
Digits stay Latin (1–0), as in the real KBDFA layout.
```

---

## Build & package

Requires **.NET 8 SDK** (and winget for automatic Inno Setup install).

```powershell
.\build.ps1
```

Artifacts:

| File | Description |
|---|---|
| `dist\KeyFixQR-Setup.exe` | Full installer (Start-Menu/Desktop shortcuts, optional auto-start, clean uninstall) |
| `dist\KeyFixQR-Portable.zip` | Portable single EXE — run anywhere, no install |

Useful switches: `-SkipTests`, `-SkipInstaller`.

## Run

- **Installed:** Start Menu → *KeyFix QR* (or let the installer launch it).
- **Portable:** unzip and run `KeyFixQR.exe`.

The app lives in the system tray. First launch shows **«KeyFix QR فعال شد»**.

## Changing shortcuts

Tray icon → **Settings** → click a shortcut box → press the new combination → **Save**.
Shortcuts must include `Ctrl`, `Alt` or `Win`. Both feature shortcuts must differ.

## Project structure

```
KeyFixQR/
├── KeyFixQR.sln
├── build.ps1
├── src/KeyFixQR/
│   ├── App.xaml(.cs)            # orchestration, hotkey flows
│   ├── Interop/NativeMethods.cs # RegisterHotKey, SendInput, caret/monitor APIs
│   ├── Services/
│   │   ├── KeyboardLayoutService.cs  # exact KBDFA mapping + direction detection
│   │   ├── GlobalHotkeyService.cs    # message-only window + WM_HOTKEY
│   │   ├── ClipboardService.cs       # retry-safe read/write/backup/restore
│   │   ├── InputSender.cs            # Ctrl+C / Ctrl+V synthesis, caret helper
│   │   ├── QrCodeService.cs          # QRCoder UTF-8 PNG
│   │   ├── TrayHost.cs               # NotifyIcon menu + balloons
│   │   ├── SettingsService.cs        # %APPDATA%\KeyFixQR\settings.json
│   │   ├── StartupService.cs         # HKCU Run key
│   │   ├── ThemeService.cs           # Light/Dark/Auto palettes
│   │   └── LocalizationService.cs    # Fa/En strings
│   └── Views/                   # QrOverlayWindow, SettingsWindow (+ Styles.xaml)
├── tests/KeyFixQR.Tests/        # xUnit: mapping round-trips, detection, settings, QR
├── installer/keyfixqr.iss       # Inno Setup script
└── tools/gen-icon.ps1           # icon generator
```

## Manual test matrix

Verified via unit tests + manual runs:

- Notepad / Word / Chrome / Edge / VS Code: convert both directions ✓
- Mixed text, numbers (stay Latin), punctuation, multi-line, special chars (﷼ ، ؟ «») ✓
- QR: short/long/Persian/English/URL/multi-line/mixed input ✓
- No-selection → «ابتدا متن موردنظر را انتخاب کنید.» ✓
- Hotkey conflict → warning toast, changeable in Settings ✓

## Known limitations

- Uses the classic **Persian (KBDFA)** layout table — the default on most systems. If you use the newer *"Persian (Standard)"* layout, letters convert identically, but its shifted digits/symbols differ slightly.
- Clipboard restore covers **text** content; if your clipboard held an image/file when converting, it will be cleared instead of restored.
- Apps that block programmatic paste (some terminals/admin-elevated windows) can't be auto-fixed — run KeyFix QR elevated to reach elevated apps.
- The first paste into slow apps may need the small built-in delay; extremely slow apps could race the clipboard restore (~450 ms).
- Balloon notifications depend on Windows notification settings (Focus Assist can hide them).

## Privacy

No network access. No telemetry. No logging of converted text (a local crash log at `%TEMP%\KeyFixQR.log` contains error messages only). Selected text exists only in memory during conversion.
