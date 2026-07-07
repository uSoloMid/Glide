using SharpGen.Runtime;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Glide.Graphics;

/// <summary>
/// Wraps IDXGIOutputDuplication for a single output and keeps a private GPU
/// copy of the latest desktop frame plus its shader resource view.
/// </summary>
public sealed class DesktopDuplicator : IDisposable
{
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private IDXGIOutputDuplication? _duplication;
    private ID3D11Texture2D? _texture;

    public ID3D11ShaderResourceView? ShaderView { get; private set; }
    public bool HasFrame { get; private set; }

    public DesktopDuplicator(ID3D11Device device, ID3D11DeviceContext context, IDXGIOutput output)
    {
        _device = device;
        _context = context;
        using var output1 = output.QueryInterface<IDXGIOutput1>();
        try
        {
            _duplication = output1.DuplicateOutput(device);
        }
        catch (SharpGenException ex)
        {
            throw new DuplicationLostException("DuplicateOutput failed", ex);
        }
    }

    /// <summary>
    /// Pulls the next desktop frame if one is ready within <paramref name="timeoutMs"/>.
    /// Returns false on timeout (the previous frame stays valid).
    /// </summary>
    public bool TryAcquireFrame(int timeoutMs)
    {
        if (_duplication is null)
            throw new DuplicationLostException("Duplication is not initialized");

        var result = _duplication.AcquireNextFrame((uint)timeoutMs, out _, out IDXGIResource? resource);
        if (result == Vortice.DXGI.ResultCode.WaitTimeout)
            return false;
        if (result.Failure)
            throw new DuplicationLostException($"AcquireNextFrame failed: {result}");

        try
        {
            using var frameTexture = resource!.QueryInterface<ID3D11Texture2D>();
            EnsureTargetTexture(frameTexture);
            _context.CopyResource(_texture!, frameTexture);
            HasFrame = true;
            return true;
        }
        finally
        {
            resource?.Dispose();
            _duplication.ReleaseFrame();
        }
    }

    private void EnsureTargetTexture(ID3D11Texture2D source)
    {
        if (_texture is not null) return;

        var desc = source.Description;
        _texture = _device.CreateTexture2D(new Texture2DDescription
        {
            Width = desc.Width,
            Height = desc.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = desc.Format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None,
        });
        ShaderView = _device.CreateShaderResourceView(_texture);
    }

    public void Dispose()
    {
        ShaderView?.Dispose();
        ShaderView = null;
        _texture?.Dispose();
        _texture = null;
        _duplication?.Dispose();
        _duplication = null;
    }
}
