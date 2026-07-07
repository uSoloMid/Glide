using Glide.Common;

namespace Glide.Graphics;

/// <summary>
/// Borderless, topmost, click-through window that covers one monitor and
/// hosts the zoom swap chain. It is excluded from desktop capture so the
/// duplicated frames never contain the zoom itself (no hall-of-mirrors).
/// Must be created, pumped and destroyed on the same thread.
/// </summary>
public sealed class OverlayWindow : IDisposable
{
    private const string WindowClassName = "GlideZoomOverlay";

    private static readonly object ClassLock = new();
    private static OverlayNative.WndProc? s_wndProc; // rooted for the process lifetime
    private static bool s_classRegistered;

    public IntPtr Hwnd { get; private set; }
    public bool IsVisible { get; private set; }

    public OverlayWindow(RectI bounds)
    {
        EnsureWindowClass();

        Hwnd = OverlayNative.CreateWindowExW(
            OverlayNative.WS_EX_TOPMOST | OverlayNative.WS_EX_TRANSPARENT |
            OverlayNative.WS_EX_LAYERED | OverlayNative.WS_EX_TOOLWINDOW |
            OverlayNative.WS_EX_NOACTIVATE,
            WindowClassName,
            "Glide Zoom",
            OverlayNative.WS_POPUP,
            bounds.X, bounds.Y, bounds.Width, bounds.Height,
            IntPtr.Zero, IntPtr.Zero,
            OverlayNative.GetModuleHandleW(null), IntPtr.Zero);

        if (Hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create the overlay window.");

        OverlayNative.SetLayeredWindowAttributes(Hwnd, 0, 255, OverlayNative.LWA_ALPHA);

        // Critical: keep our own output out of the capture pipeline.
        if (!OverlayNative.SetWindowDisplayAffinity(Hwnd, OverlayNative.WDA_EXCLUDEFROMCAPTURE))
            Log.Error("SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE) failed — requires Windows 10 2004+");
    }

    private static void EnsureWindowClass()
    {
        lock (ClassLock)
        {
            if (s_classRegistered) return;
            s_wndProc = static (hWnd, msg, wParam, lParam) =>
                OverlayNative.DefWindowProcW(hWnd, msg, wParam, lParam);

            var cls = new OverlayNative.WNDCLASSEX
            {
                Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<OverlayNative.WNDCLASSEX>(),
                WndProc = s_wndProc,
                Instance = OverlayNative.GetModuleHandleW(null),
                ClassName = WindowClassName,
            };
            if (OverlayNative.RegisterClassExW(ref cls) == 0)
                throw new InvalidOperationException("Failed to register the overlay window class.");
            s_classRegistered = true;
        }
    }

    public void Show()
    {
        OverlayNative.ShowWindow(Hwnd, OverlayNative.SW_SHOWNOACTIVATE);
        OverlayNative.SetWindowPos(
            Hwnd, OverlayNative.HWND_TOPMOST, 0, 0, 0, 0,
            OverlayNative.SWP_NOACTIVATE | 0x0001 /*NOSIZE*/ | 0x0002 /*NOMOVE*/);
        IsVisible = true;
    }

    public void Hide()
    {
        OverlayNative.ShowWindow(Hwnd, OverlayNative.SW_HIDE);
        IsVisible = false;
    }

    /// <summary>Drains pending window messages. Call once per frame.</summary>
    public void Pump()
    {
        while (OverlayNative.PeekMessageW(out var msg, Hwnd, 0, 0, OverlayNative.PM_REMOVE))
        {
            OverlayNative.TranslateMessage(ref msg);
            OverlayNative.DispatchMessageW(ref msg);
        }
    }

    public void Dispose()
    {
        if (Hwnd != IntPtr.Zero)
        {
            OverlayNative.DestroyWindow(Hwnd);
            Hwnd = IntPtr.Zero;
        }
    }
}
