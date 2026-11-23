// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using ClapPanel.Audio;
using ClapPanel.Detection;
using ClapPanel.Launcher;
using ClapPanel.Localization;
using ClapPanel.Settings;
using ClapPanel.Theming;
using Forms = System.Windows.Forms;
using ClapPanelThemeMode = ClapPanel.Settings.ThemeMode;

namespace ClapPanel.Views;

public partial class SettingsWindow : Window
{
    private static readonly string[] BackgroundPalette =
    {
        "#1E1E1E", "#101820", "#1B2A41", "#2B2118", "#F3F3F3", "#FFF8E7"
    };

    private static readonly string[] AccentPalette =
    {
        "#0078D4", "#E81123", "#10893E", "#FFB900", "#8E24AA", "#00B7C3", "#F7630C", "#E3008C"
    };

    private readonly AppSettings _settingsSource;
    private readonly AudioDeviceProvider _provider;
    private readonly MicrophoneService _microphone;
    private readonly ClapDetector _detector;
    private readonly ImageStore _store;
    private readonly IconProvider _icons;
    private readonly ConfigurationArchiveService _archives;
    private readonly Action<AppSettings> _onSave;
    private readonly Action<AppearanceSettings, int, double?, double?>? _onAppearancePreview;
    private readonly Action _stopMicrophoneRequested;
    private readonly Action<string?> _testMicrophoneRequested;
    private readonly Action _exitApplicationRequested;
    private readonly AppearanceSettings _originalAppearance;
    private readonly int _originalColumns;
    private double? _originalPanelLeft;
    private double? _originalPanelTop;
    private readonly double? _legacyPanelLeft;
    private readonly double? _legacyPanelTop;
    private readonly ObservableCollection<PanelItem> _items;
    private readonly ObservableCollection<AppearanceProfile> _profiles;
    private readonly Dictionary<string, PanelPositionData> _monitorPositions;
    private DetectionOptions _detection;
    private readonly WidgetSettings _widgets;
    private double? _panelLeft;
    private double? _panelTop;
    private PanelPosition _lastPreviewPosition;
    private PanelDockEdge _lastPreviewDockEdge;
    private string _lastPreviewMonitor;
    private bool _loading = true;
    private bool _saved;
    private bool _categorySelectionReset;
    private readonly DispatcherTimer _micStatusTimer;
    private DateTime _lastRawSignalUtc = DateTime.MinValue;
    private float _lastRawPeak;

    private sealed class MonitorOption
    {
        public required string DisplayName { get; init; }
        public required string DeviceName { get; init; }
    }

    public SettingsWindow(
        AppSettings settings,
        AudioDeviceProvider provider,
        MicrophoneService microphone,
        ClapDetector detector,
        ImageStore store,
        IconProvider icons,
        ConfigurationArchiveService archives,
        Action<AppSettings> onSave,
        Action<AppearanceSettings, int, double?, double?>? onAppearancePreview,
        Action stopMicrophoneRequested,
        Action<string?> testMicrophoneRequested,
        Action exitApplicationRequested,
        bool openAppearanceTab = false)
    {
        InitializeComponent();

        _settingsSource = settings;
        _provider = provider;
        _microphone = microphone;
        _detector = detector;
        _detection = settings.Detection.Clone();
        _widgets = settings.Widgets;
        _store = store;
        _icons = icons;
        _archives = archives;
        _onSave = onSave;
        _onAppearancePreview = onAppearancePreview;
        _stopMicrophoneRequested = stopMicrophoneRequested;
        _testMicrophoneRequested = testMicrophoneRequested;
        _exitApplicationRequested = exitApplicationRequested;
        _originalAppearance = settings.Appearance.Clone();
        _originalColumns = settings.Columns;
        _originalPanelLeft = settings.PanelLeft;
        _originalPanelTop = settings.PanelTop;
        _legacyPanelLeft = settings.PanelLeft;
        _legacyPanelTop = settings.PanelTop;
        _panelLeft = settings.PanelLeft;
        _panelTop = settings.PanelTop;
        _lastPreviewPosition = settings.Appearance.Position;
        _items = new ObservableCollection<PanelItem>(settings.Items.Select(item => item.CloneDeep()));
        _profiles = new ObservableCollection<AppearanceProfile>((settings.AppearanceProfiles ?? new List<AppearanceProfile>()).Select(profile => profile.Clone()));
        _monitorPositions = settings.MonitorPositions is null
            ? new Dictionary<string, PanelPositionData>(StringComparer.OrdinalIgnoreCase)
            : settings.MonitorPositions.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.OrdinalIgnoreCase);

        var devices = provider.GetMicrophones();
        DeviceCombo.ItemsSource = devices;
        DeviceCombo.SelectedValue = settings.MicrophoneId;
        if (DeviceCombo.SelectedItem is null && devices.Count > 0)
        {
            DeviceCombo.SelectedIndex = 0;
        }

