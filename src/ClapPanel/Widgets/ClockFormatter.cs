// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Globalization;
using ClapPanel.Settings;

namespace ClapPanel.Widgets;

public static class ClockFormatter
{
    public static string FormatTime(DateTime moment, WidgetSettings settings, CultureInfo culture)
    {
        var format = settings.ClockUse24Hours
            ? settings.ClockShowSeconds ? "HH:mm:ss" : "HH:mm"
            : settings.ClockShowSeconds ? "h:mm:ss tt" : "h:mm tt";

        return moment.ToString(format, culture);
    }

    public static string FormatDate(DateTime moment, CultureInfo culture)
    {
        var spanish = string.Equals(culture.TwoLetterISOLanguageName, "es", StringComparison.OrdinalIgnoreCase);
        var text = moment.ToString(spanish ? "dddd, d 'de' MMMM 'de' yyyy" : "dddd, MMMM d, yyyy", culture);
        return text.Length == 0 ? text : char.ToUpper(text[0], culture) + text[1..];
    }
}
