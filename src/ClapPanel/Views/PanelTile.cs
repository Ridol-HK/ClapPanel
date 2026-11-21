// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Windows.Media;
using ClapPanel.Settings;

namespace ClapPanel.Views;

public sealed class PanelTile
{
    public PanelTile(PanelItem item, ImageSource icon, bool missing)
    {
        Item = item;
        Icon = icon;
        Missing = missing;
    }

    public PanelItem Item { get; }

    public ImageSource Icon { get; }

    public bool Missing { get; }
}
