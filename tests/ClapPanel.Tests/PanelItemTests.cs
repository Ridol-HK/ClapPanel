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
}
