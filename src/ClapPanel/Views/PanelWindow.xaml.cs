// Copyright (C) 2026 Ridol-HK
// This file is part of ClapPanel.
// ClapPanel is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// ClapPanel is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ClapPanel.Launcher;
using ClapPanel.Settings;
using ClapPanel.Theming;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace ClapPanel.Views;

public partial class PanelWindow : Window, INotifyPropertyChanged
{
    private const double Gap = 16;
    private const double SnapDistance = 30;
    private const double DockInset = 4;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(Drawing.Point point, uint flags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);
    private const int RevealThresholdPixels = 12;

    private readonly ObservableCollection<PanelTile> _tiles = new();
    private AppearanceSettings _appearance;
    private readonly AppLauncher _launcher;
    private readonly IconProvider _icons;
    private readonly Action<IReadOnlyList<PanelItem>> _onItemsChanged;
    private readonly Action _settingsRequested;
    private readonly Action<Point> _onPositionChanged;
    private readonly Action<PanelDockEdge> _onDockEdgeChanged;
    private readonly Action<Size, bool> _onSizeChanged;
    private readonly DispatcherTimer _autoHideTimer;
    private List<PanelItem> _rootItems;
    private List<PanelItem> _currentItems;
    private readonly Stack<PanelItem> _folderStack = new();
    private Point? _customPosition;
    private Size? _preferredHorizontalSize;
    private Size? _preferredVerticalSize;
    private bool _isVerticalDocked;
    private bool _initialSizingCompleted;
    private bool _allowPanelMovement;
    private Point _dragStart;
    private int _configuredColumns;
    private string _searchTerm = "";
    private string _categoryFilter = "Todas";
    private bool _favoritesOnly;
    private bool _uiReady;
    private bool _isAutoHidden;
    private bool _autoHideSuspended;
    private bool _isClosingAnimated;
    private DateTime _lastInteractionUtc = DateTime.UtcNow;

    public PanelWindow(
        IReadOnlyList<PanelItem> items,
        int columns,
        AppearanceSettings appearance,
        AppLauncher launcher,
        IconProvider icons,
        Action<IReadOnlyList<PanelItem>> onItemsChanged,
        Action settingsRequested,
        Point? savedPosition,
        bool allowPanelMovement,
        Action<Point> onPositionChanged,
        Size? savedHorizontalSize,
        Size? savedVerticalSize,
        Action<PanelDockEdge> onDockEdgeChanged,
        Action<Size, bool> onSizeChanged)
    {
        _appearance = appearance.Clone();
        _launcher = launcher;
        _icons = icons;
        _onItemsChanged = onItemsChanged;
        _settingsRequested = settingsRequested;
        _customPosition = savedPosition;
        _allowPanelMovement = allowPanelMovement;
        _rootItems = items as List<PanelItem> ?? items.ToList();
        _currentItems = _rootItems;
        _onPositionChanged = onPositionChanged;
        _onDockEdgeChanged = onDockEdgeChanged;
        _onSizeChanged = onSizeChanged;
        _preferredHorizontalSize = savedHorizontalSize;
        _preferredVerticalSize = savedVerticalSize;
        _isVerticalDocked = IsVerticalDockEdge(_appearance.DockEdge);
        _configuredColumns = columns;
        _autoHideTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _autoHideTimer.Tick += AutoHideTimer_Tick;

        IconSize = AppearanceLayout.IconSize(_appearance);
        TileSize = AppearanceLayout.TileSize(_appearance);
        MaxWidth = 1600;
        MaxHeight = 1000;

        RecalculateColumns(_rootItems.Count);
        NameWidth = TileSize - 12;
        NameVisibility = _appearance.ShowNames ? Visibility.Visible : Visibility.Collapsed;
        PanelRadius = AppearanceLayout.PanelRadius(_appearance);
        TileRadius = AppearanceLayout.TileRadius(_appearance);

        InitializeComponent();
        AppearancePalette.From(_appearance).Apply(Resources);
        Tiles.ItemsSource = _tiles;
        _uiReady = true;
        UpdateDockLayout();
        RefreshCategoryOptions();
        RefreshTiles();

        Loaded += OnLoaded;
        ContentRendered += OnContentRendered;
        SizeChanged += PanelWindow_SizeChanged;
        MouseEnter += (_, _) => RegisterInteraction(true);
        MouseLeave += (_, _) => RegisterInteraction(false);
        PreviewMouseMove += (_, _) => RegisterInteraction(true);
        Closed += (_, _) => _autoHideTimer.Stop();
    }

    public bool IsAutoHidden => _isAutoHidden;

    public int Columns { get; private set; }

    public double IconSize { get; private set; }

    public double TileSize { get; private set; }

    public double NameWidth { get; private set; }

    public Visibility NameVisibility { get; private set; }

    public CornerRadius PanelRadius { get; private set; }

