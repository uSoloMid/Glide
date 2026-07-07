using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Glide.UI;

/// <summary>System tray icon with the Glide quick menu.</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _enabledItem;
    private Icon? _icon;

    public event Action<bool>? EnabledChanged;
    public event Action? SettingsRequested;
    public event Action? RestartRequested;
    public event Action? ExitRequested;

    public TrayIcon(bool enabled)
    {
        _enabledItem = new ToolStripMenuItem("Enable Glide")
        {
            Checked = enabled,
            CheckOnClick = true,
        };
        _enabledItem.CheckedChanged += (_, _) => EnabledChanged?.Invoke(_enabledItem.Checked);

        var settingsItem = new ToolStripMenuItem("Settings…");
        settingsItem.Click += (_, _) => SettingsRequested?.Invoke();

        var restartItem = new ToolStripMenuItem("Restart engine");
        restartItem.Click += (_, _) => RestartRequested?.Invoke();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_enabledItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(restartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _icon = CreateGlideIcon();
        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "Glide — cursor-centered zoom",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => SettingsRequested?.Invoke();
    }

    public void SetEnabled(bool enabled) => _enabledItem.Checked = enabled;

    /// <summary>Draws the magnifier-glyph icon at runtime (no asset pipeline needed).</summary>
    private static Icon CreateGlideIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var pen = new Pen(Color.FromArgb(255, 76, 194, 255), 4.5f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            g.DrawEllipse(pen, 4, 4, 16, 16);
            g.DrawLine(pen, 18.5f, 18.5f, 27, 27);
        }
        var handle = bmp.GetHicon();
        try
        {
            // Clone so we can free the GDI handle immediately.
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon?.Dispose();
        _icon = null;
    }
}
