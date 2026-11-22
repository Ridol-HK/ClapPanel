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

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
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
        foreach (var item in settings.Items)
        {
            NormalizeItem(item);
        }

        foreach (var profile in settings.AppearanceProfiles)
        {
            profile.Appearance ??= new AppearanceSettings();
        }
    }

    private static void NormalizeItem(PanelItem item)
    {
        item.Name ??= "";
        item.Target ??= "";
        item.Arguments ??= "";
        item.Category ??= "";
        item.Children ??= new List<PanelItem>();
        foreach (var child in item.Children)
        {
            NormalizeItem(child);
        }
    }
}
