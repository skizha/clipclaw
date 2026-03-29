namespace Clipclaw.Models;

/// <summary>
/// Maps a named action to a keyboard combination.
/// ActionName values must come from <see cref="Clipclaw.Infrastructure.HotkeyConstants"/>.
/// </summary>
public sealed class ShortcutBinding
{
    public int    Id         { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string Modifiers  { get; set; } = string.Empty;
    public string Key        { get; set; } = string.Empty;
    public bool   IsEnabled  { get; set; } = true;

    /// <summary>Human-readable representation, e.g. "Win+Shift+V".</summary>
    public string DisplayText =>
        string.IsNullOrEmpty(Modifiers) ? Key : $"{Modifiers}+{Key}";
}
