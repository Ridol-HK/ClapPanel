// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ClapPanel.Localization;
using ClapPanel.Settings;

namespace ClapPanel.Views;

public partial class PanelWindow
{
    private DispatcherTimer? _announceTimer;
    private bool _suppressAnnounce;

    public void ApplyWidgets(WidgetSettings widgets)
    {
        WidgetsHost.Initialize(widgets, _clipboard, _onNotesChanged);
    }

    private void PanelWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var modifiers = Keyboard.Modifiers;

        // Keyboard navigation is real user activity even when the pointer stays
        // outside the panel. Reset both inactivity counters so it remains opaque
        // while the user moves between shortcuts with the arrow keys.
        if (PanelKeyboard.IsNavigationKey(key))
        {
            RegisterKeyboardInteraction();
        }

        if (modifiers == ModifierKeys.Control && key == Key.F)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (IsTextEntryFocused())
        {
            if (key == Key.Down && ReferenceEquals(Keyboard.FocusedElement, SearchBox))
            {
                FocusTileAt(0);
                e.Handled = true;
            }

            return;
        }

        if (modifiers == ModifierKeys.Alt && key == Key.Left)
        {
            GoBack();
            e.Handled = true;
            return;
        }

        if (modifiers != ModifierKeys.None)
        {
            return;
        }

        if (PanelKeyboard.DigitIndex(key) is { } index)
        {
            if (index < _tiles.Count)
            {
                ActivateTile(_tiles[index]);
            }

            e.Handled = true;
            return;
        }

        switch (key)
        {
            case Key.Home:
                FocusTileAt(0);
                e.Handled = true;
                break;
            case Key.End:
                FocusTileAt(_tiles.Count - 1);
                e.Handled = true;
                break;
            case Key.Back:
                GoBack();
                e.Handled = true;
                break;
        }
    }

    private void PanelWindow_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (IsTextEntryFocused() || !PanelKeyboard.StartsSearch(e.Text))
        {
            return;
        }

        SearchBox.Focus();
        if (e.Text != "/")
        {
            SearchBox.Text = e.Text;
            SearchBox.CaretIndex = SearchBox.Text.Length;
        }

        e.Handled = true;
    }

    private static bool IsTextEntryFocused() =>
        Keyboard.FocusedElement is TextBox or ComboBox or ComboBoxItem;

    private void GoBack()
    {
        if (_folderStack.Count > 0)
        {
            Back_Click(BackButton, new RoutedEventArgs());
        }
    }

    private void FocusFirstTileDeferred()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => FocusTileAt(0)));
    }

    private void FocusTileAt(int index)
    {
        if (index < 0 || index >= _tiles.Count)
        {
            return;
        }

        Tiles.UpdateLayout();
        if (Tiles.ItemContainerGenerator.ContainerFromIndex(index) is DependencyObject container)
        {
            FindVisualChild<Button>(container)?.Focus();
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private void QueueAnnounceResults()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (_suppressAnnounce)
        {
            _suppressAnnounce = false;
            return;
        }

        _announceTimer ??= CreateAnnounceTimer();
        _announceTimer.Stop();
        _announceTimer.Start();
    }

    private DispatcherTimer CreateAnnounceTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Announce(_tiles.Count == 0
                ? Loc.Get("Panel.ResultsNone")
                : Loc.Format("Panel.ResultsCount", _tiles.Count));
        };
        return timer;
    }

    private void Announce(string message)
    {
        var peer = UIElementAutomationPeer.FromElement(Tiles) ?? UIElementAutomationPeer.CreatePeerForElement(Tiles);
        peer?.RaiseNotificationEvent(
            AutomationNotificationKind.ActionCompleted,
            AutomationNotificationProcessing.ImportantMostRecent,
            message,
            "ClapPanelStatus");
    }
}
