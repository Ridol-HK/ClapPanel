// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace ClapPanel.Audio;

public delegate void SamplesHandler(ReadOnlySpan<float> samples, int sampleRate);

public sealed class MicrophoneService : IDisposable
{
    private readonly AudioDeviceProvider _devices;
    private WasapiCapture? _capture;
    private float[] _mono = Array.Empty<float>();

    public MicrophoneService(AudioDeviceProvider devices)
    {
        _devices = devices;
    }

    public event SamplesHandler? SamplesAvailable;

    public bool IsRunning => _capture is not null;

    public bool Start(string? deviceId)
    {
        Stop();

        try
        {
            var device = _devices.Resolve(deviceId);
            if (device is null)
            {
                return false;
            }

            var capture = new WasapiCapture(device, true, 20);
            capture.DataAvailable += OnDataAvailable;
            capture.StartRecording();
            _capture = capture;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Stop()
    {
        var capture = _capture;
        _capture = null;
        if (capture is null)
        {
            return;
        }

        capture.DataAvailable -= OnDataAvailable;
        try
        {
            capture.StopRecording();
        }
        catch
        {
        }
        capture.Dispose();
    }

    public void Dispose() => Stop();

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (sender is not WasapiCapture capture)
        {
            return;
        }

        var format = capture.WaveFormat;
        var channels = Math.Max(1, format.Channels);
        var bytesPerSample = format.BitsPerSample / 8;
        if (bytesPerSample is not (2 or 4))
        {
            return;
        }

        var frames = e.BytesRecorded / (bytesPerSample * channels);
        if (_mono.Length < frames)
        {
            _mono = new float[frames];
        }

        for (var frame = 0; frame < frames; frame++)
        {
            float sum = 0;
            for (var channel = 0; channel < channels; channel++)
            {
                var offset = (frame * channels + channel) * bytesPerSample;
                sum += bytesPerSample == 4
                    ? BitConverter.ToSingle(e.Buffer, offset)
                    : BitConverter.ToInt16(e.Buffer, offset) / 32768f;
            }

            _mono[frame] = sum / channels;
        }

        SamplesAvailable?.Invoke(new ReadOnlySpan<float>(_mono, 0, frames), format.SampleRate);
    }
}
