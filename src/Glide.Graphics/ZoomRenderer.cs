using System.Numerics;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Glide.Graphics;

/// <summary>Normalized (UV) source rectangle to display fullscreen.</summary>
public readonly record struct ViewRect(float U, float V, float Width, float Height)
{
    public static readonly ViewRect Full = new(0f, 0f, 1f, 1f);
}

/// <summary>
/// Renders the captured desktop texture into the overlay window's swap chain,
/// scaled so that <see cref="ViewRect"/> fills the monitor.
/// </summary>
public sealed class ZoomRenderer : IDisposable
{
    private readonly ID3D11DeviceContext _context;
    private readonly IDXGISwapChain1 _swapChain;
    private readonly ID3D11RenderTargetView _renderTarget;
    private readonly ID3D11VertexShader _vertexShader;
    private readonly ID3D11PixelShader _pixelShader;
    private readonly ID3D11SamplerState _sampler;
    private readonly ID3D11Buffer _constantBuffer;
    private readonly int _width;
    private readonly int _height;

    public ZoomRenderer(ID3D11Device device, ID3D11DeviceContext context,
        IDXGIFactory2 factory, IntPtr hwnd, int width, int height)
    {
        _context = context;
        _width = width;
        _height = height;

        _swapChain = factory.CreateSwapChainForHwnd(device, hwnd, new SwapChainDescription1
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
        });

        using var backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _renderTarget = device.CreateRenderTargetView(backBuffer);

        var vsBytecode = Compiler.Compile(ShaderSource.Hlsl, "VSMain", "glide.hlsl", "vs_5_0");
        var psBytecode = Compiler.Compile(ShaderSource.Hlsl, "PSMain", "glide.hlsl", "ps_5_0");
        _vertexShader = device.CreateVertexShader(vsBytecode.Span);
        _pixelShader = device.CreatePixelShader(psBytecode.Span);

        _sampler = device.CreateSamplerState(new SamplerDescription(
            Filter.MinMagMipLinear,
            TextureAddressMode.Clamp, TextureAddressMode.Clamp, TextureAddressMode.Clamp));

        _constantBuffer = device.CreateBuffer(new BufferDescription(
            16, BindFlags.ConstantBuffer, ResourceUsage.Default));
    }

    public void Render(ID3D11ShaderResourceView? desktop, ViewRect view, bool vsync)
    {
        if (desktop is null) return;

        var rect = new Vector4(view.U, view.V, view.Width, view.Height);
        _context.UpdateSubresource(rect, _constantBuffer);

        _context.OMSetRenderTargets(_renderTarget);
        _context.RSSetViewport(new Viewport(0, 0, _width, _height));
        _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _context.IASetInputLayout(null);
        _context.VSSetShader(_vertexShader);
        _context.PSSetShader(_pixelShader);
        _context.PSSetConstantBuffer(0, _constantBuffer);
        _context.PSSetShaderResource(0, desktop);
        _context.PSSetSampler(0, _sampler);
        _context.Draw(3, 0);

        // Unbind so CopyResource on the capture texture never conflicts.
        _context.PSSetShaderResource(0, null!);

        _swapChain.Present(vsync ? 1u : 0u, PresentFlags.None);
    }

    public void Dispose()
    {
        _constantBuffer.Dispose();
        _sampler.Dispose();
        _pixelShader.Dispose();
        _vertexShader.Dispose();
        _renderTarget.Dispose();
        _swapChain.Dispose();
    }
}
