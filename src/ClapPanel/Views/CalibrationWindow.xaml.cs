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
        TitleText.Text = "Calibrar el micrófono";
        BodyText.Text = "Vamos a medir el ruido de fondo, tu voz y tus aplausos con el micrófono elegido en Ajustes. "
            + "Dura menos de 30 segundos y durante la calibración el panel no se abrirá.";
        ProgressPanel.Visibility = Visibility.Collapsed;
        ResultText.Visibility = Visibility.Collapsed;
        SecondaryButton.Visibility = Visibility.Collapsed;
        PrimaryButton.Content = "Empezar";
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
                TitleText.Text = "Paso 1 de 3: silencio";
                BodyText.Text = "Quédate en silencio unos segundos mientras medimos el ruido de fondo.";
                PrimaryButton.Content = "Espera…";
                break;
            case Step.Voice:
                TitleText.Text = "Paso 2 de 3: voz";
                BodyText.Text = "Habla con normalidad, como lo harías frente al ordenador.";
                PrimaryButton.Content = "Espera…";
                break;
            case Step.Claps:
                TitleText.Text = "Paso 3 de 3: aplausos";
                BodyText.Text = "Haz 4 dobles aplausos con la separación que usarías normalmente. "
                    + "Espera un segundo entre cada doble aplauso. Puedes terminar antes cuando acabes.";
                PrimaryButton.Content = "Terminar";
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
        TitleText.Text = "Resultado de la calibración";
        BodyText.Text = result.Quality switch
        {
            CalibrationQuality.Good => "Calibración excelente: la voz y los aplausos se distinguen con claridad.",
            CalibrationQuality.Fair => "Calibración aceptable. Puede haber algún fallo ocasional.",
            CalibrationQuality.Poor => "La voz y los aplausos quedan demasiado cerca. Acerca el micrófono, "
                + "bájale la ganancia en Windows o aplaude más fuerte, y repite la calibración.",
            _ => "No se detectaron suficientes aplausos. Comprueba que el micrófono elegido es el correcto "
                + "y repite la calibración."
        };

        var lines = new List<string>();
        if (result.Sensitivity is { } sensitivity)
        {
            lines.Add($"Sensibilidad recomendada: {sensitivity} (actual: {_currentSensitivity})");
            lines.Add($"Dobles aplausos reconocidos con ese valor: {result.DoubleClapsDetected} de {CalibrationAnalyzer.ExpectedDoubleClaps}");
            lines.Add($"Falsos disparos con silencio y voz: {result.FalseTriggers}");
        }
        else
        {
            lines.Add($"Aplausos detectados: {result.ClapsFound} (se necesitan al menos 6)");
        }

        lines.Add($"Ruido: {result.NoiseLevel:0.000} · voz: {result.VoiceLevel:0.000} · aplauso más débil: {result.WeakestClap:0.000}");

        ResultText.Text = string.Join(Environment.NewLine, lines);
        ProgressPanel.Visibility = Visibility.Collapsed;
        ResultText.Visibility = Visibility.Visible;
        SecondaryButton.Visibility = Visibility.Visible;
        PrimaryButton.Content = "Aplicar";
        PrimaryButton.IsEnabled = result.Sensitivity is not null;
    }

    private bool StartMicrophone()
    {
        if (_microphone.Start(_deviceId))
        {
            return true;
        }

        MessageBox.Show(this, "No se pudo abrir el micrófono seleccionado.", "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Warning);
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
