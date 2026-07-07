using System.IO;
using System.Windows;
using System.Windows.Threading;
using Glide.Common;
using Glide.Engine;
using Glide.Input;
using Glide.Settings;

namespace Glide.UI;

/// <summary>Composition root: wires settings, engine, input hooks, tray and UI.</summary>
public partial class App : System.Windows.Application
{
    private Mutex? _singleInstanceMutex;
    private GlideSettings _settings = new();
    private GlideEngine? _engine;
    private InputManager? _input;
    private TrayIcon? _tray;
    private SettingsWindow? _settingsWindow;
    private SettingsViewModel? _viewModel;
    private DispatcherTimer? _saveDebounce;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, @"Local\Glide.SingleInstance", out var isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }

        Log.Init(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Glide"));
        Log.Info("Glide starting");

        bool firstRun = !File.Exists(SettingsStore.DefaultPath);
        _settings = SettingsStore.Load();

        _engine = new GlideEngine(_settings) { Enabled = _settings.Enabled };
        _input = new InputManager
        {
            ZoomTick = (ticks, x, y) => _engine.HandleZoomTick(ticks, x, y),
            ModifierReleased = () => _engine.HandleModifierReleased(),
            ModifierDoubleTapped = () => _engine.HandleDoubleTap(),
            PanButton = (down, x, y) => _engine.HandlePanButton(down, x, y),
            MouseMoved = (x, y) => _engine.HandleMouseMove(x, y),
            IsZoomActive = () => _engine.IsZoomActive,
        };
        _input.Configure(_settings.Modifier, _settings.DoubleTapReset);
        _input.Start();

        _tray = new TrayIcon(_settings.Enabled);
        _tray.EnabledChanged += OnTrayEnabledChanged;
        _tray.SettingsRequested += ShowSettingsWindow;
        _tray.RestartRequested += () => _engine?.Restart();
        _tray.ExitRequested += ExitApplication;

        StartupManager.Apply(_settings.StartWithWindows);

        bool silent = e.Args.Contains("--silent", StringComparer.OrdinalIgnoreCase);
        if (firstRun && !silent)
            ShowSettingsWindow();

        Log.Info("Glide ready");
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is null)
        {
            _viewModel = new SettingsViewModel(_settings);
            _viewModel.Changed += ScheduleApply;
            _settingsWindow = new SettingsWindow(_viewModel);
            _settingsWindow.ResetRequested += ResetToDefaults;
        }

        _settingsWindow.Show();
        if (_settingsWindow.WindowState == WindowState.Minimized)
            _settingsWindow.WindowState = WindowState.Normal;
        _settingsWindow.Activate();
    }

    /// <summary>Debounces slider drags so we save/apply at most ~3×/second.</summary>
    private void ScheduleApply()
    {
        _saveDebounce ??= CreateDebounceTimer();
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }

    private DispatcherTimer CreateDebounceTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (_viewModel is not null)
                ApplySettings(_viewModel.ToSettings());
        };
        return timer;
    }

    private void ApplySettings(GlideSettings settings)
    {
        _settings = settings;
        SettingsStore.Save(settings);
        _engine?.ApplySettings(settings);
        if (_engine is not null)
            _engine.Enabled = settings.Enabled;
        _input?.Configure(settings.Modifier, settings.DoubleTapReset);
        _tray?.SetEnabled(settings.Enabled);
        StartupManager.Apply(settings.StartWithWindows);
    }

    private void OnTrayEnabledChanged(bool enabled)
    {
        _settings.Enabled = enabled;
        SettingsStore.Save(_settings);
        if (_engine is not null)
            _engine.Enabled = enabled;
        _viewModel?.ReplaceModel(_settings);
    }

    private void ResetToDefaults()
    {
        var defaults = new GlideSettings();
        ApplySettings(defaults);
        _viewModel?.ReplaceModel(defaults);
    }

    private void ExitApplication()
    {
        if (_settingsWindow is not null)
            _settingsWindow.AllowClose = true;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Info("Glide shutting down");
        _tray?.Dispose();
        _input?.Dispose();
        _engine?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
