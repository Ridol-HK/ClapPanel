using ClapPanel.Settings;

namespace ClapPanel.Tests;

public sealed class ConfigurationArchiveTests
{
    [Fact]
    public void ExportAndFullImportPreserveShortcutsProfilesAndCustomImages()
    {
        var root = CreateTempDirectory();
        try
        {
            var sourceDirectory = Path.Combine(root, "source");
            var destinationDirectory = Path.Combine(root, "destination");
            Directory.CreateDirectory(sourceDirectory);
            var imagePath = Path.Combine(sourceDirectory, "custom.png");
            File.WriteAllBytes(imagePath, new byte[] { 1, 2, 3, 4 });

            var source = new SettingsService(Path.Combine(root, "source-data"));
            source.Save(new AppSettings
            {
                Sensitivity = 72,
                Items = new List<PanelItem>
                {
                    new()
                    {
                        Name = "Tools",
                        Kind = PanelItemKind.Folder,
                        Category = "Work",
                        Children = new List<PanelItem>
                        {
                            new() { Name = "Editor", Target = "editor.exe", ImagePath = imagePath, Category = "Development" }
                        }
                    }
                },
                AppearanceProfiles = new List<AppearanceProfile>
                {
                    new() { Name = "Night", Columns = 5, Appearance = new AppearanceSettings { Theme = ThemeMode.Dark } }
                }
            });

            var archivePath = Path.Combine(root, "configuration.zip");
            source.Archives.Export(archivePath);

            var destinationArchives = new ConfigurationArchiveService(destinationDirectory);
            var imported = destinationArchives.Import(archivePath, new AppSettings(), shortcutsOnly: false);

            Assert.Equal(72, imported.Sensitivity);
            Assert.Equal("Tools", Assert.Single(imported.Items).Name);
            var importedChild = Assert.Single(imported.Items[0].Children);
            Assert.Equal("Development", importedChild.Category);
            Assert.NotNull(importedChild.ImagePath);
            Assert.True(File.Exists(importedChild.ImagePath));
            Assert.Equal("Night", Assert.Single(imported.AppearanceProfiles).Name);
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    [Fact]
    public void ShortcutOnlyImportLeavesAppearanceAndOtherPreferencesUntouched()
    {
        var root = CreateTempDirectory();
        try
        {
            var sourceRoot = Path.Combine(root, "source");
            var source = new SettingsService(sourceRoot);
            source.Save(new AppSettings
            {
                Items = new List<PanelItem>
                {
                    new()
                    {
                        Name = "Imported folder",
                        Kind = PanelItemKind.Folder,
                        Category = "Imported",
                        Children = new List<PanelItem> { new() { Name = "Nested shortcut", Target = "nested.exe" } }
                    }
                }
            });
            var archivePath = Path.Combine(root, "shortcuts.zip");
            source.Archives.Export(archivePath);

            var current = new AppSettings
            {
                Sensitivity = 91,
                Appearance = new AppearanceSettings { BackgroundColor = "#123456", Theme = ThemeMode.Dark },
                StartWithWindows = true,
                Items = new List<PanelItem> { new() { Name = "Keep me" } }
            };

            var destination = new ConfigurationArchiveService(Path.Combine(root, "destination"));
            var imported = destination.Import(archivePath, current, shortcutsOnly: true);

            Assert.Equal(91, imported.Sensitivity);
            Assert.True(imported.StartWithWindows);
            Assert.Equal("#123456", imported.Appearance.BackgroundColor);
            Assert.Equal(ThemeMode.Dark, imported.Appearance.Theme);
            var folder = Assert.Single(imported.Items);
            Assert.Equal("Imported folder", folder.Name);
            Assert.Equal("Imported", folder.Category);
            Assert.Equal("Nested shortcut", Assert.Single(folder.Children).Name);
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    [Fact]
    public void MissingManagedImageIsRecoveredFromBackup()
    {
        var root = CreateTempDirectory();
        try
        {
            var iconsDirectory = Path.Combine(root, "icons");
            Directory.CreateDirectory(iconsDirectory);
            var imagePath = Path.Combine(iconsDirectory, "custom.png");
            File.WriteAllBytes(imagePath, new byte[] { 9, 8, 7, 6 });

            var service = new SettingsService(root);
            service.Save(new AppSettings
            {
                Items = new List<PanelItem> { new() { Name = "Image shortcut", ImagePath = imagePath } }
            });

            File.Delete(imagePath);
            var recovered = service.Load();

            var recoveredPath = Assert.Single(recovered.Items).ImagePath;
            Assert.NotNull(recoveredPath);
            Assert.True(File.Exists(recoveredPath));
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    [Fact]
    public void CorruptSettingsAreRecoveredFromTheLatestValidBackup()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new SettingsService(root);
            service.Save(new AppSettings { Sensitivity = 42 });
            Assert.NotNull(service.Archives.CreateBackup());

            service.Save(new AppSettings { Sensitivity = 96 });
            File.WriteAllText(Path.Combine(root, "settings.json"), "{ this is not valid JSON");

            var recovered = service.Load();

            Assert.Equal(42, recovered.Sensitivity);
            Assert.Contains("\"Sensitivity\": 42", File.ReadAllText(Path.Combine(root, "settings.json")));
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ClapPanelTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }
}
