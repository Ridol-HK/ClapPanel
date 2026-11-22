// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Diagnostics;
using System.IO;
using ClapPanel.Settings;

namespace ClapPanel.Launcher;

public sealed class AppLauncher
{
    public bool Exists(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        var path = Environment.ExpandEnvironmentVariables(target.Trim());
        if (File.Exists(path) || Directory.Exists(path))
        {
            return true;
        }

        return Uri.TryCreate(path, UriKind.Absolute, out var uri) && !uri.IsFile && uri.Scheme.Length > 1;
    }

    public bool Launch(PanelItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Target))
        {
            return false;
        }

        try
        {
            var path = Environment.ExpandEnvironmentVariables(item.Target.Trim());
            ProcessStartInfo info;
            if (Directory.Exists(path))
            {
                info = new ProcessStartInfo("explorer.exe")
                {
                    UseShellExecute = true,
                    Arguments = $"\"{path}\""
                };
            }
            else
            {
                info = new ProcessStartInfo(path) { UseShellExecute = true };
                if (!string.IsNullOrWhiteSpace(item.Arguments))
                {
                    info.Arguments = item.Arguments;
                }

                if (File.Exists(path))
                {
                    info.WorkingDirectory = Path.GetDirectoryName(path) ?? "";
                }
            }

            Process.Start(info)?.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
