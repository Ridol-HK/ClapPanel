// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClapPanel.Launcher;
using ClapPanel.Settings;

namespace ClapPanel.Views;

public partial class PanelWindow : Window
{
    private const double Gap = 16;

    private readonly ObservableCollection<PanelTile> _tiles;
    private readonly AppLauncher _launcher;
    private readonly Action<IReadOnlyList<PanelItem>> _onReorder;
    private Point _dragStart;

    public PanelWindow(
        IReadOnlyList<PanelItem> items,
        int columns,
        AppLauncher launcher,
        IconProvider icons,
        Action<IReadOnlyList<PanelItem>> onReorder)
    {
        _launcher = launcher;
        _onReorder = onReorder;
        _tiles = new ObservableCollection<PanelTile>(
            items.Select(item => new PanelTile(item, icons.Load(item), !launcher.Exists(item.Target))));

        Columns = Math.Clamp(columns, 1, Math.Max(1, Math.Min(12, items.Count)));

        InitializeComponent();

        Tiles.ItemsSource = _tiles;
        EmptyText.Visibility = _tiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var area = SystemParameters.WorkArea;
        MaxHeight = area.Height - 2 * Gap;
        Loaded += (_, _) =>
        {
            Left = area.Right - ActualWidth - Gap;
            Top = area.Bottom - ActualHeight - Gap;
        };
    }

    public int Columns { get; }

    private void Tile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PanelTile tile })
        {
            return;
        }

        if (!_launcher.Launch(tile.Item))
        {
            MessageBox.Show(this, $"No se pudo abrir \"{tile.Item.Name}\".", "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Tile_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
    }

    private void Tile_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var offset = e.GetPosition(null) - _dragStart;
        if (Math.Abs(offset.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(offset.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: PanelTile tile } element)
        {
            DragDrop.DoDragDrop(element, new DataObject(typeof(PanelTile), tile), DragDropEffects.Move);
        }
    }

    private void Tile_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(PanelTile)) is not PanelTile source)
        {
            return;
        }

        if (sender is not FrameworkElement { DataContext: PanelTile target } || ReferenceEquals(source, target))
        {
            return;
        }

        _tiles.Move(_tiles.IndexOf(source), _tiles.IndexOf(target));
        _onReorder(_tiles.Select(tile => tile.Item).ToList());
    }
}
