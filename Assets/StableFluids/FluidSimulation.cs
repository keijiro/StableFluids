using System;
using UnityEngine;
using Kernels = StableFluids.FluidCompute.Kernels;
using PropID = StableFluids.FluidCompute.PropertyIds;

namespace StableFluids {

public interface IFluidSimulation : IDisposable
{
    float Viscosity { get; set; }
    RenderTexture VelocityField { get; }
    void PreStep(float deltaTime);
    void PostStep(float deltaTime);
    void ApplyPointForce(Vector2 origin, Vector2 force, float exponent);
}

public sealed class FluidSimulation : IDisposable, IFluidSimulation
{
    #region Simulation parameters

    public float Viscosity { get; set; } = 1e-6f;

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

    #region Simulation methods

    public void PreStep(float deltaTime)
    {
        var dx = 1.0f / Resolution.x;

        _compute.SetFloat(PropID.Time, Time.time);
        _compute.SetFloat(PropID.DeltaTime, deltaTime);

        // Advection
        _compute.SetTexture(Kernels.Advect, PropID.U_in, _buffers.v1);
        _compute.SetTexture(Kernels.Advect, PropID.W_out, _buffers.v2);
        _compute.Dispatch(Kernels.Advect, _threadCount);

        // Diffusion
        var dif_alpha = dx * dx / (Viscosity * deltaTime);
        _compute.SetFloat(PropID.Alpha, dif_alpha);
        _compute.SetFloat(PropID.Beta, 4 + dif_alpha);
        Graphics.CopyTexture(_buffers.v2, _buffers.v1);
        _compute.SetTexture(Kernels.Jacobi2, PropID.B2_in, _buffers.v1);

        for (var i = 0; i < 20; i++)
        {
            _compute.SetTexture(Kernels.Jacobi2, PropID.X2_in, _buffers.v2);
            _compute.SetTexture(Kernels.Jacobi2, PropID.X2_out, _buffers.v3);
            _compute.Dispatch(Kernels.Jacobi2, _threadCount);

            _compute.SetTexture(Kernels.Jacobi2, PropID.X2_in, _buffers.v3);
            _compute.SetTexture(Kernels.Jacobi2, PropID.X2_out, _buffers.v2);
            _compute.Dispatch(Kernels.Jacobi2, _threadCount);
        }
    }

    public void PostStep(float deltaTime)
    {
        var dx = 1.0f / Resolution.x;

        // Projection
        _compute.SetTexture(Kernels.PSetup, PropID.W_in, _buffers.v3);
        _compute.SetTexture(Kernels.PSetup, PropID.DivW_out, _buffers.v2);
        _compute.SetTexture(Kernels.PSetup, PropID.P_out, _buffers.p1);
        _compute.Dispatch(Kernels.PSetup, _threadCount);

        _compute.SetFloat(PropID.Alpha, -dx * dx);
        _compute.SetFloat(PropID.Beta, 4);
        _compute.SetTexture(Kernels.Jacobi1, PropID.B1_in, _buffers.v2);

        for (var i = 0; i < 20; i++)
        {
            _compute.SetTexture(Kernels.Jacobi1, PropID.X1_in, _buffers.p1);
            _compute.SetTexture(Kernels.Jacobi1, PropID.X1_out, _buffers.p2);
            _compute.Dispatch(Kernels.Jacobi1, _threadCount);

            _compute.SetTexture(Kernels.Jacobi1, PropID.X1_in, _buffers.p2);
            _compute.SetTexture(Kernels.Jacobi1, PropID.X1_out, _buffers.p1);
            _compute.Dispatch(Kernels.Jacobi1, _threadCount);
        }

        _compute.SetTexture(Kernels.PFinish, PropID.W_in, _buffers.v3);
        _compute.SetTexture(Kernels.PFinish, PropID.P_in, _buffers.p1);
        _compute.SetTexture(Kernels.PFinish, PropID.U_out, _buffers.v1);
        _compute.Dispatch(Kernels.PFinish, _threadCount);
    }

    #endregion

    #region Force methods

    public void ApplyPointForce(Vector2 origin, Vector2 force, float exponent)
    {
        _compute.SetVector(PropID.ForceOrigin, origin);
        _compute.SetFloat(PropID.ForceExponent, exponent);
        _compute.SetVector(PropID.ForceVector, force);
        _compute.SetTexture(Kernels.Force, PropID.W_in, _buffers.v2);
        _compute.SetTexture(Kernels.Force, PropID.W_out, _buffers.v3);
        _compute.Dispatch(Kernels.Force, _threadCount);
    }

    #endregion
}

} // namespace StableFluids
