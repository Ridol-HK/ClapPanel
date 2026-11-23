// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Collections.Generic;
using System.Reflection;

namespace ClapPanel;

public sealed record CreditInfo(string Name, string License, string DescriptionKey, string Url);

public static class AppInfo
{
    public const string Name = "ClapPanel";

    public const string Copyright = "Copyright © 2026 Ridol-HK";

    public const string RepositoryUrl = "https://github.com/Ridol-HK/ClapPanel";

    public const string LicenseFileUrl = RepositoryUrl + "/blob/main/LICENSE";

    public static string Version { get; } = ReadVersion();

    public static IReadOnlyList<CreditInfo> Credits { get; } = new[]
    {
        new CreditInfo("NAudio", "MIT", "About.CreditNAudio", "https://github.com/naudio/NAudio"),
        new CreditInfo(".NET, WPF, Windows Forms", "MIT", "About.CreditDotNet", "https://github.com/dotnet/runtime"),
        new CreditInfo("xUnit", "Apache-2.0", "About.CreditXunit", "https://xunit.net")
    };

    public static string CleanVersion(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "0.0.0";
        }

        var plus = raw.IndexOf('+');
        return (plus >= 0 ? raw[..plus] : raw).Trim();
    }

    private static string ReadVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return CleanVersion(informational ?? assembly.GetName().Version?.ToString(3));
    }
}
