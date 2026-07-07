using System.Text;

namespace Glide.Engine;

/// <summary>
/// Resolves the process name of the foreground window, cached briefly so the
/// hook thread never pays the lookup cost on every wheel tick.
/// </summary>
public sealed class ForegroundProcessProvider
{
    private const int CacheMs = 500;

    private long _cachedAt;
    private IntPtr _cachedHwnd;
    private string? _cachedName;

    public string? GetForegroundProcessName()
    {
        var hwnd = EngineNative.GetForegroundWindow();
        var now = Environment.TickCount64;
        if (hwnd == _cachedHwnd && now - _cachedAt < CacheMs)
            return _cachedName;

        _cachedHwnd = hwnd;
        _cachedAt = now;
        _cachedName = Resolve(hwnd);
        return _cachedName;
    }

    private static string? Resolve(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;
        EngineNative.GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0) return null;

        var handle = EngineNative.OpenProcess(
            EngineNative.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (handle == IntPtr.Zero) return null;

        try
        {
            var buffer = new StringBuilder(1024);
            uint size = (uint)buffer.Capacity;
            if (!EngineNative.QueryFullProcessImageNameW(handle, 0, buffer, ref size))
                return null;
            return Path.GetFileName(buffer.ToString(0, (int)size));
        }
        finally
        {
            EngineNative.CloseHandle(handle);
        }
    }
}
