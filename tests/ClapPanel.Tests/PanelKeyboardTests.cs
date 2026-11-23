// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Windows.Input;
using ClapPanel.Views;

namespace ClapPanel.Tests;

public class PanelKeyboardTests
{
    [Theory]
    [InlineData(Key.D1, 0)]
    [InlineData(Key.D9, 8)]
    [InlineData(Key.D0, 9)]
    [InlineData(Key.NumPad1, 0)]
    [InlineData(Key.NumPad9, 8)]
    [InlineData(Key.NumPad0, 9)]
    [InlineData(Key.A, null)]
    [InlineData(Key.Enter, null)]
    public void DigitKeysMapToTileIndexes(Key key, int? expected) =>
        Assert.Equal(expected, PanelKeyboard.DigitIndex(key));

    [Theory]
    [InlineData(0, "1")]
    [InlineData(8, "9")]
    [InlineData(9, "0")]
    [InlineData(10, "")]
    [InlineData(-1, "")]
    public void ShortcutLabelsCoverTheFirstTenTiles(int index, string expected) =>
        Assert.Equal(expected, PanelKeyboard.ShortcutLabel(index));

    [Theory]
    [InlineData(Key.Left, true)]
    [InlineData(Key.Right, true)]
    [InlineData(Key.Up, true)]
    [InlineData(Key.Down, true)]
    [InlineData(Key.Enter, false)]
    [InlineData(Key.Escape, false)]
    public void DetectsArrowNavigationKeys(Key key, bool expected) =>
        Assert.Equal(expected, PanelKeyboard.IsNavigationKey(key));

    [Theory]
    [InlineData("a", true)]
    [InlineData("ñ", true)]
    [InlineData("/", true)]
    [InlineData("1", false)]
    [InlineData(" ", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void OnlyLettersAndSlashStartASearch(string? text, bool expected) =>
        Assert.Equal(expected, PanelKeyboard.StartsSearch(text));
}
