// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

namespace ClapPanel.Calibration;

public enum CalibrationQuality
{
    Failed,
    Poor,
    Fair,
    Good
}

public sealed record CalibrationResult(
    int? Sensitivity,
    float NoiseLevel,
    float VoiceLevel,
    float WeakestClap,
    int ClapsFound,
    int DoubleClapsDetected,
    int FalseTriggers,
    CalibrationQuality Quality);
