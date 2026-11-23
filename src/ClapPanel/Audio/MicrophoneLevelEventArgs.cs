// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;

namespace ClapPanel.Audio;

/// <summary>Raw input measurements collected while the microphone is running.</summary>
public sealed class MicrophoneLevelEventArgs : EventArgs
{
    public MicrophoneLevelEventArgs(float rms, float peak, int sampleRate, int channels)
    {
        Rms = rms;
        Peak = peak;
        SampleRate = sampleRate;
        Channels = channels;
    }

    public float Rms { get; }

    public float Peak { get; }

    public int SampleRate { get; }

    public int Channels { get; }
}
