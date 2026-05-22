using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lumiere.App;

public sealed partial class SettingsSectionHeader : UserControl
{
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(SettingsSectionHeader),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SettingsSectionHeader),
            new PropertyMetadata(string.Empty));

    public SettingsSectionHeader()
    {
        InitializeComponent();
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}
