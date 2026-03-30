using System.Windows.Input;
using Clipclaw.Infrastructure;
using NHotkey;
using NHotkey.Wpf;

namespace Clipclaw.Services;

/// <summary>
/// Registers global keyboard shortcuts using NHotkey and raises
/// <see cref="HotkeyPressed"/> with the matching action name.
/// Validates bindings against <see cref="HotkeyConstants.ReservedCombinations"/>
/// before registering to prevent conflicts with Windows system shortcuts.
/// </summary>
internal sealed class HotkeyService : IHotkeyService
{
    private readonly IPersistenceService _persistence;
    private readonly List<string> _registeredActions = [];

    public event Action<string>? HotkeyPressed;

    public HotkeyService(IPersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async void RegisterFromDatabaseAsync()
    {
        UnregisterAll();

        var bindings = await _persistence.GetShortcutBindingsAsync();

        foreach (var binding in bindings.Where(b => b.IsEnabled))
        {
            if (HotkeyConstants.IsReserved(binding.Modifiers, binding.Key))
                continue; // Skip reserved system shortcuts silently

            if (!TryParseBinding(binding.Modifiers, binding.Key,
                    out var modifiers, out var key))
                continue;

            try
            {
                HotkeyManager.Current.AddOrReplace(
                    binding.ActionName,
                    key,
                    modifiers,
                    OnNHotkeyPressed);

                _registeredActions.Add(binding.ActionName);
            }
            catch (HotkeyAlreadyRegisteredException)
            {
                // Another application owns this shortcut — skip it gracefully
            }
        }
    }

    public void UnregisterAll()
    {
        foreach (var action in _registeredActions)
        {
            try { HotkeyManager.Current.Remove(action); }
            catch { /* Already unregistered — ignore */ }
        }
        _registeredActions.Clear();
    }

    private void OnNHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        HotkeyPressed?.Invoke(e.Name);
        e.Handled = true;
    }

    // ── Parsing helpers ───────────────────────────────────────────────────────

    private static bool TryParseBinding(
        string modifierString, string keyString,
        out ModifierKeys modifiers, out Key key)
    {
        modifiers = ModifierKeys.None;
        key       = Key.None;

        foreach (var part in modifierString.Split('+', StringSplitOptions.RemoveEmptyEntries))
        {
            modifiers |= part.Trim().ToLowerInvariant() switch
            {
                "win"   or "windows" => ModifierKeys.Windows,
                "ctrl"  or "control" => ModifierKeys.Control,
                "alt"                => ModifierKeys.Alt,
                "shift"              => ModifierKeys.Shift,
                _ => ModifierKeys.None,
            };
        }

        // Key enum uses "D0"–"D9" for digit keys; stored bindings use bare "0"–"9".
        if (keyString.Length == 1 && char.IsDigit(keyString[0]))
            keyString = "D" + keyString;

        return Enum.TryParse(keyString, ignoreCase: true, out key) && key != Key.None;
    }
}
