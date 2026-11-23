// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Text.RegularExpressions;
using ClapPanel.Localization;

namespace ClapPanel.Tests;

[Collection("Localization")]
public sealed class LocTests : IDisposable
{
    public LocTests() => Loc.SetLanguage(AppLanguage.Spanish);

    public void Dispose() => Loc.SetLanguage(AppLanguage.Spanish);

    [Fact]
    public void EveryKeyHasBothLanguages()
    {
        foreach (var key in Loc.Keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(Loc.GetFor(key, false)), $"{key} sin español");
            Assert.False(string.IsNullOrWhiteSpace(Loc.GetFor(key, true)), $"{key} sin inglés");
        }
    }

    [Fact]
    public void PlaceholdersMatchBetweenLanguages()
    {
        foreach (var key in Loc.Keys)
        {
            Assert.Equal(Placeholders(Loc.GetFor(key, false)), Placeholders(Loc.GetFor(key, true)));
        }
    }

    [Fact]
    public void SpanishIsTheDefaultLanguage()
    {
        Assert.False(Loc.IsEnglish);
        Assert.Equal("Cancelar", Loc.Get("Common.Cancel"));
    }

    [Fact]
    public void ExplicitLanguageWinsOverSystemLanguage()
    {
        Loc.SetLanguage(AppLanguage.English, "es");
        Assert.True(Loc.IsEnglish);
        Assert.Equal("Cancel", Loc.Get("Common.Cancel"));

        Loc.SetLanguage(AppLanguage.Spanish, "en");
        Assert.False(Loc.IsEnglish);
    }

    [Theory]
    [InlineData("es", false)]
    [InlineData("ES", false)]
    [InlineData("en", true)]
    [InlineData("fr", true)]
    public void SystemLanguageFollowsTheOperatingSystem(string code, bool english)
    {
        Loc.SetLanguage(AppLanguage.System, code);

        Assert.Equal(english, Loc.IsEnglish);
    }

    [Fact]
    public void UnknownKeyReturnsTheKey()
    {
        Assert.Equal("No.Existe", Loc.Get("No.Existe"));
    }

    [Fact]
    public void FormatFillsInArguments()
    {
        Loc.SetLanguage(AppLanguage.English);

        Assert.Equal("Version 1.4.3", Loc.Format("About.Version", "1.4.3"));
    }

    [Fact]
    public void MarkupExtensionReturnsTheCurrentText()
    {
        Assert.Equal("Cancelar", new TrExtension("Common.Cancel").ProvideValue(null!));

        Loc.SetLanguage(AppLanguage.English);

        Assert.Equal("Cancel", new TrExtension("Common.Cancel").ProvideValue(null!));
    }

    private static string Placeholders(string text) =>
        string.Join(",", Regex.Matches(text, @"\{(\d+)(?::[^}]*)?\}").Select(match => match.Groups[1].Value).Distinct().OrderBy(index => index));
}
