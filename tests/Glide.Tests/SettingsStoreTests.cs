using Glide.Common;
using Glide.Settings;
using Xunit;

namespace Glide.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "GlideTests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void RoundTripsAllValues()
    {
        var original = new GlideSettings
        {
            Enabled = false,
            Modifier = ModifierKey.Alt,
            Mode = ZoomMode.Persistent,
            MaxZoom = 6.5,
            ZoomSpeed = 2.0,
            Curve = EasingCurve.Spring,
            VSync = false,
            ExcludedApps = ["valorant.exe", "cs2.exe"],
        };

        SettingsStore.Save(original, SettingsPath);
        var loaded = SettingsStore.Load(SettingsPath);

        Assert.False(loaded.Enabled);
        Assert.Equal(ModifierKey.Alt, loaded.Modifier);
        Assert.Equal(ZoomMode.Persistent, loaded.Mode);
        Assert.Equal(6.5, loaded.MaxZoom, 6);
        Assert.Equal(2.0, loaded.ZoomSpeed, 6);
        Assert.Equal(EasingCurve.Spring, loaded.Curve);
        Assert.False(loaded.VSync);
        Assert.Equal(["valorant.exe", "cs2.exe"], loaded.ExcludedApps);
    }

    [Fact]
    public void MissingFileReturnsDefaults()
    {
        var loaded = SettingsStore.Load(Path.Combine(_dir, "does-not-exist.json"));
        Assert.True(loaded.Enabled);
        Assert.Equal(ModifierKey.Ctrl, loaded.Modifier);
        Assert.Equal(10.0, loaded.MaxZoom, 6);
    }

    [Fact]
    public void CorruptFileReturnsDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, "{ not valid json !!");

        var loaded = SettingsStore.Load(SettingsPath);

        Assert.Equal(ModifierKey.Ctrl, loaded.Modifier);
        Assert.Equal(1.0, loaded.MinZoom, 6);
    }

    [Fact]
    public void OutOfRangeValuesAreClamped()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """
            {
              "MinZoom": -5,
              "MaxZoom": 900,
              "ZoomSpeed": 100,
              "AnimationDurationMs": 99999,
              "MaxFps": 5,
              "ExcludedApps": ["  game.exe  ", "", "GAME.EXE"]
            }
            """);

        var loaded = SettingsStore.Load(SettingsPath);

        Assert.Equal(1.0, loaded.MinZoom, 6);
        Assert.Equal(10.0, loaded.MaxZoom, 6);
        Assert.Equal(3.0, loaded.ZoomSpeed, 6);
        Assert.Equal(1000, loaded.AnimationDurationMs);
        Assert.Equal(30, loaded.MaxFps);
        Assert.Single(loaded.ExcludedApps); // trimmed + deduplicated
        Assert.Equal("game.exe", loaded.ExcludedApps[0]);
    }

    [Fact]
    public void SwappedMinMaxZoomIsRepaired()
    {
        var settings = new GlideSettings { MinZoom = 8.0, MaxZoom = 2.0 };
        var sanitized = SettingsValidator.Sanitize(settings);

        Assert.True(sanitized.MinZoom <= sanitized.MaxZoom);
    }
}
