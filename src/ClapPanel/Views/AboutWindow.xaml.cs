// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System.Diagnostics;
using System.Linq;
using System.Windows;
using ClapPanel.Localization;

namespace ClapPanel.Views;

public sealed record CreditView(string Name, string License, string Description, string Url);

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        VersionText.Text = Loc.Format("About.Version", AppInfo.Version);
        CopyrightText.Text = AppInfo.Copyright;
        CreditsList.ItemsSource = AppInfo.Credits
            .Select(credit => new CreditView(credit.Name, credit.License, Loc.Get(credit.DescriptionKey), credit.Url))
            .ToList();
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show(this, Loc.Get("About.OpenLinkError"), AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Repository_Click(object sender, RoutedEventArgs e) => OpenUrl(AppInfo.RepositoryUrl);

    private void License_Click(object sender, RoutedEventArgs e) => OpenUrl(AppInfo.LicenseFileUrl);

    private void Credit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url })
        {
            OpenUrl(url);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
