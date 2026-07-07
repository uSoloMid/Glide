namespace Glide.Engine;

/// <summary>Matches foreground process names against the user's blacklist.</summary>
public static class ExclusionChecker
{
    public static bool IsExcluded(string? processName, IEnumerable<string> excludedApps)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return false;

        var name = Normalize(processName);
        return excludedApps.Any(app => Normalize(app) == name);
    }

    private static string Normalize(string value)
    {
        var name = Path.GetFileName(value.Trim());
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];
        return name.ToLowerInvariant();
    }
}
