// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Text.Json.Serialization;

namespace ClapPanel.Detection;

public sealed class DetectionOptions
{
    [JsonIgnore]
    public float Threshold { get; set; } = 0.2f;

    public double CutoffHz { get; set; } = 1500;

    public double FrameMs { get; set; } = 5;

    public double AmbientWindowMs { get; set; } = 3000;

    public float AmbientRatio { get; set; } = 6;

    public float QuietRatio { get; set; } = 0.4f;

    public double QuietBeforeFirstMs { get; set; } = 150;

    public double QuietBetweenMs { get; set; } = 40;

    public double MaxRiseMs { get; set; } = 30;

    public double MaxDurationMs { get; set; } = 150;

    public float DecayRatio { get; set; } = 0.3f;

    public float StrengthRatio { get; set; } = 2.5f;

    public double MinGapMs { get; set; } = 150;

    public double MaxGapMs { get; set; } = 800;

    public double CooldownMs { get; set; } = 1000;

    public void ApplySensitivity(int sensitivity)
    {
        var value = Math.Clamp(sensitivity, 0, 100);
        Threshold = 0.4f - 0.38f * value / 100f;
    }
}
