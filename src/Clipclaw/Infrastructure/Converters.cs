using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Clipclaw.Infrastructure;

/// <summary>
/// Collapses a UI element when the bound integer count is zero.
/// Used to hide empty section headers in the clipboard panel.
/// </summary>
[ValueConversion(typeof(int), typeof(Visibility))]
internal sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Maps a 0-based AlternationIndex to a shortcut badge label for the first five
/// items in the recent list ("1"–"5"). Returns empty string for index ≥ 5 so the
/// badge collapses automatically when bound to Visibility via StringToVisibility.
/// </summary>
[ValueConversion(typeof(int), typeof(string))]
internal sealed class IndexToBadgeConverter : IValueConverter
{
    private static readonly string[] Badges = ["1", "2", "3", "4", "5"];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int index && index < Badges.Length ? Badges[index] : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Collapses an element when the bound string is null or empty.
/// Paired with IndexToBadgeConverter to hide badges beyond the first five items.
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
internal sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns Visible for AlternationIndex 0–4 (items with a Ctrl+Shift shortcut badge),
/// Collapsed for all higher indices.
/// </summary>
[ValueConversion(typeof(int), typeof(Visibility))]
internal sealed class IndexToVisibilityConverter : IValueConverter
{
    private const int BadgeCount = 5;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int index && index < BadgeCount ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
