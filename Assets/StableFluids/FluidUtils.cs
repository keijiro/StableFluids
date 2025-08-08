using UnityEngine;

namespace StableFluids {

static class ComputeShaderExtensions
{
    public static void Dispatch
      (this ComputeShader compute, int kernelIndex, Vector2Int threadGroups)
        => compute.Dispatch(kernelIndex, threadGroups.x, threadGroups.y, 1);
}

static class RTUtil
{
    public static RenderTexture AllocateUav
      (int width, int height, RenderTextureFormat format)
    {
        var rt = new RenderTexture(width, height, 0, format);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }

    public static RenderTexture AllocateUavR(Vector2Int dims)
      => AllocateUav(dims.x, dims.y, RenderTextureFormat.RHalf);

    public static RenderTexture AllocateUavRg(Vector2Int dims)
      => AllocateUav(dims.x, dims.y, RenderTextureFormat.RGHalf);

    public static RenderTexture AllocateUavRgba(int width, int height)
      => AllocateUav(width, height, RenderTextureFormat.ARGBHalf);
}

static class ShaderIDs
{
    public static readonly int TexWidth = Shader.PropertyToID("_TexWidth");
    public static readonly int TexHeight = Shader.PropertyToID("_TexHeight");
    public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");

    public static readonly int X = Shader.PropertyToID("_X");
    public static readonly int B = Shader.PropertyToID("_B");
    public static readonly int Alpha = Shader.PropertyToID("_Alpha");
    public static readonly int Beta = Shader.PropertyToID("_Beta");

    public static readonly int MainTex = Shader.PropertyToID("_MainTex");
    public static readonly int W = Shader.PropertyToID("_W");
    public static readonly int P = Shader.PropertyToID("_P");

    public static readonly int ForceOrigin = Shader.PropertyToID("_ForceOrigin");
    public static readonly int ForceVector = Shader.PropertyToID("_ForceVector");
    public static readonly int ForceExponent = Shader.PropertyToID("_ForceExponent");
}

} // namespace StableFluids
