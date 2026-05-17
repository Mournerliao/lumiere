using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Lumiere.App;

public sealed partial class CaptureActionCard : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(Icon),
            typeof(CaptureActionCard),
            new PropertyMetadata(Icon.Camera));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(CaptureActionCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(CaptureActionCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SecondaryForegroundProperty =
        DependencyProperty.Register(
            nameof(SecondaryForeground),
            typeof(Brush),
            typeof(CaptureActionCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IconBackgroundProperty =
        DependencyProperty.Register(
            nameof(IconBackground),
            typeof(Brush),
            typeof(CaptureActionCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IconBorderBrushProperty =
        DependencyProperty.Register(
            nameof(IconBorderBrush),
            typeof(Brush),
            typeof(CaptureActionCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IconForegroundProperty =
        DependencyProperty.Register(
            nameof(IconForeground),
            typeof(Brush),
            typeof(CaptureActionCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ShortcutForegroundProperty =
        DependencyProperty.Register(
            nameof(ShortcutForeground),
            typeof(Brush),
            typeof(CaptureActionCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ShortcutTextProperty =
        DependencyProperty.Register(
            nameof(ShortcutText),
            typeof(string),
            typeof(CaptureActionCard),
            new PropertyMetadata(string.Empty));

    public CaptureActionCard()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? Click;

    public Icon Icon
    {
        get => (Icon)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public Brush? SecondaryForeground
    {
        get => (Brush?)GetValue(SecondaryForegroundProperty);
        set => SetValue(SecondaryForegroundProperty, value);
    }

    public Brush? IconBackground
    {
        get => (Brush?)GetValue(IconBackgroundProperty);
        set => SetValue(IconBackgroundProperty, value);
    }

    public Brush? IconBorderBrush
    {
        get => (Brush?)GetValue(IconBorderBrushProperty);
        set => SetValue(IconBorderBrushProperty, value);
    }

    public Brush? IconForeground
    {
        get => (Brush?)GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    public Brush? ShortcutForeground
    {
        get => (Brush?)GetValue(ShortcutForegroundProperty);
        set => SetValue(ShortcutForegroundProperty, value);
    }

    public string ShortcutText
    {
        get => (string)GetValue(ShortcutTextProperty);
        set => SetValue(ShortcutTextProperty, value);
    }

    private void OnActionButtonClick(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, e);
    }
}
