using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Clipclaw.Infrastructure;

/// <summary>
/// Hooks into a WPF window's HWND message pump via <see cref="HwndSource"/>
/// and forwards WM_CLIPBOARDUPDATE messages to a registered callback.
/// Must be attached after the window's HWND is created (i.e., after Show()).
/// </summary>
internal sealed class WindowMessageHandler : IDisposable
{
    private HwndSource? _hwndSource;
    private Action?     _onClipboardUpdate;

    public void Attach(Window window, Action onClipboardUpdate)
    {
        _onClipboardUpdate = onClipboardUpdate;

        var helper = new WindowInteropHelper(window);
        _hwndSource = HwndSource.FromHwnd(helper.Handle)
            ?? throw new InvalidOperationException(
                "Could not obtain HwndSource — ensure the window is visible before attaching.");

        _hwndSource.AddHook(WndProc);

        var registered = WindowsClipboardInterop.AddClipboardFormatListener(helper.Handle);
        if (!registered)
            throw new InvalidOperationException(
                $"AddClipboardFormatListener failed. Win32 error: {Marshal.GetLastWin32Error()}");
    }

    public void Detach()
    {
        if (_hwndSource is null) return;
        _hwndSource.RemoveHook(WndProc);

        WindowsClipboardInterop.RemoveClipboardFormatListener(_hwndSource.Handle);
        _hwndSource = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == HotkeyConstants.WmClipboardUpdate)
        {
            _onClipboardUpdate?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose() => Detach();
}
