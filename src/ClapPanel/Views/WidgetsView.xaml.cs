// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ClapPanel.Localization;
using ClapPanel.Settings;
using ClapPanel.Widgets;

namespace ClapPanel.Views;

public sealed record ClipboardEntry(string Text)
{
    public string Preview => ClipboardHistory.Preview(Text);
}

public partial class WidgetsView : UserControl
{
    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private WidgetSettings? _settings;
    private ClipboardHistory? _clipboard;
    private Action<string>? _onNotesChanged;
    private bool _loadingNotes;
    private bool _pendingNotes;

    public WidgetsView()
    {
        InitializeComponent();
        _clockTimer.Tick += (_, _) => UpdateClock();
        _saveTimer.Tick += (_, _) => FlushNotes();
    }

    public void Initialize(WidgetSettings settings, ClipboardHistory clipboard, Action<string> onNotesChanged)
    {
        Shutdown();

        _settings = settings;
        _clipboard = clipboard;
        _onNotesChanged = onNotesChanged;

        ClockCard.Visibility = ToVisibility(settings.ShowClock);
        MediaCard.Visibility = ToVisibility(settings.ShowMedia);
        NotesCard.Visibility = ToVisibility(settings.ShowNotes);
        ClipboardCard.Visibility = ToVisibility(settings.ShowClipboard);
        Visibility = ToVisibility(settings.AnyEnabled);

        if (settings.ShowClock)
        {
            UpdateClock();
            _clockTimer.Start();
        }

        if (settings.ShowNotes)
        {
            _loadingNotes = true;
            NotesBox.Text = settings.Notes;
            _loadingNotes = false;
        }

        if (settings.ShowClipboard)
        {
            clipboard.Changed += Clipboard_Changed;
            RefreshClipboard();
        }
    }

    public void Shutdown()
    {
        _clockTimer.Stop();
        FlushNotes();

        if (_clipboard is not null)
        {
            _clipboard.Changed -= Clipboard_Changed;
        }
    }

    private static Visibility ToVisibility(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateClock()
    {
        if (_settings is null)
        {
            return;
        }

        var now = DateTime.Now;
        TimeText.Text = ClockFormatter.FormatTime(now, _settings, Loc.Culture);
        DateText.Text = _settings.ClockShowDate ? ClockFormatter.FormatDate(now, Loc.Culture) : "";
        DateText.Visibility = ToVisibility(_settings.ClockShowDate);
    }

    private void NotesBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingNotes)
        {
            return;
        }

        _pendingNotes = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void FlushNotes()
    {
        _saveTimer.Stop();
        if (!_pendingNotes || _settings is null || !_settings.ShowNotes)
        {
            return;
        }

        _pendingNotes = false;
        _onNotesChanged?.Invoke(NotesBox.Text);
    }

    private void Clipboard_Changed(object? sender, EventArgs e) => RefreshClipboard();

    private void RefreshClipboard()
    {
        if (_clipboard is null)
        {
            return;
        }

        var entries = _clipboard.Items.Select(text => new ClipboardEntry(text)).ToList();
        ClipboardList.ItemsSource = entries;
        ClipboardEmptyText.Visibility = ToVisibility(entries.Count == 0);
        ClearClipboardButton.IsEnabled = entries.Count > 0;
    }

    private void ClipboardEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ClipboardEntry entry })
        {
            return;
        }

        try
        {
            Clipboard.SetText(entry.Text);
        }
        catch
        {
        }
    }

    private void ClearClipboard_Click(object sender, RoutedEventArgs e) => _clipboard?.Clear();

    private void Previous_Click(object sender, RoutedEventArgs e) => MediaKeys.Send(MediaKey.PreviousTrack);

    private void PlayPause_Click(object sender, RoutedEventArgs e) => MediaKeys.Send(MediaKey.PlayPause);

    private void Next_Click(object sender, RoutedEventArgs e) => MediaKeys.Send(MediaKey.NextTrack);
}
