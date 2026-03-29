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
