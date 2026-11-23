// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Text.Json.Serialization;

namespace ClapPanel.Settings;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThemeMode
{
    System,
    Light,
    Dark
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PanelPosition
{
    BottomRight,
    BottomLeft,
    TopRight,
    TopLeft,
    Center
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PanelDockEdge
{
    None,
    Left,
    Right,
    Top,
    Bottom
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PanelAnimationStyle
{
    None,
    Fade,
    Scale,
    Slide,
    Drop,
    Pop,
    Bounce
}

public sealed class AppearanceSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    public string BackgroundColor { get; set; } = "";

    public string AccentColor { get; set; } = "";

    public double Opacity { get; set; } = 1.0;

    public int IconSize { get; set; } = 40;

    public bool ShowNames { get; set; } = true;

    public bool RoundedCorners { get; set; } = true;

    /// <summary>Expands the panel to cover the selected monitor. Disabled by default.</summary>
    public bool FullScreen { get; set; }

    public PanelPosition Position { get; set; } = PanelPosition.BottomRight;

    // Kept for compatibility with settings.json files created by older versions.
    public bool Animation { get; set; } = true;

    public PanelAnimationStyle AnimationStyle { get; set; } = PanelAnimationStyle.Scale;

    public bool AutoHide { get; set; }

    public int AutoHideDelaySeconds { get; set; } = 5;

    /// <summary>Maximum number of shortcuts shown by the compact smart dock (1–12).</summary>
    public int SmartDockVisibleItems { get; set; } = 6;

    /// <summary>Shows favorite shortcuts before regular shortcuts in the smart dock.</summary>
    public bool SmartDockPrioritizeFavorites { get; set; }

    /// <summary>Width of the screen-edge activation strip used to reveal an auto-hidden panel (4–40 px).</summary>
    public int SmartDockRevealThresholdPixels { get; set; } = 12;

    public PanelDockEdge DockEdge { get; set; } = PanelDockEdge.None;

    public bool DynamicTransparency { get; set; }

    public double InactiveOpacity { get; set; } = 0.65;

    /// <summary>Seconds to wait after the pointer leaves before applying inactive opacity.</summary>
    public double InactiveOpacityDelaySeconds { get; set; } = 0;

    /// <summary>Whether inactive-opacity changes use a short, smooth fade instead of an immediate switch.</summary>
    public bool AnimateInactiveOpacity { get; set; } = true;

    public bool HighContrast { get; set; }

    public bool ShowShortcutNumbers { get; set; } = true;

    /// <summary>WinForms screen device name. Empty means the primary monitor.</summary>
    public string MonitorDeviceName { get; set; } = "";

    public AppearanceSettings Clone() => (AppearanceSettings)MemberwiseClone();
}
