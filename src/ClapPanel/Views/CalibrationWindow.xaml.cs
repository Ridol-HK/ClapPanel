// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using ClapPanel.Audio;
using ClapPanel.Calibration;
using ClapPanel.Detection;
using ClapPanel.Localization;

namespace ClapPanel.Views;

public partial class CalibrationWindow : Window
{
    private enum Step
    {
        Intro,
        Silence,
        Voice,
        Claps,
        Result
    }

    private static readonly TimeSpan SilenceTime = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan VoiceTime = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan ClapsTime = TimeSpan.FromSeconds(14);
    private static readonly TimeSpan MinClapsTime = TimeSpan.FromSeconds(6);

    private readonly object _lock = new();
    private readonly string? _deviceId;
    private readonly int _currentSensitivity;
    private readonly DetectionOptions _options;
    private readonly MicrophoneService _microphone;
    private readonly ClapDetector _meter;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly Stopwatch _clock = new();

    private List<float>? _current;
    private int _sampleRate = 48000;
    private float[] _silence = Array.Empty<float>();
    private float[] _voice = Array.Empty<float>();
    private float[] _claps = Array.Empty<float>();
    private Step _step = Step.Intro;
    private CalibrationResult? _result;

    public CalibrationWindow(
        AudioDeviceProvider devices,
        string? deviceId,
        DetectionOptions options,
        int currentSensitivity)
    {
        InitializeComponent();

        _deviceId = deviceId;
        _options = options;
        _currentSensitivity = currentSensitivity;
        _microphone = new MicrophoneService(devices);
        _meter = new ClapDetector(options.Clone());

        _meter.LevelMeasured += OnLevel;
        _microphone.SamplesAvailable += OnSamples;
        _timer.Tick += OnTick;
        Closed += (_, _) =>
        {
            _timer.Stop();
            _microphone.Dispose();
        };

        ShowIntro();
    }

    public int? Sensitivity { get; private set; }

    private void OnSamples(ReadOnlySpan<float> samples, int sampleRate)
    {
        _meter.Process(samples, sampleRate);

        lock (_lock)
        {
            _sampleRate = sampleRate;
            if (_current is null)
            {
                return;
            }

            foreach (var sample in samples)
            {
                _current.Add(sample);
            }
        }
    }

    private void OnLevel(object? sender, float level)
    {
        Dispatcher.BeginInvoke(new Action(() => LevelBar.Value = level), DispatcherPriority.Background);
    }

    private void ShowIntro()
    {
        TitleText.Text = Loc.Get("Cal.IntroTitle");
        BodyText.Text = Loc.Get("Cal.IntroBody");
        ProgressPanel.Visibility = Visibility.Collapsed;
        ResultText.Visibility = Visibility.Collapsed;
        SecondaryButton.Visibility = Visibility.Collapsed;
        PrimaryButton.Content = Loc.Get("Cal.Start");
        PrimaryButton.IsEnabled = true;
    }

