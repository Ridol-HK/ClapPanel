// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Windows;
using ClapPanel.Settings;

namespace ClapPanel.Theming;

public static class AppearanceLayout
{
    public const double MinIconSize = 24;

    public const double MaxIconSize = 96;

    public const double MinOpacity = 0.4;

    public static double IconSize(AppearanceSettings settings) =>
        Math.Clamp(settings.IconSize, MinIconSize, MaxIconSize);

    public static double Opacity(AppearanceSettings settings) =>
        Math.Clamp(settings.Opacity, MinOpacity, 1.0);

    public static double TileSize(AppearanceSettings settings) =>
        settings.ShowNames
            ? Math.Max(80, IconSize(settings) + 56)
            : Math.Max(48, IconSize(settings) + 28);

    public static CornerRadius PanelRadius(AppearanceSettings settings) =>
        new(settings.RoundedCorners ? 12 : 0);

    public static CornerRadius TileRadius(AppearanceSettings settings) =>
        new(settings.RoundedCorners ? 8 : 0);
}
