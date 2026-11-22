// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

namespace ClapPanel.Settings;

public sealed class AppearanceProfile
{
    public string Name { get; set; } = "Nuevo perfil";

    public int Columns { get; set; } = 4;

    public AppearanceSettings Appearance { get; set; } = new();

    public AppearanceProfile Clone() => new()
    {
        Name = Name,
        Columns = Columns,
        Appearance = Appearance.Clone()
    };
}