    private void BeginStep(Step step)
    {
        _step = step;
        lock (_lock)
        {
            _current = new List<float>();
        }

        _clock.Restart();
        _timer.Start();
        TimeBar.Value = 0;
        ProgressPanel.Visibility = Visibility.Visible;
        ResultText.Visibility = Visibility.Collapsed;
        SecondaryButton.Visibility = Visibility.Collapsed;
        PrimaryButton.IsEnabled = false;

        switch (step)
        {
            case Step.Silence:
                TitleText.Text = Loc.Get("Cal.Step1Title");
                BodyText.Text = Loc.Get("Cal.Step1Body");
                PrimaryButton.Content = Loc.Get("Cal.Wait");
                break;
            case Step.Voice:
                TitleText.Text = Loc.Get("Cal.Step2Title");
                BodyText.Text = Loc.Get("Cal.Step2Body");
                PrimaryButton.Content = Loc.Get("Cal.Wait");
                break;
            case Step.Claps:
                TitleText.Text = Loc.Get("Cal.Step3Title");
                BodyText.Text = Loc.Get("Cal.Step3Body");
                PrimaryButton.Content = Loc.Get("Cal.Finish");
                break;
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var duration = _step switch
        {
            Step.Silence => SilenceTime,
            Step.Voice => VoiceTime,
            Step.Claps => ClapsTime,
            _ => TimeSpan.Zero
        };

        if (duration == TimeSpan.Zero)
        {
            _timer.Stop();
            return;
        }

        var elapsed = _clock.Elapsed;
        TimeBar.Value = Math.Min(1, elapsed / duration);

        if (_step == Step.Claps && elapsed >= MinClapsTime)
        {
            PrimaryButton.IsEnabled = true;
        }

        if (elapsed >= duration)
        {
            Advance();
        }
    }

    private void Advance()
    {
        float[] recorded;
        lock (_lock)
        {
            recorded = _current?.ToArray() ?? Array.Empty<float>();
            _current = null;
        }

        switch (_step)
        {
            case Step.Silence:
                _silence = recorded;
                BeginStep(Step.Voice);
                break;
            case Step.Voice:
                _voice = recorded;
                BeginStep(Step.Claps);
                break;
            case Step.Claps:
                _claps = recorded;
                Finish();
                break;
        }
    }

    private void Finish()
    {
        _timer.Stop();
        _microphone.Stop();

        int sampleRate;
        lock (_lock)
        {
            sampleRate = _sampleRate;
        }

        _result = CalibrationAnalyzer.Analyze(_silence, _voice, _claps, sampleRate, _options);
        _step = Step.Result;
        ShowResult(_result);
    }

    private void ShowResult(CalibrationResult result)
    {
        TitleText.Text = Loc.Get("Cal.ResultTitle");
        BodyText.Text = result.Quality switch
        {
            CalibrationQuality.Good => Loc.Get("Cal.Good"),
            CalibrationQuality.Fair => Loc.Get("Cal.Fair"),
            CalibrationQuality.Poor => Loc.Get("Cal.Poor"),
            _ => Loc.Get("Cal.Failed")
        };

        var lines = new List<string>();
        if (result.Sensitivity is { } sensitivity)
        {
            lines.Add(Loc.Format("Cal.Recommended", sensitivity, _currentSensitivity));
            lines.Add(Loc.Format("Cal.Doubles", result.DoubleClapsDetected, CalibrationAnalyzer.ExpectedDoubleClaps));
            lines.Add(Loc.Format("Cal.FalseTriggers", result.FalseTriggers));
        }
        else
        {
            lines.Add(Loc.Format("Cal.ClapsDetected", result.ClapsFound));
        }

        lines.Add(Loc.Format("Cal.Levels", result.NoiseLevel, result.VoiceLevel, result.WeakestClap));

        ResultText.Text = string.Join(Environment.NewLine, lines);
        ProgressPanel.Visibility = Visibility.Collapsed;
        ResultText.Visibility = Visibility.Visible;
        SecondaryButton.Visibility = Visibility.Visible;
        PrimaryButton.Content = Loc.Get("Common.Apply");
        PrimaryButton.IsEnabled = result.Sensitivity is not null;
    }

    private bool StartMicrophone()
    {
        if (_microphone.Start(_deviceId))
        {
            return true;
        }

        MessageBox.Show(this, Loc.Get("Cal.MicError"), "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void Primary_Click(object sender, RoutedEventArgs e)
    {
        switch (_step)
        {
            case Step.Intro:
                if (StartMicrophone())
                {
                    BeginStep(Step.Silence);
                }

                break;
            case Step.Claps:
                Advance();
                break;
            case Step.Result:
                Sensitivity = _result?.Sensitivity;
                DialogResult = true;
                break;
        }
    }

    private void Secondary_Click(object sender, RoutedEventArgs e)
    {
        if (StartMicrophone())
        {
            BeginStep(Step.Silence);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
