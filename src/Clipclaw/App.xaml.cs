using System.IO;
using System.Windows;
using Clipclaw.Infrastructure;
using Clipclaw.Services;
using Clipclaw.ViewModels;
using Clipclaw.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;

namespace Clipclaw;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private TaskbarIcon?      _trayIcon;
    private ClipboardPanel?   _panel;
    private SettingsWindow?   _settingsWindow;
    private IntPtr            _previousForegroundWindow;

    // ── Startup ──────────────────────────────────────────────────────────────

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SetupUnhandledExceptionHandlers();
        Services = BuildServiceProvider();

        var persistence = Services.GetRequiredService<IPersistenceService>();
        await persistence.InitialiseAsync();

        var settings = await persistence.GetSettingsAsync();
        StartupService.Apply(settings.LaunchOnStartup);

        SetupTrayIcon();
        SetupHotkeys();
        SetupClipboardMonitoring();
        SetupSessionEvents();
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IPersistenceService,  SqlitePersistenceService>();
        services.AddSingleton<IClipboardService,    ClipboardService>();
        services.AddSingleton<IHotkeyService,       HotkeyService>();
        services.AddSingleton<IUsageTrackingService, UsageTrackingService>();

        services.AddTransient<PanelViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    // ── Tray icon ────────────────────────────────────────────────────────────

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            IconSource  = new System.Windows.Media.Imaging.BitmapImage(
                              new Uri("pack://application:,,,/Assets/Icons/tray.ico")),
            ToolTipText = "Clipclaw",
        };

        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowPanel();

        // Handle right-click manually so the menu appears near the tray icon
        // on any monitor, not on Hardcodet's internal helper window.
        _trayIcon.TrayRightMouseUp += (_, _) => ShowTrayContextMenu();
    }

    private void ShowTrayContextMenu()
    {
        var open = new System.Windows.Controls.MenuItem { Header = "Open Clipclaw" };
        open.Click += (_, _) => ShowPanel();

        var settings = new System.Windows.Controls.MenuItem { Header = "Settings…" };
        settings.Click += (_, _) => ShowSettings();

        var exit = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exit.Click += (_, _) => ExitApplication();

        var menu = new System.Windows.Controls.ContextMenu
        {
            Items = { open, settings, new System.Windows.Controls.Separator(), exit }
        };

        // Position at the cursor — this is the tray icon location, correct on any monitor.
        // AbsolutePoint + corner of screen → WPF auto-flips the menu above the taskbar.
        WindowsClipboardInterop.GetCursorPos(out var cursor);
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
        menu.HorizontalOffset = cursor.X;
        menu.VerticalOffset   = cursor.Y;
        menu.IsOpen = true;
    }

    // ── Hotkeys ───────────────────────────────────────────────────────────────

    private void SetupHotkeys()
    {
        var hotkeys = Services.GetRequiredService<IHotkeyService>();
        hotkeys.HotkeyPressed += OnHotkeyPressed;
        hotkeys.RegisterFromDatabaseAsync();
    }

    private void OnHotkeyPressed(string actionName)
    {
        if (actionName == HotkeyConstants.ShowPanel)
        {
            TogglePanel();
            return;
        }

        // PasteItem_1 … PasteItem_5 → paste by 0-based index
        var index = HotkeyConstants.PasteItemActions.IndexOf(actionName);
        if (index >= 0)
        {
            var clipboard = Services.GetRequiredService<IClipboardService>();
            clipboard.SetActiveClipboard(index);
        }
    }

    // ── Clipboard monitoring ─────────────────────────────────────────────────

    private void SetupClipboardMonitoring()
    {
        // A hidden window is required for AddClipboardFormatListener.
        // EnsureHandle() creates the HWND without making the window visible.
        var messageHost = new Window
        {
            Width = 0, Height = 0,
            WindowStyle   = WindowStyle.None,
            ShowInTaskbar = false,
        };
        new System.Windows.Interop.WindowInteropHelper(messageHost).EnsureHandle();

        var clipboard = Services.GetRequiredService<IClipboardService>();
        clipboard.StartMonitoring(messageHost);
    }

    // ── Session events ───────────────────────────────────────────────────────

    private void SetupSessionEvents()
    {
        Microsoft.Win32.SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    private void OnSessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
    {
        var hotkeys = Services.GetRequiredService<IHotkeyService>();

        var lockEvents = new[]
        {
            Microsoft.Win32.SessionSwitchReason.SessionLock,
            Microsoft.Win32.SessionSwitchReason.ConsoleDisconnect,
            Microsoft.Win32.SessionSwitchReason.RemoteDisconnect,
        };

        var unlockEvents = new[]
        {
            Microsoft.Win32.SessionSwitchReason.SessionUnlock,
            Microsoft.Win32.SessionSwitchReason.ConsoleConnect,
            Microsoft.Win32.SessionSwitchReason.RemoteConnect,
        };

        if (Array.Exists(lockEvents, r => r == e.Reason))
        {
            // Close the panel and unregister hotkeys when screen locks
            Dispatcher.Invoke(() => _panel?.Hide());
            hotkeys.UnregisterAll();
        }
        else if (Array.Exists(unlockEvents, r => r == e.Reason))
        {
            hotkeys.RegisterFromDatabaseAsync();
        }
    }

    // ── Panel show / hide ────────────────────────────────────────────────────

    public void ShowPanel()
    {
        // Remember which window had focus so we can restore it after paste
        _previousForegroundWindow = WindowsClipboardInterop.GetForegroundWindow();

        if (_panel is null || !_panel.IsLoaded)
        {
            _panel = new ClipboardPanel(Services.GetRequiredService<PanelViewModel>());
            _panel.PasteRequested += OnPanelPasteRequested;
        }

        _panel.OpenAndLoadAsync();
    }

    public void HidePanel()
    {
        _panel?.Hide();

        // Return focus to the app that was active before the panel opened
        if (_previousForegroundWindow != IntPtr.Zero)
            WindowsClipboardInterop.SetForegroundWindow(_previousForegroundWindow);
    }

    private void TogglePanel()
    {
        if (_panel is { IsVisible: true })
            HidePanel();
        else
            ShowPanel();
    }

    private void OnPanelPasteRequested(object? sender, EventArgs e) => HidePanel();

    // ── Settings ──────────────────────────────────────────────────────────────

    public void ShowSettings()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(Services.GetRequiredService<SettingsViewModel>());
        _settingsWindow.Show();
    }

    // ── Exit ──────────────────────────────────────────────────────────────────

    private async void ExitApplication()
    {
        var persistence = Services.GetRequiredService<IPersistenceService>();
        var settings    = await persistence.GetSettingsAsync();

        if (!settings.PersistHistory)
            await persistence.ClearNonPinnedAsync();

        // Release Win32 resources before shutdown
        Services.GetRequiredService<IClipboardService>().StopMonitoring();
        Services.GetRequiredService<IHotkeyService>().UnregisterAll();
        Microsoft.Win32.SystemEvents.SessionSwitch -= OnSessionSwitch;

        _trayIcon?.Dispose();
        Shutdown();
    }

    // ── Unhandled exceptions ──────────────────────────────────────────────────

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Clipclaw", "clipclaw.log");

    private void SetupUnhandledExceptionHandlers()
    {
        // Catch fatal exceptions on background threads (would otherwise kill the process)
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            WriteLog("FATAL", (Exception)e.ExceptionObject);

        // Catch exceptions on the WPF dispatcher thread
        DispatcherUnhandledException += (_, e) =>
        {
            WriteLog("ERROR", e.Exception);
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nSee {LogPath} for details.",
                "Clipclaw Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        };
    }

    private static void WriteLog(string level, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level}: {ex}\n\n");
        }
        catch { /* logging must never crash the app */ }
    }
}
