# ClapPanel

Windows 10/11 application that opens and closes a shortcut panel when a double clap is detected.

## Features

- Double-clap detection that distinguishes claps from voice and ambient noise.
- Shortcut panel with folders, categories, favorites, and search.
- Customizable appearance with profiles, plus a movable, resizable, and dockable panel.
- Assisted microphone calibration.
- Optional startup with Windows and system tray icon.

## Requirements

- Windows 10 or 11 and a microphone.
- To build from source: .NET 10 SDK.

## Installation

Executable

1. Download "ClapPanel-vX.Y.Z-win-x64.zip" from the repository's Releases section.
2. Extract the contents and run "ClapPanel.exe".

.NET does not need to be installed. Since the executable is not signed, Windows SmartScreen may display a warning: select "More info" and then "Run anyway".

From source

dotnet run --project src/ClapPanel

To build the executable:

dotnet publish src/ClapPanel -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

## Usage

- Double clap: opens the panel; when the panel is already open, closes it.
- System tray icon: opens or closes the panel, opens Settings, pauses microphone capture, or exits the application.
- Clicking an icon: opens its target without closing the panel. Internal ClapPanel folders are displayed within the same panel.
- "⋯" button: directly opens the Appearance tab.
- "ClapPanel" top bar: allows the panel to be dragged when movement is enabled. Moving it close to the left or right edges docks it and changes the layout to a single vertical column.
- Panel edges and corners: allow resizing by dragging the cursor; horizontal and vertical sizes are saved separately for each monitor.
- Dragging one icon onto another: reorders it; dragging it onto a folder moves it into that folder. Dropping it onto empty space moves it to the end of its list.
- Dragging Windows executables, shortcuts, files, or folders onto the panel: creates shortcuts. Dropping an item onto an internal folder adds the shortcut to that folder.
- Fullscreen: press "Esc" to clear the search first and then close the menu, or use the "×" button.

## Application Organization

Under Settings → Icons and folders, you can add and edit applications and internal folders, assign a category, mark items as favorites, and choose a custom image. Folders support subfolders and applications.

## In the panel:

- The search bar finds names, targets, and categories, including items inside nested folders.
- The category selector and favorites filter also include items inside folders.
- Internal folders open within the panel and include a button to go back.
- Items can be rearranged by dragging them and can also be moved between folders.

Shortcuts support a target, arguments, and an optional image. Targets can be executables, Windows shortcuts, files, folders, or addresses supported by the Windows handler. A folder added as a target opens File Explorer; an internal ClapPanel folder displays its own items.

## Settings

The Settings window has four tabs: General, Microphone, Appearance, and Icons and folders. Its tabs, buttons, fields, and lists use the same color palette as the panel.

Appearance changes are previewed while editing:

- Save keeps all changes.
- Cancel or closing the window reverts the preview.

General and Microphone

- Start with Windows.
- Allow or block manual panel movement.
- Stop microphone capture.
- Restore default settings or close ClapPanel. Restoring defaults also removes custom shortcuts and profiles.
- Select the microphone, adjust sensitivity, and perform calibration.

Appearance

- System, light, or dark theme; background color and accent color.
- Base opacity and reduced opacity when the pointer is not over the panel.
- Icon size, visible names, and rounded corners.
- Configurable animation:
  - None
  - Fade
  - Scale
  - Slide
  - Slide in from the top
  - Soft bounce zoom
  - Light bounce
- Number of panel columns.
- Visual profiles that save and restore the appearance, number of columns, and fullscreen mode.

Position and Monitors

- Monitor selection. Each screen keeps its last manual position and horizontal and vertical sizes.
- Initial position in any of the four corners or in the center.
- Optional docking to the left, right, top, or bottom edges. Docking to the left or right changes the panel to a vertical layout.
- Fullscreen: the menu occupies the entire selected monitor. In this mode, movement, resizing, and automatic hiding are disabled.
- Configurable automatic hiding between 1 and 30 seconds. The panel reappears when the pointer approaches the edge associated with its position or docking, or the edge closest to its current location.

Calibration

In Settings → Microphone, the Calibrate button opens a three-step wizard:

1. Silence (3 s): measures background noise.
2. Voice (4 s): measures how far your voice reaches through the low-frequency filter.
3. Claps (up to 14 s): four double claps, with a one-second pause between each.

The wizard calculates the sensitivity and verifies it by playing back the recorded audio through the actual detector. It displays how many double claps were detected and whether any false triggers occurred.

Applying the calibration updates the sensitivity value. You must then press Save.

Detection

- Low frequencies (voice) are filtered out before analyzing the sound.
- The peak must be sharp: a sudden rise followed by a rapid fall.
- There must be silence before the first clap and between the two claps.
- Both claps should have similar strength.
- The threshold adapts to ambient noise and never drops below the level defined by the sensitivity setting.
- Advanced parameters are located in the "Detection" section of "settings.json".

## Project Structure
'''
ClapPanel/
├── src/ClapPanel/
│   ├── Audio/         microphone capture
│   ├── Calibration/   calibration wizard
│   ├── Detection/     double-clap filter and detector
│   ├── Launcher/      target, image, and icon launching
│   ├── Resources/     shared visual theme
│   ├── Settings/      settings models and storage
│   ├── Startup/       Windows startup
│   ├── Theming/       panel palette, layout, and positioning
│   ├── Tray/          system tray icon
│   └── Views/         panel, settings, calibration, and dialogs
├── tests/ClapPanel.Tests/
├── CHANGELOG.md
├── LICENSE
└── README.md
'''
Tests

dotnet test

## License

This project is distributed under the GNU General Public License v3.0 or later (GPL-3.0-or-later).

Copyright (C) 2026 Ridol-HK. See the "LICENSE" file.

Personal Data

Configuration and icon images are stored in:

%APPDATA%\ClapPanel\

They are not part of the repository.