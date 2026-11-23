// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

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

    // Category is kept as the primary category so existing settings.json files remain compatible.
    public string Category { get; set; } = "";

    /// <summary>Additional categories beyond the legacy primary Category field.</summary>
    public List<string> AdditionalCategories { get; set; } = new();

    [JsonIgnore]
    public IReadOnlyList<string> Categories
    {
        get
        {
            var values = new List<string>();
            AddCategoryValues(values, Category);
            if (AdditionalCategories is not null)
            {
                foreach (var category in AdditionalCategories)
                {
                    AddCategoryValues(values, category);
                }
            }

            return values;
        }
    }

    [JsonIgnore]
    public string CategoriesText => string.Join(", ", Categories);

    /// <summary>Replaces this item's categories, removing duplicates and empty values.</summary>
    public void SetCategories(IEnumerable<string>? categories)
    {
        var normalized = new List<string>();
        if (categories is not null)
        {
            foreach (var category in categories)
            {
                AddCategoryValues(normalized, category);
            }
        }

        Category = normalized.FirstOrDefault() ?? "";
        AdditionalCategories = normalized.Skip(1).ToList();
    }

    /// <summary>Normalizes old single-category and new multi-category settings after loading.</summary>
    public void NormalizeCategories()
    {
        var values = new List<string>();
        AddCategoryValues(values, Category);
        if (AdditionalCategories is not null)
        {
            foreach (var category in AdditionalCategories)
            {
                AddCategoryValues(values, category);
            }
        }

        SetCategories(values);
    }

    private static void AddCategoryValues(List<string> destination, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (var category in value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!destination.Contains(category, StringComparer.CurrentCultureIgnoreCase))
            {
                destination.Add(category);
            }
        }
    }

    public bool IsFavorite { get; set; }

    public PanelItemKind Kind { get; set; } = PanelItemKind.Application;

    public List<PanelItem> Children { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsFolder => Kind == PanelItemKind.Folder;

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayLabel =>
        $"{(IsFavorite ? "★ " : "")}{Name}{(Categories.Count == 0 ? "" : $"  [{CategoriesText}]")}{(IsFolder ? "  📁" : "")}";

    public PanelItem Clone() => CloneDeep();

    public PanelItem CloneDeep()
    {
        var clone = (PanelItem)MemberwiseClone();
        clone.AdditionalCategories = AdditionalCategories is null ? new List<string>() : new List<string>(AdditionalCategories);
        clone.Children = Children is null
            ? new List<PanelItem>()
            : Children.ConvertAll(child => child.CloneDeep());
        return clone;
    }
}
