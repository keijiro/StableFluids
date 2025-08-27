using UnityEngine;

namespace StableFluids.Marbling {

public sealed class MarblingFluidSimulator : MonoBehaviour
{
    #region Public properties

    [field:SerializeField] public float Viscosity { get; set; } = 1e-6f;

    #endregion

    #region Editable attributes

    [SerializeField] RenderTexture _velocityField = null;
    [SerializeField] RenderTexture _forceField = null;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] Shader _kernelShader = null;

    #endregion

    #region Private objects

    FluidSimulation _simulation;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _simulation = new FluidSimulation(_velocityField, _kernelShader);
        _simulation.ClearVelocityField();
    }

    void OnDestroy()
    {
        _simulation.Dispose();
        _simulation = null;
    }

    void Update()
    {
        _simulation.Viscosity = Viscosity;
        _simulation.PreStep();
        _simulation.ApplyForceField(_forceField);
        _simulation.PostStep();
    }

    #endregion
}

} // namespace StableFluids.Marbling
