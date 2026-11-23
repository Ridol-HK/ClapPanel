// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Windows.Input;

namespace ClapPanel.Views;

public static class PanelKeyboard
{
    public static int? DigitIndex(Key key)
    {
        if (key >= Key.D1 && key <= Key.D9)
        {
            return key - Key.D1;
        }

        if (key >= Key.NumPad1 && key <= Key.NumPad9)
        {
            return key - Key.NumPad1;
        }

        return key == Key.D0 || key == Key.NumPad0 ? 9 : null;
    }

    public static string ShortcutLabel(int index)
    {
        if (index >= 0 && index <= 8)
        {
            return (index + 1).ToString();
        }

        return index == 9 ? "0" : "";
    }

    public static bool IsNavigationKey(Key key) =>
        key is Key.Left or Key.Right or Key.Up or Key.Down;

    public static bool StartsSearch(string? text) =>
        !string.IsNullOrEmpty(text) && (text == "/" || char.IsLetter(text[0]));
}
