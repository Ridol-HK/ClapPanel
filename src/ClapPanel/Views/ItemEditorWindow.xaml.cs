// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClapPanel.Launcher;
using ClapPanel.Settings;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace ClapPanel.Views;

public partial class ItemEditorWindow : Window
{
    private readonly ImageStore _store;
    private readonly IconProvider _icons;
    private string? _imagePath;

    public ItemEditorWindow(PanelItem item, ImageStore store, IconProvider icons)
    {
        _store = store;
        _icons = icons;
        _imagePath = item.ImagePath;
        Result = item;

        InitializeComponent();

        NameBox.Text = item.Name;
        TargetBox.Text = item.Target;
        ArgumentsBox.Text = item.Arguments;
        CategoryBox.Text = item.Category;
        FavoriteCheck.IsChecked = item.IsFavorite;
        FolderCheck.IsChecked = item.IsFolder;
        UpdateItemTypeVisibility();
        RefreshPreview();
    }

    public PanelItem Result { get; }

    private void UpdateItemTypeVisibility()
    {
        var isFolder = FolderCheck.IsChecked == true;
        TargetPanel.Visibility = isFolder ? Visibility.Collapsed : Visibility.Visible;
        ArgumentsPanel.Visibility = isFolder ? Visibility.Collapsed : Visibility.Visible;
        Title = isFolder ? "Carpeta de ClapPanel" : "Acceso directo";
    }

    private void FolderType_Changed(object sender, RoutedEventArgs e)
    {
        UpdateItemTypeVisibility();
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        ImageBox.Text = _imagePath ?? "";
        PreviewImage.Source = _icons.Load(new PanelItem
        {
            Target = TargetBox.Text,
            ImagePath = _imagePath,
            Kind = FolderCheck.IsChecked == true ? PanelItemKind.Folder : PanelItemKind.Application
        });
    }

    private void Target_TextChanged(object sender, TextChangedEventArgs e) => RefreshPreview();

    private void BrowseTarget_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar destino",
            Filter = "Todos los archivos|*.*",
            DereferenceLinks = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        TargetBox.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            NameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Selecciona la carpeta que quieres abrir desde ClapPanel",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        TargetBox.Text = dialog.SelectedPath;
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            NameBox.Text = new DirectoryInfo(dialog.SelectedPath).Name;
        }
    }

    private void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar imagen",
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.ico;*.bmp"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _imagePath = dialog.FileName;
        RefreshPreview();
    }

    private void ClearImage_Click(object sender, RoutedEventArgs e)
    {
        _imagePath = null;
        RefreshPreview();
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        var isFolder = FolderCheck.IsChecked == true;
        var target = TargetBox.Text.Trim();
        if (!isFolder && target.Length == 0)
        {
            MessageBox.Show(this, "Indica un destino.", "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show(this, "Indica un nombre para el elemento.", "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Result.ImagePath = string.IsNullOrEmpty(_imagePath) ? null : _store.Import(_imagePath);
        }
        catch (Exception)
        {
            MessageBox.Show(this, "No se pudo copiar la imagen.", "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result.Kind = isFolder ? PanelItemKind.Folder : PanelItemKind.Application;
        Result.Target = isFolder ? "" : target;
        Result.Name = NameBox.Text.Trim();
        Result.Arguments = isFolder ? "" : ArgumentsBox.Text.Trim();
        Result.Category = CategoryBox.Text.Trim();
        Result.IsFavorite = FavoriteCheck.IsChecked == true;
        Result.Children ??= new System.Collections.Generic.List<PanelItem>();

        DialogResult = true;
    }
}
