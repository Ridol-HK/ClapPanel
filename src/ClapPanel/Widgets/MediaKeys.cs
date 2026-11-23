// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Runtime.InteropServices;

namespace ClapPanel.Widgets;

public enum MediaKey : byte
{
    NextTrack = 0xB0,
    PreviousTrack = 0xB1,
    PlayPause = 0xB3
}

public static class MediaKeys
{
    private const uint ExtendedKey = 0x0001;
    private const uint KeyUp = 0x0002;

    public static void Send(MediaKey key)
    {
        keybd_event((byte)key, 0, ExtendedKey, UIntPtr.Zero);
        keybd_event((byte)key, 0, ExtendedKey | KeyUp, UIntPtr.Zero);
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);
}
