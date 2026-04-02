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
/// Returns true when the AlternationIndex is odd. Used to apply alternating row
/// backgrounds via a DataTrigger in the ClipRow style — works with any AlternationCount.
/// </summary>
[ValueConversion(typeof(int), typeof(bool))]
internal sealed class IndexToOddConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int index && index % 2 == 1;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns Visible when the bound bool is true, Collapsed otherwise.
/// Used to show/hide the slot-assigned shortcut TextBlock in the panel.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
internal sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Formats a copy count integer for display in a row badge.
/// Returns the count as a string, or "999+" when the count would overflow the badge.
/// </summary>
[ValueConversion(typeof(int), typeof(string))]
internal sealed class CopyCountDisplayConverter : IValueConverter
{
    private const int Cap = 999;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count ? (count > Cap ? $"{Cap}+" : count.ToString()) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
