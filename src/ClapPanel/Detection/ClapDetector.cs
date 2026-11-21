// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;

namespace ClapPanel.Detection;

public sealed class ClapDetector
{
    private enum State
    {
        Idle,
        Capturing
    }

    private readonly DetectionOptions _options;
    private readonly HighPassFilter _filter = new();

    private int _sampleRate;
    private double _cutoff;
    private int _frameSize;
    private double _frameMs;
    private int _frameFill;
    private float _framePeak;

    private double _clock;
    private double _quietMs;
    private double _ambient;
    private double _lastTrigger = double.NegativeInfinity;
    private bool _needRelease;

    private State _state = State.Idle;
    private double _onset;
    private double _peakTime;
    private float _peak;
    private double _quietAtOnset;

    private (double Time, float Peak)? _first;

    private float _meterPeak;
    private int _meterFrames;

    public ClapDetector(DetectionOptions options)
    {
        _options = options;
    }

    public event EventHandler? DoubleClapDetected;

    public event EventHandler<float>? LevelMeasured;

    public void Process(ReadOnlySpan<float> samples, int sampleRate)
    {
        Configure(sampleRate);

        foreach (var sample in samples)
        {
            var value = Math.Abs(_filter.Process(sample));
            if (value > _framePeak)
            {
                _framePeak = value;
            }

            if (++_frameFill < _frameSize)
            {
                continue;
            }

            var peak = _framePeak;
            _framePeak = 0;
            _frameFill = 0;
            _clock += _frameMs;
            ProcessFrame(peak);
        }
    }

    private void Configure(int sampleRate)
    {
        if (sampleRate == _sampleRate && _options.CutoffHz == _cutoff)
        {
            return;
        }

        _sampleRate = sampleRate;
        _cutoff = _options.CutoffHz;
        _frameSize = Math.Max(1, (int)(sampleRate * _options.FrameMs / 1000.0));
        _frameMs = 1000.0 * _frameSize / sampleRate;
        _frameFill = 0;
        _framePeak = 0;
        _filter.Configure(_cutoff, sampleRate);
    }

    private void ProcessFrame(float env)
    {
        var threshold = Math.Max(_options.Threshold, (float)(_ambient * _options.AmbientRatio));
        var quietLevel = threshold * _options.QuietRatio;
        var quietBefore = _quietMs;
        _quietMs = env < quietLevel ? _quietMs + _frameMs : 0;

        PublishLevel(env, threshold);

        if (_state == State.Capturing)
        {
            Capture(env);
            return;
        }

        _ambient += (Math.Min(env, threshold) - _ambient) * Math.Min(1.0, _frameMs / _options.AmbientWindowMs);

        if (_needRelease)
        {
            if (env < threshold)
            {
                _needRelease = false;
            }

            return;
        }

        if (env < threshold)
        {
            return;
        }

        if (_clock - _lastTrigger < _options.CooldownMs)
        {
            _needRelease = true;
            return;
        }

        _state = State.Capturing;
        _onset = _clock;
        _peak = env;
        _peakTime = _clock;
        _quietAtOnset = quietBefore;
    }

    private void Capture(float env)
    {
        if (env > _peak)
        {
            _peak = env;
            _peakTime = _clock;
        }

        if (_clock - _onset > _options.MaxDurationMs)
        {
            _state = State.Idle;
            _needRelease = true;
            return;
        }

        if (env <= _peak * _options.DecayRatio)
        {
            _state = State.Idle;
            _needRelease = true;

            if (_peakTime - _onset <= _options.MaxRiseMs)
            {
                RegisterClap(_onset, _peak, _quietAtOnset);
            }
        }
    }

    private void RegisterClap(double onset, float peak, double quietBefore)
    {
        if (_first is { } first)
        {
            var gap = onset - first.Time;
            if (gap < _options.MinGapMs)
            {
                return;
            }

            if (gap <= _options.MaxGapMs
                && quietBefore >= _options.QuietBetweenMs
                && IsSimilar(first.Peak, peak))
            {
                _first = null;
                _lastTrigger = _clock;
                DoubleClapDetected?.Invoke(this, EventArgs.Empty);
                return;
            }

            _first = null;
        }

        if (quietBefore >= _options.QuietBeforeFirstMs)
        {
            _first = (onset, peak);
        }
    }

    private bool IsSimilar(float first, float second)
    {
        var ratio = second / first;
        return ratio <= _options.StrengthRatio && ratio >= 1f / _options.StrengthRatio;
    }

    private void PublishLevel(float env, float threshold)
    {
        if (env > _meterPeak)
        {
            _meterPeak = env;
        }

        if (++_meterFrames < 10)
        {
            return;
        }

        var level = Math.Min(_meterPeak / threshold * 0.5f, 1f);
        _meterPeak = 0;
        _meterFrames = 0;
        LevelMeasured?.Invoke(this, level);
    }
}
