// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;

namespace ClapPanel.Settings;

public sealed class PanelItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    public string? ImagePath { get; set; }

    public string Target { get; set; } = "";

    public string Arguments { get; set; } = "";

    public PanelItem Clone() => (PanelItem)MemberwiseClone();
}
