using Microsoft.Win32;

namespace Clipclaw.Services;

/// <summary>
/// Manages the Windows registry entry that controls whether Clipclaw
/// launches automatically with the user's Windows session.
/// </summary>
internal static class StartupService
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName         = "Clipclaw";

    public static void Apply(bool launchOnStartup)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
        if (key is null) return;

        if (launchOnStartup)
        {
            var exePath = Environment.ProcessPath ?? string.Empty;
            key.SetValue(AppName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
