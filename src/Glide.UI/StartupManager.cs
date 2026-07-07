using Glide.Common;
using Microsoft.Win32;

namespace Glide.UI;

/// <summary>Registers/unregisters Glide in HKCU Run for silent startup with Windows.</summary>
public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Glide";

    public static void Apply(bool startWithWindows)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key is null) return;

            if (startWithWindows && Environment.ProcessPath is { } exePath)
                key.SetValue(ValueName, $"\"{exePath}\" --silent");
            else if (key.GetValue(ValueName) is not null)
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to update startup registration", ex);
        }
    }
}
