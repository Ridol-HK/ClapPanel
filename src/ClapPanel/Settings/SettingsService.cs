// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClapPanel.Detection;

namespace ClapPanel.Settings;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClapPanel",
        "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_path);
            using var document = JsonDocument.Parse(json);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

            // Existing installations predate the first-run assistant. Treat a
            // legacy settings file without this property as already configured,
            // so upgrading users are not interrupted by the onboarding flow.
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && !document.RootElement.TryGetProperty(nameof(AppSettings.FirstRunCompleted), out _))
            {
                settings.FirstRunCompleted = true;
            }

            Normalize(settings);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Normalize(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static void Normalize(AppSettings settings)
    {
        settings.Items ??= new List<PanelItem>();
        settings.AppearanceProfiles ??= new List<AppearanceProfile>();
        settings.MonitorPositions ??= new Dictionary<string, PanelPositionData>(StringComparer.OrdinalIgnoreCase);
        settings.Appearance ??= new AppearanceSettings();
        settings.Detection ??= new DetectionOptions();
        settings.Widgets ??= new WidgetSettings();
        settings.GlobalShortcut ??= new GlobalShortcutSettings();
        settings.Appearance.SmartDockVisibleItems = Math.Clamp(settings.Appearance.SmartDockVisibleItems, 1, 12);
        settings.Appearance.SmartDockRevealThresholdPixels = Math.Clamp(settings.Appearance.SmartDockRevealThresholdPixels, 4, 40);
        settings.GlobalShortcut.Modifiers &= 0x000F;
        if (settings.GlobalShortcut.VirtualKeyCode <= 0 || settings.GlobalShortcut.VirtualKeyCode > 0xFE)
        {
            settings.GlobalShortcut.VirtualKeyCode = 0x20;
        }
        settings.LastSelectedCategory ??= "";
        foreach (var item in settings.Items)
        {
            NormalizeItem(item);
        }

        foreach (var profile in settings.AppearanceProfiles)
        {
            profile.Appearance ??= new AppearanceSettings();
            profile.Appearance.SmartDockVisibleItems = Math.Clamp(profile.Appearance.SmartDockVisibleItems, 1, 12);
            profile.Appearance.SmartDockRevealThresholdPixels = Math.Clamp(profile.Appearance.SmartDockRevealThresholdPixels, 4, 40);
        }
    }

    private static void NormalizeItem(PanelItem item)
    {
        item.Name ??= "";
        item.Target ??= "";
        item.Arguments ??= "";
        item.Category ??= "";
        item.AdditionalCategories ??= new List<string>();
        item.NormalizeCategories();
        item.Children ??= new List<PanelItem>();
        foreach (var child in item.Children)
        {
            NormalizeItem(child);
        }
    }
}
