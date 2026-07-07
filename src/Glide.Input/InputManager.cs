using Glide.Common;
using Glide.Settings;

namespace Glide.Input;

/// <summary>
/// Owns the low-level keyboard and mouse hooks on a dedicated message-pump
/// thread and translates raw events into zoom/pan intents.
/// All callbacks fire on the hook thread and must return quickly.
/// </summary>
public sealed class InputManager : IDisposable
{
    private const int DoubleTapWindowMs = 400;

    // --- Wiring (set once at startup by the composition root) ---

    /// <summary>Wheel scrolled while the modifier is held. Args: ticks, x, y. Return true to swallow.</summary>
    public Func<double, int, int, bool>? ZoomTick;

    /// <summary>The zoom modifier was released.</summary>
    public Action? ModifierReleased;

    /// <summary>The zoom modifier was pressed twice quickly.</summary>
    public Action? ModifierDoubleTapped;

    /// <summary>Middle button pressed/released for panning. Return true to swallow.</summary>
    public Func<bool, int, int, bool>? PanButton;

    /// <summary>Raw mouse movement (physical pixels).</summary>
    public Action<int, int>? MouseMoved;

    /// <summary>Fast query: is any zoom session currently active?</summary>
    public Func<bool>? IsZoomActive;

    // --- Configuration (updated from the UI thread) ---

    private volatile ModifierConfig _config = ModifierConfig.For(ModifierKey.Ctrl, true);

    // --- Hook state (touched only on the hook thread) ---

    private Thread? _thread;
    private uint _threadId;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private HookNative.HookProc? _keyboardProc;
    private HookNative.HookProc? _mouseProc;
    private readonly ManualResetEventSlim _started = new(false);

    private bool _modifierDown;
    private bool _wheelUsedDuringHold;
    private long _lastModifierDownAt;

    private sealed record ModifierConfig(
        ModifierKey Key,
        bool DoubleTapReset,
        HashSet<uint> VkCodes,
        bool IsMouseButton)
    {
        public static ModifierConfig For(ModifierKey key, bool doubleTapReset)
        {
            HashSet<uint> vk = key switch
            {
                ModifierKey.Ctrl => [0xA2, 0xA3, 0x11],
                ModifierKey.Alt => [0xA4, 0xA5, 0x12],
                ModifierKey.Shift => [0xA0, 0xA1, 0x10],
                ModifierKey.Win => [0x5B, 0x5C],
                _ => [],
            };
            return new ModifierConfig(key, doubleTapReset, vk, vk.Count == 0);
        }
    }

    public void Configure(ModifierKey modifier, bool doubleTapReset) =>
        _config = ModifierConfig.For(modifier, doubleTapReset);

