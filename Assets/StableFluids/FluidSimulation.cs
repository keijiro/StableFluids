using System;
using UnityEngine;
using Kernels = StableFluids.FluidCompute.Kernels;

namespace StableFluids {

public sealed class FluidSimulation : IDisposable
{
    #region Simulation parameters

    public float Viscosity { get; set; } = 1e-6f;
    public float Force { get; set; } = 300;
    public float Exponent { get; set; } = 200;

    #endregion

    #region Read-only properties

    public RenderTexture VelocityField => _buffers.v1;

    #endregion

    #region Private members

    readonly ComputeShader _compute;

    Vector2Int _threadCount;
    Vector2Int Resolution => _threadCount * 8;

    (RenderTexture v1,
     RenderTexture v2,
     RenderTexture v3,
     RenderTexture p1,
     RenderTexture p2) _buffers;

    #endregion

    #region Constructor and Dispose

    public FluidSimulation(ComputeShader compute, int width, int height)
    {
        _compute = compute;
        _threadCount = new Vector2Int(width + 7, height + 7) / 8;
        _buffers.v1 = RTUtil.AllocateUavRg(Resolution);
        _buffers.v2 = RTUtil.AllocateUavRg(Resolution);
        _buffers.v3 = RTUtil.AllocateUavRg(Resolution);
        _buffers.p1 = RTUtil.AllocateUavR(Resolution);
        _buffers.p2 = RTUtil.AllocateUavR(Resolution);
    }

    public void Dispose()
    {
        if (_buffers.v1 != null) UnityEngine.Object.Destroy(_buffers.v1);
        if (_buffers.v2 != null) UnityEngine.Object.Destroy(_buffers.v2);
        if (_buffers.v3 != null) UnityEngine.Object.Destroy(_buffers.v3);
        if (_buffers.p1 != null) UnityEngine.Object.Destroy(_buffers.p1);
        if (_buffers.p2 != null) UnityEngine.Object.Destroy(_buffers.p2);
    }

    #endregion

    #region Simulation method

    public void Step(float deltaTime, Vector2 forceOrigin, Vector2 forceVector)
    {
        var dx = 1.0f / Resolution.y;

        _compute.SetFloat("Time", Time.time);
        _compute.SetFloat("DeltaTime", deltaTime);

        _compute.SetTexture(Kernels.Advect, "U_in", _buffers.v1);
        _compute.SetTexture(Kernels.Advect, "W_out", _buffers.v2);
        _compute.Dispatch(Kernels.Advect, _threadCount);

        var dif_alpha = dx * dx / (Viscosity * deltaTime);
        _compute.SetFloat("Alpha", dif_alpha);
        _compute.SetFloat("Beta", 4 + dif_alpha);
        Graphics.CopyTexture(_buffers.v2, _buffers.v1);
        _compute.SetTexture(Kernels.Jacobi2, "B2_in", _buffers.v1);

        for (var i = 0; i < 20; i++)
        {
            _compute.SetTexture(Kernels.Jacobi2, "X2_in", _buffers.v2);
            _compute.SetTexture(Kernels.Jacobi2, "X2_out", _buffers.v3);
            _compute.Dispatch(Kernels.Jacobi2, _threadCount);

            _compute.SetTexture(Kernels.Jacobi2, "X2_in", _buffers.v3);
            _compute.SetTexture(Kernels.Jacobi2, "X2_out", _buffers.v2);
            _compute.Dispatch(Kernels.Jacobi2, _threadCount);
        }

        _compute.SetVector("ForceOrigin", forceOrigin);
        _compute.SetFloat("ForceExponent", Exponent);
        _compute.SetVector("ForceVector", forceVector);
        _compute.SetTexture(Kernels.Force, "W_in", _buffers.v2);
        _compute.SetTexture(Kernels.Force, "W_out", _buffers.v3);
        _compute.Dispatch(Kernels.Force, _threadCount);

        _compute.SetTexture(Kernels.PSetup, "W_in", _buffers.v3);
        _compute.SetTexture(Kernels.PSetup, "DivW_out", _buffers.v2);
        _compute.SetTexture(Kernels.PSetup, "P_out", _buffers.p1);
        _compute.Dispatch(Kernels.PSetup, _threadCount);

        _compute.SetFloat("Alpha", -dx * dx);
        _compute.SetFloat("Beta", 4);
        _compute.SetTexture(Kernels.Jacobi1, "B1_in", _buffers.v2);

        for (var i = 0; i < 20; i++)
        {
            _compute.SetTexture(Kernels.Jacobi1, "X1_in", _buffers.p1);
            _compute.SetTexture(Kernels.Jacobi1, "X1_out", _buffers.p2);
            _compute.Dispatch(Kernels.Jacobi1, _threadCount);

            _compute.SetTexture(Kernels.Jacobi1, "X1_in", _buffers.p2);
            _compute.SetTexture(Kernels.Jacobi1, "X1_out", _buffers.p1);
            _compute.Dispatch(Kernels.Jacobi1, _threadCount);
        }

        _compute.SetTexture(Kernels.PFinish, "W_in", _buffers.v3);
        _compute.SetTexture(Kernels.PFinish, "P_in", _buffers.p1);
        _compute.SetTexture(Kernels.PFinish, "U_out", _buffers.v1);
        _compute.Dispatch(Kernels.PFinish, _threadCount);
    }

    #endregion
}

} // namespace StableFluids