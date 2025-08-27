using System;
using UnityEngine;

namespace StableFluids {

public sealed class FluidSimulation : IDisposable
{
    #region Simulation parameters

    public float Viscosity { get; set; } = 1e-6f;

    #endregion

    #region Private members

    readonly Vector2Int _resolution;
    RenderTexture _v1, _v2, _v3, _p1, _p2, _divW;
    Material _mat;

    #endregion

    #region Constructor and Dispose

    public FluidSimulation(RenderTexture velocityField, Shader kernelsShader)
    {
        _v1 = velocityField;
        _resolution = new Vector2Int(_v1.width, _v1.height);

        _v2 = RTUtil.AllocateRGHalf(_resolution);
        _v3 = RTUtil.AllocateRGHalf(_resolution);
        _p1 = RTUtil.AllocateRHalf(_resolution);
        _p2 = RTUtil.AllocateRHalf(_resolution);
        _divW = RTUtil.AllocateRHalf(_resolution);

        _mat = new Material(kernelsShader);
        _mat.SetInt(ShaderIDs.TexWidth, _resolution.x);
        _mat.SetInt(ShaderIDs.TexHeight, _resolution.y);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(_v2);
        UnityEngine.Object.Destroy(_v3);
        UnityEngine.Object.Destroy(_p1);
        UnityEngine.Object.Destroy(_p2);
        UnityEngine.Object.Destroy(_divW);
        UnityEngine.Object.Destroy(_mat);
    }

    #endregion

    #region Simulation methods

    public void ClearVelocityField()
    {
        Graphics.Blit(Texture2D.blackTexture, _v1);
    }

    public void ApplyForceField(RenderTexture forceField)
    {
        _mat.SetTexture(ShaderIDs.ForceField, forceField);
        Graphics.Blit(_v2, _v3, _mat, 0);
        (_v2, _v3) = (_v3, _v2);
    }

    public void PreStep()
    {
        var dt = Time.deltaTime;

        // Advection U -> W (_v1 -> v2)
        Graphics.Blit(_v1, _v2, _mat, 1);

        // Diffusion via Jacobi on vector field (pass 4)
        var dx = 1.0f / _resolution.x;
        var dif_alpha = dx * dx / (Mathf.Max(Viscosity, 1e-12f) * Mathf.Max(dt, 1e-12f));
        var beta = 4 + dif_alpha;

        Graphics.CopyTexture(_v2, _v1); // B2_in = _v1

        // Iterate: X2_in/out over v2 <-> v3
        for (var i = 0; i < 20; i++)
        {
            _mat.SetTexture(ShaderIDs.X, _v2);
            _mat.SetTexture(ShaderIDs.B, _v1);
            _mat.SetFloat(ShaderIDs.Alpha, dif_alpha);
            _mat.SetFloat(ShaderIDs.Beta, beta);
            Graphics.Blit(_v2, _v3, _mat, 4);
            (_v2, _v3) = (_v3, _v2);
        }
    }

    public void PostStep()
    {
        // PSetup: compute divergence of W_in (v2) to divW and clear p1 (pass 2)
        _mat.SetTexture(ShaderIDs.MainTex, _v2);
        Graphics.Blit(_v2, _divW, _mat, 2);
        Graphics.Blit(Texture2D.blackTexture, _p1);

        // Jacobi on scalar field for pressure
        var dx = 1.0f / _resolution.x;
        _mat.SetFloat(ShaderIDs.Alpha, -dx * dx);
        _mat.SetFloat(ShaderIDs.Beta, 4);
        for (var i = 0; i < 20; i++)
        {
            _mat.SetTexture(ShaderIDs.X, _p1);
            _mat.SetTexture(ShaderIDs.B, _divW);
            Graphics.Blit(_p1, _p2, _mat, 3);
            (_p1, _p2) = (_p2, _p1);
        }

        // PFinish (pass 5): subtract pressure gradient to get divergence-free field U_out (_v1)
        _mat.SetTexture(ShaderIDs.W, _v2);
        _mat.SetTexture(ShaderIDs.P, _p1);
        Graphics.Blit(_v2, _v1, _mat, 5);
    }

    #endregion
}

} // namespace StableFluids
