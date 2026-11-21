// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;

namespace ClapPanel.Audio;

public sealed record AudioDevice(string Id, string Name);

public sealed class AudioDeviceProvider : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();

    public IReadOnlyList<AudioDevice> GetMicrophones()
    {
        return _enumerator
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(device => new AudioDevice(device.ID, device.FriendlyName))
            .ToList();
    }

    public MMDevice? Resolve(string? id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            try
            {
                var device = _enumerator.GetDevice(id);
                if (device.State == DeviceState.Active)
                {
                    return device;
                }
            }
            catch
            {
            }
        }

        try
        {
            return _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() => _enumerator.Dispose();
}
