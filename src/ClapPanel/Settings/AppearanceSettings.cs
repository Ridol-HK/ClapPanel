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

    public PanelPosition Position { get; set; } = PanelPosition.BottomRight;

    // Kept for compatibility with settings.json files created by older versions.
    public bool Animation { get; set; } = true;

    public PanelAnimationStyle AnimationStyle { get; set; } = PanelAnimationStyle.Scale;

    public bool AutoHide { get; set; }

    public int AutoHideDelaySeconds { get; set; } = 5;

    public PanelDockEdge DockEdge { get; set; } = PanelDockEdge.None;

    public bool DynamicTransparency { get; set; }

    public double InactiveOpacity { get; set; } = 0.65;

    /// <summary>WinForms screen device name. Empty means the primary monitor.</summary>
    public string MonitorDeviceName { get; set; } = "";

    public AppearanceSettings Clone() => (AppearanceSettings)MemberwiseClone();
}
