// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClapPanel.Widgets;

public sealed class ClipboardHistory
{
    private const int MaxTextLength = 4000;
    private const int MinCapacity = 1;
    private const int MaxCapacity = 50;

    private readonly List<string> _items = new();
    private int _maxItems = 8;

    public event EventHandler? Changed;

    public IReadOnlyList<string> Items => _items;

    public int MaxItems
    {
        get => _maxItems;
        set
        {
            _maxItems = Math.Clamp(value, MinCapacity, MaxCapacity);
            if (_items.Count > _maxItems)
            {
                _items.RemoveRange(_maxItems, _items.Count - _maxItems);
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool Add(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (text.Length > MaxTextLength)
        {
            text = text[..MaxTextLength];
        }

        if (_items.Count > 0 && _items[0] == text)
        {
            return false;
        }

        _items.Remove(text);
        _items.Insert(0, text);
        if (_items.Count > _maxItems)
        {
            _items.RemoveRange(_maxItems, _items.Count - _maxItems);
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public static string Preview(string text, int maxLength = 80)
    {
        var flat = Regex.Replace(text, @"\s+", " ").Trim();
        return flat.Length <= maxLength ? flat : flat[..(maxLength - 1)] + "…";
    }
}
