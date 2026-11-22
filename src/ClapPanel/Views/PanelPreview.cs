// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ClapPanel.Settings;
using ClapPanel.Theming;

namespace ClapPanel.Views;

public sealed class PanelPreview : Border
{
    public PanelPreview()
    {
        Height = 170;
        CornerRadius = new CornerRadius(8);
        ClipToBounds = true;
        Background = new LinearGradientBrush(Color.FromRgb(0xFF, 0x6B, 0x6B), Color.FromRgb(0x4D, 0x96, 0xFF), 45);
    }

    public void Apply(AppearanceSettings settings)
    {
        var palette = AppearancePalette.From(settings);
        var iconSize = AppearanceLayout.IconSize(settings);
        var tileSize = AppearanceLayout.TileSize(settings);

        var grid = new UniformGrid { Columns = 3 };
        for (var i = 0; i < 3; i++)
        {
            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            content.Children.Add(new Border
            {
                Width = iconSize,
                Height = iconSize,
                CornerRadius = new CornerRadius(settings.RoundedCorners ? 8 : 0),
                Background = AppearancePalette.Brush(palette.Accent)
            });

            if (settings.ShowNames)
            {
                content.Children.Add(new TextBlock
                {
                    Text = "Ejemplo",
                    Foreground = AppearancePalette.Brush(palette.Text),
                    Margin = new Thickness(0, 6, 0, 0),
                    TextAlignment = TextAlignment.Center
                });
            }

            grid.Children.Add(new Border
            {
                Width = tileSize,
                Height = tileSize,
                Margin = new Thickness(4),
                Background = AppearancePalette.Brush(palette.Tile),
                BorderBrush = AppearancePalette.Brush(palette.TileBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = AppearanceLayout.TileRadius(settings),
                Child = content
            });
        }

        var panel = new Border
        {
            Padding = new Thickness(12),
            Background = AppearancePalette.Brush(palette.PanelBackground),
            BorderBrush = AppearancePalette.Brush(palette.Accent),
            BorderThickness = new Thickness(1),
            CornerRadius = AppearanceLayout.PanelRadius(settings),
            Child = grid
        };

        Child = new Viewbox
        {
            Margin = new Thickness(12),
            StretchDirection = StretchDirection.DownOnly,
            Child = panel
        };
    }
}
