namespace Glide.Graphics;

/// <summary>
/// Desktop duplication was invalidated (display mode change, secure desktop,
/// GPU reset). The whole graphics stack must be rebuilt.
/// </summary>
public sealed class DuplicationLostException(string message, Exception? inner = null)
    : Exception(message, inner);
