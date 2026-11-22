// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Windows;
using ClapPanel.Settings;

namespace ClapPanel.Theming;

/// <summary>
/// Calculates a panel position inside the usable desktop area.
/// </summary>
public static class PanelPlacement
{
    public static Point Calculate(Rect workArea, Size panelSize, PanelPosition position, double margin)
    {
        var minLeft = workArea.Left + margin;
        var minTop = workArea.Top + margin;
        var maxLeft = Math.Max(minLeft, workArea.Right - panelSize.Width - margin);
        var maxTop = Math.Max(minTop, workArea.Bottom - panelSize.Height - margin);

        var desiredLeft = position switch
        {
            PanelPosition.BottomLeft or PanelPosition.TopLeft => minLeft,
            PanelPosition.Center => workArea.Left + (workArea.Width - panelSize.Width) / 2,
            PanelPosition.BottomRight or PanelPosition.TopRight => workArea.Right - panelSize.Width - margin,
            _ => minLeft
        };

        var desiredTop = position switch
        {
            PanelPosition.TopLeft or PanelPosition.TopRight => minTop,
            PanelPosition.Center => workArea.Top + (workArea.Height - panelSize.Height) / 2,
            PanelPosition.BottomLeft or PanelPosition.BottomRight => workArea.Bottom - panelSize.Height - margin,
            _ => maxTop
        };

        return new Point(
            Math.Clamp(desiredLeft, minLeft, maxLeft),
            Math.Clamp(desiredTop, minTop, maxTop));
    }
    /// <summary>
    /// Returns Left or Right only when the panel reaches the central docking zone
    /// of that screen edge. Requiring the panel's vertical center to be in this
    /// zone prevents accidental docking when it is moved to a corner.
    /// </summary>
    public static PanelDockEdge DetectHorizontalDockEdge(Rect workArea, Rect panelBounds, double snapDistance)
    {
        if (workArea.IsEmpty || panelBounds.IsEmpty || workArea.Width <= 0 || workArea.Height <= 0)
        {
            return PanelDockEdge.None;
        }

        var threshold = Math.Max(0, snapDistance);
        // The edge is split into 35% (top), 30% (docking zone), and 35% (bottom).
        const double centerDockBandRatio = 0.30;
        var centerBandMargin = workArea.Height * (1 - centerDockBandRatio) / 2;
        var centerBandTop = workArea.Top + centerBandMargin;
        var centerBandBottom = workArea.Bottom - centerBandMargin;
        var panelCenterY = panelBounds.Top + panelBounds.Height / 2;

        // Only the middle 30% of the left/right edges is a docking target.
        if (panelCenterY < centerBandTop || panelCenterY > centerBandBottom)
        {
            return PanelDockEdge.None;
        }

        if (panelBounds.Left <= workArea.Left + threshold)
        {
            return PanelDockEdge.Left;
        }

        if (panelBounds.Right >= workArea.Right - threshold)
        {
            return PanelDockEdge.Right;
        }

        return PanelDockEdge.None;
    }

}
