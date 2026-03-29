using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Clipclaw.Models;
using Clipclaw.ViewModels;

namespace Clipclaw.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    // ── Shortcut key recorder ─────────────────────────────────────────────────

    private void ShortcutField_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox field)
            field.Text = "Press keys…";
    }

    private async void ShortcutField_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox field) return;
        if (field.Tag is not ShortcutBinding binding) return;

        // Ignore standalone modifier key presses — wait for the full combo
        if (e.Key is Key.LeftShift or Key.RightShift
                  or Key.LeftCtrl  or Key.RightCtrl
                  or Key.LeftAlt   or Key.RightAlt
                  or Key.LWin      or Key.RWin)
            return;

        e.Handled = true;

        var modifiers = BuildModifierString(e.KeyboardDevice.Modifiers);
        var keyName   = e.Key.ToString();

        var error = await _viewModel.TryApplyShortcutAsync(binding, modifiers, keyName);

        if (error is not null)
        {
            _viewModel.ConflictMessage = error;
            field.Text = binding.DisplayText; // Revert to old binding
        }
        else
        {
            _viewModel.ConflictMessage = string.Empty;
            field.Text = binding.DisplayText; // Show newly saved binding
        }

        // Move focus away so the field stops capturing keys
        MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
    }

    private static string BuildModifierString(ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt))     parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift))   parts.Add("Shift");
        return string.Join("+", parts);
    }

    // ── About tab hyperlink ───────────────────────────────────────────────────

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
