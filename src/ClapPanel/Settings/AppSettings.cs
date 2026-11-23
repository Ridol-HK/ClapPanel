// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Collections.Generic;
using ClapPanel.Detection;
using ClapPanel.Localization;

namespace ClapPanel.Settings;

public sealed class AppSettings
{
    public string? MicrophoneId { get; set; }

    /// <summary>Whether the first-run setup assistant has been completed or skipped.</summary>
    public bool FirstRunCompleted { get; set; }

    public GlobalShortcutSettings GlobalShortcut { get; set; } = new();

    public int Sensitivity { get; set; } = 60;

    public bool StartWithWindows { get; set; }

    public int Columns { get; set; } = 4;

    /// <summary>Whether the panel can be moved by dragging its top bar.</summary>
    public bool AllowPanelMovement { get; set; } = true;

    /// <summary>Whether the last selected category filter is restored when the panel opens.</summary>
    public bool RememberCategorySelection { get; set; } = true;

    /// <summary>The last selected category filter; an empty value means all categories.</summary>
    public string LastSelectedCategory { get; set; } = "";

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

    public AppLanguage Language { get; set; } = AppLanguage.System;

    public WidgetSettings Widgets { get; set; } = new();
}
