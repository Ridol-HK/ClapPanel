// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

namespace ClapPanel.Tests;

public class AppInfoTests
{
    [Theory]
    [InlineData("1.4.0+abc123", "1.4.0")]
    [InlineData("1.4.0", "1.4.0")]
    [InlineData("  1.4.0  ", "1.4.0")]
    [InlineData(null, "0.0.0")]
    [InlineData("", "0.0.0")]
    public void CleansTheVersion(string? raw, string expected) =>
        Assert.Equal(expected, AppInfo.CleanVersion(raw));

    [Fact]
    public void VersionLooksLikeSemanticVersion() =>
        Assert.Matches(@"^\d+\.\d+\.\d+", AppInfo.Version);

    [Fact]
    public void CreditsIncludeNAudioWithItsLicense()
    {
        var credit = Assert.Single(AppInfo.Credits, item => item.Name == "NAudio");

        Assert.Equal("MIT", credit.License);
        Assert.StartsWith("https://", credit.Url);
    }
}
