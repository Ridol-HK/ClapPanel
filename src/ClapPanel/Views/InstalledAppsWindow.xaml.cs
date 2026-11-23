// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ClapPanel.Launcher;
using ClapPanel.Localization;
using ClapPanel.Settings;

namespace ClapPanel.Views;

public sealed class InstalledAppEntry
{
    private readonly IconProvider _icons;
    private ImageSource? _icon;

    public InstalledAppEntry(InstalledApp app, IconProvider icons)
    {
        App = app;
        _icons = icons;
    }

    public InstalledApp App { get; }

    public string Name => App.Name;

    public ImageSource Icon => _icon ??= _icons.Load(new PanelItem { Target = App.Target });
}

public partial class InstalledAppsWindow : Window
{
    private readonly IconProvider _icons;
    private ICollectionView? _view;

    public InstalledAppsWindow(IconProvider icons)
    {
        _icons = icons;
        InitializeComponent();
        StatusText.Text = Loc.Get("Picker.Loading");
        Loaded += async (_, _) => await LoadAsync();
    }

    public InstalledApp? SelectedApp { get; private set; }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        var apps = await InstalledAppsScanner.ScanAsync();
        var entries = apps.Select(app => new InstalledAppEntry(app, _icons)).ToList();

        AppsList.ItemsSource = entries;
        _view = CollectionViewSource.GetDefaultView(entries);
        _view.Filter = MatchesSearch;
        UpdateStatus();

        if (entries.Count > 0)
        {
            AppsList.SelectedIndex = 0;
        }
    }

    private bool MatchesSearch(object item)
    {
        var term = SearchBox.Text.Trim();
        return term.Length == 0
            || (item is InstalledAppEntry entry && entry.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }

    private void UpdateStatus()
    {
        var count = _view?.Cast<object>().Count() ?? 0;
        StatusText.Text = count == 0 ? Loc.Get("Picker.None") : Loc.Format("Picker.Count", count);
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_view is null)
        {
            return;
        }

        _view.Refresh();
        UpdateStatus();
        if (AppsList.SelectedItem is null && AppsList.Items.Count > 0)
        {
            AppsList.SelectedIndex = 0;
        }
    }

    private void AppsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectButton.IsEnabled = AppsList.SelectedItem is InstalledAppEntry;
    }

    private void AppsList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => Confirm();

    private void Select_Click(object sender, RoutedEventArgs e) => Confirm();

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Confirm()
    {
        if (AppsList.SelectedItem is InstalledAppEntry entry)
        {
            SelectedApp = entry.App;
            DialogResult = true;
        }
    }
}
