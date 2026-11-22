// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;

namespace ClapPanel.Settings;

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum PanelItemKind
{
    Application,
    Folder
}

public sealed class PanelItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    public string? ImagePath { get; set; }

    public string Target { get; set; } = "";

    public string Arguments { get; set; } = "";

    public string Category { get; set; } = "";

    public bool IsFavorite { get; set; }

    public PanelItemKind Kind { get; set; } = PanelItemKind.Application;

    public List<PanelItem> Children { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsFolder => Kind == PanelItemKind.Folder;

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayLabel =>
        $"{(IsFavorite ? "★ " : "")}{Name}{(string.IsNullOrWhiteSpace(Category) ? "" : $"  [{Category}]")}{(IsFolder ? "  📁" : "")}";

    public PanelItem Clone() => CloneDeep();

    public PanelItem CloneDeep()
    {
        var clone = (PanelItem)MemberwiseClone();
        clone.Children = Children is null
            ? new List<PanelItem>()
            : Children.ConvertAll(child => child.CloneDeep());
        return clone;
    }
}
