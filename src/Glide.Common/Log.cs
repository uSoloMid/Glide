namespace Glide.Common;

/// <summary>Minimal thread-safe file logger. Never throws.</summary>
public static class Log
{
    private static readonly object Sync = new();
    private static string? _path;

    public static void Init(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, "glide.log");
            // Keep the log from growing forever.
            var info = new FileInfo(_path);
            if (info.Exists && info.Length > 1_000_000)
                info.Delete();
        }
        catch
        {
            _path = null;
        }
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message} :: {ex}");

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        if (_path is null) return;
        lock (Sync)
        {
            try
            {
                File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch
            {
                // Logging must never crash the app.
            }
        }
    }
}