    public CornerRadius TileRadius { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ApplyAppearance(AppearanceSettings appearance, int? columns = null, Point? customPosition = null)
    {
        var wasFullScreen = _appearance.FullScreen;
        var wasVerticalLayout = UsesVerticalLayout;
        var oldAnimationStyle = _appearance.AnimationStyle;
        _appearance = appearance.Clone();
        _customPosition = customPosition;
        _isVerticalDocked = IsVerticalDockEdge(_appearance.DockEdge);
        var usesVerticalLayout = UsesVerticalLayout;
        if (columns.HasValue)
        {
            _configuredColumns = columns.Value;
        }

        if (_uiReady)
        {
            if (_initialSizingCompleted && wasFullScreen != _appearance.FullScreen)
            {
                // Keep the last normal dimensions while the full-screen mode is active,
                // so leaving it restores the appropriate horizontal/vertical layout.
                if (_appearance.FullScreen)
                {
                    if (wasVerticalLayout)
                    {
                        _preferredVerticalSize = CurrentWindowSize();
                    }
                    else
                    {
                        _preferredHorizontalSize = CurrentWindowSize();
                    }

                    UpdateDockLayout();
                    ApplyFullScreenBounds();
                }
                else
                {
                    UpdateDockLayout();
                    var restoredSize = usesVerticalLayout
                        ? _preferredVerticalSize ?? CreateDefaultVerticalSize()
                        : _preferredHorizontalSize ?? CreateDefaultHorizontalSize();
                    SetWindowSize(restoredSize);
                }
            }
            else if (_initialSizingCompleted && wasVerticalLayout != usesVerticalLayout)
            {
                if (usesVerticalLayout)
                {
                    _preferredHorizontalSize ??= CurrentWindowSize();
                    UpdateDockLayout();
                    SetWindowSize(_preferredVerticalSize ?? CreateDefaultVerticalSize());
                }
                else
                {
                    _preferredVerticalSize ??= CurrentWindowSize();
                    UpdateDockLayout();
                    SetWindowSize(_preferredHorizontalSize ?? CreateDefaultHorizontalSize());
                }
            }
            else
            {
                UpdateDockLayout();
            }
        }
        else
        {
            IconSize = UsesVerticalLayout
                ? Math.Min(AppearanceLayout.IconSize(_appearance), 32)
                : AppearanceLayout.IconSize(_appearance);
            TileSize = UsesVerticalLayout
                ? Math.Min(AppearanceLayout.TileSize(_appearance), 76)
                : AppearanceLayout.TileSize(_appearance);
            NameWidth = TileSize - 12;
            NameVisibility = _appearance.ShowNames ? Visibility.Visible : Visibility.Collapsed;
            PanelRadius = _appearance.FullScreen ? new CornerRadius(0) : AppearanceLayout.PanelRadius(_appearance);
            TileRadius = AppearanceLayout.TileRadius(_appearance);
        }

        RecalculateColumns(_tiles.Count);
        NotifyAppearanceProperties();
        AppearancePalette.From(_appearance).Apply(Resources);
        SetDynamicOpacity(IsMouseOver);
        UpdateAutoHideTimer();

        InvalidateMeasure();
        UpdateLayout();
        QueueReposition();

        if (oldAnimationStyle != _appearance.AnimationStyle && _appearance.AnimationStyle != PanelAnimationStyle.None)
        {
            PlayEntranceAnimation();
        }
    }

    public void ApplySettings(
        IReadOnlyList<PanelItem> items,
        int columns,
        AppearanceSettings appearance,
        Point? customPosition,
        bool allowPanelMovement)
    {
        _rootItems = items as List<PanelItem> ?? items.ToList();
        _currentItems = _rootItems;
        _folderStack.Clear();
        _configuredColumns = columns;
        _allowPanelMovement = allowPanelMovement;
        _customPosition = customPosition;
        UpdateMoveHandle();
        UpdateFolderNavigation();
        RefreshCategoryOptions();
        RefreshTiles();
        ApplyAppearance(appearance, customPosition: customPosition);
    }

    public void SetAutoHideSuspended(bool suspended)
    {
        _autoHideSuspended = suspended;
        _lastInteractionUtc = DateTime.UtcNow;
    }

    public void RevealFromCommand()
    {
        if (_isAutoHidden)
        {
            RevealFromAutoHide();
        }
        else if (IsVisible)
        {
            Activate();
        }
    }

    public void CloseWithAnimation()
    {
        if (_isClosingAnimated || !IsVisible)
        {
            return;
        }

        var style = EffectiveAnimationStyle;
        if (style == PanelAnimationStyle.None)
        {
            Close();
            return;
        }

        _isClosingAnimated = true;
        _autoHideTimer.Stop();
        var duration = AnimationDuration(style);
        var fade = new DoubleAnimation(PanelBorder.Opacity, 0, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) => Close();
        PanelBorder.BeginAnimation(OpacityProperty, fade);
        PlayExitTransform(style, duration);
    }

    private static TimeSpan AnimationDuration(PanelAnimationStyle style) =>
        TimeSpan.FromMilliseconds(style == PanelAnimationStyle.Bounce ? 270 : 210);

    private void PlayExitTransform(PanelAnimationStyle style, TimeSpan duration)
    {
        PanelScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        PanelScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        PanelTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        PanelTranslate.BeginAnimation(TranslateTransform.YProperty, null);
        PanelScale.ScaleX = 1;
        PanelScale.ScaleY = 1;
        PanelTranslate.X = 0;
        PanelTranslate.Y = 0;

        var easing = new QuadraticEase { EasingMode = EasingMode.EaseIn };
        switch (style)
        {
            case PanelAnimationStyle.Scale:
            case PanelAnimationStyle.Pop:
            case PanelAnimationStyle.Bounce:
            {
                var target = style switch
                {
                    PanelAnimationStyle.Pop => 0.86,
                    PanelAnimationStyle.Bounce => 0.91,
                    _ => 0.94
                };
                PanelScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(1, target, duration) { EasingFunction = easing });
                PanelScale.BeginAnimation(ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(1, target, duration) { EasingFunction = easing });
                break;
            }
            case PanelAnimationStyle.Slide:
            {
                var offset = SlideOffset();
                PanelTranslate.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(0, offset.X, duration) { EasingFunction = easing });
                PanelTranslate.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(0, offset.Y, duration) { EasingFunction = easing });
                break;
            }
            case PanelAnimationStyle.Drop:
                PanelTranslate.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(0, -24, duration) { EasingFunction = easing });
                break;
        }
    }

    private PanelAnimationStyle EffectiveAnimationStyle =>
        _appearance.Animation ? _appearance.AnimationStyle : PanelAnimationStyle.None;

    private void RecalculateColumns(int itemCount)
    {
        var workArea = _appearance.FullScreen ? GetSelectedScreenBounds() : GetSelectedWorkArea();
        MaxWidth = Math.Max(1, workArea.Width - (_appearance.FullScreen ? 0 : 2 * Gap));
        MaxHeight = Math.Max(1, workArea.Height - (_appearance.FullScreen ? 0 : 2 * Gap));
        MinWidth = _appearance.FullScreen ? 1 : Math.Min(UsesVerticalLayout ? 150 : 240, MaxWidth);
        MinHeight = _appearance.FullScreen ? 1 : Math.Min(140, MaxHeight);

        if (UsesVerticalLayout)
        {
            Columns = 1;
            return;
        }

        var currentWidth = _initialSizingCompleted && double.IsFinite(Width) && Width > 0
            ? Math.Min(Width, MaxWidth)
            : MaxWidth;
        var availableTilesWidth = Math.Max(TileSize + 8, currentWidth - 36);
        var maxVisibleColumns = Math.Max(1, (int)Math.Floor(availableTilesWidth / (TileSize + 8)));
        var requestedColumns = _configuredColumns > 0 ? _configuredColumns : Columns;
        Columns = Math.Clamp(
            requestedColumns,
            1,
            Math.Max(1, Math.Min(12, Math.Min(itemCount == 0 ? 1 : itemCount, maxVisibleColumns))));
    }

    private static bool IsVerticalDockEdge(PanelDockEdge edge) =>
        edge is PanelDockEdge.Left or PanelDockEdge.Right;

    // Full-screen mode always uses the normal horizontal content layout, even
    // when the saved docking preference is a vertical screen edge.
    private bool UsesVerticalLayout => !_appearance.FullScreen && _isVerticalDocked;

    private Size CurrentWindowSize() => new(
        double.IsFinite(Width) && Width > 0 ? Width : Math.Max(150, ActualWidth),
        double.IsFinite(Height) && Height > 0 ? Height : Math.Max(140, ActualHeight));

    private Size CreateDefaultVerticalSize()
    {
        var area = GetSelectedWorkArea();
        var width = Math.Max(165, TileSize + 62);
        var height = Math.Max(360, ActualHeight);
        return new Size(Math.Min(width, area.Width), Math.Min(height, area.Height));
    }

    private Size CreateDefaultHorizontalSize()
    {
        var area = GetSelectedWorkArea();
        var width = Math.Max(340, TileSize * Math.Clamp(_configuredColumns, 2, 5) + 72);
        var height = Math.Max(250, ActualHeight);
        return new Size(Math.Min(width, area.Width), Math.Min(height, area.Height));
    }

    private void SetWindowSize(Size requested)
    {
        var area = GetSelectedWorkArea();
        var maxWidth = Math.Max(150, area.Width - 2 * Gap);
        var maxHeight = Math.Max(140, area.Height - 2 * Gap);
        var minWidth = Math.Min(UsesVerticalLayout ? 150 : 240, maxWidth);
        var minHeight = Math.Min(140, maxHeight);
        MinWidth = minWidth;
        MinHeight = minHeight;
        Width = Math.Clamp(requested.Width, minWidth, maxWidth);
        Height = Math.Clamp(requested.Height, minHeight, maxHeight);
        UpdateToolbarControlWidths();
        RecalculateColumns(_tiles.Count);
        NotifyAppearanceProperties();
        UpdateLayout();
    }

    private void UpdateDockLayout()
    {
        if (!_uiReady)
        {
            return;
        }

        ToolbarPanel.Orientation = UsesVerticalLayout ? Orientation.Vertical : Orientation.Horizontal;
        SearchBox.Width = UsesVerticalLayout ? Math.Max(100, (double.IsFinite(Width) && Width > 0 ? Width : 180) - 42) : 145;
        CategoryCombo.Width = UsesVerticalLayout ? Math.Max(100, (double.IsFinite(Width) && Width > 0 ? Width : 180) - 42) : 105;
        IconSize = UsesVerticalLayout
            ? Math.Min(AppearanceLayout.IconSize(_appearance), 32)
            : AppearanceLayout.IconSize(_appearance);
        TileSize = UsesVerticalLayout
            ? Math.Min(AppearanceLayout.TileSize(_appearance), 76)
            : AppearanceLayout.TileSize(_appearance);
        NameWidth = TileSize - 12;
        NameVisibility = _appearance.ShowNames ? Visibility.Visible : Visibility.Collapsed;
        PanelRadius = _appearance.FullScreen ? new CornerRadius(0) : AppearanceLayout.PanelRadius(_appearance);
        TileRadius = AppearanceLayout.TileRadius(_appearance);
        PanelBorder.Margin = _appearance.FullScreen ? new Thickness(0) : new Thickness(6);
        UpdateFullScreenChrome();
        RecalculateColumns(_tiles.Count);
        NotifyAppearanceProperties();
    }

    private void UpdateToolbarControlWidths()
    {
        if (!_uiReady || !UsesVerticalLayout)
        {
            return;
        }

        var controlWidth = Math.Max(100, (double.IsFinite(Width) && Width > 0 ? Width : 180) - 42);
        SearchBox.Width = controlWidth;
        CategoryCombo.Width = controlWidth;
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        if (_initialSizingCompleted)
        {
            PlacePanel();
            return;
        }

        var naturalSize = new Size(Math.Max(150, ActualWidth), Math.Max(140, ActualHeight));
        SizeToContent = System.Windows.SizeToContent.Manual;
        if (_appearance.FullScreen)
        {
            _initialSizingCompleted = true;
            UpdateDockLayout();
            ApplyFullScreenBounds();
            return;
        }

        var requested = UsesVerticalLayout
            ? _preferredVerticalSize ?? CreateDefaultVerticalSize()
            : _preferredHorizontalSize ?? naturalSize;

        if (UsesVerticalLayout)
        {
            _preferredVerticalSize ??= requested;
        }
        else
        {
            _preferredHorizontalSize ??= requested;
        }

        SetWindowSize(requested);
        _initialSizingCompleted = true;
        UpdateDockLayout();
        UpdateLayout();
        PlacePanel();
        if (UsesVerticalLayout)
        {
            var dockedPosition = DockPanelToEdge(_appearance.DockEdge);
            Left = dockedPosition.X;
            Top = dockedPosition.Y;
        }
    }

    private void PanelWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_uiReady || !_initialSizingCompleted)
        {
            return;
        }

        UpdateToolbarControlWidths();
        RecalculateColumns(_tiles.Count);
        OnPropertyChanged(nameof(Columns));
    }

    private void QueueReposition()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            UpdateLayout();
            PlacePanel();
        }));
    }

    private void NotifyAppearanceProperties()
    {
        OnPropertyChanged(nameof(Columns));
        OnPropertyChanged(nameof(IconSize));
        OnPropertyChanged(nameof(TileSize));
        OnPropertyChanged(nameof(NameWidth));
        OnPropertyChanged(nameof(NameVisibility));
        OnPropertyChanged(nameof(PanelRadius));
        OnPropertyChanged(nameof(TileRadius));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(PlacePanel));
        UpdateMoveHandle();
        SetDynamicOpacity(true);
        UpdateAutoHideTimer();
        if (EffectiveAnimationStyle != PanelAnimationStyle.None)
        {
            PlayEntranceAnimation();
        }
    }

    private void PlayEntranceAnimation()
    {
        var style = EffectiveAnimationStyle;
        PanelBorder.BeginAnimation(OpacityProperty, null);
        PanelScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        PanelScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        PanelTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        PanelTranslate.BeginAnimation(TranslateTransform.YProperty, null);
        PanelScale.ScaleX = 1;
        PanelScale.ScaleY = 1;
        PanelTranslate.X = 0;
        PanelTranslate.Y = 0;

        if (style == PanelAnimationStyle.None)
        {
            PanelBorder.Opacity = 1;
            return;
        }

        var duration = AnimationDuration(style);
        PanelBorder.Opacity = 0;
        var fade = new DoubleAnimation(0, 1, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        PanelBorder.BeginAnimation(OpacityProperty, fade);

        switch (style)
        {
            case PanelAnimationStyle.Scale:
                AnimateScaleEntrance(0.94, duration, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                break;
            case PanelAnimationStyle.Slide:
            {
                var offset = SlideOffset();
                PanelTranslate.X = offset.X;
                PanelTranslate.Y = offset.Y;
                PanelTranslate.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(offset.X, 0, duration) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
                PanelTranslate.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(offset.Y, 0, duration) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
                break;
            }
            case PanelAnimationStyle.Drop:
                PanelTranslate.Y = -24;
                PanelTranslate.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(-24, 0, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                break;
            case PanelAnimationStyle.Pop:
                AnimateScaleEntrance(0.84, duration,
                    new BackEase { Amplitude = 0.22, EasingMode = EasingMode.EaseOut });
                break;
            case PanelAnimationStyle.Bounce:
                AnimateScaleEntrance(0.91, duration,
                    new BounceEase { Bounces = 1, Bounciness = 1.6, EasingMode = EasingMode.EaseOut });
                break;
            case PanelAnimationStyle.Fade:
                break;
        }
    }

    private void AnimateScaleEntrance(double startScale, TimeSpan duration, IEasingFunction easing)
    {
        PanelScale.ScaleX = startScale;
        PanelScale.ScaleY = startScale;
        PanelScale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(startScale, 1, duration) { EasingFunction = easing });
        PanelScale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(startScale, 1, duration) { EasingFunction = easing });
    }

    private Point SlideOffset()
    {
        const double distance = 28;
        return _appearance.DockEdge switch
        {
            PanelDockEdge.Left => new Point(-distance, 0),
            PanelDockEdge.Right => new Point(distance, 0),
            PanelDockEdge.Top => new Point(0, -distance),
            PanelDockEdge.Bottom => new Point(0, distance),
            _ when _appearance.Position is PanelPosition.TopLeft or PanelPosition.TopRight => new Point(0, -distance),
            _ => new Point(0, distance)
        };
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => _settingsRequested();

    private void ClosePanel_Click(object sender, RoutedEventArgs e) => CloseWithAnimation();

    private void PanelWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (!_appearance.FullScreen || e.Key != Key.Escape)
        {
            return;
        }

        // Let users clear the search first; a second Escape closes the full-screen menu.
        if (!string.IsNullOrEmpty(SearchBox.Text))
        {
            SearchBox.Clear();
        }
        else
        {
            CloseWithAnimation();
        }

        e.Handled = true;
    }

    private void UpdateMoveHandle()
    {
        if (MoveHandle is not null)
        {
            MoveHandle.Cursor = _allowPanelMovement && !_appearance.FullScreen ? Cursors.SizeAll : Cursors.Arrow;
            MoveHandle.ToolTip = _appearance.FullScreen
                ? "Pantalla completa: pulsa Esc para cerrar el menú"
                : _allowPanelMovement
                    ? "Arrastrar para mover el panel"
                    : "El movimiento del panel está desactivado en Ajustes > General";
        }
    }

    private void MoveHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_appearance.FullScreen || !_allowPanelMovement || e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        var area = GetSelectedWorkArea();
        var draggedBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        var snappedEdge = PanelPlacement.DetectHorizontalDockEdge(area, draggedBounds, SnapDistance);

        if (snappedEdge != PanelDockEdge.None)
        {
            if (!_isVerticalDocked)
            {
                _preferredHorizontalSize = CurrentWindowSize();
                _onSizeChanged(_preferredHorizontalSize.Value, false);
            }

            _appearance.DockEdge = snappedEdge;
            _isVerticalDocked = true;
            UpdateDockLayout();
            SetWindowSize(_preferredVerticalSize ?? CreateDefaultVerticalSize());
            UpdateLayout();
            _preferredVerticalSize ??= CurrentWindowSize();
        }
        else if (_appearance.DockEdge != PanelDockEdge.None)
        {
            if (_isVerticalDocked)
            {
                _preferredVerticalSize = CurrentWindowSize();
            }

            _appearance.DockEdge = PanelDockEdge.None;
            _isVerticalDocked = false;
            UpdateDockLayout();
            SetWindowSize(_preferredHorizontalSize ?? CreateDefaultHorizontalSize());
            UpdateLayout();
        }

        var position = _appearance.DockEdge != PanelDockEdge.None
            ? DockPanelToEdge(_appearance.DockEdge)
            : ClampToWorkArea(new Point(Left, Top));
        Left = position.X;
        Top = position.Y;
        _customPosition = position;
        _onDockEdgeChanged(_appearance.DockEdge);
        _onPositionChanged(position);
        _onSizeChanged(CurrentWindowSize(), _isVerticalDocked);
        e.Handled = true;
    }

    private void Resize_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_appearance.FullScreen || !_initialSizingCompleted || sender is not Thumb thumb || thumb.Tag is not string grip)
        {
            return;
        }

        var area = GetSelectedWorkArea();
        var oldWidth = Width;
        var oldHeight = Height;
        var newWidth = oldWidth;
        var newHeight = oldHeight;
        var newLeft = Left;
        var newTop = Top;
        var resizeLeft = grip.Contains("Left", StringComparison.Ordinal);
        var resizeRight = grip.Contains("Right", StringComparison.Ordinal);
        var resizeTop = grip.Contains("Top", StringComparison.Ordinal);
        var resizeBottom = grip.Contains("Bottom", StringComparison.Ordinal);

        if (resizeLeft)
        {
            newWidth = oldWidth - e.HorizontalChange;
        }
        else if (resizeRight)
        {
            newWidth = oldWidth + e.HorizontalChange;
        }

        if (resizeTop)
        {
            newHeight = oldHeight - e.VerticalChange;
        }
        else if (resizeBottom)
        {
            newHeight = oldHeight + e.VerticalChange;
        }

        var maxWidth = Math.Max(150, area.Width - 2 * Gap);
        var maxHeight = Math.Max(140, area.Height - 2 * Gap);
        var minWidth = Math.Min(_isVerticalDocked ? 150 : 240, maxWidth);
        var minHeight = Math.Min(140, maxHeight);
        newWidth = Math.Clamp(newWidth, minWidth, maxWidth);
        newHeight = Math.Clamp(newHeight, minHeight, maxHeight);

        if (resizeLeft)
        {
            newLeft = Left + oldWidth - newWidth;
        }
        if (resizeTop)
        {
            newTop = Top + oldHeight - newHeight;
        }

        var clampedPosition = ClampToWorkArea(new Point(newLeft, newTop), newWidth, newHeight);
        if (_appearance.DockEdge == PanelDockEdge.Left)
        {
            newLeft = area.Left + DockInset;
            newTop = clampedPosition.Y;
        }
        else if (_appearance.DockEdge == PanelDockEdge.Right)
        {
            newLeft = area.Right - newWidth - DockInset;
            newTop = clampedPosition.Y;
        }
        else if (_appearance.DockEdge == PanelDockEdge.Top)
        {
            newLeft = clampedPosition.X;
            newTop = area.Top + Gap;
        }
        else if (_appearance.DockEdge == PanelDockEdge.Bottom)
        {
            newLeft = clampedPosition.X;
            newTop = area.Bottom - newHeight - Gap;
        }
        else
        {
            newLeft = clampedPosition.X;
            newTop = clampedPosition.Y;
        }

        Width = newWidth;
        Height = newHeight;
        Left = newLeft;
        Top = newTop;
    }

    private void Resize_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (_appearance.FullScreen || !_initialSizingCompleted)
        {
            return;
        }

        var size = CurrentWindowSize();
        if (_isVerticalDocked)
        {
            _preferredVerticalSize = size;
        }
        else
        {
            _preferredHorizontalSize = size;
        }

        var position = _appearance.DockEdge switch
        {
            PanelDockEdge.Left => new Point(GetSelectedWorkArea().Left + DockInset, ClampToWorkArea(new Point(Left, Top)).Y),
            PanelDockEdge.Right => new Point(GetSelectedWorkArea().Right - Width - DockInset, ClampToWorkArea(new Point(Left, Top)).Y),
            _ => ClampToWorkArea(new Point(Left, Top))
        };
        Left = position.X;
        Top = position.Y;
        _customPosition = position;
        _onPositionChanged(position);
        _onSizeChanged(size, _isVerticalDocked);
    }

    private void PlacePanel()
    {
        if (_appearance.FullScreen)
        {
            ApplyFullScreenBounds();
            return;
        }

        if (_appearance.DockEdge != PanelDockEdge.None)
        {
            var desiredDocked = DockPanelToEdge(_appearance.DockEdge);
            var clamped = ClampToWorkArea(desiredDocked);
            Left = IsVerticalDockEdge(_appearance.DockEdge) ? desiredDocked.X : clamped.X;
            Top = clamped.Y;
            return;
        }

        if (_customPosition is Point customPosition)
        {
            var clamped = ClampToWorkArea(customPosition);
            Left = clamped.X;
            Top = clamped.Y;
            _customPosition = clamped;
            return;
        }

        PositionPanel();
    }

    private Rect GetSelectedScreenBounds()
    {
        var screens = Forms.Screen.AllScreens;
        var selected = screens.FirstOrDefault(screen =>
            string.Equals(screen.DeviceName, _appearance.MonitorDeviceName, StringComparison.OrdinalIgnoreCase));
        selected ??= Forms.Screen.PrimaryScreen ?? screens.FirstOrDefault();

        if (selected is null)
        {
            return new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        }

        var dpiScale = GetMonitorDpiScale(selected);
        var bounds = selected.Bounds;
        return new Rect(bounds.Left / dpiScale, bounds.Top / dpiScale, bounds.Width / dpiScale, bounds.Height / dpiScale);
    }

    private void ApplyFullScreenBounds()
    {
        if (!_appearance.FullScreen)
        {
            return;
        }

        var bounds = GetSelectedScreenBounds();
        WindowState = System.Windows.WindowState.Normal;
        MinWidth = 1;
        MinHeight = 1;
        MaxWidth = Math.Max(1, bounds.Width);
        MaxHeight = Math.Max(1, bounds.Height);
        Width = Math.Max(1, bounds.Width);
        Height = Math.Max(1, bounds.Height);
        Left = bounds.Left;
        Top = bounds.Top;
        PanelBorder.Margin = new Thickness(0);
        PanelRadius = new CornerRadius(0);
        UpdateLayout();
    }

    private void UpdateFullScreenChrome()
    {
        if (!_uiReady)
        {
            return;
        }

        var resizeVisibility = _appearance.FullScreen ? Visibility.Collapsed : Visibility.Visible;
        ClosePanelButton.Visibility = _appearance.FullScreen ? Visibility.Visible : Visibility.Collapsed;
        LeftResizeThumb.Visibility = resizeVisibility;
        RightResizeThumb.Visibility = resizeVisibility;
        TopResizeThumb.Visibility = resizeVisibility;
        BottomResizeThumb.Visibility = resizeVisibility;
        TopLeftResizeThumb.Visibility = resizeVisibility;
        TopRightResizeThumb.Visibility = resizeVisibility;
        BottomLeftResizeThumb.Visibility = resizeVisibility;
        BottomRightResizeThumb.Visibility = resizeVisibility;
        UpdateMoveHandle();
    }

    private Rect GetSelectedWorkArea()
    {
        var screens = Forms.Screen.AllScreens;
        var selected = screens.FirstOrDefault(screen =>
            string.Equals(screen.DeviceName, _appearance.MonitorDeviceName, StringComparison.OrdinalIgnoreCase));
        selected ??= Forms.Screen.PrimaryScreen ?? screens.FirstOrDefault();

        if (selected is null)
        {
            return SystemParameters.WorkArea;
        }

        var dpiScale = GetMonitorDpiScale(selected);
        var area = selected.WorkingArea;
        return new Rect(area.Left / dpiScale, area.Top / dpiScale, area.Width / dpiScale, area.Height / dpiScale);
    }

    private static double GetMonitorDpiScale(Forms.Screen screen)
    {
        try
        {
            var bounds = screen.Bounds;
            var point = new Drawing.Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
            var monitor = MonitorFromPoint(point, 2); // MONITOR_DEFAULTTONEAREST
            if (monitor != IntPtr.Zero && GetDpiForMonitor(monitor, 0, out var dpiX, out _) == 0 && dpiX > 0)
            {
                return dpiX / 96.0;
            }
        }
        catch
        {
            // Keep the 100% fallback if the monitor DPI API is unavailable.
        }

        return 1.0;
    }

    private Point ClampToWorkArea(Point position) => ClampToWorkArea(position, ActualWidth, ActualHeight);

    private Point ClampToWorkArea(Point position, double width, double height)
    {
        var area = GetSelectedWorkArea();
        var minLeft = area.Left + Gap;
        var minTop = area.Top + Gap;
        var maxLeft = Math.Max(minLeft, area.Right - width - Gap);
        var maxTop = Math.Max(minTop, area.Bottom - height - Gap);
        return new Point(Math.Clamp(position.X, minLeft, maxLeft), Math.Clamp(position.Y, minTop, maxTop));
    }

    private Point DockPanelToEdge(PanelDockEdge edge)
    {
        var area = GetSelectedWorkArea();
        return edge switch
        {
            PanelDockEdge.Left => new Point(area.Left + DockInset, area.Top + Math.Max(Gap, (area.Height - ActualHeight) / 2)),
            PanelDockEdge.Right => new Point(area.Right - ActualWidth - DockInset, area.Top + Math.Max(Gap, (area.Height - ActualHeight) / 2)),
            PanelDockEdge.Top => new Point(area.Left + Math.Max(Gap, (area.Width - ActualWidth) / 2), area.Top + Gap),
            PanelDockEdge.Bottom => new Point(area.Left + Math.Max(Gap, (area.Width - ActualWidth) / 2), area.Bottom - ActualHeight - Gap),
            _ => new Point(area.Right - ActualWidth - Gap, area.Bottom - ActualHeight - Gap)
        };
    }

    private void PositionPanel()
    {
        var area = GetSelectedWorkArea();
        var position = PanelPlacement.Calculate(area, new Size(ActualWidth, ActualHeight), _appearance.Position, Gap);
        Left = position.X;
        Top = position.Y;
    }

    private void RegisterInteraction(bool pointerInside)
    {
        _lastInteractionUtc = DateTime.UtcNow;
        if (pointerInside)
        {
            SetDynamicOpacity(true);
        }
        else
        {
            SetDynamicOpacity(false);
        }
    }

    private void SetDynamicOpacity(bool active)
    {
        // The configured opacity is already applied to the panel brush. The
        // Window opacity is reserved for the additional inactive-state effect.
        Opacity = _appearance.DynamicTransparency && !active
            ? Math.Clamp(_appearance.InactiveOpacity, 0.2, 1.0)
            : 1.0;
    }

    private void UpdateAutoHideTimer()
    {
        if (_appearance.FullScreen)
        {
            _autoHideTimer.Stop();
            if (_isAutoHidden)
            {
                RevealFromAutoHide();
            }
            return;
        }

        if (_appearance.AutoHide)
        {
            _lastInteractionUtc = DateTime.UtcNow;
            _autoHideTimer.Start();
        }
        else
        {
            _autoHideTimer.Stop();
            if (_isAutoHidden)
            {
                RevealFromAutoHide();
            }
        }
    }

    private void AutoHideTimer_Tick(object? sender, EventArgs e)
    {
        if (_appearance.FullScreen || !_appearance.AutoHide || _autoHideSuspended || _isClosingAnimated)
        {
            return;
        }

        if (_isAutoHidden)
        {
            if (IsCursorAtRevealEdge())
            {
                RevealFromAutoHide();
            }
            return;
        }

        if (IsMouseOver || DateTime.UtcNow - _lastInteractionUtc < TimeSpan.FromSeconds(Math.Clamp(_appearance.AutoHideDelaySeconds, 1, 60)))
        {
            return;
        }

        HideForAutoHide();
    }

    private void HideForAutoHide()
    {
        if (_appearance.FullScreen || _isAutoHidden || !IsVisible)
        {
            return;
        }

        _isAutoHidden = true;
        var style = EffectiveAnimationStyle;
        if (style == PanelAnimationStyle.None)
        {
            Hide();
            return;
        }

        var duration = AnimationDuration(style);
        var animation = new DoubleAnimation(PanelBorder.Opacity, 0, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        animation.Completed += (_, _) =>
        {
            if (_isAutoHidden && !_isClosingAnimated)
            {
                Hide();
            }
        };
        PanelBorder.BeginAnimation(OpacityProperty, animation);
        PlayExitTransform(style, duration);
    }

    private void RevealFromAutoHide()
    {
        if (!_isAutoHidden || _isClosingAnimated)
        {
            return;
        }

        _isAutoHidden = false;
        PanelBorder.BeginAnimation(OpacityProperty, null);
        PanelBorder.Opacity = 1;
        if (!IsVisible)
        {
            Show();
        }
        PlacePanel();
        Activate();
        _lastInteractionUtc = DateTime.UtcNow;
        SetDynamicOpacity(true);
        PlayEntranceAnimation();
    }

    private bool IsCursorAtRevealEdge()
    {
        var cursor = Forms.Cursor.Position;
        var screens = Forms.Screen.AllScreens;
        var selected = screens.FirstOrDefault(screen =>
            string.Equals(screen.DeviceName, _appearance.MonitorDeviceName, StringComparison.OrdinalIgnoreCase));
        selected ??= Forms.Screen.PrimaryScreen;
        if (selected is null || !selected.Bounds.Contains(cursor))
        {
            return false;
        }

        var area = selected.Bounds;
        return GetRevealEdges().Any(edge => edge switch
        {
            PanelDockEdge.Left => cursor.X <= area.Left + RevealThresholdPixels,
            PanelDockEdge.Right => cursor.X >= area.Right - RevealThresholdPixels,
            PanelDockEdge.Top => cursor.Y <= area.Top + RevealThresholdPixels,
            PanelDockEdge.Bottom => cursor.Y >= area.Bottom - RevealThresholdPixels,
            _ => false
        });
    }

    private IEnumerable<PanelDockEdge> GetRevealEdges()
    {
        if (_appearance.DockEdge != PanelDockEdge.None)
        {
            yield return _appearance.DockEdge;
            yield break;
        }

        // Use the panel's current location rather than its original preset so
        // a manually moved panel reappears at the nearest edges of its monitor.
        var area = GetSelectedWorkArea();
        var centerX = Left + ActualWidth / 2;
        var centerY = Top + ActualHeight / 2;
        var horizontal = Math.Abs(centerX - area.Left) < Math.Abs(area.Right - centerX)
            ? PanelDockEdge.Left : PanelDockEdge.Right;
        var vertical = Math.Abs(centerY - area.Top) < Math.Abs(area.Bottom - centerY)
            ? PanelDockEdge.Top : PanelDockEdge.Bottom;
        yield return horizontal;
        yield return vertical;
    }

    private void RefreshCategoryOptions()
    {
        if (!_uiReady)
        {
            return;
        }

        var categories = FlattenItems(_rootItems)
            .Select(item => item.Category?.Trim() ?? "")
            .Where(category => category.Length > 0)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(category => category, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        CategoryCombo.SelectionChanged -= Category_Changed;
        CategoryCombo.Items.Clear();
        CategoryCombo.Items.Add("Todas");
        foreach (var category in categories)
        {
            CategoryCombo.Items.Add(category);
        }
        CategoryCombo.SelectedItem = categories.Contains(_categoryFilter, StringComparer.CurrentCultureIgnoreCase)
            ? _categoryFilter
            : "Todas";
        _categoryFilter = CategoryCombo.SelectedItem as string ?? "Todas";
        CategoryCombo.SelectionChanged += Category_Changed;
    }

    private static IEnumerable<PanelItem> FlattenItems(IEnumerable<PanelItem> items)
    {
        foreach (var item in items)
        {
            yield return item;
            if (item.IsFolder && item.Children is not null)
            {
                foreach (var child in FlattenItems(item.Children))
                {
                    yield return child;
                }
            }
        }
    }

    private void RefreshTiles()
    {
        _tiles.Clear();
        var query = _searchTerm.Trim();
        var parentFolders = _folderStack.Reverse().ToList();
        var useRecursiveResults = query.Length > 0 || _favoritesOnly || _categoryFilter != "Todas";
        var candidates = useRecursiveResults
            ? EnumerateItemsWithParents(_currentItems, parentFolders)
            : _currentItems.Select(item => (Item: item, Parents: (IReadOnlyList<PanelItem>)parentFolders));

        foreach (var candidate in candidates)
        {
            var item = candidate.Item;
            var matchesCategory = _categoryFilter == "Todas"
                || string.Equals(item.Category, _categoryFilter, StringComparison.CurrentCultureIgnoreCase);
            var matchesFavorite = !_favoritesOnly || item.IsFavorite;
            var matchesSearch = query.Length == 0
                || (item.Name ?? "").Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || (item.Target ?? "").Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || (item.Category ?? "").Contains(query, StringComparison.CurrentCultureIgnoreCase);

            if (!matchesCategory || !matchesFavorite || !matchesSearch)
            {
                continue;
            }

            var missing = !item.IsFolder && !_launcher.Exists(item.Target);
            _tiles.Add(new PanelTile(item, _icons.Load(item), missing, candidate.Parents));
        }

        EmptyText.Text = _currentItems.Count == 0 && !useRecursiveResults
            ? "Esta carpeta está vacía. Añade accesos directos desde Ajustes o arrastra elementos aquí."
            : "No hay elementos que coincidan con la búsqueda o los filtros.";
        EmptyText.Visibility = _tiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        FavoritesButton.IsChecked = _favoritesOnly;
        RecalculateColumns(_tiles.Count);
        OnPropertyChanged(nameof(Columns));
        UpdateLayout();
    }

    private static IEnumerable<(PanelItem Item, IReadOnlyList<PanelItem> Parents)> EnumerateItemsWithParents(
        IEnumerable<PanelItem> items,
        IReadOnlyList<PanelItem> parents)
    {
        foreach (var item in items)
        {
            yield return (item, parents);
            if (!item.IsFolder || item.Children is null || item.Children.Count == 0)
            {
                continue;
            }

            var nestedParents = parents.Concat(new[] { item }).ToList();
            foreach (var nested in EnumerateItemsWithParents(item.Children, nestedParents))
            {
                yield return nested;
            }
        }
    }

    private void UpdateFolderNavigation()
    {
        BackButton.Visibility = _folderStack.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        PanelTitleText.Text = _folderStack.Count == 0 ? "ClapPanel" : $"ClapPanel  /  {_folderStack.Peek().Name}";
    }

    private void Category_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady)
        {
            return;
        }
        _categoryFilter = CategoryCombo.SelectedItem as string ?? "Todas";
        RefreshTiles();
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTerm = SearchBox.Text;
        RefreshTiles();
    }

    private void Favorites_Click(object sender, RoutedEventArgs e)
    {
        _favoritesOnly = FavoritesButton.IsChecked == true;
        RefreshTiles();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_folderStack.Count == 0)
        {
            return;
        }

        var parent = _folderStack.Pop();
        _currentItems = _folderStack.Count == 0 ? _rootItems : _folderStack.Peek().Children;
        if (_currentItems is null)
        {
            _currentItems = new List<PanelItem>();
            parent.Children = _currentItems;
        }
        UpdateFolderNavigation();
        RefreshTiles();
    }

    private void Tile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PanelTile tile })
        {
            return;
        }

        if (tile.Item.IsFolder)
        {
            NavigateToParentFolders(tile.ParentFolders);
            _folderStack.Push(tile.Item);
            tile.Item.Children ??= new List<PanelItem>();
            _currentItems = tile.Item.Children;
            _searchTerm = "";
            SearchBox.Text = "";
            UpdateFolderNavigation();
            RefreshTiles();
            return;
        }

        if (!_launcher.Launch(tile.Item))
        {
            MessageBox.Show(this, $"No se pudo abrir \"{tile.Item.Name}\".", "ClapPanel", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }


    private void NavigateToParentFolders(IReadOnlyList<PanelItem> parentFolders)
    {
        _folderStack.Clear();
        foreach (var folder in parentFolders)
        {
            _folderStack.Push(folder);
            folder.Children ??= new List<PanelItem>();
        }

        _currentItems = parentFolders.Count == 0
            ? _rootItems
            : parentFolders[^1].Children;
        UpdateFolderNavigation();
    }

    private void Tile_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
    }

    private void Tile_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var offset = e.GetPosition(null) - _dragStart;
        if (Math.Abs(offset.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(offset.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: PanelTile tile } element)
        {
            DragDrop.DoDragDrop(element, new DataObject(typeof(PanelTile), tile), DragDropEffects.Move);
            e.Handled = true;
        }
    }

    private void Panel_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : e.Data.GetDataPresent(typeof(PanelTile)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void Panel_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddDroppedPaths(paths);
            e.Handled = true;
            return;
        }

        if (e.Data.GetData(typeof(PanelTile)) is PanelTile source)
        {
            var sourceCollection = GetCollectionFor(source);
            if (sourceCollection is null || !sourceCollection.Remove(source.Item))
            {
                return;
            }

            sourceCollection.Add(source.Item);
            RefreshTiles();
            _onItemsChanged(_rootItems);
            e.Handled = true;
        }
    }

    private void Tile_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            if (sender is FrameworkElement { DataContext: PanelTile folderTile } && folderTile.Item.IsFolder)
            {
                AddDroppedPaths(paths, folderTile.Item.Children ??= new List<PanelItem>());
            }
            else
            {
                AddDroppedPaths(paths);
            }
            e.Handled = true;
            return;
        }

        if (e.Data.GetData(typeof(PanelTile)) is not PanelTile source)
        {
            return;
        }

        if (sender is not FrameworkElement { DataContext: PanelTile target } || ReferenceEquals(source, target))
        {
            return;
        }

        var sourceCollection = GetCollectionFor(source);
        var targetCollection = GetCollectionFor(target);
        if (sourceCollection is null || targetCollection is null
            || ReferenceEquals(source.Item, target.Item)
            || !sourceCollection.Contains(source.Item)
            || !targetCollection.Contains(target.Item))
        {
            return;
        }

        // A folder cannot be moved into one of its own descendants.
        if (source.Item.IsFolder && target.ParentFolders.Any(folder => folder.Id == source.Item.Id))
        {
            e.Handled = true;
            return;
        }

        // Dropping an item onto a folder moves it into that folder.
        if (target.Item.IsFolder)
        {
            // It is already inside this folder, so do not create a duplicate or loop.
            if (source.ParentFolders.LastOrDefault()?.Id == target.Item.Id)
            {
                e.Handled = true;
                return;
            }

            sourceCollection.Remove(source.Item);
            target.Item.Children ??= new List<PanelItem>();
            target.Item.Children.Add(source.Item);
        }
        else if (ReferenceEquals(sourceCollection, targetCollection))
        {
            var sourceIndex = sourceCollection.IndexOf(source.Item);
            var targetIndex = targetCollection.IndexOf(target.Item);
            if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
            {
                return;
            }

            sourceCollection.RemoveAt(sourceIndex);
            if (sourceIndex < targetIndex)
            {
                targetIndex--;
            }
            sourceCollection.Insert(targetIndex, source.Item);
        }
        else
        {
            var targetIndex = targetCollection.IndexOf(target.Item);
            if (targetIndex < 0)
            {
                return;
            }

            sourceCollection.Remove(source.Item);
            targetCollection.Insert(targetIndex, source.Item);
        }

        RefreshCategoryOptions();
        RefreshTiles();
        _onItemsChanged(_rootItems);
        e.Handled = true;
    }

    private List<PanelItem>? GetCollectionFor(PanelTile tile)
    {
        if (tile.ParentFolders.Count == 0)
        {
            return _rootItems;
        }

        var parent = tile.ParentFolders[^1];
        parent.Children ??= new List<PanelItem>();
        return parent.Children;
    }

    private void AddDroppedPaths(IEnumerable<string> paths) => AddDroppedPaths(paths, _currentItems);

    private void AddDroppedPaths(IEnumerable<string> paths, List<PanelItem> destination)
    {
        var added = 0;
        foreach (var rawPath in paths)
        {
            var path = Environment.ExpandEnvironmentVariables(rawPath.Trim());
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                continue;
            }

            var alreadyAdded = FlattenItems(_rootItems).Any(item =>
                !string.IsNullOrWhiteSpace(item.Target)
                && string.Equals(
                    Environment.ExpandEnvironmentVariables(item.Target),
                    path,
                    StringComparison.OrdinalIgnoreCase));
            if (alreadyAdded)
            {
                continue;
            }

            var isDirectory = Directory.Exists(path);
            var name = isDirectory
                ? new DirectoryInfo(path).Name
                : Path.GetFileNameWithoutExtension(path);
            destination.Add(new PanelItem
            {
                Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileName(path) : name,
                Target = path,
                Kind = PanelItemKind.Application
            });
            added++;
        }

        if (added == 0)
        {
            return;
        }

        RefreshCategoryOptions();
        RefreshTiles();
        _onItemsChanged(_rootItems);
    }
}
