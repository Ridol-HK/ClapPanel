// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;

namespace ClapPanel.Detection;

public sealed class HighPassFilter
{
    private static readonly double[] Qualities = { 0.5411961, 1.3065630 };

    private readonly double[] _b0 = new double[2];
    private readonly double[] _b1 = new double[2];
    private readonly double[] _b2 = new double[2];
    private readonly double[] _a1 = new double[2];
    private readonly double[] _a2 = new double[2];
    private readonly double[] _z1 = new double[2];
    private readonly double[] _z2 = new double[2];

    public void Configure(double cutoffHz, int sampleRate)
    {
        var cutoff = Math.Min(cutoffHz, sampleRate * 0.45);
        var w0 = 2 * Math.PI * cutoff / sampleRate;
        var cos = Math.Cos(w0);
        var sin = Math.Sin(w0);

        for (var i = 0; i < Qualities.Length; i++)
        {
            var alpha = sin / (2 * Qualities[i]);
            var a0 = 1 + alpha;
            _b0[i] = (1 + cos) / 2 / a0;
            _b1[i] = -(1 + cos) / a0;
            _b2[i] = _b0[i];
            _a1[i] = -2 * cos / a0;
            _a2[i] = (1 - alpha) / a0;
            _z1[i] = 0;
            _z2[i] = 0;
        }
    }

    public float Process(float sample)
    {
        double x = sample;

        for (var i = 0; i < Qualities.Length; i++)
        {
            var y = _b0[i] * x + _z1[i];
            _z1[i] = _b1[i] * x - _a1[i] * y + _z2[i];
            _z2[i] = _b2[i] * x - _a2[i] * y;
            x = y;
        }

        return (float)x;
    }
}
