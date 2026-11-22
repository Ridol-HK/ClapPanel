// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using ClapPanel.Settings;
using ClapPanel.Theming;

namespace ClapPanel.Tests;

public class AppearanceTests
{
    [Theory]
    [InlineData("#FF8800", true)]
    [InlineData("FF8800", true)]
    [InlineData("#80FF8800", true)]
    [InlineData("#12345", false)]
    [InlineData("zzzzzz", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ParsesHexColors(string? text, bool expected) =>
        Assert.Equal(expected, AppearancePalette.TryParseColor(text, out _));

    [Fact]
    public void ParsedColorHasExpectedChannels()
    {
        Assert.True(AppearancePalette.TryParseColor("#80FF8800", out var color));

        Assert.Equal(255, color.R);
        Assert.Equal(136, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void DarkThemeUsesLightText()
    {
        var palette = AppearancePalette.From(new AppearanceSettings { Theme = ThemeMode.Dark });

        Assert.True(palette.Text.R > 200);
        Assert.True(palette.PanelBackground.R < 60);
    }

    [Fact]
    public void LightThemeUsesDarkText()
    {
        var palette = AppearancePalette.From(new AppearanceSettings { Theme = ThemeMode.Light });

        Assert.True(palette.Text.R < 60);
        Assert.True(palette.PanelBackground.R > 200);
    }

    [Fact]
    public void CustomLightBackgroundUsesDarkText()
    {
        var palette = AppearancePalette.From(new AppearanceSettings { Theme = ThemeMode.Dark, BackgroundColor = "#FFFFFF" });

        Assert.True(palette.Text.R < 100);
    }

    [Fact]
    public void CustomDarkBackgroundUsesLightText()
    {
        var palette = AppearancePalette.From(new AppearanceSettings { Theme = ThemeMode.Light, BackgroundColor = "#101820" });

        Assert.True(palette.Text.R > 200);
    }

    [Fact]
    public void CustomAccentIsApplied()
    {
        var palette = AppearancePalette.From(new AppearanceSettings { Theme = ThemeMode.Dark, AccentColor = "#E81123" });

        Assert.Equal(0xE8, palette.Accent.R);
        Assert.Equal(0x11, palette.Accent.G);
        Assert.Equal(0x23, palette.Accent.B);
    }

    [Fact]
    public void ThemePaletteIncludesNavigationAndAccentVariants()
    {
        var palette = AppearancePalette.From(new AppearanceSettings
        {
            Theme = ThemeMode.Dark,
            AccentColor = "#E81123"
        });

        Assert.NotEqual(default, palette.WindowNavigation);
        Assert.NotEqual(default, palette.AccentSoft);
        Assert.Equal(0xE8, palette.Accent.R);
        Assert.Equal(0xD1, palette.Danger.R);
    }

    [Fact]
    public void AdditionalAnimationStylesAreAvailable()
    {
        Assert.Contains(PanelAnimationStyle.Drop, Enum.GetValues<PanelAnimationStyle>());
        Assert.Contains(PanelAnimationStyle.Pop, Enum.GetValues<PanelAnimationStyle>());
        Assert.Contains(PanelAnimationStyle.Bounce, Enum.GetValues<PanelAnimationStyle>());
    }

    [Fact]
    public void OpacityControlsBackgroundAlpha()
    {
        var half = AppearancePalette.From(new AppearanceSettings { Theme = ThemeMode.Dark, Opacity = 0.5 });
        var full = AppearancePalette.From(new AppearanceSettings { Theme = ThemeMode.Dark, Opacity = 1.0 });

        Assert.InRange(half.PanelBackground.A, 127, 128);
        Assert.Equal(255, full.PanelBackground.A);
    }

    [Fact]
    public void TileSizeFollowsIconSizeAndNames()
    {
        Assert.Equal(96, AppearanceLayout.TileSize(new AppearanceSettings { IconSize = 40, ShowNames = true }));
        Assert.Equal(80, AppearanceLayout.TileSize(new AppearanceSettings { IconSize = 24, ShowNames = true }));
        Assert.Equal(68, AppearanceLayout.TileSize(new AppearanceSettings { IconSize = 40, ShowNames = false }));
    }

    [Fact]
    public void ValuesAreClamped()
    {
        Assert.Equal(96, AppearanceLayout.IconSize(new AppearanceSettings { IconSize = 500 }));
        Assert.Equal(24, AppearanceLayout.IconSize(new AppearanceSettings { IconSize = 1 }));
        Assert.Equal(0.4, AppearanceLayout.Opacity(new AppearanceSettings { Opacity = 0.1 }));
    }

    [Theory]
    [InlineData(PanelPosition.BottomRight, 584, 484)]
    [InlineData(PanelPosition.BottomLeft, 116, 484)]
    [InlineData(PanelPosition.TopRight, 584, 116)]
    [InlineData(PanelPosition.TopLeft, 116, 116)]
    [InlineData(PanelPosition.Center, 350, 300)]
    public void PanelPositionUsesSelectedAnchor(PanelPosition position, double expectedLeft, double expectedTop)
    {
        var actual = PanelPlacement.Calculate(
            new System.Windows.Rect(100, 100, 700, 500),
            new System.Windows.Size(200, 100),
            position,
            16);

        Assert.Equal(expectedLeft, actual.X);
        Assert.Equal(expectedTop, actual.Y);
    }

    [Theory]
    // Hitting the middle docking zone of the left/right edge docks the panel.
    [InlineData(4, 270, 220, 260, PanelDockEdge.Left)]
    [InlineData(760, 270, 240, 260, PanelDockEdge.Right)]
    // Reaching an edge near the top or bottom must not trigger docking.
    [InlineData(4, 10, 220, 180, PanelDockEdge.None)]
    [InlineData(760, 610, 240, 180, PanelDockEdge.None)]
    // Being away from the screen edge never docks, even in the central zone.
    [InlineData(420, 270, 240, 260, PanelDockEdge.None)]
    public void DetectsHorizontalDockEdgesOnlyInTheCenterOfEachSide(
        double left, double top, double width, double height, PanelDockEdge expected)
    {
        var area = new System.Windows.Rect(0, 0, 1000, 800);
        var panel = new System.Windows.Rect(left, top, width, height);

        Assert.Equal(expected, PanelPlacement.DetectHorizontalDockEdge(area, panel, 30));
    }

    [Theory]
    // For an 800 px work area, the 30% central docking band spans Y=280..520.
    [InlineData(279, PanelDockEdge.None)]
    [InlineData(280, PanelDockEdge.Left)]
    [InlineData(520, PanelDockEdge.Left)]
    [InlineData(521, PanelDockEdge.None)]
    public void DockingBandUsesThirtyPercentCenterWithThirtyFivePercentMargins(
        double panelTop, PanelDockEdge expected)
    {
        var area = new System.Windows.Rect(0, 0, 1000, 800);
        var panel = new System.Windows.Rect(0, panelTop - 50, 220, 100);

        Assert.Equal(expected, PanelPlacement.DetectHorizontalDockEdge(area, panel, 30));
    }

    [Fact]
    public void PanelPositionIsClampedInsideWorkArea()
    {
        var actual = PanelPlacement.Calculate(
            new System.Windows.Rect(0, 0, 500, 300),
            new System.Windows.Size(900, 600),
            PanelPosition.BottomRight,
            16);

        Assert.Equal(16, actual.X);
        Assert.Equal(16, actual.Y);
    }
}
