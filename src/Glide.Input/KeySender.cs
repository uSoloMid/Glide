using System.Runtime.InteropServices;
using Glide.Common;

namespace Glide.Input;

/// <summary>Injects synthetic key events.</summary>
internal static class KeySender
{
    /// <summary>
    /// Sends a Ctrl press+release. Injected right before a Win key-up so
    /// Windows does not open the Start menu after a Win+wheel zoom.
    /// </summary>
    public static void SendCtrlTap()
    {
        var inputs = new HookNative.INPUT[2];
        inputs[0].Type = HookNative.INPUT_KEYBOARD;
        inputs[0].Union.Ki = new HookNative.KEYBDINPUT { Vk = HookNative.VK_CONTROL };
        inputs[1].Type = HookNative.INPUT_KEYBOARD;
        inputs[1].Union.Ki = new HookNative.KEYBDINPUT
        {
            Vk = HookNative.VK_CONTROL,
            Flags = HookNative.KEYEVENTF_KEYUP,
        };

        var sent = HookNative.SendInput(2, inputs, Marshal.SizeOf<HookNative.INPUT>());
        if (sent != 2)
            Log.Error($"SendCtrlTap: SendInput sent {sent}/2 events");
    }
}
