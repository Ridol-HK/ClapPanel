// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using ClapPanel.Settings;

namespace ClapPanel.Tests;

public sealed class PanelItemTests
{
    [Fact]
    public void CloneDeep_CopiesNestedFolderContentsWithoutSharingLists()
    {
        var app = new PanelItem { Name = "Editor", Target = @"C:\Apps\editor.exe" };
        var nestedFolder = new PanelItem
        {
            Name = "Desarrollo",
            Kind = PanelItemKind.Folder,
            Children = new List<PanelItem> { app }
        };
        var root = new PanelItem
        {
            Name = "Trabajo",
            Kind = PanelItemKind.Folder,
            Children = new List<PanelItem> { nestedFolder }
        };

        var clone = root.CloneDeep();
        clone.Children[0].Children[0].Name = "Editor modificado";
        clone.Children.Add(new PanelItem { Name = "Otro" });

        Assert.Equal("Editor", root.Children[0].Children[0].Name);
        Assert.Single(root.Children);
        Assert.NotSame(root.Children, clone.Children);
        Assert.NotSame(root.Children[0].Children, clone.Children[0].Children);
    }

    [Fact]
    public void FolderKind_CanBeSerializedAndRestored()
    {
        var folder = new PanelItem
        {
            Name = "Multimedia",
            Kind = PanelItemKind.Folder,
            Children = new List<PanelItem> { new() { Name = "Reproductor", Target = "player.exe" } }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(folder);
        var restored = System.Text.Json.JsonSerializer.Deserialize<PanelItem>(json);

        Assert.NotNull(restored);
        Assert.True(restored.IsFolder);
        Assert.Equal("Multimedia", restored.Name);
        Assert.Single(restored.Children);
    }

    [Fact]
    public void MultipleCategories_AreNormalizedAndDeepCloned()
    {
        var item = new PanelItem { Name = "Game", Category = "Games; Multimedia" };
        item.AdditionalCategories = new List<string> { "Work", "games" };
        item.NormalizeCategories();

        Assert.Equal(new[] { "Games", "Multimedia", "Work" }, item.Categories);

        var clone = item.CloneDeep();
        clone.AdditionalCategories.Add("Streaming");

        Assert.Equal(new[] { "Games", "Multimedia", "Work" }, item.Categories);
        Assert.Equal(new[] { "Games", "Multimedia", "Work", "Streaming" }, clone.Categories);
    }

    [Fact]
    public void LegacyCategoryJson_RemainsReadable()
    {
        const string json = "{\"Name\":\"Editor\",\"Category\":\"Work\"}";
        var restored = System.Text.Json.JsonSerializer.Deserialize<PanelItem>(json);

        Assert.NotNull(restored);
        restored.NormalizeCategories();
        Assert.Equal(new[] { "Work" }, restored.Categories);
    }

    [Fact]
    public void MultipleCategories_AreSerializedAndRestored()
    {
        var item = new PanelItem { Name = "Game" };
        item.SetCategories(new[] { "Games", "Multimedia" });

        var json = System.Text.Json.JsonSerializer.Serialize(item);
        var restored = System.Text.Json.JsonSerializer.Deserialize<PanelItem>(json);

        Assert.NotNull(restored);
        restored.NormalizeCategories();
        Assert.Equal(new[] { "Games", "Multimedia" }, restored.Categories);
    }

}
