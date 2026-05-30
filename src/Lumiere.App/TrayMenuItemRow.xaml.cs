using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lumiere.App;

public sealed partial class TrayMenuItemRow : UserControl
{
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
            var brush = row.IsDestructive
                ? (Microsoft.UI.Xaml.Media.SolidColorBrush)Application.Current.Resources["ErrorBrush"]
                : (Microsoft.UI.Xaml.Media.SolidColorBrush)Application.Current.Resources["MutedTextBrush"];
            row.ItemIcon.Foreground = brush;
            row.ItemLabel.Foreground = brush;
        }
    }

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, e);
    }
}
