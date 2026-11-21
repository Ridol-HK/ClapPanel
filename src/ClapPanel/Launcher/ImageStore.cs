// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClapPanel.Launcher;

public sealed class ImageStore
{
    private readonly string _directory;

    public ImageStore(string? directory = null)
    {
        _directory = Path.GetFullPath(directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClapPanel",
            "icons"));
    }

    public string Import(string sourcePath)
    {
        Directory.CreateDirectory(_directory);

        var source = Path.GetFullPath(sourcePath);
        if (string.Equals(Path.GetDirectoryName(source), _directory, StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        var destination = Path.Combine(_directory, Guid.NewGuid().ToString("N") + Path.GetExtension(source));
        File.Copy(source, destination);
        return destination;
    }

    public void Cleanup(IEnumerable<string?> inUse)
    {
        if (!Directory.Exists(_directory))
        {
            return;
        }

        var keep = new HashSet<string>(
            inUse.Where(path => !string.IsNullOrEmpty(path)).Select(path => Path.GetFullPath(path!)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(_directory))
        {
            if (keep.Contains(file))
            {
                continue;
            }

            try
            {
                File.Delete(file);
            }
            catch
            {
            }
        }
    }
}
