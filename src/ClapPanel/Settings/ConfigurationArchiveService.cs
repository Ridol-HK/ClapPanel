// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
namespace ClapPanel.Settings;

/// <summary>
/// Creates portable configuration archives, imports full or shortcut-only
/// configurations, and manages rolling local backups for recovery.
/// </summary>
public sealed class ConfigurationArchiveService
{
    private const int ArchiveFormatVersion = 1;
    private const int MaximumBackupCount = 20;
    private const long MaximumEntryBytes = 64L * 1024 * 1024;
    private const long MaximumArchiveBytes = 512L * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _settingsPath;
    private readonly string _iconsDirectory;
    private readonly string _backupsDirectory;

    public ConfigurationArchiveService(string? rootDirectory = null)
    {
        var resolvedRoot = Path.GetFullPath(rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClapPanel"));
        _settingsPath = Path.Combine(resolvedRoot, "settings.json");
        _iconsDirectory = Path.Combine(resolvedRoot, "icons");
        _backupsDirectory = Path.Combine(resolvedRoot, "backups");
    }

    public string BackupsDirectory => _backupsDirectory;

    /// <summary>Returns true when a managed image referenced by a shortcut is missing or empty.</summary>
    public bool HasMissingManagedImages(AppSettings settings)
    {
        var managedRoot = Path.GetFullPath(_iconsDirectory) + Path.DirectorySeparatorChar;
        foreach (var item in FlattenItems(settings.Items))
        {
            if (string.IsNullOrWhiteSpace(item.ImagePath))
            {
                continue;
            }

            try
            {
                var imagePath = Path.GetFullPath(item.ImagePath);
                if (imagePath.StartsWith(managedRoot, StringComparison.OrdinalIgnoreCase)
                    && (!File.Exists(imagePath) || new FileInfo(imagePath).Length == 0))
                {
                    return true;
                }
            }
            catch
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Exports the current saved configuration and its image assets to a ZIP archive.</summary>
    public void Export(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        var settings = ReadCurrentSettings();
        CreateArchive(Path.GetFullPath(destinationPath), settings);
    }

    /// <summary>
    /// Creates a timestamped local backup. Returns null when there is no saved
    /// configuration or image directory to back up.
    /// </summary>
    public string? CreateBackup()
    {
        if (!File.Exists(_settingsPath) && !Directory.Exists(_iconsDirectory))
        {
            return null;
        }

        Directory.CreateDirectory(_backupsDirectory);
        var fileName = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.zip";
        var destination = Path.Combine(_backupsDirectory, fileName);
        CreateArchive(destination, ReadCurrentSettings());
        TrimOldBackups();
        return destination;
    }

    /// <summary>
    /// Imports a ZIP archive. When shortcutsOnly is true, only the shortcut tree
    /// is replaced; all other settings come from currentSettings unchanged.
    /// Image assets are copied into the local image store and paths are remapped.
    /// </summary>
    public AppSettings Import(string archivePath, AppSettings currentSettings, bool shortcutsOnly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentNullException.ThrowIfNull(currentSettings);

        var package = ReadArchive(Path.GetFullPath(archivePath));
        var importedSettings = package.Settings;
        SettingsService.Normalize(importedSettings);

        // Preserve a known-good copy before any imported file is written.
        CreateBackup();

        var result = shortcutsOnly ? CloneSettings(currentSettings) : importedSettings;
        if (shortcutsOnly)
        {
            result.Items = importedSettings.Items ?? new List<PanelItem>();
        }

        var assetMap = InstallAssets(package.Assets);
        RemapItemImages(result.Items, assetMap);
        SettingsService.Normalize(result);
        return result;
    }

    /// <summary>
    /// Loads the newest valid local backup and installs its image assets. The
    /// caller persists the returned settings through the normal save pipeline.
    /// Corrupt backup files are skipped in favor of an older valid copy.
    /// </summary>
    public bool TryRestoreLatestBackup(out AppSettings? restoredSettings)
    {
        restoredSettings = null;
        if (!Directory.Exists(_backupsDirectory))
        {
            return false;
        }

        foreach (var backupPath in Directory.GetFiles(_backupsDirectory, "backup_*.zip")
                     .OrderByDescending(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var package = ReadArchive(backupPath);
                var settings = package.Settings;
                SettingsService.Normalize(settings);
                var assetMap = InstallAssets(package.Assets);
                RemapItemImages(settings.Items, assetMap);
                SettingsService.Normalize(settings);
                restoredSettings = settings;
                return true;
            }
            catch
            {
                // A damaged backup must not prevent trying the next older one.
            }
        }

        return false;
    }

    private AppSettings ReadCurrentSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        var settings = DeserializeSettings(File.ReadAllBytes(_settingsPath));
        SettingsService.Normalize(settings);
        return settings;
    }

    private static AppSettings DeserializeSettings(byte[] jsonBytes)
    {
        using var document = JsonDocument.Parse(jsonBytes);
        var settings = JsonSerializer.Deserialize<AppSettings>(jsonBytes)
            ?? throw new InvalidDataException("The configuration does not contain a valid settings object.");

        // Treat pre-onboarding configurations as already configured, matching
        // the compatibility behavior of SettingsService.Load.
        if (document.RootElement.ValueKind == JsonValueKind.Object
            && !document.RootElement.TryGetProperty(nameof(AppSettings.FirstRunCompleted), out _))
        {
            settings.FirstRunCompleted = true;
        }

        return settings;
    }

    private void CreateArchive(string destinationPath, AppSettings sourceSettings)
    {
        var settings = CloneSettings(sourceSettings);
        var assets = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var sourceToArchive = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        long totalAssetBytes = 0;

        string? AddAsset(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(sourcePath);
            }
            catch
            {
                return null;
            }

            if (!File.Exists(fullPath))
            {
                return null;
            }

            if (sourceToArchive.TryGetValue(fullPath, out var existingEntry))
            {
                return existingEntry;
            }

            var extension = SafeExtension(fullPath);
            var archiveEntry = $"icons/{Guid.NewGuid():N}{extension}";
            var bytes = File.ReadAllBytes(fullPath);
            if (bytes.LongLength > MaximumEntryBytes)
            {
                throw new InvalidDataException($"Image asset is too large to archive: {Path.GetFileName(fullPath)}");
            }

            totalAssetBytes += bytes.LongLength;
            if (totalAssetBytes > MaximumArchiveBytes)
            {
                throw new InvalidDataException("The custom images exceed the maximum archive size.");
            }

            assets.Add(archiveEntry, bytes);
            sourceToArchive.Add(fullPath, archiveEntry);
            return archiveEntry;
        }

        foreach (var item in FlattenItems(settings.Items))
        {
            if (!string.IsNullOrWhiteSpace(item.ImagePath))
            {
                // Image paths in an archive are relative, never tied to the source PC.
                item.ImagePath = AddAsset(item.ImagePath);
            }
        }

        // Include all managed image files, including currently unused assets.
        if (Directory.Exists(_iconsDirectory))
        {
            foreach (var imagePath in Directory.EnumerateFiles(_iconsDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                AddAsset(imagePath);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var temporaryPath = destinationPath + ".tmp";
        try
        {
            using (var file = new FileStream(temporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: false))
            {
                WriteEntry(archive, "manifest.json", JsonSerializer.Serialize(new
                {
                    formatVersion = ArchiveFormatVersion,
                    application = "ClapPanel",
                    createdUtc = DateTimeOffset.UtcNow
                }, JsonOptions));
                WriteEntry(archive, "settings.json", JsonSerializer.Serialize(settings, JsonOptions));
                foreach (var asset in assets)
                {
                    var entry = archive.CreateEntry(asset.Key, CompressionLevel.Optimal);
                    using var stream = entry.Open();
                    stream.Write(asset.Value, 0, asset.Value.Length);
                }
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        catch
        {
            TryDelete(temporaryPath);
            throw;
        }
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    private static ArchivePackage ReadArchive(string archivePath)
    {
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException("The selected configuration archive does not exist.", archivePath);
        }

        using var file = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: false);
        var settingsEntry = archive.GetEntry("settings.json")
            ?? throw new InvalidDataException("The ZIP file does not contain settings.json.");
        if (settingsEntry.Length > 10L * 1024 * 1024)
        {
            throw new InvalidDataException("The settings file inside the archive is too large.");
        }

        AppSettings settings;
        using (var settingsStream = settingsEntry.Open())
        using (var memory = new MemoryStream())
        {
            settingsStream.CopyTo(memory);
            settings = DeserializeSettings(memory.ToArray());
        }

        var assets = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        long totalBytes = settingsEntry.Length;
        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith("icons/", StringComparison.OrdinalIgnoreCase)
                || entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                continue;
            }

            ValidateArchiveAssetPath(entry.FullName);
            if (entry.Length == 0 || entry.Length > MaximumEntryBytes)
            {
                throw new InvalidDataException($"An image asset is empty or too large: {entry.Name}");
            }

            totalBytes += entry.Length;
            if (totalBytes > MaximumArchiveBytes)
            {
                throw new InvalidDataException("The configuration archive exceeds the allowed size.");
            }

            using var input = entry.Open();
            using var output = new MemoryStream();
            input.CopyTo(output);
            assets[entry.FullName.Replace('\\', '/')] = output.ToArray();
        }

        return new ArchivePackage(settings, assets);
    }

    private Dictionary<string, string> InstallAssets(Dictionary<string, byte[]> assets)
    {
        Directory.CreateDirectory(_iconsDirectory);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var asset in assets)
        {
            var extension = SafeExtension(asset.Key);
            var destination = Path.Combine(_iconsDirectory, Guid.NewGuid().ToString("N") + extension);
            File.WriteAllBytes(destination, asset.Value);
            map[asset.Key.Replace('\\', '/')] = destination;
        }

        return map;
    }

    private static void RemapItemImages(IEnumerable<PanelItem> items, IReadOnlyDictionary<string, string> assetMap)
    {
        foreach (var item in FlattenItems(items))
        {
            if (string.IsNullOrWhiteSpace(item.ImagePath))
            {
                continue;
            }

            var archivePath = item.ImagePath.Replace('\\', '/').TrimStart('/');
            if (assetMap.TryGetValue(archivePath, out var localPath))
            {
                item.ImagePath = localPath;
                continue;
            }

            // Compatibility fallback for early or hand-created archives that used
            // an absolute source path but included the asset under its file name.
            var fileName = Path.GetFileName(archivePath);
            var match = assetMap.FirstOrDefault(pair =>
                string.Equals(Path.GetFileName(pair.Key), fileName, StringComparison.OrdinalIgnoreCase));
            item.ImagePath = string.IsNullOrEmpty(match.Value) ? null : match.Value;
        }
    }

    private static IEnumerable<PanelItem> FlattenItems(IEnumerable<PanelItem>? items)
    {
        if (items is null)
        {
            yield break;
        }

        foreach (var item in items)
        {
            if (item is null)
            {
                continue;
            }

            yield return item;
            foreach (var child in FlattenItems(item.Children))
            {
                yield return child;
            }
        }
    }

    private static AppSettings CloneSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    private static string SafeExtension(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 12)
        {
            return ".img";
        }

        return extension.All(character => char.IsLetterOrDigit(character) || character == '.')
            ? extension
            : ".img";
    }

    private static void ValidateArchiveAssetPath(string path)
    {
        var segments = path.Replace('\\', '/').Split('/');
        if (path.StartsWith('/') || path.Contains(':') || segments.Any(segment => segment is ".." or "."))
        {
            throw new InvalidDataException("The archive contains an unsafe image path.");
        }
    }

    private void TrimOldBackups()
    {
        var backups = Directory.GetFiles(_backupsDirectory, "backup_*.zip")
            .OrderByDescending(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var oldBackup in backups.Skip(MaximumBackupCount))
        {
            TryDelete(oldBackup);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Cleanup is best-effort; it must not invalidate a completed backup.
        }
    }

    private sealed record ArchivePackage(AppSettings Settings, Dictionary<string, byte[]> Assets);
}
