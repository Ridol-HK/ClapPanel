// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using ClapPanel.Launcher;

namespace ClapPanel.Tests;

public class AppLauncherTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
    private readonly AppLauncher _launcher = new();

    public AppLauncherTests()
    {
        File.WriteAllText(_file, "x");
    }

    public void Dispose() => File.Delete(_file);

    [Fact]
    public void ExistingFileExists() => Assert.True(_launcher.Exists(_file));

    [Fact]
    public void ExistingFolderExists() => Assert.True(_launcher.Exists(Path.GetTempPath()));

    [Fact]
    public void MissingFileDoesNotExist() => Assert.False(_launcher.Exists(_file + ".missing"));

    [Fact]
    public void EmptyTargetDoesNotExist() => Assert.False(_launcher.Exists("  "));

    [Fact]
    public void SystemUriExists() => Assert.True(_launcher.Exists("ms-settings:display"));
}