        SensitivitySlider.Value = settings.Sensitivity;
        StartupCheck.IsChecked = settings.StartWithWindows;
        AllowPanelMovementCheck.IsChecked = settings.AllowPanelMovement;
        RememberCategoryCheck.IsChecked = settings.RememberCategorySelection;
        CloseAfterLaunchCheck.IsChecked = settings.ClosePanelAfterLaunch;
        RefreshSystemStatus();
        var globalShortcut = settings.GlobalShortcut ?? new GlobalShortcutSettings();
        HotkeyEnabledCheck.IsChecked = globalShortcut.Enabled;
        HotkeyCtrlCheck.IsChecked = (globalShortcut.Modifiers & GlobalShortcutSettings.Control) != 0;
        HotkeyAltCheck.IsChecked = (globalShortcut.Modifiers & GlobalShortcutSettings.Alt) != 0;
        HotkeyShiftCheck.IsChecked = (globalShortcut.Modifiers & GlobalShortcutSettings.Shift) != 0;
        HotkeyWinCheck.IsChecked = (globalShortcut.Modifiers & GlobalShortcutSettings.Windows) != 0;
        SelectByTag(HotkeyKeyCombo, globalShortcut.VirtualKeyCode.ToString(CultureInfo.InvariantCulture));
        ColumnsSlider.Value = Math.Clamp(settings.Columns, 1, 10);
        ItemsList.ItemsSource = _items;
        ListBoxReorderBehavior.Attach(ItemsList);
        ProfileCombo.ItemsSource = _profiles;
        var appearance = settings.Appearance;
        BuildMonitorOptions(appearance.MonitorDeviceName);
        MonitorCombo.SelectedValue = string.IsNullOrWhiteSpace(appearance.MonitorDeviceName)
            ? Forms.Screen.PrimaryScreen?.DeviceName
            : appearance.MonitorDeviceName;
        if (MonitorCombo.SelectedItem is null && MonitorCombo.Items.Count > 0)
        {
            MonitorCombo.SelectedIndex = 0;
        }
        _lastPreviewMonitor = SelectedMonitorDeviceName();
        if (_monitorPositions.TryGetValue(_lastPreviewMonitor, out var monitorPosition))
        {
            _panelLeft = monitorPosition.Left;
            _panelTop = monitorPosition.Top;
            _originalPanelLeft = monitorPosition.Left;
            _originalPanelTop = monitorPosition.Top;
        }
        else if (!string.Equals(_lastPreviewMonitor, Forms.Screen.PrimaryScreen?.DeviceName, StringComparison.OrdinalIgnoreCase))
        {
            _panelLeft = null;
            _panelTop = null;
            _originalPanelLeft = null;
            _originalPanelTop = null;
        }

        _lastPreviewDockEdge = appearance.DockEdge;
        SelectByTag(ThemeCombo, appearance.Theme.ToString());
        BackgroundBox.Text = appearance.BackgroundColor;
        AccentBox.Text = appearance.AccentColor;
        OpacitySlider.Value = AppearanceLayout.Opacity(appearance) * 100;
        DynamicTransparencyCheck.IsChecked = appearance.DynamicTransparency;
        InactiveOpacitySlider.Value = Math.Clamp(appearance.InactiveOpacity, 0.2, 1.0) * 100;
        InactiveOpacityDelaySlider.Value = Math.Clamp(appearance.InactiveOpacityDelaySeconds, 0, 5);
        AnimateInactiveOpacityCheck.IsChecked = appearance.AnimateInactiveOpacity;
        IconSizeSlider.Value = AppearanceLayout.IconSize(appearance);
        ShowNamesCheck.IsChecked = appearance.ShowNames;
        RoundedCheck.IsChecked = appearance.RoundedCorners;
        FullScreenCheck.IsChecked = appearance.FullScreen;
        var animationStyle = appearance.Animation ? appearance.AnimationStyle : PanelAnimationStyle.None;
        SelectByTag(AnimationStyleCombo, animationStyle.ToString());
        SelectByTag(PositionCombo, appearance.Position.ToString());
        SelectByTag(DockEdgeCombo, appearance.DockEdge.ToString());
        AutoHideCheck.IsChecked = appearance.AutoHide;
        AutoHideDelaySlider.Value = Math.Clamp(appearance.AutoHideDelaySeconds, 1, 30);
        SmartDockVisibleItemsSlider.Value = Math.Clamp(appearance.SmartDockVisibleItems, 1, 12);
        SmartDockPrioritizeFavoritesCheck.IsChecked = appearance.SmartDockPrioritizeFavorites;
        SmartDockRevealThresholdSlider.Value = Math.Clamp(appearance.SmartDockRevealThresholdPixels, 4, 40);
        HighContrastCheck.IsChecked = appearance.HighContrast;
        ShortcutNumbersCheck.IsChecked = appearance.ShowShortcutNumbers;
        SelectByTag(LanguageCombo, settings.Language.ToString());
        ApplyWidgetControls(settings.Widgets);

        BuildSwatches(BackgroundSwatches, BackgroundPalette, BackgroundBox);
        BuildSwatches(AccentSwatches, AccentPalette, AccentBox);

