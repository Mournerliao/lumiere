using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Lumiere.App;

public sealed partial class TrayMenuItemRow : UserControl
{
    private static readonly SolidColorBrush TransparentBrush = new(Microsoft.UI.Colors.Transparent);

    private bool hasKeyboardFocus;
    private bool isPointerOver;
    private bool isPointerPressed;

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(Icon),
            typeof(TrayMenuItemRow),
            new PropertyMetadata(Icon.Camera));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(TrayMenuItemRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ShortcutTextProperty =
        DependencyProperty.Register(
            nameof(ShortcutText),
            typeof(string),
            typeof(TrayMenuItemRow),
            new PropertyMetadata(string.Empty, OnShortcutTextChanged));

    public static readonly DependencyProperty IsDestructiveProperty =
        DependencyProperty.Register(
            nameof(IsDestructive),
            typeof(bool),
            typeof(TrayMenuItemRow),
            new PropertyMetadata(false, OnIsDestructiveChanged));

    public TrayMenuItemRow()
    {
        InitializeComponent();
        IsEnabledChanged += (_, _) => UpdateInteractiveVisual();
    }

    public event RoutedEventHandler? Click;

    public Icon Icon
    {
        get => (Icon)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string ShortcutText
    {
        get => (string)GetValue(ShortcutTextProperty);
        set => SetValue(ShortcutTextProperty, value);
    }

    public bool IsDestructive
    {
        get => (bool)GetValue(IsDestructiveProperty);
        set => SetValue(IsDestructiveProperty, value);
    }

    internal Visibility ShortcutVisibility =>
        string.IsNullOrEmpty(ShortcutText) ? Visibility.Collapsed : Visibility.Visible;

    private static void OnShortcutTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TrayMenuItemRow row)
        {
            row.ItemShortcut.Visibility = row.ShortcutVisibility;
        }
    }

    private static void OnIsDestructiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TrayMenuItemRow row)
        {
            row.ApplyDestructiveVisual();
            row.UpdateInteractiveVisual();
        }
    }

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, e);
    }

    private void OnItemButtonPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        isPointerOver = true;
        UpdateInteractiveVisual();
    }

    private void OnItemButtonPointerExited(object sender, PointerRoutedEventArgs e)
    {
        isPointerOver = false;
        isPointerPressed = false;
        UpdateInteractiveVisual();
    }

    private void OnItemButtonPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        isPointerPressed = true;
        UpdateInteractiveVisual();
    }

    private void OnItemButtonPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        isPointerPressed = false;
        UpdateInteractiveVisual();
    }

    private void OnItemButtonGotFocus(object sender, RoutedEventArgs e)
    {
        hasKeyboardFocus = true;
        UpdateInteractiveVisual();
    }

    private void OnItemButtonLostFocus(object sender, RoutedEventArgs e)
    {
        hasKeyboardFocus = false;
        isPointerPressed = false;
        UpdateInteractiveVisual();
    }

    private void ApplyDestructiveVisual()
    {
        var brush = ResourceBrush(IsDestructive ? "ErrorBrush" : "MutedTextBrush");
        ItemIcon.Foreground = brush;
        ItemLabel.Foreground = IsDestructive ? brush : ResourceBrush("TextBrush");
    }

    private void UpdateInteractiveVisual()
    {
        Opacity = IsEnabled ? 1.0 : 0.5;

        var backgroundKey = "TransparentBrush";
        if (IsEnabled && isPointerPressed)
        {
            backgroundKey = IsDestructive ? "MenuDestructivePressedBrush" : "MenuPressedBrush";
        }
        else if (IsEnabled && isPointerOver)
        {
            backgroundKey = IsDestructive ? "MenuDestructiveHoverBrush" : "MenuHoverBrush";
        }

        ItemSurface.Background = backgroundKey == "TransparentBrush"
            ? TransparentBrush
            : ResourceBrush(backgroundKey);
        ItemSurface.BorderBrush = TransparentBrush;
    }

    private static Brush ResourceBrush(string key) => (Brush)Application.Current.Resources[key];
}
