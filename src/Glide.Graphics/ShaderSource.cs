namespace Glide.Graphics;

/// <summary>HLSL for the fullscreen zoom pass, compiled at startup.</summary>
internal static class ShaderSource
{
    public const string Hlsl = """
        cbuffer Params : register(b0)
        {
            // xy = source origin (UV), zw = source size (UV)
            float4 SrcRect;
        };

        Texture2D DesktopTex : register(t0);
        SamplerState LinearSampler : register(s0);

        struct VSOut
        {
            float4 Pos : SV_Position;
            float2 Uv  : TEXCOORD0;
        };

        VSOut VSMain(uint id : SV_VertexID)
        {
            // Fullscreen triangle without a vertex buffer.
            float2 uv = float2((id << 1) & 2, id & 2);
            VSOut o;
            o.Pos = float4(uv * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
            o.Uv = uv;
            return o;
        }

        float4 PSMain(VSOut input) : SV_Target
        {
            float2 uv = SrcRect.xy + input.Uv * SrcRect.zw;
            return DesktopTex.Sample(LinearSampler, uv);
        }
        """;
}
