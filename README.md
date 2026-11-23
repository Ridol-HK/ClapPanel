# ClapPanel

[English](README.md) | [Español](README.es.md)

ClapPanel is a Windows 10/11 application that opens and closes a shortcut panel when it detects a double clap.

**Latest version: 1.6.0**

## Version 1.6.0 highlights

- Detects application, file, and folder shortcuts whose targets are no longer available and marks them visually.
- Right-click context menu to edit, duplicate, delete, or open an item's location.
- Optional automatic panel closing after successfully launching an application or file.
- **Settings → System Status** page for the clap detector, microphone, global shortcut, configuration, custom images, backups, Windows startup, and installed version.

## Features

- Double-clap detection designed to distinguish claps from speech and ambient noise.
- Shortcut panel with folders, categories, favorites, and search.
- A shortcut can belong to multiple categories at the same time.
- Optional restoration of the last selected category when the panel is opened again.
- Customizable appearance profiles and a panel that can be moved, resized, docked, or displayed full screen.
- Configurable dynamic transparency, including an optional smooth fade and the delay before inactive opacity is applied.
- Live microphone input monitor with peak and average levels, sample rate, channel count, and diagnostics for signal and capture errors.
- Assisted microphone calibration.
- Configurable global keyboard shortcut to open or close the panel while ClapPanel is in the background.
- Keyboard navigation, screen-reader support, and high-contrast mode.
- Optional widgets: clock, quick notes, clipboard history, and media playback controls.
- Spanish and English interface.
- First-run setup assistant to choose and test the microphone, adjust sensitivity, and configure the global shortcut.
- Smart dock can show 1–12 shortcuts, prioritize favorites, and customize the screen-edge reveal zone width.
- Optional Windows startup and system tray icon.
- Full configuration export and import through ZIP archives, including shortcuts, categories, nested folders, appearance profiles, and custom images.
- Shortcut-only import that leaves appearance and all other preferences unchanged.
- Automatic local backups before settings are overwritten, imported, or reset, plus recovery from the latest valid backup if the settings file becomes corrupted.

## Requirements

- Windows 10 or 11 and a microphone.
- .NET 10 SDK to build from source.

## Installation

### Release executable

