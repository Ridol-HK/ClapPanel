// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClapPanel.Launcher;
using ClapPanel.Localization;
using ClapPanel.Settings;

namespace ClapPanel.Views;

public partial class FolderContentsWindow : Window
{
    private readonly PanelItem _folder;
    private readonly ImageStore _store;
    private readonly IconProvider _icons;
    private readonly ObservableCollection<PanelItem> _items;

    public FolderContentsWindow(PanelItem folder, ImageStore store, IconProvider icons)
    {
        _folder = folder;
        _store = store;
        _icons = icons;
        _folder.Children ??= new System.Collections.Generic.List<PanelItem>();
        _items = new ObservableCollection<PanelItem>(_folder.Children);

        InitializeComponent();
        FolderTitle.Text = Loc.Format("Folder.Header", _folder.Name);
        ItemsList.ItemsSource = _items;
        ListBoxReorderBehavior.Attach(ItemsList);
        if (_items.Count > 0)
        {
            ItemsList.SelectedIndex = 0;
        }

        Closed += (_, _) => _folder.Children = _items.ToList();
    }

    private void AddApplication_Click(object sender, RoutedEventArgs e)
    {
        var editor = new ItemEditorWindow(new PanelItem(), _store, _icons) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            _items.Add(editor.Result);
            ItemsList.SelectedItem = editor.Result;
        }
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

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is not PanelItem selected)
        {
            return;
        }

        var index = _items.IndexOf(selected);
        var editor = new ItemEditorWindow(selected.CloneDeep(), _store, _icons) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            _items[index] = editor.Result;
            ItemsList.SelectedIndex = index;
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is not PanelItem { IsFolder: true } folder)
        {
            return;
        }

        var window = new FolderContentsWindow(folder, _store, _icons) { Owner = this };
        window.ShowDialog();
        ItemsList.Items.Refresh();
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

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
