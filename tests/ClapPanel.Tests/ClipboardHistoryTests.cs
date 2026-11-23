// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using ClapPanel.Widgets;

namespace ClapPanel.Tests;

public class ClipboardHistoryTests
{
    [Fact]
    public void NewestTextComesFirst()
    {
        var history = new ClipboardHistory();

        history.Add("uno");
        history.Add("dos");

        Assert.Equal(new[] { "dos", "uno" }, history.Items);
    }

    [Fact]
    public void RepeatedTextMovesToTheTop()
    {
        var history = new ClipboardHistory();
        history.Add("uno");
        history.Add("dos");

        history.Add("uno");

        Assert.Equal(new[] { "uno", "dos" }, history.Items);
    }

    [Fact]
    public void ConsecutiveDuplicateIsIgnored()
    {
        var history = new ClipboardHistory();
        history.Add("uno");

        Assert.False(history.Add("uno"));
        Assert.Single(history.Items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyTextIsIgnored(string? text)
    {
        var history = new ClipboardHistory();

        Assert.False(history.Add(text));
        Assert.Empty(history.Items);
    }

    [Fact]
    public void OldestItemsAreDroppedWhenTheLimitIsReached()
    {
        var history = new ClipboardHistory { MaxItems = 3 };

        foreach (var text in new[] { "a", "b", "c", "d" })
        {
            history.Add(text);
        }

        Assert.Equal(new[] { "d", "c", "b" }, history.Items);
    }

    [Fact]
    public void ShrinkingTheLimitTrimsTheList()
    {
        var history = new ClipboardHistory { MaxItems = 5 };
        foreach (var text in new[] { "a", "b", "c", "d" })
        {
            history.Add(text);
        }

        history.MaxItems = 2;

        Assert.Equal(new[] { "d", "c" }, history.Items);
    }

    [Fact]
    public void ChangedIsRaisedOnlyWhenTheListChanges()
    {
        var history = new ClipboardHistory();
        var count = 0;
        history.Changed += (_, _) => count++;

        history.Add("uno");
        history.Add("uno");
        history.Clear();
        history.Clear();

        Assert.Equal(2, count);
    }

    [Fact]
    public void VeryLongTextIsTruncated()
    {
        var history = new ClipboardHistory();

        history.Add(new string('x', 10000));

        Assert.Equal(4000, history.Items[0].Length);
    }

    [Fact]
    public void PreviewCollapsesWhitespaceAndTruncates()
    {
        Assert.Equal("a b c", ClipboardHistory.Preview("a\n  b\t c"));

        var preview = ClipboardHistory.Preview(new string('x', 200), 20);

        Assert.Equal(20, preview.Length);
        Assert.EndsWith("…", preview);
    }
}
