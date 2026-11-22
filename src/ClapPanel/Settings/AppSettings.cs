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

    /// <summary>Whether the panel can be moved by dragging its top bar.</summary>
    public bool AllowPanelMovement { get; set; } = true;

    /// <summary>Last custom horizontal position in WPF device-independent pixels (legacy fallback).</summary>
    public double? PanelLeft { get; set; }

    /// <summary>Last custom vertical position in WPF device-independent pixels (legacy fallback).</summary>
    public double? PanelTop { get; set; }

    /// <summary>Individual custom positions, indexed by the Windows monitor device name.</summary>
    public Dictionary<string, PanelPositionData> MonitorPositions { get; set; } = new();

    public List<PanelItem> Items { get; set; } = new();

    public List<AppearanceProfile> AppearanceProfiles { get; set; } = new();

    public DetectionOptions Detection { get; set; } = new();

    public AppearanceSettings Appearance { get; set; } = new();
}
