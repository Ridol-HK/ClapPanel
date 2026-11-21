// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using ClapPanel.Audio;
using ClapPanel.Detection;
using ClapPanel.Launcher;
using ClapPanel.Settings;

namespace ClapPanel.Views;

public partial class SettingsWindow : Window
{
    private readonly ClapDetector _detector;
    private readonly DetectionOptions _detection;
    private readonly ImageStore _store;
    private readonly IconProvider _icons;
    private readonly Action<AppSettings> _onSave;
    private readonly ObservableCollection<PanelItem> _items;

    public SettingsWindow(
        AppSettings settings,
        IReadOnlyList<AudioDevice> devices,
        ClapDetector detector,
        ImageStore store,
        IconProvider icons,
        Action<AppSettings> onSave)
    {
        InitializeComponent();

        _detector = detector;
        _detection = settings.Detection;
        _store = store;
        _icons = icons;
        _onSave = onSave;
        _items = new ObservableCollection<PanelItem>(settings.Items.Select(item => item.Clone()));

        DeviceCombo.ItemsSource = devices;
        DeviceCombo.SelectedValue = settings.MicrophoneId;
        if (DeviceCombo.SelectedItem is null && devices.Count > 0)
        {
            DeviceCombo.SelectedIndex = 0;
        }

        SensitivitySlider.Value = settings.Sensitivity;
        StartupCheck.IsChecked = settings.StartWithWindows;
        ColumnsSlider.Value = Math.Clamp(settings.Columns, 1, 10);
        ItemsList.ItemsSource = _items;

        _detector.LevelMeasured += OnLevelChanged;
        Closed += (_, _) => _detector.LevelMeasured -= OnLevelChanged;
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

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _onSave(new AppSettings
        {
            MicrophoneId = DeviceCombo.SelectedValue as string,
            Sensitivity = (int)SensitivitySlider.Value,
            StartWithWindows = StartupCheck.IsChecked == true,
            Columns = (int)ColumnsSlider.Value,
            Items = _items.ToList(),
            Detection = _detection
        });

        Close();
    }
}
