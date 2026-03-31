namespace Clipclaw.Models;

/// <summary>
/// The two available colour themes.
/// Stored as a string in SQLite; parsed back to this enum on read.
/// </summary>
public enum ClipTheme
{
    Dark,
    Light,
}

/// <summary>
/// User preferences. There is exactly one instance of this in the database.
/// </summary>
public sealed class AppSettings
{
    public int       Id               { get; set; } = 1;
    public int       MaxHistorySize   { get; set; } = 50;
    public bool      LaunchOnStartup  { get; set; } = true;
    public bool      PersistHistory   { get; set; } = true;
    public string    PanelShortcut    { get; set; } = "Ctrl+Shift+C";
    public ClipTheme Theme            { get; set; } = ClipTheme.Dark;
}
