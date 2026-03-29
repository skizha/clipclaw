namespace Clipclaw.Models;

/// <summary>
/// User preferences. There is exactly one instance of this in the database.
/// </summary>
public sealed class AppSettings
{
    public int    Id               { get; set; } = 1;
    public int    MaxHistorySize   { get; set; } = 50;
    public bool   LaunchOnStartup  { get; set; } = true;
    public bool   PersistHistory   { get; set; } = true;
    public string PanelShortcut    { get; set; } = "Win+Shift+V";
}
