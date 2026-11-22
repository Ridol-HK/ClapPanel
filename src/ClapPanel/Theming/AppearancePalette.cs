// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ClapPanel.Settings;
using ClapPanelThemeMode = ClapPanel.Settings.ThemeMode;
using Microsoft.Win32;

namespace ClapPanel.Theming;

public sealed class AppearancePalette
{
    public Color PanelBackground { get; init; }

    public Color Tile { get; init; }

    public Color TileHover { get; init; }

    public Color TileBorder { get; init; }

    public Color Accent { get; init; }

    public Color Text { get; init; }

    public Color Muted { get; init; }

    public Color WindowNavigation { get; init; }

    public Color AccentSoft { get; init; }

    public Color AccentHover { get; init; }

    public Color AccentText { get; init; }

    public Color Danger { get; init; }

    public Color DangerHover { get; init; }

    public static AppearancePalette From(AppearanceSettings settings)
    {
        var light = settings.Theme switch
        {
            ClapPanelThemeMode.Light => true,
            ClapPanelThemeMode.Dark => false,
            _ => SystemUsesLightTheme()
        };

        var background = light ? Rgb(0xF3, 0xF3, 0xF3) : Rgb(0x1E, 0x1E, 0x1E);
        var tile = light ? Rgb(0xFF, 0xFF, 0xFF) : Rgb(0x2D, 0x2D, 0x30);
        var hover = light ? Rgb(0xE8, 0xE8, 0xE8) : Rgb(0x3A, 0x3A, 0x3F);
        var border = light ? Rgb(0xD0, 0xD0, 0xD0) : Rgb(0x3F, 0x3F, 0x46);
        var text = light ? Rgb(0x1E, 0x1E, 0x1E) : Rgb(0xFF, 0xFF, 0xFF);
        var accent = Rgb(0x00, 0x78, 0xD4);

        if (TryParseColor(settings.BackgroundColor, out var custom))
        {
            var dark = Luminance(custom) < 0.5;
            var toward = dark ? Rgb(0xFF, 0xFF, 0xFF) : Rgb(0x00, 0x00, 0x00);
            background = custom;
            text = dark ? Rgb(0xFF, 0xFF, 0xFF) : Rgb(0x1E, 0x1E, 0x1E);
            tile = Mix(custom, toward, 0.08);
            hover = Mix(custom, toward, 0.16);
            border = Mix(custom, toward, 0.22);
        }

        if (TryParseColor(settings.AccentColor, out var customAccent))
        {
            accent = customAccent;
        }

        var alpha = (byte)Math.Round(255 * AppearanceLayout.Opacity(settings));

        return new AppearancePalette
        {
            PanelBackground = Color.FromArgb(alpha, background.R, background.G, background.B),
            Tile = tile,
            TileHover = hover,
            TileBorder = border,
            Accent = accent,
            Text = text,
            Muted = Mix(text, background, 0.45),
            WindowNavigation = Mix(background, tile, 0.34),
            AccentSoft = Mix(background, accent, 0.20),
            AccentHover = Mix(accent, Luminance(accent) > 0.55 ? Rgb(0x00, 0x00, 0x00) : Rgb(0xFF, 0xFF, 0xFF), 0.14),
            AccentText = Luminance(accent) > 0.58 ? Rgb(0x18, 0x18, 0x18) : Rgb(0xFF, 0xFF, 0xFF),
            Danger = Rgb(0xD1, 0x34, 0x38),
            DangerHover = Rgb(0xA4, 0x26, 0x2C)
        };
    }

    public static bool TryParseColor(string? text, out Color color)
    {
        color = default;

        var hex = text?.Trim().TrimStart('#');
        if (hex is null || (hex.Length != 6 && hex.Length != 8) || !hex.All(Uri.IsHexDigit))
        {
            return false;
        }

        var offset = hex.Length == 8 ? 2 : 0;
        color = Color.FromRgb(
            Convert.ToByte(hex.Substring(offset, 2), 16),
            Convert.ToByte(hex.Substring(offset + 2, 2), 16),
            Convert.ToByte(hex.Substring(offset + 4, 2), 16));
        return true;
    }

    public static SolidColorBrush Brush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public void Apply(ResourceDictionary resources)
    {
        resources["PanelBackgroundBrush"] = Brush(PanelBackground);
        resources["TileBrush"] = Brush(Tile);
        resources["TileHoverBrush"] = Brush(TileHover);
        resources["TileBorderBrush"] = Brush(TileBorder);
        resources["AccentBrush"] = Brush(Accent);
        resources["TextBrush"] = Brush(Text);
        resources["MutedTextBrush"] = Brush(Muted);
        resources["WindowBackgroundBrush"] = Brush(Color.FromRgb(PanelBackground.R, PanelBackground.G, PanelBackground.B));
        resources["WindowNavigationBrush"] = Brush(WindowNavigation);
        resources["WindowSurfaceBrush"] = Brush(Tile);
        resources["WindowSurfaceHoverBrush"] = Brush(TileHover);
        resources["WindowBorderBrush"] = Brush(TileBorder);
        resources["InputBackgroundBrush"] = Brush(Mix(WindowNavigation, Tile, 0.55));
        resources["PopupBackgroundBrush"] = Brush(Tile);
        resources["AccentSoftBrush"] = Brush(AccentSoft);
        resources["AccentHoverBrush"] = Brush(AccentHover);
        resources["AccentTextBrush"] = Brush(AccentText);
        resources["DangerBrush"] = Brush(Danger);
        resources["DangerHoverBrush"] = Brush(DangerHover);
        resources[SystemColors.HighlightBrushKey] = Brush(Accent);
        resources[SystemColors.HighlightTextBrushKey] = Brush(AccentText);
    }

    private static Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);

    private static double Luminance(Color color) =>
        (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;

    private static Color Mix(Color from, Color to, double amount) =>
        Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));

    private static bool SystemUsesLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 1;
        }
        catch
        {
            return false;
        }
    }
}
