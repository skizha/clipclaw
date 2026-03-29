namespace Clipclaw.Infrastructure;

/// <summary>
/// Single source of truth for all action names, Win32 message constants,
/// and Windows system shortcuts that must never be overridden.
/// No other file may use string literals for action names or Win32 constants.
/// </summary>
internal static class HotkeyConstants
{
    // ── Action names ────────────────────────────────────────────────────────
    public const string ShowPanel   = "ShowPanel";
    public const string PasteItem1  = "PasteItem_1";
    public const string PasteItem2  = "PasteItem_2";
    public const string PasteItem3  = "PasteItem_3";
    public const string PasteItem4  = "PasteItem_4";
    public const string PasteItem5  = "PasteItem_5";

    public static readonly List<string> PasteItemActions =
        [PasteItem1, PasteItem2, PasteItem3, PasteItem4, PasteItem5];

    // ── Win32 window message constants ──────────────────────────────────────
    public const int WmClipboardUpdate = 0x031D;
    public const int WmHotkey          = 0x0312;

    // ── Default key bindings ────────────────────────────────────────────────
    // Stored as (Modifiers, Key) pairs for seeding the database.
    public static readonly IReadOnlyDictionary<string, (string Modifiers, string Key)> DefaultBindings =
        new Dictionary<string, (string, string)>
        {
            [ShowPanel]  = ("Win+Shift", "V"),
            [PasteItem1] = ("Win+Shift", "1"),
            [PasteItem2] = ("Win+Shift", "2"),
            [PasteItem3] = ("Win+Shift", "3"),
            [PasteItem4] = ("Win+Shift", "4"),
            [PasteItem5] = ("Win+Shift", "5"),
        };

    // ── Windows system shortcuts that must never be assigned ────────────────
    // Any user-defined binding matching one of these must be rejected.
    public static readonly IReadOnlySet<string> ReservedCombinations = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "Win+C", "Win+V", "Win+X", "Win+D", "Win+E", "Win+L",
        "Win+R", "Win+S", "Win+I", "Win+A", "Win+Tab",
        "Ctrl+C", "Ctrl+V", "Ctrl+X", "Ctrl+Z", "Ctrl+Y",
        "Ctrl+A", "Ctrl+S", "Ctrl+P", "Ctrl+Alt+Del",
        "Alt+F4", "Alt+Tab", "Alt+Esc",
    };

    /// <summary>Returns true when the given combination is reserved by Windows.</summary>
    public static bool IsReserved(string modifiers, string key)
    {
        var combo = string.IsNullOrEmpty(modifiers) ? key : $"{modifiers}+{key}";
        return ReservedCombinations.Contains(combo);
    }
}
