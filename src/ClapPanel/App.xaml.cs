// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Forms = System.Windows.Forms;
using System.Threading;
using System.Windows;
using ClapPanel.Audio;
using ClapPanel.Detection;
using ClapPanel.Launcher;
using ClapPanel.Settings;
using ClapPanel.Startup;
using ClapPanel.Tray;
using ClapPanel.Theming;
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
    private bool _profileChooserShown;

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
        AppearancePalette.From(_settings.Appearance).Apply(Resources);
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
            if (_panel.IsAutoHidden)
            {
                _panel.RevealFromCommand();
            }
            else
            {
                _panel.CloseWithAnimation();
            }
            return;
        }

        // The profile chooser is shown only on the first panel activation of
        // each application run, and only when there are at least two profiles.
        if (!_profileChooserShown)
        {
            _profileChooserShown = true;
            var profiles = _settings.AppearanceProfiles ?? new List<AppearanceProfile>();
            if (profiles.Count > 1)
            {
                var chooser = new ProfileSelectionWindow(profiles);
                if (chooser.ShowDialog() == true && chooser.SelectedProfile is { } selectedProfile)
                {
                    ApplyAppearanceProfile(selectedProfile);
                }
            }
        }

        OpenPanel();
    }

    private void ApplyAppearanceProfile(AppearanceProfile profile)
    {
        _settings.Appearance = profile.Appearance.Clone();
        _settings.Columns = Math.Clamp(profile.Columns, 1, 10);
        AppearancePalette.From(_settings.Appearance).Apply(Resources);
        _settingsService.Save(_settings);
    }

    private void OpenPanel()
    {
        _panel = new PanelWindow(
            _settings.Items,
            _settings.Columns,
            _settings.Appearance,
            _launcher,
            _icons,
            ReorderItems,
            () => OpenSettings(selectAppearance: true),
            GetSavedPanelPosition(),
            _settings.AllowPanelMovement,
            PersistPanelPosition,
            GetSavedPanelSize(verticalDocked: false),
            GetSavedPanelSize(verticalDocked: true),
            PersistPanelDockEdge,
            PersistPanelSize);
        _panel.Closed += (_, _) => _panel = null;
        _panel.Show();
        _panel.Activate();
    }

    private void OpenSettings(bool selectAppearance = false)
    {
        if (_settingsWindow is not null)
        {
            if (selectAppearance)
            {
                _settingsWindow.SelectAppearanceTab();
            }

            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(
            _settings,
            _devices,
            _detector,
            _imageStore,
            _icons,
            SaveSettings,
            PreviewAppearance,
            StopMicrophoneCapture,
            () => Shutdown(),
            selectAppearance);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;

        // Keep auto-hide paused while any settings window is open so the panel
        // does not disappear while the user is changing its appearance.
        var panelForSettings = _panel;
        panelForSettings?.SetAutoHideSuspended(true);
        _settingsWindow.Closed += (_, _) => panelForSettings?.SetAutoHideSuspended(false);

        if (selectAppearance && panelForSettings is { IsVisible: true })
        {
            _settingsWindow.Owner = panelForSettings;
            _settingsWindow.ShowDialog();
        }
        else
        {
            _settingsWindow.Show();
        }
    }

    private string SelectedMonitorDeviceName()
    {
        if (!string.IsNullOrWhiteSpace(_settings.Appearance.MonitorDeviceName))
        {
            return _settings.Appearance.MonitorDeviceName;
        }

        return Forms.Screen.PrimaryScreen?.DeviceName ?? "";
    }

    private Point? GetSavedPanelPosition()
    {
        var monitorName = SelectedMonitorDeviceName();
        if (_settings.MonitorPositions is not null
            && _settings.MonitorPositions.TryGetValue(monitorName, out var saved))
        {
            return new Point(saved.Left, saved.Top);
        }

        // Preserve legacy coordinates only on the primary monitor. They were
        // written before ClapPanel stored a separate location for each display.
        if (string.Equals(monitorName, Forms.Screen.PrimaryScreen?.DeviceName, StringComparison.OrdinalIgnoreCase)
            && _settings.PanelLeft is double left && _settings.PanelTop is double top)
        {
            return new Point(left, top);
        }

        return null;
    }

    private Size? GetSavedPanelSize(bool verticalDocked)
    {
        var monitorName = SelectedMonitorDeviceName();
        if (_settings.MonitorPositions is null
            || !_settings.MonitorPositions.TryGetValue(monitorName, out var saved))
        {
            return null;
        }

        var width = verticalDocked ? saved.DockedWidth : saved.Width;
        var height = verticalDocked ? saved.DockedHeight : saved.Height;
        return width is > 0 && height is > 0 ? new Size(width.Value, height.Value) : null;
    }

    private PanelPositionData GetOrCreateMonitorLayout()
    {
        _settings.MonitorPositions ??= new Dictionary<string, PanelPositionData>(StringComparer.OrdinalIgnoreCase);
        var monitorName = SelectedMonitorDeviceName();
        if (!_settings.MonitorPositions.TryGetValue(monitorName, out var layout))
        {
            layout = new PanelPositionData();
            _settings.MonitorPositions[monitorName] = layout;
        }

        return layout;
    }

    private void PersistPanelPosition(Point position)
    {
        _settings.PanelLeft = position.X;
        _settings.PanelTop = position.Y;
        var layout = GetOrCreateMonitorLayout();
        layout.Left = position.X;
        layout.Top = position.Y;
        _settingsWindow?.UpdatePanelPosition(position.X, position.Y);
        _settingsService.Save(_settings);
    }

    private void PersistPanelDockEdge(PanelDockEdge edge)
    {
        _settings.Appearance.DockEdge = edge;
        _settingsService.Save(_settings);
    }

    private void PersistPanelSize(Size size, bool verticalDocked)
    {
        var layout = GetOrCreateMonitorLayout();
        if (verticalDocked)
        {
            layout.DockedWidth = size.Width;
            layout.DockedHeight = size.Height;
        }
        else
        {
            layout.Width = size.Width;
            layout.Height = size.Height;
        }

        _settingsService.Save(_settings);
    }

    private void PreviewAppearance(AppearanceSettings appearance, int columns, double? panelLeft, double? panelTop)
    {
        // Settings dialogs use DynamicResource brushes, so live previews update their controls too.
        AppearancePalette.From(appearance).Apply(Resources);
        Point? customPosition = panelLeft is double left && panelTop is double top
            ? new Point(left, top)
            : null;

        _panel?.ApplyAppearance(appearance, columns, customPosition);
    }

    private void StopMicrophoneCapture()
    {
        if (_tray is not null)
        {
            _tray.SetPaused(true);
        }

        if (!_paused)
        {
            _paused = true;
            ApplyAudio();
        }
    }

    private void ReorderItems(IReadOnlyList<PanelItem> items)
    {
        _settings.Items = items.ToList();
        _settingsService.Save(_settings);
    }

    private static IEnumerable<PanelItem> FlattenItems(IEnumerable<PanelItem> items)
    {
        foreach (var item in items)
        {
            yield return item;
            if (item.IsFolder && item.Children is not null)
            {
                foreach (var child in FlattenItems(item.Children))
                {
                    yield return child;
                }
            }
        }
    }

    private void SaveSettings(AppSettings settings)
    {
        _settings = settings;
        AppearancePalette.From(settings.Appearance).Apply(Resources);
        _settingsService.Save(settings);

        // The detector keeps the original options object, so copy the saved values into it.
        _options.CopyFrom(settings.Detection);

        // Reflect saved panel settings immediately without requiring a close/reopen.
        _panel?.ApplySettings(
            settings.Items,
            settings.Columns,
            settings.Appearance,
            GetSavedPanelPosition(),
            settings.AllowPanelMovement);

        _imageStore.Cleanup(FlattenItems(settings.Items).Select(item => item.ImagePath));
        StartupManager.SetEnabled(settings.StartWithWindows);
        ApplyAudio();
    }
}
