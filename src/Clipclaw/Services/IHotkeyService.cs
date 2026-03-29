namespace Clipclaw.Services;

public interface IHotkeyService
{
    /// <summary>Raised on the UI thread when a registered hotkey is pressed.</summary>
    event Action<string> HotkeyPressed;

    /// <summary>Loads bindings from the database and registers them all.</summary>
    void RegisterFromDatabaseAsync();

    void UnregisterAll();
}
