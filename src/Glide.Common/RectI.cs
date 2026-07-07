namespace Glide.Common;

/// <summary>Integer rectangle in physical screen pixels.</summary>
public readonly record struct RectI(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public int CenterX => X + Width / 2;
    public int CenterY => Y + Height / 2;

    public bool Contains(int px, int py) =>
        px >= X && px < Right && py >= Y && py < Bottom;
}
