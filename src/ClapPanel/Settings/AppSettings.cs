// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Collections.Generic;
using ClapPanel.Detection;

namespace ClapPanel.Settings;

public sealed class AppSettings
{
    public string? MicrophoneId { get; set; }

    public int Sensitivity { get; set; } = 60;

    public bool StartWithWindows { get; set; }

    public int Columns { get; set; } = 4;

    public List<PanelItem> Items { get; set; } = new();

    public DetectionOptions Detection { get; set; } = new();
}
