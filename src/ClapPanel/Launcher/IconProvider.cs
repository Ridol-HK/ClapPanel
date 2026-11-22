// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using DrawingIcon = System.Drawing.Icon;
using DrawingSystemIcons = System.Drawing.SystemIcons;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaPen = System.Windows.Media.Pen;
using System.Windows.Media.Imaging;
using ClapPanel.Settings;

namespace ClapPanel.Launcher;

public sealed class IconProvider
{
    private static readonly Lazy<ImageSource> Fallback = new(() => FromIcon(DrawingSystemIcons.Application));
    private static readonly Lazy<ImageSource> FolderFallback = new(CreateFolderImage);

    public ImageSource Load(PanelItem item)
    {
        if (item.IsFolder || Directory.Exists(Environment.ExpandEnvironmentVariables(item.Target ?? "")))
        {
            return LoadImage(item.ImagePath) ?? FolderFallback.Value;
        }

        return LoadImage(item.ImagePath) ?? LoadAssociated(item.Target) ?? Fallback.Value;
    }

    private static ImageSource? LoadImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? LoadAssociated(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        try
        {
            var path = Environment.ExpandEnvironmentVariables(target.Trim());
            if (!File.Exists(path))
            {
                return null;
            }

            using var icon = DrawingIcon.ExtractAssociatedIcon(path);
            return icon is null ? null : FromIcon(icon);
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource FromIcon(DrawingIcon icon)
    {
        var source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        source.Freeze();
        return source;
    }

    private static ImageSource CreateFolderImage()
    {
        var folderGeometry = Geometry.Parse("M 2,5 L 9,5 L 11,7 L 22,7 Q 24,7 24,9 L 24,19 Q 24,21 22,21 L 3,21 Q 1,21 1,19 L 1,7 Q 1,5 2,5 Z");
        var tabGeometry = Geometry.Parse("M 2,6 L 9,6 L 11,8 L 23,8 L 23,10 L 2,10 Z");
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(
            new SolidColorBrush(MediaColor.FromRgb(0xE8, 0xB7, 0x4A)),
            new MediaPen(new SolidColorBrush(MediaColor.FromRgb(0xA8, 0x79, 0x25)), 0.8),
            folderGeometry));
        group.Children.Add(new GeometryDrawing(
            new SolidColorBrush(MediaColor.FromRgb(0xF5, 0xD0, 0x72)),
            null,
            tabGeometry));

        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}
