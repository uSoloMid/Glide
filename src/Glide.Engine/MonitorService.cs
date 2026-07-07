using Glide.Common;

namespace Glide.Engine;

/// <summary>One physical display.</summary>
public sealed record MonitorInfo(string DeviceName, RectI Bounds, bool IsPrimary);

/// <summary>Enumerates monitors and maps points to monitors.</summary>
public static class MonitorService
{
    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();

        bool Callback(IntPtr hMonitor, IntPtr hdc, ref EngineNative.RECT rect, IntPtr data)
        {
            var info = new EngineNative.MONITORINFOEX
            {
                Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<EngineNative.MONITORINFOEX>(),
            };
            if (EngineNative.GetMonitorInfoW(hMonitor, ref info))
            {
                var b = info.Monitor;
                monitors.Add(new MonitorInfo(
                    info.DeviceName,
                    new RectI(b.Left, b.Top, b.Right - b.Left, b.Bottom - b.Top),
                    (info.Flags & EngineNative.MONITORINFOF_PRIMARY) != 0));
            }
            return true;
        }

        EngineNative.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero);
        return monitors;
    }

    public static MonitorInfo? FromPoint(IReadOnlyList<MonitorInfo> monitors, int x, int y)
    {
        foreach (var monitor in monitors)
        {
            if (monitor.Bounds.Contains(x, y))
                return monitor;
        }
        return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
    }

    public static (int X, int Y) GetCursorPosition()
    {
        EngineNative.GetCursorPos(out var point);
        return (point.X, point.Y);
    }
}
