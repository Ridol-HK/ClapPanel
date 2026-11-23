// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClapPanel.Localization;
using ClapPanel.Settings;
using ClapPanel.Theming;
using ClapPanelThemeMode = ClapPanel.Settings.ThemeMode;

namespace ClapPanel.Views;

public partial class ProfileSelectionWindow : Window
{
    public sealed class ProfileChoice
    {
        public AppearanceProfile Profile { get; }

        public string Name => Profile.Name;

        public string Summary { get; }

        public Brush AccentBrush { get; }

        public ProfileChoice(AppearanceProfile profile)
        {
            Profile = profile;
            var palette = AppearancePalette.From(profile.Appearance);
            AccentBrush = AppearancePalette.Brush(palette.Accent);
            var themeName = profile.Appearance.Theme switch
            {
                ClapPanelThemeMode.Light => Loc.Get("Profile.ThemeLight"),
                ClapPanelThemeMode.Dark => Loc.Get("Profile.ThemeDark"),
                _ => Loc.Get("Profile.ThemeSystem")
            };
            var displayMode = profile.Appearance.FullScreen ? Loc.Get("Profile.FullScreen") : Loc.Get("Profile.Windowed");
            Summary = Loc.Format("Profile.Summary", themeName, Math.Clamp(profile.Columns, 1, 10), displayMode);
        }
    }

    public AppearanceProfile? SelectedProfile { get; private set; }

    public ProfileSelectionWindow(IEnumerable<AppearanceProfile> profiles)
    {
        InitializeComponent();
        ProfilesList.ItemsSource = profiles
            .Where(profile => profile is not null)
            .Select(profile => new ProfileChoice(profile.Clone()))
            .ToList();
    }

    private void ChooseProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ProfileChoice choice })
        {
            SelectedProfile = choice.Profile.Clone();
            DialogResult = true;
        }
    }
}