    public void Start()
    {
        if (_thread is not null) return;
        _thread = new Thread(HookThreadMain)
        {
            IsBackground = true,
            Name = "Glide.Input",
            Priority = ThreadPriority.Highest,
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _started.Wait(TimeSpan.FromSeconds(3));
    }

    public void Stop()
    {
        if (_thread is null) return;
        HookNative.PostThreadMessageW(_threadId, HookNative.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _started.Reset();
    }

    public void Dispose() => Stop();

    private void HookThreadMain()
    {
        _threadId = HookNative.GetCurrentThreadId();
        _keyboardProc = KeyboardHookProc;
        _mouseProc = MouseHookProc;
        var module = HookNative.GetModuleHandleW(null);

        _keyboardHook = HookNative.SetWindowsHookExW(HookNative.WH_KEYBOARD_LL, _keyboardProc, module, 0);
        _mouseHook = HookNative.SetWindowsHookExW(HookNative.WH_MOUSE_LL, _mouseProc, module, 0);
        if (_keyboardHook == IntPtr.Zero || _mouseHook == IntPtr.Zero)
            Log.Error("Failed to install low-level hooks");

        _started.Set();
        while (HookNative.GetMessageW(out _, IntPtr.Zero, 0, 0) > 0)
        {
            // Low-level hooks are serviced during GetMessage; nothing to do here.
        }

        if (_keyboardHook != IntPtr.Zero) HookNative.UnhookWindowsHookEx(_keyboardHook);
        if (_mouseHook != IntPtr.Zero) HookNative.UnhookWindowsHookEx(_mouseHook);
        _keyboardHook = _mouseHook = IntPtr.Zero;
    }

    // --- Keyboard ---

    private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return HookNative.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

        var data = System.Runtime.InteropServices.Marshal.PtrToStructure<HookNative.KBDLLHOOKSTRUCT>(lParam);
        if ((data.Flags & HookNative.LLKHF_INJECTED) == 0)
        {
            int msg = (int)wParam;
            bool isDown = msg is HookNative.WM_KEYDOWN or HookNative.WM_SYSKEYDOWN;
            bool isUp = msg is HookNative.WM_KEYUP or HookNative.WM_SYSKEYUP;
            var cfg = _config;
            if ((isDown || isUp) && cfg.VkCodes.Contains(data.VkCode))
                OnModifierKeyEvent(cfg, isDown);
        }

        return HookNative.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private void OnModifierKeyEvent(ModifierConfig cfg, bool isDown)
    {
        if (isDown)
        {
            if (_modifierDown) return; // key auto-repeat
            _modifierDown = true;
            _wheelUsedDuringHold = false;

            var now = Environment.TickCount64;
            if (cfg.DoubleTapReset
                && now - _lastModifierDownAt < DoubleTapWindowMs
                && IsZoomActive?.Invoke() == true)
            {
                ModifierDoubleTapped?.Invoke();
            }
            _lastModifierDownAt = now;
        }
        else if (_modifierDown)
        {
            _modifierDown = false;
            if (cfg.Key == ModifierKey.Win && _wheelUsedDuringHold)
                KeySender.SendCtrlTap();
            ModifierReleased?.Invoke();
        }
    }

    // --- Mouse ---

    private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return HookNative.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

        var data = System.Runtime.InteropServices.Marshal.PtrToStructure<HookNative.MSLLHOOKSTRUCT>(lParam);
        if ((data.Flags & HookNative.LLMHF_INJECTED) == 0 && HandleMouseEvent((int)wParam, in data))
            return 1; // swallow

        return HookNative.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private bool HandleMouseEvent(int msg, in HookNative.MSLLHOOKSTRUCT data)
    {
        switch (msg)
        {
            case HookNative.WM_MOUSEMOVE:
                MouseMoved?.Invoke(data.Pt.X, data.Pt.Y);
                return false;

            case HookNative.WM_MOUSEWHEEL:
                return HandleWheel(in data);

            case HookNative.WM_MBUTTONDOWN:
                return PanButton?.Invoke(true, data.Pt.X, data.Pt.Y) == true;

            case HookNative.WM_MBUTTONUP:
                return PanButton?.Invoke(false, data.Pt.X, data.Pt.Y) == true;

            case HookNative.WM_XBUTTONDOWN:
            case HookNative.WM_XBUTTONUP:
                return HandleXButton(msg == HookNative.WM_XBUTTONDOWN, in data);

            default:
                return false;
        }
    }

    private bool HandleWheel(in HookNative.MSLLHOOKSTRUCT data)
    {
        if (!_modifierDown) return false;

        double ticks = unchecked((short)(data.MouseData >> 16)) / 120.0;
        bool handled = ZoomTick?.Invoke(ticks, data.Pt.X, data.Pt.Y) == true;
        _wheelUsedDuringHold |= handled;
        return handled;
    }

    private bool HandleXButton(bool isDown, in HookNative.MSLLHOOKSTRUCT data)
    {
        var cfg = _config;
        int button = unchecked((short)(data.MouseData >> 16)); // 1 = XButton1, 2 = XButton2
        bool isConfigured =
            (cfg.Key == ModifierKey.XButton1 && button == 1) ||
            (cfg.Key == ModifierKey.XButton2 && button == 2);
        if (!isConfigured) return false;

        OnModifierKeyEvent(cfg, isDown);
        // The side button is reserved as the zoom modifier: swallow both
        // edges so the browser/app never sees a half-click.
        return true;
    }
}
