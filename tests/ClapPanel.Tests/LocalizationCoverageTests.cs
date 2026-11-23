// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Text.RegularExpressions;
using ClapPanel.Localization;

namespace ClapPanel.Tests;

public class LocalizationCoverageTests
{
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ClapPanel.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
    }

    private static IEnumerable<string> SourceFiles(string pattern) =>
        Directory.EnumerateFiles(Path.Combine(FindRoot(), "src"), pattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    [Fact]
    public void KeysUsedInXamlExist()
    {
        var known = Loc.Keys.ToHashSet();
        var missing = SourceFiles("*.xaml")
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"\{loc:Tr ([A-Za-z0-9.]+)\}").Select(match => match.Groups[1].Value))
            .Where(key => !known.Contains(key))
            .Distinct()
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void KeysUsedInCodeExist()
    {
        var known = Loc.Keys.ToHashSet();
        var missing = SourceFiles("*.cs")
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"Loc\.(?:Get|Format)\(""([A-Za-z0-9.]+)""").Select(match => match.Groups[1].Value))
            .Where(key => !known.Contains(key))
            .Distinct()
            .ToList();

        Assert.Empty(missing);
    }
}
