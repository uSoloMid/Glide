using System.Text.Json;
using System.Text.Json.Serialization;
using Glide.Common;

namespace Glide.Settings;

/// <summary>Loads and saves <see cref="GlideSettings"/> as JSON in %APPDATA%\Glide.</summary>
public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Glide");

    public static string DefaultPath => Path.Combine(DefaultDirectory, "settings.json");

    public static GlideSettings Load() => Load(DefaultPath);

    public static GlideSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new GlideSettings();

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<GlideSettings>(json, JsonOptions);
            return SettingsValidator.Sanitize(settings ?? new GlideSettings());
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load settings from {path}; using defaults", ex);
            return new GlideSettings();
        }
    }

    public static void Save(GlideSettings settings) => Save(settings, DefaultPath);

    public static void Save(GlideSettings settings, string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var temp = path + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save settings to {path}", ex);
        }
    }
}
