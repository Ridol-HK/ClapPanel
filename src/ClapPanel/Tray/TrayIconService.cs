// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClapPanel.Tray;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _pauseItem;

    public TrayIconService()
    {
        _pauseItem = new ToolStripMenuItem("Pausar escucha") { CheckOnClick = true };
        _pauseItem.CheckedChanged += (_, _) => PauseChanged?.Invoke(this, _pauseItem.Checked);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir o cerrar panel", null, (_, _) => TogglePanelRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Ajustes", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "ClapPanel",
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => TogglePanelRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? TogglePanelRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler<bool>? PauseChanged;

    public event EventHandler? ExitRequested;

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
    }
}
