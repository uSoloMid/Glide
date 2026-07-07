using Glide.Graphics;

namespace Glide.Engine;

/// <summary>Per-frame instructions handed from the engine to a render session.</summary>
public readonly record struct FrameState(ViewRect View, bool Idle, bool VSync, int MaxFps);