1. Download `ClapPanel-vX.Y.Z-win-x64.zip` from [Releases](https://github.com/Ridol-HK/ClapPanel/releases).
2. Extract the archive and run `ClapPanel.exe`.

You do not need to install .NET. Since the executable is not signed, Windows SmartScreen may show a warning. Select **More info** and then **Run anyway**.

### From source

```powershell
git clone https://github.com/Ridol-HK/ClapPanel.git
cd ClapPanel
dotnet run --project src/ClapPanel
```

To publish a self-contained executable:

```powershell
dotnet publish src/ClapPanel -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Usage

- **Double clap:** opens the panel; another double clap closes it.
- **Global shortcut:** Ctrl + Alt + Space opens or closes the panel from any application by default. Change or disable it in Settings → General.
- **System tray icon:** opens or closes the panel, opens Settings, pauses microphone capture, or exits the application.
- **Click an icon:** launches its target without closing the panel. ClapPanel folders open inside the same panel.
- **⋯ button:** opens the Appearance tab directly.
- **ClapPanel title bar:** drag it to move the panel when movement is enabled. Moving it close to the left or right edge docks it and switches to a one-column vertical layout.
- **Panel edges and corners:** drag them to resize the panel. Horizontal and vertical sizes are saved separately for each monitor.
- **Drag one icon onto another:** reorder it by dropping before or after the target icon. Dropping it directly onto a folder moves it into that folder; dropping it on empty space moves it to the end of the current list.
- **Settings → Icons and folders:** drag entries in the list to change their order; the same works in the contents editor for nested folders.
- **Drag Windows executables, shortcuts, files, or folders onto the panel:** creates shortcuts. Dropping an item onto an internal folder adds it to that folder.
- **Esc:** clears the search first; when the search is already empty, it closes the panel. The × button does the same.
- **Keyboard controls:** see [Keyboard and accessibility](#keyboard-and-accessibility).

## Backups and configuration transfer

From **Settings → General**, use **Export configuration** to create a ZIP archive or **Import configuration** to restore one. Archives include the shortcut tree (including categories and nested folders), appearance profiles, settings, and custom images. **Import shortcuts only** replaces the shortcut tree while keeping appearance and every other preference unchanged. ClapPanel also keeps up to 20 rolling local backups in `%APPDATA%\ClapPanel\backups`; backups are created before major configuration changes are saved, imported, or reset. High-frequency state updates such as window movement and note autosave do not create a new archive each time. If `settings.json` becomes invalid or a managed custom image is missing, the app attempts to recover the newest valid backup automatically. Use **Restore latest backup** to restore one manually.

## Keyboard and accessibility

When the panel opens, keyboard focus moves to the first icon, so the panel can be used without a mouse.

| Key | Action |
| --- | --- |
| Arrow keys | Move between icons and controls; each move resets the inactivity counter and keeps the panel opaque during keyboard navigation |
| Enter or Space | Open the selected icon or folder |
| 1–9 and 0 | Open items 1–10 in the currently visible list |
| `/`, Ctrl+F, or a letter | Focus the search box |
| Down arrow (in the search box) | Move to the icons |
| Home / End | Select the first or last icon |
| Backspace or Alt+Left | Return to the previous folder |
| Esc | Clear the search or close the panel |

- Shortcut numbers can be displayed on icons (Settings → General → Accessibility).
- Keyboard focus is visible in the panel and Settings. Icons, folders, and buttons have screen-reader names; screen readers also announce the number of results when searching or filtering.
- High-contrast mode (black, white, and yellow) can be enabled in Settings → General → Accessibility. ClapPanel automatically uses it when Windows has a high-contrast theme enabled.

## Widgets

In **Settings → Widgets**, choose which widgets appear at the top of the panel:

- **Clock:** 12- or 24-hour format, optional seconds, and optional date.
- **Quick notes:** a text box that is saved automatically in `settings.json`.
- **Clipboard:** remembers 3–20 recent text entries. Entries are kept only while ClapPanel is running, are not written to disk, and are cleared when the application closes. Password managers that mark their content as private are ignored. Click an entry to copy it again.
- **Media playback controls:** previous track, play/pause, and next track, using Windows multimedia keys.

## Organizing applications and categories

In **Settings → Icons and folders**, you can add and edit applications and internal folders, assign categories, mark favorites, and choose a custom image. A shortcut can belong to more than one category: enter category names separated by commas or semicolons, for example `Games, Multimedia`. Folders support nested folders and applications.

In the panel:

- Search matches names, targets, and categories, including items inside nested folders.
- The category selector and favorites filter also include items inside folders.
- An item assigned to multiple categories appears when any one of those categories is selected.
- Internal folders open inside the panel and include a Back button.
- Items can be rearranged by dragging in the panel or in **Settings → Icons and folders**. In the panel, drop before or after another shortcut to set its position; drop directly onto a folder to move it inside. The Settings list and nested-folder editor also support drag reordering.
- When **Remember the last selected category** is enabled in Settings → General, the panel restores that category the next time it opens. Disable the option to always start with all categories.

When adding or editing a shortcut, **Installed applications…** opens a searchable list of applications from the Start menu and Microsoft Store, so you do not have to locate the `.exe` manually.

Shortcuts support a target, arguments, and an optional image. Targets can be executables, Windows shortcuts, files, folders, or addresses supported by the Windows handler. A folder used as a target opens in File Explorer; an internal ClapPanel folder displays its own items.

## Settings

On a fresh installation, the first time ClapPanel runs, a setup assistant helps choose and test the microphone, adjust sensitivity, and configure the global shortcut. It can be skipped, and all choices can later be changed in Settings. Existing installations keep their settings and do not see the assistant again after upgrading. The Settings window has five tabs: **General, Microphone, Appearance, Icons and folders, and Widgets**. Its tabs, buttons, fields, and lists use the same color palette as the panel.

Appearance changes are previewed while editing. Select **Save** to keep them; select **Cancel** or close the window to revert the preview.

### General

- Configurable global shortcut (Ctrl, Alt, Shift, or Win plus a key) to open or close the panel even while another app is active.
- Optional startup with Windows.
- Allow or disable manual panel movement by dragging its title bar.
- Remember the last selected category when reopening the panel.
- Interface language: System, Español, or English. The language is applied when saved; reopen Settings to refresh every label in the Settings window.
- Accessibility: high-contrast mode and shortcut numbers on icons.
- **About…:** version, license, third-party credits, and privacy note (also available from the system tray menu).
- Stop microphone capture, restore default settings, or close ClapPanel. Restoring defaults also removes custom shortcuts and profiles.

### Microphone

- Select the microphone and adjust detection sensitivity.
- Live monitor for raw input peak and average levels, plus the filtered level used by the clap detector.
- Diagnostics for capture state, low or missing signal, input device name, sample rate, channels, and errors that prevent capture from starting. Use **Test microphone** to start or temporarily switch the input device.
- Run the assisted calibration wizard.

### Appearance

- System, light, or dark theme; background color and accent color.
- Base panel opacity and optional reduced opacity when the pointer is outside the panel.
- Configurable delay before reduced opacity takes effect (0–5 seconds).
- Optional smooth 220 ms fade when switching between active and inactive opacity. Disable **Animate opacity transitions smoothly** to apply the opacity change immediately.
- Icon size, visible names, and rounded corners.
- Configurable opening and closing animation: none, fade, scale, slide, drop in from the top, soft bounce zoom, or light bounce.
- Visual profiles that save and restore the appearance, number of columns, and full-screen mode.

### Position and monitors

- Select the monitor. Each display remembers its last manual position and its horizontal and vertical sizes.
- Initial position in any of the four corners or in the center.
- Optional docking to the left, right, top, or bottom edge. Docking to the left or right switches the panel to a vertical layout.
- **Full screen:** the panel fills the selected monitor. Movement, resizing, and automatic hiding are disabled in this mode.
- **Automatic hiding / smart dock:** configurable from 1 to 30 seconds. Choose 1–12 visible shortcuts and optionally place favorites first while preserving manual order within each group. Left/right docking uses one vertical column; top/bottom docking uses one horizontal row. The screen-edge reveal zone can also be adjusted from 4 to 40 pixels. The panel reappears when the pointer approaches the relevant edge.

## Calibration

In **Settings → Microphone**, select **Calibrate…** to open a three-step wizard:

1. **Silence (3 seconds):** measures background noise.
2. **Voice (4 seconds):** measures how strongly your voice is registered after the low-frequency filter.
3. **Claps (up to 14 seconds):** four double claps, with a one-second pause between each pair.

The wizard calculates a sensitivity value and tests it by replaying the recorded audio through the actual detector. It reports how many double claps were detected and whether any false triggers occurred. Applying the result updates the sensitivity value; select **Save** to keep it.

## Detection

- Low frequencies associated with speech are filtered before the sound is analyzed.
- A clap must produce a sharp peak: a sudden rise followed by a rapid fall.
- There must be silence before the first clap and between the two claps.
- Both claps should have similar strength.
- The threshold adapts to ambient noise and never drops below the level set by the sensitivity control.
- Advanced parameters are available in the `Detection` section of `settings.json`.

## Project structure

```text
ClapPanel/
├── src/ClapPanel/
│   ├── Audio/         microphone capture
│   ├── Calibration/   calibration wizard
│   ├── Detection/     double-clap filter and detector
│   ├── Launcher/      targets, images, icons, and installed apps
│   ├── Localization/  Spanish and English text
│   ├── Resources/     shared visual theme
│   ├── Settings/      settings models and storage
│   ├── Startup/       Windows startup and global hotkeys
│   ├── Theming/       panel palette, layout, and positioning
│   ├── Tray/          system tray icon
│   ├── Views/         panel, settings, calibration, and dialogs
│   └── Widgets/       clock, clipboard, and media keys
├── tests/ClapPanel.Tests/
├── CHANGELOG.md       (Español)
├── CHANGELOG.en.md    (English)
├── LICENSE
├── README.md
└── THIRD-PARTY-NOTICES.md
```

## Tests

```powershell
dotnet test
```

## Privacy

Microphone audio is analyzed in real time to detect claps. It is not saved to disk or sent anywhere. Calibration keeps recorded audio in memory only while the calibration wizard is open.

## License

ClapPanel is distributed under the **GNU General Public License v3.0 or later (GPL-3.0-or-later)**. Copyright (C) 2026 Ridol-HK. See `LICENSE`. Third-party components are listed in `THIRD-PARTY-NOTICES.md`.

## Personal data

Configuration and icon images are stored in `%APPDATA%\ClapPanel\` and are not part of the repository.
