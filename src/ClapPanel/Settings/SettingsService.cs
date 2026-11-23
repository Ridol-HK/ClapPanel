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

    private readonly string _path;

    public SettingsService(string? directory = null)
    {
        var rootDirectory = Path.GetFullPath(directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClapPanel"));
        _path = Path.Combine(rootDirectory, "settings.json");
        Archives = new ConfigurationArchiveService(rootDirectory);
    }

    public ConfigurationArchiveService Archives { get; }

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
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("The settings file does not contain a JSON object.");
            }
            var settings = JsonSerializer.Deserialize<AppSettings>(json)
                ?? throw new InvalidDataException("The settings file does not contain a valid configuration.");

            // Existing installations predate the first-run assistant. Treat a
            // legacy settings file without this property as already configured,
            // so upgrading users are not interrupted by the onboarding flow.
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && !document.RootElement.TryGetProperty(nameof(AppSettings.FirstRunCompleted), out _))
            {
                settings.FirstRunCompleted = true;
            }

            Normalize(settings);
            if (Archives.HasMissingManagedImages(settings)
                && Archives.TryRestoreLatestBackup(out var recoveredImages) && recoveredImages is not null)
            {
                try
                {
                    WriteSettingsAtomically(recoveredImages);
                    return recoveredImages;
                }
                catch
                {
                    return recoveredImages;
                }
            }

            return settings;
        }
        catch
        {
            // A malformed settings.json should not silently discard the user's
            // configuration. Recover the newest valid local archive first.
            if (Archives.TryRestoreLatestBackup(out var recovered) && recovered is not null)
            {
                try
                {
                    WriteSettingsAtomically(recovered);
                    return recovered;
                }
                catch
                {
                    QuarantineDamagedFile();
                    // The recovered object can still be used for this session even
                    // when the damaged disk cannot be overwritten.
                    return recovered;
                }
            }

            // Do not let a broken file block the user's next attempt to save a
            // fresh configuration when no valid backup exists.
            QuarantineDamagedFile();
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings, bool createBackup = true)
    {
        Normalize(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        // User-initiated configuration changes keep a recoverable snapshot.
        // High-frequency state persistence (window position, size, category
        // selection, and note autosave) can opt out to avoid filling the backup
        // rotation with near-identical archives.
        var hadPreviousSettings = File.Exists(_path);
        if (hadPreviousSettings && createBackup)
        {
            Archives.CreateBackup();
        }

        WriteSettingsAtomically(settings);
        if (!hadPreviousSettings)
        {
            // Keep a baseline snapshot after the first successful save so even
            // a newly configured installation has a recovery point.
            Archives.CreateBackup();
        }
    }

    private void WriteSettingsAtomically(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = _path + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, _path, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
            }
            throw;
        }
    }

    private void QuarantineDamagedFile()
    {
        try
        {
            if (File.Exists(_path))
            {
                var quarantinePath = _path + ".corrupt_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                File.Move(_path, quarantinePath, overwrite: true);
            }
        }
        catch
        {
            // If the profile directory is read-only, continue with in-memory defaults.
        }
    }

    internal static void Normalize(AppSettings settings)
    {
        settings.Items ??= new List<PanelItem>();
        settings.Items.RemoveAll(item => item is null);
        settings.AppearanceProfiles ??= new List<AppearanceProfile>();
        settings.AppearanceProfiles.RemoveAll(profile => profile is null);
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
            if (item is not null)
            {
                NormalizeItem(item);
            }
        }

        foreach (var profile in settings.AppearanceProfiles)
        {
            if (profile is null)
            {
                continue;
            }

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
        item.Children.RemoveAll(child => child is null);
        foreach (var child in item.Children)
        {
            if (child is not null)
            {
                NormalizeItem(child);
            }
        }
    }
}
