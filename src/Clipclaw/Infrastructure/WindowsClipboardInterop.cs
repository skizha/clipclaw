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

    // ── Cursor position ──────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT point);

    // ── Focus management ─────────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    // ── Modifier key flag constants (for RegisterHotKey fsModifiers) ─────────
    public const uint ModAlt      = 0x0001;
    public const uint ModControl  = 0x0002;
    public const uint ModShift    = 0x0004;
    public const uint ModWin      = 0x0008;
    public const uint ModNoRepeat = 0x4000;

    // ── Native popup menu (for correct tray-icon menu positioning) ───────────

    [DllImport("user32.dll")]
    public static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AppendMenu(IntPtr hMenu, uint uFlags,
        UIntPtr uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    public static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags,
        int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyMenu(IntPtr hMenu);

    // AppendMenu flag values
    public const uint MfString    = 0x00000000;
    public const uint MfSeparator = 0x00000800;

    // TrackPopupMenu flag values
    public const uint TpmBottomAlign = 0x0020;
    public const uint TpmReturnCmd   = 0x0100;
    public const uint TpmNoNotify    = 0x0080;
    public const uint TpmRightButton = 0x0002;

    // ── SendInput — keyboard simulation ──────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint       type;
        public InputUnion U;
        public static int Size => Marshal.SizeOf<INPUT>();
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT   ki;
        [FieldOffset(0)] public MOUSEINPUT   mi;   // keeps union size correct
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint   dwFlags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int    dx, dy, mouseData;
        public uint   dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint   uMsg;
        public ushort wParamL, wParamH;
    }

    private const uint   INPUT_KEYBOARD   = 1;
    private const uint   KEYEVENTF_KEYUP  = 0x0002;
    private const ushort VK_SHIFT         = 0x10;
    private const ushort VK_CONTROL       = 0x11;
    private const ushort VK_V             = 0x56;

    /// <summary>
    /// Simulates Ctrl+V on the currently focused window.
    /// Releases Shift first so that a Shift-modified hotkey trigger
    /// (e.g. Ctrl+Shift+1) does not produce Ctrl+Shift+V in the target app.
    /// </summary>
    public static void SimulatePaste()
    {
        var inputs = new INPUT[]
        {
            Key(VK_SHIFT,   KEYEVENTF_KEYUP), // release Shift held by hotkey trigger
            Key(VK_CONTROL, 0),               // Ctrl down
            Key(VK_V,       0),               // V down  → Ctrl+V
            Key(VK_V,       KEYEVENTF_KEYUP), // V up
            Key(VK_CONTROL, KEYEVENTF_KEYUP), // Ctrl up
        };
        SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }

    private static INPUT Key(ushort vk, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        U    = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } },
    };
}
