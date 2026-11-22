// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using ClapPanel.Detection;

namespace ClapPanel.Calibration;

public static class CalibrationAnalyzer
{
    public const int ExpectedDoubleClaps = 4;

    private const int MinClapsFound = 6;
    private const int MaxClapsConsidered = ExpectedDoubleClaps * 2;

    public static CalibrationResult Analyze(
        float[] silence,
        float[] voice,
        float[] claps,
        int sampleRate,
        DetectionOptions options)
    {
        var noise = Percentile(Envelope(silence, sampleRate, options), 0.99f);
        var voiceLevel = Percentile(Envelope(voice, sampleRate, options), 0.99f);
        var ceiling = Math.Max(noise, voiceLevel);

        var low = Math.Max(ceiling * 1.2f, 0.02f);
        var maxFrames = (int)Math.Ceiling(options.MaxDurationMs / FrameMilliseconds(sampleRate, options));
        var peaks = FindPeaks(Envelope(claps, sampleRate, options), low, maxFrames)
            .OrderByDescending(peak => peak)
            .ToList();

        if (peaks.Count < MinClapsFound)
        {
            var weakestFound = peaks.Count > 0 ? peaks[^1] : 0f;
            return new CalibrationResult(null, noise, voiceLevel, weakestFound, peaks.Count, 0, 0, CalibrationQuality.Failed);
        }

        var weakest = peaks.Take(MaxClapsConsidered).Last();
        var threshold = (float)Math.Sqrt(Math.Max(ceiling, 0.005f) * weakest);
        threshold = Math.Min(Math.Max(threshold, 0.03f), weakest * 0.6f);
        var sensitivity = (int)Math.Clamp(Math.Round((0.4 - threshold) / 0.38 * 100), 0, 100);

        var applied = options.Clone();
        applied.ApplySensitivity(sensitivity);
        var detected = CountDetections(applied, sampleRate, silence, claps);
        var falseTriggers = CountDetections(applied, sampleRate, silence, voice);

        var ratio = weakest / Math.Max(ceiling, 0.001f);
        var quality = CalibrationQuality.Poor;
        if (detected >= 3 && falseTriggers == 0 && ratio >= 3)
        {
            quality = CalibrationQuality.Good;
        }
        else if (detected >= 2 && falseTriggers == 0 && ratio >= 1.5f)
        {
            quality = CalibrationQuality.Fair;
        }

        return new CalibrationResult(sensitivity, noise, voiceLevel, weakest, peaks.Count, detected, falseTriggers, quality);
    }

    private static double FrameMilliseconds(int sampleRate, DetectionOptions options)
    {
        var size = Math.Max(1, (int)(sampleRate * options.FrameMs / 1000.0));
        return 1000.0 * size / sampleRate;
    }

    private static List<float> Envelope(float[] samples, int sampleRate, DetectionOptions options)
    {
        var frames = new List<float>();
        var detector = new ClapDetector(options.Clone());
        detector.FrameMeasured += (_, level) => frames.Add(level);
        detector.Process(samples, sampleRate);
        return frames;
    }

    private static List<float> FindPeaks(List<float> envelope, float low, int maxFrames)
    {
        var peaks = new List<float>();
        var i = 0;

        while (i < envelope.Count)
        {
            if (envelope[i] < low)
            {
                i++;
                continue;
            }

            var peak = 0f;
            var j = i;
            while (j < envelope.Count && envelope[j] >= low * 0.5f && j - i <= maxFrames)
            {
                peak = Math.Max(peak, envelope[j]);
                j++;
            }

            peaks.Add(peak);

            while (j < envelope.Count && envelope[j] >= low)
            {
                j++;
            }

            i = j;
        }

        return peaks;
    }

    private static float Percentile(List<float> values, float percentile)
    {
        if (values.Count == 0)
        {
            return 0f;
        }

        var sorted = values.OrderBy(value => value).ToList();
        return sorted[(int)Math.Round((sorted.Count - 1) * percentile)];
    }

    private static int CountDetections(DetectionOptions options, int sampleRate, params float[][] parts)
    {
        var detector = new ClapDetector(options);
        var count = 0;
        detector.DoubleClapDetected += (_, _) => count++;

        foreach (var part in parts)
        {
            detector.Process(part, sampleRate);
        }

        return count;
    }
}
