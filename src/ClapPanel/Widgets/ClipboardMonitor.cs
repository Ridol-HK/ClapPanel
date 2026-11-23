// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ClapPanel.Widgets;

public sealed class ClipboardMonitor : IDisposable
{
    private const int WmClipboardUpdate = 0x031D;
    private static readonly IntPtr MessageOnlyParent = new(-3);

    private readonly ClipboardHistory _history;
    private HwndSource? _source;

    public ClipboardMonitor(ClipboardHistory history)
    {
        _history = history;
    }

    public bool IsRunning => _source is not null;

    public void Start()
    {
        if (_source is not null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("ClapPanelClipboard")
        {
            ParentWindow = MessageOnlyParent,
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WindowProc);
        AddClipboardFormatListener(_source.Handle);
    }

    public void Stop()
    {
        if (_source is null)
        {
            return;
        }

        RemoveClipboardFormatListener(_source.Handle);
        _source.RemoveHook(WindowProc);
        _source.Dispose();
        _source = null;
    }

    public void Dispose() => Stop();

    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmClipboardUpdate)
        {
            Capture();
        }

        return IntPtr.Zero;
    }

    private void Capture()
    {
        try
        {
            var data = System.Windows.Clipboard.GetDataObject();
            if (data is null
                || data.GetDataPresent("ExcludeClipboardContentFromMonitorProcessing")
                || !data.GetDataPresent(System.Windows.DataFormats.UnicodeText))
            {
                return;
            }

            _history.Add(data.GetData(System.Windows.DataFormats.UnicodeText) as string);
        }
        catch
        {
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
}
