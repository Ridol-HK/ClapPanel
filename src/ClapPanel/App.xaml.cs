// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using ClapPanel.Audio;
using ClapPanel.Detection;
using ClapPanel.Launcher;
using ClapPanel.Settings;
using ClapPanel.Startup;
using ClapPanel.Tray;
using ClapPanel.Views;

namespace ClapPanel;

public partial class App : Application
{
    private readonly SettingsService _settingsService = new();
    private readonly AudioDeviceProvider _devices = new();
    private readonly AppLauncher _launcher = new();
    private readonly IconProvider _icons = new();
    private readonly ImageStore _imageStore = new();
    private readonly MicrophoneService _microphone;

    private DetectionOptions _options = new();
    private ClapDetector _detector = null!;

    private Mutex? _mutex;
    private bool _ownsMutex;
    private TrayIconService? _tray;
    private PanelWindow? _panel;
    private SettingsWindow? _settingsWindow;
    private AppSettings _settings = new();
    private bool _paused;

    public App()
    {
        _microphone = new MicrophoneService(_devices);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, "ClapPanel.SingleInstance", out _ownsMutex);
        if (!_ownsMutex)
        {
            Shutdown();
            return;
        }

        _settings = _settingsService.Load();
        _options = _settings.Detection;
        _detector = new ClapDetector(_options);

        _microphone.SamplesAvailable += _detector.Process;
        _detector.DoubleClapDetected += (_, _) => Dispatcher.BeginInvoke(new Action(TogglePanel));

        _tray = new TrayIconService();
        _tray.TogglePanelRequested += (_, _) => TogglePanel();
        _tray.SettingsRequested += (_, _) => OpenSettings();
        _tray.PauseChanged += (_, paused) =>
        {
            _paused = paused;
            ApplyAudio();
        };
        _tray.ExitRequested += (_, _) => Shutdown();

        ApplyAudio();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _microphone.Dispose();
        _devices.Dispose();
        if (_ownsMutex)
        {
            _mutex?.ReleaseMutex();
        }
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void ApplyAudio()
    {
        _options.ApplySensitivity(_settings.Sensitivity);
        if (_paused)
        {
            _microphone.Stop();
        }
        else
        {
            _microphone.Start(_settings.MicrophoneId);
        }
    }

    private void TogglePanel()
    {
        if (_panel is not null)
        {
            _panel.Close();
            return;
        }

        _panel = new PanelWindow(_settings.Items, _settings.Columns, _launcher, _icons, ReorderItems);
        _panel.Closed += (_, _) => _panel = null;
        _panel.Show();
        _panel.Activate();
    }

    private void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(
            _settings,
            _devices.GetMicrophones(),
            _detector,
            _imageStore,
            _icons,
            SaveSettings);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void ReorderItems(IReadOnlyList<PanelItem> items)
    {
        _settings.Items = items.ToList();
        _settingsService.Save(_settings);
    }

    private void SaveSettings(AppSettings settings)
    {
        _settings = settings;
        _settingsService.Save(settings);
        _imageStore.Cleanup(settings.Items.Select(item => item.ImagePath));
        StartupManager.SetEnabled(settings.StartWithWindows);
        ApplyAudio();
    }
}
