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

} // namespace StableFluids
