// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClapPanel.Launcher;

public static class InstalledAppsScanner
{
    private static readonly string[] AuxiliaryMarkers = { "uninstall", "desinstal" };

    public static Task<IReadOnlyList<InstalledApp>> ScanAsync() => Task.Run(Scan);

    public static IReadOnlyList<InstalledApp> Scan()
    {
        var shortcuts = new List<InstalledApp>();
        foreach (var folder in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
            Environment.GetFolderPath(Environment.SpecialFolder.Programs)
        })
        {
            shortcuts.AddRange(ScanShortcuts(folder));
        }

        return Merge(shortcuts, ReadStoreApps());
    }

    public static IReadOnlyList<InstalledApp> Merge(IEnumerable<InstalledApp> shortcuts, IEnumerable<InstalledApp> storeApps)
    {
        var result = new Dictionary<string, InstalledApp>(StringComparer.CurrentCultureIgnoreCase);
        foreach (var app in shortcuts.Concat(storeApps))
        {
            if (!string.IsNullOrWhiteSpace(app.Name) && !IsAuxiliary(app.Name))
            {
                result.TryAdd(app.Name.Trim(), app with { Name = app.Name.Trim() });
            }
        }

        return result.Values
            .OrderBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static bool IsAuxiliary(string name) =>
        AuxiliaryMarkers.Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<InstalledApp> ParseStartApps(string? json)
    {
        var apps = new List<InstalledApp>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return apps;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    AddStoreApp(apps, element);
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                AddStoreApp(apps, root);
            }
        }
        catch (JsonException)
        {
        }

        return apps;
    }

    private static void AddStoreApp(List<InstalledApp> apps, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty("Name", out var name)
            || !element.TryGetProperty("AppID", out var id)
            || name.ValueKind != JsonValueKind.String
            || id.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var appName = name.GetString();
        var appId = id.GetString();
        if (!string.IsNullOrWhiteSpace(appName) && !string.IsNullOrWhiteSpace(appId) && appId.Contains('!'))
        {
            apps.Add(new InstalledApp(appName, @"shell:AppsFolder\" + appId));
        }
    }

    private static IEnumerable<InstalledApp> ScanShortcuts(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return Array.Empty<InstalledApp>();
        }

        try
        {
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
            return Directory.EnumerateFiles(folder, "*.lnk", options)
                .Select(path => new InstalledApp(Path.GetFileNameWithoutExtension(path), path))
                .ToList();
        }
        catch
        {
            return Array.Empty<InstalledApp>();
        }
    }

    private static IReadOnlyList<InstalledApp> ReadStoreApps()
    {
        try
        {
            var info = new ProcessStartInfo("powershell.exe")
            {
                Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"[Console]::OutputEncoding=[Text.Encoding]::UTF8; Get-StartApps | ConvertTo-Json -Compress\"",
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(info);
            if (process is null)
            {
                return Array.Empty<InstalledApp>();
            }

            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(10000))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }

                return Array.Empty<InstalledApp>();
            }

            return ParseStartApps(output.GetAwaiter().GetResult());
        }
        catch
        {
            return Array.Empty<InstalledApp>();
        }
    }
}
