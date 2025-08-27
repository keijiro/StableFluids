using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace StableFluids {

static class RTUtil
{
    public static RenderTexture Allocate
      (int width, int height, RenderTextureFormat format)
    {
        var rt = new RenderTexture(width, height, 0, format);
        rt.Create();
        return rt;
    }

    public static RenderTexture AllocateCompatible
      (int width, int height, GraphicsFormat requestedFormat)
    {
        var compatibleFormat = SystemInfo.GetCompatibleFormat(
            requestedFormat,
            GraphicsFormatUsage.Render
        );

        var descriptor = new RenderTextureDescriptor(width, height)
        {
            graphicsFormat = compatibleFormat,
            depthBufferBits = 0,
            msaaSamples = 1
        };

        var rt = new RenderTexture(descriptor);
        rt.Create();
        return rt;
    }

    public static RenderTexture AllocateRHalf(Vector2Int dims)
      => AllocateCompatible(dims.x, dims.y, GraphicsFormat.R16_SFloat);

    public static RenderTexture AllocateRGHalf(Vector2Int dims)
      => AllocateCompatible(dims.x, dims.y, GraphicsFormat.R16G16_SFloat);

    public static RenderTexture GetTemporaryCompatible(RenderTexture source)
    {
        var descriptor = source.descriptor;
        descriptor.graphicsFormat = SystemInfo.GetCompatibleFormat(
            descriptor.graphicsFormat,
            GraphicsFormatUsage.Render
        );
        return RenderTexture.GetTemporary(descriptor);
    }
}

static class ShaderIDs
{
    public static readonly int TexWidth = Shader.PropertyToID("_TexWidth");
    public static readonly int TexHeight = Shader.PropertyToID("_TexHeight");

    public static readonly int X = Shader.PropertyToID("_X");
    public static readonly int B = Shader.PropertyToID("_B");
    public static readonly int Alpha = Shader.PropertyToID("_Alpha");
    public static readonly int Beta = Shader.PropertyToID("_Beta");

    public static readonly int MainTex = Shader.PropertyToID("_MainTex");
    public static readonly int W = Shader.PropertyToID("_W");
    public static readonly int P = Shader.PropertyToID("_P");
    public static readonly int ForceField = Shader.PropertyToID("_ForceField");
}

} // namespace StableFluids
