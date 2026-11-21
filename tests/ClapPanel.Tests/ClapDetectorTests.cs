// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using ClapPanel.Detection;

namespace ClapPanel.Tests;

public class ClapDetectorTests
{
    private const int Rate = 48000;

    private readonly Random _random = new(42);
    private readonly ClapDetector _detector;
    private int _count;

    public ClapDetectorTests()
    {
        _detector = new ClapDetector(new DetectionOptions { Threshold = 0.1f });
        _detector.DoubleClapDetected += (_, _) => _count++;
    }

    private static float[] Silence(int milliseconds) => new float[Rate * milliseconds / 1000];

    private float[] Clap(float amplitude = 0.8f)
    {
        var samples = new float[Rate * 60 / 1000];
        for (var i = 0; i < samples.Length; i++)
        {
            var envelope = amplitude * Math.Exp(-(i / (double)Rate) / 0.008);
            samples[i] = (float)(envelope * (_random.NextDouble() * 2 - 1));
        }

        return samples;
    }

    private float[] Noise(int milliseconds, float amplitude)
    {
        var samples = new float[Rate * milliseconds / 1000];
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = amplitude * (float)(_random.NextDouble() * 2 - 1);
        }

        return samples;
    }

    private static float[] Voice(int milliseconds)
    {
        var samples = new float[Rate * milliseconds / 1000];
        for (var i = 0; i < samples.Length; i++)
        {
            var carrier = Math.Sin(2 * Math.PI * 250 * i / Rate);
            var modulation = 0.5 + 0.5 * Math.Sin(2 * Math.PI * 4 * i / Rate);
            samples[i] = (float)(0.9 * carrier * modulation);
        }

        return samples;
    }

    private void Feed(params float[][] parts)
    {
        var all = parts.SelectMany(part => part).ToArray();
        for (var i = 0; i < all.Length; i += 441)
        {
            _detector.Process(all.AsSpan(i, Math.Min(441, all.Length - i)), Rate);
        }
    }

    [Fact]
    public void DoubleClapTriggers()
    {
        Feed(Silence(500), Clap(), Silence(190), Clap(), Silence(300));

        Assert.Equal(1, _count);
    }

    [Fact]
    public void ClapsOfSimilarStrengthTrigger()
    {
        Feed(Silence(500), Clap(0.8f), Silence(190), Clap(0.5f), Silence(300));

        Assert.Equal(1, _count);
    }

    [Fact]
    public void SingleClapDoesNotTrigger()
    {
        Feed(Silence(500), Clap(), Silence(2000));

        Assert.Equal(0, _count);
    }

    [Fact]
    public void ClapsTooCloseDoNotTrigger()
    {
        Feed(Silence(500), Clap(), Silence(40), Clap(), Silence(300));

        Assert.Equal(0, _count);
    }

    [Fact]
    public void ClapsTooFarApartDoNotTrigger()
    {
        Feed(Silence(500), Clap(), Silence(940), Clap(), Silence(300));

        Assert.Equal(0, _count);
    }

    [Fact]
    public void VoiceDoesNotTrigger()
    {
        Feed(Silence(500), Voice(2000), Silence(300));

        Assert.Equal(0, _count);
    }

    [Fact]
    public void SustainedNoiseDoesNotTrigger()
    {
        Feed(Silence(500), Noise(300, 0.8f), Silence(300), Noise(300, 0.8f), Silence(300));

        Assert.Equal(0, _count);
    }

    [Fact]
    public void ClapsWithVeryDifferentStrengthDoNotTrigger()
    {
        Feed(Silence(500), Clap(0.9f), Silence(190), Clap(0.25f), Silence(300));

        Assert.Equal(0, _count);
    }

    [Fact]
    public void ContinuousNoiseBeforeClapsPreventsTrigger()
    {
        Feed(Silence(500), Noise(1000, 0.06f), Clap(), Silence(190), Clap(), Silence(300));

        Assert.Equal(0, _count);
    }

    [Fact]
    public void CooldownPreventsSecondTrigger()
    {
        Feed(
            Silence(500), Clap(), Silence(190), Clap(),
            Silence(190), Clap(), Silence(190), Clap(),
            Silence(300));

        Assert.Equal(1, _count);
    }

    [Fact]
    public void TriggersAgainAfterCooldown()
    {
        Feed(
            Silence(500), Clap(), Silence(190), Clap(),
            Silence(1500), Clap(), Silence(190), Clap(),
            Silence(300));

        Assert.Equal(2, _count);
    }
}
