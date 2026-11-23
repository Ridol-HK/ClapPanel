// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace ClapPanel.Localization;

public static class Loc
{
    private static bool _english;

    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("es-ES");

    public static bool IsEnglish => _english;

    public static void SetLanguage(AppLanguage language) =>
        SetLanguage(language, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    public static void SetLanguage(AppLanguage language, string systemLanguageCode)
    {
        _english = language switch
        {
            AppLanguage.Spanish => false,
            AppLanguage.English => true,
            _ => !string.Equals(systemLanguageCode, "es", StringComparison.OrdinalIgnoreCase)
        };

        Culture = CultureInfo.GetCultureInfo(_english ? "en-US" : "es-ES");
    }

    public static string Get(string key)
    {
        if (!Strings.Table.TryGetValue(key, out var pair))
        {
            return key;
        }

        return _english ? pair.En : pair.Es;
    }

    public static IReadOnlyCollection<string> Keys => Strings.Table.Keys;

    public static string GetFor(string key, bool english)
    {
        if (!Strings.Table.TryGetValue(key, out var pair))
        {
            return key;
        }

        return english ? pair.En : pair.Es;
    }

    public static string Format(string key, params object[] arguments) =>
        string.Format(Culture, Get(key), arguments);
}
