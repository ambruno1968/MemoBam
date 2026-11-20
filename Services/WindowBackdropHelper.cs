using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MemoBam.Services;

/// <summary>
/// Win32 helpers for window Z-order, multi-monitor work area detection, and runtime layered opacity.
/// </summary>
public static class WindowBackdropHelper
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int LWA_ALPHA = 0x00000002;
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    private static readonly IntPtr HWND_BOTTOM = new(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    /// <summary>
    /// Returns the DPI-scaled WPF WorkArea for whichever monitor the window is currently on.
    /// </summary>
    public static Rect GetCurrentMonitorWorkArea(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return SystemParameters.WorkArea;

        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info))
        {
            return SystemParameters.WorkArea;
        }

        var source = PresentationSource.FromVisual(window);
        double scaleX = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
        double scaleY = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1.0;

        double left = info.rcWork.Left * scaleX;
        double top = info.rcWork.Top * scaleY;
        double width = (info.rcWork.Right - info.rcWork.Left) * scaleX;
        double height = (info.rcWork.Bottom - info.rcWork.Top) * scaleY;

        return new Rect(left, top, width, height);
    }

    /// <summary>
    /// Sets the window's opacity (0-255) at runtime using WS_EX_LAYERED.
    /// </summary>
    public static void SetWindowOpacity(Window window, byte alpha)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        if (alpha < 255)
        {
            if ((exStyle & WS_EX_LAYERED) == 0)
            {
                _ = SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);
            }
            _ = SetLayeredWindowAttributes(hwnd, 0, alpha, LWA_ALPHA);
        }
        else
        {
            if ((exStyle & WS_EX_LAYERED) != 0)
            {
                _ = SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);
                _ = SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_LAYERED);
            }
        }
    }

    public static void SendToBottom(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }
}