        _detector.LevelMeasured += OnLevelChanged;
        _microphone.AudioLevelMeasured += OnRawAudioLevelChanged;
        _micStatusTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _micStatusTimer.Tick += (_, _) => RefreshMicrophoneDiagnostics();
        _micStatusTimer.Start();
        RefreshMicrophoneDiagnostics();
        Closed += (_, _) =>
        {
            _detector.LevelMeasured -= OnLevelChanged;
            _microphone.AudioLevelMeasured -= OnRawAudioLevelChanged;
            _micStatusTimer.Stop();
            // Closing with Cancel or the window's X restores the appearance
            // that was active before the user opened this dialog.
            if (!_saved)
            {
                _onAppearancePreview?.Invoke(
                    _originalAppearance.Clone(),
                    _originalColumns,
                    _originalPanelLeft,
                    _originalPanelTop);
            }
        };

        _loading = false;
        if (openAppearanceTab)
        {
            SelectAppearanceTab();
        }
        UpdatePreview();
    }

    public void SelectAppearanceTab()
    {
        SettingsTabs.SelectedItem = AppearanceTab;
    }

    public void SelectIconsTab()
    {
        SettingsTabs.SelectedItem = IconsTab;
        ItemsList.Focus();
    }

    /// <summary>Tracks panel movement while this settings window is open.</summary>
    public void UpdatePanelPosition(double left, double top)
    {
        _panelLeft = left;
        _panelTop = top;
        var monitorName = SelectedMonitorDeviceName();
        if (!_monitorPositions.TryGetValue(monitorName, out var monitorLayout))
        {
            monitorLayout = new PanelPositionData();
            _monitorPositions[monitorName] = monitorLayout;
        }
        monitorLayout.Left = left;
        monitorLayout.Top = top;
        // A physical move is an immediate user action, not an appearance preview;
        // Cancel should not undo that movement.
        _originalPanelLeft = left;
        _originalPanelTop = top;
    }

    private void BuildMonitorOptions(string preferredDeviceName)
    {
        var screens = Forms.Screen.AllScreens;
        MonitorCombo.ItemsSource = screens.Select((screen, index) => new MonitorOption
        {
            DeviceName = screen.DeviceName,
            DisplayName = Loc.Format("Settings.MonitorName", index + 1, screen.Bounds.Width, screen.Bounds.Height, screen.Primary ? Loc.Get("Settings.MonitorPrimary") : "")
        }).ToList();
        MonitorCombo.SelectedValue = string.IsNullOrWhiteSpace(preferredDeviceName)
            ? Forms.Screen.PrimaryScreen?.DeviceName
            : preferredDeviceName;
        if (MonitorCombo.SelectedItem is null && MonitorCombo.Items.Count > 0)
        {
            MonitorCombo.SelectedIndex = 0;
        }
    }

    private string SelectedMonitorDeviceName() => MonitorCombo.SelectedValue as string ?? "";

    private static void BuildSwatches(Panel host, IEnumerable<string> colors, TextBox target)
    {
        foreach (var hex in colors)
        {
            if (!AppearancePalette.TryParseColor(hex, out var color))
            {
                continue;
            }

            var button = new Button
            {
                Width = 22,
                Height = 22,
                MinHeight = 0,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 6, 6),
                Background = AppearancePalette.Brush(color),
                ToolTip = hex
            };
            button.Click += (_, _) => target.Text = hex;
            host.Children.Add(button);
        }
    }

    private static string SelectedTag(ComboBox combo) =>
        combo.SelectedValue as string
        ?? (combo.SelectedItem as ComboBoxItem)?.Tag as string
        ?? "";

    private static void SelectByTag(ComboBox combo, string tag)
    {
        var match = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Tag as string == tag);
        combo.SelectedValue = match?.Tag ?? combo.Items.OfType<ComboBoxItem>().FirstOrDefault()?.Tag;
    }

    private static T ParseEnum<T>(string value, T fallback) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var result) && Enum.IsDefined(result)
            ? result
            : fallback;

    private static string Normalize(string text) =>
        AppearancePalette.TryParseColor(text, out var color) ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : "";

    private static bool IsValidColor(string text) =>
        string.IsNullOrWhiteSpace(text) || AppearancePalette.TryParseColor(text, out _);

    private AppearanceSettings ReadAppearance()
    {
        return new AppearanceSettings
        {
            Theme = ParseEnum(SelectedTag(ThemeCombo), ClapPanelThemeMode.System),
            BackgroundColor = Normalize(BackgroundBox.Text),
            AccentColor = Normalize(AccentBox.Text),
            Opacity = OpacitySlider.Value / 100.0,
            IconSize = (int)IconSizeSlider.Value,
            ShowNames = ShowNamesCheck.IsChecked == true,
            RoundedCorners = RoundedCheck.IsChecked == true,
            FullScreen = FullScreenCheck.IsChecked == true,
            Position = ParseEnum(SelectedTag(PositionCombo), PanelPosition.BottomRight),
            AnimationStyle = ParseEnum(SelectedTag(AnimationStyleCombo), PanelAnimationStyle.Scale),
            Animation = ParseEnum(SelectedTag(AnimationStyleCombo), PanelAnimationStyle.Scale) != PanelAnimationStyle.None,
            AutoHide = AutoHideCheck.IsChecked == true,
            AutoHideDelaySeconds = (int)AutoHideDelaySlider.Value,
            SmartDockVisibleItems = (int)SmartDockVisibleItemsSlider.Value,
            SmartDockPrioritizeFavorites = SmartDockPrioritizeFavoritesCheck.IsChecked == true,
            SmartDockRevealThresholdPixels = (int)SmartDockRevealThresholdSlider.Value,
            DockEdge = ParseEnum(SelectedTag(DockEdgeCombo), PanelDockEdge.None),
            DynamicTransparency = DynamicTransparencyCheck.IsChecked == true,
            InactiveOpacity = InactiveOpacitySlider.Value / 100.0,
            InactiveOpacityDelaySeconds = InactiveOpacityDelaySlider.Value,
            AnimateInactiveOpacity = AnimateInactiveOpacityCheck.IsChecked == true,
            HighContrast = HighContrastCheck.IsChecked == true,
            ShowShortcutNumbers = ShortcutNumbersCheck.IsChecked == true,
            MonitorDeviceName = SelectedMonitorDeviceName()
        };
    }

    private GlobalShortcutSettings ReadGlobalShortcut()
    {
        var modifiers = 0;
        if (HotkeyCtrlCheck.IsChecked == true) modifiers |= GlobalShortcutSettings.Control;
        if (HotkeyAltCheck.IsChecked == true) modifiers |= GlobalShortcutSettings.Alt;
        if (HotkeyShiftCheck.IsChecked == true) modifiers |= GlobalShortcutSettings.Shift;
        if (HotkeyWinCheck.IsChecked == true) modifiers |= GlobalShortcutSettings.Windows;

        return new GlobalShortcutSettings
        {
            Enabled = HotkeyEnabledCheck.IsChecked == true,
            Modifiers = modifiers,
            VirtualKeyCode = int.TryParse(HotkeyKeyCombo.SelectedValue as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out var keyCode)
                ? keyCode
                : 0x20
        };
    }

    private void UpdatePreview()
    {
        if (_loading)
        {
            return;
        }

        // Do not apply partially typed/invalid hexadecimal colors to the live panel.
        if (!IsValidColor(BackgroundBox.Text) || !IsValidColor(AccentBox.Text))
        {
            return;
        }

        var appearance = ReadAppearance();
        Preview.Apply(appearance);
        if (appearance.Position != _lastPreviewPosition)
        {
            // Selecting a preset position removes the previous custom location.
            _panelLeft = null;
            _panelTop = null;
            _monitorPositions.Remove(SelectedMonitorDeviceName());
            _lastPreviewPosition = appearance.Position;
        }

        if (appearance.DockEdge != _lastPreviewDockEdge)
        {
            _panelLeft = null;
            _panelTop = null;
            _monitorPositions.Remove(SelectedMonitorDeviceName());
            _lastPreviewDockEdge = appearance.DockEdge;
        }

        if (!string.Equals(appearance.MonitorDeviceName, _lastPreviewMonitor, StringComparison.OrdinalIgnoreCase))
        {
            if (_monitorPositions.TryGetValue(appearance.MonitorDeviceName, out var saved))
            {
                _panelLeft = saved.Left;
                _panelTop = saved.Top;
            }
            else if (string.Equals(appearance.MonitorDeviceName, Forms.Screen.PrimaryScreen?.DeviceName, StringComparison.OrdinalIgnoreCase)
                && _monitorPositions.Count == 0)
            {
                // Migrate the legacy single-monitor position when selecting the primary display.
                _panelLeft = _legacyPanelLeft;
                _panelTop = _legacyPanelTop;
            }
            else
            {
                _panelLeft = null;
                _panelTop = null;
            }
            _lastPreviewMonitor = appearance.MonitorDeviceName;
        }

        _onAppearancePreview?.Invoke(appearance, (int)ColumnsSlider.Value, _panelLeft, _panelTop);
    }

    private void Appearance_Changed(object sender, RoutedEventArgs e) => UpdatePreview();

    private void ClearBackground_Click(object sender, RoutedEventArgs e) => BackgroundBox.Text = "";

    private void ClearAccent_Click(object sender, RoutedEventArgs e) => AccentBox.Text = "";

    private void StopMicrophone_Click(object sender, RoutedEventArgs e)
    {
        _stopMicrophoneRequested();
        MicrophoneStatusText.Text = Loc.Get("Settings.MicStopped");
    }

    private void ExitApplication_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            Loc.Get("Settings.ExitConfirm"),
            Loc.Get("Settings.ExitApp"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _exitApplicationRequested();
        }
    }

    private void ExportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = Loc.Get("Settings.ExportDialogTitle"),
            Filter = "ClapPanel backup (*.zip)|*.zip",
            DefaultExt = ".zip",
            AddExtension = true,
            FileName = $"ClapPanel-backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _archives.Export(dialog.FileName);
            BackupStatusText.Text = Loc.Get("Settings.ExportSuccess");
        }
        catch (Exception exception)
        {
            ShowBackupError(exception);
        }
    }

    private void ImportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.Get("Settings.ImportDialogTitle"),
            Filter = "ClapPanel backup (*.zip)|*.zip|ZIP (*.zip)|*.zip",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (MessageBox.Show(this, Loc.Get("Settings.ImportConfirm"), "ClapPanel",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var imported = _archives.Import(dialog.FileName, _settingsSource, shortcutsOnly: false);
            _onSave(imported);
            _saved = true;
            BackupStatusText.Text = Loc.Get("Settings.ImportSuccess");
            Close();
        }
        catch (Exception exception)
        {
            ShowBackupError(exception);
        }
    }

    private void ImportShortcuts_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.Get("Settings.ImportDialogTitle"),
            Filter = "ClapPanel backup (*.zip)|*.zip|ZIP (*.zip)|*.zip",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (MessageBox.Show(this, Loc.Get("Settings.ImportShortcutsConfirm"), "ClapPanel",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        if (!TryBuildSettingsFromControls(out var currentSettings))
        {
            return;
        }

        try
        {
            var imported = _archives.Import(dialog.FileName, currentSettings, shortcutsOnly: true);
            _onSave(imported);
            _saved = true;
            BackupStatusText.Text = Loc.Get("Settings.ImportShortcutsSuccess");
            Close();
        }
        catch (Exception exception)
        {
            ShowBackupError(exception);
        }
    }

    private void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, Loc.Get("Settings.RestoreConfirm"), "ClapPanel",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            if (!_archives.TryRestoreLatestBackup(out var restored) || restored is null)
            {
                BackupStatusText.Text = Loc.Get("Settings.NoBackup");
                return;
            }

            _onSave(restored);
            _saved = true;
            BackupStatusText.Text = Loc.Get("Settings.RestoreSuccess");
            Close();
        }
        catch (Exception exception)
        {
            ShowBackupError(exception);
        }
    }

    private void ShowBackupError(Exception exception)
    {
        var message = Loc.Format("Settings.BackupError", exception.Message);
        BackupStatusText.Text = message;
        MessageBox.Show(this, message, "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            Loc.Get("Settings.ResetConfirm"),
            Loc.Get("Settings.ResetTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _archives.CreateBackup();
        }
        catch (Exception exception)
        {
            ShowBackupError(exception);
            return;
        }

        var defaults = new AppSettings();
        _categorySelectionReset = true;
        _loading = true;

        DeviceCombo.SelectedValue = null;
        DeviceCombo.SelectedIndex = -1;
        SensitivitySlider.Value = defaults.Sensitivity;
        StartupCheck.IsChecked = defaults.StartWithWindows;
        AllowPanelMovementCheck.IsChecked = defaults.AllowPanelMovement;
        RememberCategoryCheck.IsChecked = defaults.RememberCategorySelection;
        CloseAfterLaunchCheck.IsChecked = defaults.ClosePanelAfterLaunch;
        HotkeyEnabledCheck.IsChecked = defaults.GlobalShortcut.Enabled;
        HotkeyCtrlCheck.IsChecked = (defaults.GlobalShortcut.Modifiers & GlobalShortcutSettings.Control) != 0;
        HotkeyAltCheck.IsChecked = (defaults.GlobalShortcut.Modifiers & GlobalShortcutSettings.Alt) != 0;
        HotkeyShiftCheck.IsChecked = (defaults.GlobalShortcut.Modifiers & GlobalShortcutSettings.Shift) != 0;
        HotkeyWinCheck.IsChecked = (defaults.GlobalShortcut.Modifiers & GlobalShortcutSettings.Windows) != 0;
        SelectByTag(HotkeyKeyCombo, defaults.GlobalShortcut.VirtualKeyCode.ToString(CultureInfo.InvariantCulture));
        ColumnsSlider.Value = defaults.Columns;
        _items.Clear();
        ItemsList.SelectedIndex = -1;
        _profiles.Clear();
        _monitorPositions.Clear();
        _detection = defaults.Detection;
        _panelLeft = null;
        _panelTop = null;

        var appearance = defaults.Appearance;
        SelectByTag(ThemeCombo, appearance.Theme.ToString());
        BackgroundBox.Text = appearance.BackgroundColor;
        AccentBox.Text = appearance.AccentColor;
        OpacitySlider.Value = AppearanceLayout.Opacity(appearance) * 100;
        IconSizeSlider.Value = AppearanceLayout.IconSize(appearance);
        ShowNamesCheck.IsChecked = appearance.ShowNames;
        RoundedCheck.IsChecked = appearance.RoundedCorners;
        FullScreenCheck.IsChecked = appearance.FullScreen;
        DynamicTransparencyCheck.IsChecked = appearance.DynamicTransparency;
        InactiveOpacitySlider.Value = appearance.InactiveOpacity * 100;
        InactiveOpacityDelaySlider.Value = appearance.InactiveOpacityDelaySeconds;
        AnimateInactiveOpacityCheck.IsChecked = appearance.AnimateInactiveOpacity;
        SelectByTag(AnimationStyleCombo, appearance.AnimationStyle.ToString());
        SelectByTag(PositionCombo, appearance.Position.ToString());
        SelectByTag(DockEdgeCombo, appearance.DockEdge.ToString());
        AutoHideCheck.IsChecked = appearance.AutoHide;
        AutoHideDelaySlider.Value = appearance.AutoHideDelaySeconds;
        SmartDockVisibleItemsSlider.Value = appearance.SmartDockVisibleItems;
        SmartDockPrioritizeFavoritesCheck.IsChecked = appearance.SmartDockPrioritizeFavorites;
        SmartDockRevealThresholdSlider.Value = appearance.SmartDockRevealThresholdPixels;
        HighContrastCheck.IsChecked = appearance.HighContrast;
        ShortcutNumbersCheck.IsChecked = appearance.ShowShortcutNumbers;
        SelectByTag(LanguageCombo, defaults.Language.ToString());
        ApplyWidgetControls(new WidgetSettings());
        MonitorCombo.SelectedValue = Forms.Screen.PrimaryScreen?.DeviceName;
        _lastPreviewPosition = appearance.Position;
        _lastPreviewMonitor = SelectedMonitorDeviceName();

        MicrophoneStatusText.Text = Loc.Get("Settings.ResetDone");
        _loading = false;
        UpdatePreview();
    }

    private void OnLevelChanged(object? sender, float level)
    {
        Dispatcher.BeginInvoke(new Action(() => LevelBar.Value = level), DispatcherPriority.Background);
    }

    private void OnRawAudioLevelChanged(object? sender, MicrophoneLevelEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            RawPeakBar.Value = Math.Clamp(e.Peak, 0, 1);
            RawRmsBar.Value = Math.Clamp(e.Rms * 3, 0, 1);
            _lastRawPeak = e.Peak;
            if (e.Peak >= 0.008f)
            {
                _lastRawSignalUtc = DateTime.UtcNow;
            }

            MicMeasurementText.Text = Loc.Format("Settings.MicMeasurements", e.Peak * 100, e.Rms * 100, e.SampleRate, e.Channels);
            RefreshMicrophoneDiagnostics();
        }), DispatcherPriority.Background);
    }

    private void RefreshMicrophoneDiagnostics()
    {
        if (!_microphone.IsRunning)
        {
            MicDeviceStatusText.Text = string.IsNullOrWhiteSpace(_microphone.LastError)
                ? Loc.Get("Settings.MicNotRunning")
                : Loc.Format("Settings.MicCaptureError", _microphone.LastError);
            return;
        }

        if (_lastRawSignalUtc == DateTime.MinValue || DateTime.UtcNow - _lastRawSignalUtc > TimeSpan.FromSeconds(2))
        {
            MicDeviceStatusText.Text = _lastRawPeak > 0.001f
                ? Loc.Get("Settings.MicQuiet")
                : Loc.Get("Settings.MicWaitingSignal");
        }
        else
        {
            MicDeviceStatusText.Text = Loc.Format("Settings.MicSignalDetected", _microphone.ActiveDeviceName ?? "");
        }
    }

    private void TestMicrophone_Click(object sender, RoutedEventArgs e)
    {
        _lastRawSignalUtc = DateTime.MinValue;
        _lastRawPeak = 0;
        RawPeakBar.Value = 0;
        RawRmsBar.Value = 0;
        MicMeasurementText.Text = "—";
        _testMicrophoneRequested(DeviceCombo.SelectedValue as string);
        RefreshMicrophoneDiagnostics();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var editor = new ItemEditorWindow(new PanelItem(), _store, _icons) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            _items.Add(editor.Result);
            ItemsList.SelectedItem = editor.Result;
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var index = ItemsList.SelectedIndex;
        if (index < 0)
        {
            return;
        }

        var editor = new ItemEditorWindow(_items[index].Clone(), _store, _icons) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            _items[index] = editor.Result;
            ItemsList.SelectedIndex = index;
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var index = ItemsList.SelectedIndex;
        if (index < 0)
        {
            return;
        }

        _items.RemoveAt(index);
        if (_items.Count > 0)
        {
            ItemsList.SelectedIndex = Math.Min(index, _items.Count - 1);
        }
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        var index = ItemsList.SelectedIndex;
        if (index <= 0)
        {
            return;
        }

        _items.Move(index, index - 1);
        ItemsList.SelectedIndex = index - 1;
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        var index = ItemsList.SelectedIndex;
        if (index < 0 || index >= _items.Count - 1)
        {
            return;
        }

        _items.Move(index, index + 1);
        ItemsList.SelectedIndex = index + 1;
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = new PanelItem { Kind = PanelItemKind.Folder, Name = Loc.Get("Common.NewFolder") };
        var editor = new ItemEditorWindow(folder, _store, _icons) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            _items.Add(editor.Result);
            ItemsList.SelectedItem = editor.Result;
        }
    }

    private void EditFolder_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is not PanelItem { IsFolder: true } folder)
        {
            MessageBox.Show(this, Loc.Get("Settings.SelectFolderFirst"), "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var editor = new FolderContentsWindow(folder, _store, _icons) { Owner = this };
        editor.ShowDialog();
        ItemsList.Items.Refresh();
    }

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        var name = ProfileNameBox.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show(this, Loc.Get("Settings.ProfileNameRequired"), "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var existing = _profiles.FirstOrDefault(profile => string.Equals(profile.Name, name, StringComparison.CurrentCultureIgnoreCase));
        var profile = new AppearanceProfile
        {
            Name = name,
            Columns = (int)ColumnsSlider.Value,
            Appearance = ReadAppearance()
        };

        if (existing is null)
        {
            _profiles.Add(profile);
            ProfileCombo.SelectedItem = profile;
        }
        else
        {
            var index = _profiles.IndexOf(existing);
            _profiles[index] = profile;
            ProfileCombo.SelectedItem = profile;
        }
    }

    private void ApplyProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is not AppearanceProfile profile)
        {
            return;
        }

        _loading = true;
        var appearance = profile.Appearance;
        SelectByTag(ThemeCombo, appearance.Theme.ToString());
        BackgroundBox.Text = appearance.BackgroundColor;
        AccentBox.Text = appearance.AccentColor;
        OpacitySlider.Value = AppearanceLayout.Opacity(appearance) * 100;
        DynamicTransparencyCheck.IsChecked = appearance.DynamicTransparency;
        InactiveOpacitySlider.Value = Math.Clamp(appearance.InactiveOpacity, 0.2, 1.0) * 100;
        InactiveOpacityDelaySlider.Value = Math.Clamp(appearance.InactiveOpacityDelaySeconds, 0, 5);
        AnimateInactiveOpacityCheck.IsChecked = appearance.AnimateInactiveOpacity;
        IconSizeSlider.Value = AppearanceLayout.IconSize(appearance);
        ShowNamesCheck.IsChecked = appearance.ShowNames;
        RoundedCheck.IsChecked = appearance.RoundedCorners;
        FullScreenCheck.IsChecked = appearance.FullScreen;
        SelectByTag(AnimationStyleCombo, (appearance.Animation ? appearance.AnimationStyle : PanelAnimationStyle.None).ToString());
        SelectByTag(PositionCombo, appearance.Position.ToString());
        SelectByTag(DockEdgeCombo, appearance.DockEdge.ToString());
        AutoHideCheck.IsChecked = appearance.AutoHide;
        AutoHideDelaySlider.Value = Math.Clamp(appearance.AutoHideDelaySeconds, 1, 30);
        SmartDockVisibleItemsSlider.Value = Math.Clamp(appearance.SmartDockVisibleItems, 1, 12);
        SmartDockPrioritizeFavoritesCheck.IsChecked = appearance.SmartDockPrioritizeFavorites;
        SmartDockRevealThresholdSlider.Value = Math.Clamp(appearance.SmartDockRevealThresholdPixels, 4, 40);
        HighContrastCheck.IsChecked = appearance.HighContrast;
        ShortcutNumbersCheck.IsChecked = appearance.ShowShortcutNumbers;
        MonitorCombo.SelectedValue = string.IsNullOrWhiteSpace(appearance.MonitorDeviceName)
            ? Forms.Screen.PrimaryScreen?.DeviceName
            : appearance.MonitorDeviceName;
        if (MonitorCombo.SelectedItem is null && MonitorCombo.Items.Count > 0)
        {
            MonitorCombo.SelectedIndex = 0;
        }
        ColumnsSlider.Value = Math.Clamp(profile.Columns, 1, 10);
        _loading = false;
        UpdatePreview();
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is AppearanceProfile profile)
        {
            _profiles.Remove(profile);
            ProfileNameBox.Clear();
        }
    }

    private void Calibrate_Click(object sender, RoutedEventArgs e)
    {
        var window = new CalibrationWindow(
            _provider,
            DeviceCombo.SelectedValue as string,
            _detection,
            (int)SensitivitySlider.Value)
        {
            Owner = this
        };

        _detector.Muted = true;
        try
        {
            if (window.ShowDialog() == true && window.Sensitivity is { } sensitivity)
            {
                SensitivitySlider.Value = sensitivity;
                CalibrationText.Text = Loc.Format("Settings.Calibrated", sensitivity);
            }
        }
        finally
        {
            _detector.Muted = false;
        }
    }

    private void ApplyWidgetControls(WidgetSettings widgets)
    {
        ClockWidgetCheck.IsChecked = widgets.ShowClock;
        Clock24Check.IsChecked = widgets.ClockUse24Hours;
        ClockSecondsCheck.IsChecked = widgets.ClockShowSeconds;
        ClockDateCheck.IsChecked = widgets.ClockShowDate;
        NotesWidgetCheck.IsChecked = widgets.ShowNotes;
        ClipboardWidgetCheck.IsChecked = widgets.ShowClipboard;
        ClipboardCountSlider.Value = Math.Clamp(widgets.ClipboardMaxItems, 3, 20);
        MediaWidgetCheck.IsChecked = widgets.ShowMedia;
    }

    private WidgetSettings ReadWidgets()
    {
        _widgets.ShowClock = ClockWidgetCheck.IsChecked == true;
        _widgets.ClockUse24Hours = Clock24Check.IsChecked == true;
        _widgets.ClockShowSeconds = ClockSecondsCheck.IsChecked == true;
        _widgets.ClockShowDate = ClockDateCheck.IsChecked == true;
        _widgets.ShowNotes = NotesWidgetCheck.IsChecked == true;
        _widgets.ShowClipboard = ClipboardWidgetCheck.IsChecked == true;
        _widgets.ClipboardMaxItems = (int)ClipboardCountSlider.Value;
        _widgets.ShowMedia = MediaWidgetCheck.IsChecked == true;
        return _widgets;
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private bool TryBuildSettingsFromControls(out AppSettings settings)
    {
        settings = new AppSettings();
        if (!IsValidColor(BackgroundBox.Text) || !IsValidColor(AccentBox.Text))
        {
            MessageBox.Show(
                this,
                Loc.Get("Settings.ColorFormat"),
                "ClapPanel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }

        var globalShortcut = ReadGlobalShortcut();
        if (globalShortcut.Enabled && globalShortcut.Modifiers == 0)
        {
            MessageBox.Show(this, Loc.Get("Settings.GlobalShortcutNeedsModifier"), "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        settings = new AppSettings
        {
            FirstRunCompleted = _settingsSource.FirstRunCompleted,
            GlobalShortcut = globalShortcut,
            MicrophoneId = DeviceCombo.SelectedValue as string,
            Sensitivity = (int)SensitivitySlider.Value,
            StartWithWindows = StartupCheck.IsChecked == true,
            Columns = (int)ColumnsSlider.Value,
            AllowPanelMovement = AllowPanelMovementCheck.IsChecked == true,
            RememberCategorySelection = RememberCategoryCheck.IsChecked == true,
            ClosePanelAfterLaunch = CloseAfterLaunchCheck.IsChecked == true,
            LastSelectedCategory = RememberCategoryCheck.IsChecked == true && !_categorySelectionReset ? _settingsSource.LastSelectedCategory : "",
            PanelLeft = _panelLeft,
            PanelTop = _panelTop,
            Items = _items.Select(item => item.CloneDeep()).ToList(),
            AppearanceProfiles = _profiles.Select(profile => profile.Clone()).ToList(),
            MonitorPositions = _monitorPositions.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.OrdinalIgnoreCase),
            Detection = _detection,
            Appearance = ReadAppearance(),
            Language = ParseEnum(SelectedTag(LanguageCombo), AppLanguage.System),
            Widgets = ReadWidgets()
        };
        return true;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildSettingsFromControls(out var settings))
        {
            return;
        }

        _onSave(settings);
        _saved = true;
        Close();
    }
    private void RefreshSystemStatus_Click(object sender, RoutedEventArgs e) => RefreshSystemStatus();

    private void RefreshSystemStatus()
    {
        var mic = false;
        try { mic = _provider.GetMicrophones().Any(device => string.Equals(device.Id, _settingsSource.MicrophoneId, StringComparison.OrdinalIgnoreCase)); }
        catch { }
        var captureRunning = _microphone.IsRunning;
        var shortcut = _settingsSource.GlobalShortcut?.Enabled == true;
        var items = _settingsSource.Items ?? new List<PanelItem>();
        var allItems = FlattenItems(items).ToList();
        var broken = allItems.Count(item => !string.IsNullOrWhiteSpace(item.ImagePath) && !File.Exists(Environment.ExpandEnvironmentVariables(item.ImagePath)));
        var launcher = new AppLauncher();
        var targetsBroken = allItems.Count(item =>
            (!item.IsFolder || !string.IsNullOrWhiteSpace(item.Target)) && !launcher.Exists(item.Target));
        var config = _settingsSource.Items is not null && allItems.All(item => item is not null);
        var backups = false;
        try { backups = Directory.Exists(_archives.BackupsDirectory) && Directory.EnumerateFiles(_archives.BackupsDirectory, "*.zip").Any(); } catch { }
        var rows = new[]
        {
            $"{Loc.Get("Status.ClapDetector")}: {(captureRunning ? Loc.Get("Status.Running") : Loc.Get("Status.Stopped"))}",
            $"{Loc.Get("Status.Microphone")}: {(mic ? Loc.Get("Status.Connected") : Loc.Get("Status.Disconnected"))}",
            $"{Loc.Get("Status.GlobalShortcut")}: {(shortcut ? Loc.Get("Status.Configured") : Loc.Get("Status.Disabled"))}",
            $"{Loc.Get("Status.Configuration")}: {(config ? Loc.Get("Status.Correct") : Loc.Get("Status.Warning"))}",
            $"{Loc.Get("Status.CustomImages")}: {(broken == 0 ? Loc.Get("Status.Correct") : Loc.Format("Status.MissingCount", broken))}",
            $"{Loc.Get("Status.Backups")}: {(backups ? Loc.Get("Status.Available") : Loc.Get("Status.NotFound"))}",
            $"{Loc.Get("Status.Startup")}: {(_settingsSource.StartWithWindows ? Loc.Get("Status.Configured") : Loc.Get("Status.Disabled"))}",
            $"{Loc.Get("Status.Version")}: {AppInfo.Version}",
            $"{Loc.Get("Status.BrokenTargets")}: {targetsBroken}"
        };
        SystemStatusText.Text = string.Join(Environment.NewLine, rows);
    }

    private static IEnumerable<PanelItem> FlattenItems(IEnumerable<PanelItem> source)
    {
        foreach (var item in source)
        {
            yield return item;
            if (item.Children is not null)
                foreach (var child in FlattenItems(item.Children)) yield return child;
        }
    }

}
