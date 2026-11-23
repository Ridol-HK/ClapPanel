// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClapPanel.Settings;

namespace ClapPanel.Views;

/// <summary>
/// Adds drag-and-drop reordering to a ListBox whose source is a list of PanelItem objects.
/// The behavior is shared by the main Settings list and nested-folder editor lists.
/// </summary>
internal static class ListBoxReorderBehavior
{
    public static void Attach(ListBox listBox)
    {
        ArgumentNullException.ThrowIfNull(listBox);

        listBox.AllowDrop = true;
        Point dragStart = default;
        PanelItem? draggedItem = null;

        listBox.PreviewMouseLeftButtonDown += (_, e) =>
        {
            var container = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            draggedItem = container?.DataContext as PanelItem;
            if (draggedItem is not null)
            {
                listBox.SelectedItem = draggedItem;
                dragStart = e.GetPosition(listBox);
            }
        };

        listBox.PreviewMouseLeftButtonUp += (_, _) => draggedItem = null;

        listBox.PreviewMouseMove += (_, e) =>
        {
            if (e.LeftButton != MouseButtonState.Pressed || draggedItem is null)
            {
                return;
            }

            var current = e.GetPosition(listBox);
            if (Math.Abs(current.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(current.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            var item = draggedItem;
            draggedItem = null;
            DragDrop.DoDragDrop(listBox, new DataObject(typeof(PanelItem), item), DragDropEffects.Move);
            e.Handled = true;
        };

        listBox.DragOver += (_, e) =>
        {
            var items = listBox.ItemsSource as IList<PanelItem>;
            var item = e.Data.GetData(typeof(PanelItem)) as PanelItem;
            e.Effects = items is not null && item is not null && items.Contains(item)
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        };

        listBox.Drop += (_, e) =>
        {
            if (listBox.ItemsSource is not IList<PanelItem> items
                || e.Data.GetData(typeof(PanelItem)) is not PanelItem item)
            {
                return;
            }

            var sourceIndex = items.IndexOf(item);
            if (sourceIndex < 0)
            {
                return;
            }

            var insertionIndex = GetInsertionIndex(listBox, e.GetPosition(listBox), items.Count);
            if (insertionIndex > sourceIndex)
            {
                insertionIndex--;
            }

            insertionIndex = Math.Clamp(insertionIndex, 0, items.Count - 1);
            if (insertionIndex != sourceIndex)
            {
                if (items is ObservableCollection<PanelItem> observable)
                {
                    observable.Move(sourceIndex, insertionIndex);
                }
                else
                {
                    items.RemoveAt(sourceIndex);
                    items.Insert(insertionIndex, item);
                }
            }

            listBox.SelectedItem = item;
            e.Handled = true;
        };
    }

    private static int GetInsertionIndex(ListBox listBox, Point pointer, int itemCount)
    {
        var lastVisibleIndex = -1;
        for (var index = 0; index < itemCount; index++)
        {
            if (listBox.ItemContainerGenerator.ContainerFromIndex(index) is not ListBoxItem container
                || !container.IsVisible)
            {
                continue;
            }

            var topLeft = container.TranslatePoint(new Point(0, 0), listBox);
            var middle = topLeft.Y + container.ActualHeight / 2;
            if (pointer.Y < middle)
            {
                return index;
            }

            lastVisibleIndex = index;
        }

        return lastVisibleIndex >= 0 ? lastVisibleIndex + 1 : itemCount;
    }
}
