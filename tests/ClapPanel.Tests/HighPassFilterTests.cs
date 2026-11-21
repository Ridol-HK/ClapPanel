// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using ClapPanel.Detection;

namespace ClapPanel.Tests;

public class HighPassFilterTests
{
    private const int Rate = 48000;

    private static double PeakAfterSettling(double frequency)
    {
        var filter = new HighPassFilter();
        filter.Configure(1500, Rate);

        double peak = 0;
        for (var i = 0; i < Rate / 2; i++)
        {
            var output = filter.Process((float)Math.Sin(2 * Math.PI * frequency * i / Rate));
            if (i > Rate / 4)
            {
                peak = Math.Max(peak, Math.Abs(output));
            }
        }

        return peak;
    }

    [Fact]
    public void AttenuatesLowFrequencies() => Assert.True(PeakAfterSettling(200) < 0.01);

    [Fact]
    public void PassesHighFrequencies() => Assert.True(PeakAfterSettling(4000) > 0.9);
}
