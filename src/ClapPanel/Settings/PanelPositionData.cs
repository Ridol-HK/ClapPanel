// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

namespace ClapPanel.Settings;

public sealed class PanelPositionData
{
    public double Left { get; set; }

    public double Top { get; set; }

    /// <summary>Saved size while the panel uses its regular horizontal layout.</summary>
    public double? Width { get; set; }

    public double? Height { get; set; }

    /// <summary>Saved size while the panel is docked vertically to a left/right edge.</summary>
    public double? DockedWidth { get; set; }

    public double? DockedHeight { get; set; }

    public PanelPositionData Clone() => (PanelPositionData)MemberwiseClone();
}
