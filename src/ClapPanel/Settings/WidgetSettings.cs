// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Text.Json.Serialization;

namespace ClapPanel.Settings;

public sealed class WidgetSettings
{
    public bool ShowClock { get; set; }

    public bool ClockUse24Hours { get; set; } = true;

    public bool ClockShowSeconds { get; set; }

    public bool ClockShowDate { get; set; } = true;

    public bool ShowNotes { get; set; }

    public string Notes { get; set; } = "";

    public bool ShowClipboard { get; set; }

    public int ClipboardMaxItems { get; set; } = 8;

    public bool ShowMedia { get; set; }

    [JsonIgnore]
    public bool AnyEnabled => ShowClock || ShowNotes || ShowClipboard || ShowMedia;
}
