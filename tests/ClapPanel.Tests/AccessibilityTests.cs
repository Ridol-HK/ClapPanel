// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Text.Json;
using System.Windows;
using ClapPanel.Localization;
using ClapPanel.Settings;
using ClapPanel.Theming;
using ClapPanel.Views;

namespace ClapPanel.Tests;

[Collection("Localization")]
public sealed class AccessibilityTests : IDisposable
{
    public AccessibilityTests() => Loc.SetLanguage(AppLanguage.Spanish);

    public void Dispose() => Loc.SetLanguage(AppLanguage.Spanish);

    [Fact]
    public void HighContrastUsesBlackWhiteAndYellow()
    {
        if (SystemParameters.HighContrast)
        {
            return;
        }

        var palette = AppearancePalette.From(new AppearanceSettings { HighContrast = true, Opacity = 0.5 });

        Assert.Equal(255, palette.PanelBackground.A);
        Assert.Equal(0, palette.PanelBackground.R);
        Assert.Equal(255, palette.Text.R);
        Assert.Equal(255, palette.Accent.R);
        Assert.Equal(255, palette.Accent.G);
        Assert.Equal(0, palette.Accent.B);
    }

    [Fact]
    public void HighContrastFlagIsDetected()
    {
        Assert.True(AppearancePalette.UsesHighContrast(new AppearanceSettings { HighContrast = true }));
    }

    [Fact]
    public void ShortcutNumbersAreShownByDefault()
    {
        var appearance = new AppearanceSettings();

        Assert.True(appearance.ShowShortcutNumbers);
        Assert.False(appearance.HighContrast);
    }

    [Fact]
    public void TileNameDescribesItemForScreenReaders()
    {
        var item = new PanelItem { Name = "Notepad", Category = "Work", IsFavorite = true };
        var tile = new PanelTile(item, null!, false) { ShortcutNumber = "1" };

        Assert.Equal("Notepad, favorito, categoría Work, atajo 1", tile.AutomationName);
    }

    [Fact]
    public void TileNameIsTranslated()
    {
        Loc.SetLanguage(AppLanguage.English);
        var item = new PanelItem { Name = "Notepad", IsFavorite = true };
        var tile = new PanelTile(item, null!, true);

        Assert.Equal("Notepad, favorite, unavailable", tile.AutomationName);
    }

    [Fact]
    public void FolderNameIncludesItsSize()
    {
        var folder = new PanelItem { Name = "Juegos", Kind = PanelItemKind.Folder, Children = new List<PanelItem> { new() { Name = "A" }, new() { Name = "B" } } };
        var tile = new PanelTile(folder, null!, false);

        Assert.Equal("Juegos, carpeta con 2 elementos", tile.AutomationName);
    }
}

public class NewSettingsTests
{
    [Fact]
    public void LanguageAndWidgetsHaveSafeDefaults()
    {
        var settings = JsonSerializer.Deserialize<AppSettings>("{}")!;

        Assert.Equal(AppLanguage.System, settings.Language);
        Assert.NotNull(settings.Widgets);
        Assert.False(settings.Widgets.AnyEnabled);
        Assert.True(settings.Widgets.ClockUse24Hours);
    }

    [Fact]
    public void LanguageIsStoredAsText()
    {
        var json = JsonSerializer.Serialize(new AppSettings { Language = AppLanguage.English });

        Assert.Contains("\"Language\":\"English\"", json);
    }

    [Fact]
    public void WidgetSettingsRoundTrip()
    {
        const string json = "{\"Language\":\"Spanish\",\"Widgets\":{\"ShowClock\":true,\"ShowNotes\":true,\"Notes\":\"hola\",\"ClipboardMaxItems\":12}}";

        var settings = JsonSerializer.Deserialize<AppSettings>(json)!;

        Assert.Equal(AppLanguage.Spanish, settings.Language);
        Assert.True(settings.Widgets.ShowClock);
        Assert.True(settings.Widgets.ShowNotes);
        Assert.Equal("hola", settings.Widgets.Notes);
        Assert.Equal(12, settings.Widgets.ClipboardMaxItems);
        Assert.True(settings.Widgets.AnyEnabled);
    }
}
