using System.Runtime.InteropServices;

namespace Clipclaw.Infrastructure;

/// <summary>
/// All Win32 P/Invoke declarations used by Clipclaw.
/// No other file may import user32.dll directly.
/// </summary>
internal static class WindowsClipboardInterop
{
    // ── Clipboard listener ───────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    // ── Hotkey registration ──────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // ── Focus management ─────────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    // ── Modifier key flag constants (for RegisterHotKey fsModifiers) ─────────
    public const uint ModAlt     = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift   = 0x0004;
    public const uint ModWin     = 0x0008;
    public const uint ModNoRepeat = 0x4000;
}
