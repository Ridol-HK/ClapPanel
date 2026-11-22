// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using ClapPanel.Calibration;
using ClapPanel.Detection;

namespace ClapPanel.Tests;

public class CalibrationAnalyzerTests
{
    private const int Rate = 48000;

    private readonly Random _random = new(7);

    private float[] Noise(int milliseconds, float amplitude)
    {
        var samples = new float[Rate * milliseconds / 1000];
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = amplitude * (float)(_random.NextDouble() * 2 - 1);
        }

        return samples;
    }

    private float[] Quiet(int milliseconds) => Noise(milliseconds, 0.002f);

    private float[] Voice(int milliseconds, float noise = 0f)
    {
        var samples = new float[Rate * milliseconds / 1000];
        for (var i = 0; i < samples.Length; i++)
        {
            var carrier = Math.Sin(2 * Math.PI * 250 * i / Rate);
            var modulation = 0.5 + 0.5 * Math.Sin(2 * Math.PI * 4 * i / Rate);
            samples[i] = (float)(0.9 * carrier * modulation) + noise * (float)(_random.NextDouble() * 2 - 1);
        }

        return samples;
    }

    private float[] Clap(float amplitude)
    {
        var samples = new float[Rate * 60 / 1000];
        for (var i = 0; i < samples.Length; i++)
        {
            var envelope = amplitude * Math.Exp(-(i / (double)Rate) / 0.008);
            samples[i] = (float)(envelope * (_random.NextDouble() * 2 - 1)) + 0.002f * (float)(_random.NextDouble() * 2 - 1);
        }

        return samples;
    }

    private float[] ClapPhase(float amplitude)
    {
        var parts = new List<float[]> { Quiet(1000) };
        for (var i = 0; i < CalibrationAnalyzer.ExpectedDoubleClaps; i++)
        {
            parts.Add(Clap(amplitude));
            parts.Add(Quiet(190));
            parts.Add(Clap(amplitude));
            parts.Add(Quiet(1500));
        }

        return parts.SelectMany(part => part).ToArray();
    }

    private CalibrationResult Run(float[] voice, float[] claps) =>
        CalibrationAnalyzer.Analyze(Quiet(3000), voice, claps, Rate, new DetectionOptions());

    [Fact]
    public void CleanRecordingProducesGoodCalibration()
    {
        var result = Run(Voice(4000), ClapPhase(0.8f));

        Assert.Equal(CalibrationQuality.Good, result.Quality);
        Assert.NotNull(result.Sensitivity);
        Assert.Equal(CalibrationAnalyzer.ExpectedDoubleClaps, result.DoubleClapsDetected);
        Assert.Equal(0, result.FalseTriggers);
        Assert.True(result.ClapsFound >= 8);
        Assert.True(result.WeakestClap > result.VoiceLevel);
    }

    [Fact]
    public void LouderVoiceLowersSensitivity()
    {
        var clean = Run(Voice(4000), ClapPhase(0.8f));
        var noisy = Run(Voice(4000, 0.1f), ClapPhase(0.8f));

        Assert.NotNull(clean.Sensitivity);
        Assert.NotNull(noisy.Sensitivity);
        Assert.True(noisy.Sensitivity < clean.Sensitivity);
    }

    [Fact]
    public void MissingClapsFailTheCalibration()
    {
        var result = Run(Voice(4000), Quiet(14000));

        Assert.Equal(CalibrationQuality.Failed, result.Quality);
        Assert.Null(result.Sensitivity);
    }

    [Fact]
    public void ResultingSensitivityIsWithinRange()
    {
        var result = Run(Voice(4000), ClapPhase(0.5f));

        Assert.InRange(result.Sensitivity ?? -1, 0, 100);
    }
}
