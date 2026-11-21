// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using ClapPanel.Launcher;

namespace ClapPanel.Tests;

public class ImageStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _source;
    private readonly ImageStore _store;

    public ImageStoreTests()
    {
        Directory.CreateDirectory(_root);
        _source = Path.Combine(_root, "source.png");
        File.WriteAllText(_source, "x");
        _store = new ImageStore(Path.Combine(_root, "icons"));
    }

    public void Dispose() => Directory.Delete(_root, true);

    [Fact]
    public void ImportCopiesFileIntoStore()
    {
        var stored = _store.Import(_source);

        Assert.True(File.Exists(stored));
        Assert.NotEqual(_source, stored);
        Assert.EndsWith(".png", stored);
    }

    [Fact]
    public void ImportReturnsSamePathWhenAlreadyStored()
    {
        var stored = _store.Import(_source);

        Assert.Equal(stored, _store.Import(stored));
    }

    [Fact]
    public void CleanupRemovesOnlyUnreferencedFiles()
    {
        var kept = _store.Import(_source);
        var removed = _store.Import(_source);

        _store.Cleanup(new string?[] { kept });

        Assert.True(File.Exists(kept));
        Assert.False(File.Exists(removed));
    }
}
