// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

namespace ClapPanel.Settings;

/// <summary>Persistent configuration for the system-wide panel shortcut.</summary>
public sealed class GlobalShortcutSettings
{
    // Values match the MOD_* flags expected by the Windows RegisterHotKey API.
    public const int Alt = 0x0001;
    public const int Control = 0x0002;
    public const int Shift = 0x0004;
    public const int Windows = 0x0008;

    public bool Enabled { get; set; } = true;

    /// <summary>Modifier flags: Alt=1, Control=2, Shift=4, Windows=8.</summary>
    public int Modifiers { get; set; } = Control | Alt;

    /// <summary>Windows virtual-key code. Space (0x20) is the default key.</summary>
    public int VirtualKeyCode { get; set; } = 0x20;

    public GlobalShortcutSettings Clone() => (GlobalShortcutSettings)MemberwiseClone();
}
