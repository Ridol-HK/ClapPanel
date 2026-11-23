// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using ClapPanel.Launcher;

namespace ClapPanel.Tests;

public class InstalledAppsScannerTests
{
    [Theory]
    [InlineData("Uninstall Foo", true)]
    [InlineData("Desinstalar Foo", true)]
    [InlineData("Foo", false)]
    public void DetectsUninstallers(string name, bool expected) =>
        Assert.Equal(expected, InstalledAppsScanner.IsAuxiliary(name));

    [Fact]
    public void ParsesStoreAppsFromAnArray()
    {
        const string json = "[{\"Name\":\"Calculadora\",\"AppID\":\"Microsoft.WindowsCalculator_8wekyb3d8bbwe!App\"},{\"Name\":\"Bloc\",\"AppID\":\"C:\\\\Windows\\\\notepad.exe\"}]";

        var apps = InstalledAppsScanner.ParseStartApps(json);

        var app = Assert.Single(apps);
        Assert.Equal("Calculadora", app.Name);
        Assert.Equal(@"shell:AppsFolder\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", app.Target);
    }

    [Fact]
    public void ParsesASingleObject()
    {
        const string json = "{\"Name\":\"Fotos\",\"AppID\":\"Microsoft.Windows.Photos_8wekyb3d8bbwe!App\"}";

        Assert.Single(InstalledAppsScanner.ParseStartApps(json));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no es json")]
    [InlineData("[1,2,3]")]
    public void InvalidOutputProducesNoApps(string? json) =>
        Assert.Empty(InstalledAppsScanner.ParseStartApps(json));

    [Fact]
    public void MergeRemovesDuplicatesAndSortsByName()
    {
        var shortcuts = new[]
        {
            new InstalledApp("Zeta", @"C:\a\Zeta.lnk"),
            new InstalledApp("alfa", @"C:\a\alfa.lnk"),
            new InstalledApp("Uninstall Zeta", @"C:\a\u.lnk"),
            new InstalledApp("  ", @"C:\a\vacio.lnk")
        };
        var store = new[]
        {
            new InstalledApp("ALFA", @"shell:AppsFolder\x!App"),
            new InstalledApp("Beta", @"shell:AppsFolder\y!App")
        };

        var merged = InstalledAppsScanner.Merge(shortcuts, store);

        Assert.Equal(new[] { "alfa", "Beta", "Zeta" }, merged.Select(app => app.Name));
        Assert.Equal(@"C:\a\alfa.lnk", merged[0].Target);
    }
}
