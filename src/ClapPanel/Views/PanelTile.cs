// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ClapPanel.Settings;

namespace ClapPanel.Views;

public sealed class PanelTile
{
    public PanelTile(
        PanelItem item,
        ImageSource icon,
        bool missing,
        IReadOnlyList<PanelItem>? parentFolders = null)
    {
        Item = item;
        Icon = icon;
        Missing = missing;
        ParentFolders = parentFolders ?? new List<PanelItem>();
    }

    public PanelItem Item { get; }

    public ImageSource Icon { get; }

    public bool Missing { get; }

    /// <summary>Folders from the panel root to the folder that contains this tile.</summary>
    public IReadOnlyList<PanelItem> ParentFolders { get; }

    public bool IsFavorite => Item.IsFavorite;

    public bool IsFolder => Item.IsFolder;

    public string ToolTipText
    {
        get
        {
            var location = ParentFolders.Count == 0
                ? Item.Name
                : $"{string.Join(" / ", ParentFolders.Select(folder => folder.Name))} / {Item.Name}";
            return Item.IsFolder
                ? $"{location}\nCarpeta · {Item.Children?.Count ?? 0} elementos"
                : $"{location}\n{Item.Target}";
        }
    }
}
