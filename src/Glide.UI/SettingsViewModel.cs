using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Glide.Common;
using Glide.Settings;

namespace Glide.UI;

/// <summary>
/// Binds the settings window to a working copy of <see cref="GlideSettings"/>.
/// Every change raises <see cref="Changed"/>; the app debounces, saves and
/// applies the new snapshot.
/// </summary>
public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private GlideSettings _model;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;

    public SettingsViewModel(GlideSettings settings)
    {
        _model = settings.Clone();
        Exclusions = new ObservableCollection<string>(_model.ExcludedApps);
    }

    public IReadOnlyList<ModifierKey> Modifiers { get; } = Enum.GetValues<ModifierKey>();
    public IReadOnlyList<EasingCurve> Curves { get; } = Enum.GetValues<EasingCurve>();

    /// <summary>Snapshot for saving/applying.</summary>
    public GlideSettings ToSettings()
    {
        var snapshot = _model.Clone();
        snapshot.ExcludedApps = [.. Exclusions];
        return SettingsValidator.Sanitize(snapshot);
    }

    public void ReplaceModel(GlideSettings settings)
    {
        _model = settings.Clone();
        Exclusions.Clear();
        foreach (var app in _model.ExcludedApps)
            Exclusions.Add(app);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    // --- General ---

    public bool Enabled
    {
        get => _model.Enabled;
        set => Set(v => _model.Enabled = v, value);
    }

    public bool TemporaryMode
    {
        get => _model.Mode == ZoomMode.Temporary;
        set => Set(v => _model.Mode = v ? ZoomMode.Temporary : ZoomMode.Persistent, value);
    }

    public bool AnimateReturn
    {
        get => _model.AnimateReturn;
        set => Set(v => _model.AnimateReturn = v, value);
    }

    public bool StartWithWindows
    {
        get => _model.StartWithWindows;
        set => Set(v => _model.StartWithWindows = v, value);
    }

    // --- Zoom ---

    public double MinZoomPercent
    {
        get => _model.MinZoom * 100.0;
        set => Set(v => _model.MinZoom = v / 100.0, value);
    }

    public double MaxZoomPercent
    {
        get => _model.MaxZoom * 100.0;
        set => Set(v => _model.MaxZoom = v / 100.0, value);
    }

    public double ZoomSpeed
    {
        get => _model.ZoomSpeed;
        set => Set(v => _model.ZoomSpeed = v, value);
    }

    public double ScrollSensitivity
    {
        get => _model.ScrollSensitivity;
        set => Set(v => _model.ScrollSensitivity = v, value);
    }

    public double AnimationDurationMs
    {
        get => _model.AnimationDurationMs;
        set => Set(v => _model.AnimationDurationMs = (int)v, value);
    }

    public EasingCurve SelectedCurve
    {
        get => _model.Curve;
        set => Set(v => _model.Curve = v, value);
    }

    // --- Mouse ---

    public ModifierKey SelectedModifier
    {
        get => _model.Modifier;
        set => Set(v => _model.Modifier = v, value);
    }

    public bool DoubleTapReset
    {
        get => _model.DoubleTapReset;
        set => Set(v => _model.DoubleTapReset = v, value);
    }

    public bool PanWithMiddleButton
    {
        get => _model.PanWithMiddleButton;
        set => Set(v => _model.PanWithMiddleButton = v, value);
    }

    public double PanSpeed
    {
        get => _model.PanSpeed;
        set => Set(v => _model.PanSpeed = v, value);
    }

    // --- Performance ---

    public bool VSync
    {
        get => _model.VSync;
        set => Set(v => _model.VSync = v, value);
    }

    public double MaxFps
    {
        get => _model.MaxFps;
        set => Set(v => _model.MaxFps = (int)v, value);
    }

    public bool ZoomAllMonitors
    {
        get => _model.MonitorMode == MonitorZoomMode.AllMonitors;
        set => Set(v => _model.MonitorMode = v ? MonitorZoomMode.AllMonitors : MonitorZoomMode.CursorMonitor, value);
    }

    // --- Applications ---

    public ObservableCollection<string> Exclusions { get; }

    private string _newExclusion = string.Empty;
    public string NewExclusion
    {
        get => _newExclusion;
        set
        {
            _newExclusion = value;
            OnPropertyChanged();
        }
    }

    public void AddExclusion()
    {
        var name = NewExclusion.Trim();
        if (name.Length == 0) return;
        if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name += ".exe";
        if (!Exclusions.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            Exclusions.Add(name);
            Changed?.Invoke();
        }
        NewExclusion = string.Empty;
    }

    public void RemoveExclusion(string app)
    {
        if (Exclusions.Remove(app))
            Changed?.Invoke();
    }

    // --- About ---

    public string VersionText =>
        $"Glide {typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "0.1.0"}";

    private void Set<T>(Action<T> apply, T value, [CallerMemberName] string? property = null)
    {
        apply(value);
        OnPropertyChanged(property);
        Changed?.Invoke();
    }

    private void OnPropertyChanged([CallerMemberName] string? property = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
