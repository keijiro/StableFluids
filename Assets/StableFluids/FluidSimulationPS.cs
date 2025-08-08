using System;
using UnityEngine;

namespace StableFluids {

public sealed class FluidSimulationPS : IDisposable, IFluidSimulation
{
    #region Simulation parameters
    public float Viscosity { get; set; } = 1e-6f;
    #endregion

    #region Read-only properties
    public RenderTexture VelocityField => _v1;
    #endregion

    #region Private members
    readonly Vector2Int _resolution;

    RenderTexture _v1, _v2, _v3, _p1, _p2, _divW;

    Material _mat;
    #endregion

    #region Constructor and Dispose
    public FluidSimulationPS(int width, int height)
    {
        _resolution = new Vector2Int(width, height);

        _v1 = RTUtil.AllocateUavRg(_resolution);
        _v2 = RTUtil.AllocateUavRg(_resolution);
        _v3 = RTUtil.AllocateUavRg(_resolution);
        _p1 = RTUtil.AllocateUavR(_resolution);
        _p2 = RTUtil.AllocateUavR(_resolution);
        _divW = RTUtil.AllocateUavR(_resolution);

        _mat = new Material(Shader.Find("Hidden/StableFluids/PixelKernels"));
        _mat.SetInteger("_TexWidth", width);
        _mat.SetInteger("_TexHeight", height);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(_v1);
        UnityEngine.Object.Destroy(_v2);
        UnityEngine.Object.Destroy(_v3);
        UnityEngine.Object.Destroy(_p1);
        UnityEngine.Object.Destroy(_p2);
        UnityEngine.Object.Destroy(_divW);

        UnityEngine.Object.Destroy(_mat);
    }
    #endregion

    #region Simulation methods
    public void PreStep(float deltaTime)
    {
        // Advection U -> W (v1 -> v2)
        _mat.SetFloat("_DeltaTime", deltaTime);
        Graphics.Blit(_v1, _v2, _mat, 0);

        // Diffusion via Jacobi on vector field (pass 4)
        var dx = 1.0f / _resolution.x;
        var dif_alpha = dx * dx / (Mathf.Max(Viscosity, 1e-12f) * Mathf.Max(deltaTime, 1e-12f));
        var beta = 4 + dif_alpha;

        Graphics.CopyTexture(_v2, _v1); // B2_in = v1

        // Iterate: X2_in/out over v2 <-> v3
        for (var i = 0; i < 20; i++)
        {
            _mat.SetTexture("_X", _v2);
            _mat.SetTexture("_B", _v1);
            _mat.SetFloat("_Alpha", dif_alpha);
            _mat.SetFloat("_Beta", beta);
            Graphics.Blit(_v2, _v3, _mat, 4);

            // swap v2/v3
            var tmp = _v2; _v2 = _v3; _v3 = tmp;
        }
    }

    public void PostStep(float deltaTime)
    {
        // PSetup: compute divergence of W_in (v3) to divW and clear p1 (pass 2)
        _mat.SetTexture("_MainTex", _v3);
        Graphics.Blit(_v3, _divW, _mat, 2);
        Graphics.Blit(Texture2D.blackTexture, _p1);

        // Jacobi on scalar field for pressure
        var dx = 1.0f / _resolution.x;
        _mat.SetFloat("_Alpha", -dx * dx);
        _mat.SetFloat("_Beta", 4);
        for (var i = 0; i < 20; i++)
        {
            _mat.SetTexture("_X", _p1);
            _mat.SetTexture("_B", _divW);
            Graphics.Blit(_p1, _p2, _mat, 3);
            var tmp = _p1; _p1 = _p2; _p2 = tmp;
        }

        // PFinish (pass 5): subtract pressure gradient to get divergence-free field U_out (v1)
        _mat.SetTexture("_W", _v3);
        _mat.SetTexture("_P", _p1);
        Graphics.Blit(_v3, _v1, _mat, 5);
    }
    #endregion

    #region Force methods
    public void ApplyPointForce(Vector2 origin, Vector2 force, float exponent)
    {
        _mat.SetVector("_ForceOrigin", origin);
        _mat.SetFloat("_ForceExponent", exponent);
        _mat.SetVector("_ForceVector", force);
        _mat.SetTexture("_MainTex", _v2);
        Graphics.Blit(_v2, _v3, _mat, 1);
    }
    #endregion
}

} // namespace StableFluids
