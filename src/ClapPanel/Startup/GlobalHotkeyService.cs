// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ClapPanel.Settings;

namespace ClapPanel.Startup;

/// <summary>Registers a system-wide keyboard shortcut without requiring the panel to be focused.</summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 0xCA71;
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;

    private HotkeyMessageWindow? _window;
    private bool _registered;

    public event EventHandler? Pressed;

    public bool IsRegistered => _registered;

    public int LastErrorCode { get; private set; }

    public bool Apply(GlobalShortcutSettings settings)
    {
        Unregister();
        LastErrorCode = 0;

        if (!settings.Enabled)
        {
            return true;
        }

        var modifiers = (uint)(settings.Modifiers & 0x000F);
        if (modifiers == 0 || settings.VirtualKeyCode <= 0 || settings.VirtualKeyCode > 0xFE)
        {
            LastErrorCode = 87; // ERROR_INVALID_PARAMETER
            return false;
        }

        _window = new HotkeyMessageWindow(this);
        if (!RegisterHotKey(_window.Handle, HotkeyId, modifiers | ModNoRepeat, (uint)settings.VirtualKeyCode))
        {
            LastErrorCode = Marshal.GetLastWin32Error();
            _window.Dispose();
            _window = null;
            return false;
        }

        _registered = true;
        return true;
    }

    public void Suspend() => Unregister();

    public void Dispose() => Unregister();

    private void Unregister()
    {
        if (_window is not null)
        {
            if (_registered)
            {
                UnregisterHotKey(_window.Handle, HotkeyId);
            }

            _registered = false;
            _window.Dispose();
            _window = null;
        }
    }

    private void RaisePressed() => Pressed?.Invoke(this, EventArgs.Empty);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private sealed class HotkeyMessageWindow : NativeWindow, IDisposable
    {
        private readonly GlobalHotkeyService _owner;

        public HotkeyMessageWindow(GlobalHotkeyService owner)
        {
            _owner = owner;
            CreateHandle(new CreateParams { Caption = "ClapPanel.GlobalHotkey" });
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmHotkey && message.WParam.ToInt32() == HotkeyId)
            {
                _owner.RaisePressed();
            }

            base.WndProc(ref message);
        }

        public void Dispose() => DestroyHandle();
    }
}
