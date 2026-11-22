// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ClapPanel.Audio;
using ClapPanel.Detection;
using ClapPanel.Launcher;
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

    private readonly AudioDeviceProvider _provider;
    private readonly ClapDetector _detector;
    private readonly ImageStore _store;
    private readonly IconProvider _icons;
    private readonly Action<AppSettings> _onSave;
    private readonly Action<AppearanceSettings, int, double?, double?>? _onAppearancePreview;
    private readonly Action _stopMicrophoneRequested;
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
    private double? _panelLeft;
    private double? _panelTop;
    private PanelPosition _lastPreviewPosition;
    private PanelDockEdge _lastPreviewDockEdge;
    private string _lastPreviewMonitor;
    private bool _loading = true;
    private bool _saved;

    private sealed class MonitorOption
    {
        public required string DisplayName { get; init; }
        public required string DeviceName { get; init; }
    }

    public SettingsWindow(
        AppSettings settings,
        AudioDeviceProvider provider,
        ClapDetector detector,
        ImageStore store,
        IconProvider icons,
        Action<AppSettings> onSave,
        Action<AppearanceSettings, int, double?, double?>? onAppearancePreview,
        Action stopMicrophoneRequested,
        Action exitApplicationRequested,
        bool openAppearanceTab = false)
    {
        InitializeComponent();

        _provider = provider;
        _detector = detector;
        _detection = settings.Detection.Clone();
        _store = store;
        _icons = icons;
        _onSave = onSave;
        _onAppearancePreview = onAppearancePreview;
        _stopMicrophoneRequested = stopMicrophoneRequested;
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
        ColumnsSlider.Value = Math.Clamp(settings.Columns, 1, 10);
        ItemsList.ItemsSource = _items;
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
        BuildSwatches(BackgroundSwatches, BackgroundPalette, BackgroundBox);
        BuildSwatches(AccentSwatches, AccentPalette, AccentBox);

        _detector.LevelMeasured += OnLevelChanged;
        Closed += (_, _) =>
        {
            _detector.LevelMeasured -= OnLevelChanged;
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
            DisplayName = $"Monitor {index + 1} — {screen.Bounds.Width} × {screen.Bounds.Height}{(screen.Primary ? " (principal)" : "")}" 
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
            DockEdge = ParseEnum(SelectedTag(DockEdgeCombo), PanelDockEdge.None),
            DynamicTransparency = DynamicTransparencyCheck.IsChecked == true,
            InactiveOpacity = InactiveOpacitySlider.Value / 100.0,
            MonitorDeviceName = SelectedMonitorDeviceName()
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
        MicrophoneStatusText.Text = "La captura está detenida. Para reanudarla, desmarca «Pausar escucha» desde el icono de ClapPanel en la bandeja del sistema.";
    }

    private void ExitApplication_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "¿Querés cerrar ClapPanel por completo?",
            "Cerrar ClapPanel",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _exitApplicationRequested();
        }
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "Se restaurarán los ajustes predeterminados y se eliminarán los iconos personalizados del panel. Esta acción no se puede deshacer. ¿Continuar?",
            "Restablecer valores",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var defaults = new AppSettings();
        _loading = true;

        DeviceCombo.SelectedValue = null;
        DeviceCombo.SelectedIndex = -1;
        SensitivitySlider.Value = defaults.Sensitivity;
        StartupCheck.IsChecked = defaults.StartWithWindows;
        AllowPanelMovementCheck.IsChecked = defaults.AllowPanelMovement;
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
        SelectByTag(AnimationStyleCombo, appearance.AnimationStyle.ToString());
        SelectByTag(PositionCombo, appearance.Position.ToString());
        SelectByTag(DockEdgeCombo, appearance.DockEdge.ToString());
        AutoHideCheck.IsChecked = appearance.AutoHide;
        AutoHideDelaySlider.Value = appearance.AutoHideDelaySeconds;
        MonitorCombo.SelectedValue = Forms.Screen.PrimaryScreen?.DeviceName;
        _lastPreviewPosition = appearance.Position;
        _lastPreviewMonitor = SelectedMonitorDeviceName();

        MicrophoneStatusText.Text = "Se restauraron los valores predeterminados. La captura del micrófono conserva su estado actual.";
        _loading = false;
        UpdatePreview();
    }

    private void OnLevelChanged(object? sender, float level)
    {
        Dispatcher.BeginInvoke(new Action(() => LevelBar.Value = level), DispatcherPriority.Background);
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
        var folder = new PanelItem { Kind = PanelItemKind.Folder, Name = "Nueva carpeta" };
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
            MessageBox.Show(this, "Selecciona primero una carpeta.", "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show(this, "Escribe un nombre para el perfil.", "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Information);
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
        IconSizeSlider.Value = AppearanceLayout.IconSize(appearance);
        ShowNamesCheck.IsChecked = appearance.ShowNames;
        RoundedCheck.IsChecked = appearance.RoundedCorners;
        FullScreenCheck.IsChecked = appearance.FullScreen;
        SelectByTag(AnimationStyleCombo, (appearance.Animation ? appearance.AnimationStyle : PanelAnimationStyle.None).ToString());
        SelectByTag(PositionCombo, appearance.Position.ToString());
        SelectByTag(DockEdgeCombo, appearance.DockEdge.ToString());
        AutoHideCheck.IsChecked = appearance.AutoHide;
        AutoHideDelaySlider.Value = Math.Clamp(appearance.AutoHideDelaySeconds, 1, 30);
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
                CalibrationText.Text = $"Sensibilidad calibrada: {sensitivity}. Pulsa Guardar para aplicarla.";
            }
        }
        finally
        {
            _detector.Muted = false;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!IsValidColor(BackgroundBox.Text) || !IsValidColor(AccentBox.Text))
        {
            MessageBox.Show(
                this,
                "Los colores deben tener formato hexadecimal, por ejemplo #1E1E1E.",
                "ClapPanel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _onSave(new AppSettings
        {
            MicrophoneId = DeviceCombo.SelectedValue as string,
            Sensitivity = (int)SensitivitySlider.Value,
            StartWithWindows = StartupCheck.IsChecked == true,
            Columns = (int)ColumnsSlider.Value,
            AllowPanelMovement = AllowPanelMovementCheck.IsChecked == true,
            PanelLeft = _panelLeft,
            PanelTop = _panelTop,
            Items = _items.Select(item => item.CloneDeep()).ToList(),
            AppearanceProfiles = _profiles.Select(profile => profile.Clone()).ToList(),
            MonitorPositions = _monitorPositions.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.OrdinalIgnoreCase),
            Detection = _detection,
            Appearance = ReadAppearance()
        });

        _saved = true;
        Close();
    }
}
