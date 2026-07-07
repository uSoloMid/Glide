using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Glide.Settings;
using MessageBox = System.Windows.MessageBox;
using RadioButton = System.Windows.Controls.RadioButton;

namespace Glide.UI;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    /// <summary>Set by the app right before a real shutdown.</summary>
    public bool AllowClose { get; set; }

    /// <summary>Raised when the user asks to reset every setting.</summary>
    public event Action? ResetRequested;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        SourceInitialized += (_, _) => EnableDarkTitleBar();
        Closing += (_, e) =>
        {
            if (AllowClose) return;
            e.Cancel = true;
            Hide();
        };
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (PanelGeneral is null) return; // fired during InitializeComponent
        var tag = (sender as RadioButton)?.Tag as string;

        PanelGeneral.Visibility = Show(tag == "General");
        PanelZoom.Visibility = Show(tag == "Zoom");
        PanelMouse.Visibility = Show(tag == "Mouse");
        PanelPerformance.Visibility = Show(tag == "Performance");
        PanelApplications.Visibility = Show(tag == "Applications");
        PanelAdvanced.Visibility = Show(tag == "Advanced");
        PanelAbout.Visibility = Show(tag == "About");
    }

    private static Visibility Show(bool visible) =>
        visible ? Visibility.Visible : Visibility.Collapsed;

    private void OnAddExclusion(object sender, RoutedEventArgs e) => _viewModel.AddExclusion();

    private void OnExclusionKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            _viewModel.AddExclusion();
    }

    private void OnRemoveExclusion(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string app)
            _viewModel.RemoveExclusion(app);
    }

    private void OnOpenSettingsFolder(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(SettingsStore.DefaultDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = SettingsStore.DefaultDirectory,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Glide.Common.Log.Error("Failed to open settings folder", ex);
        }
    }

    private void OnResetDefaults(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(this,
            "Reset every setting to its default value?", "Glide",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
            ResetRequested?.Invoke();
    }

    private void EnableDarkTitleBar()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int enabled = 1;
            // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (19 on older Win10 builds)
            _ = DwmSetWindowAttribute(hwnd, 20, ref enabled, sizeof(int));
            _ = DwmSetWindowAttribute(hwnd, 19, ref enabled, sizeof(int));
        }
        catch
        {
            // Cosmetic only.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
