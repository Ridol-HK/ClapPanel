# Changelog

[English](CHANGELOG.en.md) | [Español](CHANGELOG.md)

All notable changes to ClapPanel are documented in this file.

## [Unreleased]

## [1.5.0] - 2026-11-22

### Added

- Live microphone monitor: raw input peak and RMS levels, sample rate, channel count, and the filtered level used by the clap detector.
- Capture diagnostics: reports whether the device is active, whether input signal is missing, and whether Windows prevented capture from starting; includes an action to test or temporarily switch microphones.
- Configurable global keyboard shortcut to open or close the panel from any application. It can be enabled or disabled, and its modifiers (Ctrl, Alt, Shift, Win) and key can be selected; the default is Ctrl + Alt + Space.
- First-run setup assistant with welcome, microphone selection and test, sensitivity adjustment, global shortcut, and Windows startup settings. The assistant can be skipped; existing installations do not see it again after upgrading.
- Smart-dock customization: maximum visible shortcuts (1–12), optional favorite-first ordering, and screen-edge reveal zone width (4–40 px). Favorite priority preserves manual order within each group.
- Arrow-key navigation between shortcuts resets the inactivity counter, keeping the panel opaque while the user continues navigating even when the pointer is outside.

### Changed

- Project version set to 1.5.0.

## [1.4.3] - 2026-11-22

### Added

- Option to remember the last selected category and restore it when the panel is reopened.
- Ability to assign a shortcut to multiple categories; it appears when any of its categories is selected.
- Configurable delay (0–5 seconds) before reduced opacity is applied while the pointer is outside the panel.
- Optional smooth fade transition (220 ms) when switching between active and inactive opacity; it can be disabled to apply opacity changes immediately.
- Expanded drag-and-drop reordering: shortcuts can be inserted before or after other items in the panel, and the lists in Settings → Icons and folders and the nested-folder editor can also be reordered. Dropping a shortcut directly onto a folder still moves it inside that folder.
- Smart-dock menu limited to the first six shortcuts in list order. It displays a vertical column when docked left or right and a horizontal row when docked top or bottom; with no fixed docking edge, it uses the horizontal layout.

### Changed

- Project version set to 1.4.3.

## [1.4.0] - 2026-11-22

### Added

- Keyboard navigation in the panel: arrow keys to move, Enter to open, 1–9 and 0 to choose one of the first ten icons, `/` or Ctrl+F to search (also when typing a letter), Home and End to select the first or last icon, Backspace or Alt+Left to go back from a folder, and Esc to clear the search or close the panel.
- Optional shortcut numbers on icons and visible keyboard focus in the panel and Settings.
- Accessibility: screen-reader names for shortcuts, folders, favorites, categories, unavailable items, and shortcut numbers; result announcements; and a dedicated high-contrast mode. ClapPanel automatically uses the Windows high-contrast theme when enabled.
- Installed-app picker when adding or editing an icon: searchable list of Start menu and Microsoft Store applications.
- Optional widgets at the top of the panel: clock (12- or 24-hour format, seconds, and date), auto-saved quick notes, clipboard (text history kept in memory only), and media playback controls (previous, pause, and next).
- Spanish and English interface, with a language selector in Settings → General (System, Español, or English).
- About window with version, license, third-party credits, and privacy note, accessible from the system tray and Settings → General.
- `THIRD-PARTY-NOTICES.md` with NAudio and .NET notices.
- Language tests (including a check that all referenced keys exist), keyboard, widgets, installed applications, accessibility, and About tests.

### Changed

- Esc closes the panel in all modes (previously only in full-screen mode); if the search box contains text, it clears it first.
- When the panel opens, keyboard focus is placed on the first icon so it can be used immediately.
- Changing the language closes the panel if it is open; the next time it opens, it uses the selected language.
- Project version set to 1.4.0.

## [1.3.1] - 2026-11-22

This entry groups everything added since 1.1.0 (versions 1.2.x and 1.3.x).

### Added

- Application organization: internal folders with nested folders, categories, favorites, and search across nested folders.
- Panel filters by category and favorites.
- Drag and drop: reorder icons, move them between folders, and create shortcuts by dropping Windows executables, shortcuts, files, or folders onto the panel.
- Movable panel: drag from the title bar, dock to edges with a one-column vertical layout, and resize from edges and corners with per-monitor saved sizes.
- Monitor selection and preservation of each display's manual position.
- Full-screen mode to fill the selected monitor.
- Configurable automatic hiding from 1 to 30 seconds.
- Base opacity with dynamic transparency when the pointer is outside the panel.
- Configurable animations: none, fade, scale, slide, drop in from the top, soft bounce zoom, and light bounce.
- Visual profiles that save and restore appearance, columns, and full-screen mode.
- Panel ⋯ button that opens the Appearance tab directly.
- Settings window using the same palette as the panel, with preview changes reverted when canceled.
- Settings → General options: allow or disable panel movement, stop microphone capture, restore defaults, and exit ClapPanel.
- Tests for `PanelItem`.
- GitHub Actions workflows to build and test, and to publish the executable when a `v*` tag is created.
- Changelog and release notes.

### Changed

- Project version set to 1.3.1.
- README reorganized into installation, usage, settings, calibration, detection, and project structure sections.
- Expanded `.gitignore` and added `.gitattributes`.

## [1.1.0] - 2026-11-21

### Added

- Panel visual customization: theme, background and accent colors, opacity, icon size, names, rounded corners, position, and animation, with live preview.
- Three-step assisted microphone calibration, verified with the actual detector.
- Settings organized into tabs.

## [1.0.0] - 2026-11-21

### Added

- Double-clap detection with low-frequency filtering, sharp-peak analysis, silence before and between claps, similar clap strength, and an adaptive ambient-noise threshold.
- Panel with configurable application shortcuts, custom images, arguments, and drag reordering.
- System tray icon with a menu to open the panel, open Settings, pause listening, and exit.
- Microphone selection, sensitivity, and optional Windows startup.
- Single-instance application and JSON settings stored in the user's profile.
- GPL-3.0-or-later license.

[1.5.0]: https://github.com/Ridol-HK/ClapPanel/releases/tag/v1.5.0
[1.4.3]: https://github.com/Ridol-HK/ClapPanel/releases/tag/v1.4.3
[1.4.0]: https://github.com/Ridol-HK/ClapPanel/releases/tag/v1.4.0
[1.3.1]: https://github.com/Ridol-HK/ClapPanel/releases/tag/v1.3.1
