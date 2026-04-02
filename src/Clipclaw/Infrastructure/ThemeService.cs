using System.Windows;
using System.Windows.Media;
using Clipclaw.Models;
using MaterialDesignThemes.Wpf;

namespace Clipclaw.Infrastructure;

/// <summary>
/// Applies a <see cref="ClipTheme"/> to the running application.
/// Updates both the MaterialDesign base palette and the custom
/// DynamicResource brushes used by our hand-crafted XAML styles.
/// </summary>
internal static class ThemeService
{
    public static void Apply(ClipTheme theme)
    {
        var isDark = theme == ClipTheme.Dark;

        // MaterialDesign base theme switch — updates all MD control styles live.
        var paletteHelper = new PaletteHelper();
        var mdTheme       = paletteHelper.GetTheme();
        mdTheme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(mdTheme);

        // Update our custom DynamicResource brushes so the hand-styled panel and
        // settings window also pick up the new theme without an app restart.
        var r = Application.Current.Resources;

        if (isDark)
        {
            r["ClipPanelBackground"]       = Brush(0x16, 0x16, 0x1F);
            r["ClipPanelBorderBrush"]      = Brush(0x2A, 0x2A, 0x3A);
            r["ClipPanelInnerBorderBrush"] = Brush(0x25, 0x25, 0x35);
            r["ClipSearchBackground"]      = Brush(0x20, 0x20, 0x2E);
            r["ClipSearchBorderBrush"]     = Brush(0x30, 0x30, 0x48);
            r["ClipRowForeground"]         = Brush(0xE2, 0xE2, 0xEE);
            r["ClipRowSelectedBackground"] = Brush(0x38, 0x38, 0x58);
            r["ClipRowSelectedForeground"] = Brush(0xF0, 0xF0, 0xFA);
            r["ClipRowHoverBackground"]    = Brush(0x24, 0x24, 0x36);
            r["ClipRowAlternateBackground"]= BrushA(0x12, 0xFF, 0xFF, 0xFF);
            r["ClipMutedForeground"]       = Brush(0x6B, 0x6B, 0x8A);
            r["ClipPlaceholderForeground"] = Brush(0x3A, 0x3A, 0x5A);
            r["ClipDividerBrush"]          = Brush(0x25, 0x25, 0x35);
            r["ClipBadgeBackground"]       = Brush(0x1E, 0x1E, 0x2E);
            r["ClipBadgeForeground"]       = Brush(0x4A, 0x4A, 0x6A);
            r["ClipToggleTrack"]           = Brush(0x2A, 0x2A, 0x3A);
            r["ClipToggleThumb"]           = Brush(0x5A, 0x5A, 0x7A);
        }
        else
        {
            r["ClipPanelBackground"]       = Brush(0xF8, 0xF8, 0xFC);
            r["ClipPanelBorderBrush"]      = Brush(0xC8, 0xC8, 0xDC);
            r["ClipPanelInnerBorderBrush"] = Brush(0xD8, 0xD8, 0xEC);
            r["ClipSearchBackground"]      = Brush(0xEE, 0xEE, 0xF8);
            r["ClipSearchBorderBrush"]     = Brush(0xB8, 0xB8, 0xD0);
            r["ClipRowForeground"]         = Brush(0x1A, 0x1A, 0x2A);
            r["ClipRowSelectedBackground"] = Brush(0xA8, 0xC8, 0xF0);
            r["ClipRowSelectedForeground"] = Brush(0x0A, 0x0A, 0x1A);
            r["ClipRowHoverBackground"]    = Brush(0xDA, 0xE8, 0xFA);
            r["ClipRowAlternateBackground"]= BrushA(0x0F, 0x00, 0x00, 0x00);
            r["ClipMutedForeground"]       = Brush(0x60, 0x60, 0x80);
            r["ClipPlaceholderForeground"] = Brush(0x90, 0x90, 0xB0);
            r["ClipDividerBrush"]          = Brush(0xD0, 0xD0, 0xE4);
            r["ClipBadgeBackground"]       = Brush(0xE4, 0xE4, 0xF4);
            r["ClipBadgeForeground"]       = Brush(0x70, 0x70, 0x90);
            r["ClipToggleTrack"]           = Brush(0xC0, 0xC0, 0xD4);
            r["ClipToggleThumb"]           = Brush(0x80, 0x80, 0xA0);
        }
    }

    private static SolidColorBrush Brush(byte r, byte g, byte b)
        => new(Color.FromRgb(r, g, b));

    private static SolidColorBrush BrushA(byte a, byte r, byte g, byte b)
        => new(Color.FromArgb(a, r, g, b));
}
