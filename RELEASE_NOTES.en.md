[English](RELEASE_NOTES.en.md) | [Español](RELEASE_NOTES.md)

## ClapPanel 1.6.0

ClapPanel is a Windows 10/11 application that opens and closes a shortcut panel when it detects a double clap.

### What's new

- Broken shortcut detection for applications, files, and folders with configured targets.
- Right-click context menu to edit, duplicate, delete, or open an item's location.
- Optional automatic panel closing after successfully launching an application or file.
- New System Status section in Settings with the state of core components and the installed version.
- Export and import the complete configuration through ZIP archives, including shortcuts, categories, nested folders, appearance profiles, and custom images.
- Import shortcuts only to preserve the current appearance and all other preferences.
- Automatic backups before overwriting settings, importing a configuration, or restoring defaults.
- Automatic recovery from the latest valid backup when the settings file is damaged, plus manual restore from Settings → General.
- Keep up to 20 rolling local backups in `%APPDATA%\ClapPanel\backups`.

### Installation

1. Download `ClapPanel-v1.6.0-win-x64.zip` from this release's assets.
2. Extract the archive and run `ClapPanel.exe`.

You do not need to install .NET. Since the executable is not signed, Windows SmartScreen may show a warning: select **More info** and then **Run anyway**.

The full change history is available in `CHANGELOG.en.md` and `CHANGELOG.md`.

Repository: https://github.com/Ridol-HK/ClapPanel

---

## ClapPanel 1.4.3

ClapPanel is a Windows 10/11 application that opens and closes a shortcut panel when it detects a double clap.

### What's new

- Remember the last selected category when reopening the panel.
- Assign the same shortcut to multiple categories.
- Configure the delay (0–5 seconds) before reducing opacity while the pointer is outside the panel.
- Enable or disable the smooth opacity fade (220 ms).
- Reorder shortcuts by dragging them in the panel, in Settings → Icons and folders, and in the nested-folder editor.
- Smart-dock menu that displays up to six shortcuts and adapts its orientation to the docking edge.

### Installation

1. Download `ClapPanel-v1.4.3-win-x64.zip` from this release's assets.
2. Extract the archive and run `ClapPanel.exe`.

You do not need to install .NET. Since the executable is not signed, Windows SmartScreen may show a warning: select **More info** and then **Run anyway**.

The full change history is available in `CHANGELOG.en.md` and `CHANGELOG.md`.

Repository: https://github.com/Ridol-HK/ClapPanel
