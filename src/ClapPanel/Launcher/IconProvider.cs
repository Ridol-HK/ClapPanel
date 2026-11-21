// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClapPanel.Settings;

namespace ClapPanel.Launcher;

public sealed class IconProvider
{
    private static readonly Lazy<ImageSource> Fallback = new(() => FromIcon(SystemIcons.Application));

    public ImageSource Load(PanelItem item)
    {
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

    private static ImageSource? LoadAssociated(string target)
    {
        try
        {
            var path = Environment.ExpandEnvironmentVariables(target.Trim());
            if (!File.Exists(path))
            {
                return null;
            }

            using var icon = Icon.ExtractAssociatedIcon(path);
            return icon is null ? null : FromIcon(icon);
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource FromIcon(Icon icon)
    {
        var source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        source.Freeze();
        return source;
    }
}
