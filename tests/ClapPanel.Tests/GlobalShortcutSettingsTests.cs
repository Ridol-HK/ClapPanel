// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using ClapPanel.Settings;

namespace ClapPanel.Tests;

public class GlobalShortcutSettingsTests
{
    [Fact]
    public void DefaultShortcutIsCtrlAltSpace()
    {
        var shortcut = new GlobalShortcutSettings();

        Assert.True(shortcut.Enabled);
        Assert.Equal(GlobalShortcutSettings.Control | GlobalShortcutSettings.Alt, shortcut.Modifiers);
        Assert.Equal(0x20, shortcut.VirtualKeyCode);
    }

    [Fact]
    public void CloningPreservesTheShortcutConfiguration()
    {
        var shortcut = new GlobalShortcutSettings
        {
            Enabled = false,
            Modifiers = GlobalShortcutSettings.Windows | GlobalShortcutSettings.Shift,
            VirtualKeyCode = 0x50
        };

        var clone = shortcut.Clone();

        Assert.False(clone.Enabled);
        Assert.Equal(shortcut.Modifiers, clone.Modifiers);
        Assert.Equal(shortcut.VirtualKeyCode, clone.VirtualKeyCode);
    }
}
