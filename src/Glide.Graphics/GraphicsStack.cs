using Glide.Common;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Glide.Graphics;

/// <summary>
/// Everything needed to zoom one monitor: a D3D11 device on the adapter that
/// owns the output, the overlay window, the desktop duplicator and the renderer.
/// Create, use and dispose on the same (render) thread.
/// </summary>
public sealed class GraphicsStack : IDisposable
{
    private static readonly FeatureLevel[] Levels =
    [
        FeatureLevel.Level_11_1,
        FeatureLevel.Level_11_0,
        FeatureLevel.Level_10_1,
        FeatureLevel.Level_10_0,
    ];

    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;

    public OverlayWindow Window { get; }
    public DesktopDuplicator Duplicator { get; }
    public ZoomRenderer Renderer { get; }

    public GraphicsStack(string monitorDeviceName, RectI bounds)
    {
        using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory2>();
        var (adapter, output) = FindOutput(factory, monitorDeviceName);
        try
        {
            D3D11.D3D11CreateDevice(
                adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport, Levels,
                out _device!, out _context!).CheckError();

            Window = new OverlayWindow(bounds);
            Duplicator = new DesktopDuplicator(_device, _context, output);
            Renderer = new ZoomRenderer(_device, _context, factory, Window.Hwnd,
                bounds.Width, bounds.Height);
        }
        catch
        {
            Dispose();
            throw;
        }
        finally
        {
            output.Dispose();
            adapter.Dispose();
        }
    }

    private static (IDXGIAdapter1 Adapter, IDXGIOutput Output) FindOutput(
        IDXGIFactory2 factory, string monitorDeviceName)
    {
        for (uint a = 0; factory.EnumAdapters1(a, out IDXGIAdapter1 adapter).Success; a++)
        {
            for (uint o = 0; adapter.EnumOutputs(o, out IDXGIOutput output).Success; o++)
            {
                if (string.Equals(output.Description.DeviceName, monitorDeviceName,
                        StringComparison.OrdinalIgnoreCase))
                    return (adapter, output);
                output.Dispose();
            }
            adapter.Dispose();
        }
        throw new DuplicationLostException($"Output not found for monitor '{monitorDeviceName}'");
    }

    public void Dispose()
    {
        Renderer?.Dispose();
        Duplicator?.Dispose();
        Window?.Dispose();
        _context?.Dispose();
        _device?.Dispose();
    }
}
