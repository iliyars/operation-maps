using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OperationMaps.Wpf.Shared.Controls;

/// <summary>
/// Two well-known WindowChrome quirks, both fixed by hooking the window's
/// message loop once, right after construction (before SourceInitialized
/// fires — a Loaded-time hook is too late).
/// </summary>
public static class WindowChromeFixes
{
  public static void Apply(Window window)
  {
    window.SourceInitialized += (_, _) =>
    {
      var handle = new WindowInteropHelper(window).Handle;

      RequestRoundedCorners(handle);

      HwndSource.FromHwnd(handle)?.AddHook(
          (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
              => WindowProc(window, hwnd, msg, lParam, ref handled));
    };
  }

  // ── Fix 1: maximize covering the taskbar ──────────────────────────────────
  //
  // A chrome-less, WindowChrome-styled window that gets maximized normally
  // (via WPF's own logic) ends up sized to the WHOLE MONITOR rather than its
  // usable work area — covering the taskbar and extending past the visible
  // screen on every edge. The fix is to intercept WM_GETMINMAXINFO and
  // answer it with the monitor's real work area ourselves — this is what a
  // normal (non-chrome-less) window gets for free from Windows, and there's
  // no WPF-only equivalent.

  private const int WM_GETMINMAXINFO = 0x0024;
  private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

  //
  // Marking the message handled also skips WPF's OWN WM_GETMINMAXINFO
  // handling — which is what tells Windows about MinWidth/MinHeight. Without
  // re-supplying the minimum here, Windows let the window be dragged
  // narrower than MinWidth while WPF still laid the content out at MinWidth,
  // so the right edge of every screen was silently cut off.

  private static IntPtr WindowProc(Window window, IntPtr hwnd, int msg, IntPtr lParam, ref bool handled)
  {
    if (msg == WM_GETMINMAXINFO)
    {
      SetMaxSizeToWorkArea(window, hwnd, lParam);
      handled = true;
    }

    return IntPtr.Zero;
  }

  private static void SetMaxSizeToWorkArea(Window window, IntPtr hwnd, IntPtr lParam)
  {
    var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

    // MinWidth/MinHeight are in device-independent units; Windows wants physical pixels.
    var dpi = VisualTreeHelper.GetDpi(window);
    if (window.MinWidth > 0)
      mmi.ptMinTrackSize.X = (int)Math.Ceiling(window.MinWidth * dpi.DpiScaleX);
    if (window.MinHeight > 0)
      mmi.ptMinTrackSize.Y = (int)Math.Ceiling(window.MinHeight * dpi.DpiScaleY);

    var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
    if (monitor != IntPtr.Zero)
    {
      var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
      GetMonitorInfo(monitor, ref monitorInfo);

      var workArea = monitorInfo.rcWork;
      var monitorArea = monitorInfo.rcMonitor;

      mmi.ptMaxPosition.X = Math.Abs(workArea.Left - monitorArea.Left);
      mmi.ptMaxPosition.Y = Math.Abs(workArea.Top - monitorArea.Top);
      mmi.ptMaxSize.X = Math.Abs(workArea.Right - workArea.Left);
      mmi.ptMaxSize.Y = Math.Abs(workArea.Bottom - workArea.Top);
    }

    Marshal.StructureToPtr(mmi, lParam, true);
  }

  [DllImport("user32.dll")]
  private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

  [DllImport("user32.dll")]
  private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

  [StructLayout(LayoutKind.Sequential)]
  private struct POINT
  {
    public int X;
    public int Y;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct RECT
  {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct MINMAXINFO
  {
    public POINT ptReserved;
    public POINT ptMaxSize;
    public POINT ptMaxPosition;
    public POINT ptMinTrackSize;
    public POINT ptMaxTrackSize;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct MONITORINFO
  {
    public int cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;
  }

  // ── Fix 2: square corners on Windows 11 ───────────────────────────────────
  //
  // Windows 11 rounds every top-level window's corners automatically —
  // EXCEPT a WindowChrome-customized one, which DWM otherwise treats as a
  // bare rectangle. WindowChrome.CornerRadius draws WPF's OWN rounded
  // corners (a different, unrelated feature); getting the real, native
  // Win11 corner treatment back requires explicitly asking DWM for it.
  // Silently does nothing pre-Win11 (DwmSetWindowAttribute just fails).

  private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
  private const int DWMWCP_ROUND = 2;

  private static void RequestRoundedCorners(IntPtr hwnd)
  {
    int preference = DWMWCP_ROUND;
    _ = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
  }

  [DllImport("dwmapi.dll", PreserveSig = true)]
  private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
