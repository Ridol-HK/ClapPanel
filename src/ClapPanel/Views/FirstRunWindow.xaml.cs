// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ClapPanel.Audio;
using ClapPanel.Localization;
using ClapPanel.Settings;

namespace ClapPanel.Views;

public partial class FirstRunWindow : Window
{
    private readonly AppSettings _settings;
    private readonly AudioDeviceProvider _devices;
    private readonly MicrophoneService _microphone;
    private readonly DispatcherTimer _diagnosticTimer;
    private int _step = 1;
    private DateTime _lastSignalUtc = DateTime.MinValue;
    private float _lastPeak;
    private bool _loading = true;

    public FirstRunWindow(AppSettings settings, AudioDeviceProvider devices, MicrophoneService microphone)
    {
        InitializeComponent();
        _settings = settings;
        _devices = devices;
        _microphone = microphone;

        var microphones = _devices.GetMicrophones();
        DeviceCombo.ItemsSource = microphones;
        DeviceCombo.SelectedValue = settings.MicrophoneId;
        if (DeviceCombo.SelectedItem is null && microphones.Count > 0)
        {
            DeviceCombo.SelectedIndex = 0;
        }

        SelectByTag(LanguageCombo, settings.Language.ToString());
        SensitivitySlider.Value = settings.Sensitivity;
        var shortcut = settings.GlobalShortcut ?? new GlobalShortcutSettings();
        HotkeyEnabledCheck.IsChecked = shortcut.Enabled;
        HotkeyCtrlCheck.IsChecked = (shortcut.Modifiers & GlobalShortcutSettings.Control) != 0;
        HotkeyAltCheck.IsChecked = (shortcut.Modifiers & GlobalShortcutSettings.Alt) != 0;
        HotkeyShiftCheck.IsChecked = (shortcut.Modifiers & GlobalShortcutSettings.Shift) != 0;
        HotkeyWinCheck.IsChecked = (shortcut.Modifiers & GlobalShortcutSettings.Windows) != 0;
        SelectByTag(HotkeyKeyCombo, shortcut.VirtualKeyCode.ToString());
        StartupCheck.IsChecked = settings.StartWithWindows;

        _microphone.AudioLevelMeasured += OnAudioLevelMeasured;
        _diagnosticTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };
        _diagnosticTimer.Tick += (_, _) => RefreshMicrophoneStatus();
        _diagnosticTimer.Start();
        Closed += (_, _) =>
        {
            _diagnosticTimer.Stop();
            _microphone.AudioLevelMeasured -= OnAudioLevelMeasured;
        };

        _loading = false;
        StartMicrophoneTest();
    }

    public AppSettings? Result { get; private set; }

    private static void SelectByTag(ComboBox combo, string tag)
    {
        var match = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Tag as string == tag);
        combo.SelectedValue = match?.Tag ?? combo.Items.OfType<ComboBoxItem>().FirstOrDefault()?.Tag;
    }

    private void ShowStep(int step)
    {
        _step = Math.Clamp(step, 1, 3);
        WelcomePage.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
        MicrophonePage.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
        ShortcutPage.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
        StepText.Text = Loc.Get($"Setup.Step{_step}");
        StepProgress.Value = _step;
        BackButton.Visibility = _step == 1 ? Visibility.Collapsed : Visibility.Visible;
        NextButton.Visibility = _step == 3 ? Visibility.Collapsed : Visibility.Visible;
        FinishButton.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_step == 2 && !_microphone.IsRunning)
        {
            StartMicrophoneTest();
        }

        ShowStep(_step + 1);
    }

    private void Back_Click(object sender, RoutedEventArgs e) => ShowStep(_step - 1);

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        _settings.FirstRunCompleted = true;
        Result = _settings;
        DialogResult = true;
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        var modifiers = 0;
        if (HotkeyCtrlCheck.IsChecked == true) modifiers |= GlobalShortcutSettings.Control;
        if (HotkeyAltCheck.IsChecked == true) modifiers |= GlobalShortcutSettings.Alt;
        if (HotkeyShiftCheck.IsChecked == true) modifiers |= GlobalShortcutSettings.Shift;
        if (HotkeyWinCheck.IsChecked == true) modifiers |= GlobalShortcutSettings.Windows;

        if (HotkeyEnabledCheck.IsChecked == true && modifiers == 0)
        {
            MessageBox.Show(this, Loc.Get("Settings.GlobalShortcutNeedsModifier"), "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _settings.FirstRunCompleted = true;
        _settings.Language = ParseLanguage(LanguageCombo.SelectedValue as string);
        _settings.MicrophoneId = DeviceCombo.SelectedValue as string;
        _settings.Sensitivity = (int)SensitivitySlider.Value;
        _settings.StartWithWindows = StartupCheck.IsChecked == true;
        _settings.GlobalShortcut = new GlobalShortcutSettings
        {
            Enabled = HotkeyEnabledCheck.IsChecked == true,
            Modifiers = modifiers,
            VirtualKeyCode = int.TryParse(HotkeyKeyCombo.SelectedValue as string, out var keyCode) ? keyCode : 0x20
        };

        Result = _settings;
        DialogResult = true;
    }

    private static AppLanguage ParseLanguage(string? value) =>
        Enum.TryParse<AppLanguage>(value, true, out var language) ? language : AppLanguage.System;

    private void DeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading && IsLoaded)
        {
            StartMicrophoneTest();
        }
    }

    private void TestMic_Click(object sender, RoutedEventArgs e) => StartMicrophoneTest();

    private void StartMicrophoneTest()
    {
        var started = _microphone.Start(DeviceCombo.SelectedValue as string);
        if (!started)
        {
            MicStatusText.Text = Loc.Format("Setup.MicError", _microphone.LastError ?? Loc.Get("Setup.MicUnavailable"));
            return;
        }

        _lastSignalUtc = DateTime.MinValue;
        MicStatusText.Text = Loc.Format("Setup.MicConnected", _microphone.ActiveDeviceName ?? "");
    }

    private void OnAudioLevelMeasured(object? sender, MicrophoneLevelEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            PeakBar.Value = Math.Clamp(e.Peak, 0, 1);
            RmsBar.Value = Math.Clamp(e.Rms * 3, 0, 1);
            _lastPeak = e.Peak;
            if (e.Peak >= 0.008f)
            {
                _lastSignalUtc = DateTime.UtcNow;
            }
            MicMeasurementText.Text = Loc.Format("Setup.MicMeasurements", e.Peak * 100, e.Rms * 100, e.SampleRate, e.Channels);
            RefreshMicrophoneStatus();
        }), DispatcherPriority.Background);
    }

    private void RefreshMicrophoneStatus()
    {
        if (!_microphone.IsRunning)
        {
            MicStatusText.Text = Loc.Format("Setup.MicError", _microphone.LastError ?? Loc.Get("Setup.MicUnavailable"));
            return;
        }

        if (_lastSignalUtc == DateTime.MinValue || DateTime.UtcNow - _lastSignalUtc > TimeSpan.FromSeconds(2))
        {
            MicStatusText.Text = _lastPeak > 0.001f
                ? Loc.Get("Setup.MicQuiet")
                : Loc.Get("Setup.MicWaiting");
        }
        else
        {
            MicStatusText.Text = Loc.Get("Setup.MicSignalDetected");
        }
    }
}
