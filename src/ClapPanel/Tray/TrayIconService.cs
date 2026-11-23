// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Drawing;
using System.Windows.Forms;
using ClapPanel.Localization;

namespace ClapPanel.Tray;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _aboutItem;
    private readonly ToolStripMenuItem _exitItem;

    public TrayIconService()
    {
        _toggleItem = new ToolStripMenuItem();
        _toggleItem.Click += (_, _) => TogglePanelRequested?.Invoke(this, EventArgs.Empty);

        _settingsItem = new ToolStripMenuItem();
        _settingsItem.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);

        _pauseItem = new ToolStripMenuItem { CheckOnClick = true };
        _pauseItem.CheckedChanged += (_, _) => PauseChanged?.Invoke(this, _pauseItem.Checked);

        _aboutItem = new ToolStripMenuItem();
        _aboutItem.Click += (_, _) => AboutRequested?.Invoke(this, EventArgs.Empty);

        _exitItem = new ToolStripMenuItem();
        _exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var menu = new ContextMenuStrip();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(_settingsItem);
        menu.Items.Add(_pauseItem);
        menu.Items.Add(_aboutItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitItem);

        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "ClapPanel",
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => TogglePanelRequested?.Invoke(this, EventArgs.Empty);

        ApplyLanguage();
    }

    public event EventHandler? TogglePanelRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler<bool>? PauseChanged;

    public event EventHandler? AboutRequested;

    public event EventHandler? ExitRequested;

    public void ApplyLanguage()
    {
        _toggleItem.Text = Loc.Get("Tray.Toggle");
        _settingsItem.Text = Loc.Get("Settings.Title");
        _pauseItem.Text = Loc.Get("Tray.Pause");
        _aboutItem.Text = Loc.Get("Tray.About");
        _exitItem.Text = Loc.Get("Tray.Exit");
    }

    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _icon.ShowBalloonTip(3500, title, message, icon);
    }

    public void SetPaused(bool paused)
    {
        if (_pauseItem.Checked != paused)
        {
            _pauseItem.Checked = paused;
        }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
    }
}
