// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Globalization;
using ClapPanel.Settings;
using ClapPanel.Widgets;

namespace ClapPanel.Tests;

public class ClockFormatterTests
{
    private static readonly DateTime Moment = new(2026, 11, 22, 15, 4, 9);
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void Uses24HoursWithoutSecondsByDefault() =>
        Assert.Equal("15:04", ClockFormatter.FormatTime(Moment, new WidgetSettings(), Spanish));

    [Fact]
    public void CanShowSeconds() =>
        Assert.Equal("15:04:09", ClockFormatter.FormatTime(Moment, new WidgetSettings { ClockShowSeconds = true }, Spanish));

    [Fact]
    public void CanUseTwelveHours() =>
        Assert.Equal("3:04 PM", ClockFormatter.FormatTime(Moment, new WidgetSettings { ClockUse24Hours = false }, English));

    [Fact]
    public void FormatsTheDateInSpanish() =>
        Assert.Equal("Domingo, 22 de noviembre de 2026", ClockFormatter.FormatDate(Moment, Spanish));

    [Fact]
    public void FormatsTheDateInEnglish() =>
        Assert.Equal("Sunday, November 22, 2026", ClockFormatter.FormatDate(Moment, English));
}
